namespace FluentPDF.Core.Models;

/// <summary>
/// Represents the application settings and user preferences.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the default zoom level for newly opened documents.
    /// </summary>
    public ZoomLevel DefaultZoom { get; set; } = ZoomLevel.OneHundredPercent;

    /// <summary>
    /// Gets or sets the default scroll mode for document viewing.
    /// </summary>
    public ScrollMode ScrollMode { get; set; } = ScrollMode.Vertical;

    /// <summary>
    /// Gets or sets the application theme preference.
    /// </summary>
    public AppTheme Theme { get; set; } = AppTheme.UseSystem;

    /// <summary>
    /// Gets or sets whether anonymous telemetry is enabled.
    /// Default is false (opt-in only).
    /// </summary>
    public bool TelemetryEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether anonymous crash reporting is enabled.
    /// Default is false (opt-in only).
    /// </summary>
    public bool CrashReportingEnabled { get; set; } = false;

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public static AppSettings CreateDefault() => new();
}

/// <summary>
/// Defines available zoom levels for PDF viewing.
/// </summary>
public enum ZoomLevel
{
    /// <summary>
    /// 50% zoom level.
    /// </summary>
    FiftyPercent = 50,

    /// <summary>
    /// 75% zoom level.
    /// </summary>
    SeventyFivePercent = 75,

    /// <summary>
    /// 100% zoom level (actual size).
    /// </summary>
    OneHundredPercent = 100,

    /// <summary>
    /// 125% zoom level.
    /// </summary>
    OneTwentyFivePercent = 125,

    /// <summary>
    /// 150% zoom level.
    /// </summary>
    OneFiftyPercent = 150,

    /// <summary>
    /// 175% zoom level.
    /// </summary>
    OneSeventyFivePercent = 175,

    /// <summary>
    /// 200% zoom level.
    /// </summary>
    TwoHundredPercent = 200,

    /// <summary>
    /// Fit page width to viewport.
    /// </summary>
    FitWidth = 1000,

    /// <summary>
    /// Fit entire page in viewport.
    /// </summary>
    FitPage = 1001
}

/// <summary>
/// Defines scroll modes for document navigation.
/// </summary>
public enum ScrollMode
{
    /// <summary>
    /// Vertical scrolling (default).
    /// </summary>
    Vertical,

    /// <summary>
    /// Horizontal scrolling.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Single page view with page-by-page navigation.
    /// </summary>
    FitPage
}

/// <summary>
/// Defines application theme options.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Follow system theme preference (default).
    /// </summary>
    UseSystem
}
