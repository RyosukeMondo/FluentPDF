using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF bookmark extraction operations.
/// Provides methods to extract hierarchical bookmark structures from PDF documents.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IBookmarkService
{
    /// <summary>
    /// Extracts the hierarchical bookmark tree from a PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document to extract bookmarks from.</param>
    /// <returns>
    /// A Result containing a list of root-level BookmarkNode objects if successful,
    /// or a PdfError if the operation failed.
    /// Returns an empty list if the document has no bookmarks.
    /// Error codes: BOOKMARK_EXTRACTION_FAILED, BOOKMARK_INVALID_HANDLE.
    /// </returns>
    Task<Result<List<BookmarkNode>>> ExtractBookmarksAsync(PdfDocument document);
}
