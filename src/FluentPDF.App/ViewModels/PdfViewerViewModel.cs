using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
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
    private readonly ILogger<PdfViewerViewModel> _logger;
    private PdfDocument? _currentDocument;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerViewModel"/> class.
    /// </summary>
    /// <param name="documentService">Service for loading PDF documents.</param>
    /// <param name="renderingService">Service for rendering PDF pages.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public PdfViewerViewModel(
        IPdfDocumentService documentService,
        IPdfRenderingService renderingService,
        ILogger<PdfViewerViewModel> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

            _logger.LogInformation(
                "Document loaded successfully. FilePath={FilePath}, PageCount={PageCount}",
                _currentDocument.FilePath, _currentDocument.PageCount);

            // Render first page
            await RenderCurrentPageAsync();
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

        try
        {
            var result = await _renderingService.RenderPageAsync(
                _currentDocument,
                CurrentPageNumber,
                ZoomLevel);

            if (result.IsSuccess)
            {
                // Convert Stream to BitmapImage for WinUI
                var bitmapImage = new BitmapImage();
                var randomAccessStream = await ConvertStreamToRandomAccessStreamAsync(result.Value);
                await bitmapImage.SetSourceAsync(randomAccessStream);

                CurrentPageImage = bitmapImage;
                StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";

                _logger.LogInformation(
                    "Page rendered successfully. PageNumber={PageNumber}, ZoomLevel={ZoomLevel}",
                    CurrentPageNumber, ZoomLevel);
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

        if (_currentDocument != null)
        {
            _documentService.CloseDocument(_currentDocument);
            _currentDocument = null;
        }

        _disposed = true;
    }
}
