using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentResults;
using Mammoth;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for DOCX document parsing using Mammoth.NET library.
/// Implements asynchronous operations with comprehensive error handling and structured logging.
/// Converts DOCX files to clean HTML with embedded images as base64 data URIs.
/// </summary>
public sealed class DocxParserService : IDocxParserService
{
    private readonly ILogger<DocxParserService> _logger;
    private readonly DocumentConverter _documentConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocxParserService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public DocxParserService(ILogger<DocxParserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentConverter = new DocumentConverter();
    }

    /// <inheritdoc />
    public async Task<Result<string>> ParseDocxToHtmlAsync(string filePath)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Parsing DOCX document. CorrelationId={CorrelationId}, FilePath={FilePath}",
            correlationId, filePath);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            var error = new PdfError(
                "DOCX_FILE_NOT_FOUND",
                $"DOCX file not found: {filePath}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "DOCX file not found. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }

        // Validate file extension
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension != ".docx")
        {
            var error = new PdfError(
                "DOCX_INVALID_FORMAT",
                $"File is not a DOCX document. Extension: {extension}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("Extension", extension)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "Invalid file format. CorrelationId={CorrelationId}, FilePath={FilePath}, Extension={Extension}",
                correlationId, filePath, extension);

            return Result.Fail(error);
        }

        try
        {
            // Parse DOCX on background thread
            var html = await Task.Run(() =>
            {
                using var fileStream = File.OpenRead(filePath);
                var result = _documentConverter.ConvertToHtml(fileStream);

                // Check for conversion warnings (non-fatal)
                if (result.Warnings.Any())
                {
                    _logger.LogWarning(
                        "DOCX conversion completed with warnings. CorrelationId={CorrelationId}, WarningCount={WarningCount}",
                        correlationId, result.Warnings.Count());

                    foreach (var warning in result.Warnings)
                    {
                        _logger.LogDebug(
                            "Conversion warning: {Warning}. CorrelationId={CorrelationId}",
                            warning, correlationId);
                    }
                }

                return result.Value;
            });

            var htmlLength = html.Length;
            var fileSize = new FileInfo(filePath).Length;

            _logger.LogInformation(
                "DOCX parsed successfully. CorrelationId={CorrelationId}, FilePath={FilePath}, HtmlLength={HtmlLength}, FileSizeBytes={FileSizeBytes}",
                correlationId, filePath, htmlLength, fileSize);

            return Result.Ok(html);
        }
        catch (InvalidDataException ex)
        {
            var error = new PdfError(
                "DOCX_INVALID_FORMAT",
                $"Invalid DOCX format or corrupted file: {ex.Message}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Invalid DOCX format. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }
        catch (IOException ex)
        {
            var error = new PdfError(
                "DOCX_READ_FAILED",
                $"Failed to read DOCX file: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to read DOCX file. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "DOCX_PARSE_FAILED",
                $"Failed to parse DOCX document: {ex.Message}",
                ErrorCategory.Conversion,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to parse DOCX document. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }
    }
}
