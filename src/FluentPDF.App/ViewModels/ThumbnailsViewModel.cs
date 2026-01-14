using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentPDF.App.Models;
using FluentPDF.App.Views;
using FluentPDF.Core.Caching;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// Message sent when navigating to a specific page via thumbnail click.
/// </summary>
/// <param name="PageNumber">The 1-based page number to navigate to.</param>
public record NavigateToPageMessage(int PageNumber);

/// <summary>
/// Message sent to raise an accessibility notification for screen readers.
/// </summary>
/// <param name="Message">The message to announce to screen readers.</param>
public record AccessibilityNotificationMessage(string Message);

/// <summary>
/// Message sent when pages have been modified (rotated, deleted, reordered, or inserted).
/// </summary>
public record PageModifiedMessage();

/// <summary>
/// ViewModel for the thumbnails sidebar.
/// Manages thumbnail state, caching, and navigation.
/// </summary>
public partial class ThumbnailsViewModel : ObservableObject, IDisposable
{
    private const int MaxConcurrentRenders = 4;
    private const int PriorityRange = 5; // Load current page ± 5 pages first
    private const long MaxCacheMemoryBytes = 50 * 1024 * 1024; // 50 MB
    private const int MinCacheCapacity = 20;
    private const int MaxCacheCapacity = 100;

    private readonly IThumbnailRenderingService _thumbnailService;
    private readonly IPageOperationsService _pageOperationsService;
    private readonly ILogger<ThumbnailsViewModel> _logger;
    private readonly LruCache<int, DisposableBitmapImage> _cache;
    private readonly SemaphoreSlim _renderSemaphore;
    private PdfDocument? _document;
    private bool _disposed;
    private long _estimatedCacheMemory;

    /// <summary>
    /// Gets the collection of thumbnail items.
    /// </summary>
    public ObservableCollection<ThumbnailItem> Thumbnails { get; }

    /// <summary>
    /// Gets the collection of currently selected thumbnails.
    /// </summary>
    public IEnumerable<ThumbnailItem> SelectedThumbnails => Thumbnails.Where(t => t.IsSelected);

