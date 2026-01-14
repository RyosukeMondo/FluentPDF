using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

using CoreLogLevel = FluentPDF.Core.Observability.LogLevel;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// View model for the log viewer.
/// Provides log filtering, searching, and export functionality.
/// </summary>
public partial class LogViewerViewModel : ObservableObject
{
    private readonly ILogExportService _logExportService;
    private readonly ILogger<LogViewerViewModel> _logger;
    private CancellationTokenSource? _searchCancellationTokenSource;
    private IReadOnlyList<LogEntry> _allLogs = Array.Empty<LogEntry>();

    /// <summary>
    /// Gets the collection of log entries currently being displayed.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected log entry.
    /// </summary>
    [ObservableProperty]
    private LogEntry? _selectedLogEntry;

    /// <summary>
    /// Gets or sets the minimum log level filter.
    /// </summary>
    [ObservableProperty]
    private CoreLogLevel? _minimumLevel;

    /// <summary>
    /// Gets or sets the correlation ID filter.
    /// </summary>
    [ObservableProperty]
    private string? _correlationIdFilter;

    /// <summary>
    /// Gets or sets the component filter.
    /// </summary>
    [ObservableProperty]
    private string? _componentFilter;

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    [ObservableProperty]
    private string? _searchText;

    /// <summary>
    /// Gets or sets the start time for filtering.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _startTime;

    /// <summary>
    /// Gets or sets the end time for filtering.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _endTime;

    /// <summary>
    /// Gets or sets a value indicating whether logs are being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    partial void OnSearchTextChanged(string? value)
    {
        // Debounce search with 500ms delay
        _ = DebouncedSearchAsync();
    }

    partial void OnMinimumLevelChanged(CoreLogLevel? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnCorrelationIdFilterChanged(string? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnComponentFilterChanged(string? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnStartTimeChanged(DateTimeOffset? value)
    {
        _ = ApplyFiltersAsync();
    }

    partial void OnEndTimeChanged(DateTimeOffset? value)
    {
        _ = ApplyFiltersAsync();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogViewerViewModel"/> class.
    /// </summary>
    /// <param name="logExportService">Service for reading and exporting log files.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public LogViewerViewModel(
        ILogExportService logExportService,
        ILogger<LogViewerViewModel> logger)
    {
        _logExportService = logExportService ?? throw new ArgumentNullException(nameof(logExportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("LogViewerViewModel initialized");
    }

    /// <summary>
    /// Loads the most recent log entries.
    /// </summary>
    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogInformation("Loading recent logs");

            var result = await _logExportService.GetRecentLogsAsync(1000);

            if (result.IsSuccess)
            {
                _allLogs = result.Value;
                await ApplyFiltersAsync();
                _logger.LogInformation("Loaded {Count} log entries", _allLogs.Count);
            }
            else
            {
                _logger.LogWarning("Failed to load logs: {Errors}",
                    string.Join(", ", result.Errors));
                LogEntries.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while loading logs");
            LogEntries.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Applies the current filter criteria to the log entries.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        try
        {
            _logger.LogDebug("Applying filters: MinLevel={MinLevel}, CorrelationId={CorrelationId}, " +
                "Component={Component}, Search={Search}, StartTime={StartTime}, EndTime={EndTime}",
                MinimumLevel, CorrelationIdFilter, ComponentFilter, SearchText, StartTime, EndTime);

            // Build filter criteria
            var criteria = new LogFilterCriteria
            {
                MinimumLevel = MinimumLevel,
                CorrelationId = CorrelationIdFilter,
                ComponentFilter = ComponentFilter,
                SearchText = SearchText,
                StartTime = StartTime?.DateTime,
                EndTime = EndTime?.DateTime
            };

            // Apply filters locally to avoid unnecessary service calls
            var filteredLogs = _allLogs.Where(log => criteria.Matches(log)).ToList();

            // Update the UI collection
            LogEntries.Clear();
            foreach (var log in filteredLogs)
            {
                LogEntries.Add(log);
            }

            _logger.LogInformation("Filters applied. Showing {Count} of {Total} logs",
                LogEntries.Count, _allLogs.Count);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while applying filters");
        }
    }

    /// <summary>
    /// Implements debounced search with 500ms delay.
    /// </summary>
    private async Task DebouncedSearchAsync()
    {
        // Cancel previous search operation
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();

        var cancellationToken = _searchCancellationTokenSource.Token;

        try
        {
            // Wait for 500ms
            await Task.Delay(500, cancellationToken);

            // If not cancelled, apply filters
            if (!cancellationToken.IsCancellationRequested)
            {
                await ApplyFiltersAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when search is cancelled, do nothing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during debounced search");
        }
    }

    /// <summary>
    /// Clears all filter criteria.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        _logger.LogInformation("Clearing all filters");

        MinimumLevel = null;
        CorrelationIdFilter = null;
        ComponentFilter = null;
        SearchText = null;
        StartTime = null;
        EndTime = null;

        _ = ApplyFiltersAsync();
    }

    /// <summary>
    /// Exports the currently filtered log entries to a file.
    /// </summary>
    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        try
        {
            _logger.LogInformation("Starting log export. Entries={Count}", LogEntries.Count);

            // Create file picker
            var picker = new FileSavePicker();

            // Get the main window handle for the picker
            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"logs-{DateTime.Now:yyyyMMdd-HHmmss}";
            picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });

            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                _logger.LogInformation("Log export cancelled by user");
                return;
            }

            var result = await _logExportService.ExportLogsAsync(LogEntries.ToList(), file.Path);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Logs exported successfully to {FilePath}", file.Path);
            }
            else
            {
                _logger.LogWarning("Log export failed: {Errors}",
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during log export");
        }
    }

    /// <summary>
    /// Copies the correlation ID of the selected log entry to the clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyCorrelationId()
    {
        if (SelectedLogEntry?.CorrelationId == null)
        {
            _logger.LogWarning("Cannot copy correlation ID: no log entry selected or no correlation ID present");
            return;
        }

        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(SelectedLogEntry.CorrelationId);
            Clipboard.SetContent(dataPackage);

            _logger.LogInformation("Correlation ID copied to clipboard: {CorrelationId}",
                SelectedLogEntry.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy correlation ID to clipboard");
        }
    }
}
