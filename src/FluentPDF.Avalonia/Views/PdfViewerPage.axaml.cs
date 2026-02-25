using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FluentPDF.Core.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Main PDF viewer page that displays the PDF document with toolbar controls.
/// Implements text selection and annotation functionality.
/// </summary>
public partial class PdfViewerPage : UserControl
{
    private PdfViewerViewModel? _viewModel;
    private Point _selectionStartPoint;
    private bool _isSelecting;
    private readonly ILogger<PdfViewerPage> _logger;

    // Middle button panning
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerPage"/> class.
    /// Default constructor for design-time.
    /// </summary>
    public PdfViewerPage()
    {
        InitializeComponent();

        // Get logger from DI
        _logger = App.GetService<ILogger<PdfViewerPage>>();

        // Wire up pointer events for text selection
        this.Loaded += OnLoaded;

        // Wire up DataContextChanged to set RenderPageCallback
        this.DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerPage"/> class.
    /// Constructor with view model injection.
    /// </summary>
    /// <param name="viewModel">The PDF viewer view model.</param>
    public PdfViewerPage(PdfViewerViewModel viewModel)
    {
        InitializeComponent();

        // Get logger from DI
        _logger = App.GetService<ILogger<PdfViewerPage>>();

        DataContext = viewModel;
        _viewModel = viewModel;

        // Wire up RenderPageCallback immediately
        if (_viewModel != null)
        {
            _viewModel.RenderPageCallback = RenderPageAsync;
            if (_viewModel.Thumbnails != null)
            {
                _viewModel.Thumbnails.RenderThumbnailCallback = RenderThumbnailAsync;
            }
        }

        // Wire up pointer events for text selection
        this.Loaded += OnLoaded;

        // Wire up DataContextChanged to set RenderPageCallback
        this.DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Handles DataContextChanged to wire up RenderPageCallback.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PdfViewerViewModel vm)
        {
            _viewModel = vm;
            vm.RenderPageCallback = RenderPageAsync;
            if (vm.Thumbnails != null)
            {
                vm.Thumbnails.RenderThumbnailCallback = RenderThumbnailAsync;
            }
            _logger.LogDebug("RenderPageCallback wired up for PdfViewerViewModel");
        }
    }

    /// <summary>
    /// Handles the Loaded event to wire up event handlers.
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Get ViewModel from DataContext if not already set
        if (_viewModel == null && DataContext is PdfViewerViewModel vm)
        {
            _viewModel = vm;
            vm.RenderPageCallback = RenderPageAsync;
            if (vm.Thumbnails != null)
            {
                vm.Thumbnails.RenderThumbnailCallback = RenderThumbnailAsync;
            }
        }

