using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies single page rendering functionality.
/// Tests page rendering with dimension and file size verification.
/// </summary>
public sealed class PageRenderCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "page-render";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Renders a single PDF page to PNG and verifies dimensions and file size";

    /// <summary>
    /// Executes the page rendering test using the provided test context.
    /// Loads a test PDF, renders a specified page, and captures rendering metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing rendered file path, dimensions, and metrics</returns>
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
            context.Logger.Information("Starting page render test");

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

            // Get page number from context data or use default
            var pageNumber = context.Data.TryGetValue("PageNumber", out var pageObj) && pageObj is int page
                ? page
                : 1;

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
            context.Logger.Information("Loaded PDF with {PageCount} pages", document.PageCount);

            // Validate page number
            if (pageNumber < 1 || pageNumber > document.PageCount)
            {
                result.ErrorMessage = $"Invalid page number {pageNumber}. Document has {document.PageCount} pages.";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Record render start time
            var renderStart = DateTime.UtcNow;

            // Render the specified page
            context.Logger.Information("Rendering page {PageNumber}", pageNumber);
            var renderResult = await renderingService.RenderPageAsync(document, pageNumber, zoomLevel: 1.0, dpi: 96);
            if (renderResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to render page {pageNumber}: {string.Join(", ", renderResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var renderTime = DateTime.UtcNow - renderStart;

            // Save the rendered image to a PNG file
            var outputPath = Path.Combine(context.WorkingDirectory, $"page_{pageNumber}.png");
            using (var imageStream = renderResult.Value)
            using (var fileStream = File.Create(outputPath))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            context.Logger.Information("Saved page {PageNumber} to: {OutputPath}", pageNumber, outputPath);

            // Get file size
            var fileInfo = new FileInfo(outputPath);
            var fileSizeKB = fileInfo.Length / 1024.0;

            // Load image to get dimensions
            int imageWidth, imageHeight;
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(outputPath))
            {
                imageWidth = image.Width;
                imageHeight = image.Height;
                context.Logger.Information("Image dimensions: {Width}x{Height}", imageWidth, imageHeight);
            }

            context.Logger.Information("File size: {FileSizeKB:F2} KB", fileSizeKB);
            context.Logger.Information("Render time: {RenderTimeMs} ms", renderTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ImageWidth"] = imageWidth;
            result.Outputs["ImageHeight"] = imageHeight;
            result.Outputs["FileSizeKB"] = fileSizeKB;
            result.Outputs["RenderTimeMs"] = renderTime.TotalMilliseconds;
            result.Outputs["PageNumber"] = pageNumber;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Page render test completed successfully. Page {PageNumber}, {Width}x{Height}, {FileSizeKB:F2} KB, {RenderTimeMs:F0} ms",
                pageNumber, imageWidth, imageHeight, fileSizeKB, renderTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Page render test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the rendering test produced expected outputs.
    /// Checks that the page was rendered, file exists, has valid dimensions, and meets size requirements.
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

        // Verify file size is reasonable (>1KB as per requirement 1.3)
        if (!result.Outputs.TryGetValue("FileSizeKB", out var fileSizeObj) ||
            fileSizeObj is not double fileSizeKB ||
            fileSizeKB <= 1.0)
        {
            return Task.FromResult(false);
        }

        // Verify image has valid dimensions
        if (!result.Outputs.TryGetValue("ImageWidth", out var widthObj) ||
            widthObj is not int width ||
            width <= 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("ImageHeight", out var heightObj) ||
            heightObj is not int height ||
            height <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify render time was captured
        if (!result.Outputs.TryGetValue("RenderTimeMs", out var renderTimeObj) ||
            renderTimeObj is not double renderTimeMs ||
            renderTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
