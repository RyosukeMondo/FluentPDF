using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies PDF merge functionality.
/// Tests merging multiple PDF documents and validates page count aggregation.
/// </summary>
public sealed class MergeCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "merge";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Merges 2-3 PDFs and verifies output page count equals sum of input page counts";

    /// <summary>
    /// Executes the merge test using the provided test context.
    /// Merges multiple PDFs and validates the result.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing merge metrics and output file path</returns>
    public async Task<CliTestResult> RunAsync(CliTestContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var result = new CliTestResult
        {
            TestName = Name,
            Success = false
        };

        var startTime = DateTime.UtcNow;

        try
        {
            context.Logger.Information("Starting PDF merge test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var editingService = context.Services.GetRequiredService<IDocumentEditingService>();

            // Get test PDF paths from context data or use defaults
            var inputPaths = context.Data.TryGetValue("InputPaths", out var pathsObj) && pathsObj is string[] paths
                ? paths
                : new[]
                {
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures", "sample.pdf")),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures", "multi-page.pdf")),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures", "simple-text.pdf"))
                };

            context.Logger.Information("Merging {Count} PDF files", inputPaths.Length);

            // Validate input files exist
            var missingFiles = inputPaths.Where(p => !File.Exists(p)).ToList();
            if (missingFiles.Any())
            {
                result.ErrorMessage = $"Input PDFs not found: {string.Join(", ", missingFiles)}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Count total expected pages
            var inputPageCounts = new List<int>();
            foreach (var inputPath in inputPaths)
            {
                var loadResult = await documentService.LoadDocumentAsync(inputPath);
                if (loadResult.IsFailed)
                {
                    result.ErrorMessage = $"Failed to load {Path.GetFileName(inputPath)}: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                    result.Duration = DateTime.UtcNow - startTime;
                    return result;
                }

                using var doc = loadResult.Value;
                inputPageCounts.Add(doc.PageCount);
                context.Logger.Information("Input {Index}: {FileName} ({Pages} pages)",
                    inputPageCounts.Count, Path.GetFileName(inputPath), doc.PageCount);
            }

            var expectedPageCount = inputPageCounts.Sum();
            context.Logger.Information("Expected merged page count: {Expected}", expectedPageCount);

            // Perform merge operation
            var outputPath = Path.Combine(context.WorkingDirectory, "merged_output.pdf");
            var operationStart = DateTime.UtcNow;

            var progress = new Progress<double>(p =>
            {
                if (context.Data.TryGetValue("VerboseProgress", out var verboseObj) && verboseObj is bool verbose && verbose)
                {
                    context.Logger.Debug("Merge progress: {Progress:F1}%", p);
                }
            });

            var mergeResult = await editingService.MergeAsync(inputPaths, outputPath, progress, CancellationToken.None);

            if (mergeResult.IsFailed)
            {
                result.ErrorMessage = $"Merge failed: {string.Join(", ", mergeResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var operationTime = DateTime.UtcNow - operationStart;

            // Verify output file
            if (!File.Exists(outputPath))
            {
                result.ErrorMessage = $"Output file not created: {outputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Verify merged page count
            var verifyResult = await documentService.LoadDocumentAsync(outputPath);
            if (verifyResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load merged PDF: {string.Join(", ", verifyResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var mergedDoc = verifyResult.Value;
            var actualPageCount = mergedDoc.PageCount;

            context.Logger.Information("Actual merged page count: {Actual}", actualPageCount);

            // Validate page count
            if (actualPageCount != expectedPageCount)
            {
                result.ErrorMessage = $"Page count mismatch: expected {expectedPageCount}, got {actualPageCount}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "merge_report.txt");
            var report = new StringBuilder();
            report.AppendLine("PDF Merge Test Report");
            report.AppendLine($"Input Files: {inputPaths.Length}");
            for (int i = 0; i < inputPaths.Length; i++)
            {
                report.AppendLine($"  {i + 1}. {Path.GetFileName(inputPaths[i])} ({inputPageCounts[i]} pages)");
            }
            report.AppendLine($"Expected Page Count: {expectedPageCount}");
            report.AppendLine($"Actual Page Count: {actualPageCount}");
            report.AppendLine($"Page Count Match: {actualPageCount == expectedPageCount}");
            report.AppendLine($"Operation Time: {operationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");
            report.AppendLine($"Output Size: {new FileInfo(outputPath).Length:N0} bytes");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved merge report to: {ReportPath}", reportPath);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["InputCount"] = inputPaths.Length;
            result.Outputs["ExpectedPageCount"] = expectedPageCount;
            result.Outputs["ActualPageCount"] = actualPageCount;
            result.Outputs["PageCountMatch"] = actualPageCount == expectedPageCount;
            result.Outputs["OperationTimeMs"] = operationTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Merge test completed successfully. Merged {Count} files ({Expected} pages), {OperationTimeMs:F0} ms",
                inputPaths.Length,
                expectedPageCount,
                operationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Merge test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the merge test produced expected outputs.
    /// </summary>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Check basic success
        if (!result.Success)
        {
            return Task.FromResult(false);
        }

        // Verify output file exists
        if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
            outputFileObj is not string outputFile ||
            !File.Exists(outputFile))
        {
            return Task.FromResult(false);
        }

        // Verify page count match
        if (!result.Outputs.TryGetValue("PageCountMatch", out var matchObj) ||
            matchObj is not bool match ||
            !match)
        {
            return Task.FromResult(false);
        }

        // Verify output file has content
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// CLI test that verifies PDF split functionality.
/// Tests splitting a PDF by page ranges and validates output files.
/// </summary>
public sealed class SplitCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "split";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Splits PDF by page ranges and verifies output files exist with correct page counts";

    /// <summary>
    /// Executes the split test using the provided test context.
    /// Splits a PDF and validates the result.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing split metrics and output file path</returns>
    public async Task<CliTestResult> RunAsync(CliTestContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var result = new CliTestResult
        {
            TestName = Name,
            Success = false
        };

        var startTime = DateTime.UtcNow;

        try
        {
            context.Logger.Information("Starting PDF split test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var editingService = context.Services.GetRequiredService<IDocumentEditingService>();

            // Get test PDF path from context data or use default
            var inputPath = context.Data.TryGetValue("InputPath", out var pathObj) && pathObj is string path
                ? path
                : Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                ));

            // Get page ranges from context or use default
            var pageRanges = context.Data.TryGetValue("PageRanges", out var rangesObj) && rangesObj is string ranges
                ? ranges
                : "1-3,5"; // Default: extract pages 1-3 and 5

            context.Logger.Information("Splitting PDF: {FileName}", Path.GetFileName(inputPath));
            context.Logger.Information("Page ranges: {Ranges}", pageRanges);

            if (!File.Exists(inputPath))
            {
                result.ErrorMessage = $"Input PDF not found: {inputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load original document to get page count
            var loadResult = await documentService.LoadDocumentAsync(inputPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            int originalPageCount;
            using (var doc = loadResult.Value)
            {
                originalPageCount = doc.PageCount;
                context.Logger.Information("Original page count: {Pages}", originalPageCount);
            }

            // Perform split operation
            var outputPath = Path.Combine(context.WorkingDirectory, "split_output.pdf");
            var operationStart = DateTime.UtcNow;

            var progress = new Progress<double>(p =>
            {
                if (context.Data.TryGetValue("VerboseProgress", out var verboseObj) && verboseObj is bool verbose && verbose)
                {
                    context.Logger.Debug("Split progress: {Progress:F1}%", p);
                }
            });

            var splitResult = await editingService.SplitAsync(inputPath, pageRanges, outputPath, progress, CancellationToken.None);

            if (splitResult.IsFailed)
            {
                result.ErrorMessage = $"Split failed: {string.Join(", ", splitResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var operationTime = DateTime.UtcNow - operationStart;

            // Verify output file
            if (!File.Exists(outputPath))
            {
                result.ErrorMessage = $"Output file not created: {outputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Verify split page count
            var verifyResult = await documentService.LoadDocumentAsync(outputPath);
            if (verifyResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load split PDF: {string.Join(", ", verifyResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            int actualPageCount;
            using (var splitDoc = verifyResult.Value)
            {
                actualPageCount = splitDoc.PageCount;
                context.Logger.Information("Split page count: {Pages}", actualPageCount);
            }

            // Calculate expected page count from ranges
            var expectedPageCount = ParsePageRanges(pageRanges, originalPageCount);

            // Validate page count
            if (actualPageCount != expectedPageCount)
            {
                result.ErrorMessage = $"Page count mismatch: expected {expectedPageCount}, got {actualPageCount}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "split_report.txt");
            var report = new StringBuilder();
            report.AppendLine("PDF Split Test Report");
            report.AppendLine($"Input File: {Path.GetFileName(inputPath)}");
            report.AppendLine($"Original Page Count: {originalPageCount}");
            report.AppendLine($"Page Ranges: {pageRanges}");
            report.AppendLine($"Expected Page Count: {expectedPageCount}");
            report.AppendLine($"Actual Page Count: {actualPageCount}");
            report.AppendLine($"Page Count Match: {actualPageCount == expectedPageCount}");
            report.AppendLine($"Operation Time: {operationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");
            report.AppendLine($"Output Size: {new FileInfo(outputPath).Length:N0} bytes");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved split report to: {ReportPath}", reportPath);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["OriginalPageCount"] = originalPageCount;
            result.Outputs["ExpectedPageCount"] = expectedPageCount;
            result.Outputs["ActualPageCount"] = actualPageCount;
            result.Outputs["PageCountMatch"] = actualPageCount == expectedPageCount;
            result.Outputs["PageRanges"] = pageRanges;
            result.Outputs["OperationTimeMs"] = operationTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Split test completed successfully. Split {Original} pages to {Actual} pages, {OperationTimeMs:F0} ms",
                originalPageCount,
                actualPageCount,
                operationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Split test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the split test produced expected outputs.
    /// </summary>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Check basic success
        if (!result.Success)
        {
            return Task.FromResult(false);
        }

        // Verify output file exists
        if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
            outputFileObj is not string outputFile ||
            !File.Exists(outputFile))
        {
            return Task.FromResult(false);
        }

        // Verify page count match
        if (!result.Outputs.TryGetValue("PageCountMatch", out var matchObj) ||
            matchObj is not bool match ||
            !match)
        {
            return Task.FromResult(false);
        }

        // Verify output file has content
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Parses page ranges string and calculates expected page count.
    /// </summary>
    private static int ParsePageRanges(string ranges, int maxPages)
    {
        var parts = ranges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pageSet = new HashSet<int>();

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var rangeParts = part.Split('-');
                if (rangeParts.Length == 2 &&
                    int.TryParse(rangeParts[0], out var start) &&
                    int.TryParse(rangeParts[1], out var end))
                {
                    for (int i = start; i <= end && i <= maxPages; i++)
                    {
                        pageSet.Add(i);
                    }
                }
            }
            else if (int.TryParse(part, out var page) && page <= maxPages)
            {
                pageSet.Add(page);
            }
        }

        return pageSet.Count;
    }
}
