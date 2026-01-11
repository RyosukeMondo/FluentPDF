using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for detecting and monitoring display DPI using WinUI 3 XamlRoot.
/// Uses reflection to access platform-specific XamlRoot APIs to maintain cross-platform compatibility.
/// Provides DPI detection, monitoring with throttling, and effective DPI calculation.
/// </summary>
public sealed class DpiDetectionService : IDpiDetectionService, IDisposable
{
    private readonly ILogger<DpiDetectionService> _logger;
    private static readonly ActivitySource ActivitySource = new("FluentPDF.Rendering");
    private const double BaseDpi = 96.0;
    private const double MinDpi = 50.0;
    private const double MaxDpi = 576.0;

    private readonly Dictionary<int, IDisposable> _monitoringSubscriptions = new();
    private int _nextSubscriptionId = 0;

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
        using var activity = ActivitySource.StartActivity("GetCurrentDisplayInfo");
        var correlationId = Guid.NewGuid();

        _logger.LogDebug("Getting current display info. CorrelationId={CorrelationId}", correlationId);

        try
        {
            if (xamlRoot == null)
            {
                var error = new PdfError(
                    "DPI_DETECTION_FAILED",
                    "XamlRoot is null. Cannot detect DPI without valid XamlRoot.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Warning)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogWarning("XamlRoot is null, returning standard DPI. CorrelationId={CorrelationId}", correlationId);

                // Return standard display as fallback
                return Result.Ok(DisplayInfo.Standard());
            }

            // Use reflection to access XamlRoot.RasterizationScale
            var xamlRootType = xamlRoot.GetType();
            var rasterizationScaleProperty = xamlRootType.GetProperty("RasterizationScale");

            if (rasterizationScaleProperty == null)
            {
                var error = new PdfError(
                    "DPI_DETECTION_FAILED",
                    "XamlRoot does not have RasterizationScale property. Platform may not support DPI detection.",
                    ErrorCategory.System,
                    ErrorSeverity.Warning)
                    .WithContext("XamlRootType", xamlRootType.FullName ?? "Unknown")
                    .WithContext("CorrelationId", correlationId);

                _logger.LogWarning("XamlRoot.RasterizationScale property not found. CorrelationId={CorrelationId}, Type={Type}",
                    correlationId, xamlRootType.FullName);

                return Result.Ok(DisplayInfo.Standard());
            }

            var rasterizationScale = (double)rasterizationScaleProperty.GetValue(xamlRoot)!;
            var effectiveDpi = BaseDpi * rasterizationScale;

            _logger.LogInformation(
                "Detected display DPI. CorrelationId={CorrelationId}, RasterizationScale={RasterizationScale}, EffectiveDpi={EffectiveDpi}",
                correlationId, rasterizationScale, effectiveDpi);

            activity?.SetTag("rasterization.scale", rasterizationScale);
            activity?.SetTag("effective.dpi", effectiveDpi);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Ok(DisplayInfo.Create(rasterizationScale, effectiveDpi));
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "DPI_DETECTION_FAILED",
                $"Failed to detect display DPI: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex, "Failed to detect display DPI. CorrelationId={CorrelationId}", correlationId);

            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return Result.Fail(error);
        }
    }

