using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Main view model for the FluentPDF application.
/// UI-framework agnostic implementation that manages tab collection.
/// </summary>
public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IThumbnailCacheService? _thumbnailCacheService;
    private readonly IDialogService? _dialogService;
    private readonly Func<PdfViewerViewModel> _viewerViewModelFactory;
    private readonly Func<string, PdfViewerViewModel, TabViewModel> _tabViewModelFactory;
    private bool _disposed;

    /// <summary>
    /// Gets the collection of open tabs.
    /// </summary>
    public ObservableCollection<TabViewModel> Tabs { get; }

    /// <summary>
    /// Gets the recent file cards for the empty state screen.
    /// </summary>
    public ObservableCollection<RecentFileCardViewModel> RecentFileCards { get; } = new();

    /// <summary>
    /// Gets whether there are recent files to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasRecentFiles;

    /// <summary>
    /// Gets or sets the currently active tab.
    /// </summary>
    [ObservableProperty]
    private TabViewModel? _activeTab;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        ILogger<MainViewModel> logger,
        IRecentFilesService recentFilesService,
        Func<PdfViewerViewModel> viewerViewModelFactory,
        Func<string, PdfViewerViewModel, TabViewModel> tabViewModelFactory,
        IDialogService? dialogService = null,
        IThumbnailCacheService? thumbnailCacheService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));
        _viewerViewModelFactory = viewerViewModelFactory ?? throw new ArgumentNullException(nameof(viewerViewModelFactory));
        _tabViewModelFactory = tabViewModelFactory ?? throw new ArgumentNullException(nameof(tabViewModelFactory));
        _dialogService = dialogService;
        _thumbnailCacheService = thumbnailCacheService;

        Tabs = new ObservableCollection<TabViewModel>();

        _logger.LogInformation("MainViewModel initialized");
    }

    /// <summary>
    /// Opens a file picker and creates a new tab.
    /// </summary>
    [RelayCommand]
    private async Task OpenFileInNewTabAsync()
    {
        _logger.LogInformation("OpenFileInNewTab command invoked");

        if (_dialogService == null)
        {
            _logger.LogWarning("DialogService not available, cannot open file picker");
            return;
        }

        try
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync(
                "Open PDF Document",
                new[] { ".pdf" });

            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation("File picker cancelled");
                return;
            }

            await OpenFileInTabAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in new tab");
        }
    }

    /// <summary>
    /// Opens a file in a new tab or activates existing tab if already open.
    /// </summary>
    public async Task OpenFileInTabAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        _logger.LogInformation("Opening file in tab: {FilePath}", filePath);

        // Check if file is already open
        var existingTab = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existingTab != null)
        {
            _logger.LogInformation("File already open in tab, activating existing tab");
            ActivateTab(existingTab);
            return;
        }

        TabViewModel? tabViewModel = null;
        try
        {
            var viewerViewModel = _viewerViewModelFactory();
            tabViewModel = _tabViewModelFactory(filePath, viewerViewModel);

            Tabs.Add(tabViewModel);
            ActivateTab(tabViewModel);

            _logger.LogInformation("Loading document from path: {FilePath}", filePath);
            await viewerViewModel.LoadDocumentFromPathAsync(filePath);

            _recentFilesService.AddRecentFile(filePath);

            _logger.LogInformation("Successfully opened file in new tab: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in tab: {FilePath}", filePath);

            if (tabViewModel != null && Tabs.Contains(tabViewModel))
            {
                Tabs.Remove(tabViewModel);
                tabViewModel.Dispose();
            }
        }
    }

    /// <summary>
    /// Opens a file from the recent files list.
    /// </summary>
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
            if (ActiveTab == tab)
            {
                var index = Tabs.IndexOf(tab);
                if (Tabs.Count > 1)
                {
                    var nextTab = index < Tabs.Count - 1 ? Tabs[index + 1] : Tabs[index - 1];
                    ActivateTab(nextTab);
                }
                else
                {
                    ActiveTab = null;
                }
            }

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
    public void ActivateTab(TabViewModel tab)
    {
        if (tab == null)
        {
            throw new ArgumentNullException(nameof(tab));
        }

        _logger.LogInformation("Activating tab: {FilePath}", tab.FilePath);

        if (ActiveTab != null && ActiveTab != tab)
        {
            ActiveTab.Deactivate();
        }

        ActiveTab = tab;
        tab.Activate();
    }

    /// <summary>
    /// Gets the list of recent files.
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
        RecentFileCards.Clear();
        HasRecentFiles = false;
    }

    /// <summary>
    /// Refreshes the recent file cards with thumbnails for the empty state.
    /// </summary>
    [RelayCommand]
    public async Task RefreshRecentFileCardsAsync()
    {
        var recentFiles = _recentFilesService.GetRecentFiles();

        RecentFileCards.Clear();

        foreach (var entry in recentFiles)
        {
            if (!File.Exists(entry.FilePath)) continue;
            var card = new RecentFileCardViewModel(entry.FilePath, entry.LastAccessed);

            // Set cached thumbnail immediately if available
            var cached = _thumbnailCacheService?.GetCachedThumbnailPath(entry.FilePath);
            if (cached != null)
            {
                card.ThumbnailPath = cached;
                card.IsThumbnailLoading = false;
            }

            RecentFileCards.Add(card);
        }

        HasRecentFiles = RecentFileCards.Count > 0;

        // Generate missing thumbnails in the background
        foreach (var card in RecentFileCards.ToList())
        {
            if (card.ThumbnailPath != null) continue;
            try
            {
                var path = await (_thumbnailCacheService?.GenerateThumbnailAsync(card.FilePath)
                    ?? Task.FromResult<string?>(null));
                card.ThumbnailPath = path;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to generate thumbnail for {Path}", card.FilePath);
            }
            finally
            {
                card.IsThumbnailLoading = false;
            }
        }

        // Prune stale thumbnails
        _thumbnailCacheService?.PruneStaleThumbnails(
            RecentFileCards.Select(c => c.FilePath));
    }

    /// <summary>
    /// Disposes resources.
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
