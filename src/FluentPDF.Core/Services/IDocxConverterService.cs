using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for complete DOCX to PDF conversion operations.
/// Orchestrates the conversion pipeline: DOCX parsing → HTML rendering → PDF generation.
/// Optionally performs quality validation against LibreOffice baseline.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IDocxConverterService
{
    /// <summary>
    /// Converts a DOCX document to PDF format with optional quality validation.
    /// Orchestrates: file validation → Mammoth parsing → WebView2 rendering → validation → cleanup.
    /// </summary>
    /// <param name="inputPath">Full path to the source DOCX file.</param>
    /// <param name="outputPath">Full path where the PDF file will be saved.</param>
    /// <param name="options">Optional conversion settings. Uses defaults if null.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// A Result containing ConversionResult with metrics if successful, or a PdfError if the operation failed.
    /// Error codes: DOCX_FILE_NOT_FOUND, DOCX_INVALID_FORMAT, OUTPUT_PATH_INVALID,
    /// DOCX_PARSE_FAILED, HTML_TO_PDF_FAILED, CONVERSION_TIMEOUT, VALIDATION_FAILED.
    /// </returns>
    Task<Result<ConversionResult>> ConvertDocxToPdfAsync(
        string inputPath,
        string outputPath,
        ConversionOptions? options = null,
        CancellationToken cancellationToken = default);
}
