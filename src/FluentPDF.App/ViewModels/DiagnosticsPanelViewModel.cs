using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// View model for the diagnostics panel.
/// Provides real-time performance metrics display with periodic updates.
/// </summary>
public partial class DiagnosticsPanelViewModel : ObservableObject, IDisposable
{
    private readonly IMetricsCollectionService _metricsService;
    private readonly ILogger<DiagnosticsPanelViewModel> _logger;
    private readonly DispatcherQueueTimer _updateTimer;
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
    private SolidColorBrush _fpsColor = new(Colors.Green);

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

        LoadPanelState();

        // Create and configure the update timer with 500ms interval
        _updateTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(500);
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();

        _logger.LogInformation("DiagnosticsPanelViewModel initialized. Visible={Visible}", IsVisible);
    }

    /// <summary>
    /// Handles the timer tick event to update metrics periodically.
    /// </summary>
    private void OnUpdateTimerTick(DispatcherQueueTimer sender, object args)
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

            // Update FPS color based on performance level
            FpsColor = metrics.Level switch
            {
                PerformanceLevel.Good => new SolidColorBrush(Colors.Green),
                PerformanceLevel.Warning => new SolidColorBrush(Colors.Orange),
                PerformanceLevel.Critical => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray)
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

            // Create file picker
            var picker = new FileSavePicker();

            // Get the main window handle for the picker
            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"metrics-{DateTime.Now:yyyyMMdd-HHmmss}";
            picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" });

            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                _logger.LogInformation("Metrics export cancelled by user");
                return;
            }

            // Determine format from file extension
            var format = file.FileType.ToLowerInvariant() == ".csv"
                ? ExportFormat.Csv
                : ExportFormat.Json;

            var result = await _metricsService.ExportMetricsAsync(file.Path, format);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Metrics exported successfully to {FilePath}", file.Path);
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
    /// Toggles the visibility of the diagnostics panel.
    /// </summary>
    [RelayCommand]
    private void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        _logger.LogInformation("Diagnostics panel visibility toggled. Visible={Visible}", IsVisible);
        SavePanelState();
    }

    /// <summary>
    /// Saves the current panel state to application settings.
    /// </summary>
    private void SavePanelState()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["DiagnosticsPanelVisible"] = IsVisible;
            _logger.LogDebug("Panel state saved. Visible={Visible}", IsVisible);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save panel state");
        }
    }

    /// <summary>
    /// Loads the panel state from application settings.
    /// </summary>
    private void LoadPanelState()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            if (settings.Values.TryGetValue("DiagnosticsPanelVisible", out var visible))
            {
                IsVisible = (bool)visible;
            }

            _logger.LogDebug("Panel state loaded. Visible={Visible}", IsVisible);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load panel state, using defaults");
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
