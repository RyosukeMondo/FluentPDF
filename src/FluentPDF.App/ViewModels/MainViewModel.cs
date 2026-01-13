using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Windows.Storage.Pickers;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// Main view model for the FluentPDF application.
/// Manages tab collection and coordinates between tabs and recent files service.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking view model operations.</param>
    /// <param name="recentFilesService">Service for managing recent files.</param>
    /// <param name="serviceProvider">Service provider for creating tab dependencies.</param>
    public MainViewModel(
        ILogger<MainViewModel> logger,
        IRecentFilesService recentFilesService,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        Tabs = new ObservableCollection<TabViewModel>();

        _logger.LogInformation("MainViewModel initialized");
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
        var debugLog = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log");
        System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: OpenFileInNewTab command invoked\n");
        _logger.LogInformation("OpenFileInNewTab command invoked");

        try
        {
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: Creating FileOpenPicker\n");
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: Getting window handle\n");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: Initializing picker with window handle\n");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: Showing file picker\n");
            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: File picker cancelled\n");
                _logger.LogInformation("File picker cancelled");
                return;
            }

            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: File selected: {file.Path}\n");
            await OpenFileInTabAsync(file.Path);
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: ERROR: {ex.Message}\n{ex.StackTrace}\n");
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

        _logger.LogInformation("Opening file in tab: {FilePath}", filePath);

        // Check if file is already open in a tab
        var existingTab = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingTab != null)
        {
            _logger.LogInformation("File already open in tab, activating existing tab");
            ActivateTab(existingTab);
            return;
        }

        try
        {
            // Create PdfViewerViewModel for the new tab
            var viewerViewModel = _serviceProvider.GetRequiredService<PdfViewerViewModel>();

            // Create TabViewModel
            var tabLogger = _serviceProvider.GetRequiredService<ILogger<TabViewModel>>();
            var tabViewModel = new TabViewModel(filePath, viewerViewModel, tabLogger);

            // Add tab and activate it
            Tabs.Add(tabViewModel);
            ActivateTab(tabViewModel);

            // Load the document
            await viewerViewModel.OpenDocumentCommand.ExecuteAsync(null);

            // Add to recent files
            _recentFilesService.AddRecentFile(filePath);

            _logger.LogInformation("Successfully opened file in new tab: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in tab: {FilePath}", filePath);
            throw;
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
}
