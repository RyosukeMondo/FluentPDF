using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for finding and replacing text within PDF documents.
/// Provides methods to replace text with preview and undo support.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface ITextReplacementService
{
    /// <summary>
    /// Replaces a specific occurrence of text in the PDF document.
    /// This creates a text annotation overlay (PDFium limitation - not true content editing).
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="match">The search match to replace.</param>
    /// <param name="replacementText">The new text to insert.</param>
    /// <param name="preview">If true, returns preview without modifying the document.</param>
    /// <returns>
    /// A Result containing the replacement operation details if successful,
    /// or a PdfError if the operation failed.
    /// Error codes: PDF_REPLACE_FAILED, PDF_PAGE_INVALID, PDF_TEXT_PAGE_LOAD_FAILED.
    /// </returns>
    Task<Result<TextReplacement>> ReplaceAsync(
        PdfDocument document,
        SearchMatch match,
        string replacementText,
        bool preview = false);

    /// <summary>
    /// Replaces all occurrences found by a search query in the PDF document.
    /// This creates text annotation overlays (PDFium limitation - not true content editing).
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="findText">The text to find.</param>
    /// <param name="replaceText">The text to replace with.</param>
    /// <param name="options">Search options controlling case sensitivity and word matching.</param>
    /// <param name="preview">If true, returns preview without modifying the document.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result containing a list of replacement operations if successful,
    /// or a PdfError if the operation failed.
    /// Error codes: PDF_REPLACE_FAILED, PDF_SEARCH_FAILED, PDF_SEARCH_CANCELLED.
    /// </returns>
    Task<Result<List<TextReplacement>>> ReplaceAllAsync(
        PdfDocument document,
        string findText,
        string replaceText,
        SearchOptions? options = null,
        bool preview = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes replacement operations by removing the replacement annotations.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="replacements">The replacements to undo.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Error codes: PDF_UNDO_FAILED, PDF_ANNOTATION_NOT_FOUND.
    /// </returns>
    Task<Result> UndoReplacementsAsync(
        PdfDocument document,
        List<TextReplacement> replacements);
}