    /// <summary>
    /// Gets or sets the currently selected page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _selectedPageNumber = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailsViewModel"/> class.
    /// </summary>
    /// <param name="thumbnailService">Service for rendering thumbnails.</param>
    /// <param name="pageOperationsService">Service for page operations.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public ThumbnailsViewModel(
        IThumbnailRenderingService thumbnailService,
        IPageOperationsService pageOperationsService,
        ILogger<ThumbnailsViewModel> logger)
    {
        _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
        _pageOperationsService = pageOperationsService ?? throw new ArgumentNullException(nameof(pageOperationsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Thumbnails = new ObservableCollection<ThumbnailItem>();
        _cache = new LruCache<int, DisposableBitmapImage>(MaxCacheCapacity);
        _renderSemaphore = new SemaphoreSlim(MaxConcurrentRenders, MaxConcurrentRenders);
        _estimatedCacheMemory = 0;

        _logger.LogInformation("ThumbnailsViewModel initialized with max cache capacity {Capacity}", MaxCacheCapacity);
    }

    /// <summary>
    /// Loads thumbnails for the specified PDF document.
    /// </summary>
    /// <param name="document">The PDF document to load thumbnails for.</param>
    public async Task LoadThumbnailsAsync(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        _document = document;

        // Clear existing thumbnails
        Thumbnails.Clear();

        // Create thumbnail items for all pages
        for (int i = 1; i <= document.PageCount; i++)
        {
            var item = new ThumbnailItem(i);
            item.PropertyChanged += OnThumbnailItemPropertyChanged;
            Thumbnails.Add(item);
        }

        _logger.LogInformation("Created {Count} thumbnail items for document", document.PageCount);

        // Load first 20 thumbnails with priority (current page neighborhood first)
        await LoadPriorityThumbnailsAsync(1, Math.Min(20, document.PageCount));
        _logger.LogInformation("Loaded initial thumbnails");
    }

    /// <summary>
    /// Loads a single thumbnail for the specified item.
    /// </summary>
    /// <param name="item">The thumbnail item to load.</param>
    private async Task LoadThumbnailAsync(ThumbnailItem item)
    {
        if (_document == null)
        {
            return;
        }

        // Check cache first
        if (_cache.TryGet(item.PageNumber, out var cachedImage) && cachedImage != null)
        {
            item.Thumbnail = cachedImage.Image;
            item.IsLoading = false;
            return;
        }

        await _renderSemaphore.WaitAsync();
        try
        {
            var result = await _thumbnailService.RenderThumbnailAsync(_document, item.PageNumber);

            if (result.IsSuccess && result.Value != null)
            {
                var bitmap = new BitmapImage();
                var randomAccessStream = await ConvertStreamToRandomAccessStreamAsync(result.Value);
                await bitmap.SetSourceAsync(randomAccessStream);

                item.Thumbnail = bitmap;
                item.IsLoading = false;

                // Cache the thumbnail and track memory
                var disposableImage = new DisposableBitmapImage(bitmap);
                _cache.Add(item.PageNumber, disposableImage);

                // Estimate memory usage (typical thumbnail ~150x200 pixels, 4 bytes per pixel)
                var estimatedSize = 150 * 200 * 4;
                _estimatedCacheMemory += estimatedSize;

                // Check memory and adjust cache if needed
                MonitorCacheMemory();

                _logger.LogDebug("Loaded thumbnail for page {PageNumber}, estimated cache memory: {Memory} MB",
                    item.PageNumber, _estimatedCacheMemory / (1024.0 * 1024.0));
            }
            else
            {
                item.IsLoading = false;
                _logger.LogWarning("Failed to render thumbnail for page {PageNumber}: {Errors}",
                    item.PageNumber, string.Join(", ", result.Errors.Select(e => e.Message)));
            }
        }
        catch (Exception ex)
        {
            item.IsLoading = false;
            _logger.LogError(ex, "Error loading thumbnail for page {PageNumber}", item.PageNumber);
        }
        finally
        {
            _renderSemaphore.Release();
        }
    }

    /// <summary>
    /// Loads thumbnails for the specified range of pages.
    /// Uses priority loading to load pages near the current selection first.
    /// </summary>
    /// <param name="startIndex">The 0-based start index.</param>
    /// <param name="endIndex">The 0-based end index (exclusive).</param>
    public async Task LoadVisibleThumbnailsAsync(int startIndex, int endIndex)
    {
        if (_document == null)
        {
            return;
        }

        // Use priority loading with current page as center
        var currentPage = SelectedPageNumber;
        var visibleCount = endIndex - startIndex;

        // Load with priority around current page within visible range
        await LoadPriorityRangeThumbnailsAsync(currentPage, startIndex, endIndex);
    }

    /// <summary>
    /// Loads thumbnails in a specific range with priority around the current page.
    /// </summary>
    /// <param name="currentPage">The current page number (1-based).</param>
    /// <param name="startIndex">The 0-based start index.</param>
    /// <param name="endIndex">The 0-based end index (exclusive).</param>
    private async Task LoadPriorityRangeThumbnailsAsync(int currentPage, int startIndex, int endIndex)
    {
        if (_document == null)
        {
            return;
        }

        var loadTasks = new List<Task>();
        var loaded = new HashSet<int>();

        // Priority 1: Load current page and neighbors within visible range
        var priorityStart = Math.Max(startIndex, currentPage - 1 - PriorityRange);
        var priorityEnd = Math.Min(endIndex - 1, currentPage - 1 + PriorityRange);

        for (int i = priorityStart; i <= priorityEnd && i < Thumbnails.Count; i++)
        {
            var item = Thumbnails[i];
            if (item.Thumbnail == null)
            {
                loadTasks.Add(LoadThumbnailAsync(item));
                loaded.Add(i);
            }
        }

        // Priority 2: Load remaining visible thumbnails
        for (int i = startIndex; i < endIndex && i < Thumbnails.Count; i++)
        {
            if (!loaded.Contains(i))
            {
                var item = Thumbnails[i];
                if (item.Thumbnail == null)
                {
                    loadTasks.Add(LoadThumbnailAsync(item));
                }
            }
        }

        await Task.WhenAll(loadTasks);
        _logger.LogDebug("Loaded thumbnails in range {Start}-{End} with priority around page {Page}",
            startIndex, endIndex, currentPage);
    }

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number to navigate to.</param>
    [RelayCommand(CanExecute = nameof(CanNavigateToPage))]
    private void NavigateToPage(int pageNumber)
    {
        if (pageNumber < 1 || (_document != null && pageNumber > _document.PageCount))
        {
            return;
        }

        SelectedPageNumber = pageNumber;

        // Update selection state
        foreach (var item in Thumbnails)
        {
            item.IsSelected = item.PageNumber == pageNumber;
        }

        // Send message to PdfViewerViewModel
        WeakReferenceMessenger.Default.Send(new NavigateToPageMessage(pageNumber));

        _logger.LogDebug("Navigating to page {PageNumber} via thumbnail click", pageNumber);
    }

    /// <summary>
    /// Determines whether navigation to a page can execute.
    /// Navigation is disabled when any thumbnail is currently loading.
    /// </summary>
    private bool CanNavigateToPage(int pageNumber)
    {
        // Check if any thumbnail is currently being loaded
        return !Thumbnails.Any(t => t.IsLoading);
    }

    /// <summary>
    /// Updates the selected page number from external navigation (e.g., PdfViewerViewModel).
    /// </summary>
    /// <param name="pageNumber">The 1-based page number that is now current.</param>
    public void UpdateSelectedPage(int pageNumber)
    {
        if (SelectedPageNumber == pageNumber)
        {
            return; // Prevent navigation loops
        }

        SelectedPageNumber = pageNumber;

        // Update selection state
        foreach (var item in Thumbnails)
        {
            item.IsSelected = item.PageNumber == pageNumber;
        }

        _logger.LogDebug("Updated selected thumbnail to page {PageNumber}", pageNumber);
    }

    /// <summary>
    /// Handles property changes on thumbnail items to update command states.
    /// </summary>
    private void OnThumbnailItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThumbnailItem.IsLoading))
        {
            // Update CanExecute state when loading state changes
            NavigateToPageCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Loads thumbnails with priority for pages near the current page.
    /// </summary>
    /// <param name="currentPage">The current page number (1-based).</param>
    /// <param name="visibleCount">Total number of thumbnails to load.</param>
    private async Task LoadPriorityThumbnailsAsync(int currentPage, int visibleCount)
    {
        if (_document == null)
        {
            return;
        }

        var loadTasks = new List<Task>();
        var loaded = new HashSet<int>();

        // Priority 1: Load current page and immediate neighbors (± PriorityRange pages)
        var priorityStart = Math.Max(1, currentPage - PriorityRange);
        var priorityEnd = Math.Min(_document.PageCount, currentPage + PriorityRange);

        for (int i = priorityStart; i <= priorityEnd && i <= visibleCount; i++)
        {
            if (i >= 1 && i <= Thumbnails.Count)
            {
                var item = Thumbnails[i - 1];
                loadTasks.Add(LoadThumbnailAsync(item));
                loaded.Add(i);
            }
        }

        // Priority 2: Load remaining visible thumbnails
        for (int i = 1; i <= visibleCount && i <= Thumbnails.Count; i++)
        {
            if (!loaded.Contains(i))
            {
                var item = Thumbnails[i - 1];
                loadTasks.Add(LoadThumbnailAsync(item));
            }
        }

        await Task.WhenAll(loadTasks);
        _logger.LogDebug("Loaded {Count} thumbnails with priority around page {Page}", loaded.Count, currentPage);
    }

    /// <summary>
    /// Monitors cache memory usage and adjusts capacity if needed.
    /// </summary>
    private void MonitorCacheMemory()
    {
        if (_estimatedCacheMemory > MaxCacheMemoryBytes)
        {
            // Memory exceeded - we need to reduce cache capacity
            // The LruCache will automatically evict items when new ones are added
            _logger.LogWarning("Cache memory exceeded {Current} MB / {Max} MB, items will be evicted",
                _estimatedCacheMemory / (1024.0 * 1024.0),
                MaxCacheMemoryBytes / (1024.0 * 1024.0));

            // Reduce estimated memory (the cache eviction will handle actual cleanup)
            // We'll recalibrate on the next add
            var itemSize = 150 * 200 * 4;
            var itemsToRemove = (int)((_estimatedCacheMemory - MaxCacheMemoryBytes) / itemSize) + 1;
            _estimatedCacheMemory -= itemsToRemove * itemSize;

            if (_estimatedCacheMemory < 0)
            {
                _estimatedCacheMemory = 0;
            }
        }
    }

    /// <summary>
    /// Converts a Stream to IRandomAccessStream for WinUI BitmapImage.
    /// </summary>
    private static async Task<IRandomAccessStream> ConvertStreamToRandomAccessStreamAsync(Stream stream)
    {
        var randomAccessStream = new InMemoryRandomAccessStream();
        using (var outputStream = randomAccessStream.GetOutputStreamAt(0))
        {
            await stream.CopyToAsync(outputStream.AsStreamForWrite());
            await outputStream.FlushAsync();
        }
        randomAccessStream.Seek(0);
        return randomAccessStream;
    }

    /// <summary>
    /// Rotates selected pages 90 degrees clockwise.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedThumbnails))]
    private async Task RotateRightAsync()
    {
        if (_document == null) return;

        var selectedIndices = SelectedThumbnails.Select(t => t.PageNumber - 1).ToArray();

        _logger.LogInformation("Rotating {Count} pages right 90°", selectedIndices.Length);

        var result = await _pageOperationsService.RotatePagesAsync(_document, selectedIndices, RotationAngle.Rotate90);

        if (result.IsSuccess)
        {
            await RefreshThumbnailsAsync(selectedIndices);
            NotifyPageModification();
            _logger.LogInformation("Successfully rotated {Count} pages right", selectedIndices.Length);
        }
        else
        {
            _logger.LogError("Failed to rotate pages: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>
    /// Rotates selected pages 90 degrees counter-clockwise.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedThumbnails))]
    private async Task RotateLeftAsync()
    {
        if (_document == null) return;

        var selectedIndices = SelectedThumbnails.Select(t => t.PageNumber - 1).ToArray();

        _logger.LogInformation("Rotating {Count} pages left 90°", selectedIndices.Length);

        var result = await _pageOperationsService.RotatePagesAsync(_document, selectedIndices, RotationAngle.Rotate270);

        if (result.IsSuccess)
        {
            await RefreshThumbnailsAsync(selectedIndices);
            NotifyPageModification();
            _logger.LogInformation("Successfully rotated {Count} pages left", selectedIndices.Length);
        }
        else
        {
            _logger.LogError("Failed to rotate pages: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>
    /// Rotates selected pages 180 degrees.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedThumbnails))]
    private async Task Rotate180Async()
    {
        if (_document == null) return;

        var selectedIndices = SelectedThumbnails.Select(t => t.PageNumber - 1).ToArray();

        _logger.LogInformation("Rotating {Count} pages 180°", selectedIndices.Length);

        var result = await _pageOperationsService.RotatePagesAsync(_document, selectedIndices, RotationAngle.Rotate180);

        if (result.IsSuccess)
        {
            await RefreshThumbnailsAsync(selectedIndices);
            NotifyPageModification();
            _logger.LogInformation("Successfully rotated {Count} pages 180°", selectedIndices.Length);
        }
        else
        {
            _logger.LogError("Failed to rotate pages: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>
    /// Deletes selected pages with confirmation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedThumbnails))]
    private async Task DeletePagesAsync()
    {
        if (_document == null) return;

        var selectedIndices = SelectedThumbnails.Select(t => t.PageNumber - 1).ToArray();
        var selectedCount = selectedIndices.Length;

        _logger.LogInformation("Attempting to delete {Count} pages", selectedCount);

        // Show confirmation dialog
        var confirmed = await DeletePagesDialog.ShowAsync(
            App.MainWindow.Content.XamlRoot,
            selectedCount,
            _document.PageCount);

        if (!confirmed)
        {
            _logger.LogDebug("Page deletion cancelled by user");
            return;
        }

        var result = await _pageOperationsService.DeletePagesAsync(_document, selectedIndices);

        if (result.IsSuccess)
        {
            await ReloadAllThumbnailsAsync();
            NotifyPageModification();
            _logger.LogInformation("Successfully deleted {Count} pages", selectedCount);
        }
        else
        {
            _logger.LogError("Failed to delete pages: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>
    /// Inserts a blank page at the current position.
    /// </summary>
    /// <param name="pageSize">The size of the blank page to insert.</param>
    [RelayCommand]
    private async Task InsertBlankPageAsync(PageSize pageSize)
    {
        if (_document == null) return;

        var insertIndex = SelectedPageNumber - 1; // Convert to 0-based

        _logger.LogInformation("Inserting blank page at index {Index} with size {Size}", insertIndex, pageSize);

        var result = await _pageOperationsService.InsertBlankPageAsync(_document, insertIndex, pageSize);

        if (result.IsSuccess)
        {
            await ReloadAllThumbnailsAsync();
            NotifyPageModification();
            _logger.LogInformation("Successfully inserted blank page at index {Index}", insertIndex);
        }
        else
        {
            _logger.LogError("Failed to insert blank page: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>
    /// Moves selected pages to a target position for drag-drop reordering.
    /// </summary>
    /// <param name="pageIndices">Zero-based indices of pages to move.</param>
    /// <param name="targetIndex">Zero-based target position.</param>
    public async Task MovePagesTo(int[] pageIndices, int targetIndex)
    {
        if (_document == null) return;

        _logger.LogInformation("Moving {Count} pages to index {Target}", pageIndices.Length, targetIndex);

        var result = await _pageOperationsService.ReorderPagesAsync(_document, pageIndices, targetIndex);

        if (result.IsSuccess)
        {
            await ReloadAllThumbnailsAsync();
            NotifyPageModification();
            _logger.LogInformation("Successfully moved pages to index {Target}", targetIndex);
        }
        else
        {
            _logger.LogError("Failed to reorder pages: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    /// <summary>
    /// Selects all thumbnails.
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Thumbnails)
        {
            item.IsSelected = true;
        }
        _logger.LogDebug("Selected all {Count} thumbnails", Thumbnails.Count);
    }

    /// <summary>
    /// Determines whether any thumbnails are selected.
    /// </summary>
    private bool HasSelectedThumbnails()
    {
        return SelectedThumbnails.Any();
    }

    /// <summary>
    /// Refreshes thumbnails for the specified page indices.
    /// </summary>
    /// <param name="pageIndices">Zero-based page indices to refresh.</param>
    private async Task RefreshThumbnailsAsync(int[] pageIndices)
    {
        if (_document == null) return;

        var refreshTasks = new List<Task>();

        foreach (var index in pageIndices)
        {
            if (index >= 0 && index < Thumbnails.Count)
            {
                var item = Thumbnails[index];

                // Remove from cache to force reload
                _cache.Remove(item.PageNumber);

                // Clear existing thumbnail
                item.Thumbnail = null;
                item.IsLoading = true;

                // Reload thumbnail
                refreshTasks.Add(LoadThumbnailAsync(item));
            }
        }

        await Task.WhenAll(refreshTasks);
        _logger.LogDebug("Refreshed {Count} thumbnails", pageIndices.Length);
    }

    /// <summary>
    /// Reloads all thumbnails after structural changes like delete or insert.
    /// </summary>
    private async Task ReloadAllThumbnailsAsync()
    {
        if (_document == null) return;

        // Clear cache and thumbnails
        _cache.Clear();
        _estimatedCacheMemory = 0;
        Thumbnails.Clear();

        // Reload thumbnails for new page count
        for (int i = 1; i <= _document.PageCount; i++)
        {
            var item = new ThumbnailItem(i);
            item.PropertyChanged += OnThumbnailItemPropertyChanged;
            Thumbnails.Add(item);
        }

        // Load visible thumbnails
        await LoadPriorityThumbnailsAsync(SelectedPageNumber, Math.Min(20, _document.PageCount));

        _logger.LogInformation("Reloaded all thumbnails, new count: {Count}", _document.PageCount);
    }

    /// <summary>
    /// Notifies that page modifications have occurred.
    /// </summary>
    private void NotifyPageModification()
    {
        // Send message to notify PdfViewerViewModel about changes
        // This will be used to set HasUnsavedChanges
        WeakReferenceMessenger.Default.Send(new PageModifiedMessage());
    }

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Unsubscribe from thumbnail item events
        foreach (var item in Thumbnails)
        {
            item.PropertyChanged -= OnThumbnailItemPropertyChanged;
        }

        _cache.Dispose();
        _renderSemaphore.Dispose();
        _disposed = true;

        _logger.LogInformation("ThumbnailsViewModel disposed");
    }
}
