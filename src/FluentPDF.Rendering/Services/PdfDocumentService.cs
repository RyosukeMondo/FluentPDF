using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for PDF document loading and management operations using PDFium.
/// Implements asynchronous operations with comprehensive error handling and structured logging.
/// </summary>
public sealed class PdfDocumentService : IPdfDocumentService
{
    private readonly ILogger<PdfDocumentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfDocumentService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public PdfDocumentService(ILogger<PdfDocumentService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<PdfDocument>> LoadDocumentAsync(string filePath, string? password = null)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Loading PDF document. CorrelationId={CorrelationId}, FilePath={FilePath}, HasPassword={HasPassword}",
            correlationId, filePath, password != null);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            var error = new PdfError(
                "PDF_FILE_NOT_FOUND",
                $"PDF file not found: {filePath}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "PDF file not found. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }

        // Load document on background thread
        return await Task.Run(() =>
        {
            try
            {
                // Load PDF document
                var handle = PdfiumInterop.LoadDocument(filePath, password);

                if (handle.IsInvalid)
                {
                    var errorCode = PdfiumInterop.GetLastError();
                    return HandleLoadError(errorCode, filePath, correlationId);
                }

                // Get page count
                var pageCount = PdfiumInterop.GetPageCount(handle);
                if (pageCount == 0)
                {
                    handle.Dispose();
                    var error = new PdfError(
                        "PDF_INVALID_FORMAT",
                        "PDF document has no pages or is corrupted.",
                        ErrorCategory.Validation,
                        ErrorSeverity.Error)
                        .WithContext("FilePath", filePath)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError(
                        "PDF document has no pages. CorrelationId={CorrelationId}, FilePath={FilePath}",
                        correlationId, filePath);

                    return Result.Fail(error);
                }

                // Get file size
                var fileInfo = new FileInfo(filePath);
                var fileSizeBytes = fileInfo.Length;

                var document = new PdfDocument
                {
                    FilePath = filePath,
                    PageCount = pageCount,
                    Handle = handle,
                    LoadedAt = DateTime.UtcNow,
                    FileSizeBytes = fileSizeBytes
                };

                _logger.LogInformation(
                    "PDF document loaded successfully. CorrelationId={CorrelationId}, FilePath={FilePath}, PageCount={PageCount}, FileSizeBytes={FileSizeBytes}",
                    correlationId, filePath, pageCount, fileSizeBytes);

                return Result.Ok(document);
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "PDF_LOAD_FAILED",
                    $"Failed to load PDF document: {ex.Message}",
                    ErrorCategory.System,
                    ErrorSeverity.Error)
                    .WithContext("FilePath", filePath)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogError(ex,
                    "Failed to load PDF document. CorrelationId={CorrelationId}, FilePath={FilePath}",
                    correlationId, filePath);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<PdfPage>> GetPageInfoAsync(PdfDocument document, int pageNumber)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogDebug(
            "Getting page info. CorrelationId={CorrelationId}, FilePath={FilePath}, PageNumber={PageNumber}",
            correlationId, document.FilePath, pageNumber);

        // Validate page number
        if (pageNumber < 1 || pageNumber > document.PageCount)
        {
            var error = new PdfError(
                "PDF_PAGE_INVALID",
                $"Page number {pageNumber} is out of range. Valid range: 1-{document.PageCount}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("PageNumber", pageNumber)
                .WithContext("TotalPages", document.PageCount)
                .WithContext("FilePath", document.FilePath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogWarning(
                "Invalid page number. CorrelationId={CorrelationId}, PageNumber={PageNumber}, TotalPages={TotalPages}",
                correlationId, pageNumber, document.PageCount);

            return Result.Fail(error);
        }

        return await Task.Run(() =>
        {
            try
            {
                // Load page (0-based index)
                using var pageHandle = PdfiumInterop.LoadPage((SafePdfDocumentHandle)document.Handle, pageNumber - 1);

                if (pageHandle.IsInvalid)
                {
                    var error = new PdfError(
                        "PDF_PAGE_LOAD_FAILED",
                        $"Failed to load page {pageNumber}.",
                        ErrorCategory.Rendering,
                        ErrorSeverity.Error)
                        .WithContext("PageNumber", pageNumber)
                        .WithContext("FilePath", document.FilePath)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError(
                        "Failed to load page. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                        correlationId, pageNumber);

                    return Result.Fail(error);
                }

                // Get page dimensions
                var width = PdfiumInterop.GetPageWidth(pageHandle);
                var height = PdfiumInterop.GetPageHeight(pageHandle);

                var page = new PdfPage
                {
                    PageNumber = pageNumber,
                    Width = width,
                    Height = height
                };

                _logger.LogDebug(
                    "Page info retrieved. CorrelationId={CorrelationId}, PageNumber={PageNumber}, Width={Width}, Height={Height}",
                    correlationId, pageNumber, width, height);

                return Result.Ok(page);
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "PDF_PAGE_LOAD_FAILED",
                    $"Failed to get page info: {ex.Message}",
                    ErrorCategory.System,
                    ErrorSeverity.Error)
                    .WithContext("PageNumber", pageNumber)
                    .WithContext("FilePath", document.FilePath)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogError(ex,
                    "Failed to get page info. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                    correlationId, pageNumber);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public Result CloseDocument(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        try
        {
            _logger.LogInformation(
                "Closing PDF document. FilePath={FilePath}",
                document.FilePath);

            document.Dispose();

            _logger.LogInformation(
                "PDF document closed successfully. FilePath={FilePath}",
                document.FilePath);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "PDF_CLOSE_FAILED",
                $"Failed to close PDF document: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Warning)
                .WithContext("FilePath", document.FilePath)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogWarning(ex,
                "Failed to close PDF document. FilePath={FilePath}",
                document.FilePath);

            return Result.Fail(error);
        }
    }

    private Result<PdfDocument> HandleLoadError(uint errorCode, string filePath, Guid correlationId)
    {
        var (code, message, category) = errorCode switch
        {
            PdfiumInterop.ErrorCodes.File => (
                "PDF_FILE_NOT_FOUND",
                "PDF file not found or could not be opened.",
                ErrorCategory.IO),

            PdfiumInterop.ErrorCodes.Format => (
                "PDF_INVALID_FORMAT",
                "File is not in PDF format or is corrupted.",
                ErrorCategory.Validation),

            PdfiumInterop.ErrorCodes.Password => (
                "PDF_REQUIRES_PASSWORD",
                "PDF document requires a password or incorrect password provided.",
                ErrorCategory.Security),

            PdfiumInterop.ErrorCodes.Security => (
                "PDF_UNSUPPORTED_SECURITY",
                "PDF document uses an unsupported security scheme.",
                ErrorCategory.Security),

            PdfiumInterop.ErrorCodes.Page => (
                "PDF_CORRUPTED",
                "PDF document is corrupted or has invalid page data.",
                ErrorCategory.Validation),

            _ => (
                "PDF_LOAD_FAILED",
                $"Failed to load PDF document. PDFium error code: {errorCode}",
                ErrorCategory.System)
        };

        var error = new PdfError(code, message, category, ErrorSeverity.Error)
            .WithContext("FilePath", filePath)
            .WithContext("CorrelationId", correlationId)
            .WithContext("PDFiumErrorCode", errorCode);

        _logger.LogError(
            "PDF load failed. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}, PDFiumError={PDFiumError}",
            correlationId, code, errorCode);

        return Result.Fail(error);
    }
}
