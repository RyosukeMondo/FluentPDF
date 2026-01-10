using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for HTML to PDF conversion operations.
/// Provides methods to convert HTML content to PDF files using WebView2 rendering engine.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IHtmlToPdfService
{
    /// <summary>
    /// Converts HTML content to a PDF file using the Chromium rendering engine.
    /// Supports embedded images, CSS styling, and produces high-quality PDF output.
    /// </summary>
    /// <param name="htmlContent">The HTML content to convert. Must not be null or empty.</param>
    /// <param name="outputPath">Full path where the PDF file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// A Result containing the output file path if successful, or a PdfError if the operation failed.
    /// Error codes: HTML_EMPTY, OUTPUT_PATH_EMPTY, WEBVIEW2_RUNTIME_NOT_FOUND,
    /// WEBVIEW2_INIT_FAILED, HTML_TO_PDF_FAILED, CONVERSION_TIMEOUT, CONVERSION_CANCELLED.
    /// </returns>
    Task<Result<string>> ConvertHtmlToPdfAsync(
        string htmlContent,
        string outputPath,
        CancellationToken cancellationToken = default);
}
