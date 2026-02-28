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
using FluentPDF.Avalonia.Helpers;

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

    // Selection state (Select tool)
    private PageObjectInfo? _selectedObject;
    private readonly System.Collections.Generic.List<PageObjectInfo> _selectedObjects = new();
    private readonly System.Collections.Generic.List<Rectangle> _multiSelectionHighlights = new();
    private Rectangle? _selectionHighlight;
    private bool _isDraggingSelection;
    private Point _dragStartPoint;
    private Point _originalDragStart; // Preserved across incremental updates
    private SelectionManager? _selectionManager;

    // Resize handle state
    private enum HandlePosition { TopLeft, TopRight, BottomLeft, BottomRight, MiddleLeft, MiddleRight, TopMiddle, BottomMiddle }
    private bool _isResizing;
    private HandlePosition _activeHandle;
    private Point _resizeStartPoint;
    private Rect _resizeOriginalRect; // Screen rect of selection at resize start

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

        // Wire up callbacks immediately
        if (_viewModel != null)
        {
            _viewModel.RenderPageCallback = RenderPageAsync;
            _viewModel.PageOperationCallback = ExecutePageOperationAsync;
            _viewModel.SaveDocumentCallback = SaveDocumentAsync;
            _viewModel.PreSaveAction = docId => App.GetService<IShapeService>().FlushDirtyPages(docId);
            if (_viewModel.Thumbnails != null)
            {
                _viewModel.Thumbnails.RenderThumbnailCallback = RenderThumbnailAsync;
                _viewModel.Thumbnails.PageOperationCallback = ExecutePageOperationAsync;
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
            vm.PageOperationCallback = ExecutePageOperationAsync;
            vm.SaveDocumentCallback = SaveDocumentAsync;
            vm.PreSaveAction = docId => App.GetService<IShapeService>().FlushDirtyPages(docId);
            if (vm.Thumbnails != null)
            {
                vm.Thumbnails.RenderThumbnailCallback = RenderThumbnailAsync;
                vm.Thumbnails.PageOperationCallback = ExecutePageOperationAsync;
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
            vm.PageOperationCallback = ExecutePageOperationAsync;
            if (vm.Thumbnails != null)
            {
                vm.Thumbnails.RenderThumbnailCallback = RenderThumbnailAsync;
                vm.Thumbnails.PageOperationCallback = ExecutePageOperationAsync;
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

            _selectionManager = new SelectionManager(
                DrawingCanvas,
                screenToPdf: pt => ScreenPointToPdfCoords(pt),
                pdfToScreen: (px, py) => PdfCoordsToScreen(px, py));
        }

        // Wire up drawing toolbar toggle buttons and color popups
        SetupDrawingToolbar();

        // Original object tracking is done lazily when Select tool is activated

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
            ClearSelectionRectangle();
            ClearSelection();
        }
        else if (e.PropertyName == nameof(PdfViewerViewModel.ZoomLevel))
        {
            // Re-draw selection highlight at new zoom
            if (_selectedObject != null)
                ShowSelectionHighlight(_selectedObject);
        }
    }

    private async Task TrackOriginalObjectCountAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;
        var pageIndex = _viewModel.CurrentPageNumber - 1;
        if (_viewModel.OriginalObjectCounts.ContainsKey(pageIndex)) return;

        var docId = _viewModel.CurrentDocument.FilePath;

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));
            if (_viewModel != null && !_viewModel.OriginalObjectCounts.ContainsKey(pageIndex))
            {
                _viewModel.OriginalObjectCounts[pageIndex] = objects.Count;
                _logger.LogDebug("Tracked {Count} original objects on page {Page}", objects.Count, pageIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track original object count");
        }
    }

    /// <summary>
    /// Handles keyboard events for annotation shortcuts (H/U/S keys).
    /// Creates text markup annotations when text is selected.
    /// </summary>
    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        // Escape closes drawing toolbar
        if (e.Key == Key.Escape && _viewModel?.IsDrawingToolbarVisible == true)
        {
            ClearSelection();
            _viewModel.CloseDrawingToolbarCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Ctrl+Z undo
        if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = UndoAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+Y redo
        if (e.Key == Key.Y && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = RedoAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+A select all objects on page
        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            _viewModel?.ActiveDrawingTool == DrawingTool.Select)
        {
            _ = SelectAllObjectsAsync();
            e.Handled = true;
            return;
        }

        // Delete selected shape(s)
        if ((e.Key == Key.Delete || e.Key == Key.Back) && (_selectedObject != null || _selectedObjects.Count > 0))
        {
            _ = DeleteSelectedObjectsAsync();
            e.Handled = true;
            return;
        }

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

            // Use ratio-based conversion: bitmap represents full page
            var renderingService = App.GetService<IPdfRenderingService>();
            var pageSizeResult = renderingService.GetPageSize(_viewModel.CurrentDocument, _viewModel.CurrentPageNumber);
            if (pageSizeResult.IsFailed) return;
            var pageWidth = pageSizeResult.Value.Width;
            var pageHeight = pageSizeResult.Value.Height;

            var pdfX = (float)(bmpX / bitmapWidth * pageWidth);
            var pdfW = (float)(bmpW / bitmapWidth * pageWidth);
            var pdfH = (float)(bmpH / bitmapHeight * pageHeight);

            // PDF coordinate system has Y increasing upward from bottom
            var pdfY = (float)(pageHeight - (bmpY / bitmapHeight * pageHeight));
            var pdfBottom = (float)(pageHeight - ((bmpY + bmpH) / bitmapHeight * pageHeight));

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

    #region Page Operations

    private async Task<bool> ExecutePageOperationAsync(
        PdfDocument document, int pageIndex, string operation)
    {
        try
        {
            return await Task.Run(() =>
            {
                var docHandle = (SafePdfDocumentHandle)document.Handle;
                if (docHandle.IsInvalid) return false;

                switch (operation)
                {
                    case "rotate_cw":
                    {
                        using var page = PdfiumInterop.LoadPage(docHandle, pageIndex);
                        if (page.IsInvalid) return false;
                        var current = PdfiumInterop.GetPageRotation(page);
                        PdfiumInterop.SetPageRotation(page, (current + 1) % 4);
                        return true;
                    }
                    case "rotate_ccw":
                    {
                        using var page = PdfiumInterop.LoadPage(docHandle, pageIndex);
                        if (page.IsInvalid) return false;
                        var current = PdfiumInterop.GetPageRotation(page);
                        PdfiumInterop.SetPageRotation(page, (current + 3) % 4);
                        return true;
                    }
                    case "delete":
                    {
                        PdfiumInterop.DeletePage(docHandle, pageIndex);
                        return true;
                    }
                    case "insert_blank":
                    {
                        // Get current page size for the new blank page
                        double width = 612, height = 792; // Letter size default
                        if (pageIndex > 0)
                        {
                            using var prevPage = PdfiumInterop.LoadPage(docHandle, pageIndex - 1);
                            if (!prevPage.IsInvalid)
                            {
                                width = PdfiumInterop.GetPageWidth(prevPage);
                                height = PdfiumInterop.GetPageHeight(prevPage);
                            }
                        }
                        using var newPage = PdfiumInterop.CreateNewPage(docHandle, pageIndex, width, height);
                        return !newPage.IsInvalid;
                    }
                    default:
                        return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Page operation {Operation} failed on page {Index}", operation, pageIndex);
            return false;
        }
    }

    private async Task<bool> SaveDocumentAsync(PdfDocument document, string? filePath)
    {
        try
        {
            string targetPath;
            if (string.IsNullOrEmpty(filePath))
            {
                // Save As - show file picker
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return false;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                    new global::Avalonia.Platform.Storage.FilePickerSaveOptions
                    {
                        Title = "Save PDF As",
                        DefaultExtension = "pdf",
                        FileTypeChoices = new[]
                        {
                            new global::Avalonia.Platform.Storage.FilePickerFileType("PDF Files")
                            {
                                Patterns = new[] { "*.pdf" }
                            }
                        },
                        SuggestedFileName = System.IO.Path.GetFileName(document.FilePath)
                    });

                if (file == null) return false;
                targetPath = file.Path.LocalPath;
            }
            else
            {
                targetPath = document.FilePath;
            }

            return await Task.Run(() =>
            {
                var docHandle = (SafePdfDocumentHandle)document.Handle;
                if (docHandle.IsInvalid) return false;

                // Check if we have content stream patches (move/resize/new shapes)
                // If so, use QPDF to patch the original file instead of PDFium's GenerateContent
                var shapeService = App.GetService<IShapeService>();
                if (shapeService is FluentPDF.Rendering.Services.ShapeService ss && ss.Patcher.HasPendingChanges)
                {
                    _logger.LogInformation("Using QPDF content stream patching to preserve CIDFont text");
                    var tempPath = targetPath + ".tmp";
                    var pdfiumSaved = PdfiumInterop.SaveDocument(docHandle, tempPath);
                    if (!pdfiumSaved)
                    {
                        _logger.LogError("PDFium save to temp file failed");
                        return false;
                    }

                    var patched = ss.Patcher.SaveWithPatches(document.FilePath, targetPath);
                    try { System.IO.File.Delete(tempPath); } catch { }

                    if (patched)
                    {
                        ss.Patcher.Clear();
                        // Evict cached page handles — page will be re-rendered from saved file
                        ss.PageCache.EvictAll(docHandle);
                        _logger.LogInformation("Saved with QPDF patches to {Path}", targetPath);
                    }
                    else
                    {
                        _logger.LogWarning("QPDF patching failed, falling back to PDFium save");
                        try
                        {
                            if (System.IO.File.Exists(targetPath)) System.IO.File.Delete(targetPath);
                            System.IO.File.Move(tempPath, targetPath);
                        }
                        catch (Exception ex2)
                        {
                            _logger.LogError(ex2, "Fallback save also failed");
                            return false;
                        }
                        ss.PageCache.EvictAll(docHandle);
                    }
                    return true;
                }

                var success = PdfiumInterop.SaveDocument(docHandle, targetPath);
                if (success)
                    _logger.LogInformation("Document saved to {Path}", targetPath);
                else
                    _logger.LogError("Failed to save document to {Path}", targetPath);
                return success;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed");
            return false;
        }
    }

    #endregion

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

        // Use ratio-based conversion: bitmap represents the full page, so
        // pdfX = (bmpX / bitmapWidth) * pageWidth. This is independent of DPI/zoom.
        var renderingService = App.GetService<IPdfRenderingService>();
        var pageSizeResult = renderingService.GetPageSize(
            _viewModel.CurrentDocument, _viewModel.CurrentPageNumber);
        if (pageSizeResult.IsFailed) return null;

        var pageWidth = pageSizeResult.Value.Width;
        var pageHeight = pageSizeResult.Value.Height;

        var pdfX = (float)(bmpX / bitmapWidth * pageWidth);
        // PDF Y-axis is bottom-up, bitmap Y-axis is top-down
        var pdfY = (float)(pageHeight - (bmpY / bitmapHeight * pageHeight));

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

    #region Drawing Toolbar Setup

    private void SetupDrawingToolbar()
    {
        var toolButtons = new (string Name, DrawingTool Tool)[]
        {
            ("DrawToolPan", DrawingTool.None),
            ("DrawToolSelect", DrawingTool.Select),
            ("DrawToolLasso", DrawingTool.Lasso),
            ("DrawToolRectangle", DrawingTool.Rectangle),
            ("DrawToolCircle", DrawingTool.Circle),
            ("DrawToolLine", DrawingTool.Line),
            ("DrawToolFreehand", DrawingTool.Freehand),
            ("DrawToolText", DrawingTool.Text),
        };

        foreach (var (name, tool) in toolButtons)
        {
            var toggle = this.FindControl<global::Avalonia.Controls.Primitives.ToggleButton>(name);
            if (toggle == null) continue;

            var capturedTool = tool;
            toggle.Click += (s, e) =>
            {
                if (_viewModel == null) return;

                if (capturedTool == DrawingTool.None)
                {
                    _viewModel.ActiveDrawingTool = DrawingTool.None;
                }
                else
                {
                    _viewModel.SetDrawingToolCommand.Execute(capturedTool.ToString());
                }
                UpdateDrawingToolToggleStates();
            };
        }

        // Containment mode toggle (intersect vs fully contained)
        var containmentToggle = this.FindControl<global::Avalonia.Controls.Primitives.ToggleButton>("ContainmentModeToggle");
        if (containmentToggle != null)
        {
            containmentToggle.Click += (s, e) =>
            {
                if (_selectionManager == null) return;
                _selectionManager.ContainmentMode = containmentToggle.IsChecked == true
                    ? SelectionContainment.FullyContained
                    : SelectionContainment.Intersect;
                if (_viewModel != null)
                    _viewModel.StatusMessage = _selectionManager.ContainmentMode == SelectionContainment.FullyContained
                        ? "Selection: Fully Contained" : "Selection: Intersect";
            };
        }

        // Stroke color popup
        var strokeBtn = this.FindControl<Button>("StrokeColorButton");
        var strokePopup = this.FindControl<global::Avalonia.Controls.Primitives.Popup>("StrokeColorPopup");
        if (strokeBtn != null && strokePopup != null)
        {
            strokeBtn.Click += (s, e) => strokePopup.IsOpen = !strokePopup.IsOpen;
        }

        // Wire stroke color swatch clicks
        WireColorSwatches("ColorSwatch", color =>
        {
            if (_viewModel != null) _viewModel.DrawingStrokeColor = color;
            if (strokePopup != null) strokePopup.IsOpen = false;
        });

        // Fill color popup
        var fillBtn = this.FindControl<Button>("FillColorButton");
        var fillPopup = this.FindControl<global::Avalonia.Controls.Primitives.Popup>("FillColorPopup");
        if (fillBtn != null && fillPopup != null)
        {
            fillBtn.Click += (s, e) => fillPopup.IsOpen = !fillPopup.IsOpen;
        }

        WireColorSwatches("FillColorSwatch", color =>
        {
            if (_viewModel != null) _viewModel.DrawingFillColor = color;
            if (fillPopup != null) fillPopup.IsOpen = false;
        });

        // Stroke width combo
        var widthCombo = this.FindControl<ComboBox>("StrokeWidthCombo");
        if (widthCombo != null)
        {
            widthCombo.SelectionChanged += (s, e) =>
            {
                if (_viewModel == null) return;
                if (widthCombo.SelectedItem is ComboBoxItem item && item.Tag is string tagStr
                    && float.TryParse(tagStr, out var w))
                {
                    _viewModel.DrawingStrokeWidth = w;
                }
            };
        }

        // Subscribe to ActiveDrawingTool changes to sync toggle states
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PdfViewerViewModel.ActiveDrawingTool))
                {
                    Dispatcher.UIThread.Post(UpdateDrawingToolToggleStates);
                    // Lazily track original object count when any drawing tool is first activated
                    if (_viewModel?.ActiveDrawingTool != DrawingTool.None)
                        _ = TrackOriginalObjectCountAsync();
                }
            };
        }
    }

    private void WireColorSwatches(string className, Action<string> onColorSelected)
    {
        // Find buttons by walking the visual tree from the popups
        var popups = new[] { "StrokeColorPopup", "FillColorPopup" };
        foreach (var popupName in popups)
        {
            var popup = this.FindControl<global::Avalonia.Controls.Primitives.Popup>(popupName);
            if (popup?.Child is not Border border) continue;
            if (border.Child is not WrapPanel wrap) continue;

            foreach (var child in wrap.Children)
            {
                if (child is Button btn && btn.Classes.Contains(className) && btn.Tag is string color)
                {
                    var capturedColor = color;
                    btn.Click += (s, e) => onColorSelected(capturedColor);
                }
            }
        }
    }

    private void UpdateDrawingToolToggleStates()
    {
        var activeTool = _viewModel?.ActiveDrawingTool ?? DrawingTool.None;
        var mapping = new (string Name, DrawingTool Tool)[]
        {
            ("DrawToolPan", DrawingTool.None),
            ("DrawToolSelect", DrawingTool.Select),
            ("DrawToolLasso", DrawingTool.Lasso),
            ("DrawToolRectangle", DrawingTool.Rectangle),
            ("DrawToolCircle", DrawingTool.Circle),
            ("DrawToolLine", DrawingTool.Line),
            ("DrawToolFreehand", DrawingTool.Freehand),
            ("DrawToolText", DrawingTool.Text),
        };

        foreach (var (name, tool) in mapping)
        {
            var toggle = this.FindControl<global::Avalonia.Controls.Primitives.ToggleButton>(name);
            if (toggle != null)
                toggle.IsChecked = activeTool == tool;
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
        var point = e.GetCurrentPoint(PdfImage).Position;

        // Right-click context menu for Select tool
        if (properties.IsRightButtonPressed && _viewModel.ActiveDrawingTool == DrawingTool.Select && _selectedObject != null)
        {
            ShowShapeContextMenu(point);
            e.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed)
            return;

        // Handle Lasso tool
        if (_viewModel.ActiveDrawingTool == DrawingTool.Lasso && _selectionManager != null)
        {
            var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            var modifier = SelectionManager.GetModifier(shiftHeld, altHeld);
            _selectionManager.StartLasso(point, modifier);
            e.Handled = true;
            return;
        }

        // Handle Select tool (left click)
        if (_viewModel.ActiveDrawingTool == DrawingTool.Select)
        {
            _isDraggingSelection = false;
            var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            _ = HandleSelectClickAsync(point, shiftHeld, altHeld);
            e.Handled = true;
            return;
        }

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
        // Handle lasso tool drag
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Lasso && _selectionManager?.IsLassoActive == true && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                _selectionManager.UpdateLasso(pt);
                e.Handled = true;
                return;
            }
        }

        // Handle marquee selection drag (Select tool, started on empty space)
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Select && _selectionManager?.IsMarqueeActive == true && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                _selectionManager.UpdateMarquee(pt);
                e.Handled = true;
                return;
            }
        }

        // Handle multi-object move (Select tool, dragging selected objects)
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Select && _selectionManager?.IsMultiMoving == true && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                _selectionManager.UpdateMultiMove(pt);
                e.Handled = true;
                return;
            }
        }

        // Handle resize drag
        if (_isResizing && _selectionHighlight != null && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                var newRect = ComputeResizedRect(pt, shiftHeld, altHeld);

                // Update visual preview
                Canvas.SetLeft(_selectionHighlight, newRect.X);
                Canvas.SetTop(_selectionHighlight, newRect.Y);
                _selectionHighlight.Width = newRect.Width;
                _selectionHighlight.Height = newRect.Height;

                // Update handle positions to match new rect
                UpdateResizeHandlePositions(newRect.X, newRect.Y, newRect.Width, newRect.Height);
                e.Handled = true;
                return;
            }
        }

        // Handle single-object drag-to-move for Select tool (legacy single select path)
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Select && _selectedObject != null && _selectedObjects.Count <= 1 && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var dragPoint = e.GetCurrentPoint(PdfImage).Position;
                var dx = dragPoint.X - _dragStartPoint.X;
                var dy = dragPoint.Y - _dragStartPoint.Y;
                if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
                    _isDraggingSelection = true;

                if (_isDraggingSelection && _selectionHighlight != null)
                {
                    Canvas.SetLeft(_selectionHighlight, Canvas.GetLeft(_selectionHighlight) + dx);
                    Canvas.SetTop(_selectionHighlight, Canvas.GetTop(_selectionHighlight) + dy);
                    foreach (var (h, _) in _resizeHandles)
                    {
                        Canvas.SetLeft(h, Canvas.GetLeft(h) + dx);
                        Canvas.SetTop(h, Canvas.GetTop(h) + dy);
                    }
                    _dragStartPoint = dragPoint;
                }
                e.Handled = true;
                return;
            }
        }

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
        if (PdfImage == null) { _isDrawing = false; return; }

        var releasePoint = e.GetCurrentPoint(PdfImage).Position;

        // Finish lasso selection
        if (_selectionManager?.IsLassoActive == true && _viewModel != null)
        {
            _ = FinishLassoSelectionAsync();
            e.Handled = true;
            return;
        }

        // Finish marquee selection
        if (_selectionManager?.IsMarqueeActive == true && _viewModel != null)
        {
            _ = FinishMarqueeSelectionAsync(releasePoint);
            e.Handled = true;
            return;
        }

        // Finish multi-object move
        if (_selectionManager?.IsMultiMoving == true && _viewModel != null)
        {
            var delta = _selectionManager.FinishMultiMove(releasePoint);
            if (delta != null)
                _ = MoveSelectedObjectsAsync(delta.Value.deltaPdfX, delta.Value.deltaPdfY);
            e.Handled = true;
            return;
        }

        // Commit resize
        if (_isResizing && _selectedObject != null && _viewModel != null && _selectionHighlight != null)
        {
            _isResizing = false;
            var origW = _resizeOriginalRect.Width;
            var origH = _resizeOriginalRect.Height;
            var newW = _selectionHighlight.Width;
            var newH = _selectionHighlight.Height;

            if (origW > 0 && origH > 0 && (Math.Abs(newW - origW) > 1 || Math.Abs(newH - origH) > 1))
            {
                var scaleX = (float)(newW / origW);
                var scaleY = (float)(newH / origH);

                // Determine anchor in PDF coords (opposite corner of dragged handle)
                var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                float anchorPdfX, anchorPdfY;

                if (altHeld)
                {
                    // Alt: anchor at center
                    anchorPdfX = (_selectedObject.Left + _selectedObject.Right) / 2;
                    anchorPdfY = (_selectedObject.Bottom + _selectedObject.Top) / 2;
                }
                else
                {
                    // Anchor at opposite corner
                    (anchorPdfX, anchorPdfY) = _activeHandle switch
                    {
                        HandlePosition.TopLeft => (_selectedObject.Right, _selectedObject.Bottom),
                        HandlePosition.TopRight => (_selectedObject.Left, _selectedObject.Bottom),
                        HandlePosition.BottomLeft => (_selectedObject.Right, _selectedObject.Top),
                        HandlePosition.BottomRight => (_selectedObject.Left, _selectedObject.Top),
                        HandlePosition.MiddleLeft => (_selectedObject.Right, (_selectedObject.Bottom + _selectedObject.Top) / 2),
                        HandlePosition.MiddleRight => (_selectedObject.Left, (_selectedObject.Bottom + _selectedObject.Top) / 2),
                        HandlePosition.TopMiddle => ((_selectedObject.Left + _selectedObject.Right) / 2, _selectedObject.Bottom),
                        HandlePosition.BottomMiddle => ((_selectedObject.Left + _selectedObject.Right) / 2, _selectedObject.Top),
                        _ => (_selectedObject.Left, _selectedObject.Bottom)
                    };
                }

                _ = CommitResizeAsync(scaleX, scaleY, anchorPdfX, anchorPdfY);
            }
            e.Handled = true;
            return;
        }

        // Commit single-object drag-to-move for Select tool
        if (_isDraggingSelection && _selectedObject != null && _viewModel != null)
        {
            _isDraggingSelection = false;
            if (_selectedObject != null)
            {
                var currentTL = PdfCoordsToScreen(_selectedObject.Left, _selectedObject.Top);
                if (currentTL != null && _selectionHighlight != null)
                {
                    var actualX = Canvas.GetLeft(_selectionHighlight);
                    var actualY = Canvas.GetTop(_selectionHighlight);
                    var screenDx = (float)(actualX - currentTL.Value.X);
                    var screenDy = (float)(actualY - currentTL.Value.Y);

                    var pdfOrigin = ScreenPointToPdfCoords(new Point(0, 0));
                    var pdfDelta = ScreenPointToPdfCoords(new Point(screenDx, screenDy));
                    if (pdfOrigin != null && pdfDelta != null)
                    {
                        var deltaPdfX = pdfDelta.Value.pdfX - pdfOrigin.Value.pdfX;
                        var deltaPdfY = pdfDelta.Value.pdfY - pdfOrigin.Value.pdfY;
                        _ = MoveSelectedObjectAsync(deltaPdfX, deltaPdfY);
                    }
                }
            }
            e.Handled = true;
            return;
        }

        if (!_isDrawing || _viewModel == null || PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        _isDrawing = false;

        // Keep preview visible until re-render completes to avoid flicker
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
            var pageNumber = _viewModel.CurrentPageNumber - 1; // 0-based for PDFium
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
                _logger.LogInformation("Shape {Tool} committed on page {Page}", tool, pageNumber);

                // Record undo action
                var undoService = App.GetService<IUndoRedoService>();
                var creationData = new ShapeCreationData
                {
                    ShapeType = tool switch
                    {
                        DrawingTool.Rectangle => DrawingShapeType.Rectangle,
                        DrawingTool.Circle => DrawingShapeType.Circle,
                        DrawingTool.Line => DrawingShapeType.Line,
                        DrawingTool.Freehand => DrawingShapeType.Freehand,
                        DrawingTool.Text => DrawingShapeType.Text,
                        _ => DrawingShapeType.Rectangle
                    },
                    X = pdfStart.Value.X,
                    Y = pdfStart.Value.Y,
                    X2 = pdfEnd.Value.X,
                    Y2 = pdfEnd.Value.Y,
                    Width = Math.Abs(pdfEnd.Value.X - pdfStart.Value.X),
                    Height = Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y),
                    Radius = Math.Max(Math.Abs(pdfEnd.Value.X - pdfStart.Value.X),
                                      Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y)) / 2,
                    FillColor = fillColor,
                    StrokeColor = strokeColor,
                    StrokeWidth = strokeWidth,
                    Points = tool == DrawingTool.Freehand ? ConvertPointsToPdfArray(_drawingPoints) : null,
                    Text = tool == DrawingTool.Text ? "Text" : null,
                };
                undoService.Push(new AddShapeAction(docId, pageNumber, creationData));

                // Silent refresh: re-render without showing loading overlay to avoid flicker
                await _viewModel.RefreshCurrentPageSilentAsync();
            }
            else
            {
                _logger.LogWarning("Shape {Tool} failed on page {Page} (docId={DocId})",
                    tool, pageNumber, docId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit drawing shape");
        }
        finally
        {
            // Remove drawing preview now that re-render is complete
            if (_drawingPreview != null && DrawingCanvas != null)
            {
                DrawingCanvas.Children.Remove(_drawingPreview);
                _drawingPreview = null;
            }
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

    #region Select Tool

    private bool IsOriginalObject(PageObjectInfo obj)
    {
        if (_viewModel == null) return false;
        var pageIndex = _viewModel.CurrentPageNumber - 1;
        if (_viewModel.OriginalObjectCounts.TryGetValue(pageIndex, out var originalCount))
            return obj.Index < originalCount;
        return true; // Assume original if not tracked
    }

    private async Task HandleSelectClickAsync(Point screenPoint, bool shiftHeld = false, bool altHeld = false)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return;

        var pdfCoords = ScreenPointToPdfCoords(screenPoint);
        if (pdfCoords == null)
        {
            ClearSelection();
            return;
        }

        var modifier = SelectionManager.GetModifier(shiftHeld, altHeld);

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageNumber = _viewModel.CurrentPageNumber - 1;

            var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));

            PageObjectInfo? hit = null;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var obj = objects[i];
                if (pdfCoords.Value.pdfX >= obj.Left && pdfCoords.Value.pdfX <= obj.Right &&
                    pdfCoords.Value.pdfY >= obj.Bottom && pdfCoords.Value.pdfY <= obj.Top)
                {
                    // Skip locked original objects entirely
                    if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(obj))
                        continue;
                    hit = obj;
                    break;
                }
            }

            if (hit != null)
            {
                // Check if clicked on already-selected object → prepare multi-move
                var alreadySelected = _selectedObjects.Any(o => o.Index == hit.Index);

                if (modifier == SelectionModifier.Add || modifier == SelectionModifier.Subtract)
                {
                    if (modifier == SelectionModifier.Subtract)
                    {
                        // Alt+click: remove from selection
                        _selectedObjects.RemoveAll(o => o.Index == hit.Index);
                    }
                    else
                    {
                        // Shift+click: toggle in selection
                        var existing = _selectedObjects.FindIndex(o => o.Index == hit.Index);
                        if (existing >= 0)
                            _selectedObjects.RemoveAt(existing);
                        else
                            _selectedObjects.Add(hit);
                    }
                    _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
                    _dragStartPoint = screenPoint;
                    _originalDragStart = screenPoint;
                    ShowMultiSelectionHighlights();
                    _logger.LogInformation("Multi-select: {Count} objects selected", _selectedObjects.Count);
                }
                else if (alreadySelected && _selectedObjects.Count > 1)
                {
                    // Click on already-selected object with multiple selected → prepare multi-move
                    _dragStartPoint = screenPoint;
                    _originalDragStart = screenPoint;
                    if (_selectionManager != null)
                    {
                        _selectionManager.StartMultiMove(screenPoint, _selectedObjects);
                        foreach (var h in _multiSelectionHighlights)
                            _selectionManager.RegisterMoveHighlight(h);
                    }
                }
                else
                {
                    // Plain click: single select
                    _selectedObjects.Clear();
                    _selectedObjects.Add(hit);
                    _selectedObject = hit;
                    _dragStartPoint = screenPoint;
                    _originalDragStart = screenPoint;
                    ShowSelectionHighlight(hit);
                    _logger.LogInformation("Selected object #{Index} type={Type}", hit.Index, hit.Type);
                }
            }
            else
            {
                // No hit - start marquee selection on empty space
                if (modifier == SelectionModifier.Replace)
                    ClearSelection();

                if (_selectionManager != null)
                    _selectionManager.StartMarquee(screenPoint, modifier);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select page object");
        }
    }

    private void ShowSelectionHighlight(PageObjectInfo obj)
    {
        ClearSelectionHighlight();
        if (DrawingCanvas == null || PdfImage == null) return;

        var topLeft = PdfCoordsToScreen(obj.Left, obj.Top);
        var bottomRight = PdfCoordsToScreen(obj.Right, obj.Bottom);
        if (topLeft == null || bottomRight == null) return;

        var x = Math.Min(topLeft.Value.X, bottomRight.Value.X);
        var y = Math.Min(topLeft.Value.Y, bottomRight.Value.Y);
        var w = Math.Abs(bottomRight.Value.X - topLeft.Value.X);
        var h = Math.Abs(bottomRight.Value.Y - topLeft.Value.Y);

        var isOrig = IsOriginalObject(obj);
        var strokeColor = isOrig
            ? Color.FromArgb(200, 255, 140, 0)   // Orange for original
            : Color.FromArgb(200, 0, 120, 215);   // Blue for user-added
        var fillColor = isOrig
            ? Color.FromArgb(15, 255, 140, 0)
            : Color.FromArgb(30, 0, 120, 215);

        _selectionHighlight = new Rectangle
        {
            Stroke = new SolidColorBrush(strokeColor),
            StrokeThickness = 2,
            StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 4, 2 },
            Fill = new SolidColorBrush(fillColor),
            Width = w,
            Height = h,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_selectionHighlight, x);
        Canvas.SetTop(_selectionHighlight, y);
        DrawingCanvas.Children.Add(_selectionHighlight);

        // Add resize handles (4 corners)
        AddResizeHandles(x, y, w, h);
    }

    private readonly System.Collections.Generic.List<(Rectangle rect, HandlePosition position)> _resizeHandles = new();
    private const double HandleSize = 8;

    private static StandardCursorType GetCursorForHandle(HandlePosition pos) => pos switch
    {
        HandlePosition.TopLeft or HandlePosition.BottomRight => StandardCursorType.TopLeftCorner,
        HandlePosition.TopRight or HandlePosition.BottomLeft => StandardCursorType.TopRightCorner,
        HandlePosition.MiddleLeft or HandlePosition.MiddleRight => StandardCursorType.SizeWestEast,
        HandlePosition.TopMiddle or HandlePosition.BottomMiddle => StandardCursorType.SizeNorthSouth,
        _ => StandardCursorType.Arrow
    };

    private void AddResizeHandles(double x, double y, double w, double h)
    {
        RemoveResizeHandles();
        if (DrawingCanvas == null) return;

        var positions = new (double px, double py, HandlePosition pos)[]
        {
            (x, y, HandlePosition.TopLeft),
            (x + w, y, HandlePosition.TopRight),
            (x, y + h, HandlePosition.BottomLeft),
            (x + w, y + h, HandlePosition.BottomRight),
            (x, y + h / 2, HandlePosition.MiddleLeft),
            (x + w, y + h / 2, HandlePosition.MiddleRight),
            (x + w / 2, y, HandlePosition.TopMiddle),
            (x + w / 2, y + h, HandlePosition.BottomMiddle),
        };

        foreach (var (px, py, pos) in positions)
        {
            var handle = new Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                StrokeThickness = 1.5,
                IsHitTestVisible = true,
                Cursor = new Cursor(GetCursorForHandle(pos)),
                Tag = pos
            };
            Canvas.SetLeft(handle, px - HandleSize / 2);
            Canvas.SetTop(handle, py - HandleSize / 2);
            handle.PointerPressed += OnResizeHandlePressed;
            DrawingCanvas.Children.Add(handle);
            _resizeHandles.Add((handle, pos));
        }
    }

    private void RemoveResizeHandles()
    {
        if (DrawingCanvas == null) return;
        foreach (var (h, _) in _resizeHandles)
        {
            h.PointerPressed -= OnResizeHandlePressed;
            DrawingCanvas.Children.Remove(h);
        }
        _resizeHandles.Clear();
    }

    private void OnResizeHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Rectangle rect || rect.Tag is not HandlePosition pos) return;
        if (_selectedObject == null || _selectionHighlight == null || PdfImage == null) return;

        _isResizing = true;
        _activeHandle = pos;
        _resizeStartPoint = e.GetCurrentPoint(PdfImage).Position;
        _resizeOriginalRect = new Rect(
            Canvas.GetLeft(_selectionHighlight),
            Canvas.GetTop(_selectionHighlight),
            _selectionHighlight.Width,
            _selectionHighlight.Height);
        e.Handled = true;
    }

    private Rect ComputeResizedRect(Point currentPoint, bool shift, bool alt)
    {
        var dx = currentPoint.X - _resizeStartPoint.X;
        var dy = currentPoint.Y - _resizeStartPoint.Y;
        var r = _resizeOriginalRect;

        double newX = r.X, newY = r.Y, newW = r.Width, newH = r.Height;

        // Adjust based on which handle is being dragged
        switch (_activeHandle)
        {
            case HandlePosition.TopLeft:
                newX = r.X + dx; newY = r.Y + dy; newW = r.Width - dx; newH = r.Height - dy; break;
            case HandlePosition.TopRight:
                newY = r.Y + dy; newW = r.Width + dx; newH = r.Height - dy; break;
            case HandlePosition.BottomLeft:
                newX = r.X + dx; newW = r.Width - dx; newH = r.Height + dy; break;
            case HandlePosition.BottomRight:
                newW = r.Width + dx; newH = r.Height + dy; break;
            case HandlePosition.MiddleLeft:
                newX = r.X + dx; newW = r.Width - dx; break;
            case HandlePosition.MiddleRight:
                newW = r.Width + dx; break;
            case HandlePosition.TopMiddle:
                newY = r.Y + dy; newH = r.Height - dy; break;
            case HandlePosition.BottomMiddle:
                newH = r.Height + dy; break;
        }

        // Shift = proportional (maintain aspect ratio)
        if (shift && r.Width > 0 && r.Height > 0)
        {
            var aspect = r.Width / r.Height;
            var isEdge = _activeHandle is HandlePosition.MiddleLeft or HandlePosition.MiddleRight
                or HandlePosition.TopMiddle or HandlePosition.BottomMiddle;
            if (!isEdge)
            {
                // Use the dominant axis
                if (Math.Abs(newW / r.Width - 1) > Math.Abs(newH / r.Height - 1))
                    newH = newW / aspect;
                else
                    newW = newH * aspect;

                // Recalculate position for top-left anchored handles
                if (_activeHandle is HandlePosition.TopLeft)
                { newX = r.X + r.Width - newW; newY = r.Y + r.Height - newH; }
                else if (_activeHandle is HandlePosition.TopRight)
                { newY = r.Y + r.Height - newH; }
                else if (_activeHandle is HandlePosition.BottomLeft)
                { newX = r.X + r.Width - newW; }
            }
        }

        // Alt = resize from center (symmetric)
        if (alt)
        {
            var cx = r.X + r.Width / 2;
            var cy = r.Y + r.Height / 2;
            newX = cx - newW / 2;
            newY = cy - newH / 2;
        }

        // Clamp minimum size
        newW = Math.Max(newW, 5);
        newH = Math.Max(newH, 5);

        return new Rect(newX, newY, newW, newH);
    }

    /// <summary>Resize the selected object via REST API.</summary>
    public async Task<bool> ResizeSelectedObjectAsync(float scaleX, float scaleY, float anchorPdfX, float anchorPdfY)
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null) return false;
        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var result = await shapeService.ResizePageObjectAsync(docId, pageNumber, _selectedObject.Index, scaleX, scaleY, anchorPdfX, anchorPdfY);
        if (result) await _viewModel.RefreshCurrentPageSilentAsync();
        return result;
    }

    private async Task CommitResizeAsync(float scaleX, float scaleY, float anchorPdfX, float anchorPdfY)
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null) return;
        try
        {
            var shapeService = App.GetService<IShapeService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageNumber = _viewModel.CurrentPageNumber - 1;
            var success = await shapeService.ResizePageObjectAsync(
                docId, pageNumber, _selectedObject.Index, scaleX, scaleY, anchorPdfX, anchorPdfY);
            if (success)
            {
                // Refresh and re-select the resized object
                await _viewModel.RefreshCurrentPageSilentAsync();
                var objects = await shapeService.GetPageObjectsAsync(docId, pageNumber);
                var updated = objects.FirstOrDefault(o => o.Index == _selectedObject.Index);
                if (updated != null)
                {
                    _selectedObject = updated;
                    _selectedObjects.Clear();
                    _selectedObjects.Add(updated);
                    ShowSelectionHighlight(updated);
                }
                _logger.LogInformation("Resized object {Index} by ({ScaleX:F2}, {ScaleY:F2})", _selectedObject?.Index, scaleX, scaleY);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit resize");
        }
    }

    private void UpdateResizeHandlePositions(double x, double y, double w, double h)
    {
        foreach (var (handle, pos) in _resizeHandles)
        {
            var (px, py) = pos switch
            {
                HandlePosition.TopLeft => (x, y),
                HandlePosition.TopRight => (x + w, y),
                HandlePosition.BottomLeft => (x, y + h),
                HandlePosition.BottomRight => (x + w, y + h),
                HandlePosition.MiddleLeft => (x, y + h / 2),
                HandlePosition.MiddleRight => (x + w, y + h / 2),
                HandlePosition.TopMiddle => (x + w / 2, y),
                HandlePosition.BottomMiddle => (x + w / 2, y + h),
                _ => (x, y)
            };
            Canvas.SetLeft(handle, px - HandleSize / 2);
            Canvas.SetTop(handle, py - HandleSize / 2);
        }
    }

    public void ClearSelection()
    {
        _selectedObject = null;
        _selectedObjects.Clear();
        ClearSelectionHighlight();
        ClearMultiSelectionHighlights();
    }

    private void ShowMultiSelectionHighlights()
    {
        ClearSelectionHighlight();
        ClearMultiSelectionHighlights();
        if (DrawingCanvas == null || PdfImage == null) return;

        foreach (var obj in _selectedObjects)
        {
            var topLeft = PdfCoordsToScreen(obj.Left, obj.Top);
            var bottomRight = PdfCoordsToScreen(obj.Right, obj.Bottom);
            if (topLeft == null || bottomRight == null) continue;

            var x = Math.Min(topLeft.Value.X, bottomRight.Value.X);
            var y = Math.Min(topLeft.Value.Y, bottomRight.Value.Y);
            var w = Math.Abs(bottomRight.Value.X - topLeft.Value.X);
            var h = Math.Abs(bottomRight.Value.Y - topLeft.Value.Y);

            var isOrig = IsOriginalObject(obj);
            var strokeColor = isOrig
                ? Color.FromArgb(200, 255, 140, 0)
                : Color.FromArgb(200, 0, 120, 215);
            var fillColor = isOrig
                ? Color.FromArgb(15, 255, 140, 0)
                : Color.FromArgb(30, 0, 120, 215);

            var rect = new Rectangle
            {
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 2,
                StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 4, 2 },
                Fill = new SolidColorBrush(fillColor),
                Width = w,
                Height = h,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            DrawingCanvas.Children.Add(rect);
            _multiSelectionHighlights.Add(rect);
        }
    }

    private void ClearMultiSelectionHighlights()
    {
        if (DrawingCanvas == null) return;
        foreach (var h in _multiSelectionHighlights)
            DrawingCanvas.Children.Remove(h);
        _multiSelectionHighlights.Clear();
    }

    private void ClearSelectionHighlight()
    {
        RemoveResizeHandles();
        if (_selectionHighlight != null && DrawingCanvas != null)
        {
            DrawingCanvas.Children.Remove(_selectionHighlight);
            _selectionHighlight = null;
        }
    }

    private async Task DeleteSelectedObjectsAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var toDelete = _selectedObjects.Count > 0
            ? _selectedObjects.ToList()
            : (_selectedObject != null ? new List<PageObjectInfo> { _selectedObject } : new List<PageObjectInfo>());

        if (toDelete.Count == 0) return;

        // Check lock for all
        foreach (var obj in toDelete)
        {
            if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(obj))
            {
                _logger.LogInformation("Cannot delete locked original object #{Index}", obj.Index);
                _viewModel.StatusMessage = "Some original PDF objects are locked. Unlock to delete.";
                return;
            }
        }

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var undoService = App.GetService<IUndoRedoService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageNumber = _viewModel.CurrentPageNumber - 1;

            // Delete in reverse index order to avoid index shifting
            var sorted = toDelete.OrderByDescending(o => o.Index).ToList();
            int deletedCount = 0;

            foreach (var obj in sorted)
            {
                // Get properties before deletion for undo
                var props = await shapeService.GetPageObjectPropertiesAsync(docId, pageNumber, obj.Index);

                var success = await shapeService.RemovePageObjectAsync(docId, pageNumber, obj.Index);
                if (success)
                {
                    deletedCount++;

                    // Push undo action with shape data for recreation
                    undoService.Push(new DeleteShapeAction(docId, pageNumber,
                        new ShapeCreationData
                        {
                            ShapeType = DrawingShapeType.Rectangle, // Generic - we store bounds
                            X = obj.Left,
                            Y = obj.Bottom,
                            Width = obj.Right - obj.Left,
                            Height = obj.Top - obj.Bottom,
                            StrokeColor = props?.StrokeColor ?? "#000000",
                            FillColor = props?.FillColor ?? "#00000000",
                            StrokeWidth = props?.StrokeWidth ?? 2f
                        }));
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} page objects", deletedCount);
                ClearSelection();
                await _viewModel.RefreshCurrentPageAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete selected objects");
        }
    }

    private async Task DeleteSelectedObjectAsync()
    {
        await DeleteSelectedObjectsAsync();
    }

    private async Task MoveSelectedObjectAsync(float deltaPdfX, float deltaPdfY)
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null)
            return;

        if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(_selectedObject))
        {
            _viewModel.StatusMessage = "Original PDF objects are locked. Unlock to move.";
            return;
        }

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageNumber = _viewModel.CurrentPageNumber - 1;

            var success = await shapeService.MovePageObjectAsync(
                docId, pageNumber, _selectedObject.Index, deltaPdfX, deltaPdfY);
            if (success)
            {
                // Record undo
                var undoService = App.GetService<IUndoRedoService>();
                undoService.Push(new MoveShapeAction(docId, pageNumber, _selectedObject.Index, deltaPdfX, deltaPdfY));

                // Update selected object bounds
                _selectedObject = _selectedObject with
                {
                    Left = _selectedObject.Left + deltaPdfX,
                    Right = _selectedObject.Right + deltaPdfX,
                    Bottom = _selectedObject.Bottom + deltaPdfY,
                    Top = _selectedObject.Top + deltaPdfY
                };
                ShowSelectionHighlight(_selectedObject);
                await _viewModel.RefreshCurrentPageSilentAsync();
            }
            else
            {
                _viewModel.StatusMessage = "Failed to move object. It may have been invalidated.";
                ClearSelectionHighlight();
                _selectedObject = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move selected object");
        }
    }

    private async Task MoveSelectedObjectsAsync(float deltaPdfX, float deltaPdfY)
    {
        if (_selectedObjects.Count == 0 || _viewModel?.CurrentDocument == null)
            return;

        foreach (var obj in _selectedObjects)
        {
            if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(obj))
            {
                _viewModel.StatusMessage = "Some original objects are locked. Unlock to move.";
                return;
            }
        }

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var undoService = App.GetService<IUndoRedoService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageNumber = _viewModel.CurrentPageNumber - 1;

            // Batch move: single GenerateContent call to prevent index shifting and text corruption
            var indices = _selectedObjects.Select(o => o.Index).ToArray();
            int movedCount = await shapeService.MovePageObjectsBatchAsync(
                docId, pageNumber, indices, deltaPdfX, deltaPdfY);

            if (movedCount > 0)
            {
                for (int i = 0; i < _selectedObjects.Count; i++)
                {
                    var obj = _selectedObjects[i];
                    undoService.Push(new MoveShapeAction(docId, pageNumber, obj.Index, deltaPdfX, deltaPdfY));
                    _selectedObjects[i] = obj with
                    {
                        Left = obj.Left + deltaPdfX,
                        Right = obj.Right + deltaPdfX,
                        Bottom = obj.Bottom + deltaPdfY,
                        Top = obj.Top + deltaPdfY
                    };
                }
            }

            _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;

            if (movedCount > 0)
            {
                ShowMultiSelectionHighlights();
                await _viewModel.RefreshCurrentPageSilentAsync();
                _logger.LogInformation("Moved {Count} objects", movedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move selected objects");
        }
    }

    private async Task FinishMarqueeSelectionAsync(Point endPoint)
    {
        if (_selectionManager == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));

        var result = _selectionManager.FinishMarquee(endPoint, objects, _selectedObjects.ToList());
        ApplySelectionResult(result);
    }

    private async Task FinishLassoSelectionAsync()
    {
        if (_selectionManager == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));

        var result = _selectionManager.FinishLasso(objects, _selectedObjects.ToList());
        ApplySelectionResult(result);
    }

    private void ApplySelectionResult(MarqueeResult result)
    {
        _selectedObjects.Clear();
        var filtered = _viewModel?.IsOriginalObjectsLocked == true
            ? result.SelectedObjects.Where(o => !IsOriginalObject(o)).ToList()
            : result.SelectedObjects;
        _selectedObjects.AddRange(filtered);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;

        if (_selectedObjects.Count == 1)
            ShowSelectionHighlight(_selectedObjects[0]);
        else if (_selectedObjects.Count > 1)
            ShowMultiSelectionHighlights();
        else
            ClearSelection();

        _logger.LogInformation("Selection: {Count} objects", _selectedObjects.Count);
    }

    /// <summary>Gets the list of currently selected objects.</summary>
    public List<PageObjectInfo> GetSelectedObjects() => _selectedObjects.ToList();

    /// <summary>Gets/sets the selection containment mode.</summary>
    public SelectionContainment ContainmentMode
    {
        get => _selectionManager?.ContainmentMode ?? SelectionContainment.Intersect;
        set { if (_selectionManager != null) _selectionManager.ContainmentMode = value; }
    }

    private void ShowShapeContextMenu(Point screenPoint)
    {
        if (_selectedObject == null || _viewModel == null || DrawingCanvas == null) return;

        var isOrig = IsOriginalObject(_selectedObject);
        var isLocked = _viewModel.IsOriginalObjectsLocked && isOrig;

        var menu = new global::Avalonia.Controls.ContextMenu();

        var deleteItem = new global::Avalonia.Controls.MenuItem { Header = "Delete" };
        deleteItem.IsEnabled = !isLocked;
        deleteItem.Click += (s, e) => _ = DeleteSelectedObjectsAsync();
        menu.Items.Add(deleteItem);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        var strokeRedItem = new global::Avalonia.Controls.MenuItem { Header = "Stroke: Red" };
        strokeRedItem.IsEnabled = !isLocked;
        strokeRedItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("stroke", "#FF0000");
        menu.Items.Add(strokeRedItem);

        var strokeBlueItem = new global::Avalonia.Controls.MenuItem { Header = "Stroke: Blue" };
        strokeBlueItem.IsEnabled = !isLocked;
        strokeBlueItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("stroke", "#0000FF");
        menu.Items.Add(strokeBlueItem);

        var strokeBlackItem = new global::Avalonia.Controls.MenuItem { Header = "Stroke: Black" };
        strokeBlackItem.IsEnabled = !isLocked;
        strokeBlackItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("stroke", "#000000");
        menu.Items.Add(strokeBlackItem);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        var fillRedItem = new global::Avalonia.Controls.MenuItem { Header = "Fill: Red" };
        fillRedItem.IsEnabled = !isLocked;
        fillRedItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("fill", "#FF000080");
        menu.Items.Add(fillRedItem);

        var fillTransparent = new global::Avalonia.Controls.MenuItem { Header = "Fill: Transparent" };
        fillTransparent.IsEnabled = !isLocked;
        fillTransparent.Click += (s, e) => _ = ChangeSelectedPropertyAsync("fill", "#00000000");
        menu.Items.Add(fillTransparent);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        var propsItem = new global::Avalonia.Controls.MenuItem { Header = "Properties..." };
        propsItem.Click += (s, e) => _ = ShowPropertiesAsync();
        menu.Items.Add(propsItem);

        menu.Open(DrawingCanvas);
    }

    private async Task ChangeSelectedPropertyAsync(string property, string value)
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;

        bool success = property switch
        {
            "stroke" => await shapeService.SetPageObjectStrokeColorAsync(docId, pageNumber, _selectedObject.Index, value),
            "fill" => await shapeService.SetPageObjectFillColorAsync(docId, pageNumber, _selectedObject.Index, value),
            _ => false
        };

        if (success)
            await _viewModel.RefreshCurrentPageAsync();
    }

    private async Task ShowPropertiesAsync()
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var props = await shapeService.GetPageObjectPropertiesAsync(docId, pageNumber, _selectedObject.Index);

        if (props != null)
        {
            _viewModel.StatusMessage = $"Object #{_selectedObject.Index}: " +
                $"Type={_selectedObject.Type}, Stroke={props.StrokeColor}, Fill={props.FillColor}, Width={props.StrokeWidth:F1}";
        }
    }

    #region Undo/Redo

    private async Task UndoAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var undoService = App.GetService<IUndoRedoService>();
        var action = undoService.Undo();
        if (action == null) return;

        var shapeService = App.GetService<IShapeService>();

        switch (action)
        {
            case AddShapeAction addAction:
                // Undo add = remove last object on the page
                var objects = await shapeService.GetPageObjectsAsync(addAction.DocumentId, addAction.PageNumber);
                if (objects.Count > 0)
                {
                    await shapeService.RemovePageObjectAsync(addAction.DocumentId, addAction.PageNumber, objects[^1].Index);
                }
                break;

            case DeleteShapeAction deleteAction:
                // Undo delete = re-create the shape
                var d = deleteAction.CreationData;
                await shapeService.AddRectangleAsync(
                    deleteAction.DocumentId, deleteAction.PageNumber,
                    d.X, d.Y, d.Width, d.Height,
                    d.FillColor, d.StrokeColor, d.StrokeWidth);
                break;

            case MoveShapeAction moveAction:
                // Undo move = move back
                await shapeService.MovePageObjectAsync(
                    moveAction.DocumentId, moveAction.PageNumber,
                    moveAction.ObjectIndex, -moveAction.DeltaX, -moveAction.DeltaY);
                break;
        }

        ClearSelection();
        await _viewModel.RefreshCurrentPageAsync();
        _logger.LogInformation("Undo: {ActionType}", action.ActionType);
    }

    private async Task RedoAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var undoService = App.GetService<IUndoRedoService>();
        var action = undoService.Redo();
        if (action == null) return;

        var shapeService = App.GetService<IShapeService>();

        switch (action)
        {
            case AddShapeAction addAction:
                // Redo add = re-create the shape
                var d = addAction.CreationData;
                switch (d.ShapeType)
                {
                    case DrawingShapeType.Rectangle:
                        var rx = Math.Min(d.X, d.X2);
                        var ry = Math.Min(d.Y, d.Y2);
                        await shapeService.AddRectangleAsync(
                            addAction.DocumentId, addAction.PageNumber,
                            rx, ry, d.Width, d.Height,
                            d.FillColor, d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Circle:
                        var cx = (d.X + d.X2) / 2;
                        var cy = (d.Y + d.Y2) / 2;
                        await shapeService.AddCircleAsync(
                            addAction.DocumentId, addAction.PageNumber,
                            cx, cy, d.Radius,
                            d.FillColor, d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Line:
                        await shapeService.AddLineAsync(
                            addAction.DocumentId, addAction.PageNumber,
                            d.X, d.Y, d.X2, d.Y2,
                            d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Freehand when d.Points != null:
                        await shapeService.AddFreehandPathAsync(
                            addAction.DocumentId, addAction.PageNumber,
                            d.Points, d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Text:
                        await shapeService.AddTextAsync(
                            addAction.DocumentId, addAction.PageNumber,
                            d.X, d.Y, d.Text ?? "Text",
                            d.FontSize, d.FontName, d.StrokeColor);
                        break;
                }
                break;

            case DeleteShapeAction deleteAction:
                // Redo delete = remove again (find by bounds)
                var objs = await shapeService.GetPageObjectsAsync(deleteAction.DocumentId, deleteAction.PageNumber);
                if (objs.Count > 0)
                    await shapeService.RemovePageObjectAsync(deleteAction.DocumentId, deleteAction.PageNumber, objs[^1].Index);
                break;

            case MoveShapeAction moveAction:
                // Redo move = move forward again
                await shapeService.MovePageObjectAsync(
                    moveAction.DocumentId, moveAction.PageNumber,
                    moveAction.ObjectIndex, moveAction.DeltaX, moveAction.DeltaY);
                break;
        }

        ClearSelection();
        await _viewModel.RefreshCurrentPageAsync();
        _logger.LogInformation("Redo: {ActionType}", action.ActionType);
    }

    private async Task SelectAllObjectsAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;

        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));

        _selectedObjects.Clear();
        var selectable = _viewModel.IsOriginalObjectsLocked
            ? objects.Where(o => !IsOriginalObject(o)).ToList()
            : objects;
        _selectedObjects.AddRange(selectable);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        ShowMultiSelectionHighlights();
        _logger.LogInformation("Selected all {Count} objects on page (filtered from {Total})",
            _selectedObjects.Count, objects.Count);
    }

    #endregion

    /// <summary>
    /// Converts PDF coordinates (points, origin bottom-left) to screen coordinates relative to PdfImage.
    /// </summary>
    private Point? PdfCoordsToScreen(float pdfX, float pdfY)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return null;

        var imageSource = PdfImage.Source as global::Avalonia.Media.Imaging.Bitmap;
        if (imageSource == null) return null;

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

        var renderingService = App.GetService<IPdfRenderingService>();
        var pageSizeResult = renderingService.GetPageSize(
            _viewModel.CurrentDocument, _viewModel.CurrentPageNumber);
        if (pageSizeResult.IsFailed) return null;

        var pageWidth = pageSizeResult.Value.Width;
        var pageHeight = pageSizeResult.Value.Height;

        // Reverse of ScreenPointToPdfCoords (ratio-based):
        // pdfX = (bmpX / bitmapWidth) * pageWidth  =>  bmpX = (pdfX / pageWidth) * bitmapWidth
        // pdfY = pageHeight - (bmpY / bitmapHeight) * pageHeight  =>  bmpY = ((pageHeight - pdfY) / pageHeight) * bitmapHeight
        var bmpX = (pdfX / pageWidth) * bitmapWidth;
        var bmpY = ((pageHeight - pdfY) / pageHeight) * bitmapHeight;

        // bmpX = (screenX - offsetX) / scale  =>  screenX = bmpX * scale + offsetX
        var screenX = bmpX * scale + offsetX;
        var screenY = bmpY * scale + offsetY;

        return new Point(screenX, screenY);
    }

    #endregion

    #region Public Automation API

    /// <summary>Gets the currently selected page object, or null.</summary>
    public PageObjectInfo? GetSelectedObject() => _selectedObject;

    /// <summary>Whether a selection highlight is visible.</summary>
    public bool HasSelectionHighlight => _selectionHighlight != null;

    /// <summary>Select a page object at PDF coordinates. Returns the hit object or null.</summary>
    public async Task<PageObjectInfo?> SelectObjectAtPdfCoordsAsync(float pdfX, float pdfY)
    {
        if (_viewModel?.CurrentDocument == null) return null;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;

        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));

        PageObjectInfo? hit = null;
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            var obj = objects[i];
            if (pdfX >= obj.Left && pdfX <= obj.Right &&
                pdfY >= obj.Bottom && pdfY <= obj.Top)
            {
                hit = obj;
                break;
            }
        }

        if (hit != null)
        {
            _selectedObject = hit;
            ShowSelectionHighlight(hit);
        }
        else
        {
            ClearSelection();
        }

        return hit;
    }

    /// <summary>Delete the currently selected object. Returns true if deleted.</summary>
    public async Task<bool> DeleteSelectedAsync()
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null)
            return false;

        if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(_selectedObject))
            return false;

        // Capture state on UI thread
        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var objIndex = _selectedObject.Index;

        try
        {
            var success = await shapeService.RemovePageObjectAsync(docId, pageNumber, objIndex);
            if (success)
            {
                ClearSelection();
                await _viewModel.RefreshCurrentPageAsync();
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteSelectedAsync failed for object {Index}", objIndex);
            return false;
        }
    }

    /// <summary>Draw a shape at PDF coordinates using the current tool and colors.</summary>
    public async Task<bool> DrawShapeAtPdfCoordsAsync(float startX, float startY, float endX, float endY)
    {
        if (_viewModel?.CurrentDocument == null) return false;

        var shapeService = App.GetService<IShapeService>();
        var tool = _viewModel.ActiveDrawingTool;
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var strokeColor = _viewModel.DrawingStrokeColor;
        var fillColor = _viewModel.DrawingFillColor;
        var strokeWidth = _viewModel.DrawingStrokeWidth;

        bool success = false;
        switch (tool)
        {
            case DrawingTool.Rectangle:
                var rx = Math.Min(startX, endX);
                var ry = Math.Min(startY, endY);
                var rw = Math.Abs(endX - startX);
                var rh = Math.Abs(endY - startY);
                if (rw > 1 && rh > 1)
                    success = await shapeService.AddRectangleAsync(docId, pageNumber, rx, ry, rw, rh, fillColor, strokeColor, strokeWidth);
                break;
            case DrawingTool.Circle:
                var cx = (startX + endX) / 2;
                var cy = (startY + endY) / 2;
                var radius = Math.Max(Math.Abs(endX - startX), Math.Abs(endY - startY)) / 2;
                if (radius > 1)
                    success = await shapeService.AddCircleAsync(docId, pageNumber, cx, cy, radius, fillColor, strokeColor, strokeWidth);
                break;
            case DrawingTool.Line:
                success = await shapeService.AddLineAsync(docId, pageNumber, startX, startY, endX, endY, strokeColor, strokeWidth);
                break;
        }

        if (success)
            await _viewModel.RefreshCurrentPageAsync();
        return success;
    }

    /// <summary>Add text at PDF coordinates with custom string.</summary>
    public async Task<bool> AddTextAtPdfCoordsAsync(float pdfX, float pdfY, string text, float fontSize = 12f, string fontName = "Helvetica")
    {
        if (_viewModel?.CurrentDocument == null) return false;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        var color = _viewModel.DrawingStrokeColor;

        var success = await shapeService.AddTextAsync(docId, pageNumber, pdfX, pdfY, text, fontSize, fontName, color);
        if (success)
            await _viewModel.RefreshCurrentPageAsync();
        return success;
    }

    /// <summary>List all page objects on the current page.</summary>
    public async Task<List<PageObjectInfo>> GetCurrentPageObjectsAsync()
    {
        if (_viewModel?.CurrentDocument == null) return new List<PageObjectInfo>();

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;
        return await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));
    }

    /// <summary>Hit-test at PDF coordinates without selecting.</summary>
    public async Task<PageObjectInfo?> HitTestAtPdfCoordsAsync(float pdfX, float pdfY)
    {
        if (_viewModel?.CurrentDocument == null) return null;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageNumber = _viewModel.CurrentPageNumber - 1;

        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageNumber));
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            var obj = objects[i];
            if (pdfX >= obj.Left && pdfX <= obj.Right &&
                pdfY >= obj.Bottom && pdfY <= obj.Top)
                return obj;
        }
        return null;
    }

    #endregion
}
