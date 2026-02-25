using System.Diagnostics;
using System.Reactive.Linq;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for detecting and providing display DPI information.
/// Returns the standard 96 DPI as default. Avalonia handles DPI scaling internally
/// through its rendering pipeline, so explicit XamlRoot-based detection is unnecessary.
/// </summary>
public sealed class DpiDetectionService : IDpiDetectionService, IDisposable
{
    private readonly ILogger<DpiDetectionService> _logger;
    private static readonly ActivitySource _activitySource = new("FluentPDF.Rendering");
    private const double BaseDpi = 96.0;
    private const double MinDpi = 50.0;
    private const double MaxDpi = 576.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="DpiDetectionService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging and diagnostics.</param>
    public DpiDetectionService(ILogger<DpiDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Result<DisplayInfo> GetCurrentDisplayInfo(object? xamlRoot)
    {
        using var activity = _activitySource.StartActivity("GetCurrentDisplayInfo");

        // Avalonia manages DPI scaling internally. Return standard display info.
        _logger.LogDebug("Returning standard DPI ({BaseDpi}). Avalonia handles scaling internally.", BaseDpi);

        activity?.SetTag("effective.dpi", BaseDpi);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Ok(DisplayInfo.Standard());
    }

    /// <inheritdoc />
    public Result<IObservable<DisplayInfo>> MonitorDpiChanges(object? xamlRoot, int throttleMilliseconds = 500)
    {
        using var activity = _activitySource.StartActivity("MonitorDpiChanges");

        // DPI monitoring is a no-op under Avalonia. Return an empty observable
        // that never emits, since Avalonia handles DPI changes in its rendering layer.
        _logger.LogDebug("DPI monitoring is a no-op under Avalonia. Returning empty observable.");

        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Ok(Observable.Empty<DisplayInfo>());
    }

    /// <inheritdoc />
    public Result<double> CalculateEffectiveDpi(DisplayInfo displayInfo, double zoomLevel, RenderingQuality quality)
    {
        using var activity = _activitySource.StartActivity("CalculateEffectiveDpi");
        var correlationId = Guid.NewGuid();

        try
        {
            if (displayInfo == null)
            {
                var error = new PdfError(
                    "DPI_CALCULATION_FAILED",
                    "DisplayInfo cannot be null.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogWarning("DisplayInfo is null for DPI calculation. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail(error);
            }

            if (zoomLevel <= 0)
            {
                var error = new PdfError(
                    "DPI_CALCULATION_FAILED",
                    $"Zoom level must be positive. Got: {zoomLevel}",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("ZoomLevel", zoomLevel)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogWarning("Invalid zoom level for DPI calculation. CorrelationId={CorrelationId}, ZoomLevel={ZoomLevel}",
                    correlationId, zoomLevel);

                return Result.Fail(error);
            }

            var qualityMultiplier = DisplayInfo.GetQualityMultiplier(quality);
            var effectiveDpi = BaseDpi * displayInfo.RasterizationScale * zoomLevel * qualityMultiplier;
            effectiveDpi = Math.Clamp(effectiveDpi, MinDpi, MaxDpi);

            _logger.LogInformation(
                "Calculated effective DPI. CorrelationId={CorrelationId}, EffectiveDpi={EffectiveDpi}, Quality={Quality}",
                correlationId, effectiveDpi, quality);

            activity?.SetTag("effective.dpi", effectiveDpi);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Ok(effectiveDpi);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "DPI_CALCULATION_FAILED",
                $"Failed to calculate effective DPI: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex, "Failed to calculate effective DPI. CorrelationId={CorrelationId}", correlationId);

            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return Result.Fail(error);
        }
    }

    /// <summary>
    /// Disposes the service. No-op since there are no monitoring subscriptions to clean up.
    /// </summary>
    public void Dispose()
    {
        // No resources to dispose in the simplified implementation.
    }
}
