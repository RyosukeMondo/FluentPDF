using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentPDF.Avalonia.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Text;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Manages the debug console UI and log streaming.
/// Extracted from MainWindow to reduce complexity.
/// </summary>
public sealed class DebugConsoleManager : IDisposable
{
    private readonly Window _owner;
    private readonly ILogger? _logger;
    private ILogBufferService? _logBuffer;
    private readonly DispatcherTimer _logRefreshTimer;
    private readonly ObservableCollection<BufferedLogEntry> _logEntries;
    private ScrollViewer? _logScrollViewer;
    private RowDefinition? _debugConsoleRow;
    private bool _disposed;

    public DebugConsoleManager(Window owner, ILogger? logger = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _logger = logger;
        _logEntries = new ObservableCollection<BufferedLogEntry>();
        _logRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _logRefreshTimer.Tick += OnLogRefreshTimerTick;
    }

    /// <summary>
    /// Initializes debug console controls and wires up event handlers.
    /// </summary>
    public void Initialize(ILogBufferService logBuffer)
    {
        try
        {
            _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));

            _logScrollViewer = _owner.FindControl<ScrollViewer>("LogScrollViewer");
            var logItemsControl = _owner.FindControl<ItemsControl>("LogItemsControl");
            var copyLogsButton = _owner.FindControl<Button>("CopyLogsButton");
            var downloadLogButton = _owner.FindControl<Button>("DownloadLogButton");
            var clearLogsButton = _owner.FindControl<Button>("ClearLogsButton");
            var toggleConsoleButton = _owner.FindControl<ToggleButton>("ToggleConsoleButton");

            var mainGrid = _owner.FindControl<Grid>("MainGrid");
            if (mainGrid?.RowDefinitions != null && mainGrid.RowDefinitions.Count > 3)
            {
                _debugConsoleRow = mainGrid.RowDefinitions[3];
            }

            if (logItemsControl != null)
            {
                logItemsControl.ItemsSource = _logEntries;
            }

            if (copyLogsButton != null)
            {
                copyLogsButton.Click += OnCopyLogsClick;
            }

            if (downloadLogButton != null)
            {
                downloadLogButton.Click += OnDownloadLogClick;
            }

            if (clearLogsButton != null)
            {
                clearLogsButton.Click += OnClearLogsClick;
            }

            if (toggleConsoleButton != null)
            {
                toggleConsoleButton.Click += OnToggleConsoleClick;
            }

            _logBuffer.AddLog("Info", "Debug console initialized - real-time logging enabled", "MainWindow");
            _logBuffer.AddLog("Info", "Click 'Copy Last 50' to copy recent logs to clipboard", "MainWindow");
            _logBuffer.AddLog("Info", "Click 'Download Log' to save full log file", "MainWindow");
            _logBuffer.AddLog("Info", "---", "MainWindow");

            _logger?.LogInformation("Debug console initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize debug console");
        }
    }

    private void OnLogRefreshTimerTick(object? sender, EventArgs e)
    {
        try
        {
            if (_logBuffer == null) return;

            var latestLogs = _logBuffer.GetRecentLogs(100);

            if (latestLogs.Count > _logEntries.Count ||
                (latestLogs.Count > 0 && _logEntries.Count > 0 &&
                 latestLogs[latestLogs.Count - 1].Timestamp != _logEntries[_logEntries.Count - 1].Timestamp))
            {
                _logEntries.Clear();
                foreach (var log in latestLogs)
                {
                    _logEntries.Add(log);
                }

                Dispatcher.UIThread.Post(() => _logScrollViewer?.ScrollToEnd(), DispatcherPriority.Background);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing debug console logs");
        }
    }

    private async void OnCopyLogsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_logBuffer == null) return;

            var logs = _logBuffer.GetRecentLogs(50);
            var sb = new StringBuilder();
            sb.AppendLine("=== FluentPDF Debug Logs (Last 50 entries) ===");
            sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:HH:mm:ss.fff}] [{log.Level}] [{log.Source}] {log.Message}");
            }

            var clipboard = TopLevel.GetTopLevel(_owner)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(sb.ToString());
                _logBuffer.AddLog("Info", "Last 50 logs copied to clipboard", "DebugConsole");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to copy logs to clipboard");
        }
    }

    private async void OnDownloadLogClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_logBuffer == null) return;

            var storageProvider = _owner.StorageProvider;
            if (storageProvider == null) return;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Debug Log",
                SuggestedFileName = $"fluentpdf-debug-{DateTime.Now:yyyyMMdd-HHmmss}.log",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Log Files") { Patterns = new[] { "*.log" } },
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } }
                }
            });

            if (file == null) return;

            var logs = _logBuffer.GetRecentLogs(1000);
            var sb = new StringBuilder();
            sb.AppendLine("=== FluentPDF Debug Logs ===");
            sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Entries: {logs.Count}");
            sb.AppendLine();

            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{log.Level}] [{log.Source}] {log.Message}");
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(sb.ToString());

            _logBuffer.AddLog("Info", $"Debug log saved to: {file.Name}", "DebugConsole");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download log file");
        }
    }

    private void OnClearLogsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logBuffer?.Clear();
            _logEntries.Clear();
            _logBuffer?.AddLog("Info", "Debug console cleared", "DebugConsole");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear logs");
        }
    }

    private void OnToggleConsoleClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton != null && _debugConsoleRow != null)
            {
                if (toggleButton.IsChecked == true)
                {
                    _debugConsoleRow.Height = new GridLength(250);
                    _debugConsoleRow.MinHeight = 100;
                }
                else
                {
                    _debugConsoleRow.Height = new GridLength(0);
                    _debugConsoleRow.MinHeight = 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle debug console");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _logRefreshTimer.Stop();
        _disposed = true;
    }
}
