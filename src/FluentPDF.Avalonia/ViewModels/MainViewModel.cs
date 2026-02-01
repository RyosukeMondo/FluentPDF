using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

/// <summary>
/// Main view model for the FluentPDF application.
/// Manages tab collection and coordinates between tabs and recent files service.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IServiceProvider _serviceProvider;
    private readonly Services.IThemeService _themeService;
    private bool _disposed;

    private readonly Services.ILogBufferService? _logBuffer;
    private readonly Services.IOperationWatchdog? _watchdog;

    /// <summary>
    /// Gets the diagnostics panel view model.
    /// </summary>
    public DiagnosticsPanelViewModel DiagnosticsPanelViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking view model operations.</param>
    /// <param name="recentFilesService">Service for managing recent files.</param>
    /// <param name="serviceProvider">Service provider for creating tab dependencies.</param>
    /// <param name="themeService">Service for managing application theme.</param>
    /// <param name="diagnosticsPanelViewModel">View model for the diagnostics panel.</param>
    /// <param name="logBuffer">Optional log buffer for capturing debug logs.</param>
    /// <param name="watchdog">Optional operation watchdog for hang detection.</param>
    public MainViewModel(
        ILogger<MainViewModel> logger,
        IRecentFilesService recentFilesService,
        IServiceProvider serviceProvider,
        Services.IThemeService themeService,
        DiagnosticsPanelViewModel diagnosticsPanelViewModel,
        Services.ILogBufferService? logBuffer = null,
        Services.IOperationWatchdog? watchdog = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        DiagnosticsPanelViewModel = diagnosticsPanelViewModel ?? throw new ArgumentNullException(nameof(diagnosticsPanelViewModel));
        _logBuffer = logBuffer;
        _watchdog = watchdog;

        Tabs = new ObservableCollection<TabViewModel>();

        _logger.LogInformation("MainViewModel initialized with theme: {Theme}", _themeService.CurrentTheme);
        _logBuffer?.AddLog("Info", "MainViewModel initialized", "MainViewModel");
    }

    private void LogInfo(string message)
    {
        _logger.LogInformation(message);
        _logBuffer?.AddLog("Info", message, "MainViewModel");
    }

    private void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
            _logger.LogError(ex, message);
        else
            _logger.LogError(message);

        _logBuffer?.AddLog("Error", ex != null ? $"{message}: {ex.Message}" : message, "MainViewModel");
    }

    /// <summary>
    /// Gets the collection of open tabs.
    /// </summary>
    public ObservableCollection<TabViewModel> Tabs { get; }

    /// <summary>
    /// Gets or sets the currently active tab.
    /// </summary>
    [ObservableProperty]
    private TabViewModel? _activeTab;

    /// <summary>
    /// Opens a file picker and creates a new tab with the selected PDF.
    /// </summary>
    [RelayCommand]
    private async Task OpenFileInNewTabAsync()
    {
        _logger.LogInformation("OpenFileInNewTab command invoked");

        try
        {
            var fileDialogService = App.GetService<Services.IFileDialogService>();

            var filters = new List<Services.FileDialogFilter>
            {
                new Services.FileDialogFilter
                {
                    Name = "PDF Documents",
                    Extensions = new List<string> { "pdf" }
                },
                new Services.FileDialogFilter
                {
                    Name = "All Files",
                    Extensions = new List<string> { "*" }
                }
            };

            var filePath = await fileDialogService.OpenFileAsync("Open PDF File", filters);

            if (!string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation("File selected from picker: {FilePath}", filePath);
                await OpenFileInTabAsync(filePath);
            }
            else
            {
                _logger.LogInformation("File picker cancelled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in new tab");
        }
    }

    /// <summary>
    /// Opens a file in a new tab or activates existing tab if file is already open.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to open.</param>
    private async Task OpenFileInTabAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        var operationId = $"open-file-{Guid.NewGuid():N}";
        _watchdog?.StartOperation(operationId, $"Open File: {Path.GetFileName(filePath)}", timeoutMs: 30000);

        LogInfo($"[1/8] Starting OpenFileInTabAsync for: {filePath}");

        // Check if file is already open in a tab
        var existingTab = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingTab != null)
        {
            LogInfo("[2/8] File already open in tab, activating existing tab");
            ActivateTab(existingTab);
            _watchdog?.CompleteOperation(operationId);
            return;
        }

        LogInfo($"[2/8] File not already open. Current tab count: {Tabs.Count}");

        TabViewModel? tabViewModel = null;
        try
        {
            LogInfo("[3/8] Requesting PdfViewerViewModel from DI container...");
            var viewerViewModel = _serviceProvider.GetRequiredService<PdfViewerViewModel>();
            LogInfo($"[3/8] PdfViewerViewModel created: {viewerViewModel != null}");

            LogInfo("[4/8] Requesting ILogger<TabViewModel> from DI container...");
            var tabLogger = _serviceProvider.GetRequiredService<ILogger<TabViewModel>>();
            LogInfo($"[4/8] TabLogger created: {tabLogger != null}");

            LogInfo($"[5/8] Creating TabViewModel with filePath: {filePath}");

            if (viewerViewModel == null)
            {
                throw new InvalidOperationException("ViewerViewModel is null");
            }
            if (tabLogger == null)
            {
                throw new InvalidOperationException("TabLogger is null");
            }

            tabViewModel = new TabViewModel(filePath, viewerViewModel, tabLogger);
            LogInfo($"[5/8] TabViewModel created successfully");

            LogInfo($"[6/8] Adding tab to collection (current count: {Tabs.Count})");
            Tabs.Add(tabViewModel);
            LogInfo($"[6/8] Tab added (new count: {Tabs.Count})");

            LogInfo("[6/8] Activating tab...");
            ActivateTab(tabViewModel);
            LogInfo($"[6/8] Tab activated. ActiveTab is now: {ActiveTab?.FilePath}");

            LogInfo($"[7/8] Calling LoadDocumentFromPathAsync for: {filePath}");
            await viewerViewModel.LoadDocumentFromPathAsync(filePath);
            LogInfo("[7/8] LoadDocumentFromPathAsync completed successfully!");

            LogInfo("[8/8] Adding to recent files...");
            _recentFilesService.AddRecentFile(filePath);

            LogInfo($"[8/8] SUCCESS! File opened in new tab: {filePath}");
            _watchdog?.CompleteOperation(operationId);
        }
        catch (Exception ex)
        {
            _watchdog?.FailOperation(operationId, ex);
            LogError($"[ERROR] CRITICAL: Failed to open file in tab: {filePath}", ex);
            LogError($"[ERROR] Exception Type: {ex.GetType().Name}");
            LogError($"[ERROR] Exception Message: {ex.Message}");
            LogError($"[ERROR] Stack Trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                LogError($"[ERROR] Inner Exception: {ex.InnerException.Message}");
                LogError($"[ERROR] Inner Stack Trace: {ex.InnerException.StackTrace}");
            }

            // Remove failed tab to prevent broken UI state
            if (tabViewModel != null && Tabs.Contains(tabViewModel))
            {
                LogInfo($"[CLEANUP] Removing failed tab. Count before: {Tabs.Count}");
                Tabs.Remove(tabViewModel);
                LogInfo($"[CLEANUP] Tab removed. Count after: {Tabs.Count}");
                tabViewModel.Dispose();
                LogInfo("[CLEANUP] Tab disposed");
            }

            // ENABLE ERROR DIALOG
            try
            {
                LogInfo("[UI] Attempting to show error dialog to user...");
                await ShowErrorDialogAsync("Error Opening File",
                    $"Failed to open {Path.GetFileName(filePath)}:\n\n{ex.Message}\n\nType: {ex.GetType().Name}");
                LogInfo("[UI] Error dialog shown successfully");
            }
            catch (Exception dialogEx)
            {
                LogError($"[UI] Failed to show error dialog: {dialogEx.Message}", dialogEx);
            }

            LogError("[FINAL] File opening failed. Check logs above for details.");
        }
    }

    /// <summary>
    /// Opens a file from the recent files list.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    [RelayCommand]
    private async Task OpenRecentFileAsync(string filePath)
    {
        _logger.LogInformation("OpenRecentFile command invoked for: {FilePath}", filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Recent file no longer exists: {FilePath}", filePath);
                _recentFilesService.RemoveRecentFile(filePath);
                return;
            }

            await OpenFileInTabAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open recent file: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Closes the specified tab.
    /// </summary>
    /// <param name="tab">The tab to close.</param>
    [RelayCommand]
    private void CloseTab(TabViewModel tab)
    {
        if (tab == null)
        {
            throw new ArgumentNullException(nameof(tab));
        }

        _logger.LogInformation("CloseTab command invoked for: {FilePath}", tab.FilePath);

        try
        {
            // Deactivate if this was the active tab
            if (ActiveTab == tab)
            {
                // Find another tab to activate
                var index = Tabs.IndexOf(tab);
                if (Tabs.Count > 1)
                {
                    // Activate the next tab, or previous if this was the last
                    var nextTab = index < Tabs.Count - 1 ? Tabs[index + 1] : Tabs[index - 1];
                    ActivateTab(nextTab);
                }
                else
                {
                    ActiveTab = null;
                }
            }

            // Remove and dispose
            Tabs.Remove(tab);
            tab.Dispose();

            _logger.LogInformation("Tab closed successfully: {FilePath}", tab.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close tab: {FilePath}", tab.FilePath);
        }
    }

    /// <summary>
    /// Activates the specified tab.
    /// </summary>
    /// <param name="tab">The tab to activate.</param>
    private void ActivateTab(TabViewModel tab)
    {
        if (tab == null)
        {
            throw new ArgumentNullException(nameof(tab));
        }

        _logger.LogInformation("Activating tab: {FilePath}", tab.FilePath);

        // Deactivate current active tab
        if (ActiveTab != null && ActiveTab != tab)
        {
            ActiveTab.Deactivate();
        }

        // Activate new tab
        ActiveTab = tab;
        tab.Activate();
    }

    /// <summary>
    /// Gets the list of recent files for binding in UI.
    /// </summary>
    public IReadOnlyList<Core.Models.RecentFileEntry> GetRecentFiles()
    {
        return _recentFilesService.GetRecentFiles();
    }

    /// <summary>
    /// Clears all recent files.
    /// </summary>
    [RelayCommand]
    private void ClearRecentFiles()
    {
        _logger.LogInformation("ClearRecentFiles command invoked");
        _recentFilesService.ClearRecentFiles();
    }

    /// <summary>
    /// Disposes resources used by the MainViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing MainViewModel");

        foreach (var tab in Tabs)
        {
            tab.Dispose();
        }

        Tabs.Clear();

        _disposed = true;
    }

    /// <summary>
    /// Shows an error dialog to the user.
    /// </summary>
    private async Task ShowErrorDialogAsync(string title, string message)
    {
        try
        {
            // TODO: Implement Avalonia dialog service
            _logger.LogError("Dialog: {Title} - {Message}", title, message);
            await Task.CompletedTask;

            // COMMENTED OUT - WinUI 3 implementation:
            // // Initial delay to let window fully initialize
            // await Task.Delay(200);
            //
            // // Wait for XamlRoot to be available with retries (up to 2 seconds)
            // Microsoft.UI.Xaml.XamlRoot? xamlRoot = null;
            // for (int i = 0; i < 20; i++)
            // {
            //     xamlRoot = App.MainWindow?.Content?.XamlRoot;
            //     if (xamlRoot != null)
            //         break;
            //
            //     await Task.Delay(100);
            // }
            //
            // if (xamlRoot == null)
            // {
            //     // XamlRoot not available, log and return
            //     System.Diagnostics.Debug.WriteLine($"Error: Unable to show dialog - XamlRoot not available after retries. Title: {title}, Message: {message}");
            //     return;
            // }
            //
            // var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            // {
            //     Title = title,
            //     Content = message,
            //     CloseButtonText = "OK",
            //     XamlRoot = xamlRoot
            // };
            //
            // await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // If dialog fails, at least log it
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}. Original error - Title: {title}, Message: {message}");
        }
    }
}
