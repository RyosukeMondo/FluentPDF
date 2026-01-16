using System.Diagnostics;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.UI.Xaml.Media;

namespace FluentPDF.App.Services;

/// <summary>
/// Orchestrates PDF rendering with automatic fallback between strategies.
/// Integrates with observability service for comprehensive logging and diagnostics.
/// </summary>
public sealed class RenderingCoordinator
{
    private readonly RenderingStrategyFactory _strategyFactory;
    private readonly RenderingObservabilityService _observabilityService;
    private readonly PdfRenderingService _pdfRenderingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingCoordinator"/> class.
    /// </summary>
    /// <param name="strategyFactory">Factory for retrieving ordered rendering strategies.</param>
    /// <param name="observabilityService">Service for logging and diagnostics.</param>
    /// <param name="pdfRenderingService">Service for rendering PDF pages to PNG streams.</param>
    public RenderingCoordinator(
        RenderingStrategyFactory strategyFactory,
        RenderingObservabilityService observabilityService,
        PdfRenderingService pdfRenderingService)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _observabilityService = observabilityService ?? throw new ArgumentNullException(nameof(observabilityService));
        _pdfRenderingService = pdfRenderingService ?? throw new ArgumentNullException(nameof(pdfRenderingService));
    }

    /// <summary>
    /// Renders a PDF page using fallback strategy pattern.
    /// Tries each registered strategy in priority order until one succeeds.
    /// </summary>
    /// <param name="document">The PDF document to render from.</param>
    /// <param name="pageNumber">1-based page number to render.</param>
    /// <param name="zoomLevel">Zoom level for rendering.</param>
    /// <param name="dpi">DPI for rendering quality.</param>
    /// <param name="context">Rendering context for logging and diagnostics.</param>
    /// <returns>
    /// An ImageSource ready for UI binding, or null if all strategies failed.
    /// </returns>
    public async Task<ImageSource?> RenderWithFallbackAsync(
        PdfDocument document,
        int pageNumber,
        double zoomLevel,
        double dpi,
        RenderContext context)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var overallStopwatch = Stopwatch.StartNew();
        Stream? pngStream = null;
        string? lastFailedStrategy = null;
        var failedStrategies = new List<string>();

        try
        {
            // Step 1: Render PDF page to PNG stream using PdfRenderingService
            var renderResult = await _pdfRenderingService.RenderPageAsync(document, pageNumber, zoomLevel, dpi);

            if (renderResult.IsFailed)
            {
                // PNG generation failed - log and return null
                var error = renderResult.Errors.FirstOrDefault();
                _observabilityService.LogRenderFailure(
                    "RenderPage",
                    new Exception($"PDF rendering failed: {error?.Message ?? "Unknown error"}"),
                    context);
                return null;
            }

            pngStream = renderResult.Value;

            // Step 2: Try each rendering strategy in priority order
            var strategies = _strategyFactory.GetStrategies().ToList();

            if (strategies.Count == 0)
            {
                _observabilityService.LogRenderFailure(
                    "RenderWithFallback",
                    new InvalidOperationException("No rendering strategies registered"),
                    context);
                return null;
            }

            foreach (var strategy in strategies)
            {
                var strategyStopwatch = Stopwatch.StartNew();

                try
                {
                    // Try this strategy
                    var imageSource = await strategy.TryRenderAsync(pngStream, context);
                    strategyStopwatch.Stop();

                    if (imageSource != null)
                    {
                        // Success! Log if we had to fall back
                        if (failedStrategies.Count > 0)
                        {
                            _observabilityService.LogFallbackStrategyUsed(
                                lastFailedStrategy ?? failedStrategies.Last(),
                                strategy.StrategyName,
                                context);
                        }

                        overallStopwatch.Stop();
                        _observabilityService.LogRenderSuccess(
                            $"RenderWithFallback[{strategy.StrategyName}]",
                            overallStopwatch.Elapsed,
                            pngStream.Length,
                            context);

                        return imageSource;
                    }
                    else
                    {
                        // Strategy failed, try next one
                        failedStrategies.Add(strategy.StrategyName);
                        lastFailedStrategy = strategy.StrategyName;

                        _observabilityService.LogRenderFailure(
                            $"Strategy_{strategy.StrategyName}",
                            new Exception($"Strategy '{strategy.StrategyName}' returned null"),
                            context,
                            new
                            {
                                StrategyPriority = strategy.Priority,
                                AttemptDurationMs = strategyStopwatch.ElapsedMilliseconds,
                                FailedStrategiesCount = failedStrategies.Count
                            });
                    }
                }
                catch (Exception ex)
                {
                    // Strategy threw exception instead of returning null (bad practice but handle it)
                    strategyStopwatch.Stop();
                    failedStrategies.Add(strategy.StrategyName);
                    lastFailedStrategy = strategy.StrategyName;

                    _observabilityService.LogRenderFailure(
                        $"Strategy_{strategy.StrategyName}",
                        ex,
                        context,
                        new
                        {
                            StrategyPriority = strategy.Priority,
                            AttemptDurationMs = strategyStopwatch.ElapsedMilliseconds,
                            FailedStrategiesCount = failedStrategies.Count
                        });
                }
            }

            // All strategies failed
            overallStopwatch.Stop();
            _observabilityService.LogRenderFailure(
                "RenderWithFallback",
                new Exception($"All {strategies.Count} rendering strategies failed"),
                context,
                new
                {
                    FailedStrategies = failedStrategies,
                    TotalDurationMs = overallStopwatch.ElapsedMilliseconds
                });

            return null;
        }
        finally
        {
            // Clean up PNG stream
            pngStream?.Dispose();
        }
    }
}
