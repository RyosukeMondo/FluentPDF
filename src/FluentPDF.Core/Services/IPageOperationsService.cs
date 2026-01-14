using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF page-level manipulation operations.
/// Provides methods for rotating, deleting, reordering, and inserting pages using lossless operations.
/// All operations modify the document in-place and return Result for consistent error handling.
/// </summary>
public interface IPageOperationsService
{
    /// <summary>
    /// Rotates specified pages by the given angle.
    /// The rotation is applied cumulatively to any existing rotation.
    /// </summary>
    /// <param name="document">The PDF document to modify.</param>
    /// <param name="pageIndices">Zero-based indices of pages to rotate.</param>
    /// <param name="angle">Rotation angle (90°, 180°, or 270° clockwise).</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result indicating success or failure of the operation.
    /// Error codes: PDF_INVALID_DOCUMENT, PDF_PAGE_INVALID, PDF_ROTATION_FAILED.
    /// </returns>
    Task<Result> RotatePagesAsync(
        PdfDocument document,
        int[] pageIndices,
        RotationAngle angle,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes specified pages from the document.
    /// Cannot delete all pages - at least one page must remain.
    /// </summary>
    /// <param name="document">The PDF document to modify.</param>
    /// <param name="pageIndices">Zero-based indices of pages to delete.</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result indicating success or failure of the operation.
    /// Error codes: PDF_INVALID_DOCUMENT, PDF_PAGE_INVALID, PDF_DELETE_ALL_PAGES,
    /// PDF_DELETE_FAILED.
    /// </returns>
    Task<Result> DeletePagesAsync(
        PdfDocument document,
        int[] pageIndices,
        CancellationToken ct = default);

    /// <summary>
    /// Reorders pages by moving specified pages to a target position.
    /// Pages are moved as a contiguous block to the target index.
    /// </summary>
    /// <param name="document">The PDF document to modify.</param>
    /// <param name="pageIndices">Zero-based indices of pages to move (will be moved as a block).</param>
    /// <param name="targetIndex">Zero-based target position where pages should be moved.</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result indicating success or failure of the operation.
    /// Error codes: PDF_INVALID_DOCUMENT, PDF_PAGE_INVALID, PDF_REORDER_FAILED.
    /// </returns>
    Task<Result> ReorderPagesAsync(
        PdfDocument document,
        int[] pageIndices,
        int targetIndex,
        CancellationToken ct = default);

    /// <summary>
    /// Inserts a new blank page at the specified position.
    /// The page size can match the current page or use a standard size.
    /// </summary>
    /// <param name="document">The PDF document to modify.</param>
    /// <param name="insertAtIndex">Zero-based index where the blank page should be inserted.</param>
    /// <param name="pageSize">Size specification for the new blank page.</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result indicating success or failure of the operation.
    /// Error codes: PDF_INVALID_DOCUMENT, PDF_PAGE_INVALID, PDF_INSERT_FAILED.
    /// </returns>
    Task<Result> InsertBlankPageAsync(
        PdfDocument document,
        int insertAtIndex,
        PageSize pageSize,
        CancellationToken ct = default);
}

/// <summary>
/// Rotation angles for page rotation operations.
/// All angles are clockwise.
/// </summary>
public enum RotationAngle
{
    /// <summary>
    /// Rotate 90 degrees clockwise.
    /// </summary>
    Rotate90 = 90,

    /// <summary>
    /// Rotate 180 degrees (upside down).
    /// </summary>
    Rotate180 = 180,

    /// <summary>
    /// Rotate 270 degrees clockwise (equivalent to 90 degrees counter-clockwise).
    /// </summary>
    Rotate270 = 270
}

/// <summary>
/// Standard page sizes for blank page insertion.
/// </summary>
public enum PageSize
{
    /// <summary>
    /// Use the same size as the page at the insertion position.
    /// If inserting at the end, uses the size of the last page.
    /// </summary>
    SameAsCurrent,

    /// <summary>
    /// US Letter size: 8.5 x 11 inches (612 x 792 points).
    /// </summary>
    Letter,

    /// <summary>
    /// ISO A4 size: 210 x 297 mm (595 x 842 points).
    /// </summary>
    A4,

    /// <summary>
    /// US Legal size: 8.5 x 14 inches (612 x 1008 points).
    /// </summary>
    Legal
}
