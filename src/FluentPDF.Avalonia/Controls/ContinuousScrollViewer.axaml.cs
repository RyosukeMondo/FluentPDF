using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Continuous scroll viewer for PDF pages with virtual scrolling.
/// Renders only visible pages for performance optimization.
/// </summary>
public partial class ContinuousScrollViewer : UserControl
{
    private readonly ILogger<ContinuousScrollViewer>? _logger;
    private readonly IPdfRenderingService? _renderingService;
    private PdfDocument? _document;
    private double _zoomLevel = 1.0;
    private int _currentVisiblePage = 1;

    // Text selection state
    private bool _isSelecting;
    private Point _selectionStartPoint;
    private int _selectionPageIndex = -1;
    private Rectangle? _activeSelectionRectangle;

    // Performance optimization
    private CancellationTokenSource? _scrollDebounceToken;
    private const int ScrollDebounceMs = 100;
    private const int RenderBufferPages = 3; // Render 3 pages above and below viewport

    public ObservableCollection<PageItemViewModel> Pages { get; } = new();

    public ContinuousScrollViewer()
    {
        InitializeComponent();

        // Get services from DI container if available
        try
        {
            _logger = App.GetService<ILogger<ContinuousScrollViewer>>();
            _renderingService = App.GetService<IPdfRenderingService>();
        }
        catch
        {
            // Services not available in design mode
        }

        DataContext = this;
    }

    /// <summary>
    /// Loads a PDF document for continuous scrolling.
    /// </summary>
    public async Task LoadDocumentAsync(PdfDocument document, double zoomLevel)
    {
        _document = document;
        _zoomLevel = zoomLevel;

        Pages.Clear();

        if (_document == null)
        {
            return;
        }

        // Create page view models
        for (int i = 0; i < _document.PageCount; i++)
        {
            Pages.Add(new PageItemViewModel
            {
                PageNumber = i + 1,
                PageIndex = i,
                IsLoading = false,
                PageImage = null
            });
        }

        // Wire up item source
        var repeater = this.FindControl<ItemsControl>("PageRepeater");
        if (repeater != null)
        {
            repeater.ItemsSource = Pages;
        }

        // Render first few pages immediately
        await RenderVisiblePagesAsync();
    }

    /// <summary>
    /// Updates the zoom level and re-renders visible pages.
    /// </summary>
    public async Task UpdateZoomAsync(double zoomLevel)
    {
        if (Math.Abs(_zoomLevel - zoomLevel) < 0.001)
        {
            return;
        }

        _zoomLevel = zoomLevel;
        await RenderVisiblePagesAsync();
    }

