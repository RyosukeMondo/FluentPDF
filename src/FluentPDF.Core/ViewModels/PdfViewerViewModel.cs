using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Core ViewModel for the PDF viewer page.
/// Thin coordinator that delegates to NavigationViewModel, ZoomViewModel, and ViewStateViewModel.
/// </summary>
public partial class PdfViewerViewModel : ViewModelBase, IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfRenderingService _renderingService;
    private readonly IDocumentEditingService _editingService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IImageExportService? _imageExportService;
    private readonly ISecurityService? _securityService;
    private readonly ICoordinateMapper? _coordinateMapper;
    private readonly IDialogService? _dialogService;
    private readonly IDispatcherService? _dispatcherService;
    private readonly ILogger<PdfViewerViewModel> _logger;
    private readonly IMetricsCollectionService? _metricsService;
    private readonly IDpiDetectionService? _dpiDetectionService;
    private readonly IRenderingSettingsService? _renderingSettingsService;
    private readonly ISettingsService? _settingsService;
    private readonly IPageOperationsService? _pageOperationsService;

    private PdfDocument? _currentDocument;
    private bool _disposed;
    private readonly IDisposable? _qualitySubscription;
    private double _lastRenderedDpi = 96.0;

    /// <summary>
    /// Callback to render the current page. Must be set by the UI framework.
    /// </summary>
    public Func<PdfDocument, int, double, double, Task<object?>>? RenderPageCallback { get; set; }

    /// <summary>
    /// Callback to convert a stream to the UI-specific image type.
    /// </summary>
    public Func<Stream, Task<object?>>? StreamToImageCallback { get; set; }

    public PdfViewerViewModel(
        IPdfDocumentService documentService,
        IPdfRenderingService renderingService,
        IDocumentEditingService editingService,
        ITextSearchService searchService,
        ITextExtractionService textExtractionService,
        ILogger<PdfViewerViewModel> logger,
        NavigationViewModel navigation,
        ZoomViewModel zoom,
        ViewStateViewModel viewState,
        IImageExportService? imageExportService = null,
        ISecurityService? securityService = null,
        ICoordinateMapper? coordinateMapper = null,
        IDialogService? dialogService = null,
        IDispatcherService? dispatcherService = null,
        IAnimationService? animationService = null,
        IMetricsCollectionService? metricsService = null,
        IDpiDetectionService? dpiDetectionService = null,
        IRenderingSettingsService? renderingSettingsService = null,
        ISettingsService? settingsService = null,
        IPageOperationsService? pageOperationsService = null)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _editingService = editingService ?? throw new ArgumentNullException(nameof(editingService));
        _textExtractionService = textExtractionService ?? throw new ArgumentNullException(nameof(textExtractionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageExportService = imageExportService;
        _securityService = securityService;
        _coordinateMapper = coordinateMapper;
        _dialogService = dialogService;
        _dispatcherService = dispatcherService;
        _metricsService = metricsService;
        _dpiDetectionService = dpiDetectionService;
        _renderingSettingsService = renderingSettingsService;
        _settingsService = settingsService;
        _pageOperationsService = pageOperationsService;

        // Wire sub-ViewModels
        Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        Zoom = zoom ?? throw new ArgumentNullException(nameof(zoom));
        ViewState = viewState ?? throw new ArgumentNullException(nameof(viewState));

        Navigation.OnPageChanged = async _ => await RenderCurrentPageAsync();
        Zoom.OnZoomChanged = async _ => await RenderCurrentPageAsync();

        // Sync navigation state changes back to this VM's properties
        Navigation.PropertyChanged += OnNavigationPropertyChanged;
        Zoom.PropertyChanged += OnZoomPropertyChanged;
        ViewState.PropertyChanged += OnViewStatePropertyChanged;

        // Register message handler for thumbnail navigation
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, async (r, m) =>
        {
            if (m.PageNumber != Navigation.CurrentPageNumber)
            {
                await Navigation.GoToPageCommand.ExecuteAsync(m.PageNumber);
            }
        });

        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            HasPageModifications = true;
        });

        if (_renderingSettingsService != null)
        {
            _qualitySubscription = _renderingSettingsService.ObserveRenderingQuality()
                .Subscribe(quality =>
                {
                    CurrentRenderingQuality = quality;
                    if (_currentDocument != null && !IsLoading)
                    {
                        _ = RenderCurrentPageAsync();
                    }
                });
        }

        _logger.LogInformation("PdfViewerViewModel initialized");
    }

    #region Observable Properties

    [ObservableProperty]
    private object? _currentPageImage;

    /// <summary>
    /// Gets or sets the current page number (1-based). Delegates to NavigationViewModel.
    /// </summary>
    public int CurrentPageNumber
    {
        get => Navigation.CurrentPageNumber;
        set => Navigation.CurrentPageNumber = value;
    }

    /// <summary>Gets the current page index (0-based).</summary>
    public int CurrentPageIndex
    {
        get => CurrentPageNumber - 1;
        set => CurrentPageNumber = value + 1;
    }

    /// <summary>Gets or sets the total number of pages. Delegates to NavigationViewModel.</summary>
    public int TotalPages
    {
        get => Navigation.TotalPages;
        set => Navigation.TotalPages = value;
    }

    /// <summary>Gets the page count.</summary>
    public int PageCount => TotalPages;

    /// <summary>Gets or sets the current zoom level. Delegates to ZoomViewModel.</summary>
    public double ZoomLevel
    {
        get => Zoom.ZoomLevel;
        set => Zoom.ZoomLevel = value;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Open a PDF file to get started";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private double _operationProgress;

    [ObservableProperty]
    private bool _isOperationInProgress;

    /// <summary>Gets or sets the current page view mode. Delegates to ViewStateViewModel.</summary>
    public PageViewMode ViewMode
    {
        get => ViewState.ViewMode;
        set => ViewState.ViewMode = value;
    }

    public bool IsSinglePageMode => ViewState.IsSinglePageMode;
    public bool IsContinuousScrollMode => ViewState.IsContinuousScrollMode;
    public bool IsTwoPageMode => ViewState.IsTwoPageMode;

    /// <summary>Gets or sets whether the search panel is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsSearchPanelVisible
    {
        get => ViewState.IsSearchPanelVisible;
        set => ViewState.IsSearchPanelVisible = value;
    }

    [ObservableProperty]
    private double _currentPageHeight;

    [ObservableProperty]
    private string _selectedText = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedText;

    /// <summary>
    /// Stores the last text selection with character bounds for annotation placement.
    /// </summary>
    public TextSelection? LastTextSelection { get; set; }

    [ObservableProperty]
    private DisplayInfo? _currentDisplayInfo;

    [ObservableProperty]
    private RenderingQuality _currentRenderingQuality = RenderingQuality.Auto;

    [ObservableProperty]
    private bool _isAdjustingQuality;

    /// <summary>Gets or sets whether the sidebar is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsSidebarVisible
    {
        get => ViewState.IsSidebarVisible;
        set => ViewState.IsSidebarVisible = value;
    }

    /// <summary>Gets or sets whether the bookmarks panel is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsBookmarksPanelVisible
    {
        get => ViewState.IsBookmarksPanelVisible;
        set => ViewState.IsBookmarksPanelVisible = value;
    }

    [ObservableProperty]
    private bool _hasPageModifications;

    [ObservableProperty]
    private DrawingTool _activeDrawingTool = DrawingTool.None;

    [ObservableProperty]
    private string _drawingStrokeColor = "#FF0000";

    [ObservableProperty]
    private string _drawingFillColor = "#0000FF";

    [ObservableProperty]
    private float _drawingStrokeWidth = 2f;

    /// <summary>Whether a drawing tool is currently active.</summary>
    public bool IsDrawingToolActive => ActiveDrawingTool != DrawingTool.None;

    public bool HasUnsavedChanges => HasPageModifications;

    public PdfDocument? CurrentDocument => _currentDocument;

    public AnnotationViewModel? AnnotationViewModel { get; set; }

    /// <summary>Navigation sub-ViewModel.</summary>
    public NavigationViewModel Navigation { get; }

    /// <summary>Zoom sub-ViewModel.</summary>
    public ZoomViewModel Zoom { get; }

    /// <summary>ViewState sub-ViewModel.</summary>
    public ViewStateViewModel ViewState { get; }

    public ThumbnailsViewModel? Thumbnails { get; set; }
    public BookmarksViewModel? Bookmarks { get; set; }

    #endregion

    #region Command Forwarding Properties

    // Navigation commands (forwarded to NavigationViewModel)
    public IAsyncRelayCommand GoToPreviousPageCommand => Navigation.GoToPreviousPageCommand;
    public IAsyncRelayCommand GoToNextPageCommand => Navigation.GoToNextPageCommand;
    public IAsyncRelayCommand<int> GoToPageCommand => Navigation.GoToPageCommand;
    public IAsyncRelayCommand FirstPageCommand => Navigation.FirstPageCommand;
    public IAsyncRelayCommand LastPageCommand => Navigation.LastPageCommand;

    // Zoom commands (forwarded to ZoomViewModel)
    public IAsyncRelayCommand ZoomInCommand => Zoom.ZoomInCommand;
    public IAsyncRelayCommand ZoomOutCommand => Zoom.ZoomOutCommand;
    public IAsyncRelayCommand ResetZoomCommand => Zoom.ResetZoomCommand;
    public IAsyncRelayCommand<double> SetZoomCommand => Zoom.SetZoomCommand;
    public IAsyncRelayCommand FitWidthCommand => Zoom.FitWidthCommand;
    public IAsyncRelayCommand FitPageCommand => Zoom.FitPageCommand;

    // View state commands (forwarded to ViewStateViewModel)
    public IRelayCommand ToggleThumbnailsCommand => ViewState.ToggleThumbnailsCommand;
    public IRelayCommand ToggleBookmarksCommand => ViewState.ToggleBookmarksCommand;
    public IRelayCommand ShowSearchCommand => ViewState.ShowSearchCommand;
    public IRelayCommand ToggleSearchPanelCommand => ViewState.ToggleSearchPanelCommand;
    public IRelayCommand ToggleViewModeCommand => ViewState.ToggleViewModeCommand;

    #endregion

    #region Drawing Tool Commands

    [RelayCommand]
    private void SetDrawingTool(string toolName)
    {
        if (Enum.TryParse<DrawingTool>(toolName, ignoreCase: true, out var tool))
        {
            ActiveDrawingTool = ActiveDrawingTool == tool ? DrawingTool.None : tool;
        }
        else
        {
            ActiveDrawingTool = DrawingTool.None;
        }
    }

    #endregion

    #region Page Management Commands

    private bool CanExecutePageOperation() =>
        _currentDocument != null && !IsLoading && _pageOperationsService != null;

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task RotatePageClockwiseAsync()
    {
        var result = await _pageOperationsService!.RotatePagesAsync(
            _currentDocument!, new[] { CurrentPageIndex }, RotationAngle.Rotate90);
        await HandlePageOperationResult(result, "rotate");
    }

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task RotatePageCounterClockwiseAsync()
    {
        var result = await _pageOperationsService!.RotatePagesAsync(
            _currentDocument!, new[] { CurrentPageIndex }, RotationAngle.Rotate270);
        await HandlePageOperationResult(result, "rotate");
    }

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task DeleteCurrentPageAsync()
    {
        if (TotalPages <= 1)
        {
            await ShowErrorAsync("Delete Page", "Cannot delete the only page in the document.");
            return;
        }

        var result = await _pageOperationsService!.DeletePagesAsync(
            _currentDocument!, new[] { CurrentPageIndex });

        if (result.IsSuccess)
        {
            TotalPages--;
            if (CurrentPageNumber > TotalPages)
                CurrentPageNumber = TotalPages;
            OnPropertyChanged(nameof(PageCount));
        }

        await HandlePageOperationResult(result, "delete page");
    }

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task InsertBlankPageAsync()
    {
        var insertAt = CurrentPageIndex + 1;
        var result = await _pageOperationsService!.InsertBlankPageAsync(
            _currentDocument!, insertAt, PageSize.SameAsCurrent);

        if (result.IsSuccess)
        {
            TotalPages++;
            CurrentPageNumber = insertAt + 1;
            OnPropertyChanged(nameof(PageCount));
        }

        await HandlePageOperationResult(result, "insert blank page");
    }

    private async Task HandlePageOperationResult(FluentResults.Result result, string operation)
    {
        if (result.IsSuccess)
        {
            HasPageModifications = true;
            WeakReferenceMessenger.Default.Send(new PageModifiedMessage());
            await RenderCurrentPageAsync();
        }
        else
        {
            var msg = result.Errors.Count > 0 ? result.Errors[0].Message : "Unknown error";
            _logger.LogError("Failed to {Operation}: {Error}", operation, msg);
            await ShowErrorAsync("Page Operation Failed", msg);
        }
    }

    #endregion

    #region Text Selection Commands

    private bool CanSelectAllText() => _currentDocument != null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSelectAllText))]
    private async Task SelectAllTextAsync()
    {
        if (_currentDocument == null) return;

        try
        {
            var result = await _textExtractionService.ExtractTextAsync(_currentDocument, CurrentPageNumber);
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            {
                SelectedText = result.Value;
                HasSelectedText = true;
                _logger.LogInformation("Selected all text on page {Page}: {Length} characters",
                    CurrentPageNumber, result.Value.Length);
            }
            else
            {
                SelectedText = string.Empty;
                HasSelectedText = false;
                _logger.LogDebug("No text found on page {Page}", CurrentPageNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select all text on page {Page}", CurrentPageNumber);
        }
    }

    #endregion

    #region Document Operations

    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        if (_dialogService == null)
        {
            _logger.LogWarning("DialogService not available");
            return;
        }

        try
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync(
                "Open PDF Document", new[] { ".pdf" });

            if (string.IsNullOrEmpty(filePath)) return;

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

    public async Task LoadDocumentFromPathAsync(string filePath)
    {
        _logger.LogInformation("Loading document: {FilePath}", filePath);
        IsLoading = true;
        StatusMessage = "Loading document...";

        try
        {
            if (_currentDocument != null)
            {
                _documentService.CloseDocument(_currentDocument);
                _currentDocument = null;
            }

            var result = await _documentService.LoadDocumentAsync(filePath);
            if (result.IsFailed)
            {
                var msg = result.Errors.Count > 0 ? result.Errors[0].Message : "Unknown error";
                StatusMessage = $"Failed to load document: {msg}";
                IsLoading = false;
                await ShowErrorAsync("Error Loading Document", msg);
                return;
            }

            _currentDocument = result.Value;
            TotalPages = _currentDocument.PageCount;
            CurrentPageNumber = 1;
            HasPageModifications = false;
            SyncDocumentStateToSubViewModels();

            OnPropertyChanged(nameof(PageCount));

            ApplyDefaultSettings();
            NotifyPageCommandsCanExecuteChanged();
            await RenderCurrentPageAsync();
            await LoadSidePanelsAsync();

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

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_currentDocument == null)
        {
            await ShowErrorAsync("Save Error", "No document is currently loaded.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving document...";
            StatusMessage = $"Document saved: {Path.GetFileName(_currentDocument.FilePath)}";
            HasPageModifications = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document");
            StatusMessage = "Error saving document";
            await ShowErrorAsync("Save Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave() => HasUnsavedChanges && !IsLoading && _currentDocument != null;

    [RelayCommand(CanExecute = nameof(CanSaveAs))]
    private async Task SaveAsAsync()
    {
        if (_currentDocument == null)
        {
            await ShowErrorAsync("Save As Error", "No document is currently loaded.");
            return;
        }

        _logger.LogInformation("Save As: should be implemented by UI framework");
    }

    private bool CanSaveAs() => !IsLoading && _currentDocument != null;

    public async Task RefreshCurrentPageAsync()
    {
        HasPageModifications = true;
        await RenderCurrentPageAsync();
    }

    #endregion

    #region Private Helpers

    private async Task RenderCurrentPageAsync()
    {
        if (_currentDocument == null || RenderPageCallback == null) return;

        IsLoading = true;
        StatusMessage = $"Rendering page {CurrentPageNumber}...";

        try
        {
            double effectiveDpi = 96.0;
            if (_dpiDetectionService != null && CurrentDisplayInfo != null)
            {
                var dpiResult = _dpiDetectionService.CalculateEffectiveDpi(
                    CurrentDisplayInfo, ZoomLevel, CurrentRenderingQuality);
                if (dpiResult.IsSuccess) effectiveDpi = dpiResult.Value;
            }

            var imageSource = await RenderPageCallback(
                _currentDocument, CurrentPageNumber, ZoomLevel, effectiveDpi);

            if (imageSource != null)
            {
                CurrentPageImage = imageSource;
                StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";
                _lastRenderedDpi = effectiveDpi;
                _metricsService?.RecordRenderTime(CurrentPageNumber, 0);
            }
            else
            {
                StatusMessage = "Failed to render page";
                await ShowErrorAsync("Rendering Error", "Failed to render page.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering page");
            StatusMessage = "Unexpected error while rendering page";
            await ShowErrorAsync("Error", $"Failed to render page: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SyncDocumentStateToSubViewModels()
    {
        Navigation.HasDocument = _currentDocument != null;
        Zoom.HasDocument = _currentDocument != null;
    }

    private void ApplyDefaultSettings()
    {
        if (_settingsService == null) return;

        var zoomValue = ConvertZoomLevelToDouble(_settingsService.Settings.DefaultZoom);
        if (zoomValue.HasValue)
        {
            ZoomLevel = zoomValue.Value;
        }
    }

    private static double? ConvertZoomLevelToDouble(ZoomLevel zoomLevel) => zoomLevel switch
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

    private async Task LoadSidePanelsAsync()
    {
        if (Thumbnails != null && _currentDocument != null)
        {
            try { await Thumbnails.LoadThumbnailsAsync(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load thumbnails"); }
        }
        if (Bookmarks != null && _currentDocument != null)
        {
            try { await Bookmarks.LoadBookmarksCommand.ExecuteAsync(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load bookmarks"); }
        }
    }

    private void NotifyPageCommandsCanExecuteChanged()
    {
        RotatePageClockwiseCommand.NotifyCanExecuteChanged();
        RotatePageCounterClockwiseCommand.NotifyCanExecuteChanged();
        DeleteCurrentPageCommand.NotifyCanExecuteChanged();
        InsertBlankPageCommand.NotifyCanExecuteChanged();
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        if (_dialogService != null)
            await _dialogService.ShowErrorAsync(title, message);
        else
            _logger.LogError("Dialog not available. Error: {Title} - {Message}", title, message);
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NavigationViewModel.CurrentPageNumber))
        {
            OnPropertyChanged(nameof(CurrentPageNumber));
            OnPropertyChanged(nameof(CurrentPageIndex));
        }
        else if (e.PropertyName == nameof(NavigationViewModel.TotalPages))
        {
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageCount));
        }
    }

    private void OnZoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ZoomViewModel.ZoomLevel))
        {
            OnPropertyChanged(nameof(ZoomLevel));
        }
    }

    private void OnViewStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewStateViewModel.IsSidebarVisible):
                OnPropertyChanged(nameof(IsSidebarVisible));
                break;
            case nameof(ViewStateViewModel.IsBookmarksPanelVisible):
                OnPropertyChanged(nameof(IsBookmarksPanelVisible));
                break;
            case nameof(ViewStateViewModel.IsSearchPanelVisible):
                OnPropertyChanged(nameof(IsSearchPanelVisible));
                break;
            case nameof(ViewStateViewModel.ViewMode):
                OnPropertyChanged(nameof(ViewMode));
                OnPropertyChanged(nameof(IsSinglePageMode));
                OnPropertyChanged(nameof(IsContinuousScrollMode));
                OnPropertyChanged(nameof(IsTwoPageMode));
                break;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsLoading))
        {
            Navigation.IsLoading = IsLoading;
            Zoom.IsLoading = IsLoading;
            NotifyPageCommandsCanExecuteChanged();
        }

        if (e.PropertyName == nameof(HasPageModifications))
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }

        if (e.PropertyName == nameof(ActiveDrawingTool))
        {
            OnPropertyChanged(nameof(IsDrawingToolActive));
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        WeakReferenceMessenger.Default.Unregister<NavigateToPageMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);

        Navigation.PropertyChanged -= OnNavigationPropertyChanged;
        Zoom.PropertyChanged -= OnZoomPropertyChanged;
        ViewState.PropertyChanged -= OnViewStatePropertyChanged;
        Navigation.Dispose();

        if (_currentDocument != null)
        {
            _documentService.CloseDocument(_currentDocument);
            _currentDocument = null;
        }

        _qualitySubscription?.Dispose();

        _disposed = true;
    }

    #endregion
}
