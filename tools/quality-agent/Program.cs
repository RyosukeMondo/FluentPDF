using System.CommandLine;
using FluentPDF.QualityAgent.Config;
using FluentPDF.QualityAgent.Models;
using FluentPDF.QualityAgent.Services;
using Serilog;

namespace FluentPDF.QualityAgent;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog for CLI logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var rootCommand = BuildRootCommand();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Quality agent failed with unhandled exception");
            return 2; // Fail
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("FluentPDF AI Quality Agent - Intelligent quality assessment for test results, logs, and visual regressions");

        // Define CLI options
        var trxFileOption = new Option<FileInfo?>(
            aliases: new[] { "--trx-file", "-t" },
            description: "Path to TRX test results file")
        {
            IsRequired = false
        };

        var logDirOption = new Option<DirectoryInfo?>(
            aliases: new[] { "--log-dir", "-l" },
            description: "Path to directory containing Serilog JSON logs")
        {
            IsRequired = false
        };

        var visualResultsOption = new Option<FileInfo?>(
            aliases: new[] { "--visual-results", "-v" },
            description: "Path to SSIM visual regression results JSON file")
        {
            IsRequired = false
        };

        var validationResultsOption = new Option<FileInfo?>(
            aliases: new[] { "--validation-results", "-r" },
            description: "Path to validation results JSON file")
        {
            IsRequired = false
        };

        var outputOption = new Option<FileInfo?>(
            aliases: new[] { "--output", "-o" },
            description: "Path to output quality report JSON file (default: quality-report.json)")
        {
            IsRequired = false
        };

        rootCommand.AddOption(trxFileOption);
        rootCommand.AddOption(logDirOption);
        rootCommand.AddOption(visualResultsOption);
        rootCommand.AddOption(validationResultsOption);
        rootCommand.AddOption(outputOption);

        rootCommand.SetHandler(
            ExecuteAnalysis,
            trxFileOption,
            logDirOption,
            visualResultsOption,
            validationResultsOption,
            outputOption);

        return rootCommand;
    }

    private static async Task<int> ExecuteAnalysis(
        FileInfo? trxFile,
        DirectoryInfo? logDir,
        FileInfo? visualResults,
        FileInfo? validationResults,
        FileInfo? output)
    {
        try
        {
            Log.Information("FluentPDF AI Quality Agent starting...");

            // Validate that at least one input is provided
            if (trxFile == null && logDir == null && visualResults == null && validationResults == null)
            {
                Log.Error("At least one input must be provided (--trx-file, --log-dir, --visual-results, or --validation-results)");
                return 2; // Fail
            }

            // Validate input paths
            if (trxFile != null && !trxFile.Exists)
            {
                Log.Error("TRX file not found: {TrxFile}", trxFile.FullName);
                return 2; // Fail
            }

            if (logDir != null && !logDir.Exists)
            {
                Log.Error("Log directory not found: {LogDir}", logDir.FullName);
                return 2; // Fail
            }

            if (visualResults != null && !visualResults.Exists)
            {
                Log.Error("Visual results file not found: {VisualResults}", visualResults.FullName);
                return 2; // Fail
            }

            if (validationResults != null && !validationResults.Exists)
            {
                Log.Error("Validation results file not found: {ValidationResults}", validationResults.FullName);
                return 2; // Fail
            }

            // Set default output path
            var outputPath = output?.FullName ?? "quality-report.json";

            Log.Information("Configuration:");
            Log.Information("  TRX File: {TrxFile}", trxFile?.FullName ?? "(not provided)");
            Log.Information("  Log Directory: {LogDir}", logDir?.FullName ?? "(not provided)");
            Log.Information("  Visual Results: {VisualResults}", visualResults?.FullName ?? "(not provided)");
            Log.Information("  Validation Results: {ValidationResults}", validationResults?.FullName ?? "(not provided)");
            Log.Information("  Output: {Output}", outputPath);

            // Configure OpenAI from environment variable
            var openAiConfig = new OpenAiConfig
            {
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty,
                Model = "gpt-4o",
                MaxRetries = 3
            };

            if (string.IsNullOrEmpty(openAiConfig.ApiKey))
            {
                Log.Warning("OPENAI_API_KEY environment variable not set - AI analysis will use fallback rule-based analysis");
            }

            // Get schema path
            var schemaPath = FindSchemaPath();
            if (string.IsNullOrEmpty(schemaPath))
            {
                Log.Error("Quality report JSON schema not found. Expected at schemas/quality-report.schema.json");
                return 2; // Fail
            }

            // Get build info from environment
            var buildInfo = new BuildInfo
            {
                BuildId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "unknown",
                Commit = Environment.GetEnvironmentVariable("GITHUB_SHA"),
                Author = Environment.GetEnvironmentVariable("GITHUB_ACTOR")
            };

            // Prepare analysis input
            var analysisInput = new AnalysisInput
            {
                TrxFilePath = trxFile?.FullName,
                LogFilePath = logDir != null ? FindFirstLogFile(logDir) : null,
                SsimResultsPath = visualResults?.FullName,
                ValidationResultsPath = validationResults?.FullName,
                BuildInfo = buildInfo,
                SchemaPath = schemaPath,
                OutputPath = outputPath
            };

            // Execute quality analysis
            var qualityAgent = new AiQualityAgent(openAiConfig);
            var result = await qualityAgent.AnalyzeAsync(analysisInput);

            if (result.IsFailed)
            {
                Log.Error("Quality analysis failed: {Error}", result.Errors.First().Message);
                return 2; // Fail
            }

            var report = result.Value;

            // Display summary
            Log.Information("========================================");
            Log.Information("Quality Analysis Summary");
            Log.Information("========================================");
            Log.Information("Overall Score: {Score}/100", report.OverallScore);
            Log.Information("Status: {Status}", report.Status);
            Log.Information("Total Issues: {TotalIssues}", report.Summary.TotalIssues);
            Log.Information("Critical Issues: {CriticalIssues}", report.Summary.CriticalIssues);
            Log.Information("========================================");

            // Display key findings
            if (report.Analysis.TestAnalysis.FailedTests > 0)
            {
                Log.Warning("Test Failures: {FailedTests} of {TotalTests} tests failed",
                    report.Analysis.TestAnalysis.FailedTests,
                    report.Analysis.TestAnalysis.TotalTests);
            }

            if (report.Analysis.LogAnalysis.ErrorCount > 0)
            {
                Log.Warning("Log Errors: {ErrorCount} errors detected", report.Analysis.LogAnalysis.ErrorCount);
            }

            if (report.Analysis.VisualAnalysis.CriticalRegressions > 0)
            {
                Log.Warning("Visual Regressions: {CriticalRegressions} critical regressions",
                    report.Analysis.VisualAnalysis.CriticalRegressions);
            }

            // Display top recommendations
            if (report.Recommendations.Any())
            {
                Log.Information("Top Recommendations:");
                foreach (var rec in report.Recommendations.Take(3))
                {
                    Log.Information("  - {Recommendation}", rec.Description);
                }
            }

            Log.Information("Quality report written to: {OutputPath}", outputPath);

            // Return exit code based on status
            return report.Status switch
            {
                QualityStatus.Pass => 0,
                QualityStatus.Warn => 1,
                QualityStatus.Fail => 2,
                _ => 2
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Quality analysis failed");
            return 2; // Fail
        }
    }

    private static string? FindSchemaPath()
    {
        // Try multiple common locations
        var possiblePaths = new[]
        {
            "schemas/quality-report.schema.json",
            "../../../schemas/quality-report.schema.json", // When running from bin/Debug/net8.0
            "../../../../../../schemas/quality-report.schema.json" // When running from tools/quality-agent/bin/Debug/net8.0
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }

    private static string? FindFirstLogFile(DirectoryInfo logDir)
    {
        var logFiles = logDir.GetFiles("*.json", SearchOption.AllDirectories);
        return logFiles.FirstOrDefault()?.FullName;
    }
}
