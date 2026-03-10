using System.Diagnostics;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for searching text within PDF documents using PDFium.
/// Implements text search with match location tracking and performance monitoring.
/// </summary>
public sealed class TextSearchService : ITextSearchService
{
    private readonly ILogger<TextSearchService> _logger;
    private const int SlowSearchThresholdMs = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextSearchService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public TextSearchService(ILogger<TextSearchService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<List<SearchMatch>>> SearchAsync(
        PdfDocument document,
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Search query cannot be null or empty.", nameof(query));
        }

        options ??= SearchOptions.Default;
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Searching text in document. CorrelationId={CorrelationId}, FilePath={FilePath}, Query={Query}, PageCount={PageCount}, CaseSensitive={CaseSensitive}, WholeWord={WholeWord}",
            correlationId, document.FilePath, query, document.PageCount, options.CaseSensitive, options.WholeWord);

        var stopwatch = Stopwatch.StartNew();
        var allMatches = new List<SearchMatch>();

        for (int pageNumber = 1; pageNumber <= document.PageCount; pageNumber++)
        {
            // Check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Search cancelled. CorrelationId={CorrelationId}, CompletedPages={CompletedPages}/{TotalPages}, MatchesFound={MatchesFound}",
                    correlationId, pageNumber - 1, document.PageCount, allMatches.Count);

                var error = new PdfError(
                    "PDF_SEARCH_CANCELLED",
                    "Search operation was cancelled.",
                    ErrorCategory.System,
                    ErrorSeverity.Info)
                    .WithContext("FilePath", document.FilePath)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("Query", query)
                    .WithContext("CompletedPages", pageNumber - 1)
                    .WithContext("TotalPages", document.PageCount)
                    .WithContext("MatchesFound", allMatches.Count);

