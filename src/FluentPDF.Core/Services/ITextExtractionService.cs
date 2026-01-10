using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for extracting text from PDF documents.
/// Provides methods to extract text from individual pages or entire documents with caching support.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface ITextExtractionService
{
    /// <summary>
    /// Extracts text from a specific page in the PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageNumber">1-based page number to extract text from.</param>
    /// <returns>
    /// A Result containing the extracted text if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_PAGE_INVALID, PDF_TEXT_EXTRACTION_FAILED, PDF_TEXT_PAGE_LOAD_FAILED.
    /// </returns>
    Task<Result<string>> ExtractTextAsync(PdfDocument document, int pageNumber);

    /// <summary>
    /// Extracts text from all pages in the PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result containing a dictionary mapping page numbers to extracted text if successful,
    /// or a PdfError if the operation failed.
    /// Error codes: PDF_TEXT_EXTRACTION_FAILED, PDF_TEXT_PAGE_LOAD_FAILED.
    /// </returns>
    Task<Result<Dictionary<int, string>>> ExtractAllTextAsync(
        PdfDocument document,
        CancellationToken cancellationToken = default);
}