        // Detect screen DPI and set CurrentDisplayInfo
        if (_viewModel != null)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var scaling = topLevel.RenderScaling;
                _viewModel.CurrentDisplayInfo = DisplayInfo.FromScale(scaling);
                _logger.LogDebug("Display DPI detected: scale={Scale}, effectiveDpi={Dpi}",
                    scaling, 96.0 * scaling);
            }
        }

        // Wire up pointer events on the PDF image
        if (PdfImage != null)
        {
            PdfImage.PointerPressed += OnImagePointerPressed;
            PdfImage.PointerMoved += OnImagePointerMoved;
            PdfImage.PointerReleased += OnImagePointerReleased;
        }

        // Wire up scroll viewer mouse wheel for zoom and middle button panning
        if (PdfScrollViewer != null)
        {
            PdfScrollViewer.AddHandler(PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
            PdfScrollViewer.PointerPressed += OnScrollViewerPointerPressed;
            PdfScrollViewer.PointerMoved += OnScrollViewerPointerMoved;
            PdfScrollViewer.PointerReleased += OnScrollViewerPointerReleased;
        }

        // Wire up keyboard events for annotation shortcuts and text copy
        this.KeyDown += OnPageKeyDown;
        this.KeyDown += OnPageKeyDown_CopyText;

        // Subscribe to ViewModel property changes
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Renders a PDF page to an Avalonia bitmap.
    /// Called by PdfViewerViewModel to render the current page.
    /// </summary>
    /// <param name="document">The PDF document to render from.</param>
    /// <param name="pageNumber">The 1-based page number to render.</param>
    /// <param name="zoomLevel">The zoom level (1.0 = 100%).</param>
    /// <param name="dpi">The DPI for rendering.</param>
    /// <returns>An Avalonia Bitmap object, or null if rendering fails.</returns>
    private async Task<object?> RenderPageAsync(
        PdfDocument document,
        int pageNumber,
        double zoomLevel,
        double dpi)
    {
        try
        {
            var renderingService = App.GetService<IPdfRenderingService>();
            var result = await renderingService.RenderPageToRawAsync(document, pageNumber, zoomLevel, dpi);

            if (!result.IsSuccess)
            {
                _logger.LogError("Rendering failed for page {PageNumber}: {Error}",
                    pageNumber, result.Errors.FirstOrDefault()?.Message ?? "Unknown error");
                return null;
            }

            var raw = result.Value;

            // Direct blit: BGRA pixels -> Avalonia WriteableBitmap (no PNG encode/decode)
            var writeableBitmap = new global::Avalonia.Media.Imaging.WriteableBitmap(
                new global::Avalonia.PixelSize(raw.Width, raw.Height),
                new global::Avalonia.Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Bgra8888,
                global::Avalonia.Platform.AlphaFormat.Premul);

            using (var fb = writeableBitmap.Lock())
            {
                System.Runtime.InteropServices.Marshal.Copy(raw.Pixels, 0, fb.Address, raw.Pixels.Length);
            }

            _logger.LogDebug("Successfully rendered page {PageNumber} to bitmap ({Width}x{Height})",
                pageNumber, raw.Width, raw.Height);

            return writeableBitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during page {PageNumber} rendering", pageNumber);
            return null;
        }
    }

    /// <summary>
    /// Renders a PDF page thumbnail at low DPI for the sidebar.
    /// </summary>
    private async Task<object?> RenderThumbnailAsync(PdfDocument document, int pageNumber)
    {
        try
        {
            var renderingService = App.GetService<IPdfRenderingService>();
            var result = await renderingService.RenderPageToRawAsync(document, pageNumber, 1.0, 36);

            if (!result.IsSuccess)
            {
                return null;
            }

            var raw = result.Value;

            var writeableBitmap = new global::Avalonia.Media.Imaging.WriteableBitmap(
                new global::Avalonia.PixelSize(raw.Width, raw.Height),
                new global::Avalonia.Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Bgra8888,
                global::Avalonia.Platform.AlphaFormat.Premul);

            using (var fb = writeableBitmap.Lock())
            {
                System.Runtime.InteropServices.Marshal.Copy(raw.Pixels, 0, fb.Address, raw.Pixels.Length);
            }

            return writeableBitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during thumbnail rendering for page {PageNumber}", pageNumber);
            return null;
        }
    }

    /// <summary>
    /// Handles ViewModel property changes to update UI.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageNumber))
        {
            // Clear selection when page changes
            ClearSelectionRectangle();
        }
    }

    /// <summary>
    /// Handles keyboard events for annotation shortcuts (H/U/S keys).
    /// Placeholder for future annotation integration.
    /// </summary>
    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.H when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _logger.LogInformation("H key pressed - Highlight tool shortcut (not yet implemented)");
                e.Handled = true;
                break;

            case Key.U when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _logger.LogInformation("U key pressed - Underline tool shortcut (not yet implemented)");
                e.Handled = true;
                break;

            case Key.S when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _logger.LogInformation("S key pressed - Strikethrough tool shortcut (not yet implemented)");
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles pointer pressed event to begin text selection.
    /// </summary>
    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || PdfImage == null)
        {
            return;
        }

        var properties = e.GetCurrentPoint(PdfImage).Properties;

        // Only start selection on left-click
        if (properties.IsLeftButtonPressed)
        {
            var point = e.GetCurrentPoint(PdfImage).Position;
            _selectionStartPoint = point;
            _isSelecting = true;

            // Begin text selection in ViewModel (if commands exist)
            // Note: These commands need to be added to Core ViewModel
            // For now, track selection state locally

            // Show selection rectangle
            ShowSelectionRectangle(point, point);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved event to update text selection.
    /// </summary>
    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting || PdfImage == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(PdfImage).Position;

        // Update selection rectangle
        ShowSelectionRectangle(_selectionStartPoint, point);

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer released event to end text selection.
    /// </summary>
    private void OnImagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSelecting || _viewModel == null || PdfImage == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(PdfImage).Position;
        _isSelecting = false;

        // Calculate selection bounds
        var x = Math.Min(_selectionStartPoint.X, point.X);
        var y = Math.Min(_selectionStartPoint.Y, point.Y);
        var width = Math.Abs(point.X - _selectionStartPoint.X);
        var height = Math.Abs(point.Y - _selectionStartPoint.Y);

        if (width > 5 && height > 5) // Minimum selection size
        {
            _logger.LogInformation(
                "Text selection: ({X}, {Y}) - ({Width}x{Height})",
                x, y, width, height);

            // Extract text from selection area
            _ = ExtractTextFromSelectionAsync(x, y, width, height);
        }
        else
        {
            // Click without drag - clear selection
            ClearSelectionRectangle();
            _viewModel.SelectedText = string.Empty;
            _viewModel.HasSelectedText = false;
        }

        e.Handled = true;
    }

    /// <summary>
    /// Shows the selection rectangle with the given bounds.
    /// </summary>
    private void ShowSelectionRectangle(Point start, Point end)
    {
        if (SelectionRectangle == null)
        {
            return;
        }

        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        // Update selection rectangle visual
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
        Canvas.SetLeft(SelectionRectangle, x);
        Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.IsVisible = true;
    }

    /// <summary>
    /// Clears the visual selection rectangle.
    /// </summary>
    private void ClearSelectionRectangle()
    {
        if (SelectionRectangle == null)
        {
            return;
        }

        SelectionRectangle.IsVisible = false;
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
    }

    /// <summary>
    /// Handles mouse wheel events for Ctrl+scroll zoom.
    /// </summary>
    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        var ctrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Zoom ONLY with Ctrl+Wheel (as expected by user)
        if (ctrlPressed)
        {
            var delta = e.Delta.Y;
            if (delta > 0)
            {
                // Zoom in
                if (_viewModel.Zoom.ZoomInCommand.CanExecute(null))
                {
                    _ = _viewModel.Zoom.ZoomInCommand.ExecuteAsync(null);
                }
            }
            else if (delta < 0)
            {
                // Zoom out
                if (_viewModel.Zoom.ZoomOutCommand.CanExecute(null))
                {
                    _ = _viewModel.Zoom.ZoomOutCommand.ExecuteAsync(null);
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer pressed for middle button panning.
    /// </summary>
    private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (PdfScrollViewer == null)
            return;

        var point = e.GetCurrentPoint(PdfScrollViewer);

        // Start panning on middle button press
        if (point.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = point.Position;
            _panStartOffset = new Vector(PdfScrollViewer.Offset.X, PdfScrollViewer.Offset.Y);
            e.Pointer.Capture(PdfScrollViewer);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved for middle button panning.
    /// </summary>
    private void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || PdfScrollViewer == null)
            return;

        var currentPoint = e.GetPosition(PdfScrollViewer);
        var delta = _panStartPoint - currentPoint;

        // Pan the scroll viewer
        PdfScrollViewer.Offset = new Vector(
            _panStartOffset.X + delta.X,
            _panStartOffset.Y + delta.Y);

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer released to stop middle button panning.
    /// </summary>
    private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Converts screen pixel coordinates to PDF page coordinates and extracts text.
    /// </summary>
    private async Task ExtractTextFromSelectionAsync(double screenX, double screenY, double screenWidth, double screenHeight)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return;

        try
        {
            var textService = App.GetService<ITextExtractionService>();

            // Convert screen coords to PDF coords:
            // The Image control displays the bitmap with Stretch="Uniform", so we need
            // to account for the actual rendered size vs the bitmap size.
            var imageSource = PdfImage.Source as global::Avalonia.Media.Imaging.Bitmap;
            if (imageSource == null)
                return;

            var bitmapWidth = (double)imageSource.PixelSize.Width;
            var bitmapHeight = (double)imageSource.PixelSize.Height;
            var renderWidth = PdfImage.Bounds.Width;
            var renderHeight = PdfImage.Bounds.Height;

            if (renderWidth <= 0 || renderHeight <= 0 || bitmapWidth <= 0 || bitmapHeight <= 0)
                return;

            // Calculate the actual scale and offset (Uniform stretch centers the image)
            var scaleX = renderWidth / bitmapWidth;
            var scaleY = renderHeight / bitmapHeight;
            var scale = Math.Min(scaleX, scaleY);

            var offsetX = (renderWidth - bitmapWidth * scale) / 2.0;
            var offsetY = (renderHeight - bitmapHeight * scale) / 2.0;

            // Convert screen coords to bitmap pixel coords
            var bmpX = (screenX - offsetX) / scale;
            var bmpY = (screenY - offsetY) / scale;
            var bmpW = screenWidth / scale;
            var bmpH = screenHeight / scale;

            // The bitmap was rendered at (zoomLevel * dpi / 72) scale from PDF points.
            // PDF page size in points -> bitmap pixels = pagePoints * zoomLevel * dpi / 72
            var dpi = _viewModel.CurrentDisplayInfo?.EffectiveDpi ?? 96.0;
            var zoom = _viewModel.ZoomLevel;
            var pdfScale = zoom * dpi / 72.0;

            // Convert bitmap coords to PDF page coords (points, Y-up)
            var pdfX = (float)(bmpX / pdfScale);
            var pdfW = (float)(bmpW / pdfScale);
            var pdfH = (float)(bmpH / pdfScale);

            // PDF coordinate system has Y increasing upward from bottom
            // Get page height to flip Y
            var renderingService = App.GetService<IPdfRenderingService>();
            var pageSizeResult = renderingService.GetPageSize(_viewModel.CurrentDocument, _viewModel.CurrentPageNumber);
            if (pageSizeResult.IsFailed) return;
            var pageHeight = pageSizeResult.Value.Height;
            var pdfY = (float)(pageHeight - (bmpY / pdfScale));
            var pdfBottom = (float)(pageHeight - ((bmpY + bmpH) / pdfScale));

            var bounds = new System.Drawing.RectangleF(pdfX, pdfBottom, pdfW, pdfY - pdfBottom);

            _logger.LogDebug("Selection PDF bounds: ({X},{Y}) {W}x{H}, pageHeight={PH}",
                bounds.X, bounds.Y, bounds.Width, bounds.Height, pageHeight);

            var result = await textService.ExtractTextInBoundsAsync(
                _viewModel.CurrentDocument,
                _viewModel.CurrentPageNumber,
                bounds);

            if (result.IsSuccess && result.Value.HasText)
            {
                _viewModel.SelectedText = result.Value.Text;
                _viewModel.HasSelectedText = true;
                _logger.LogInformation("Selected text: \"{Text}\"",
                    result.Value.Text.Length > 100 ? result.Value.Text[..100] + "..." : result.Value.Text);
            }
            else
            {
                _viewModel.SelectedText = string.Empty;
                _viewModel.HasSelectedText = false;
                _logger.LogDebug("No text found in selection area");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from selection");
        }
    }

    /// <summary>
    /// Handles Ctrl+C to copy selected text to clipboard.
    /// </summary>
    private void OnPageKeyDown_CopyText(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (_viewModel?.HasSelectedText == true && !string.IsNullOrEmpty(_viewModel.SelectedText))
            {
                _ = CopyToClipboardAsync(_viewModel.SelectedText);
                e.Handled = true;
            }
        }
    }

    private async Task CopyToClipboardAsync(string text)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(text);
                _logger.LogInformation("Copied {Length} characters to clipboard", text.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
        }
    }
}
