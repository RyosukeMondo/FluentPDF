using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies PDF rendering functionality by rendering all pages to PNG images.
/// Tests the complete rendering pipeline: document loading, page rendering, and image output.
/// </summary>
public sealed class RenderCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "render-pdf";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Renders all pages of a PDF to PNG images and verifies output files";

    /// <summary>
    /// Executes the PDF rendering test using the provided test context.
    /// Loads a test PDF, renders all pages to PNG files, and captures results.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing rendered file paths and diagnostics</returns>
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
            context.Logger.Information("Starting PDF render test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var renderingService = context.Services.GetRequiredService<IPdfRenderingService>();

            // Use a test PDF from fixtures
            var testPdfPath = Path.Combine(
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
            context.Logger.Information("Loaded PDF with {PageCount} pages", document.PageCount);

            // Render all pages to PNG files
            var outputFiles = new List<string>();
            var imageSizes = new List<(int Width, int Height)>();

            for (int pageNumber = 1; pageNumber <= document.PageCount; pageNumber++)
            {
                context.Logger.Information("Rendering page {PageNumber}/{PageCount}", pageNumber, document.PageCount);

                var renderResult = await renderingService.RenderPageAsync(document, pageNumber, zoomLevel: 1.0, dpi: 96);
                if (renderResult.IsFailed)
                {
                    result.ErrorMessage = $"Failed to render page {pageNumber}: {string.Join(", ", renderResult.Errors.Select(e => e.Message))}";
                    result.Duration = DateTime.UtcNow - startTime;
                    return result;
                }

                // Save the rendered image to a PNG file
                var outputPath = Path.Combine(context.WorkingDirectory, $"page_{pageNumber}.png");
                using (var imageStream = renderResult.Value)
                using (var fileStream = File.Create(outputPath))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                outputFiles.Add(outputPath);
                context.Logger.Information("Saved page {PageNumber} to: {OutputPath}", pageNumber, outputPath);

                // Verify the image file and get its dimensions
                if (File.Exists(outputPath))
                {
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(outputPath);
                    imageSizes.Add((image.Width, image.Height));
                    context.Logger.Information("Image dimensions: {Width}x{Height}", image.Width, image.Height);
                }
            }

            // Store outputs for verification
            result.Outputs["OutputFiles"] = outputFiles;
            result.Outputs["ImageSizes"] = imageSizes;
            result.Outputs["PageCount"] = document.PageCount;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information("PDF render test completed successfully. Rendered {Count} pages", outputFiles.Count);
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "PDF render test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the rendering test produced expected outputs.
    /// Checks that all pages were rendered, files exist, and images have valid dimensions.
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

        // Verify output files exist
        if (!result.Outputs.TryGetValue("OutputFiles", out var outputFilesObj) ||
            outputFilesObj is not List<string> outputFiles ||
            outputFiles.Count == 0)
        {
            return Task.FromResult(false);
        }

        // Verify all files actually exist on disk
        foreach (var file in outputFiles)
        {
            if (!File.Exists(file))
            {
                return Task.FromResult(false);
            }

            var fileInfo = new FileInfo(file);
            if (fileInfo.Length == 0)
            {
                return Task.FromResult(false);
            }
        }

        // Verify image sizes are valid
        if (!result.Outputs.TryGetValue("ImageSizes", out var imageSizesObj) ||
            imageSizesObj is not List<(int Width, int Height)> imageSizes)
        {
            return Task.FromResult(false);
        }

        // All images should have positive dimensions
        if (imageSizes.Any(size => size.Width <= 0 || size.Height <= 0))
        {
            return Task.FromResult(false);
        }

        // Verify page count matches output count
        if (result.Outputs.TryGetValue("PageCount", out var pageCountObj) &&
            pageCountObj is int pageCount)
        {
            if (outputFiles.Count != pageCount)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
