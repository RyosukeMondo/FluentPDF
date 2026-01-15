using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for DOCX document parsing operations.
/// Provides methods to parse DOCX files and extract HTML content with embedded images.
/// All operations return Result&lt;T&gt; for consistent error handling.
/// </summary>
public interface IDocxParserService
{
    /// <summary>
    /// Parses a DOCX document and converts it to HTML with embedded images.
    /// Images are embedded as base64 data URIs for self-contained HTML output.
    /// </summary>
    /// <param name="filePath">Full path to the DOCX file.</param>
    /// <returns>
    /// A Result containing the HTML string if successful, or a PdfError if the operation failed.
    /// Error codes: DOCX_FILE_NOT_FOUND, DOCX_INVALID_FORMAT, DOCX_PARSE_FAILED.
    /// </returns>
    Task<Result<string>> ParseDocxToHtmlAsync(string filePath);
}
