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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentPDF.Rendering.Interop;

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

    // Drawing state
    private bool _isDrawing;
    private Point _drawStartPoint;
    private System.Collections.Generic.List<Point> _drawingPoints = new();
    private global::Avalonia.Controls.Control? _drawingPreview;

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

        // Wire up drawing canvas events
        if (DrawingCanvas != null)
        {
            DrawingCanvas.PointerPressed += OnDrawingCanvasPointerPressed;
            DrawingCanvas.PointerMoved += OnDrawingCanvasPointerMoved;
            DrawingCanvas.PointerReleased += OnDrawingCanvasPointerReleased;
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
    /// Creates text markup annotations when text is selected.
    /// </summary>
    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel?.HasSelectedText != true || _viewModel.LastTextSelection == null)
            return;

        AnnotationType? annotationType = e.Key switch
        {
            Key.H when !e.KeyModifiers.HasFlag(KeyModifiers.Control) => AnnotationType.Highlight,
            Key.U when !e.KeyModifiers.HasFlag(KeyModifiers.Control) => AnnotationType.Underline,
            Key.S when !e.KeyModifiers.HasFlag(KeyModifiers.Control) => AnnotationType.StrikeOut,
            _ => null
        };

        if (annotationType.HasValue)
        {
            _ = CreateTextMarkupAnnotationAsync(annotationType.Value);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Creates a text markup annotation (highlight, underline, strikethrough) from the current selection.
    /// </summary>
    private async Task CreateTextMarkupAnnotationAsync(AnnotationType type)
    {
        if (_viewModel?.CurrentDocument == null || _viewModel.LastTextSelection == null)
            return;

        try
        {
            var annotationService = App.GetService<IAnnotationService>();
            var selection = _viewModel.LastTextSelection;

            var annotation = new Annotation
            {
                Type = type,
                PageNumber = _viewModel.CurrentPageNumber - 1,
                Bounds = new PdfRectangle(
                    selection.SelectionBounds.Left,
                    selection.SelectionBounds.Top,
                    selection.SelectionBounds.Right,
                    selection.SelectionBounds.Bottom),
                QuadPoints = selection.ToQuadPoints(),
                FillColor = type == AnnotationType.Highlight
                    ? System.Drawing.Color.FromArgb(128, 255, 255, 0)
                    : System.Drawing.Color.FromArgb(255, 255, 0, 0),
                Opacity = type == AnnotationType.Highlight ? 0.5 : 1.0
            };

            var result = await annotationService.CreateAnnotationAsync(
                _viewModel.CurrentDocument, annotation);

            if (result.IsSuccess)
            {
                _logger.LogInformation("{Type} annotation created on page {Page}",
                    type, _viewModel.CurrentPageNumber);
                await _viewModel.RefreshCurrentPageAsync();
            }
            else
            {
                _logger.LogError("Failed to create {Type} annotation: {Error}",
                    type, result.Errors.FirstOrDefault()?.Message ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {Type} annotation", type);
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
    /// Handles pointer moved event to update text selection and link cursor.
    /// </summary>
    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;

        if (_isSelecting)
        {
            ShowSelectionRectangle(_selectionStartPoint, point);
            e.Handled = true;
            return;
        }

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
            // Click without drag - check for link, then clear selection
            _ = HandleLinkClickAsync(point);
            ClearSelectionRectangle();
            _viewModel.SelectedText = string.Empty;
            _viewModel.HasSelectedText = false;
            _viewModel.LastTextSelection = null;
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
                _viewModel.LastTextSelection = result.Value;
                _logger.LogInformation("Selected text: \"{Text}\"",
                    result.Value.Text.Length > 100 ? result.Value.Text[..100] + "..." : result.Value.Text);
            }
            else
            {
                _viewModel.SelectedText = string.Empty;
                _viewModel.HasSelectedText = false;
                _viewModel.LastTextSelection = null;
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

    #region Link Detection

    /// <summary>
    /// Converts a screen point on PdfImage to PDF page coordinates.
    /// Returns null if conversion is not possible.
    /// </summary>
    private (float pdfX, float pdfY)? ScreenPointToPdfCoords(Point screenPoint)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return null;

        var imageSource = PdfImage.Source as global::Avalonia.Media.Imaging.Bitmap;
        if (imageSource == null)
            return null;

        var bitmapWidth = (double)imageSource.PixelSize.Width;
        var bitmapHeight = (double)imageSource.PixelSize.Height;
        var renderWidth = PdfImage.Bounds.Width;
        var renderHeight = PdfImage.Bounds.Height;

        if (renderWidth <= 0 || renderHeight <= 0 || bitmapWidth <= 0 || bitmapHeight <= 0)
            return null;

        var scaleX = renderWidth / bitmapWidth;
        var scaleY = renderHeight / bitmapHeight;
        var scale = Math.Min(scaleX, scaleY);

        var offsetX = (renderWidth - bitmapWidth * scale) / 2.0;
        var offsetY = (renderHeight - bitmapHeight * scale) / 2.0;

        var bmpX = (screenPoint.X - offsetX) / scale;
        var bmpY = (screenPoint.Y - offsetY) / scale;

        var dpi = _viewModel.CurrentDisplayInfo?.EffectiveDpi ?? 96.0;
        var zoom = _viewModel.ZoomLevel;
        var pdfScale = zoom * dpi / 72.0;

        var pdfX = (float)(bmpX / pdfScale);

        var renderingService = App.GetService<IPdfRenderingService>();
        var pageSizeResult = renderingService.GetPageSize(
            _viewModel.CurrentDocument, _viewModel.CurrentPageNumber);
        if (pageSizeResult.IsFailed) return null;

        var pageHeight = pageSizeResult.Value.Height;
        var pdfY = (float)(pageHeight - (bmpY / pdfScale));

        return ((float)pdfX, (float)pdfY);
    }

    /// <summary>
    /// Checks if a link exists at the clicked point and opens it in the default browser.
    /// </summary>
    private async Task HandleLinkClickAsync(Point screenPoint)
    {
        if (_viewModel?.CurrentDocument == null)
            return;

        try
        {
            var pdfCoords = ScreenPointToPdfCoords(screenPoint);
            if (pdfCoords == null)
                return;

            var uri = await Task.Run(() =>
            {
                var docHandle = (SafePdfDocumentHandle)_viewModel.CurrentDocument.Handle;
                using var pageHandle = PdfiumInterop.LoadPage(
                    docHandle, _viewModel.CurrentPageNumber - 1);
                if (pageHandle.IsInvalid) return null;

                var link = PdfiumInterop.GetLinkAtPoint(
                    pageHandle, pdfCoords.Value.pdfX, pdfCoords.Value.pdfY);
                if (link == IntPtr.Zero) return null;

                return PdfiumInterop.GetLinkUri(docHandle, link);
            });

            if (!string.IsNullOrEmpty(uri))
            {
                _logger.LogInformation("Opening link: {Uri}", uri);
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle link click");
        }
    }

    /// <summary>
    /// Updates the cursor to a hand when hovering over a link.
    /// </summary>
    private async Task UpdateLinkCursorAsync(Point screenPoint)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return;

        try
        {
            var pdfCoords = ScreenPointToPdfCoords(screenPoint);
            if (pdfCoords == null)
                return;

            var hasLink = await Task.Run(() =>
            {
                var docHandle = (SafePdfDocumentHandle)_viewModel.CurrentDocument.Handle;
                using var pageHandle = PdfiumInterop.LoadPage(
                    docHandle, _viewModel.CurrentPageNumber - 1);
                if (pageHandle.IsInvalid) return false;

                var link = PdfiumInterop.GetLinkAtPoint(
                    pageHandle, pdfCoords.Value.pdfX, pdfCoords.Value.pdfY);
                return link != IntPtr.Zero;
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PdfImage.Cursor = hasLink
                    ? new Cursor(StandardCursorType.Hand)
                    : new Cursor(StandardCursorType.Arrow);
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check link at cursor position");
        }
    }

    #endregion

    #region Drawing Canvas Handlers

    private void OnDrawingCanvasPointerPressed(
        object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || DrawingCanvas == null || PdfImage == null)
            return;
        if (_viewModel.ActiveDrawingTool == DrawingTool.None)
            return;

        var properties = e.GetCurrentPoint(DrawingCanvas).Properties;
        if (!properties.IsLeftButtonPressed)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        _drawStartPoint = point;
        _isDrawing = true;
        _drawingPoints.Clear();
        _drawingPoints.Add(point);

        var tool = _viewModel.ActiveDrawingTool;
        var strokeBrush = ParseBrush(_viewModel.DrawingStrokeColor);
        var strokeWidth = _viewModel.DrawingStrokeWidth;

        switch (tool)
        {
            case DrawingTool.Rectangle:
                var rect = new Rectangle
                {
                    Stroke = strokeBrush,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    StrokeThickness = strokeWidth,
                    Width = 0, Height = 0
                };
                Canvas.SetLeft(rect, point.X);
                Canvas.SetTop(rect, point.Y);
                DrawingCanvas.Children.Add(rect);
                _drawingPreview = rect;
                break;

            case DrawingTool.Circle:
                var ellipse = new Ellipse
                {
                    Stroke = strokeBrush,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    StrokeThickness = strokeWidth,
                    Width = 0, Height = 0
                };
                Canvas.SetLeft(ellipse, point.X);
                Canvas.SetTop(ellipse, point.Y);
                DrawingCanvas.Children.Add(ellipse);
                _drawingPreview = ellipse;
                break;

            case DrawingTool.Line:
                var line = new Line
                {
                    Stroke = strokeBrush,
                    StrokeThickness = strokeWidth,
                    StartPoint = point,
                    EndPoint = point
                };
                DrawingCanvas.Children.Add(line);
                _drawingPreview = line;
                break;

            case DrawingTool.Freehand:
                var polyline = new Polyline
                {
                    Stroke = strokeBrush,
                    StrokeThickness = strokeWidth,
                    Points = new global::Avalonia.Collections.AvaloniaList<Point> { point }
                };
                DrawingCanvas.Children.Add(polyline);
                _drawingPreview = polyline;
                break;

            case DrawingTool.Text:
                _drawingPreview = null;
                break;
        }

        e.Handled = true;
    }

    private void OnDrawingCanvasPointerMoved(
        object? sender, PointerEventArgs e)
    {
        if (!_isDrawing || _viewModel == null || PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        var tool = _viewModel.ActiveDrawingTool;

        switch (tool)
        {
            case DrawingTool.Rectangle when _drawingPreview is Rectangle rect:
                rect.Width = Math.Abs(point.X - _drawStartPoint.X);
                rect.Height = Math.Abs(point.Y - _drawStartPoint.Y);
                Canvas.SetLeft(rect, Math.Min(_drawStartPoint.X, point.X));
                Canvas.SetTop(rect, Math.Min(_drawStartPoint.Y, point.Y));
                break;

            case DrawingTool.Circle when _drawingPreview is Ellipse ellipse:
                ellipse.Width = Math.Abs(point.X - _drawStartPoint.X);
                ellipse.Height = Math.Abs(point.Y - _drawStartPoint.Y);
                Canvas.SetLeft(ellipse, Math.Min(_drawStartPoint.X, point.X));
                Canvas.SetTop(ellipse, Math.Min(_drawStartPoint.Y, point.Y));
                break;

            case DrawingTool.Line when _drawingPreview is Line line:
                line.EndPoint = point;
                break;

            case DrawingTool.Freehand when _drawingPreview is Polyline pl:
                _drawingPoints.Add(point);
                ((global::Avalonia.Collections.AvaloniaList<Point>)pl.Points).Add(point);
                break;
        }

        e.Handled = true;
    }

    private void OnDrawingCanvasPointerReleased(
        object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDrawing || _viewModel == null || PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        _isDrawing = false;

        if (_drawingPreview != null && DrawingCanvas != null)
        {
            DrawingCanvas.Children.Remove(_drawingPreview);
            _drawingPreview = null;
        }

        _ = CommitDrawingAsync(point);
        e.Handled = true;
    }

    private async Task CommitDrawingAsync(Point endPoint)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return;

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var tool = _viewModel.ActiveDrawingTool;
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageNumber = _viewModel.CurrentPageNumber;
            var strokeColor = _viewModel.DrawingStrokeColor;
            var fillColor = _viewModel.DrawingFillColor;
            var strokeWidth = _viewModel.DrawingStrokeWidth;

            var (pdfStart, pdfEnd) = ConvertScreenToPdfPoint(
                _drawStartPoint, endPoint);
            if (pdfStart == null || pdfEnd == null)
                return;

            bool success = false;

            switch (tool)
            {
                case DrawingTool.Rectangle:
                    var rx = Math.Min(pdfStart.Value.X, pdfEnd.Value.X);
                    var ry = Math.Min(pdfStart.Value.Y, pdfEnd.Value.Y);
                    var rw = Math.Abs(pdfEnd.Value.X - pdfStart.Value.X);
                    var rh = Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y);
                    if (rw > 1 && rh > 1)
                        success = await shapeService.AddRectangleAsync(
                            docId, pageNumber, rx, ry, rw, rh,
                            fillColor, strokeColor, strokeWidth);
                    break;

                case DrawingTool.Circle:
                    var cx = (pdfStart.Value.X + pdfEnd.Value.X) / 2;
                    var cy = (pdfStart.Value.Y + pdfEnd.Value.Y) / 2;
                    var radX = Math.Abs(pdfEnd.Value.X - pdfStart.Value.X) / 2;
                    var radY = Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y) / 2;
                    var radius = Math.Max(radX, radY);
                    if (radius > 1)
                        success = await shapeService.AddCircleAsync(
                            docId, pageNumber, cx, cy, radius,
                            fillColor, strokeColor, strokeWidth);
                    break;

                case DrawingTool.Line:
                    success = await shapeService.AddLineAsync(
                        docId, pageNumber,
                        pdfStart.Value.X, pdfStart.Value.Y,
                        pdfEnd.Value.X, pdfEnd.Value.Y,
                        strokeColor, strokeWidth);
                    break;

                case DrawingTool.Freehand:
                    if (_drawingPoints.Count >= 2)
                    {
                        var pdfPts = ConvertPointsToPdfArray(_drawingPoints);
                        if (pdfPts != null && pdfPts.Length >= 4)
                            success = await shapeService.AddFreehandPathAsync(
                                docId, pageNumber, pdfPts,
                                strokeColor, strokeWidth);
                    }
                    break;

                case DrawingTool.Text:
                    success = await shapeService.AddTextAsync(
                        docId, pageNumber,
                        pdfStart.Value.X, pdfStart.Value.Y,
                        "Text", 12f, "Helvetica", strokeColor);
                    break;
            }

            if (success)
            {
                _logger.LogInformation("Shape {Tool} drawn on page {Page}",
                    tool, pageNumber);
                await _viewModel.RefreshCurrentPageAsync();
            }

            _viewModel.ActiveDrawingTool = DrawingTool.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit drawing shape");
        }
    }

    private (System.Drawing.PointF? start, System.Drawing.PointF? end)
        ConvertScreenToPdfPoint(Point screenStart, Point screenEnd)
    {
        var s = ScreenPointToPdfCoords(screenStart);
        var en = ScreenPointToPdfCoords(screenEnd);
        if (s == null || en == null) return (null, null);
        return (
            new System.Drawing.PointF(s.Value.pdfX, s.Value.pdfY),
            new System.Drawing.PointF(en.Value.pdfX, en.Value.pdfY));
    }

    private double[]? ConvertPointsToPdfArray(
        System.Collections.Generic.List<Point> pts)
    {
        if (pts.Count < 2) return null;

        var result = new double[pts.Count * 2];
        for (int i = 0; i < pts.Count; i++)
        {
            var coords = ScreenPointToPdfCoords(pts[i]);
            if (coords == null) return null;
            result[i * 2] = coords.Value.pdfX;
            result[i * 2 + 1] = coords.Value.pdfY;
        }
        return result;
    }

    private static SolidColorBrush ParseBrush(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length >= 6)
            {
                var r = System.Convert.ToByte(hex[..2], 16);
                var g = System.Convert.ToByte(hex[2..4], 16);
                var b = System.Convert.ToByte(hex[4..6], 16);
                byte a = hex.Length >= 8
                    ? System.Convert.ToByte(hex[6..8], 16) : (byte)255;
                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }
        catch { }
        return new SolidColorBrush(Colors.Red);
    }

    #endregion
}
