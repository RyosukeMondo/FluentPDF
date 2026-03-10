using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for PDF page-level manipulation operations using QPDF.
/// Provides rotating, deleting, reordering, and inserting pages with proper error handling.
/// </summary>
public sealed partial class PageOperationsService : IPageOperationsService
{
    private readonly ILogger<PageOperationsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageOperationsService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public PageOperationsService(ILogger<PageOperationsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure QPDF is initialized
        if (!QpdfNative.Initialize())
        {
            throw new InvalidOperationException("Failed to initialize QPDF library. Ensure qpdf library is available.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> RotatePagesAsync(
        PdfDocument document,
        int[] pageIndices,
        RotationAngle angle,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting page rotation. CorrelationId={CorrelationId}, PageCount={PageCount}, Angle={Angle}",
            correlationId, pageIndices?.Length ?? 0, angle);

        // Validate inputs
        var validationResult = ValidateDocument(document, correlationId);
        if (validationResult.IsFailed)
        {
            return validationResult;
        }

        if (pageIndices == null || pageIndices.Length == 0)
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "No pages specified for rotation.",
                ErrorCategory.Validation,
                correlationId);
        }

        return await Task.Run(() =>
        {
            SafeQpdfJobHandle? job = null;

            try
            {
                // Load document
                job = QpdfNative.CreateJob();
                if (job.IsInvalid)
                {
                    return CreateError(
                        "PDF_ROTATION_FAILED",
                        "Failed to create QPDF job for rotation operation.",
                        ErrorCategory.System,
                        correlationId);
                }

                var readResult = QpdfNative.ReadDocument(job, document.FilePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, readResult, document.FilePath, correlationId, "read document for rotation");
                }

                var totalPages = QpdfNative.GetPageCount(job);

                // Validate page indices
                foreach (var pageIndex in pageIndices)
                {
                    if (pageIndex < 0 || pageIndex >= totalPages)
                    {
                        return CreateError(
                            "PDF_PAGE_INVALID",
                            $"Page index {pageIndex} is out of range. Document has {totalPages} pages.",
                            ErrorCategory.Validation,
                            correlationId,
                            ("PageIndex", pageIndex),
                            ("TotalPages", totalPages));
                    }
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                // Rotate each page
                var rotationDegrees = (int)angle;
                foreach (var pageIndex in pageIndices)
                {
                    var pageNumber = pageIndex + 1; // QPDF uses 1-based page numbers
                    var pageHandle = QpdfNative.GetPageHandle(job, pageNumber);

                    if (pageHandle == 0)
                    {
                        return CreateError(
                            "PDF_ROTATION_FAILED",
                            $"Failed to get page handle for page {pageNumber}.",
                            ErrorCategory.System,
                            correlationId,
                            ("PageNumber", pageNumber));
                    }

                    var rotateResult = QpdfNative.RotatePage(job, pageHandle, rotationDegrees, relative: true);
                    if (rotateResult != QpdfNative.ErrorCodes.Success)
                    {
                        return HandleQpdfError(job, rotateResult, document.FilePath, correlationId, $"rotate page {pageNumber}");
                    }

                    _logger.LogDebug(
                        "Rotated page {PageNumber} by {Degrees} degrees. CorrelationId={CorrelationId}",
                        pageNumber, rotationDegrees, correlationId);
                }

                // Write back to the same file
                var writeResult = QpdfNative.WriteDocument(job, document.FilePath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, writeResult, document.FilePath, correlationId, "write rotated document");
                }

                _logger.LogInformation(
                    "Page rotation completed successfully. CorrelationId={CorrelationId}, PageCount={PageCount}",
                    correlationId, pageIndices.Length);

                return Result.Ok();
            }
            catch (OperationCanceledException)
            {
                return CreateCancellationError(correlationId);
            }
            catch (Exception ex)
            {
                return CreateError(
                    "PDF_ROTATION_FAILED",
                    $"Unexpected error during rotation: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name));
            }
            finally
            {
                job?.Dispose();
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result> DeletePagesAsync(
        PdfDocument document,
        int[] pageIndices,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting page deletion. CorrelationId={CorrelationId}, PageCount={PageCount}",
            correlationId, pageIndices?.Length ?? 0);

        // Validate inputs
        var validationResult = ValidateDocument(document, correlationId);
        if (validationResult.IsFailed)
        {
            return validationResult;
        }

        if (pageIndices == null || pageIndices.Length == 0)
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "No pages specified for deletion.",
                ErrorCategory.Validation,
                correlationId);
        }

        return await Task.Run(() =>
        {
            SafeQpdfJobHandle? job = null;

            try
            {
                // Load document
                job = QpdfNative.CreateJob();
                if (job.IsInvalid)
                {
                    return CreateError(
                        "PDF_DELETE_FAILED",
                        "Failed to create QPDF job for delete operation.",
                        ErrorCategory.System,
                        correlationId);
                }

                var readResult = QpdfNative.ReadDocument(job, document.FilePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, readResult, document.FilePath, correlationId, "read document for deletion");
                }

                var totalPages = QpdfNative.GetPageCount(job);

                // Cannot delete all pages
                if (pageIndices.Length >= totalPages)
                {
                    return CreateError(
                        "PDF_DELETE_ALL_PAGES",
                        "Cannot delete all pages from the document. At least one page must remain.",
                        ErrorCategory.Validation,
                        correlationId,
                        ("PagesToDelete", pageIndices.Length),
                        ("TotalPages", totalPages));
                }

                // Validate page indices
                foreach (var pageIndex in pageIndices)
                {
                    if (pageIndex < 0 || pageIndex >= totalPages)
                    {
                        return CreateError(
                            "PDF_PAGE_INVALID",
                            $"Page index {pageIndex} is out of range. Document has {totalPages} pages.",
                            ErrorCategory.Validation,
                            correlationId,
                            ("PageIndex", pageIndex),
                            ("TotalPages", totalPages));
                    }
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                // Build page range for deletion (convert 0-based to 1-based)
                var sortedIndices = pageIndices.OrderBy(i => i).ToArray();
                var pageRange = BuildPageRangeString(sortedIndices.Select(i => i + 1).ToArray());

                _logger.LogDebug(
                    "Deleting pages with range: {PageRange}. CorrelationId={CorrelationId}",
                    pageRange, correlationId);

                var removeResult = QpdfNative.RemovePages(job, pageRange);
                if (removeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, removeResult, document.FilePath, correlationId, "delete pages");
                }

                // Write back to the same file
                var writeResult = QpdfNative.WriteDocument(job, document.FilePath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, writeResult, document.FilePath, correlationId, "write document after deletion");
                }

                var remainingPages = totalPages - pageIndices.Length;

                _logger.LogInformation(
                    "Page deletion completed successfully. CorrelationId={CorrelationId}, DeletedPages={DeletedPages}, RemainingPages={RemainingPages}",
                    correlationId, pageIndices.Length, remainingPages);

                return Result.Ok();
            }
            catch (OperationCanceledException)
            {
                return CreateCancellationError(correlationId);
            }
            catch (Exception ex)
            {
                return CreateError(
                    "PDF_DELETE_FAILED",
                    $"Unexpected error during deletion: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name));
            }
            finally
            {
                job?.Dispose();
            }
        }, ct);
    }

    #region Helper Methods

    private Result ValidateDocument(PdfDocument document, Guid correlationId)
    {
        if (document == null)
        {
            return CreateError(
                "PDF_INVALID_DOCUMENT",
                "Document cannot be null.",
                ErrorCategory.Validation,
                correlationId);
        }

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            return CreateError(
                "PDF_INVALID_DOCUMENT",
                "Document file path is null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        if (!File.Exists(document.FilePath))
        {
            return CreateError(
                "PDF_FILE_NOT_FOUND",
                $"Document file not found: {document.FilePath}",
                ErrorCategory.IO,
                correlationId,
                ("FilePath", document.FilePath));
        }

        return Result.Ok();
    }

    private Result HandleQpdfError(
        SafeQpdfJobHandle job,
        int errorCode,
        string filePath,
        Guid correlationId,
        string operation)
    {
        var qpdfErrorMessage = QpdfNative.GetErrorMessage(job);
        var errorCodeDescription = QpdfNative.TranslateErrorCode(errorCode);

        var (code, category) = errorCode switch
        {
            QpdfNative.ErrorCodes.FileNotFound => ("PDF_FILE_NOT_FOUND", ErrorCategory.IO),
            QpdfNative.ErrorCodes.InvalidPassword => ("PDF_REQUIRES_PASSWORD", ErrorCategory.Security),
            QpdfNative.ErrorCodes.DamagedPdf => ("PDF_CORRUPTED", ErrorCategory.Validation),
            QpdfNative.ErrorCodes.OutOfMemory => ("PDF_OUT_OF_MEMORY", ErrorCategory.System),
            _ => ("PDF_OPERATION_FAILED", ErrorCategory.System)
        };

        var message = string.IsNullOrEmpty(qpdfErrorMessage)
            ? $"Failed to {operation}: {errorCodeDescription}"
            : $"Failed to {operation}: {qpdfErrorMessage}";

        _logger.LogError(
            "QPDF operation failed. CorrelationId={CorrelationId}, Operation={Operation}, ErrorCode={ErrorCode}",
            correlationId, operation, errorCode);

        return CreateError(
            code,
            message,
            category,
            correlationId,
            ("FilePath", filePath),
            ("QpdfErrorCode", errorCode),
            ("Operation", operation));
    }

    private Result CreateError(
        string errorCode,
        string message,
        ErrorCategory category,
        Guid correlationId,
        params (string Key, object Value)[] contexts)
    {
        var error = new PdfError(errorCode, message, category, ErrorSeverity.Error)
            .WithContext("CorrelationId", correlationId);

        foreach (var (key, value) in contexts)
        {
            error.WithContext(key, value);
        }

        _logger.LogError(
            "Page operation failed. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}, Message={Message}",
            correlationId, errorCode, message);

        return Result.Fail(error);
    }

    private Result CreateCancellationError(Guid correlationId)
    {
        _logger.LogWarning(
            "Page operation cancelled. CorrelationId={CorrelationId}",
            correlationId);

        return CreateError(
            "PDF_OPERATION_CANCELLED",
            "Page operation was cancelled by user request.",
            ErrorCategory.Validation,
            correlationId);
    }

    private static string BuildPageRangeString(int[] pageNumbers)
    {
        // Build compact page range string (e.g., "1,3,5-7,9")
        if (pageNumbers.Length == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        var rangeStart = pageNumbers[0];
        var rangeEnd = pageNumbers[0];

        for (int i = 1; i < pageNumbers.Length; i++)
        {
            if (pageNumbers[i] == rangeEnd + 1)
            {
                rangeEnd = pageNumbers[i];
            }
            else
            {
                parts.Add(rangeStart == rangeEnd
                    ? rangeStart.ToString()
                    : $"{rangeStart}-{rangeEnd}");

                rangeStart = pageNumbers[i];
                rangeEnd = pageNumbers[i];
            }
        }

        parts.Add(rangeStart == rangeEnd
            ? rangeStart.ToString()
            : $"{rangeStart}-{rangeEnd}");

        return string.Join(",", parts);
    }

    #endregion
}
