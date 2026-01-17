using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies batch rendering of all pages in a PDF document.
/// Tests performance and stability by rendering every page to separate image files.
/// </summary>
public sealed class BatchRenderCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "batch-render";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Renders all pages in a PDF to separate image files and reports performance metrics";

    /// <summary>
    /// Executes the batch rendering test using the provided test context.
    /// Loads a test PDF, renders all pages sequentially, and captures performance metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing render count, timing, success rate, and metrics</returns>
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
            context.Logger.Information("Starting batch render test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var renderingService = context.Services.GetRequiredService<IPdfRenderingService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            // Get DPI from context data or use default
            var dpi = context.Data.TryGetValue("DPI", out var dpiObj) && dpiObj is int dpiValue
                ? dpiValue
                : 96;

            // Get zoom level from context data or use default
            var zoomLevel = context.Data.TryGetValue("ZoomLevel", out var zoomObj) && zoomObj is double zoom
                ? zoom
                : 1.0;

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);

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
            context.Logger.Information("Render settings: DPI={DPI}, ZoomLevel={ZoomLevel}", dpi, zoomLevel);

            // Track rendering metrics
            var renderStart = DateTime.UtcNow;
            var successCount = 0;
            var failCount = 0;
            var totalBytes = 0L;
            var renderTimes = new List<double>();
            var failedPages = new List<int>();
            var renderedPaths = new List<string>();

            // Render all pages sequentially
            context.Logger.Information("Starting batch render of all {PageCount} pages", pageCount);

            for (int pageNum = 1; pageNum <= pageCount; pageNum++)
            {
                // Report progress (requirement 5.2)
                context.Logger.Information("Rendering page {PageNumber} of {PageCount}", pageNum, pageCount);

                var pageRenderStart = DateTime.UtcNow;
                var renderResult = await renderingService.RenderPageAsync(document, pageNum, zoomLevel, dpi);
                var pageRenderTime = DateTime.UtcNow - pageRenderStart;
                renderTimes.Add(pageRenderTime.TotalMilliseconds);

                if (renderResult.IsFailed)
                {
                    failCount++;
                    failedPages.Add(pageNum);
                    context.Logger.Warning(
                        "Failed to render page {PageNumber}: {Error}",
                        pageNum,
                        string.Join(", ", renderResult.Errors.Select(e => e.Message))
                    );
                    // Continue with remaining pages (requirement 5.4)
                    continue;
                }

                // Save the rendered page to a PNG file
                var outputPath = Path.Combine(context.WorkingDirectory, $"page_{pageNum:D4}.png");
                using (var imageStream = renderResult.Value)
                using (var fileStream = File.Create(outputPath))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                // Get file size
                var fileInfo = new FileInfo(outputPath);
                var fileSize = fileInfo.Length;
                totalBytes += fileSize;

                renderedPaths.Add(outputPath);
                successCount++;

                context.Logger.Information(
                    "Page {PageNumber}: {FileSize} bytes, {RenderTimeMs:F1} ms",
                    pageNum,
                    fileSize,
                    pageRenderTime.TotalMilliseconds
                );
            }

            var totalRenderTime = DateTime.UtcNow - renderStart;

            // Calculate metrics (requirement 5.3)
            var avgTimeMs = renderTimes.Any() ? renderTimes.Average() : 0;
            var minTimeMs = renderTimes.Any() ? renderTimes.Min() : 0;
            var maxTimeMs = renderTimes.Any() ? renderTimes.Max() : 0;
            var successRate = pageCount > 0 ? (double)successCount / pageCount * 100 : 0;

            context.Logger.Information(
                "Batch rendering complete: {SuccessCount}/{PageCount} successful, {FailCount} failed",
                successCount,
                pageCount,
                failCount
            );
            context.Logger.Information("Success rate: {SuccessRate:F1}%", successRate);
            context.Logger.Information("Total size: {TotalBytes} bytes ({TotalMB:F2} MB)", totalBytes, totalBytes / (1024.0 * 1024.0));
            context.Logger.Information("Total time: {TotalTimeMs:F0} ms ({TotalSeconds:F1} seconds)", totalRenderTime.TotalMilliseconds, totalRenderTime.TotalSeconds);
            context.Logger.Information("Average: {AvgTimeMs:F1} ms per page", avgTimeMs);
            context.Logger.Information("Min: {MinTimeMs:F1} ms, Max: {MaxTimeMs:F1} ms", minTimeMs, maxTimeMs);

            if (failedPages.Any())
            {
                context.Logger.Warning("Failed pages: {FailedPages}", string.Join(", ", failedPages));
            }

            // Store outputs for verification
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["SuccessCount"] = successCount;
            result.Outputs["FailCount"] = failCount;
            result.Outputs["FailedPages"] = failedPages;
            result.Outputs["SuccessRate"] = successRate;
            result.Outputs["TotalBytes"] = totalBytes;
            result.Outputs["TotalMB"] = totalBytes / (1024.0 * 1024.0);
            result.Outputs["TotalTimeMs"] = totalRenderTime.TotalMilliseconds;
            result.Outputs["TotalSeconds"] = totalRenderTime.TotalSeconds;
            result.Outputs["AverageTimeMs"] = avgTimeMs;
            result.Outputs["MinTimeMs"] = minTimeMs;
            result.Outputs["MaxTimeMs"] = maxTimeMs;
            result.Outputs["RenderedPaths"] = renderedPaths;
            result.Outputs["ExitCode"] = failCount > 0 ? 1 : 0;

            // Test is successful if at least some pages were rendered
            result.Success = successCount > 0;
            result.Duration = DateTime.UtcNow - startTime;

            if (failCount > 0)
            {
                result.ErrorMessage = $"{failCount} page(s) failed to render (out of {pageCount} total)";
            }

            context.Logger.Information(
                "Batch render test completed. {SuccessCount}/{PageCount} rendered ({SuccessRate:F1}%), {TotalSeconds:F1}s total, {AvgTimeMs:F1}ms avg",
                successCount,
                pageCount,
                successRate,
                totalRenderTime.TotalSeconds,
                avgTimeMs
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Batch render test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the batch rendering test produced expected outputs.
    /// Checks that pages were rendered, success rate is acceptable, and metrics are valid.
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

        // Verify page count
        if (!result.Outputs.TryGetValue("PageCount", out var pageCountObj) ||
            pageCountObj is not int pageCount ||
            pageCount <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify success count
        if (!result.Outputs.TryGetValue("SuccessCount", out var successCountObj) ||
            successCountObj is not int successCount ||
            successCount <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify success count matches page count (all pages should render)
        if (successCount != pageCount)
        {
            return Task.FromResult(false);
        }

        // Verify rendered paths exist
        if (!result.Outputs.TryGetValue("RenderedPaths", out var pathsObj) ||
            pathsObj is not List<string> renderedPaths ||
            renderedPaths.Count != pageCount)
        {
            return Task.FromResult(false);
        }

        // Verify all rendered files exist
        if (renderedPaths.Any(path => !File.Exists(path)))
        {
            return Task.FromResult(false);
        }

        // Verify success rate is 100%
        if (!result.Outputs.TryGetValue("SuccessRate", out var successRateObj) ||
            successRateObj is not double successRate ||
            Math.Abs(successRate - 100.0) > 0.01)
        {
            return Task.FromResult(false);
        }

        // Verify total size is reasonable
        if (!result.Outputs.TryGetValue("TotalBytes", out var totalBytesObj) ||
            totalBytesObj is not long totalBytes ||
            totalBytes <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify timing metrics are valid
        if (!result.Outputs.TryGetValue("TotalTimeMs", out var totalTimeObj) ||
            totalTimeObj is not double totalTimeMs ||
            totalTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("AverageTimeMs", out var avgTimeObj) ||
            avgTimeObj is not double avgTimeMs ||
            avgTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
