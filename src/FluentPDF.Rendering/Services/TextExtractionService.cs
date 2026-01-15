using System.Collections.Concurrent;
using System.Diagnostics;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for extracting text from PDF documents using PDFium.
/// Implements text extraction with caching and performance monitoring.
/// </summary>
public sealed class TextExtractionService : ITextExtractionService
{
    private readonly ILogger<TextExtractionService> _logger;
    private readonly ConcurrentDictionary<string, string> _textCache;
    private const int SlowExtractionThresholdMs = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextExtractionService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public TextExtractionService(ILogger<TextExtractionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textCache = new ConcurrentDictionary<string, string>();
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExtractTextAsync(PdfDocument document, int pageNumber)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogDebug(
            "Extracting text from page. CorrelationId={CorrelationId}, FilePath={FilePath}, PageNumber={PageNumber}",
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

        // Check cache
        var cacheKey = GetCacheKey(document.FilePath, pageNumber);
        if (_textCache.TryGetValue(cacheKey, out var cachedText))
        {
            _logger.LogDebug(
                "Text retrieved from cache. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                correlationId, pageNumber);
            return Result.Ok(cachedText);
        }

        // Extract text on background thread
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Load page (0-based index)
                using var pageHandle = PdfiumInterop.LoadPage(
                    (SafePdfDocumentHandle)document.Handle,
                    pageNumber - 1);

                if (pageHandle.IsInvalid)
                {
                    var error = new PdfError(
                        "PDF_TEXT_PAGE_LOAD_FAILED",
                        $"Failed to load page {pageNumber} for text extraction.",
                        ErrorCategory.Rendering,
                        ErrorSeverity.Error)
                        .WithContext("PageNumber", pageNumber)
                        .WithContext("FilePath", document.FilePath)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError(
                        "Failed to load page for text extraction. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                        correlationId, pageNumber);

                    return Result.Fail(error);
                }

                // Load text page
                using var textPageHandle = PdfiumInterop.LoadTextPage(pageHandle);

                if (textPageHandle.IsInvalid)
                {
                    var error = new PdfError(
                        "PDF_TEXT_PAGE_LOAD_FAILED",
                        $"Failed to load text information for page {pageNumber}.",
                        ErrorCategory.Rendering,
                        ErrorSeverity.Error)
                        .WithContext("PageNumber", pageNumber)
                        .WithContext("FilePath", document.FilePath)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError(
                        "Failed to load text page. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                        correlationId, pageNumber);

                    return Result.Fail(error);
                }

                // Get character count
                var charCount = PdfiumInterop.GetTextCharCount(textPageHandle);
                if (charCount == 0)
                {
                    // Empty page is valid, cache and return empty string
                    _textCache.TryAdd(cacheKey, string.Empty);

                    stopwatch.Stop();
                    _logger.LogDebug(
                        "Text extracted (empty page). CorrelationId={CorrelationId}, PageNumber={PageNumber}, ElapsedMs={ElapsedMs}",
                        correlationId, pageNumber, stopwatch.ElapsedMilliseconds);

                    return Result.Ok(string.Empty);
                }

                // Extract text
                var text = PdfiumInterop.GetText(textPageHandle, 0, charCount);

                // Cache the result
                _textCache.TryAdd(cacheKey, text);

                stopwatch.Stop();

                // Log performance warning for slow extractions
                if (stopwatch.ElapsedMilliseconds > SlowExtractionThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow text extraction detected. CorrelationId={CorrelationId}, PageNumber={PageNumber}, CharCount={CharCount}, ElapsedMs={ElapsedMs}",
                        correlationId, pageNumber, charCount, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogDebug(
                        "Text extracted successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, CharCount={CharCount}, ElapsedMs={ElapsedMs}",
                        correlationId, pageNumber, charCount, stopwatch.ElapsedMilliseconds);
                }

                return Result.Ok(text);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var error = new PdfError(
                    "PDF_TEXT_EXTRACTION_FAILED",
                    $"Failed to extract text from page {pageNumber}: {ex.Message}",
                    ErrorCategory.System,
                    ErrorSeverity.Error)
                    .WithContext("PageNumber", pageNumber)
                    .WithContext("FilePath", document.FilePath)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogError(ex,
                    "Failed to extract text. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                    correlationId, pageNumber);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<Dictionary<int, string>>> ExtractAllTextAsync(
        PdfDocument document,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Extracting text from all pages. CorrelationId={CorrelationId}, FilePath={FilePath}, PageCount={PageCount}",
            correlationId, document.FilePath, document.PageCount);

        var stopwatch = Stopwatch.StartNew();
        var results = new Dictionary<int, string>();
        var failedPages = new List<int>();

        for (int pageNumber = 1; pageNumber <= document.PageCount; pageNumber++)
        {
            // Check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Text extraction cancelled. CorrelationId={CorrelationId}, CompletedPages={CompletedPages}/{TotalPages}",
                    correlationId, pageNumber - 1, document.PageCount);

                var error = new PdfError(
                    "PDF_TEXT_EXTRACTION_CANCELLED",
                    "Text extraction was cancelled.",
                    ErrorCategory.System,
                    ErrorSeverity.Info)
                    .WithContext("FilePath", document.FilePath)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("CompletedPages", pageNumber - 1)
                    .WithContext("TotalPages", document.PageCount);

                return Result.Fail(error);
            }

            var result = await ExtractTextAsync(document, pageNumber);

            if (result.IsSuccess)
            {
                results[pageNumber] = result.Value;
            }
            else
            {
                failedPages.Add(pageNumber);
                _logger.LogWarning(
                    "Failed to extract text from page. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                    correlationId, pageNumber);
            }
        }

        stopwatch.Stop();

        if (failedPages.Count > 0)
        {
            var error = new PdfError(
                "PDF_TEXT_EXTRACTION_PARTIAL_FAILURE",
                $"Failed to extract text from {failedPages.Count} page(s): {string.Join(", ", failedPages)}",
                ErrorCategory.System,
                ErrorSeverity.Warning)
                .WithContext("FilePath", document.FilePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("FailedPages", failedPages)
                .WithContext("SuccessfulPages", results.Count)
                .WithContext("TotalPages", document.PageCount);

            _logger.LogWarning(
                "Text extraction completed with failures. CorrelationId={CorrelationId}, SuccessfulPages={SuccessfulPages}, FailedPages={FailedPages}, TotalPages={TotalPages}, ElapsedMs={ElapsedMs}",
                correlationId, results.Count, failedPages.Count, document.PageCount, stopwatch.ElapsedMilliseconds);

            return Result.Fail(error);
        }

        // Log performance warning for slow extractions
        if (document.PageCount > 0)
        {
            var avgMsPerPage = stopwatch.ElapsedMilliseconds / document.PageCount;

            if (stopwatch.ElapsedMilliseconds > SlowExtractionThresholdMs * document.PageCount)
            {
                _logger.LogWarning(
                    "Slow text extraction detected for all pages. CorrelationId={CorrelationId}, PageCount={PageCount}, ElapsedMs={ElapsedMs}, AvgMsPerPage={AvgMsPerPage}",
                    correlationId, document.PageCount, stopwatch.ElapsedMilliseconds, avgMsPerPage);
            }
            else
            {
                _logger.LogInformation(
                    "Text extracted from all pages successfully. CorrelationId={CorrelationId}, PageCount={PageCount}, ElapsedMs={ElapsedMs}, AvgMsPerPage={AvgMsPerPage}",
                    correlationId, document.PageCount, stopwatch.ElapsedMilliseconds, avgMsPerPage);
            }
        }
        else
        {
            _logger.LogInformation(
                "Text extraction completed (no pages). CorrelationId={CorrelationId}, ElapsedMs={ElapsedMs}",
                correlationId, stopwatch.ElapsedMilliseconds);
        }

        return Result.Ok(results);
    }

    private static string GetCacheKey(string filePath, int pageNumber)
    {
        return $"{filePath}|{pageNumber}";
    }
}
