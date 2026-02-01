// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.CommandLine;
using System.Diagnostics;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using Serilog;

namespace FluentPDF.Cli.Commands;

/// <summary>
/// Command to verify P/Invoke marshalling correctness.
/// </summary>
internal static class VerifyCommand
{
    public static Command Create()
    {
        var command = new Command("verify", "Verify P/Invoke marshalling correctness");

        var marshallingCommand = new Command("marshalling", "Verify PDFium P/Invoke signatures and marshalling");
        var outputOption = new Option<string?>("--output", "Output file path for JSON report");
        var testPdfOption = new Option<string?>("--test-pdf", "PDF file to use for marshalling tests");

        marshallingCommand.AddOption(outputOption);
        marshallingCommand.AddOption(testPdfOption);
        marshallingCommand.SetHandler(ExecuteMarshalling, outputOption, testPdfOption);

        command.AddCommand(marshallingCommand);

        return command;
    }

    private static async Task<int> ExecuteMarshalling(string? outputPath, string? testPdfPath)
    {
        try
        {
            Console.WriteLine("FluentPDF P/Invoke Marshalling Verification");
            Console.WriteLine("============================================");
            Console.WriteLine();

            // Find test PDF if not provided
            if (testPdfPath == null)
            {
                var fixturesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures");
                if (Directory.Exists(fixturesDir))
                {
                    var pdfFiles = Directory.GetFiles(fixturesDir, "*.pdf");
                    if (pdfFiles.Length > 0)
                    {
                        testPdfPath = pdfFiles[0];
                        Console.WriteLine($"Using test PDF: {Path.GetFileName(testPdfPath)}");
                    }
                }
            }

            if (testPdfPath != null && File.Exists(testPdfPath))
            {
                Console.WriteLine($"Test PDF: {testPdfPath}");
            }
            else
            {
                Console.WriteLine("No test PDF provided - signature verification only");
            }
            Console.WriteLine();

            // Create verifier
            Console.WriteLine("Creating marshalling verifier...");
            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            Console.WriteLine();

            // Load PDFium API specifications
            Console.WriteLine("Loading PDFium API specifications...");
            var expectedSignatures = PdfiumApiSpec.GetAllSpecs()
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new SignatureDetails
                    {
                        ReturnType = kvp.Value.ReturnType.Name,
                        CallingConvention = kvp.Value.CallingConvention.ToString(),
                        EntryPoint = kvp.Value.FunctionName,
                        CharSet = kvp.Value.CharSet?.ToString(),
                        Parameters = kvp.Value.Parameters.Select(p => new ParameterDetails
                        {
                            Name = p.Name,
                            Type = p.Type.Name,
                            IsOut = false,
                            IsRef = p.IsByRef,
                            MarshalAs = p.MarshalAs?.ToString()
                        }).ToList()
                    });
            Console.WriteLine($"Loaded {expectedSignatures.Count} API specifications");
            Console.WriteLine();

            // Run verification
            Console.WriteLine("Running verification...");
            var stopwatch = Stopwatch.StartNew();
            var report = await verifier.VerifyAndReportAsync(expectedSignatures, testPdfPath);
            stopwatch.Stop();
            Console.WriteLine($"Verification completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();

            // Display summary
            Console.WriteLine("SUMMARY");
            Console.WriteLine("=======");
            Console.WriteLine($"Total Functions:    {report.TotalFunctions}");
            Console.WriteLine($"Verified Functions: {report.VerifiedFunctions}");
            Console.WriteLine($"Tested Functions:   {report.TestedFunctions}");
            Console.WriteLine($"Passed Functions:   {report.PassedFunctions}");
            Console.WriteLine($"Failed Functions:   {report.FailedFunctions}");
            Console.WriteLine($"Untested Functions: {report.UntestedFunctions.Count}");
            Console.WriteLine();

            // Display failed functions
            if (report.FailedFunctionNames.Any())
            {
                Console.WriteLine("FAILED FUNCTIONS:");
                foreach (var funcName in report.FailedFunctionNames)
                {
                    Console.WriteLine($"  - {funcName}");
                }
                Console.WriteLine();
            }

            // Display untested functions
            if (report.UntestedFunctions.Any())
            {
                Console.WriteLine($"UNTESTED FUNCTIONS ({report.UntestedFunctions.Count}):");
                foreach (var funcName in report.UntestedFunctions.Take(10))
                {
                    Console.WriteLine($"  - {funcName}");
                }
                if (report.UntestedFunctions.Count > 10)
                {
                    Console.WriteLine($"  ... and {report.UntestedFunctions.Count - 10} more");
                }
                Console.WriteLine();
            }

            // Save JSON report if requested
            if (outputPath != null)
            {
                var jsonOutput = new
                {
                    timestamp = report.GeneratedAt,
                    summary = new
                    {
                        totalFunctions = report.TotalFunctions,
                        verifiedFunctions = report.VerifiedFunctions,
                        testedFunctions = report.TestedFunctions,
                        passedFunctions = report.PassedFunctions,
                        failedFunctions = report.FailedFunctions,
                        untestedCount = report.UntestedFunctions.Count
                    },
                    failedFunctionNames = report.FailedFunctionNames,
                    untestedFunctions = report.UntestedFunctions
                };

                var json = System.Text.Json.JsonSerializer.Serialize(jsonOutput, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(outputPath, json);
                Console.WriteLine($"JSON report saved to: {outputPath}");
                Console.WriteLine();
            }

            // Return exit code
            if (report.FailedFunctions > 0)
            {
                Console.WriteLine($"RESULT: FAILED ({report.FailedFunctions} function(s) failed verification)");
                Log.Warning("Marshalling verification failed with {FailedCount} failures", report.FailedFunctions);
                return 1;
            }

            Console.WriteLine("RESULT: SUCCESS (All verified functions passed)");
            Log.Information("Marshalling verification completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Marshalling verification failed");
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
