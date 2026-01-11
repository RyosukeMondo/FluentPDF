using FluentResults;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Service contract for headless PDF rendering to image files.
/// Provides methods to render PDF pages to PNG files without requiring a UI context.
/// Used for visual regression testing in CI/CD pipelines.
/// </summary>
public interface IHeadlessRenderingService : IDisposable
{
    /// <summary>
    /// Renders a specific page of a PDF document to a PNG file.
    /// </summary>
    /// <param name="pdfPath">Full path to the PDF file.</param>
    /// <param name="pageNumber">1-based page number to render.</param>
    /// <param name="outputPath">Full path where the PNG file should be saved.</param>
    /// <param name="dpi">Resolution in dots per inch (default: 96).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result indicating success or failure of the rendering operation.
    /// Error codes: PDF_FILE_NOT_FOUND, PDF_LOAD_FAILED, PAGE_INVALID, RENDERING_FAILED, IO_ERROR.
    /// </returns>
    Task<Result> RenderPageToFileAsync(
        string pdfPath,
        int pageNumber,
        string outputPath,
        int dpi = 96,
        CancellationToken cancellationToken = default);
}
