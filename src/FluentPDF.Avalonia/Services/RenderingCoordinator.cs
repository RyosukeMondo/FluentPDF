using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Orchestrates PDF rendering with automatic fallback between strategies.
/// </summary>
public sealed class RenderingCoordinator
{
    private readonly RenderingStrategyFactory _strategyFactory;
    private readonly IPdfRenderingService _pdfRenderingService;
    private readonly ILogger<RenderingCoordinator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingCoordinator"/> class.
    /// </summary>
    /// <param name="strategyFactory">Factory for retrieving ordered rendering strategies.</param>
    /// <param name="pdfRenderingService">Service for rendering PDF pages to PNG streams.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public RenderingCoordinator(
        RenderingStrategyFactory strategyFactory,
        IPdfRenderingService pdfRenderingService,
        ILogger<RenderingCoordinator> logger)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _pdfRenderingService = pdfRenderingService ?? throw new ArgumentNullException(nameof(pdfRenderingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    /// An IImage ready for UI binding, or null if all strategies failed.
    /// </returns>
    public async Task<IImage?> RenderWithFallbackAsync(
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
        var failedStrategies = new List<string>();

        try
        {
            _logger.LogDebug(
                "RenderingCoordinator: Starting render. Page={PageNumber}, Zoom={ZoomLevel}, Dpi={Dpi}",
                pageNumber, zoomLevel, dpi);

            // Step 1: Render PDF page to PNG stream using PdfRenderingService
            var renderStopwatch = Stopwatch.StartNew();
            var renderResult = await _pdfRenderingService.RenderPageAsync(
                document,
                pageNumber,
                zoomLevel,
                dpi);

            if (renderResult.IsFailed || renderResult.Value == null)
            {
                _logger.LogError(
                    "RenderingCoordinator: PDF rendering failed. Page={PageNumber}, Duration={Duration}ms",
                    pageNumber, renderStopwatch.ElapsedMilliseconds);
                return null;
            }

            pngStream = renderResult.Value;
            _logger.LogDebug(
                "RenderingCoordinator: PDF rendered to PNG. Duration={Duration}ms, StreamLength={Length}",
                renderStopwatch.ElapsedMilliseconds, pngStream.Length);

            // Step 2: Try each rendering strategy in priority order
            var strategies = _strategyFactory.GetStrategies().ToList();
            _logger.LogDebug("RenderingCoordinator: Trying {Count} strategies", strategies.Count);

            foreach (var strategy in strategies)
            {
                var strategyStopwatch = Stopwatch.StartNew();

                _logger.LogDebug(
                    "RenderingCoordinator: Trying strategy '{Strategy}' (Priority={Priority})",
                    strategy.StrategyName, strategy.Priority);

                try
                {
                    var image = await strategy.TryRenderAsync(pngStream, context);

                    if (image != null)
                    {
                        _logger.LogInformation(
                            "RenderingCoordinator: SUCCESS with '{Strategy}'. TotalDuration={TotalDuration}ms, StrategyDuration={StrategyDuration}ms, Page={PageNumber}",
                            strategy.StrategyName, overallStopwatch.ElapsedMilliseconds, strategyStopwatch.ElapsedMilliseconds, pageNumber);

                        return image;
                    }

                    _logger.LogWarning(
                        "RenderingCoordinator: Strategy '{Strategy}' returned null. Duration={Duration}ms",
                        strategy.StrategyName, strategyStopwatch.ElapsedMilliseconds);
                    failedStrategies.Add(strategy.StrategyName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "RenderingCoordinator: Strategy '{Strategy}' threw exception. Duration={Duration}ms",
                        strategy.StrategyName, strategyStopwatch.ElapsedMilliseconds);
                    failedStrategies.Add(strategy.StrategyName);
                }
            }

            // All strategies failed
            _logger.LogError(
                "RenderingCoordinator: ALL STRATEGIES FAILED. FailedStrategies=[{FailedStrategies}], TotalDuration={Duration}ms, Page={PageNumber}",
                string.Join(", ", failedStrategies), overallStopwatch.ElapsedMilliseconds, pageNumber);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RenderingCoordinator: Fatal error during rendering. Duration={Duration}ms, Page={PageNumber}",
                overallStopwatch.ElapsedMilliseconds, pageNumber);
            return null;
        }
        finally
        {
            // Clean up PNG stream
            pngStream?.Dispose();
        }
    }
}
