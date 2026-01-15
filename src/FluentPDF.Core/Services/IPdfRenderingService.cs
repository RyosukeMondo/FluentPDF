using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for rendering PDF pages to images.
/// </summary>
public interface IPdfRenderingService
{
    /// <summary>
    /// Renders a PDF page to an image stream at the specified zoom level and DPI.
    /// The stream contains PNG-encoded image data that can be converted to platform-specific image types.
    /// </summary>
    /// <param name="document">The PDF document containing the page.</param>
    /// <param name="pageNumber">The 1-based page number to render.</param>
    /// <param name="zoomLevel">The zoom level (1.0 = 100%, 2.0 = 200%, etc.).</param>
    /// <param name="dpi">The dots per inch for rendering. Default is 96 (standard screen DPI).</param>
    /// <returns>A result containing a Stream with PNG-encoded image data, or an error if rendering failed.</returns>
    Task<Result<Stream>> RenderPageAsync(PdfDocument document, int pageNumber, double zoomLevel, double dpi = 96);
}
