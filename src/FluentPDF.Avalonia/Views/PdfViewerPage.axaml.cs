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
            if (_viewModel.Metadata != null)
            {
                _viewModel.Metadata.ReadMetadataCallback = ReadDocumentMetadata;
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
            if (vm.Metadata != null)
            {
                vm.Metadata.ReadMetadataCallback = ReadDocumentMetadata;
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
        var pi = new PageIndex(_viewModel.CurrentPageNumber - 1);
        if (_viewModel.OriginalObjectCounts.ContainsKey(pi.Value)) return;

        var docId = _viewModel.CurrentDocument.FilePath;

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pi));
            if (_viewModel != null && !_viewModel.OriginalObjectCounts.ContainsKey(pi.Value))
            {
                _viewModel.OriginalObjectCounts[pi.Value] = objects.Count;
                _logger.LogDebug("Tracked {Count} original objects on page {Page}", objects.Count, pi);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track original object count");
        }
    }

    #region Metadata

    /// <summary>
    /// Reads PDF metadata from the native PDFium document handle.
    /// </summary>
    private DocumentMetadataResult? ReadDocumentMetadata(PdfDocument document)
    {
        if (document.Handle is not SafePdfDocumentHandle docHandle)
            return null;

        var title = PdfiumInterop.GetMetaText(docHandle, "Title");
        var author = PdfiumInterop.GetMetaText(docHandle, "Author");
        var subject = PdfiumInterop.GetMetaText(docHandle, "Subject");
        var keywords = PdfiumInterop.GetMetaText(docHandle, "Keywords");
        var creator = PdfiumInterop.GetMetaText(docHandle, "Creator");
        var producer = PdfiumInterop.GetMetaText(docHandle, "Producer");
        var creationDate = PdfiumInterop.GetMetaText(docHandle, "CreationDate");
        var modDate = PdfiumInterop.GetMetaText(docHandle, "ModDate");

        string pdfVersion = string.Empty;
        if (PdfiumInterop.GetFileVersion(docHandle, out int version))
        {
            pdfVersion = $"{version / 10}.{version % 10}";
        }

        uint permFlags = PdfiumInterop.GetDocPermissions(docHandle);
        bool isEncrypted = permFlags != 0 && permFlags != 0xFFFFFFFF;

        var permParts = new System.Collections.Generic.List<string>();
        if (!isEncrypted)
        {
            permParts.Add("All");
        }
        else
        {
            if ((permFlags & (1 << 2)) != 0) permParts.Add("Print");
            if ((permFlags & (1 << 4)) != 0) permParts.Add("Copy");
            if ((permFlags & (1 << 3)) != 0) permParts.Add("Modify");
            if ((permFlags & (1 << 5)) != 0) permParts.Add("Annotate");
        }

        return new DocumentMetadataResult
        {
            Title = title,
            Author = author,
            Subject = subject,
            Keywords = keywords,
            Creator = creator,
            Producer = producer,
            CreationDate = creationDate,
            ModificationDate = modDate,
            PdfVersion = pdfVersion,
            IsEncrypted = isEncrypted,
            Permissions = string.Join(", ", permParts)
        };
    }

    #endregion

    #region Coordinate Conversion

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
