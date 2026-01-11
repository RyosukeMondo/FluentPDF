using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for the PDF viewer page.
/// Provides commands for document operations (open, navigate, zoom) and observable properties for UI binding.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class PdfViewerViewModel : ObservableObject, IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfRenderingService _renderingService;
    private readonly IDocumentEditingService _editingService;
    private readonly ITextSearchService _searchService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILogger<PdfViewerViewModel> _logger;
    private readonly Core.Services.IMetricsCollectionService? _metricsService;
    private readonly IDpiDetectionService? _dpiDetectionService;
    private readonly IRenderingSettingsService? _renderingSettingsService;
    private readonly Core.Services.ISettingsService? _settingsService;
    private PdfDocument? _currentDocument;
    private bool _disposed;
    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _searchCts;
    private System.Threading.Timer? _searchDebounceTimer;
    private IDisposable? _dpiSubscription;
    private IDisposable? _qualitySubscription;
    private double _lastRenderedDpi;

    /// <summary>
    /// Gets the bookmarks view model for the bookmarks panel.
    /// </summary>
    public BookmarksViewModel BookmarksViewModel { get; }

    /// <summary>
    /// Gets the form field view model for form interactions.
    /// </summary>
    public FormFieldViewModel FormFieldViewModel { get; }

    /// <summary>
    /// Gets the diagnostics panel view model for observability metrics.
    /// </summary>
    public DiagnosticsPanelViewModel DiagnosticsPanelViewModel { get; }

    /// <summary>
    /// Gets the log viewer view model for viewing application logs.
    /// </summary>
    public LogViewerViewModel LogViewerViewModel { get; }

    /// <summary>
    /// Gets the annotation view model for PDF annotations.
    /// </summary>
    public AnnotationViewModel AnnotationViewModel { get; }

    /// <summary>
    /// Gets the thumbnails view model for the thumbnails sidebar.
    /// </summary>
    public ThumbnailsViewModel ThumbnailsViewModel { get; }

    /// <summary>
    /// Gets the image insertion view model for inserting images into PDF pages.
    /// </summary>
    public ImageInsertionViewModel ImageInsertionViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerViewModel"/> class.
    /// </summary>
    /// <param name="documentService">Service for loading PDF documents.</param>
    /// <param name="renderingService">Service for rendering PDF pages.</param>
    /// <param name="editingService">Service for editing PDF documents.</param>
    /// <param name="searchService">Service for searching text in PDF documents.</param>
    /// <param name="textExtractionService">Service for extracting text from PDF pages.</param>
    /// <param name="bookmarksViewModel">View model for the bookmarks panel.</param>
    /// <param name="formFieldViewModel">View model for form field interactions.</param>
    /// <param name="diagnosticsPanelViewModel">View model for the diagnostics panel.</param>
    /// <param name="logViewerViewModel">View model for the log viewer.</param>
    /// <param name="annotationViewModel">View model for PDF annotations.</param>
    /// <param name="thumbnailsViewModel">View model for the thumbnails sidebar.</param>
    /// <param name="imageInsertionViewModel">View model for image insertion operations.</param>
    /// <param name="metricsService">Optional metrics collection service for observability.</param>
    /// <param name="dpiDetectionService">Optional DPI detection service for HiDPI support.</param>
    /// <param name="renderingSettingsService">Optional rendering settings service for quality preferences.</param>
    /// <param name="settingsService">Optional settings service for user preferences.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public PdfViewerViewModel(
        IPdfDocumentService documentService,
        IPdfRenderingService renderingService,
        IDocumentEditingService editingService,
        ITextSearchService searchService,
        ITextExtractionService textExtractionService,
        BookmarksViewModel bookmarksViewModel,
        FormFieldViewModel formFieldViewModel,
        DiagnosticsPanelViewModel diagnosticsPanelViewModel,
        LogViewerViewModel logViewerViewModel,
        AnnotationViewModel annotationViewModel,
        ThumbnailsViewModel thumbnailsViewModel,
        ImageInsertionViewModel imageInsertionViewModel,
        Core.Services.IMetricsCollectionService? metricsService,
        IDpiDetectionService? dpiDetectionService,
        IRenderingSettingsService? renderingSettingsService,
        Core.Services.ISettingsService? settingsService,
        ILogger<PdfViewerViewModel> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _editingService = editingService ?? throw new ArgumentNullException(nameof(editingService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _textExtractionService = textExtractionService ?? throw new ArgumentNullException(nameof(textExtractionService));
        BookmarksViewModel = bookmarksViewModel ?? throw new ArgumentNullException(nameof(bookmarksViewModel));
        FormFieldViewModel = formFieldViewModel ?? throw new ArgumentNullException(nameof(formFieldViewModel));
        DiagnosticsPanelViewModel = diagnosticsPanelViewModel ?? throw new ArgumentNullException(nameof(diagnosticsPanelViewModel));
        LogViewerViewModel = logViewerViewModel ?? throw new ArgumentNullException(nameof(logViewerViewModel));
        AnnotationViewModel = annotationViewModel ?? throw new ArgumentNullException(nameof(annotationViewModel));
        ThumbnailsViewModel = thumbnailsViewModel ?? throw new ArgumentNullException(nameof(thumbnailsViewModel));
        ImageInsertionViewModel = imageInsertionViewModel ?? throw new ArgumentNullException(nameof(imageInsertionViewModel));
        _metricsService = metricsService; // Optional service
        _dpiDetectionService = dpiDetectionService; // Optional service
        _renderingSettingsService = renderingSettingsService; // Optional service
        _settingsService = settingsService; // Optional service
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set up navigation callback for bookmarks
        BookmarksViewModel.SetNavigateToPageAction(async (pageNumber) =>
        {
            await GoToPageCommand.ExecuteAsync(pageNumber);
        });

        // Register message handler for thumbnail navigation
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) =>
        {
            if (m.PageNumber != CurrentPageNumber)
            {
                CurrentPageNumber = m.PageNumber;
                _ = RenderCurrentPageAsync();
            }
        });

        // Subscribe to quality changes if settings service is available
        if (_renderingSettingsService != null)
        {
            _qualitySubscription = _renderingSettingsService.ObserveRenderingQuality()
                .Subscribe(async quality =>
                {
                    CurrentRenderingQuality = quality;
                    _logger.LogInformation("Rendering quality changed to {Quality}", quality);

                    // Re-render current page if document is loaded
                    if (_currentDocument != null && !IsLoading)
                    {
                        await RenderCurrentPageAsync();
                    }
                });
        }

        // Subscribe to property changes from child ViewModels to update HasUnsavedChanges
        AnnotationViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AnnotationViewModel.HasUnsavedChanges))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        };

        FormFieldViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FormFieldViewModel.IsModified))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        };

        ImageInsertionViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageInsertionViewModel.HasUnsavedChanges))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        };

        // Subscribe to page modification events from ThumbnailsViewModel
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            HasPageModifications = true;
        });

        _logger.LogInformation("PdfViewerViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the current page image displayed in the viewer.
    /// </summary>
    [ObservableProperty]
    private BitmapImage? _currentPageImage;

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentPageNumber = 1;

    /// <summary>
    /// Gets or sets the total number of pages in the current document.
    /// </summary>
    [ObservableProperty]
    private int _totalPages;

    /// <summary>
    /// Gets or sets the current zoom level (1.0 = 100%, 2.0 = 200%, etc.).
    /// Allowed values: 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0.
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
    /// Gets or sets the operation progress (0-100).
    /// </summary>
    [ObservableProperty]
    private double _operationProgress;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress (merge, split, optimize).
    /// </summary>
    [ObservableProperty]
    private bool _isOperationInProgress;

    /// <summary>
    /// Gets or sets a value indicating whether the search panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSearchPanelVisible;

    /// <summary>
    /// Gets or sets the current search query entered by the user.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets the list of search matches found in the document.
    /// </summary>
    [ObservableProperty]
    private List<SearchMatch> _searchMatches = new();

    /// <summary>
    /// Gets or sets the index of the currently selected match (0-based).
    /// </summary>
    [ObservableProperty]
    private int _currentMatchIndex = -1;

    /// <summary>
    /// Gets or sets a value indicating whether a search operation is in progress.
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
    /// Updated when a page is rendered. Used for coordinate transformations.
    /// </summary>
    [ObservableProperty]
    private double _currentPageHeight;

    /// <summary>
    /// Gets or sets a value indicating whether text is currently being selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelecting;

    /// <summary>
    /// Gets or sets the selection start point in screen coordinates.
    /// </summary>
    [ObservableProperty]
    private Windows.Foundation.Point _selectionStartPoint;

    /// <summary>
    /// Gets or sets the selection end point in screen coordinates.
    /// </summary>
    [ObservableProperty]
    private Windows.Foundation.Point _selectionEndPoint;

    /// <summary>
    /// Gets or sets the selected text.
    /// </summary>
    [ObservableProperty]
    private string _selectedText = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether there is selected text available for copying.
    /// </summary>
    [ObservableProperty]
    private bool _hasSelectedText;

    /// <summary>
    /// Gets or sets the current display information for HiDPI rendering.
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
    /// Used to show UI feedback during DPI changes.
    /// </summary>
    [ObservableProperty]
    private bool _isAdjustingQuality;

    /// <summary>
    /// Gets or sets a value indicating whether the thumbnails sidebar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSidebarVisible = true;

    /// <summary>
    /// Gets or sets a value indicating whether page operations have been performed.
    /// Set to true when pages are rotated, deleted, reordered, or blank pages are inserted.
    /// </summary>
    [ObservableProperty]
    private bool _hasPageModifications;

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes in the document.
    /// Returns true if annotations, form fields, images, or page operations have been modified.
    /// </summary>
    public bool HasUnsavedChanges =>
        AnnotationViewModel.HasUnsavedChanges || FormFieldViewModel.IsModified || ImageInsertionViewModel.HasUnsavedChanges || HasPageModifications;

    /// <summary>
    /// Opens a file picker dialog and loads the selected PDF document.
    /// </summary>
    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        _logger.LogInformation("OpenDocument command invoked");

        try
        {
            // Create file picker
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Get window handle for WinUI 3
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                _logger.LogInformation("File picker cancelled");
                return;
            }

            IsLoading = true;
            StatusMessage = "Loading document...";

            // Close previous document if any
            if (_currentDocument != null)
            {
                _documentService.CloseDocument(_currentDocument);
                _currentDocument = null;
            }

            // Load document
            var result = await _documentService.LoadDocumentAsync(file.Path);

            if (result.IsFailed)
            {
                _logger.LogError("Failed to load document: {Errors}", result.Errors);
                StatusMessage = $"Failed to load document: {result.Errors[0].Message}";

                // Show error dialog
                await ShowErrorDialogAsync("Failed to Load Document", result.Errors[0].Message);
                return;
            }

            _currentDocument = result.Value;
            TotalPages = _currentDocument.PageCount;
            CurrentPageNumber = 1;

            // Reset page modification flag when loading new document
            HasPageModifications = false;

            _logger.LogInformation(
                "Document loaded successfully. FilePath={FilePath}, PageCount={PageCount}",
                _currentDocument.FilePath, _currentDocument.PageCount);

            // Load bookmarks
            await BookmarksViewModel.LoadBookmarksCommand.ExecuteAsync(_currentDocument);

            // Load thumbnails
            await ThumbnailsViewModel.LoadThumbnailsAsync(_currentDocument);

            // Apply default settings if settings service is available
            ApplyDefaultSettings();

            // Render first page
            await RenderCurrentPageAsync();

            // Load form fields for first page
            await FormFieldViewModel.LoadFormFieldsCommand.ExecuteAsync((_currentDocument, CurrentPageNumber));

            // Load annotations for first page
            await AnnotationViewModel.LoadAnnotationsCommand.ExecuteAsync((_currentDocument, CurrentPageNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while opening document");
            StatusMessage = "Unexpected error while opening document";
            await ShowErrorDialogAsync("Error", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Navigates to the previous page in the document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        _logger.LogInformation("GoToPreviousPage command invoked. CurrentPage={CurrentPage}", CurrentPageNumber);
        CurrentPageNumber--;
        await RenderCurrentPageAsync();
    }

    /// <summary>
    /// Determines whether the GoToPreviousPage command can execute.
    /// </summary>
    /// <returns>true if the command can execute; otherwise, false.</returns>
    private bool CanGoToPreviousPage() => CurrentPageNumber > 1 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Navigates to the next page in the document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task GoToNextPageAsync()
    {
        _logger.LogInformation("GoToNextPage command invoked. CurrentPage={CurrentPage}", CurrentPageNumber);
        CurrentPageNumber++;
        await RenderCurrentPageAsync();
    }

    /// <summary>
    /// Determines whether the GoToNextPage command can execute.
    /// </summary>
    /// <returns>true if the command can execute; otherwise, false.</returns>
    private bool CanGoToNextPage() => CurrentPageNumber < TotalPages && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Increases the zoom level by one step.
    /// Zoom levels: 0.5 -> 0.75 -> 1.0 -> 1.25 -> 1.5 -> 1.75 -> 2.0
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

    /// <summary>
    /// Determines whether the ZoomIn command can execute.
    /// </summary>
    /// <returns>true if the command can execute; otherwise, false.</returns>
    private bool CanZoomIn() => ZoomLevel < 2.0 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Decreases the zoom level by one step.
    /// Zoom levels: 2.0 -> 1.75 -> 1.5 -> 1.25 -> 1.0 -> 0.75 -> 0.5
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

    /// <summary>
    /// Determines whether the ZoomOut command can execute.
    /// </summary>
    /// <returns>true if the command can execute; otherwise, false.</returns>
    private bool CanZoomOut() => ZoomLevel > 0.5 && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Resets the zoom level to 100% (1.0).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetZoom))]
    private async Task ResetZoomAsync()
    {
        _logger.LogInformation("ResetZoom command invoked");
        ZoomLevel = 1.0;
        await RenderCurrentPageAsync();
    }

    /// <summary>
    /// Determines whether the ResetZoom command can execute.
    /// </summary>
    /// <returns>true if the command can execute; otherwise, false.</returns>
    private bool CanResetZoom() => !IsLoading && _currentDocument != null;

    /// <summary>
    /// Navigates to a specific page number.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number to navigate to.</param>
    [RelayCommand]
    private async Task GoToPageAsync(int pageNumber)
    {
        _logger.LogInformation("GoToPage command invoked. PageNumber={PageNumber}", pageNumber);

        if (pageNumber >= 1 && pageNumber <= TotalPages && !IsLoading && _currentDocument != null)
        {
            CurrentPageNumber = pageNumber;
            await RenderCurrentPageAsync();
        }
    }

    /// <summary>
    /// Renders the current page at the current zoom level.
    /// </summary>
    private async Task RenderCurrentPageAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Attempted to render page with no document loaded");
            return;
        }

        IsLoading = true;
        StatusMessage = $"Rendering page {CurrentPageNumber}...";
        var renderStartTime = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Calculate effective DPI if DPI detection is available
            double effectiveDpi = 96.0; // Default standard DPI
            if (_dpiDetectionService != null && CurrentDisplayInfo != null)
            {
                var dpiResult = _dpiDetectionService.CalculateEffectiveDpi(
                    CurrentDisplayInfo,
                    ZoomLevel,
                    CurrentRenderingQuality);

                if (dpiResult.IsSuccess)
                {
                    effectiveDpi = dpiResult.Value;
                    _logger.LogDebug(
                        "Calculated effective DPI: {EffectiveDpi} (Display={DisplayDpi}, Zoom={Zoom}, Quality={Quality})",
                        effectiveDpi,
                        CurrentDisplayInfo.EffectiveDpi,
                        ZoomLevel,
                        CurrentRenderingQuality);
                }
                else
                {
                    _logger.LogWarning("Failed to calculate effective DPI, using default: {Errors}", dpiResult.Errors);
                }
            }

            var result = await _renderingService.RenderPageAsync(
                _currentDocument,
                CurrentPageNumber,
                ZoomLevel,
                effectiveDpi);

            if (result.IsSuccess)
            {
                // Convert Stream to BitmapImage for WinUI
                var bitmapImage = new BitmapImage();
                var randomAccessStream = await ConvertStreamToRandomAccessStreamAsync(result.Value);
                await bitmapImage.SetSourceAsync(randomAccessStream);

                CurrentPageImage = bitmapImage;
                StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";

                renderStartTime.Stop();

                // Update last rendered DPI
                _lastRenderedDpi = effectiveDpi;

                _logger.LogInformation(
                    "Page rendered successfully. PageNumber={PageNumber}, ZoomLevel={ZoomLevel}, EffectiveDpi={EffectiveDpi}, RenderTime={RenderTimeMs}ms",
                    CurrentPageNumber, ZoomLevel, effectiveDpi, renderStartTime.ElapsedMilliseconds);

                // Record render time metrics if metrics service is available
                _metricsService?.RecordRenderTime(CurrentPageNumber, renderStartTime.ElapsedMilliseconds);

                // Update current page number in diagnostics panel
                DiagnosticsPanelViewModel.CurrentPageNumber = CurrentPageNumber;

                // Get page dimensions for coordinate transformations
                UpdateCurrentPageDimensions();

                // Reload form fields for the current page
                await FormFieldViewModel.LoadFormFieldsCommand.ExecuteAsync((_currentDocument, CurrentPageNumber));

                // Load annotations for the current page
                await AnnotationViewModel.LoadAnnotationsCommand.ExecuteAsync((_currentDocument, CurrentPageNumber));
            }
            else
            {
                _logger.LogError("Failed to render page: {Errors}", result.Errors);
                StatusMessage = "Failed to render page";
                await ShowErrorDialogAsync("Rendering Error", result.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while rendering page");
            StatusMessage = "Unexpected error while rendering page";
            await ShowErrorDialogAsync("Error", $"Failed to render page: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
    /// Shows an error dialog to the user.
    /// </summary>
    private static async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Merges multiple PDF documents into a single file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteMerge))]
    private async Task MergeDocumentsAsync()
    {
        _logger.LogInformation("MergeDocuments command invoked");

        try
        {
            // Create file picker for multiple PDFs
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".pdf");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files == null || files.Count < 2)
            {
                _logger.LogInformation("Merge cancelled or insufficient files selected");
                await ShowErrorDialogAsync("Merge Error", "Please select at least 2 PDF files to merge.");
                return;
            }

            // Create save picker for output
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
            savePicker.SuggestedFileName = "merged.pdf";
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var outputFile = await savePicker.PickSaveFileAsync();
            if (outputFile == null)
            {
                _logger.LogInformation("Merge output cancelled");
                return;
            }

            IsOperationInProgress = true;
            OperationProgress = 0;
            StatusMessage = "Merging PDF documents...";
            _operationCts = new CancellationTokenSource();

            var progress = new Progress<double>(value =>
            {
                OperationProgress = value;
                StatusMessage = $"Merging PDF documents... {value:F1}%";
            });

            var sourcePaths = files.Select(f => f.Path).ToList();
            var result = await _editingService.MergeAsync(
                sourcePaths,
                outputFile.Path,
                progress,
                _operationCts.Token);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Merge completed successfully. Output={OutputPath}", result.Value);
                StatusMessage = $"Successfully merged {files.Count} PDFs";
                await ShowErrorDialogAsync("Success", $"Merged {files.Count} PDFs into:\n{result.Value}");
            }
            else
            {
                _logger.LogError("Merge failed: {Errors}", result.Errors);
                StatusMessage = "Merge operation failed";
                await ShowErrorDialogAsync("Merge Error", result.Errors[0].Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Merge operation cancelled by user");
            StatusMessage = "Merge operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during merge operation");
            StatusMessage = "Unexpected error during merge";
            await ShowErrorDialogAsync("Error", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
            OperationProgress = 0;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private bool CanExecuteMerge() => !IsLoading && !IsOperationInProgress;

    /// <summary>
    /// Splits the current PDF document by extracting specified page ranges.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSplit))]
    private async Task SplitDocumentAsync()
    {
        _logger.LogInformation("SplitDocument command invoked");

        try
        {
            if (_currentDocument == null)
            {
                await ShowErrorDialogAsync("Split Error", "No document is currently loaded.");
                return;
            }

            // Prompt user for page ranges
            var inputDialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Split PDF",
                Content = new Microsoft.UI.Xaml.Controls.TextBox
                {
                    PlaceholderText = "Enter page ranges (e.g., 1-5, 10, 15-20)",
                    AcceptsReturn = false
                },
                PrimaryButtonText = "Split",
                CloseButtonText = "Cancel",
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            var dialogResult = await inputDialog.ShowAsync();
            if (dialogResult != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                _logger.LogInformation("Split cancelled by user");
                return;
            }

            var pageRanges = ((Microsoft.UI.Xaml.Controls.TextBox)inputDialog.Content).Text;
            if (string.IsNullOrWhiteSpace(pageRanges))
            {
                await ShowErrorDialogAsync("Split Error", "Please enter valid page ranges.");
                return;
            }

            // Create save picker for output
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
            savePicker.SuggestedFileName = "split.pdf";
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var outputFile = await savePicker.PickSaveFileAsync();
            if (outputFile == null)
            {
                _logger.LogInformation("Split output cancelled");
                return;
            }

            IsOperationInProgress = true;
            OperationProgress = 0;
            StatusMessage = "Splitting PDF document...";
            _operationCts = new CancellationTokenSource();

            var progress = new Progress<double>(value =>
            {
                OperationProgress = value;
                StatusMessage = $"Splitting PDF document... {value:F1}%";
            });

            var result = await _editingService.SplitAsync(
                _currentDocument.FilePath,
                pageRanges,
                outputFile.Path,
                progress,
                _operationCts.Token);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Split completed successfully. Output={OutputPath}", result.Value);
                StatusMessage = "Successfully split PDF";
                await ShowErrorDialogAsync("Success", $"Split PDF saved to:\n{result.Value}");
            }
            else
            {
                _logger.LogError("Split failed: {Errors}", result.Errors);
                StatusMessage = "Split operation failed";
                await ShowErrorDialogAsync("Split Error", result.Errors[0].Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Split operation cancelled by user");
            StatusMessage = "Split operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during split operation");
            StatusMessage = "Unexpected error during split";
            await ShowErrorDialogAsync("Error", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
            OperationProgress = 0;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private bool CanExecuteSplit() => !IsLoading && !IsOperationInProgress && _currentDocument != null;

    /// <summary>
    /// Optimizes the current PDF document to reduce file size.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteOptimize))]
    private async Task OptimizeDocumentAsync()
    {
        _logger.LogInformation("OptimizeDocument command invoked");

        try
        {
            if (_currentDocument == null)
            {
                await ShowErrorDialogAsync("Optimize Error", "No document is currently loaded.");
                return;
            }

            // Create save picker for output
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
            savePicker.SuggestedFileName = "optimized.pdf";
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var outputFile = await savePicker.PickSaveFileAsync();
            if (outputFile == null)
            {
                _logger.LogInformation("Optimize output cancelled");
                return;
            }

            IsOperationInProgress = true;
            OperationProgress = 0;
            StatusMessage = "Optimizing PDF document...";
            _operationCts = new CancellationTokenSource();

            var progress = new Progress<double>(value =>
            {
                OperationProgress = value;
                StatusMessage = $"Optimizing PDF document... {value:F1}%";
            });

            var options = new OptimizationOptions
            {
                CompressStreams = true,
                RemoveUnusedObjects = true,
                DeduplicateResources = true,
                Linearize = false,
                PreserveEncryption = true
            };

            var result = await _editingService.OptimizeAsync(
                _currentDocument.FilePath,
                outputFile.Path,
                options,
                progress,
                _operationCts.Token);

            if (result.IsSuccess)
            {
                var optimizationResult = result.Value;
                _logger.LogInformation(
                    "Optimization completed. OriginalSize={OriginalSize}, OptimizedSize={OptimizedSize}, Reduction={Reduction}%",
                    optimizationResult.OriginalSize, optimizationResult.OptimizedSize, optimizationResult.ReductionPercentage);

                StatusMessage = $"Optimization complete - {optimizationResult.ReductionPercentage:F1}% reduction";

                var message = $"Optimized PDF saved to:\n{optimizationResult.OutputPath}\n\n" +
                              $"Original size: {optimizationResult.OriginalSize / 1024.0:F1} KB\n" +
                              $"Optimized size: {optimizationResult.OptimizedSize / 1024.0:F1} KB\n" +
                              $"Reduction: {optimizationResult.ReductionPercentage:F1}%\n" +
                              $"Processing time: {optimizationResult.ProcessingTime.TotalSeconds:F2}s";

                await ShowErrorDialogAsync("Success", message);
            }
            else
            {
                _logger.LogError("Optimization failed: {Errors}", result.Errors);
                StatusMessage = "Optimization operation failed";
                await ShowErrorDialogAsync("Optimization Error", result.Errors[0].Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Optimization operation cancelled by user");
            StatusMessage = "Optimization operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during optimization operation");
            StatusMessage = "Unexpected error during optimization";
            await ShowErrorDialogAsync("Error", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
            OperationProgress = 0;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private bool CanExecuteOptimize() => !IsLoading && !IsOperationInProgress && _currentDocument != null;

    /// <summary>
    /// Saves the current document with all annotations and form field changes to the current file path.
    /// Creates a backup before saving.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        _logger.LogInformation("Save command invoked");

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot save: no document loaded");
            await ShowErrorDialogAsync("Save Error", "No document is currently loaded.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving document...";

            // Save annotations if there are unsaved changes
            if (AnnotationViewModel.HasUnsavedChanges)
            {
                _logger.LogInformation("Saving annotations to {FilePath}", _currentDocument.FilePath);
                await AnnotationViewModel.SaveAnnotationsCommand.ExecuteAsync(null);
            }

            // Save form data if modified
            if (FormFieldViewModel.IsModified)
            {
                _logger.LogInformation("Saving form data to {FilePath}", _currentDocument.FilePath);
                await FormFieldViewModel.SaveFormCommand.ExecuteAsync(_currentDocument.FilePath);
            }

            // Note: Image changes are already persisted to the document via IImageInsertionService
            // We just need to reset the flag after saving the document
            if (ImageInsertionViewModel.HasUnsavedChanges)
            {
                _logger.LogInformation("Image changes already persisted to document");
                ImageInsertionViewModel.HasUnsavedChanges = false;
            }

            StatusMessage = $"Document saved: {Path.GetFileName(_currentDocument.FilePath)}";
            _logger.LogInformation("Document saved successfully to {FilePath}", _currentDocument.FilePath);

            // Reset page modification flag after successful save
            HasPageModifications = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving document");
            StatusMessage = "Error saving document";
            await ShowErrorDialogAsync("Save Error", $"Failed to save document: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Determines whether the Save command can execute.
    /// </summary>
    private bool CanSave() => HasUnsavedChanges && !IsLoading && _currentDocument != null;

    /// <summary>
    /// Saves the current document with all annotations and form field changes to a new file path.
    /// Prompts user to select the output location using FileSavePicker.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveAs))]
    private async Task SaveAsAsync()
    {
        _logger.LogInformation("SaveAs command invoked");

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot save as: no document loaded");
            await ShowErrorDialogAsync("Save As Error", "No document is currently loaded.");
            return;
        }

        try
        {
            // Create save picker for output
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
            savePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(_currentDocument.FilePath) + "_copy.pdf";
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var outputFile = await savePicker.PickSaveFileAsync();
            if (outputFile == null)
            {
                _logger.LogInformation("Save As cancelled by user");
                return;
            }

            IsLoading = true;
            StatusMessage = "Saving document...";

            // Save annotations to new path
            if (AnnotationViewModel.Annotations.Count > 0 || AnnotationViewModel.HasUnsavedChanges)
            {
                _logger.LogInformation("Saving annotations to {FilePath}", outputFile.Path);
                await AnnotationViewModel.SaveAnnotationsCommand.ExecuteAsync(null);
            }

            // Save form data to new path
            if (FormFieldViewModel.HasFormFields || FormFieldViewModel.IsModified)
            {
                _logger.LogInformation("Saving form data to {FilePath}", outputFile.Path);
                await FormFieldViewModel.SaveFormCommand.ExecuteAsync(outputFile.Path);
            }

            // Note: Image changes are already persisted to the document via IImageInsertionService
            // We just need to reset the flag after saving the document
            if (ImageInsertionViewModel.HasUnsavedChanges)
            {
                _logger.LogInformation("Image changes already persisted to document");
                ImageInsertionViewModel.HasUnsavedChanges = false;
            }

            StatusMessage = $"Document saved as: {Path.GetFileName(outputFile.Path)}";
            _logger.LogInformation("Document saved successfully to {FilePath}", outputFile.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving document");
            StatusMessage = "Error saving document";
            await ShowErrorDialogAsync("Save As Error", $"Failed to save document: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Determines whether the SaveAs command can execute.
    /// </summary>
    private bool CanSaveAs() => !IsLoading && _currentDocument != null;

    /// <summary>
    /// Cancels the current document editing operation (merge, split, or optimize).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelOperation))]
    private void CancelOperation()
    {
        _logger.LogInformation("CancelOperation command invoked");
        _operationCts?.Cancel();
        StatusMessage = "Cancelling operation...";
    }

    private bool CanCancelOperation() => IsOperationInProgress && _operationCts != null;

    /// <summary>
    /// Toggles the visibility of the search panel.
    /// When shown, focuses the search box for immediate input.
    /// </summary>
    [RelayCommand]
    private void ToggleSearchPanel()
    {
        _logger.LogInformation("ToggleSearchPanel command invoked");
        IsSearchPanelVisible = !IsSearchPanelVisible;

        // Clear search when hiding panel
        if (!IsSearchPanelVisible)
        {
            SearchQuery = string.Empty;
            SearchMatches.Clear();
            CurrentMatchIndex = -1;
            _searchCts?.Cancel();
        }
    }

    /// <summary>
    /// Toggles the visibility of the thumbnails sidebar.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        _logger.LogInformation("ToggleSidebar command invoked");
        IsSidebarVisible = !IsSidebarVisible;

        // Announce sidebar state change to screen readers
        RaiseAccessibilityNotification(
            IsSidebarVisible ? "Thumbnails sidebar shown" : "Thumbnails sidebar hidden");
    }

    /// <summary>
    /// Raises an accessibility notification for screen readers.
    /// </summary>
    /// <param name="message">The message to announce.</param>
    private void RaiseAccessibilityNotification(string message)
    {
        try
        {
            // This will be handled by the View to raise the notification
            // using AutomationPeer.RaiseNotificationEvent
            WeakReferenceMessenger.Default.Send(new AccessibilityNotificationMessage(message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to raise accessibility notification: {Message}", message);
        }
    }

    /// <summary>
    /// Initiates a debounced search operation.
    /// Search executes 300ms after the last query change.
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        _logger.LogInformation("Search command invoked. Query={Query}", SearchQuery);

        // Cancel any pending debounced search
        _searchDebounceTimer?.Dispose();

        // Cancel any in-progress search
        _searchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(SearchQuery) || _currentDocument == null)
        {
            SearchMatches.Clear();
            CurrentMatchIndex = -1;
            return;
        }

        // Debounce search - execute after 300ms delay
        _searchDebounceTimer = new System.Threading.Timer(
            async _ => await ExecuteSearchAsync(),
            null,
            TimeSpan.FromMilliseconds(300),
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Executes the actual search operation asynchronously.
    /// </summary>
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
            IsSearching = true;
            _logger.LogInformation("Executing search. Query={Query}, CaseSensitive={CaseSensitive}",
                SearchQuery, CaseSensitive);

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

                // Navigate to first match if available
                if (CurrentMatchIndex >= 0)
                {
                    await NavigateToCurrentMatchAsync();
                }
            }
            else
            {
                _logger.LogError("Search failed: {Errors}", result.Errors);
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
            IsSearching = false;
        }
    }

    /// <summary>
    /// Navigates to the next search match in the document.
    /// Wraps around to the first match when reaching the end.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateToNextMatch))]
    private async Task GoToNextMatchAsync()
    {
        _logger.LogInformation("GoToNextMatch command invoked. CurrentIndex={CurrentIndex}", CurrentMatchIndex);

        if (SearchMatches.Count == 0)
        {
            return;
        }

        CurrentMatchIndex = (CurrentMatchIndex + 1) % SearchMatches.Count;
        await NavigateToCurrentMatchAsync();
    }

    private bool CanNavigateToNextMatch() => SearchMatches.Count > 0 && !IsSearching;

    /// <summary>
    /// Navigates to the previous search match in the document.
    /// Wraps around to the last match when reaching the beginning.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateToPreviousMatch))]
    private async Task GoToPreviousMatchAsync()
    {
        _logger.LogInformation("GoToPreviousMatch command invoked. CurrentIndex={CurrentIndex}", CurrentMatchIndex);

        if (SearchMatches.Count == 0)
        {
            return;
        }

        CurrentMatchIndex = (CurrentMatchIndex - 1 + SearchMatches.Count) % SearchMatches.Count;
        await NavigateToCurrentMatchAsync();
    }

    private bool CanNavigateToPreviousMatch() => SearchMatches.Count > 0 && !IsSearching;

    /// <summary>
    /// Navigates to the page containing the current search match.
    /// </summary>
    private async Task NavigateToCurrentMatchAsync()
    {
        if (CurrentMatchIndex < 0 || CurrentMatchIndex >= SearchMatches.Count)
        {
            return;
        }

        var match = SearchMatches[CurrentMatchIndex];
        var targetPage = match.PageNumber + 1; // Convert 0-based to 1-based

        _logger.LogInformation(
            "Navigating to match. Index={Index}, Page={Page}, CharIndex={CharIndex}",
            CurrentMatchIndex, targetPage, match.CharIndex);

        // Navigate to the page if not already there
        if (CurrentPageNumber != targetPage)
        {
            await GoToPageCommand.ExecuteAsync(targetPage);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Update command CanExecute states when relevant properties change
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

            _logger.LogDebug(
                "Command states updated. Property={PropertyName}, IsLoading={IsLoading}",
                e.PropertyName, IsLoading);
        }

        // Update thumbnails selection when current page changes
        if (e.PropertyName == nameof(CurrentPageNumber))
        {
            ThumbnailsViewModel.UpdateSelectedPage(CurrentPageNumber);
        }

        // Update document editing command states
        if (e.PropertyName == nameof(IsLoading) ||
            e.PropertyName == nameof(IsOperationInProgress))
        {
            MergeDocumentsCommand.NotifyCanExecuteChanged();
            SplitDocumentCommand.NotifyCanExecuteChanged();
            OptimizeDocumentCommand.NotifyCanExecuteChanged();
            CancelOperationCommand.NotifyCanExecuteChanged();
        }

        // Update search command states
        if (e.PropertyName == nameof(IsSearching) ||
            e.PropertyName == nameof(SearchMatches))
        {
            GoToNextMatchCommand.NotifyCanExecuteChanged();
            GoToPreviousMatchCommand.NotifyCanExecuteChanged();
        }

        // Update copy command state
        if (e.PropertyName == nameof(HasSelectedText))
        {
            CopyToClipboardCommand.NotifyCanExecuteChanged();
        }

        // Update save command states
        if (e.PropertyName == nameof(HasUnsavedChanges))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        // Update HasUnsavedChanges when page modifications change
        if (e.PropertyName == nameof(HasPageModifications))
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        // Trigger search when query or case sensitivity changes
        if (e.PropertyName == nameof(SearchQuery) ||
            e.PropertyName == nameof(CaseSensitive))
        {
            SearchCommand.Execute(null);
        }
    }

    /// <summary>
    /// Updates the current page dimensions by loading the page and getting its dimensions.
    /// Used for coordinate transformations in search highlights and form fields.
    /// </summary>
    private void UpdateCurrentPageDimensions()
    {
        if (_currentDocument == null)
        {
            return;
        }

        try
        {
            var documentHandle = (SafePdfDocumentHandle)_currentDocument.Handle;
            using var pageHandle = PdfiumInterop.LoadPage(documentHandle, CurrentPageNumber - 1);

            if (!pageHandle.IsInvalid)
            {
                CurrentPageHeight = PdfiumInterop.GetPageHeight(pageHandle);
                _logger.LogDebug(
                    "Updated page dimensions. PageNumber={PageNumber}, Height={Height}",
                    CurrentPageNumber, CurrentPageHeight);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get page dimensions for page {PageNumber}", CurrentPageNumber);
        }
    }

    /// <summary>
    /// Begins text selection at the specified point.
    /// </summary>
    /// <param name="point">The starting point in screen coordinates.</param>
    [RelayCommand]
    private void BeginTextSelection(Windows.Foundation.Point point)
    {
        _logger.LogInformation("BeginTextSelection invoked at ({X}, {Y})", point.X, point.Y);

        IsSelecting = true;
        SelectionStartPoint = point;
        SelectionEndPoint = point;
        SelectedText = string.Empty;
        HasSelectedText = false;
    }

    /// <summary>
    /// Updates the text selection endpoint as the user drags.
    /// </summary>
    /// <param name="point">The current point in screen coordinates.</param>
    [RelayCommand]
    private void UpdateTextSelection(Windows.Foundation.Point point)
    {
        if (!IsSelecting)
        {
            return;
        }

        SelectionEndPoint = point;
    }

    /// <summary>
    /// Completes text selection and extracts the selected text.
    /// </summary>
    [RelayCommand]
    private async Task EndTextSelectionAsync()
    {
        if (!IsSelecting || _currentDocument == null)
        {
            IsSelecting = false;
            return;
        }

        _logger.LogInformation("EndTextSelection invoked");

        try
        {
            // For now, extract all text from the current page
            // In a more sophisticated implementation, we would extract only the text within the selection bounds
            var result = await _textExtractionService.ExtractTextAsync(_currentDocument, CurrentPageNumber);

            if (result.IsSuccess)
            {
                SelectedText = result.Value;
                HasSelectedText = !string.IsNullOrWhiteSpace(SelectedText);

                _logger.LogInformation(
                    "Text extraction completed. Length={Length}, HasText={HasText}",
                    SelectedText.Length, HasSelectedText);
            }
            else
            {
                _logger.LogError("Failed to extract text: {Errors}", result.Errors);
                SelectedText = string.Empty;
                HasSelectedText = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during text extraction");
            SelectedText = string.Empty;
            HasSelectedText = false;
        }
        finally
        {
            IsSelecting = false;
        }
    }

    /// <summary>
    /// Copies the selected text to the clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopyToClipboardAsync()
    {
        _logger.LogInformation("CopyToClipboard command invoked");

        try
        {
            if (!string.IsNullOrWhiteSpace(SelectedText))
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(SelectedText);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                _logger.LogInformation("Text copied to clipboard. Length={Length}", SelectedText.Length);

                // Clear selection after copying
                ClearSelection();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
            await ShowErrorDialogAsync("Copy Error", $"Failed to copy text: {ex.Message}");
        }
    }

    private bool CanCopyToClipboard() => HasSelectedText;

    /// <summary>
    /// Clears the current text selection.
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        _logger.LogInformation("ClearSelection invoked");

        IsSelecting = false;
        SelectedText = string.Empty;
        HasSelectedText = false;
        SelectionStartPoint = new Windows.Foundation.Point();
        SelectionEndPoint = new Windows.Foundation.Point();
    }

    /// <summary>
    /// Toggles the visibility of the diagnostics panel.
    /// </summary>
    [RelayCommand]
    private void ToggleDiagnostics()
    {
        _logger.LogInformation("ToggleDiagnostics command invoked");
        DiagnosticsPanelViewModel.ToggleVisibilityCommand.Execute(null);
    }

    /// <summary>
    /// Opens the log viewer in a content dialog.
    /// </summary>
    [RelayCommand]
    private async Task OpenLogViewerAsync()
    {
        _logger.LogInformation("OpenLogViewer command invoked");

        try
        {
            // Load logs when opening the viewer
            await LogViewerViewModel.LoadLogsCommand.ExecuteAsync(null);

            // Create content dialog with log viewer control
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Application Logs",
                Content = new Controls.LogViewerControl
                {
                    DataContext = LogViewerViewModel,
                    MinWidth = 800,
                    MinHeight = 600
                },
                CloseButtonText = "Close",
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open log viewer");
            await ShowErrorDialogAsync("Error", $"Failed to open log viewer: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts monitoring DPI changes for the current display.
    /// </summary>
    /// <param name="xamlRoot">The XamlRoot to monitor for DPI changes.</param>
    public void StartDpiMonitoring(object? xamlRoot)
    {
        if (_dpiDetectionService == null)
        {
            _logger.LogWarning("DPI detection service not available, cannot start DPI monitoring");
            return;
        }

        // Stop existing subscription if any
        _dpiSubscription?.Dispose();
        _dpiSubscription = null;

        // Get initial display info
        var displayInfoResult = _dpiDetectionService.GetCurrentDisplayInfo(xamlRoot);
        if (displayInfoResult.IsSuccess)
        {
            CurrentDisplayInfo = displayInfoResult.Value;
            _lastRenderedDpi = displayInfoResult.Value.EffectiveDpi;
            _logger.LogInformation(
                "Initial display info detected. Scale={Scale}, EffectiveDpi={EffectiveDpi}",
                displayInfoResult.Value.RasterizationScale,
                displayInfoResult.Value.EffectiveDpi);
        }
        else
        {
            _logger.LogWarning("Failed to get initial display info: {Errors}", displayInfoResult.Errors);
            CurrentDisplayInfo = DisplayInfo.Standard();
            _lastRenderedDpi = 96.0;
        }

        // Monitor DPI changes
        var monitorResult = _dpiDetectionService.MonitorDpiChanges(xamlRoot);
        if (monitorResult.IsSuccess)
        {
            _dpiSubscription = monitorResult.Value.Subscribe(async displayInfo =>
            {
                await OnDpiChangedAsync(displayInfo);
            });

            _logger.LogInformation("DPI monitoring started successfully");
        }
        else
        {
            _logger.LogWarning("Failed to start DPI monitoring: {Errors}", monitorResult.Errors);
        }
    }

    /// <summary>
    /// Handles DPI changes and re-renders if the change is significant.
    /// </summary>
    private async Task OnDpiChangedAsync(DisplayInfo newDisplayInfo)
    {
        CurrentDisplayInfo = newDisplayInfo;

        _logger.LogInformation(
            "DPI changed. Scale={Scale}, EffectiveDpi={EffectiveDpi}",
            newDisplayInfo.RasterizationScale,
            newDisplayInfo.EffectiveDpi);

        // Calculate the new effective DPI for rendering
        if (_dpiDetectionService == null || _currentDocument == null)
        {
            return;
        }

        var effectiveDpiResult = _dpiDetectionService.CalculateEffectiveDpi(
            newDisplayInfo,
            ZoomLevel,
            CurrentRenderingQuality);

        if (effectiveDpiResult.IsFailed)
        {
            _logger.LogWarning("Failed to calculate effective DPI: {Errors}", effectiveDpiResult.Errors);
            return;
        }

        var newEffectiveDpi = effectiveDpiResult.Value;

        // Check if DPI change is significant (> 10% threshold)
        var dpiChangePercentage = Math.Abs(newEffectiveDpi - _lastRenderedDpi) / _lastRenderedDpi;
        if (dpiChangePercentage > 0.10)
        {
            _logger.LogInformation(
                "Significant DPI change detected ({ChangePercent:P1}). Re-rendering page. OldDpi={OldDpi}, NewDpi={NewDpi}",
                dpiChangePercentage,
                _lastRenderedDpi,
                newEffectiveDpi);

            IsAdjustingQuality = true;
            try
            {
                await RenderCurrentPageAsync();
            }
            finally
            {
                IsAdjustingQuality = false;
            }
        }
        else
        {
            _logger.LogDebug(
                "DPI change below threshold ({ChangePercent:P1}), skipping re-render. OldDpi={OldDpi}, NewDpi={NewDpi}",
                dpiChangePercentage,
                _lastRenderedDpi,
                newEffectiveDpi);
        }
    }

    /// <summary>
    /// Applies default settings from the settings service when opening a new document.
    /// Sets zoom level and scroll mode based on user preferences.
    /// </summary>
    private void ApplyDefaultSettings()
    {
        if (_settingsService == null)
        {
            _logger.LogDebug("Settings service not available, using default zoom and scroll mode");
            return;
        }

        var settings = _settingsService.Settings;

        // Apply default zoom level
        var zoomValue = ConvertZoomLevelToDouble(settings.DefaultZoom);
        if (zoomValue.HasValue)
        {
            ZoomLevel = zoomValue.Value;
            _logger.LogInformation(
                "Applied default zoom level from settings: {ZoomLevel} ({ZoomValue})",
                settings.DefaultZoom, zoomValue.Value);
        }

        // Note: ScrollMode is currently not implemented in the UI, but we log it for future use
        _logger.LogInformation("Default scroll mode from settings: {ScrollMode}", settings.ScrollMode);
    }

    /// <summary>
    /// Converts a ZoomLevel enum value to its corresponding double representation.
    /// </summary>
    /// <param name="zoomLevel">The zoom level enum value.</param>
    /// <returns>The zoom level as a double (0.5 to 2.0), or null if the zoom level is FitWidth or FitPage.</returns>
    private static double? ConvertZoomLevelToDouble(Core.Models.ZoomLevel zoomLevel)
    {
        return zoomLevel switch
        {
            Core.Models.ZoomLevel.FiftyPercent => 0.5,
            Core.Models.ZoomLevel.SeventyFivePercent => 0.75,
            Core.Models.ZoomLevel.OneHundredPercent => 1.0,
            Core.Models.ZoomLevel.OneTwentyFivePercent => 1.25,
            Core.Models.ZoomLevel.OneFiftyPercent => 1.5,
            Core.Models.ZoomLevel.OneSeventyFivePercent => 1.75,
            Core.Models.ZoomLevel.TwoHundredPercent => 2.0,
            Core.Models.ZoomLevel.FitWidth => null, // Not currently supported
            Core.Models.ZoomLevel.FitPage => null,  // Not currently supported
            _ => 1.0 // Default to 100% for unknown values
        };
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

        _logger.LogInformation("Disposing PdfViewerViewModel");

        // Unregister message handlers
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Unregister<NavigateToPageMessage>(this);
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);

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

        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = null;

        _dpiSubscription?.Dispose();
        _dpiSubscription = null;

        _qualitySubscription?.Dispose();
        _qualitySubscription = null;

        _disposed = true;
    }
}