                return Result.Fail(error);
            }

            var pageResult = await SearchPageAsync(document, pageNumber, query, options);

            if (pageResult.IsSuccess)
            {
                allMatches.AddRange(pageResult.Value);
            }
            else
            {
                // Log warning but continue searching other pages
                _logger.LogWarning(
                    "Failed to search page. CorrelationId={CorrelationId}, PageNumber={PageNumber}, Error={Error}",
                    correlationId, pageNumber, pageResult.Errors.FirstOrDefault()?.Message);
            }
        }

        stopwatch.Stop();

        // Log performance warning for slow searches
        if (stopwatch.ElapsedMilliseconds > SlowSearchThresholdMs)
        {
            _logger.LogWarning(
                "Slow search detected. CorrelationId={CorrelationId}, Query={Query}, PageCount={PageCount}, MatchesFound={MatchesFound}, ElapsedMs={ElapsedMs}",
                correlationId, query, document.PageCount, allMatches.Count, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "Search completed successfully. CorrelationId={CorrelationId}, Query={Query}, PageCount={PageCount}, MatchesFound={MatchesFound}, ElapsedMs={ElapsedMs}",
                correlationId, query, document.PageCount, allMatches.Count, stopwatch.ElapsedMilliseconds);
        }

        return Result.Ok(allMatches);
    }

    /// <inheritdoc />
    public async Task<Result<List<SearchMatch>>> SearchPageAsync(
        PdfDocument document,
        int pageNumber,
        string query,
        SearchOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Search query cannot be null or empty.", nameof(query));
        }

        options ??= SearchOptions.Default;
        var correlationId = Guid.NewGuid();

        _logger.LogDebug(
            "Searching text on page. CorrelationId={CorrelationId}, FilePath={FilePath}, PageNumber={PageNumber}, Query={Query}",
            correlationId, document.FilePath, pageNumber, query);

        if (pageNumber < 1 || pageNumber > document.PageCount)
        {
            return FailPageOutOfRange(pageNumber, document, correlationId);
        }

        return await Task.Run(() => ExecutePageSearch(
            (SafePdfDocumentHandle)document.Handle,
            pageNumber, query, options, document.FilePath, correlationId));
    }

    /// <summary>
    /// Core search logic: loads page handles, finds matches, and logs performance.
    /// Runs on a background thread via Task.Run.
    /// </summary>
    private Result<List<SearchMatch>> ExecutePageSearch(
        SafePdfDocumentHandle docHandle,
        int pageNumber,
        string query,
        SearchOptions options,
        string filePath,
        Guid correlationId)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var pageHandle = PdfiumInterop.LoadPage(docHandle, pageNumber - 1);
            if (pageHandle.IsInvalid)
            {
                return FailPageLoad("Failed to load page {0} for text search.", pageNumber, filePath, correlationId);
            }

            using var textPageHandle = PdfiumInterop.LoadTextPage(pageHandle);
            if (textPageHandle.IsInvalid)
            {
                return FailPageLoad("Failed to load text information for page {0}.", pageNumber, filePath, correlationId);
            }

            var matches = FindMatchesOnPage(textPageHandle, pageNumber, query, options);

            stopwatch.Stop();
            LogSearchTiming(correlationId, pageNumber, query, matches.Count, stopwatch.ElapsedMilliseconds);

            return Result.Ok(matches);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return FailSearchException(ex, pageNumber, filePath, query, correlationId);
        }
    }

    /// <summary>
    /// Iterates PDFium search results on a single text page and builds match objects.
    /// </summary>
    private static List<SearchMatch> FindMatchesOnPage(
        SafePdfTextPageHandle textPageHandle,
        int pageNumber,
        string query,
        SearchOptions options)
    {
        var matches = new List<SearchMatch>();
        var searchFlags = ConvertSearchOptions(options);
        var searchHandle = PdfiumInterop.StartTextSearch(textPageHandle, query, searchFlags);

        if (searchHandle == IntPtr.Zero)
        {
            return matches;
        }

        try
        {
            while (PdfiumInterop.FindNext(searchHandle))
            {
                var charIndex = PdfiumInterop.GetSearchResultIndex(searchHandle);
                var matchLength = PdfiumInterop.GetSearchResultCount(searchHandle);

                if (charIndex >= 0 && matchLength > 0)
                {
                    var matchedText = PdfiumInterop.GetText(textPageHandle, charIndex, matchLength);
                    var boundingBox = CalculateBoundingBox(textPageHandle, charIndex, matchLength);

                    matches.Add(new SearchMatch(
                        PageNumber: pageNumber,
                        CharIndex: charIndex,
                        Length: matchLength,
                        Text: matchedText,
                        BoundingBox: boundingBox));
                }
            }
        }
        finally
        {
            PdfiumInterop.CloseSearch(searchHandle);
        }

        return matches;
    }

    private Result<List<SearchMatch>> FailPageOutOfRange(
        int pageNumber, PdfDocument document, Guid correlationId)
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

    private Result<List<SearchMatch>> FailPageLoad(
        string messageTemplate, int pageNumber, string filePath, Guid correlationId)
    {
        var error = new PdfError(
            "PDF_TEXT_PAGE_LOAD_FAILED",
            string.Format(messageTemplate, pageNumber),
            ErrorCategory.Rendering,
            ErrorSeverity.Error)
            .WithContext("PageNumber", pageNumber)
            .WithContext("FilePath", filePath)
            .WithContext("CorrelationId", correlationId);

        _logger.LogError(
            "Failed to load page for text search. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
            correlationId, pageNumber);

        return Result.Fail(error);
    }

    private Result<List<SearchMatch>> FailSearchException(
        Exception ex, int pageNumber, string filePath, string query, Guid correlationId)
    {
        var error = new PdfError(
            "PDF_SEARCH_FAILED",
            $"Failed to search text on page {pageNumber}: {ex.Message}",
            ErrorCategory.System,
            ErrorSeverity.Error)
            .WithContext("PageNumber", pageNumber)
            .WithContext("FilePath", filePath)
            .WithContext("Query", query)
            .WithContext("CorrelationId", correlationId)
            .WithContext("ExceptionType", ex.GetType().Name);

        _logger.LogError(ex,
            "Failed to search text. CorrelationId={CorrelationId}, PageNumber={PageNumber}, Query={Query}",
            correlationId, pageNumber, query);

        return Result.Fail(error);
    }

    private void LogSearchTiming(
        Guid correlationId, int pageNumber, string query, int matchCount, long elapsedMs)
    {
        if (elapsedMs > SlowSearchThresholdMs)
        {
            _logger.LogWarning(
                "Slow search detected. CorrelationId={CorrelationId}, PageNumber={PageNumber}, Query={Query}, MatchesFound={MatchesFound}, ElapsedMs={ElapsedMs}",
                correlationId, pageNumber, query, matchCount, elapsedMs);
        }
        else
        {
            _logger.LogDebug(
                "Search completed successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, Query={Query}, MatchesFound={MatchesFound}, ElapsedMs={ElapsedMs}",
                correlationId, pageNumber, query, matchCount, elapsedMs);
        }
    }

    /// <summary>
    /// Converts SearchOptions to PDFium SearchFlags.
    /// </summary>
    private static PdfiumInterop.SearchFlags ConvertSearchOptions(SearchOptions options)
    {
        var flags = PdfiumInterop.SearchFlags.None;

        if (options.CaseSensitive)
        {
            flags |= PdfiumInterop.SearchFlags.MatchCase;
        }

        if (options.WholeWord)
        {
            flags |= PdfiumInterop.SearchFlags.MatchWholeWord;
        }

        return flags;
    }

    /// <summary>
    /// Calculates the bounding box for a text match by combining character boxes.
    /// Handles multi-line matches by encompassing all character positions.
    /// </summary>
    private static PdfRectangle CalculateBoundingBox(SafePdfTextPageHandle textPageHandle, int charIndex, int length)
    {
        double minLeft = double.MaxValue;
        double minBottom = double.MaxValue;
        double maxRight = double.MinValue;
        double maxTop = double.MinValue;

        // Iterate through all characters in the match
        for (int i = 0; i < length; i++)
        {
            PdfiumInterop.GetCharBox(
                textPageHandle,
                charIndex + i,
                out double left,
                out double top,
                out double right,
                out double bottom);

            // Update bounding box bounds
            minLeft = Math.Min(minLeft, left);
            minBottom = Math.Min(minBottom, bottom);
            maxRight = Math.Max(maxRight, right);
            maxTop = Math.Max(maxTop, top);
        }

        return new PdfRectangle(
            Left: minLeft,
            Bottom: minBottom,
            Right: maxRight,
            Top: maxTop);
    }
}
