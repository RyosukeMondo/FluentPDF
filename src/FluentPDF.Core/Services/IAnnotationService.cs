using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF annotation operations.
/// Provides methods to create, read, update, and delete annotations in PDF documents.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IAnnotationService
{
    /// <summary>
    /// Gets all annotations from a specific page in a PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageNumber">The zero-based page number.</param>
    /// <returns>
    /// A Result containing a list of Annotation objects if successful,
    /// or an error if the operation failed.
    /// Returns an empty list if the page has no annotations.
    /// </returns>
    Task<Result<List<Annotation>>> GetAnnotationsAsync(PdfDocument document, int pageNumber);

    /// <summary>
    /// Creates a new annotation on a page.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="annotation">The annotation to create.</param>
    /// <returns>
    /// A Result containing the created Annotation with updated properties if successful,
    /// or an error if the operation failed.
    /// </returns>
    Task<Result<Annotation>> CreateAnnotationAsync(PdfDocument document, Annotation annotation);

    /// <summary>
    /// Updates an existing annotation.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="annotation">The annotation with updated properties.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> UpdateAnnotationAsync(PdfDocument document, Annotation annotation);

    /// <summary>
    /// Deletes an annotation from a page.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="pageNumber">The zero-based page number.</param>
    /// <param name="annotationIndex">The zero-based annotation index on the page.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> DeleteAnnotationAsync(PdfDocument document, int pageNumber, int annotationIndex);

    /// <summary>
    /// Saves all annotations in the document back to the PDF file.
    /// Creates a backup of the original file before saving.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="filePath">The path to save the PDF file.</param>
    /// <param name="createBackup">Whether to create a .bak backup file before saving.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// If saving fails and a backup was created, the backup will be restored.
    /// </returns>
    Task<Result> SaveAnnotationsAsync(PdfDocument document, string filePath, bool createBackup = true);
}
