using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Represents a single thumbnail item.
/// </summary>
public partial class ThumbnailItem : ObservableObject
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets or sets the thumbnail image (framework-specific type stored as object).
    /// </summary>
    [ObservableProperty]
    private object? _thumbnail;

    /// <summary>
    /// Gets or sets a value indicating whether the thumbnail is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>
    /// Gets or sets a value indicating whether this thumbnail is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailItem"/> class.
    /// </summary>
    public ThumbnailItem(int pageNumber)
    {
        PageNumber = pageNumber;
    }
}

/// <summary>
/// View model for the thumbnails sidebar.
/// UI-framework agnostic implementation.
/// </summary>
public partial class ThumbnailsViewModel : ViewModelBase, IDisposable
{
    private readonly IThumbnailRenderingService _thumbnailService;
    private readonly IPageOperationsService? _pageOperationsService;
    private readonly ILogger<ThumbnailsViewModel> _logger;
    private readonly IDispatcherService? _dispatcherService;
    private PdfDocument? _document;
    private bool _disposed;

    /// <summary>
    /// Callback to render a thumbnail. Must be set by the UI framework.
    /// Takes (document, pageNumber) and returns the framework-specific image.
    /// </summary>
    public Func<PdfDocument, int, Task<object?>>? RenderThumbnailCallback { get; set; }

    /// <summary>
    /// Callback for page operations (rotate, delete) using PDFium directly.
    /// Signature: (PdfDocument doc, int pageIndex, string operation) => Task&lt;bool&gt;
    /// Operations: "rotate_cw", "rotate_ccw", "delete"
    /// </summary>
    public Func<PdfDocument, int, string, Task<bool>>? PageOperationCallback { get; set; }

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
    /// Gets or sets whether the thumbnails sidebar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailsViewModel"/> class.
    /// </summary>
    public ThumbnailsViewModel(
        IThumbnailRenderingService thumbnailService,
        ILogger<ThumbnailsViewModel> logger,
        IPageOperationsService? pageOperationsService = null,
        IDispatcherService? dispatcherService = null)
    {
        _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pageOperationsService = pageOperationsService;
        _dispatcherService = dispatcherService;

        Thumbnails = new ObservableCollection<ThumbnailItem>();

        _logger.LogInformation("ThumbnailsViewModel initialized");
    }

    /// <summary>
    /// Loads thumbnails for the specified PDF document.
    /// </summary>
    public async Task LoadThumbnailsAsync(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        _document = document;
        Thumbnails.Clear();

        for (int i = 1; i <= document.PageCount; i++)
        {
            var item = new ThumbnailItem(i);
            item.PropertyChanged += OnThumbnailItemPropertyChanged;
            Thumbnails.Add(item);
        }

        _logger.LogInformation("Created {Count} thumbnail items for document", document.PageCount);

        // Load first batch of thumbnails
        await LoadPriorityThumbnailsAsync(1, Math.Min(20, document.PageCount));
    }

    private async Task LoadPriorityThumbnailsAsync(int currentPage, int count)
    {
        if (_document == null || RenderThumbnailCallback == null)
        {
            return;
        }

        var loadTasks = new List<Task>();
        var priorityRange = 5;

        // Priority 1: Load current page and neighbors
        var priorityStart = Math.Max(1, currentPage - priorityRange);
        var priorityEnd = Math.Min(_document.PageCount, currentPage + priorityRange);

        for (int i = priorityStart; i <= priorityEnd && i <= count; i++)
        {
            if (i >= 1 && i <= Thumbnails.Count)
            {
                loadTasks.Add(LoadSingleThumbnailAsync(Thumbnails[i - 1]));
            }
        }

        // Priority 2: Load remaining
        for (int i = 1; i <= count && i <= Thumbnails.Count; i++)
        {
            if (i < priorityStart || i > priorityEnd)
            {
                loadTasks.Add(LoadSingleThumbnailAsync(Thumbnails[i - 1]));
            }
        }

        await Task.WhenAll(loadTasks);
    }