    /// <inheritdoc />
    public Result<IObservable<DisplayInfo>> MonitorDpiChanges(object? xamlRoot, int throttleMilliseconds = 500)
    {
        using var activity = ActivitySource.StartActivity("MonitorDpiChanges");
        var correlationId = Guid.NewGuid();

        _logger.LogDebug(
            "Starting DPI monitoring. CorrelationId={CorrelationId}, ThrottleMs={ThrottleMs}",
            correlationId, throttleMilliseconds);

        try
        {
            if (xamlRoot == null)
            {
                var error = new PdfError(
                    "DPI_MONITORING_FAILED",
                    "XamlRoot is null. Cannot monitor DPI changes without valid XamlRoot.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Warning)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogWarning("XamlRoot is null, cannot monitor DPI. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail(error);
            }

            // Create a subject to publish DPI changes
            var dpiSubject = new Subject<DisplayInfo>();

            // Use reflection to access XamlRoot.Changed event
            var xamlRootType = xamlRoot.GetType();
            var changedEvent = xamlRootType.GetEvent("Changed");

            if (changedEvent == null)
            {
                var error = new PdfError(
                    "DPI_MONITORING_FAILED",
                    "XamlRoot does not have Changed event. Platform may not support DPI change monitoring.",
                    ErrorCategory.System,
                    ErrorSeverity.Warning)
                    .WithContext("XamlRootType", xamlRootType.FullName ?? "Unknown")
                    .WithContext("CorrelationId", correlationId);

                _logger.LogWarning("XamlRoot.Changed event not found. CorrelationId={CorrelationId}, Type={Type}",
                    correlationId, xamlRootType.FullName);

                return Result.Fail(error);
            }

            // Create event handler using reflection
            var eventHandlerType = changedEvent.EventHandlerType!;
            var handler = Delegate.CreateDelegate(eventHandlerType, new EventHandlerWrapper(xamlRoot, dpiSubject, _logger),
                nameof(EventHandlerWrapper.OnXamlRootChanged));

            // Subscribe to the Changed event
            changedEvent.AddEventHandler(xamlRoot, handler);

            // Create throttled observable
            var throttledObservable = dpiSubject
                .Throttle(TimeSpan.FromMilliseconds(throttleMilliseconds))
                .DistinctUntilChanged(info => info.RasterizationScale);

            // Track subscription for cleanup
            var subscriptionId = _nextSubscriptionId++;
            _monitoringSubscriptions[subscriptionId] = new MonitoringSubscription(xamlRoot, changedEvent, handler, dpiSubject);

            _logger.LogInformation(
                "DPI monitoring started. CorrelationId={CorrelationId}, SubscriptionId={SubscriptionId}, ThrottleMs={ThrottleMs}",
                correlationId, subscriptionId, throttleMilliseconds);

            activity?.SetTag("subscription.id", subscriptionId);
            activity?.SetTag("throttle.ms", throttleMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Ok<IObservable<DisplayInfo>>(throttledObservable);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "DPI_MONITORING_FAILED",
                $"Failed to start DPI monitoring: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex, "Failed to start DPI monitoring. CorrelationId={CorrelationId}", correlationId);

            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return Result.Fail(error);
        }
    }

    /// <inheritdoc />
    public Result<double> CalculateEffectiveDpi(DisplayInfo displayInfo, double zoomLevel, RenderingQuality quality)
    {
        using var activity = ActivitySource.StartActivity("CalculateEffectiveDpi");
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

            _logger.LogDebug(
                "Calculating effective DPI. CorrelationId={CorrelationId}, RasterizationScale={RasterizationScale}, ZoomLevel={ZoomLevel}, Quality={Quality}",
                correlationId, displayInfo.RasterizationScale, zoomLevel, quality);

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

            // Get quality multiplier
            var qualityMultiplier = DisplayInfo.GetQualityMultiplier(quality);

            // Calculate effective DPI: base DPI * display scale * zoom * quality multiplier
            var effectiveDpi = BaseDpi * displayInfo.RasterizationScale * zoomLevel * qualityMultiplier;

            // Clamp to valid bounds
            effectiveDpi = Math.Clamp(effectiveDpi, MinDpi, MaxDpi);

            _logger.LogInformation(
                "Calculated effective DPI. CorrelationId={CorrelationId}, EffectiveDpi={EffectiveDpi}, Quality={Quality}, QualityMultiplier={QualityMultiplier}",
                correlationId, effectiveDpi, quality, qualityMultiplier);

            activity?.SetTag("effective.dpi", effectiveDpi);
            activity?.SetTag("quality", quality.ToString());
            activity?.SetTag("quality.multiplier", qualityMultiplier);
            activity?.SetTag("zoom.level", zoomLevel);
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
    /// Disposes the service and cleans up all DPI monitoring subscriptions.
    /// </summary>
    public void Dispose()
    {
        foreach (var subscription in _monitoringSubscriptions.Values)
        {
            subscription.Dispose();
        }
        _monitoringSubscriptions.Clear();
    }

    /// <summary>
    /// Wrapper class for handling XamlRoot.Changed events using reflection.
    /// </summary>
    private class EventHandlerWrapper
    {
        private readonly object _xamlRoot;
        private readonly Subject<DisplayInfo> _subject;
        private readonly ILogger _logger;

        public EventHandlerWrapper(object xamlRoot, Subject<DisplayInfo> subject, ILogger logger)
        {
            _xamlRoot = xamlRoot;
            _subject = subject;
            _logger = logger;
        }

        public void OnXamlRootChanged(object? sender, object? args)
        {
            try
            {
                // Get current rasterization scale
                var xamlRootType = _xamlRoot.GetType();
                var rasterizationScaleProperty = xamlRootType.GetProperty("RasterizationScale");

                if (rasterizationScaleProperty != null)
                {
                    var rasterizationScale = (double)rasterizationScaleProperty.GetValue(_xamlRoot)!;
                    var effectiveDpi = BaseDpi * rasterizationScale;
                    var displayInfo = DisplayInfo.Create(rasterizationScale, effectiveDpi);

                    _logger.LogDebug(
                        "XamlRoot changed. RasterizationScale={RasterizationScale}, EffectiveDpi={EffectiveDpi}",
                        rasterizationScale, effectiveDpi);

                    _subject.OnNext(displayInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling XamlRoot.Changed event");
                _subject.OnError(ex);
            }
        }
    }

    /// <summary>
    /// Represents a DPI monitoring subscription that can be disposed.
    /// </summary>
    private class MonitoringSubscription : IDisposable
    {
        private readonly object _xamlRoot;
        private readonly System.Reflection.EventInfo _event;
        private readonly Delegate _handler;
        private readonly Subject<DisplayInfo> _subject;

        public MonitoringSubscription(object xamlRoot, System.Reflection.EventInfo evt, Delegate handler, Subject<DisplayInfo> subject)
        {
            _xamlRoot = xamlRoot;
            _event = evt;
            _handler = handler;
            _subject = subject;
        }

        public void Dispose()
        {
            try
            {
                _event.RemoveEventHandler(_xamlRoot, _handler);
                _subject.OnCompleted();
                _subject.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
