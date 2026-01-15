using System.Diagnostics;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for rendering PDF page thumbnails at low resolution.
/// Wraps PdfRenderingService with optimized settings for thumbnail generation (low DPI, small zoom).
/// </summary>
public sealed class ThumbnailRenderingService : IThumbnailRenderingService
{
    private readonly IPdfRenderingService _renderingService;
    private readonly ILogger<ThumbnailRenderingService> _logger;

    // Optimized settings for thumbnails (~150x200px for standard letter page)
    private const double ThumbnailDpi = 48.0; // Half of standard 96 DPI
    private const double ThumbnailZoom = 0.2; // 20% of full size

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailRenderingService"/> class.
    /// </summary>
    /// <param name="renderingService">The underlying PDF rendering service.</param>
    /// <param name="logger">Logger for performance monitoring.</param>
    public ThumbnailRenderingService(
        IPdfRenderingService renderingService,
        ILogger<ThumbnailRenderingService> logger)
    {
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> RenderThumbnailAsync(PdfDocument document, int pageNumber)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Starting thumbnail render. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
            correlationId, pageNumber);

        var result = await _renderingService.RenderPageAsync(
            document,
            pageNumber,
            ThumbnailZoom,
            ThumbnailDpi);

        stopwatch.Stop();

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Thumbnail rendered successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}",
                correlationId, pageNumber, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Thumbnail render failed. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}, Errors={Errors}",
                correlationId, pageNumber, stopwatch.ElapsedMilliseconds, string.Join("; ", result.Errors.Select(e => e.Message)));
        }

        return result;
    }
}
