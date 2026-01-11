using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for rendering PDF page thumbnails as low-resolution images.
/// </summary>
public interface IThumbnailRenderingService
{
    /// <summary>
    /// Renders a PDF page thumbnail at low resolution optimized for preview display.
    /// The stream contains PNG-encoded image data at reduced DPI and zoom level.
    /// </summary>
    /// <param name="document">The PDF document containing the page.</param>
    /// <param name="pageNumber">The 1-based page number to render.</param>
    /// <returns>A result containing a Stream with PNG-encoded thumbnail data, or an error if rendering failed.</returns>
    Task<Result<Stream>> RenderThumbnailAsync(PdfDocument document, int pageNumber);
}
