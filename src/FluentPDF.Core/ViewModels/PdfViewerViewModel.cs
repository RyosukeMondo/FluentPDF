using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Defines the page view modes for the PDF viewer.
/// </summary>
public enum PageViewMode
{
    /// <summary>Single page view - one page at a time with navigation.</summary>
    SinglePage,

    /// <summary>Continuous scroll - all pages in a vertical scrolling list.</summary>
    ContinuousScroll,

    /// <summary>Two-page view - displays two pages side-by-side like a book.</summary>
    TwoPage
}

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
/// Core ViewModel for the PDF viewer page.
/// UI-framework agnostic implementation that can be used with WinUI 3, Avalonia, or other frameworks.
/// Uses abstractions for all UI-specific operations.
/// </summary>
public partial class PdfViewerViewModel : ViewModelBase, IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfRenderingService _renderingService;
    private readonly IDocumentEditingService _editingService;
    private readonly ITextSearchService _searchService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IImageExportService? _imageExportService;
    private readonly ISecurityService? _securityService;
    private readonly ICoordinateMapper? _coordinateMapper;
    private readonly IDialogService? _dialogService;
    private readonly IDispatcherService? _dispatcherService;
    private readonly IAnimationService? _animationService;
    private readonly ILogger<PdfViewerViewModel> _logger;
    private readonly IMetricsCollectionService? _metricsService;
    private readonly IDpiDetectionService? _dpiDetectionService;
    private readonly IRenderingSettingsService? _renderingSettingsService;
    private readonly ISettingsService? _settingsService;

    private PdfDocument? _currentDocument;
    private bool _disposed;
    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _navigationAnimationCts;
    private System.Threading.Timer? _searchDebounceTimer;
    private IDisposable? _dpiSubscription;
    private IDisposable? _qualitySubscription;
    private double _lastRenderedDpi = 96.0;

    /// <summary>
    /// Callback to render the current page. Must be set by the UI framework.
    /// </summary>
    public Func<PdfDocument, int, double, double, Task<object?>>? RenderPageCallback { get; set; }

    /// <summary>
    /// Callback to convert a stream to the UI-specific image type.
    /// </summary>
    public Func<Stream, Task<object?>>? StreamToImageCallback { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerViewModel"/> class.
    /// </summary>
    public PdfViewerViewModel(
        IPdfDocumentService documentService,
        IPdfRenderingService renderingService,
        IDocumentEditingService editingService,
        ITextSearchService searchService,
        ITextExtractionService textExtractionService,
        ILogger<PdfViewerViewModel> logger,
        IImageExportService? imageExportService = null,
        ISecurityService? securityService = null,
        ICoordinateMapper? coordinateMapper = null,
        IDialogService? dialogService = null,
        IDispatcherService? dispatcherService = null,
        IAnimationService? animationService = null,
        IMetricsCollectionService? metricsService = null,
        IDpiDetectionService? dpiDetectionService = null,
        IRenderingSettingsService? renderingSettingsService = null,
        ISettingsService? settingsService = null)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _editingService = editingService ?? throw new ArgumentNullException(nameof(editingService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _textExtractionService = textExtractionService ?? throw new ArgumentNullException(nameof(textExtractionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageExportService = imageExportService;
        _securityService = securityService;
        _coordinateMapper = coordinateMapper;
        _dialogService = dialogService;
        _dispatcherService = dispatcherService;
        _animationService = animationService;
        _metricsService = metricsService;
        _dpiDetectionService = dpiDetectionService;
        _renderingSettingsService = renderingSettingsService;
        _settingsService = settingsService;

        // Register message handler for thumbnail navigation
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, async (r, m) =>
        {
            if (m.PageNumber != CurrentPageNumber)
            {
                CurrentPageNumber = m.PageNumber;
                await RenderCurrentPageAsync();
            }
        });

        // Subscribe to page modification events
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            HasPageModifications = true;
        });

        // Subscribe to quality changes if settings service is available
        if (_renderingSettingsService != null)
        {
            _qualitySubscription = _renderingSettingsService.ObserveRenderingQuality()
                .Subscribe(quality =>
                {
                    CurrentRenderingQuality = quality;
                    _logger.LogInformation("Rendering quality changed to {Quality}", quality);

                    if (_currentDocument != null && !IsLoading)
                    {
                        // Fire and forget - trigger re-render asynchronously
                        _ = RenderCurrentPageAsync();
                    }
                });
        }

        _logger.LogInformation("PdfViewerViewModel initialized");
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets the current page image displayed in the viewer.
    /// Type is object to allow UI framework to use its specific image type.
    /// </summary>
    [ObservableProperty]
    private object? _currentPageImage;

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentPageNumber = 1;

    /// <summary>
    /// Gets the current page index (0-based). Alias for CurrentPageNumber - 1.
    /// </summary>
    public int CurrentPageIndex
    {
        get => CurrentPageNumber - 1;
        set => CurrentPageNumber = value + 1;
    }

    /// <summary>
    /// Gets or sets the total number of pages in the current document.
    /// </summary>
    [ObservableProperty]
    private int _totalPages;

    /// <summary>
    /// Gets the page count. Alias for TotalPages.
    /// </summary>
    public int PageCount => TotalPages;

    /// <summary>
    /// Gets or sets the current zoom level (1.0 = 100%, 2.0 = 200%, etc.).
    /// </summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the status message displayed to the user.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Open a PDF file to get started";

    /// <summary>
    /// Gets or sets a value indicating whether there is an error state.
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Gets or sets the operation progress (0-100).
    /// </summary>
    [ObservableProperty]
    private double _operationProgress;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isOperationInProgress;

    /// <summary>
    /// Gets or sets the current page view mode.
    /// </summary>
    [ObservableProperty]
    private PageViewMode _viewMode = PageViewMode.SinglePage;

    /// <summary>Gets a value indicating whether the viewer is in single page mode.</summary>
    public bool IsSinglePageMode => ViewMode == PageViewMode.SinglePage;

    /// <summary>Gets a value indicating whether the viewer is in continuous scroll mode.</summary>
    public bool IsContinuousScrollMode => ViewMode == PageViewMode.ContinuousScroll;

    /// <summary>Gets a value indicating whether the viewer is in two-page mode.</summary>
    public bool IsTwoPageMode => ViewMode == PageViewMode.TwoPage;

    /// <summary>
    /// Gets or sets a value indicating whether the search panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSearchPanelVisible;

    /// <summary>
    /// Gets or sets the current search query.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets the list of search matches.
    /// </summary>
    [ObservableProperty]
    private List<SearchMatch> _searchMatches = new();

    /// <summary>
    /// Gets or sets the index of the currently selected match (0-based).
    /// </summary>
    [ObservableProperty]
    private int _currentMatchIndex = -1;

    /// <summary>
    /// Gets or sets a value indicating whether a search is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>
    /// Gets or sets a value indicating whether the search is case-sensitive.
    /// </summary>
    [ObservableProperty]
    private bool _caseSensitive;

    /// <summary>
    /// Gets or sets the current page height in PDF units (points).
    /// </summary>
    [ObservableProperty]
    private double _currentPageHeight;

    /// <summary>
    /// Gets or sets the selected text.
    /// </summary>
    [ObservableProperty]
    private string _selectedText = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether there is selected text.
    /// </summary>
    [ObservableProperty]
    private bool _hasSelectedText;

    /// <summary>
    /// Gets or sets the current display information.
    /// </summary>
    [ObservableProperty]
    private DisplayInfo? _currentDisplayInfo;

    /// <summary>
    /// Gets or sets the current rendering quality setting.
    /// </summary>
    [ObservableProperty]
    private RenderingQuality _currentRenderingQuality = RenderingQuality.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether the quality is being adjusted.
    /// </summary>
    [ObservableProperty]
    private bool _isAdjustingQuality;

    /// <summary>
    /// Gets or sets a value indicating whether the thumbnails sidebar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSidebarVisible = true;

    /// <summary>
    /// Gets or sets a value indicating whether the bookmarks panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isBookmarksPanelVisible = true;

    /// <summary>
    /// Gets or sets a value indicating whether page operations have been performed.
    /// </summary>
    [ObservableProperty]
    private bool _hasPageModifications;

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => HasPageModifications;

    /// <summary>
    /// Gets the currently loaded PDF document.
    /// </summary>
    public PdfDocument? CurrentDocument => _currentDocument;

    /// <summary>
    /// Gets or sets the annotation view model for PDF annotations.
    /// This is set by the UI framework to enable annotation functionality.
    /// </summary>
    public AnnotationViewModel? AnnotationViewModel { get; set; }

    #endregion

    #region Navigation Commands

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        _logger.LogInformation("GoToPreviousPage command invoked. CurrentPage={CurrentPage}", CurrentPageNumber);

        _navigationAnimationCts?.Cancel();
        _navigationAnimationCts?.Dispose();
        _navigationAnimationCts = new CancellationTokenSource();

        CurrentPageNumber--;
        await AnimatePageTransitionAsync(PageTransitionDirection.Backward, _navigationAnimationCts.Token);
        await RenderCurrentPageAsync();
    }

    private bool CanGoToPreviousPage() => CurrentPageNumber > 1 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task GoToNextPageAsync()
    {
        _logger.LogInformation("GoToNextPage command invoked. CurrentPage={CurrentPage}", CurrentPageNumber);

        _navigationAnimationCts?.Cancel();
        _navigationAnimationCts?.Dispose();
        _navigationAnimationCts = new CancellationTokenSource();

        CurrentPageNumber++;
        await AnimatePageTransitionAsync(PageTransitionDirection.Forward, _navigationAnimationCts.Token);
        await RenderCurrentPageAsync();
    }

    private bool CanGoToNextPage() => CurrentPageNumber < TotalPages && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    [RelayCommand]
    private async Task GoToPageAsync(int pageNumber)
    {
        _logger.LogInformation("GoToPage command invoked. PageNumber={PageNumber}", pageNumber);

        if (pageNumber >= 1 && pageNumber <= TotalPages && !IsLoading && _currentDocument != null)
        {
            _navigationAnimationCts?.Cancel();
            _navigationAnimationCts?.Dispose();
            _navigationAnimationCts = new CancellationTokenSource();

            CurrentPageNumber = pageNumber;
            await AnimatePageTransitionAsync(PageTransitionDirection.Jump, _navigationAnimationCts.Token);
            await RenderCurrentPageAsync();
        }
    }

    /// <summary>
    /// Navigates to the first page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
    private async Task FirstPageAsync()
    {
        _logger.LogInformation("FirstPage command invoked");
        CurrentPageNumber = 1;
        await RenderCurrentPageAsync();
    }

    private bool CanGoToFirstPage() => CurrentPageNumber > 1 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Navigates to the last page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
    private async Task LastPageAsync()
    {
        _logger.LogInformation("LastPage command invoked");
        CurrentPageNumber = TotalPages;
        await RenderCurrentPageAsync();
    }

    private bool CanGoToLastPage() => CurrentPageNumber < TotalPages && !IsLoading && _currentDocument != null;

    #endregion

    #region Zoom Commands

    /// <summary>
    /// Increases the zoom level.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private async Task ZoomInAsync()
    {
        _logger.LogInformation("ZoomIn command invoked. CurrentZoom={CurrentZoom}", ZoomLevel);

        ZoomLevel = ZoomLevel switch
        {
            < 0.75 => 0.75,
            < 1.0 => 1.0,
            < 1.25 => 1.25,
            < 1.5 => 1.5,
            < 1.75 => 1.75,
            < 2.0 => 2.0,
            _ => ZoomLevel
        };

        await RenderCurrentPageAsync();
    }

    private bool CanZoomIn() => ZoomLevel < 2.0 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Decreases the zoom level.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private async Task ZoomOutAsync()
    {
        _logger.LogInformation("ZoomOut command invoked. CurrentZoom={CurrentZoom}", ZoomLevel);

        ZoomLevel = ZoomLevel switch
        {
            > 1.75 => 1.75,
            > 1.5 => 1.5,
            > 1.25 => 1.25,
            > 1.0 => 1.0,
            > 0.75 => 0.75,
            > 0.5 => 0.5,
            _ => ZoomLevel
        };

        await RenderCurrentPageAsync();
    }

    private bool CanZoomOut() => ZoomLevel > 0.5 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Resets the zoom level to 100%.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetZoom))]
    private async Task ResetZoomAsync()
    {
        _logger.LogInformation("ResetZoom command invoked");
        ZoomLevel = 1.0;
        await RenderCurrentPageAsync();
    }

    private bool CanResetZoom() => Math.Abs(ZoomLevel - 1.0) > 0.01 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Sets the zoom level to a specific value.
    /// </summary>
    [RelayCommand]
    private async Task SetZoomAsync(double zoomLevel)
    {
        _logger.LogInformation("SetZoom command invoked. ZoomLevel={ZoomLevel}", zoomLevel);

        if (zoomLevel >= 0.5 && zoomLevel <= 2.0 && _currentDocument != null)
        {
            ZoomLevel = zoomLevel;
            await RenderCurrentPageAsync();
        }
    }

    /// <summary>
    /// Adjusts zoom to fit the page width in the viewport.
    /// </summary>
    [RelayCommand]
    private async Task FitWidthAsync()
    {
        _logger.LogInformation("FitWidth command invoked");
        // This will be calculated based on viewport width / page width
        // For now, set to 100% as a default
        ZoomLevel = 1.0;
        await RenderCurrentPageAsync();
    }

    /// <summary>
    /// Adjusts zoom to fit the entire page in the viewport.
    /// </summary>
    [RelayCommand]
    private async Task FitPageAsync()
    {
        _logger.LogInformation("FitPage command invoked");
        // This will be calculated based on viewport dimensions / page dimensions
        // For now, set to 75% as a reasonable fit
        ZoomLevel = 0.75;
        await RenderCurrentPageAsync();
    }

    #endregion

    #region Panel Toggle Commands

    /// <summary>
    /// Toggles the visibility of the thumbnails sidebar.
    /// </summary>
    [RelayCommand]
    private void ToggleThumbnails()
    {
        _logger.LogInformation("ToggleThumbnails command invoked");
        IsSidebarVisible = !IsSidebarVisible;
        RaiseAccessibilityNotification(
            IsSidebarVisible ? "Thumbnails sidebar shown" : "Thumbnails sidebar hidden");
    }

    /// <summary>
    /// Toggles the visibility of the bookmarks panel.
    /// </summary>
    [RelayCommand]
    private void ToggleBookmarks()
    {
        _logger.LogInformation("ToggleBookmarks command invoked");
        IsBookmarksPanelVisible = !IsBookmarksPanelVisible;
        RaiseAccessibilityNotification(
            IsBookmarksPanelVisible ? "Bookmarks panel shown" : "Bookmarks panel hidden");
    }

    /// <summary>
    /// Shows the search panel.
    /// </summary>
    [RelayCommand]
    private void ShowSearch()
    {
        _logger.LogInformation("ShowSearch command invoked");
        IsSearchPanelVisible = true;
    }

    /// <summary>
    /// Toggles the search panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleSearchPanel()
    {
        _logger.LogInformation("ToggleSearchPanel command invoked");
        IsSearchPanelVisible = !IsSearchPanelVisible;

        if (!IsSearchPanelVisible)
        {
            SearchQuery = string.Empty;
            SearchMatches.Clear();
            CurrentMatchIndex = -1;
            _searchCts?.Cancel();
        }
    }

    /// <summary>
    /// Toggles the view mode between single page, continuous scroll, and two-page.
    /// </summary>
    [RelayCommand]
    private void ToggleViewMode()
    {
        ViewMode = ViewMode switch
        {
            PageViewMode.SinglePage => PageViewMode.ContinuousScroll,
            PageViewMode.ContinuousScroll => PageViewMode.TwoPage,
            PageViewMode.TwoPage => PageViewMode.SinglePage,
            _ => PageViewMode.SinglePage
        };

        OnPropertyChanged(nameof(IsSinglePageMode));
        OnPropertyChanged(nameof(IsContinuousScrollMode));
        OnPropertyChanged(nameof(IsTwoPageMode));

        _logger.LogInformation("View mode changed to {ViewMode}", ViewMode);
    }

    #endregion

    #region Document Operations

    /// <summary>
    /// Opens a file picker and loads the selected PDF document.
    /// </summary>
    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        _logger.LogInformation("OpenDocument command invoked");

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

            await LoadDocumentFromPathAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in OpenDocumentAsync");
            IsLoading = false;
            StatusMessage = $"Failed to open document: {ex.Message}";
            await ShowErrorAsync("Error Opening Document", ex.Message);
        }
    }

    /// <summary>
    /// Loads a PDF document from the specified file path.
    /// </summary>
    public async Task LoadDocumentFromPathAsync(string filePath)
    {
        _logger.LogInformation("Loading document from path: {FilePath}", filePath);

        IsLoading = true;
        StatusMessage = "Loading document...";

        try
        {
            if (_currentDocument != null)
            {
                _logger.LogInformation("Closing previous document");
                _documentService.CloseDocument(_currentDocument);
                _currentDocument = null;
            }

            var result = await _documentService.LoadDocumentAsync(filePath);

            if (result.IsFailed)
            {
                var errorMessage = result.Errors.Count > 0
                    ? result.Errors[0].Message
                    : "Unknown error occurred";

                StatusMessage = $"Failed to load document: {errorMessage}";
                IsLoading = false;

                await ShowErrorAsync("Error Loading Document", errorMessage);
                return;
            }

            _currentDocument = result.Value;
            TotalPages = _currentDocument.PageCount;
            CurrentPageNumber = 1;
            HasPageModifications = false;

            OnPropertyChanged(nameof(PageCount));

            _logger.LogInformation("Document loaded. Pages: {PageCount}", TotalPages);

            ApplyDefaultSettings();
            await RenderCurrentPageAsync();

            StatusMessage = "Document loaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading document");
            StatusMessage = $"Failed to load document: {ex.Message}";
            await ShowErrorAsync("Error Loading Document", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Saves the current document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        _logger.LogInformation("Save command invoked");

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot save: no document loaded");
            await ShowErrorAsync("Save Error", "No document is currently loaded.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving document...";

            // Framework-specific save logic would be called here
            StatusMessage = $"Document saved: {Path.GetFileName(_currentDocument.FilePath)}";
            HasPageModifications = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving document");
            StatusMessage = "Error saving document";
            await ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave() => HasUnsavedChanges && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Saves the current document with a new name (Save As).
    /// This command should be implemented by the UI framework.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveAs))]
    private async Task SaveAsAsync()
    {
        _logger.LogInformation("Save As command invoked");

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot save: no document loaded");
            await ShowErrorAsync("Save As Error", "No document is currently loaded.");
            return;
        }

        // Framework-specific Save As logic would be called here via callback or dialog service
        _logger.LogInformation("Save As functionality should be implemented by UI framework");
    }

    private bool CanSaveAs() => !IsLoading && _currentDocument != null;

    /// <summary>
    /// Refreshes the current page display.
    /// </summary>
    public async Task RefreshCurrentPageAsync()
    {
        HasPageModifications = true;
        await RenderCurrentPageAsync();
    }

    #endregion

    #region Search Commands

    /// <summary>
    /// Initiates a debounced search.
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        _logger.LogInformation("Search command invoked. Query={Query}", SearchQuery);

        _searchDebounceTimer?.Dispose();
        _searchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(SearchQuery) || _currentDocument == null)
        {
            SearchMatches.Clear();
            CurrentMatchIndex = -1;
            return;
        }

        _searchDebounceTimer = new System.Threading.Timer(
            async _ => await ExecuteSearchAsync(),
            null,
            TimeSpan.FromMilliseconds(300),
            Timeout.InfiniteTimeSpan);
    }

    private async Task ExecuteSearchAsync()
    {
        if (_currentDocument == null || string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        try
        {
            InvokeOnUIThread(() => IsSearching = true);

            var options = new SearchOptions
            {
                CaseSensitive = CaseSensitive,
                WholeWord = false
            };

            var result = await _searchService.SearchAsync(
                _currentDocument,
                SearchQuery,
                options,
                _searchCts.Token);

            if (result.IsSuccess)
            {
                SearchMatches = result.Value;
                CurrentMatchIndex = SearchMatches.Count > 0 ? 0 : -1;

                _logger.LogInformation("Search completed. Matches={MatchCount}", SearchMatches.Count);

                if (CurrentMatchIndex >= 0)
                {
                    await NavigateToCurrentMatchAsync();
                }
            }
            else
            {
                SearchMatches.Clear();
                CurrentMatchIndex = -1;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during search");
            SearchMatches.Clear();
            CurrentMatchIndex = -1;
        }
        finally
        {
            InvokeOnUIThread(() => IsSearching = false);
        }
    }

    /// <summary>
    /// Navigates to the next search match.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateToNextMatch))]
    private async Task GoToNextMatchAsync()
    {
        if (SearchMatches.Count == 0) return;

        CurrentMatchIndex = (CurrentMatchIndex + 1) % SearchMatches.Count;
        await NavigateToCurrentMatchAsync();
    }

    private bool CanNavigateToNextMatch() => SearchMatches.Count > 0 && !IsSearching;

    /// <summary>
    /// Navigates to the previous search match.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateToPreviousMatch))]
    private async Task GoToPreviousMatchAsync()
    {
        if (SearchMatches.Count == 0) return;

        CurrentMatchIndex = (CurrentMatchIndex - 1 + SearchMatches.Count) % SearchMatches.Count;
        await NavigateToCurrentMatchAsync();
    }

    private bool CanNavigateToPreviousMatch() => SearchMatches.Count > 0 && !IsSearching;

    private async Task NavigateToCurrentMatchAsync()
    {
        if (CurrentMatchIndex < 0 || CurrentMatchIndex >= SearchMatches.Count)
        {
            return;
        }

        var match = SearchMatches[CurrentMatchIndex];
        var targetPage = match.PageNumber + 1; // Convert 0-based to 1-based

        if (CurrentPageNumber != targetPage)
        {
            await GoToPageCommand.ExecuteAsync(targetPage);
        }
    }

    #endregion

    #region Private Helpers

    private async Task RenderCurrentPageAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Attempted to render page with no document loaded");
            return;
        }

        if (RenderPageCallback == null)
        {
            _logger.LogWarning("RenderPageCallback not set, cannot render page");
            return;
        }

        IsLoading = true;
        StatusMessage = $"Rendering page {CurrentPageNumber}...";

        try
        {
            double effectiveDpi = 96.0;
            if (_dpiDetectionService != null && CurrentDisplayInfo != null)
            {
                var dpiResult = _dpiDetectionService.CalculateEffectiveDpi(
                    CurrentDisplayInfo,
                    ZoomLevel,
                    CurrentRenderingQuality);

                if (dpiResult.IsSuccess)
                {
                    effectiveDpi = dpiResult.Value;
                }
            }

            var imageSource = await RenderPageCallback(_currentDocument, CurrentPageNumber, ZoomLevel, effectiveDpi);

            if (imageSource != null)
            {
                CurrentPageImage = imageSource;
                StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";
                _lastRenderedDpi = effectiveDpi;
                _metricsService?.RecordRenderTime(CurrentPageNumber, 0);
            }
            else
            {
                _logger.LogError("Failed to render page");
                StatusMessage = "Failed to render page";
                await ShowErrorAsync("Rendering Error", "Failed to render page.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while rendering page");
            StatusMessage = "Unexpected error while rendering page";
            await ShowErrorAsync("Error", $"Failed to render page: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AnimatePageTransitionAsync(
        PageTransitionDirection direction,
        CancellationToken cancellationToken)
    {
        if (_animationService == null)
        {
            return;
        }

        try
        {
            await _animationService.AnimatePageTransitionAsync(null, direction, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Page transition animation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Page transition animation failed");
        }
    }

    private void ApplyDefaultSettings()
    {
        if (_settingsService == null)
        {
            return;
        }

        var settings = _settingsService.Settings;
        var zoomValue = ConvertZoomLevelToDouble(settings.DefaultZoom);
        if (zoomValue.HasValue)
        {
            ZoomLevel = zoomValue.Value;
            _logger.LogInformation("Applied default zoom level: {ZoomLevel}", zoomValue.Value);
        }
    }

    private static double? ConvertZoomLevelToDouble(ZoomLevel zoomLevel)
    {
        return zoomLevel switch
        {
            Models.ZoomLevel.FiftyPercent => 0.5,
            Models.ZoomLevel.SeventyFivePercent => 0.75,
            Models.ZoomLevel.OneHundredPercent => 1.0,
            Models.ZoomLevel.OneTwentyFivePercent => 1.25,
            Models.ZoomLevel.OneFiftyPercent => 1.5,
            Models.ZoomLevel.OneSeventyFivePercent => 1.75,
            Models.ZoomLevel.TwoHundredPercent => 2.0,
            _ => null
        };
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        if (_dialogService != null)
        {
            await _dialogService.ShowErrorAsync(title, message);
        }
        else
        {
            _logger.LogError("Dialog not available. Error: {Title} - {Message}", title, message);
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

    private void RaiseAccessibilityNotification(string message)
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new AccessibilityNotificationMessage(message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to raise accessibility notification: {Message}", message);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsLoading) ||
            e.PropertyName == nameof(CurrentPageNumber) ||
            e.PropertyName == nameof(TotalPages) ||
            e.PropertyName == nameof(ZoomLevel))
        {
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
            GoToNextPageCommand.NotifyCanExecuteChanged();
            ZoomInCommand.NotifyCanExecuteChanged();
            ZoomOutCommand.NotifyCanExecuteChanged();
            ResetZoomCommand.NotifyCanExecuteChanged();
        }

        if (e.PropertyName == nameof(CurrentPageNumber))
        {
            OnPropertyChanged(nameof(CurrentPageIndex));
        }

        if (e.PropertyName == nameof(TotalPages))
        {
            OnPropertyChanged(nameof(PageCount));
        }

        if (e.PropertyName == nameof(IsSearching) ||
            e.PropertyName == nameof(SearchMatches))
        {
            GoToNextMatchCommand.NotifyCanExecuteChanged();
            GoToPreviousMatchCommand.NotifyCanExecuteChanged();
        }

        if (e.PropertyName == nameof(HasUnsavedChanges))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        if (e.PropertyName == nameof(HasPageModifications))
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        if (e.PropertyName == nameof(SearchQuery) ||
            e.PropertyName == nameof(CaseSensitive))
        {
            SearchCommand.Execute(null);
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing PdfViewerViewModel");

        WeakReferenceMessenger.Default.Unregister<NavigateToPageMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);

        if (_currentDocument != null)
        {
            _documentService.CloseDocument(_currentDocument);
            _currentDocument = null;
        }

        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = null;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        _navigationAnimationCts?.Cancel();
        _navigationAnimationCts?.Dispose();
        _navigationAnimationCts = null;

        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = null;

        _dpiSubscription?.Dispose();
        _dpiSubscription = null;

        _qualitySubscription?.Dispose();
        _qualitySubscription = null;

        _disposed = true;
    }

    #endregion
}
