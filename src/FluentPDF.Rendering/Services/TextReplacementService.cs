using System.Diagnostics;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for finding and replacing text in PDF documents.
/// Uses text annotation overlays to replace text (PDFium limitation - not true content editing).
/// Provides preview and undo support through annotation management.
/// </summary>
public sealed class TextReplacementService : ITextReplacementService
{
    private readonly ITextSearchService _searchService;
    private readonly IAnnotationService _annotationService;
    private readonly ILogger<TextReplacementService> _logger;
    private const int SlowReplaceThresholdMs = 2000;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReplacementService"/> class.
    /// </summary>
    /// <param name="searchService">Service for finding text matches.</param>
    /// <param name="annotationService">Service for creating replacement annotations.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public TextReplacementService(
        ITextSearchService searchService,
        IAnnotationService annotationService,
        ILogger<TextReplacementService> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<TextReplacement>> ReplaceAsync(
        PdfDocument document,
        SearchMatch match,
        string replacementText,
        bool preview = false)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrEmpty(replacementText))
        {
            throw new ArgumentException("Replacement text cannot be null or empty.", nameof(replacementText));
        }

        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Replacing text match. CorrelationId={CorrelationId}, FilePath={FilePath}, PageNumber={PageNumber}, OriginalText={OriginalText}, ReplacementText={ReplacementText}, Preview={Preview}",
            correlationId, document.FilePath, match.PageNumber, match.Text, replacementText, preview);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate match
            if (!match.IsValid())
            {
                var error = new PdfError(
                    "PDF_REPLACE_FAILED",
                    "Invalid search match provided for replacement.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("FilePath", document.FilePath);

                return Result.Fail(error);
            }

            // Validate page number
            if (match.PageNumber < 1 || match.PageNumber > document.PageCount)
            {
                var error = new PdfError(
                    "PDF_PAGE_INVALID",
                    $"Page number {match.PageNumber} is out of range. Valid range: 1-{document.PageCount}",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("PageNumber", match.PageNumber)
                    .WithContext("TotalPages", document.PageCount)
                    .WithContext("FilePath", document.FilePath)
                    .WithContext("CorrelationId", correlationId);

                return Result.Fail(error);
            }

            if (preview)
            {
                // Preview mode - just create the replacement record without modifying document
                var previewReplacement = new TextReplacement(
                    Match: match,
                    ReplacementText: replacementText,
                    PageNumber: match.PageNumber - 1,
                    AnnotationIndex: -1, // Not created in preview mode
                    Timestamp: DateTime.UtcNow);

                stopwatch.Stop();

                _logger.LogDebug(
                    "Text replacement preview created. CorrelationId={CorrelationId}, PageNumber={PageNumber}, ElapsedMs={ElapsedMs}",
                    correlationId, match.PageNumber, stopwatch.ElapsedMilliseconds);

                return Result.Ok(previewReplacement);
            }

            // Create a white rectangle annotation to cover the original text
            var coverAnnotation = new Annotation
            {
                PageNumber = match.PageNumber - 1,
                Type = AnnotationType.Square,
                Bounds = match.BoundingBox,
                FillColor = System.Drawing.Color.White, // White to cover original text
                StrokeColor = System.Drawing.Color.White,
                Contents = string.Empty,
                Opacity = 1.0,
                StrokeWidth = 0.0 // No border
            };

            var coverResult = await _annotationService.CreateAnnotationAsync(document, coverAnnotation);

            if (coverResult.IsFailed)
            {
                _logger.LogError(
                    "Failed to create cover annotation. CorrelationId={CorrelationId}, Error={Error}",
                    correlationId, coverResult.Errors.FirstOrDefault()?.Message);

                return Result.Fail(coverResult.Errors);
            }

            // Create a text annotation with the replacement text
            var textAnnotation = new Annotation
            {
                PageNumber = match.PageNumber - 1,
                Type = AnnotationType.FreeText,
                Bounds = match.BoundingBox,
                FillColor = System.Drawing.Color.Transparent,
                StrokeColor = System.Drawing.Color.Black, // Black text
                Contents = replacementText,
                Opacity = 1.0,
                StrokeWidth = 0.0 // No border
            };

            var textResult = await _annotationService.CreateAnnotationAsync(document, textAnnotation);

            if (textResult.IsFailed)
            {
                _logger.LogError(
                    "Failed to create text annotation. CorrelationId={CorrelationId}, Error={Error}",
                    correlationId, textResult.Errors.FirstOrDefault()?.Message);

                return Result.Fail(textResult.Errors);
            }

            // Get the annotation index from the created annotation
            var annotationsResult = await _annotationService.GetAnnotationsAsync(document, match.PageNumber - 1);

            int annotationIndex = annotationsResult.IsSuccess
                ? annotationsResult.Value.Count - 1
                : 0;

            var replacement = new TextReplacement(
                Match: match,
                ReplacementText: replacementText,
                PageNumber: match.PageNumber - 1,
                AnnotationIndex: annotationIndex,
                Timestamp: DateTime.UtcNow);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowReplaceThresholdMs)
            {
                _logger.LogWarning(
                    "Slow text replacement detected. CorrelationId={CorrelationId}, PageNumber={PageNumber}, ElapsedMs={ElapsedMs}",
                    correlationId, match.PageNumber, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Text replacement completed successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, ElapsedMs={ElapsedMs}",
                    correlationId, match.PageNumber, stopwatch.ElapsedMilliseconds);
            }

            return Result.Ok(replacement);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var error = new PdfError(
                "PDF_REPLACE_FAILED",
                $"Failed to replace text: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("PageNumber", match.PageNumber)
                .WithContext("FilePath", document.FilePath)
                .WithContext("OriginalText", match.Text)
                .WithContext("ReplacementText", replacementText)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to replace text. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                correlationId, match.PageNumber);

            return Result.Fail(error);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<TextReplacement>>> ReplaceAllAsync(
        PdfDocument document,
        string findText,
        string replaceText,
        SearchOptions? options = null,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrEmpty(findText))
        {
            throw new ArgumentException("Find text cannot be null or empty.", nameof(findText));
        }

        if (string.IsNullOrEmpty(replaceText))
        {
            throw new ArgumentException("Replace text cannot be null or empty.", nameof(replaceText));
        }

        options ??= SearchOptions.Default;
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Replacing all text matches. CorrelationId={CorrelationId}, FilePath={FilePath}, FindText={FindText}, ReplaceText={ReplaceText}, Preview={Preview}, CaseSensitive={CaseSensitive}, WholeWord={WholeWord}",
            correlationId, document.FilePath, findText, replaceText, preview, options.CaseSensitive, options.WholeWord);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // First, find all matches
            var searchResult = await _searchService.SearchAsync(document, findText, options, cancellationToken);

            if (searchResult.IsFailed)
            {
                _logger.LogError(
                    "Failed to search for text. CorrelationId={CorrelationId}, Error={Error}",
                    correlationId, searchResult.Errors.FirstOrDefault()?.Message);

                return Result.Fail(searchResult.Errors);
            }

            var matches = searchResult.Value;

            _logger.LogInformation(
                "Found {MatchCount} matches to replace. CorrelationId={CorrelationId}",
                matches.Count, correlationId);

            var replacements = new List<TextReplacement>();

            // Replace each match
            foreach (var match in matches)
            {
                // Check cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Replace all cancelled. CorrelationId={CorrelationId}, CompletedReplacements={CompletedReplacements}/{TotalMatches}",
                        correlationId, replacements.Count, matches.Count);

                    var error = new PdfError(
                        "PDF_SEARCH_CANCELLED",
                        "Replace all operation was cancelled.",
                        ErrorCategory.System,
                        ErrorSeverity.Info)
                        .WithContext("FilePath", document.FilePath)
                        .WithContext("CorrelationId", correlationId)
                        .WithContext("FindText", findText)
                        .WithContext("ReplaceText", replaceText)
                        .WithContext("CompletedReplacements", replacements.Count)
                        .WithContext("TotalMatches", matches.Count);

                    return Result.Fail(error);
                }

                var replaceResult = await ReplaceAsync(document, match, replaceText, preview);

                if (replaceResult.IsSuccess)
                {
                    replacements.Add(replaceResult.Value);
                }
                else
                {
                    // Log warning but continue with other replacements
                    _logger.LogWarning(
                        "Failed to replace match. CorrelationId={CorrelationId}, PageNumber={PageNumber}, Error={Error}",
                        correlationId, match.PageNumber, replaceResult.Errors.FirstOrDefault()?.Message);
                }
            }

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowReplaceThresholdMs)
            {
                _logger.LogWarning(
                    "Slow replace all detected. CorrelationId={CorrelationId}, TotalMatches={TotalMatches}, CompletedReplacements={CompletedReplacements}, ElapsedMs={ElapsedMs}",
                    correlationId, matches.Count, replacements.Count, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Replace all completed successfully. CorrelationId={CorrelationId}, TotalMatches={TotalMatches}, CompletedReplacements={CompletedReplacements}, ElapsedMs={ElapsedMs}",
                    correlationId, matches.Count, replacements.Count, stopwatch.ElapsedMilliseconds);
            }

            return Result.Ok(replacements);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var error = new PdfError(
                "PDF_REPLACE_FAILED",
                $"Failed to replace all text: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("FilePath", document.FilePath)
                .WithContext("FindText", findText)
                .WithContext("ReplaceText", replaceText)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to replace all text. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UndoReplacementsAsync(
        PdfDocument document,
        List<TextReplacement> replacements)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (replacements == null || replacements.Count == 0)
        {
            throw new ArgumentException("Replacements list cannot be null or empty.", nameof(replacements));
        }

        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Undoing text replacements. CorrelationId={CorrelationId}, FilePath={FilePath}, ReplacementCount={ReplacementCount}",
            correlationId, document.FilePath, replacements.Count);

        var stopwatch = Stopwatch.StartNew();
        var errors = new List<FluentResults.IError>();

        try
        {
            // Sort replacements by page and annotation index in descending order
            // This ensures we delete from highest index to lowest to avoid index shifting
            var sortedReplacements = replacements
                .OrderByDescending(r => r.PageNumber)
                .ThenByDescending(r => r.AnnotationIndex)
                .ToList();

            foreach (var replacement in sortedReplacements)
            {
                if (replacement.AnnotationIndex < 0)
                {
                    // Skip preview replacements (not actually created)
                    continue;
                }

                // Delete the text annotation and cover annotation (2 annotations per replacement)
                var deleteResult1 = await _annotationService.DeleteAnnotationAsync(
                    document,
                    replacement.PageNumber,
                    replacement.AnnotationIndex);

                if (deleteResult1.IsFailed)
                {
                    errors.AddRange(deleteResult1.Errors);
                    _logger.LogWarning(
                        "Failed to delete annotation. CorrelationId={CorrelationId}, PageNumber={PageNumber}, AnnotationIndex={AnnotationIndex}",
                        correlationId, replacement.PageNumber, replacement.AnnotationIndex);
                }

                // Delete the cover annotation (created before the text annotation)
                if (replacement.AnnotationIndex > 0)
                {
                    var deleteResult2 = await _annotationService.DeleteAnnotationAsync(
                        document,
                        replacement.PageNumber,
                        replacement.AnnotationIndex - 1);

                    if (deleteResult2.IsFailed)
                    {
                        errors.AddRange(deleteResult2.Errors);
                    }
                }
            }

            stopwatch.Stop();

            if (errors.Count > 0)
            {
                _logger.LogWarning(
                    "Undo replacements completed with errors. CorrelationId={CorrelationId}, ErrorCount={ErrorCount}, ElapsedMs={ElapsedMs}",
                    correlationId, errors.Count, stopwatch.ElapsedMilliseconds);

                return Result.Fail(errors);
            }

            _logger.LogInformation(
                "Undo replacements completed successfully. CorrelationId={CorrelationId}, ReplacementCount={ReplacementCount}, ElapsedMs={ElapsedMs}",
                correlationId, replacements.Count, stopwatch.ElapsedMilliseconds);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var error = new PdfError(
                "PDF_UNDO_FAILED",
                $"Failed to undo replacements: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("FilePath", document.FilePath)
                .WithContext("ReplacementCount", replacements.Count)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to undo replacements. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }
    }
}