    /// <summary>
    /// Scrolls to a specific page number.
    /// </summary>
    public void ScrollToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > Pages.Count)
        {
            return;
        }

        var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
        if (scrollViewer != null)
        {
            // Calculate approximate scroll offset
            var estimatedPageHeight = 900 * _zoomLevel + 16; // page height + spacing
            var targetOffset = (pageNumber - 1) * estimatedPageHeight;
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, targetOffset);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Wire up scroll changed event
        var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += OnScrollViewChanged;
        }

        // Wire up pointer events for text selection on the repeater
        var repeater = this.FindControl<ItemsControl>("PageRepeater");
        if (repeater != null)
        {
            repeater.AddHandler(PointerPressedEvent, OnPagePointerPressed, handledEventsToo: true);
            repeater.AddHandler(PointerMovedEvent, OnPagePointerMoved, handledEventsToo: true);
            repeater.AddHandler(PointerReleasedEvent, OnPagePointerReleased, handledEventsToo: true);
        }
    }

    /// <summary>
    /// Handles scroll position changes to track current page and render visible pages.
    /// Uses debouncing to improve performance during rapid scrolling.
    /// </summary>
    private async void OnScrollViewChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Update current page indicator immediately for better UX
        UpdateCurrentPageIndicator();

        // Debounce rendering for performance
        _scrollDebounceToken?.Cancel();
        _scrollDebounceToken = new CancellationTokenSource();
        var token = _scrollDebounceToken.Token;

        try
        {
            // Wait for debounce delay
            await Task.Delay(ScrollDebounceMs, token);

            // Render visible pages if not cancelled
            if (!token.IsCancellationRequested)
            {
                await RenderVisiblePagesAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when scrolling continues, ignore
        }
    }

    /// <summary>
    /// Renders only the pages currently visible in the viewport.
    /// Virtual scrolling implementation for performance.
    /// </summary>
    private async Task RenderVisiblePagesAsync()
    {
        if (_document == null || Pages.Count == 0 || _renderingService == null)
        {
            return;
        }

        try
        {
            var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
            if (scrollViewer == null)
            {
                return;
            }

            var viewportHeight = scrollViewer.Viewport.Height;
            var scrollOffset = scrollViewer.Offset.Y;

            // Estimate page height (approximate, will vary with zoom)
            var estimatedPageHeight = 900 * _zoomLevel;

            // Calculate visible range with optimized buffer
            var startIndex = Math.Max(0, (int)(scrollOffset / estimatedPageHeight) - RenderBufferPages);
            var endIndex = Math.Min(Pages.Count - 1, (int)((scrollOffset + viewportHeight) / estimatedPageHeight) + RenderBufferPages);

            // Render visible pages in parallel for better performance
            var renderTasks = new System.Collections.Generic.List<Task>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                var page = Pages[i];
                if (page.PageImage == null && !page.IsLoading)
                {
                    page.IsLoading = true;
                    var pageIndex = i;
                    renderTasks.Add(Task.Run(async () =>
                    {
                        await RenderPageAsync(page);
                        page.IsLoading = false;
                    }));
                }
            }

            // Wait for all render tasks to complete
            if (renderTasks.Count > 0)
            {
                await Task.WhenAll(renderTasks);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering visible pages in continuous scroll mode");
        }
    }

    /// <summary>
    /// Renders a single page.
    /// </summary>
    private async Task RenderPageAsync(PageItemViewModel pageItem)
    {
        if (_document == null || _renderingService == null)
        {
            return;
        }

        try
        {
            var result = await _renderingService.RenderPageAsync(
                _document,
                pageItem.PageIndex + 1,
                _zoomLevel);

            if (result.IsSuccess && result.Value != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using var stream = result.Value;
                    var bitmap = new Bitmap(stream);
                    pageItem.PageImage = bitmap;
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering page {PageNumber}", pageItem.PageNumber);
        }
    }

    /// <summary>
    /// Updates the current page indicator based on scroll position.
    /// </summary>
    private void UpdateCurrentPageIndicator()
    {
        if (Pages.Count == 0)
        {
            return;
        }

        var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
        var indicator = this.FindControl<TextBlock>("CurrentPageIndicator");

        if (scrollViewer != null && indicator != null)
        {
            var scrollOffset = scrollViewer.Offset.Y;
            var estimatedPage = Math.Min(Pages.Count, Math.Max(1, (int)(scrollOffset / 900) + 1));

            if (_currentVisiblePage != estimatedPage)
            {
                _currentVisiblePage = estimatedPage;
                indicator.Text = $"Page {_currentVisiblePage} of {Pages.Count}";

                // Notify parent that current page changed
                CurrentPageChanged?.Invoke(this, _currentVisiblePage);
            }
        }
    }

    /// <summary>
    /// Event raised when the current visible page changes.
    /// </summary>
    public event EventHandler<int>? CurrentPageChanged;

    /// <summary>
    /// Event raised when text selection begins on a page.
    /// </summary>
    public event EventHandler<TextSelectionEventArgs>? TextSelectionStarted;

    /// <summary>
    /// Event raised when text selection is updated.
    /// </summary>
    public event EventHandler<TextSelectionEventArgs>? TextSelectionUpdated;

    /// <summary>
    /// Event raised when text selection ends.
    /// </summary>
    public event EventHandler<TextSelectionEventArgs>? TextSelectionEnded;

    /// <summary>
    /// Handles pointer pressed on a page image to begin text selection.
    /// </summary>
    private void OnPagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Control;
        if (source == null)
        {
            return;
        }

        // Find the Image control that was clicked
        var image = FindParentOfType<Image>(source);
        if (image == null)
        {
            return;
        }

        var properties = e.GetCurrentPoint(image).Properties;

        // Only start selection on left-click
        if (properties.IsLeftButtonPressed)
        {
            var point = e.GetCurrentPoint(image).Position;

            // Find the page index from the DataContext
            if (image.DataContext is PageItemViewModel pageItem)
            {
                _isSelecting = true;
                _selectionStartPoint = point;
                _selectionPageIndex = pageItem.PageIndex;

                // Find and show selection rectangle
                var grid = FindParentOfType<Grid>(image);
                if (grid != null)
                {
                    var canvas = grid.FindControl<Canvas>("SelectionCanvas");
                    if (canvas != null)
                    {
                        _activeSelectionRectangle = canvas.FindControl<Rectangle>("SelectionRectangle");
                        if (_activeSelectionRectangle != null)
                        {
                            _activeSelectionRectangle.IsVisible = true;
                            _activeSelectionRectangle.Width = 0;
                            _activeSelectionRectangle.Height = 0;
                            Canvas.SetLeft(_activeSelectionRectangle, point.X);
                            Canvas.SetTop(_activeSelectionRectangle, point.Y);
                        }
                    }
                }

                // Notify selection started
                TextSelectionStarted?.Invoke(this, new TextSelectionEventArgs
                {
                    PageIndex = pageItem.PageIndex,
                    StartPoint = point,
                    EndPoint = point
                });

                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Handles pointer moved on a page image to update text selection.
    /// </summary>
    private void OnPagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        var source = e.Source as Control;
        if (source == null)
        {
            return;
        }

        var image = FindParentOfType<Image>(source);
        if (image == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(image).Position;

        // Update selection rectangle
        if (_activeSelectionRectangle != null)
        {
            var x = Math.Min(_selectionStartPoint.X, point.X);
            var y = Math.Min(_selectionStartPoint.Y, point.Y);
            var width = Math.Abs(point.X - _selectionStartPoint.X);
            var height = Math.Abs(point.Y - _selectionStartPoint.Y);

            Canvas.SetLeft(_activeSelectionRectangle, x);
            Canvas.SetTop(_activeSelectionRectangle, y);
            _activeSelectionRectangle.Width = width;
            _activeSelectionRectangle.Height = height;
        }

        // Notify selection updated
        TextSelectionUpdated?.Invoke(this, new TextSelectionEventArgs
        {
            PageIndex = _selectionPageIndex,
            StartPoint = _selectionStartPoint,
            EndPoint = point
        });

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer released on a page image to end text selection.
    /// </summary>
    private void OnPagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        var source = e.Source as Control;
        if (source == null)
        {
            return;
        }

        var image = FindParentOfType<Image>(source);
        if (image == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(image).Position;

        // Notify selection ended
        TextSelectionEnded?.Invoke(this, new TextSelectionEventArgs
        {
            PageIndex = _selectionPageIndex,
            StartPoint = _selectionStartPoint,
            EndPoint = point
        });

        _isSelecting = false;
        _selectionPageIndex = -1;

        e.Handled = true;
    }

    /// <summary>
    /// Finds a parent control of a specific type in the visual tree.
    /// </summary>
    private T? FindParentOfType<T>(Control? control) where T : Control
    {
        while (control != null)
        {
            if (control is T result)
            {
                return result;
            }
            control = control.Parent as Control;
        }
        return null;
    }
}

/// <summary>
/// Event args for text selection events.
/// </summary>
public class TextSelectionEventArgs : EventArgs
{
    public int PageIndex { get; set; }
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
}

/// <summary>
/// View model for a page item in continuous scroll mode.
/// </summary>
public class PageItemViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private int _pageNumber;
    private int _pageIndex;
    private bool _isLoading;
    private Bitmap? _pageImage;

    public int PageNumber
    {
        get => _pageNumber;
        set => SetProperty(ref _pageNumber, value);
    }

    public int PageIndex
    {
        get => _pageIndex;
        set => SetProperty(ref _pageIndex, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public Bitmap? PageImage
    {
        get => _pageImage;
        set => SetProperty(ref _pageImage, value);
    }
}
