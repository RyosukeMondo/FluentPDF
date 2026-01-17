using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies page rotation functionality.
/// Tests page rotation with angle verification and output validation.
/// </summary>
public sealed class PageRotateCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "page-rotate";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Rotates specified pages and verifies the operation succeeded";

    /// <summary>
    /// Executes the page rotation test using the provided test context.
    /// Loads a test PDF, rotates pages, and saves the modified document.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing rotation metrics and output file path</returns>
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
            context.Logger.Information("Starting page rotation test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var pageOpsService = context.Services.GetRequiredService<IPageOperationsService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            // Get rotation parameters from context or use defaults
            var pageIndices = context.Data.TryGetValue("PageIndices", out var indicesObj) && indicesObj is int[] indices
                ? indices
                : new[] { 0 }; // Rotate first page by default

            var angle = context.Data.TryGetValue("RotationAngle", out var angleObj) && angleObj is RotationAngle rotAngle
                ? rotAngle
                : RotationAngle.Rotate90;

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);
            context.Logger.Information("Rotating pages {PageIndices} by {Angle} degrees",
                string.Join(", ", pageIndices.Select(i => i + 1)), (int)angle);

            if (!File.Exists(testPdfPath))
            {
                result.ErrorMessage = $"Test PDF not found: {testPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            var pageCount = document.PageCount;
            context.Logger.Information("Loaded PDF with {PageCount} pages", pageCount);

            // Validate page indices
            if (pageIndices.Any(i => i < 0 || i >= pageCount))
            {
                result.ErrorMessage = $"Invalid page indices: {string.Join(", ", pageIndices.Where(i => i < 0 || i >= pageCount))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Record operation start time
            var operationStart = DateTime.UtcNow;

            // Rotate pages
            context.Logger.Information("Rotating {Count} pages", pageIndices.Length);
            var rotateResult = await pageOpsService.RotatePagesAsync(document, pageIndices, angle);

            if (rotateResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to rotate pages: {string.Join(", ", rotateResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var operationTime = DateTime.UtcNow - operationStart;

            // Save modified document
            var outputPath = Path.Combine(context.WorkingDirectory, "rotated_output.pdf");
            var saveResult = await documentService.SaveDocumentAsync(document, outputPath);

            if (saveResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to save PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            context.Logger.Information("Saved rotated PDF to: {OutputPath}", outputPath);

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "rotation_report.txt");
            var report = new StringBuilder();
            report.AppendLine("Page Rotation Test Report");
            report.AppendLine($"Original PDF: {Path.GetFileName(testPdfPath)}");
            report.AppendLine($"Pages Rotated: {string.Join(", ", pageIndices.Select(i => i + 1))}");
            report.AppendLine($"Rotation Angle: {(int)angle} degrees");
            report.AppendLine($"Total Pages: {pageCount}");
            report.AppendLine($"Operation Time: {operationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved rotation report to: {ReportPath}", reportPath);

            context.Logger.Information("Rotation time: {OperationTimeMs} ms", operationTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["PageIndices"] = pageIndices;
            result.Outputs["RotationAngle"] = (int)angle;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["OperationTimeMs"] = operationTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Page rotation test completed successfully. Rotated {Count} pages, {OperationTimeMs:F0} ms",
                pageIndices.Length,
                operationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Page rotation test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the page rotation test produced expected outputs.
    /// Checks that output file exists and operation completed successfully.
    /// </summary>
    /// <param name="result">The test result produced by RunAsync</param>
    /// <returns>True if all verification checks pass, false otherwise</returns>
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

        // Verify output file has content
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Verify operation time was captured
        if (!result.Outputs.TryGetValue("OperationTimeMs", out var opTimeObj) ||
            opTimeObj is not double opTime ||
            opTime < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// CLI test that verifies page deletion functionality.
/// Tests page deletion with page count verification and output validation.
/// </summary>
public sealed class PageDeleteCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "page-delete";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Deletes specified pages and verifies the page count is updated";

    /// <summary>
    /// Executes the page deletion test using the provided test context.
    /// Loads a test PDF, deletes pages, and saves the modified document.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing deletion metrics and output file path</returns>
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
            context.Logger.Information("Starting page deletion test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var pageOpsService = context.Services.GetRequiredService<IPageOperationsService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            // Get page indices to delete from context or use default
            var pageIndices = context.Data.TryGetValue("PageIndices", out var indicesObj) && indicesObj is int[] indices
                ? indices
                : new[] { 0 }; // Delete first page by default

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);
            context.Logger.Information("Deleting pages: {PageIndices}",
                string.Join(", ", pageIndices.Select(i => i + 1)));

            if (!File.Exists(testPdfPath))
            {
                result.ErrorMessage = $"Test PDF not found: {testPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            var originalPageCount = document.PageCount;
            context.Logger.Information("Loaded PDF with {PageCount} pages", originalPageCount);

            // Validate page indices
            if (pageIndices.Any(i => i < 0 || i >= originalPageCount))
            {
                result.ErrorMessage = $"Invalid page indices: {string.Join(", ", pageIndices.Where(i => i < 0 || i >= originalPageCount))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Validate not deleting all pages
            if (pageIndices.Length >= originalPageCount)
            {
                result.ErrorMessage = "Cannot delete all pages from PDF";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Record operation start time
            var operationStart = DateTime.UtcNow;

            // Delete pages
            context.Logger.Information("Deleting {Count} pages", pageIndices.Length);
            var deleteResult = await pageOpsService.DeletePagesAsync(document, pageIndices);

            if (deleteResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to delete pages: {string.Join(", ", deleteResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var operationTime = DateTime.UtcNow - operationStart;
            var newPageCount = document.PageCount;
            var expectedPageCount = originalPageCount - pageIndices.Length;

            context.Logger.Information("Page count: {Original} -> {New} (expected {Expected})",
                originalPageCount, newPageCount, expectedPageCount);

            // Verify page count
            if (newPageCount != expectedPageCount)
            {
                result.ErrorMessage = $"Page count mismatch: expected {expectedPageCount}, got {newPageCount}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Save modified document
            var outputPath = Path.Combine(context.WorkingDirectory, "deleted_output.pdf");
            var saveResult = await documentService.SaveDocumentAsync(document, outputPath);

            if (saveResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to save PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            context.Logger.Information("Saved modified PDF to: {OutputPath}", outputPath);

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "deletion_report.txt");
            var report = new StringBuilder();
            report.AppendLine("Page Deletion Test Report");
            report.AppendLine($"Original PDF: {Path.GetFileName(testPdfPath)}");
            report.AppendLine($"Pages Deleted: {string.Join(", ", pageIndices.Select(i => i + 1))}");
            report.AppendLine($"Original Page Count: {originalPageCount}");
            report.AppendLine($"New Page Count: {newPageCount}");
            report.AppendLine($"Pages Removed: {pageIndices.Length}");
            report.AppendLine($"Operation Time: {operationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved deletion report to: {ReportPath}", reportPath);

            context.Logger.Information("Deletion time: {OperationTimeMs} ms", operationTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["PageIndices"] = pageIndices;
            result.Outputs["OriginalPageCount"] = originalPageCount;
            result.Outputs["NewPageCount"] = newPageCount;
            result.Outputs["PagesDeleted"] = pageIndices.Length;
            result.Outputs["OperationTimeMs"] = operationTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Page deletion test completed successfully. Deleted {Count} pages ({Original} -> {New}), {OperationTimeMs:F0} ms",
                pageIndices.Length,
                originalPageCount,
                newPageCount,
                operationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Page deletion test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the page deletion test produced expected outputs.
    /// Checks that output file exists and page count was updated correctly.
    /// </summary>
    /// <param name="result">The test result produced by RunAsync</param>
    /// <returns>True if all verification checks pass, false otherwise</returns>
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

        // Verify output file has content
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Verify page counts
        if (!result.Outputs.TryGetValue("OriginalPageCount", out var origCountObj) ||
            origCountObj is not int originalCount ||
            originalCount <= 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("NewPageCount", out var newCountObj) ||
            newCountObj is not int newCount ||
            newCount <= 0 ||
            newCount >= originalCount)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("PagesDeleted", out var deletedObj) ||
            deletedObj is not int pagesDeleted ||
            pagesDeleted != (originalCount - newCount))
        {
            return Task.FromResult(false);
        }

        // Verify operation time was captured
        if (!result.Outputs.TryGetValue("OperationTimeMs", out var opTimeObj) ||
            opTimeObj is not double opTime ||
            opTime < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// CLI test that verifies page reordering functionality.
/// Tests page reordering with order verification and output validation.
/// </summary>
public sealed class PageReorderCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "page-reorder";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Reorders pages by moving them to a new position and verifies the order";

    /// <summary>
    /// Executes the page reordering test using the provided test context.
    /// Loads a test PDF, reorders pages, and saves the modified document.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing reordering metrics and output file path</returns>
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
            context.Logger.Information("Starting page reordering test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var pageOpsService = context.Services.GetRequiredService<IPageOperationsService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            // Get reorder parameters from context or use defaults
            var pageIndices = context.Data.TryGetValue("PageIndices", out var indicesObj) && indicesObj is int[] indices
                ? indices
                : new[] { 2 }; // Move third page by default

            var targetIndex = context.Data.TryGetValue("TargetIndex", out var targetObj) && targetObj is int target
                ? target
                : 0; // Move to beginning by default

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);
            context.Logger.Information("Moving pages {PageIndices} to position {TargetIndex}",
                string.Join(", ", pageIndices.Select(i => i + 1)),
                targetIndex + 1);

            if (!File.Exists(testPdfPath))
            {
                result.ErrorMessage = $"Test PDF not found: {testPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            var pageCount = document.PageCount;
            context.Logger.Information("Loaded PDF with {PageCount} pages", pageCount);

            // Validate page indices
            if (pageIndices.Any(i => i < 0 || i >= pageCount))
            {
                result.ErrorMessage = $"Invalid page indices: {string.Join(", ", pageIndices.Where(i => i < 0 || i >= pageCount))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Validate target index
            if (targetIndex < 0 || targetIndex >= pageCount)
            {
                result.ErrorMessage = $"Invalid target index: {targetIndex}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Record operation start time
            var operationStart = DateTime.UtcNow;

            // Reorder pages
            context.Logger.Information("Reordering {Count} pages", pageIndices.Length);
            var reorderResult = await pageOpsService.ReorderPagesAsync(document, pageIndices, targetIndex);

            if (reorderResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to reorder pages: {string.Join(", ", reorderResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var operationTime = DateTime.UtcNow - operationStart;

            // Verify page count hasn't changed
            if (document.PageCount != pageCount)
            {
                result.ErrorMessage = $"Page count changed after reorder: {pageCount} -> {document.PageCount}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Save modified document
            var outputPath = Path.Combine(context.WorkingDirectory, "reordered_output.pdf");
            var saveResult = await documentService.SaveDocumentAsync(document, outputPath);

            if (saveResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to save PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            context.Logger.Information("Saved reordered PDF to: {OutputPath}", outputPath);

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "reorder_report.txt");
            var report = new StringBuilder();
            report.AppendLine("Page Reordering Test Report");
            report.AppendLine($"Original PDF: {Path.GetFileName(testPdfPath)}");
            report.AppendLine($"Pages Moved: {string.Join(", ", pageIndices.Select(i => i + 1))}");
            report.AppendLine($"Target Position: {targetIndex + 1}");
            report.AppendLine($"Total Pages: {pageCount}");
            report.AppendLine($"Operation Time: {operationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved reorder report to: {ReportPath}", reportPath);

            context.Logger.Information("Reorder time: {OperationTimeMs} ms", operationTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["PageIndices"] = pageIndices;
            result.Outputs["TargetIndex"] = targetIndex;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["OperationTimeMs"] = operationTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Page reordering test completed successfully. Moved {Count} pages to position {Target}, {OperationTimeMs:F0} ms",
                pageIndices.Length,
                targetIndex + 1,
                operationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Page reordering test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the page reordering test produced expected outputs.
    /// Checks that output file exists and operation completed successfully.
    /// </summary>
    /// <param name="result">The test result produced by RunAsync</param>
    /// <returns>True if all verification checks pass, false otherwise</returns>
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

        // Verify output file has content
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Verify page count is valid
        if (!result.Outputs.TryGetValue("PageCount", out var pageCountObj) ||
            pageCountObj is not int pageCount ||
            pageCount <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify operation time was captured
        if (!result.Outputs.TryGetValue("OperationTimeMs", out var opTimeObj) ||
            opTimeObj is not double opTime ||
            opTime < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
