using System.Drawing;
using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF image insertion operations.
/// Provides methods to insert, manipulate, and delete image objects in PDF documents.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IImageInsertionService
{
    /// <summary>
    /// Inserts an image from a file into a PDF page.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageIndex">The zero-based page index where the image will be inserted.</param>
    /// <param name="imagePath">The file path to the image file (PNG, JPEG, BMP, GIF).</param>
    /// <param name="position">The position on the page where the image will be placed (in PDF points).</param>
    /// <returns>
    /// A Result containing the created ImageObject if successful,
    /// or an error if the operation failed (invalid file, corrupted image, unsupported format).
    /// </returns>
    Task<Result<ImageObject>> InsertImageAsync(PdfDocument document, int pageIndex, string imagePath, PointF position);

    /// <summary>
    /// Moves an image to a new position on the page.
    /// </summary>
    /// <param name="image">The image object to move.</param>
    /// <param name="newPosition">The new position for the image (in PDF points).</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> MoveImageAsync(ImageObject image, PointF newPosition);

    /// <summary>
    /// Scales an image to a new size.
    /// </summary>
    /// <param name="image">The image object to scale.</param>
    /// <param name="newSize">The new size for the image (in PDF points).</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Fails if size is below minimum (10x10 points).
    /// </returns>
    Task<Result> ScaleImageAsync(ImageObject image, SizeF newSize);

    /// <summary>
    /// Rotates an image by a specified angle.
    /// </summary>
    /// <param name="image">The image object to rotate.</param>
    /// <param name="angleDegrees">The rotation angle in degrees (can be any value, but typically 90, 180, 270).</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> RotateImageAsync(ImageObject image, float angleDegrees);

    /// <summary>
    /// Deletes an image from the PDF page.
    /// </summary>
    /// <param name="image">The image object to delete.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> DeleteImageAsync(ImageObject image);
}
