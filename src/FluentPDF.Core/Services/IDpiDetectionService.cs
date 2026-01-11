using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for detecting and monitoring display DPI information.
/// Provides methods to detect current DPI settings, calculate effective DPI with quality adjustments,
/// and monitor DPI changes in real-time.
/// </summary>
public interface IDpiDetectionService
{
    /// <summary>
    /// Gets the current display information from the provided XamlRoot.
    /// XamlRoot is platform-specific (WinUI 3) and must be provided by the caller.
    /// </summary>
    /// <param name="xamlRoot">The XamlRoot object to query for rasterization scale (platform-specific).</param>
    /// <returns>A result containing DisplayInfo with current rasterization scale and base DPI, or an error if detection fails.</returns>
    Result<DisplayInfo> GetCurrentDisplayInfo(object? xamlRoot);

    /// <summary>
    /// Monitors DPI changes and emits DisplayInfo updates through an observable stream.
    /// The stream is throttled to avoid excessive updates during rapid DPI changes.
    /// </summary>
    /// <param name="xamlRoot">The XamlRoot object to monitor for DPI changes (platform-specific).</param>
    /// <param name="throttleMilliseconds">Throttle interval in milliseconds to debounce rapid changes. Default is 500ms.</param>
    /// <returns>An observable stream of DisplayInfo updates, or an error if monitoring cannot be established.</returns>
    Result<IObservable<DisplayInfo>> MonitorDpiChanges(object? xamlRoot, int throttleMilliseconds = 500);

    /// <summary>
    /// Calculates the effective DPI for rendering, taking into account display scale,
    /// zoom level, quality settings, and DPI bounds (50-576 DPI).
    /// </summary>
    /// <param name="displayInfo">The current display information.</param>
    /// <param name="zoomLevel">The zoom level (1.0 = 100%, 2.0 = 200%, etc.).</param>
    /// <param name="quality">The rendering quality setting that affects DPI multiplier.</param>
    /// <returns>A result containing the calculated effective DPI clamped to valid bounds, or an error if calculation fails.</returns>
    Result<double> CalculateEffectiveDpi(DisplayInfo displayInfo, double zoomLevel, RenderingQuality quality);
}
