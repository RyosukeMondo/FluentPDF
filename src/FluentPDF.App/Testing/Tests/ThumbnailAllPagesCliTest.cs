using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies thumbnail rendering for all pages in a PDF document.
/// Tests batch thumbnail generation with count, dimension, and performance verification.
/// </summary>
public sealed class ThumbnailAllPagesCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "thumbnail-all-pages";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Renders thumbnails for all pages in a PDF and verifies count, dimensions, and performance";

    /// <summary>
    /// Executes the thumbnail rendering test using the provided test context.
    /// Loads a test PDF, renders thumbnails for all pages, and captures performance metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing thumbnail count, dimensions, sizes, and metrics</returns>
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
            context.Logger.Information("Starting thumbnail all pages test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var thumbnailService = context.Services.GetRequiredService<IThumbnailRenderingService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

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

            // Track rendering metrics
            var renderStart = DateTime.UtcNow;
            var successCount = 0;
            var failCount = 0;
            var totalBytes = 0L;
            var thumbnailPaths = new List<string>();
            var thumbnailDimensions = new List<(int width, int height)>();

            // Render thumbnails for all pages
            context.Logger.Information("Rendering thumbnails for all {PageCount} pages", pageCount);

            for (int pageNum = 1; pageNum <= pageCount; pageNum++)
            {
                var pageRenderStart = DateTime.UtcNow;
                var thumbnailResult = await thumbnailService.RenderThumbnailAsync(document, pageNum);
                var pageRenderTime = DateTime.UtcNow - pageRenderStart;

                if (thumbnailResult.IsFailed)
                {
                    failCount++;
                    context.Logger.Warning(
                        "Failed to render thumbnail for page {PageNumber}: {Error}",
                        pageNum,
                        string.Join(", ", thumbnailResult.Errors.Select(e => e.Message))
                    );
                    continue;
                }

                // Save the thumbnail to a PNG file
                var outputPath = Path.Combine(context.WorkingDirectory, $"thumbnail_page_{pageNum}.png");
                using (var imageStream = thumbnailResult.Value)
                using (var fileStream = File.Create(outputPath))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                // Get file size
                var fileInfo = new FileInfo(outputPath);
                var fileSize = fileInfo.Length;
                totalBytes += fileSize;

                // Load image to get dimensions
                int imageWidth, imageHeight;
                using (var image = await SixLabors.ImageSharp.Image.LoadAsync(outputPath))
                {
                    imageWidth = image.Width;
                    imageHeight = image.Height;
                    thumbnailDimensions.Add((imageWidth, imageHeight));
                }

                thumbnailPaths.Add(outputPath);
                successCount++;

                context.Logger.Information(
                    "Page {PageNumber}: {Width}x{Height}, {FileSize} bytes, {RenderTimeMs:F1} ms",
                    pageNum,
                    imageWidth,
                    imageHeight,
                    fileSize,
                    pageRenderTime.TotalMilliseconds
                );
            }

            var totalRenderTime = DateTime.UtcNow - renderStart;

            // Calculate average dimensions (for verification)
            var avgWidth = thumbnailDimensions.Any() ? thumbnailDimensions.Average(d => d.width) : 0;
            var avgHeight = thumbnailDimensions.Any() ? thumbnailDimensions.Average(d => d.height) : 0;

            context.Logger.Information(
                "Thumbnail rendering complete: {SuccessCount}/{PageCount} successful, {FailCount} failed",
                successCount,
                pageCount,
                failCount
            );
            context.Logger.Information("Total size: {TotalBytes} bytes ({TotalKB:F2} KB)", totalBytes, totalBytes / 1024.0);
            context.Logger.Information("Total time: {TotalTimeMs:F0} ms", totalRenderTime.TotalMilliseconds);
            context.Logger.Information(
                "Average: {AvgTimeMs:F1} ms per thumbnail",
                successCount > 0 ? totalRenderTime.TotalMilliseconds / successCount : 0
            );

            // Store outputs for verification
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["SuccessCount"] = successCount;
            result.Outputs["FailCount"] = failCount;
            result.Outputs["TotalBytes"] = totalBytes;
            result.Outputs["TotalKB"] = totalBytes / 1024.0;
            result.Outputs["TotalTimeMs"] = totalRenderTime.TotalMilliseconds;
            result.Outputs["AverageTimeMs"] = successCount > 0 ? totalRenderTime.TotalMilliseconds / successCount : 0;
            result.Outputs["AverageWidth"] = avgWidth;
            result.Outputs["AverageHeight"] = avgHeight;
            result.Outputs["ThumbnailPaths"] = thumbnailPaths;
            result.Outputs["ThumbnailDimensions"] = thumbnailDimensions;
            result.Outputs["ExitCode"] = failCount > 0 ? 1 : 0;

            // Test is successful if at least some thumbnails were rendered
            // (full success requires all pages rendered, but we don't fail completely if some fail)
            result.Success = successCount > 0;
            result.Duration = DateTime.UtcNow - startTime;

            if (failCount > 0)
            {
                result.ErrorMessage = $"{failCount} thumbnail(s) failed to render (out of {pageCount} total)";
            }

            context.Logger.Information(
                "Thumbnail all pages test completed. {SuccessCount}/{PageCount} rendered, {AvgWidth:F0}x{AvgHeight:F0} avg, {TotalKB:F2} KB total",
                successCount,
                pageCount,
                avgWidth,
                avgHeight,
                totalBytes / 1024.0
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Thumbnail all pages test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the thumbnail rendering test produced expected outputs.
    /// Checks that thumbnails were generated, count matches page count, dimensions are valid, and files exist.
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

        // Verify page count and success count
        if (!result.Outputs.TryGetValue("PageCount", out var pageCountObj) ||
            pageCountObj is not int pageCount ||
            pageCount <= 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("SuccessCount", out var successCountObj) ||
            successCountObj is not int successCount ||
            successCount <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify success count matches page count (all thumbnails should render)
        if (successCount != pageCount)
        {
            return Task.FromResult(false);
        }

        // Verify thumbnail paths exist
        if (!result.Outputs.TryGetValue("ThumbnailPaths", out var pathsObj) ||
            pathsObj is not List<string> thumbnailPaths ||
            thumbnailPaths.Count != pageCount)
        {
            return Task.FromResult(false);
        }

        // Verify all thumbnail files exist
        if (thumbnailPaths.Any(path => !File.Exists(path)))
        {
            return Task.FromResult(false);
        }

        // Verify dimensions are valid
        if (!result.Outputs.TryGetValue("ThumbnailDimensions", out var dimensionsObj) ||
            dimensionsObj is not List<(int width, int height)> dimensions ||
            dimensions.Count != pageCount)
        {
            return Task.FromResult(false);
        }

        // All dimensions should be positive
        if (dimensions.Any(d => d.width <= 0 || d.height <= 0))
        {
            return Task.FromResult(false);
        }

        // Verify total size is reasonable (should be > 0)
        if (!result.Outputs.TryGetValue("TotalBytes", out var totalBytesObj) ||
            totalBytesObj is not long totalBytes ||
            totalBytes <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify timing metrics
        if (!result.Outputs.TryGetValue("TotalTimeMs", out var totalTimeObj) ||
            totalTimeObj is not double totalTimeMs ||
            totalTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
