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
        Zoom.OnZoomChanged = async _ => await RenderCurrentPageSilentAsync();

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

    /// <summary>Navigation sub-ViewModel.</summary>
    public NavigationViewModel Navigation { get; }

    /// <summary>Zoom sub-ViewModel.</summary>
    public ZoomViewModel Zoom { get; }

    /// <summary>ViewState sub-ViewModel.</summary>
    public ViewStateViewModel ViewState { get; }

    public PdfDocument? CurrentDocument => _currentDocument;

    public AnnotationViewModel? AnnotationViewModel { get; set; }

    public ThumbnailsViewModel? Thumbnails { get; set; }
    public BookmarksViewModel? Bookmarks { get; set; }
    public SearchPanelViewModel? Search { get; set; }
    public AnnotationsListViewModel? AnnotationsList { get; set; }
    public MetadataViewModel? Metadata { get; set; }

    /// <summary>
    /// Callback for page operations (rotate, delete, insert) that need native interop.
    /// Set by the UI layer (PdfViewerPage) since Core cannot reference Rendering.
    /// Signature: (PdfDocument doc, int pageIndex, string operation) => Task&lt;bool&gt;
    /// Operations: "rotate_cw", "rotate_ccw", "delete", "insert_blank"
    /// </summary>
    public Func<PdfDocument, int, string, Task<bool>>? PageOperationCallback { get; set; }

    /// <summary>
    /// Stores the original page object count per page index (0-based) at document load time.
    /// Used to distinguish original objects from user-added objects.
    /// </summary>
    public Dictionary<int, int> OriginalObjectCounts { get; } = new();

    /// <summary>
    /// Callback to save the document. Set by UI layer.
    /// Parameters: PdfDocument, filePath (null for overwrite). Returns success.
    /// </summary>
    public Func<PdfDocument, string?, Task<bool>>? SaveDocumentCallback { get; set; }

    /// <summary>
    /// Called before save to flush deferred page modifications (e.g., GenerateContent).
    /// Parameter: documentId (file path).
    /// </summary>
    public Action<string>? PreSaveAction { get; set; }

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

            // Flush deferred GenerateContent before saving (avoids CIDFont text corruption during editing)
            PreSaveAction?.Invoke(_currentDocument.FilePath);

            if (SaveDocumentCallback != null)
            {
                var success = await SaveDocumentCallback(_currentDocument, null);
                if (success)
                {
                    HasPageModifications = false;
                    StatusMessage = $"Document saved: {Path.GetFileName(_currentDocument.FilePath)}";
                }
                else
                {
                    StatusMessage = "Failed to save document";
                }
            }
            else
            {
                _logger.LogWarning("SaveDocumentCallback not set");
                StatusMessage = "Save not available";
            }
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

        try
        {
            IsLoading = true;
            // Flush deferred GenerateContent before saving
            PreSaveAction?.Invoke(_currentDocument.FilePath);

            if (SaveDocumentCallback != null)
            {
                // Pass empty string to signal "save as" (UI will show file picker)
                var success = await SaveDocumentCallback(_currentDocument, "");
                if (success)
                {
                    HasPageModifications = false;
                    StatusMessage = "Document saved";
                }
            }
            else
            {
                _logger.LogWarning("SaveDocumentCallback not set");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Save As");
            await ShowErrorAsync("Save As Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSaveAs() => !IsLoading && _currentDocument != null;

    public async Task RefreshCurrentPageAsync()
    {
        HasPageModifications = true;
        await RenderCurrentPageAsync();
        if (Thumbnails?.RenderThumbnailCallback != null && _currentDocument != null)
            await Thumbnails.RefreshThumbnailsAsync(new[] { CurrentPageNumber - 1 });
    }

    /// <summary>
    /// Refreshes the current page without showing the loading overlay.
    /// Used after drawing operations to avoid flicker.
    /// </summary>
    public async Task RefreshCurrentPageSilentAsync()
    {
        HasPageModifications = true;
        await RenderCurrentPageSilentAsync();
        if (Thumbnails?.RenderThumbnailCallback != null && _currentDocument != null)
            await Thumbnails.RefreshThumbnailsAsync(new[] { CurrentPageNumber - 1 });
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
                UpdatePageDimensions();
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

    /// <summary>
    /// Renders the current page without showing IsLoading overlay (no flicker).
    /// </summary>
    private async Task RenderCurrentPageSilentAsync()
    {
        if (_currentDocument == null || RenderPageCallback == null) return;

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
                UpdatePageDimensions();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during silent page render");
        }
    }

    private void UpdatePageDimensions()
    {
        if (_currentDocument == null) return;
        var pageSizeResult = _renderingService.GetPageSize(_currentDocument, CurrentPageNumber);
        if (pageSizeResult.IsSuccess)
        {
            CurrentPageWidth = pageSizeResult.Value.Width;
            CurrentPageHeight = pageSizeResult.Value.Height;
        }
    }

    private void SyncDocumentStateToSubViewModels()
    {
        Navigation.HasDocument = _currentDocument != null;
        Zoom.HasDocument = _currentDocument != null;
        Search?.SetDocument(_currentDocument);
        Search?.SetNavigateToPageAction(async pageNumber =>
        {
            if (pageNumber != CurrentPageNumber)
            {
                await Navigation.GoToPageCommand.ExecuteAsync(pageNumber);
            }
        });
        AnnotationsList?.SetNavigateToPageAction(async pageNumber =>
        {
            if (pageNumber != CurrentPageNumber)
            {
                await Navigation.GoToPageCommand.ExecuteAsync(pageNumber);
            }
        });
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
        if (AnnotationsList != null && _currentDocument != null)
        {
            try { await AnnotationsList.LoadAnnotationsCommand.ExecuteAsync(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load annotations list"); }
        }
        if (Metadata != null && _currentDocument != null)
        {
            try { Metadata.UpdateFromDocument(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load metadata"); }
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
