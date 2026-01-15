using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for searching text within PDF documents.
/// Provides methods to search for text with various options and returns matches with location information.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface ITextSearchService
{
    /// <summary>
    /// Searches for text across all pages in the PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="query">The text to search for.</param>
    /// <param name="options">Search options controlling case sensitivity and word matching.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result containing a list of SearchMatch objects if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_SEARCH_FAILED, PDF_TEXT_PAGE_LOAD_FAILED, PDF_SEARCH_CANCELLED.
    /// </returns>
    Task<Result<List<SearchMatch>>> SearchAsync(
        PdfDocument document,
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for text on a specific page in the PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageNumber">1-based page number to search.</param>
    /// <param name="query">The text to search for.</param>
    /// <param name="options">Search options controlling case sensitivity and word matching.</param>
    /// <returns>
    /// A Result containing a list of SearchMatch objects for the page if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_PAGE_INVALID, PDF_SEARCH_FAILED, PDF_TEXT_PAGE_LOAD_FAILED.
    /// </returns>
    Task<Result<List<SearchMatch>>> SearchPageAsync(
        PdfDocument document,
        int pageNumber,
        string query,
        SearchOptions? options = null);
}
