using System.CommandLine;
using FluentPDF.Verification.Core;
using Serilog;
using Serilog.Events;

namespace FluentPDF.Verification.Cli;

/// <summary>
/// Entry point for the PDFium verification CLI tool.
/// Provides command-line interface for running PDFium P/Invoke verification tests.
/// </summary>
internal class Program
{
    /// <summary>
    /// Exit code for successful verification (all tests passed).
    /// </summary>
    private const int ExitSuccess = 0;

    /// <summary>
    /// Exit code when verification fails (one or more tests failed).
    /// </summary>
    private const int ExitVerificationFailure = 1;

    /// <summary>
    /// Exit code for invalid command-line arguments.
    /// </summary>
    private const int ExitInvalidArguments = 2;

    /// <summary>
    /// Exit code for unexpected errors during execution.
    /// </summary>
    private const int ExitUnexpectedError = 3;

    /// <summary>
    /// Application version string.
    /// </summary>
    private const string Version = "1.0.0";

    private static async Task<int> Main(string[] args)
    {
        var rootCommand = BuildRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Builds the root command with all options and handlers.
    /// </summary>
    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("PDFium P/Invoke verification tool - validates function signatures, return types, and behaviors")
        {
            Name = "pdfium-verify"
        };

        // Define options
        var dllPathOption = new Option<string>(
            aliases: new[] { "--dll", "-d" },
            description: "Path to the pdfium.dll file to verify")
        {
            IsRequired = true
        };

        var testFilesOption = new Option<string?>(
            aliases: new[] { "--test-files", "-t" },
            description: "Directory containing test PDF files for behavior verification");

        var outputFormatOption = new Option<ReportFormat>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => ReportFormat.Console,
            description: "Output report format (Console, Json, Html)");

        var outputPathOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path for the report (default: stdout)");

        var parallelOption = new Option<bool>(
            aliases: new[] { "--parallel", "-p" },
            getDefaultValue: () => true,
            description: "Run verification tests in parallel");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => false,
            description: "Enable verbose logging");

        // Add options to command
        rootCommand.AddOption(dllPathOption);
        rootCommand.AddOption(testFilesOption);
        rootCommand.AddOption(outputFormatOption);
        rootCommand.AddOption(outputPathOption);
        rootCommand.AddOption(parallelOption);
        rootCommand.AddOption(verboseOption);

        // Set handler
        rootCommand.SetHandler(
            async (context) =>
            {
                var dllPath = context.ParseResult.GetValueForOption(dllPathOption)!;
                var testFiles = context.ParseResult.GetValueForOption(testFilesOption);
                var format = context.ParseResult.GetValueForOption(outputFormatOption);
                var outputPath = context.ParseResult.GetValueForOption(outputPathOption);
                var parallel = context.ParseResult.GetValueForOption(parallelOption);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);

                // Configure logging
                ConfigureLogging(verbose);

                // Validate DLL path
                if (!File.Exists(dllPath))
                {
                    Log.Error("DLL file not found: {DllPath}", dllPath);
                    Console.Error.WriteLine($"Error: DLL file not found: {dllPath}");
                    context.ExitCode = ExitInvalidArguments;
                    return;
                }

                // Validate test files directory if provided
                if (testFiles != null && !Directory.Exists(testFiles))
                {
                    Log.Error("Test files directory not found: {TestFiles}", testFiles);
                    Console.Error.WriteLine($"Error: Test files directory not found: {testFiles}");
                    context.ExitCode = ExitInvalidArguments;
                    return;
                }

                // Build verification options
                var options = new VerificationOptions
                {
                    DllPath = dllPath,
                    TestFileDirectory = testFiles,
                    OutputFormat = format,
                    OutputPath = outputPath,
                    ParallelExecution = parallel
                };

                try
                {
                    // Execute verification
                    Log.Information("Starting PDFium verification");
                    Log.Information("DLL Path: {DllPath}", dllPath);
                    Log.Information("Parallel Execution: {Parallel}", parallel);
                    Log.Information("Output Format: {Format}", format);

                    // Create DLL analyzer and executor
                    var dllAnalyzer = new DllAnalyzer();
                    var executor = new VerificationExecutor(options, dllAnalyzer);

                    // Execute verification
                    var result = await executor.ExecuteAsync(context.GetCancellationToken());

                    if (result.IsFailed)
                    {
                        Log.Error("Verification execution failed: {Error}", result.Errors[0].Message);
                        Console.Error.WriteLine($"Verification failed: {result.Errors[0].Message}");
                        context.ExitCode = ExitVerificationFailure;
                        return;
                    }

                    var summary = result.Value;

                    // Generate and output report (will be implemented in task 9)
                    // For now, output basic summary
                    Console.WriteLine();
                    Console.WriteLine("PDFium Verification Results");
                    Console.WriteLine("===========================");
                    Console.WriteLine($"Total Tests: {summary.TotalTests}");
                    Console.WriteLine($"Passed: {summary.PassedTests}");
                    Console.WriteLine($"Failed: {summary.FailedTests}");
                    Console.WriteLine($"Duration: {summary.TotalDuration.TotalSeconds:F2}s");
                    Console.WriteLine($"Library Version: {summary.LibraryVersion ?? "Unknown"}");
                    Console.WriteLine();

                    if (summary.FailedTests > 0)
                    {
                        Console.WriteLine("Failed Tests:");
                        foreach (var failedResult in summary.Results.Where(r => !r.Success))
                        {
                            Console.WriteLine($"  - {failedResult.TestName}");
                            Console.WriteLine($"    Error: {failedResult.ErrorMessage}");
                            if (!string.IsNullOrWhiteSpace(failedResult.SuggestedFix))
                            {
                                Console.WriteLine($"    Suggested Fix: {failedResult.SuggestedFix}");
                            }
                        }
                        context.ExitCode = ExitVerificationFailure;
                    }
                    else
                    {
                        Console.WriteLine("All tests passed!");
                        context.ExitCode = ExitSuccess;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error during verification");
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    context.ExitCode = ExitUnexpectedError;
                }
                finally
                {
                    await Log.CloseAndFlushAsync();
                }
            });

        return rootCommand;
    }

    /// <summary>
    /// Configures Serilog logging based on verbosity level.
    /// </summary>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    private static void ConfigureLogging(bool verbose)
    {
        var logLevel = verbose ? LogEventLevel.Debug : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Debug("Verbose logging enabled");
    }
}
