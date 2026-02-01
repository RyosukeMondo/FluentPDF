using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for FDF (Forms Data Format) and XFDF (XML Forms Data Format) operations.
/// Provides methods to export and import PDF annotations and form data.
/// FDF is the standard format for exchanging PDF annotations and form data.
/// </summary>
public interface IFdfService
{
    /// <summary>
    /// Exports all annotations from a PDF document to an XFDF (XML) file.
    /// XFDF is the XML-based variant of FDF, which is more human-readable and easier to process.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="outputPath">Path where the XFDF file should be saved.</param>
    /// <param name="pageFilter">Optional: only export annotations from specific pages (null = all pages).</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> ExportAnnotationsToXfdfAsync(
        PdfDocument document,
        string outputPath,
        int[]? pageFilter = null);

    /// <summary>
    /// Imports annotations from an XFDF file into a PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="xfdfPath">Path to the XFDF file to import.</param>
    /// <param name="mergeBehavior">How to handle existing annotations (replace, merge, skip duplicates).</param>
    /// <returns>
    /// A Result containing the number of annotations imported if successful,
    /// or an error if the operation failed.
    /// </returns>
    Task<Result<int>> ImportAnnotationsFromXfdfAsync(
        PdfDocument document,
        string xfdfPath,
        AnnotationMergeBehavior mergeBehavior = AnnotationMergeBehavior.Merge);

    /// <summary>
    /// Exports form field data from a PDF document to an XFDF file.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="outputPath">Path where the XFDF file should be saved.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> ExportFormDataToXfdfAsync(PdfDocument document, string outputPath);

    /// <summary>
    /// Imports form field data from an XFDF file into a PDF document.
    /// </summary>
    /// <param name="document">The loaded PDF document.</param>
    /// <param name="xfdfPath">Path to the XFDF file to import.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// </returns>
    Task<Result> ImportFormDataFromXfdfAsync(PdfDocument document, string xfdfPath);
}

/// <summary>
/// Defines how to handle existing annotations when importing.
/// </summary>
public enum AnnotationMergeBehavior
{
    /// <summary>
    /// Replace all existing annotations with imported ones.
    /// </summary>
    Replace,

    /// <summary>
    /// Merge imported annotations with existing ones (add new, keep existing).
    /// </summary>
    Merge,

    /// <summary>
    /// Skip importing annotations that appear to be duplicates.
    /// </summary>
    SkipDuplicates
}
