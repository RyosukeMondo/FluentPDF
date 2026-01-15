using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF watermark operations.
/// Provides methods to apply text and image watermarks to PDF documents.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IWatermarkService
{
    /// <summary>
    /// Applies a text watermark to specified pages in a PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="config">Configuration for the text watermark (text, font, color, position, etc.).</param>
    /// <param name="pageRange">The range of pages to apply the watermark to.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Fails if document is null, config is invalid, or PDFium operations fail.
    /// </returns>
    Task<Result> ApplyTextWatermarkAsync(PdfDocument document, TextWatermarkConfig config, WatermarkPageRange pageRange);

    /// <summary>
    /// Applies an image watermark to specified pages in a PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="config">Configuration for the image watermark (image path, scale, position, etc.).</param>
    /// <param name="pageRange">The range of pages to apply the watermark to.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Fails if document is null, image file not found, config is invalid, or PDFium operations fail.
    /// </returns>
    Task<Result> ApplyImageWatermarkAsync(PdfDocument document, ImageWatermarkConfig config, WatermarkPageRange pageRange);

    /// <summary>
    /// Removes all watermarks from specified pages in a PDF document.
    /// Only removes watermarks that were added by this service (tagged watermarks).
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageRange">The range of pages to remove watermarks from.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Returns success even if no watermarks were found to remove.
    /// </returns>
    Task<Result> RemoveWatermarksAsync(PdfDocument document, WatermarkPageRange pageRange);

    /// <summary>
    /// Generates a preview image showing how a watermark will appear on a specific page.
    /// Does not modify the actual document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageIndex">The zero-based page index to generate preview for.</param>
    /// <param name="textConfig">Text watermark configuration (null if using image watermark).</param>
    /// <param name="imageConfig">Image watermark configuration (null if using text watermark).</param>
    /// <returns>
    /// A Result containing a PNG image as byte array if successful,
    /// or an error if preview generation failed.
    /// Exactly one of textConfig or imageConfig must be non-null.
    /// </returns>
    Task<Result<byte[]>> GeneratePreviewAsync(
        PdfDocument document,
        int pageIndex,
        TextWatermarkConfig? textConfig,
        ImageWatermarkConfig? imageConfig);
}