    private async Task LoadSingleThumbnailAsync(ThumbnailItem item)
    {
        if (_document == null || RenderThumbnailCallback == null)
        {
            return;
        }

        try
        {
            var thumbnail = await RenderThumbnailCallback(_document, item.PageNumber);

            InvokeOnUIThread(() =>
            {
                item.Thumbnail = thumbnail;
                item.IsLoading = false;
            });

            _logger.LogDebug("Loaded thumbnail for page {PageNumber}", item.PageNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading thumbnail for page {PageNumber}", item.PageNumber);
            InvokeOnUIThread(() => item.IsLoading = false);
        }
    }

    /// <summary>
    /// Loads thumbnails for the specified visible range.
    /// </summary>
    public async Task LoadVisibleThumbnailsAsync(int startIndex, int endIndex)
    {
        if (_document == null || RenderThumbnailCallback == null)
        {
            return;
        }

        var loadTasks = new List<Task>();

        for (int i = startIndex; i < endIndex && i < Thumbnails.Count; i++)
        {
            var item = Thumbnails[i];
            if (item.Thumbnail == null)
            {
                loadTasks.Add(LoadSingleThumbnailAsync(item));
            }
        }

        await Task.WhenAll(loadTasks);
    }

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateToPage))]
    private void NavigateToPage(int pageNumber)
    {
        if (pageNumber < 1 || (_document != null && pageNumber > _document.PageCount))
        {
            return;
        }

        SelectedPageNumber = pageNumber;

        foreach (var item in Thumbnails)
        {
            item.IsSelected = item.PageNumber == pageNumber;
        }

        WeakReferenceMessenger.Default.Send(new NavigateToPageMessage(pageNumber));

        _logger.LogDebug("Navigating to page {PageNumber} via thumbnail click", pageNumber);
    }

    private bool CanNavigateToPage(int pageNumber)
    {
        return _document != null && pageNumber >= 1 && pageNumber <= _document.PageCount;
    }

    /// <summary>
    /// Updates the selected page from external navigation.
    /// </summary>
    public void UpdateSelectedPage(int pageNumber)
    {
        if (SelectedPageNumber == pageNumber)
        {
            return;
        }

        SelectedPageNumber = pageNumber;

        foreach (var item in Thumbnails)
        {
            item.IsSelected = item.PageNumber == pageNumber;
        }

        _logger.LogDebug("Updated selected thumbnail to page {PageNumber}", pageNumber);
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
    /// Rotates selected pages 90 degrees clockwise.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedThumbnails))]
    private async Task RotateRightAsync()
    {
        if (_document == null) return;

        var selectedIndices = SelectedThumbnails.Select(t => t.PageNumber - 1).ToArray();
        _logger.LogInformation("Rotating {Count} pages right 90 degrees", selectedIndices.Length);

        if (PageOperationCallback != null)
        {
            var allSuccess = true;
            foreach (var idx in selectedIndices)
            {
                if (!await PageOperationCallback(_document, idx, "rotate_cw"))
                    allSuccess = false;
            }
            if (allSuccess)
            {
                await RefreshThumbnailsAsync(selectedIndices);
                NotifyPageModification();
            }
        }
        else if (_pageOperationsService != null)
        {
            var result = await _pageOperationsService.RotatePagesAsync(_document, selectedIndices, RotationAngle.Rotate90);
            if (result.IsSuccess)
            {
                await RefreshThumbnailsAsync(selectedIndices);
                NotifyPageModification();
            }
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
        _logger.LogInformation("Rotating {Count} pages left 90 degrees", selectedIndices.Length);

        if (PageOperationCallback != null)
        {
            var allSuccess = true;
            foreach (var idx in selectedIndices)
            {
                if (!await PageOperationCallback(_document, idx, "rotate_ccw"))
                    allSuccess = false;
            }
            if (allSuccess)
            {
                await RefreshThumbnailsAsync(selectedIndices);
                NotifyPageModification();
            }
        }
        else if (_pageOperationsService != null)
        {
            var result = await _pageOperationsService.RotatePagesAsync(_document, selectedIndices, RotationAngle.Rotate270);
            if (result.IsSuccess)
            {
                await RefreshThumbnailsAsync(selectedIndices);
                NotifyPageModification();
            }
        }
    }

    /// <summary>
    /// Deletes selected pages.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedThumbnails))]
    private async Task DeletePagesAsync()
    {
        if (_document == null) return;

        var selectedIndices = SelectedThumbnails.Select(t => t.PageNumber - 1).OrderByDescending(i => i).ToArray();
        _logger.LogInformation("Deleting {Count} pages", selectedIndices.Length);

        if (PageOperationCallback != null)
        {
            var allSuccess = true;
            // Delete from highest index to lowest to avoid index shifting
            foreach (var idx in selectedIndices)
            {
                if (!await PageOperationCallback(_document, idx, "delete"))
                    allSuccess = false;
            }
            if (allSuccess)
            {
                await ReloadAllThumbnailsAsync();
                NotifyPageModification();
            }
        }
        else if (_pageOperationsService != null)
        {
            var result = await _pageOperationsService.DeletePagesAsync(_document, selectedIndices);
            if (result.IsSuccess)
            {
                await ReloadAllThumbnailsAsync();
                NotifyPageModification();
            }
        }
    }

    private bool HasSelectedThumbnails()
    {
        return SelectedThumbnails.Any();
    }

    public async Task RefreshThumbnailsAsync(int[] pageIndices)
    {
        if (_document == null || RenderThumbnailCallback == null) return;

        var refreshTasks = new List<Task>();

        foreach (var index in pageIndices)
        {
            if (index >= 0 && index < Thumbnails.Count)
            {
                var item = Thumbnails[index];
                item.Thumbnail = null;
                item.IsLoading = true;
                refreshTasks.Add(LoadSingleThumbnailAsync(item));
            }
        }

        await Task.WhenAll(refreshTasks);
    }

    private async Task ReloadAllThumbnailsAsync()
    {
        if (_document == null) return;

        Thumbnails.Clear();

        for (int i = 1; i <= _document.PageCount; i++)
        {
            var item = new ThumbnailItem(i);
            item.PropertyChanged += OnThumbnailItemPropertyChanged;
            Thumbnails.Add(item);
        }

        await LoadPriorityThumbnailsAsync(SelectedPageNumber, Math.Min(20, _document.PageCount));
    }

    private void NotifyPageModification()
    {
        WeakReferenceMessenger.Default.Send(new PageModifiedMessage());
    }

    private void OnThumbnailItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThumbnailItem.IsSelected))
        {
            RotateRightCommand.NotifyCanExecuteChanged();
            RotateLeftCommand.NotifyCanExecuteChanged();
            DeletePagesCommand.NotifyCanExecuteChanged();
        }
    }

    private void InvokeOnUIThread(Action action)
    {
        if (_dispatcherService != null)
        {
            _dispatcherService.Invoke(action);
        }
        else
        {
            action();
        }
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

        foreach (var item in Thumbnails)
        {
            item.PropertyChanged -= OnThumbnailItemPropertyChanged;
        }

        _disposed = true;
        _logger.LogInformation("ThumbnailsViewModel disposed");
    }
}
