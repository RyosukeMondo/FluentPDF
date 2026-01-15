using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF document loading and management operations.
/// Provides methods to load PDF documents, retrieve page information, and close documents.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IPdfDocumentService
{
    /// <summary>
    /// Loads a PDF document from the specified file path.
    /// </summary>
    /// <param name="filePath">Full path to the PDF file.</param>
    /// <param name="password">Optional password for encrypted PDFs. Pass null for unencrypted documents.</param>
    /// <returns>
    /// A Result containing the loaded PdfDocument if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_FILE_NOT_FOUND, PDF_INVALID_FORMAT, PDF_CORRUPTED, PDF_REQUIRES_PASSWORD, PDF_LOAD_FAILED.
    /// </returns>
    Task<Result<PdfDocument>> LoadDocumentAsync(string filePath, string? password = null);

    /// <summary>
    /// Gets metadata information about a specific page in the PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageNumber">1-based page number to retrieve information for.</param>
    /// <returns>
    /// A Result containing the PdfPage metadata if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_PAGE_INVALID, PDF_PAGE_LOAD_FAILED.
    /// </returns>
    Task<Result<PdfPage>> GetPageInfoAsync(PdfDocument document, int pageNumber);

    /// <summary>
    /// Closes a PDF document and releases all associated resources.
    /// </summary>
    /// <param name="document">The PDF document to close.</param>
    /// <returns>
    /// A Result indicating success or failure of the close operation.
    /// </returns>
    Result CloseDocument(PdfDocument document);
}
