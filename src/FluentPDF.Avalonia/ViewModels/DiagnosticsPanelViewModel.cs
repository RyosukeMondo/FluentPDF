using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Avalonia.Services;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

/// <summary>
/// View model for the diagnostics panel.
/// Provides real-time performance metrics display with periodic updates.
/// </summary>
public partial class DiagnosticsPanelViewModel : ObservableObject, IDisposable
{
    // Lazy-initialized brushes to avoid creating UI objects before UI thread is ready
    // These MUST be created on the UI thread to avoid "Call from invalid thread" errors
    // Colors chosen for WCAG AA contrast (4.5:1+) on the dark overlay background (#202020)
    private static SolidColorBrush? _greenBrush;
    private static SolidColorBrush? _orangeBrush;
    private static SolidColorBrush? _redBrush;
    private static SolidColorBrush? _grayBrush;

    private static SolidColorBrush GreenBrush => _greenBrush ??= new SolidColorBrush(Color.FromRgb(108, 203, 95));   // Bright green (7.2:1 on #202020)
    private static SolidColorBrush OrangeBrush => _orangeBrush ??= new SolidColorBrush(Color.FromRgb(255, 183, 77)); // Bright orange (7.8:1 on #202020)
    private static SolidColorBrush RedBrush => _redBrush ??= new SolidColorBrush(Color.FromRgb(255, 99, 99));        // Bright red (5.1:1 on #202020)
    private static SolidColorBrush GrayBrush => _grayBrush ??= new SolidColorBrush(Color.FromRgb(170, 170, 170));    // Light gray (6.5:1 on #202020)

    private readonly IMetricsCollectionService _metricsService;
    private readonly ILogger<DiagnosticsPanelViewModel> _logger;
    private readonly DispatcherTimer _updateTimer;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the current frames per second.
    /// </summary>
    [ObservableProperty]
    private double _currentFPS;

    /// <summary>
    /// Gets or sets the managed memory usage in megabytes.
    /// </summary>
    [ObservableProperty]
    private long _managedMemoryMB;

    /// <summary>
    /// Gets or sets the native memory usage in megabytes.
    /// </summary>
    [ObservableProperty]
    private long _nativeMemoryMB;

    /// <summary>
    /// Gets or sets the total memory usage in megabytes.
    /// </summary>
    [ObservableProperty]
    private long _totalMemoryMB;

    /// <summary>
    /// Gets or sets the last render time in milliseconds.
    /// </summary>
    [ObservableProperty]
    private double _lastRenderTimeMs;

    /// <summary>
    /// Gets or sets the current page number being displayed.
    /// </summary>
    [ObservableProperty]
    private int _currentPageNumber;

    /// <summary>
    /// Gets or sets the color for FPS display based on performance level.
    /// </summary>
    [ObservableProperty]
    private SolidColorBrush _fpsColor = GreenBrush;

    /// <summary>
    /// Gets or sets a value indicating whether the diagnostics panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsPanelViewModel"/> class.
    /// </summary>
    /// <param name="metricsService">Service for collecting and reporting performance metrics.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public DiagnosticsPanelViewModel(
        IMetricsCollectionService metricsService,
        ILogger<DiagnosticsPanelViewModel> logger)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Always start hidden
        IsVisible = false;

        // Create and configure the update timer with 500ms interval
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();

        _logger.LogInformation("DiagnosticsPanelViewModel initialized. Visible={Visible}", IsVisible);
    }

    /// <summary>
    /// Handles the timer tick event to update metrics periodically.
    /// </summary>
    private void OnUpdateTimerTick(object? sender, EventArgs args)
    {
        try
        {
            UpdateMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metrics in timer tick");
        }
    }

    /// <summary>
    /// Updates all metrics from the metrics collection service.
    /// </summary>
    private void UpdateMetrics()
    {
        try
        {
            var metrics = _metricsService.GetCurrentMetrics();

            CurrentFPS = metrics.CurrentFPS;
            ManagedMemoryMB = metrics.ManagedMemoryMB;
            NativeMemoryMB = metrics.NativeMemoryMB;
            TotalMemoryMB = metrics.TotalMemoryMB;
            LastRenderTimeMs = metrics.LastRenderTimeMs;
            CurrentPageNumber = metrics.CurrentPageNumber;

            // Update FPS color based on performance level (using static brushes to avoid thread issues)
            FpsColor = metrics.Level switch
            {
                PerformanceLevel.Good => GreenBrush,
                PerformanceLevel.Warning => OrangeBrush,
                PerformanceLevel.Critical => RedBrush,
                _ => GrayBrush
            };

            _logger.LogDebug("Metrics updated: FPS={FPS:F1}, Memory={Memory}MB, Level={Level}",
                CurrentFPS, TotalMemoryMB, metrics.Level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update metrics");
        }
    }

    /// <summary>
    /// Exports performance metrics to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportMetricsAsync()
    {
        try
        {
            _logger.LogInformation("Starting metrics export");

            var fileDialogService = App.GetService<IFileDialogService>();

            var filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "JSON Files",
                    Extensions = new List<string> { "json" }
                },
                new FileDialogFilter
                {
                    Name = "CSV Files",
                    Extensions = new List<string> { "csv" }
                }
            };

            var suggestedFileName = $"metrics-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            var filePath = await fileDialogService.SaveFileAsync(
                "Export Metrics",
                suggestedFileName,
                filters);

            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation("Metrics export cancelled by user");
                return;
            }

            // Determine format from file extension
            var format = Path.GetExtension(filePath).ToLowerInvariant() == ".csv"
                ? ExportFormat.Csv
                : ExportFormat.Json;

            var result = await _metricsService.ExportMetricsAsync(filePath, format);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Metrics exported successfully to {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("Metrics export failed: {Errors}",
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during metrics export");
        }
    }


    /// <summary>
    /// Disposes resources used by the view model.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _updateTimer?.Stop();
        _disposed = true;
        _logger.LogInformation("DiagnosticsPanelViewModel disposed");
    }
}
