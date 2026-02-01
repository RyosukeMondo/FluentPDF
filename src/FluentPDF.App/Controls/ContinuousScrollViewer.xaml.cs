// Copyright (c) 2025 FluentPDF. All rights reserved.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.App.Controls;

/// <summary>
/// Continuous scroll viewer for PDF pages with virtual scrolling.
/// Renders only visible pages for performance optimization.
/// </summary>
public sealed partial class ContinuousScrollViewer : UserControl
{
    private readonly ILogger<ContinuousScrollViewer> _logger;
    private readonly IPdfRenderingService _renderingService;
    private PdfDocument? _document;
    private double _zoomLevel = 1.0;
    private int _currentVisiblePage = 1;

    // Text selection state
    private bool _isSelecting;
    private Windows.Foundation.Point _selectionStartPoint;
    private int _selectionPageIndex = -1;
    private Microsoft.UI.Xaml.Shapes.Rectangle? _activeSelectionRectangle;

    // Performance optimization
    private System.Threading.CancellationTokenSource? _scrollDebounceToken;
    private const int ScrollDebounceMs = 100;
    private const int RenderBufferPages = 3; // Render 3 pages above and below viewport

    public ObservableCollection<PageItemViewModel> Pages { get; } = new();

    public ContinuousScrollViewer()
    {
        InitializeComponent();

        // Get services from DI container
        _logger = App.GetService<ILogger<ContinuousScrollViewer>>();
        _renderingService = App.GetService<IPdfRenderingService>();

        PageRepeater.ItemsSource = Pages;

        // Wire up element prepared event to attach pointer handlers
        PageRepeater.ElementPrepared += OnPageElementPrepared;
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

        var targetPage = Pages[pageNumber - 1];
        var container = PageRepeater.TryGetElement(pageNumber - 1);
        if (container != null)
        {
            container.StartBringIntoView(new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.2
            });
        }
    }

    /// <summary>
    /// Handles scroll position changes to track current page and render visible pages.
    /// Uses debouncing to improve performance during rapid scrolling.
    /// </summary>
    private async void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Update current page indicator immediately for better UX
        UpdateCurrentPageIndicator();

        // Debounce rendering for performance
        if (!e.IsIntermediate)
        {
            // Cancel any pending render operations
            _scrollDebounceToken?.Cancel();
            _scrollDebounceToken = new System.Threading.CancellationTokenSource();
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
    }

    /// <summary>
    /// Renders only the pages currently visible in the viewport.
    /// Virtual scrolling implementation for performance.
    /// </summary>
    private async Task RenderVisiblePagesAsync()
    {
        if (_document == null || Pages.Count == 0)
        {
            return;
        }

        try
        {
            var viewportHeight = MainScrollViewer.ActualHeight;
            var scrollOffset = MainScrollViewer.VerticalOffset;

            // Estimate page height (approximate, will vary with zoom)
            var estimatedPageHeight = 900 * _zoomLevel;

            // Calculate visible range with optimized buffer
            var startIndex = Math.Max(0, (int)(scrollOffset / estimatedPageHeight) - RenderBufferPages);
            var endIndex = Math.Min(Pages.Count - 1, (int)((scrollOffset + viewportHeight) / estimatedPageHeight) + RenderBufferPages);

            // Track which pages should be rendered
            var pagesToRender = new System.Collections.Generic.HashSet<int>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                pagesToRender.Add(i);
            }

            // Render visible pages in parallel for better performance
            var renderTasks = new System.Collections.Generic.List<Task>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                var page = Pages[i];
                if (page.PageImage == null && !page.IsLoading)
                {
                    page.IsLoading = true;
                    renderTasks.Add(RenderPageAsync(page).ContinueWith(_ => page.IsLoading = false));
                }
            }

            // Wait for all render tasks to complete
            if (renderTasks.Count > 0)
            {
                await Task.WhenAll(renderTasks);
            }

            // Optional: Clear cached images for pages far from viewport to save memory
            // Commented out for now - can enable if memory becomes an issue
            /*
            for (int i = 0; i < Pages.Count; i++)
            {
                if (!pagesToRender.Contains(i) && Pages[i].PageImage != null)
                {
                    // Page is too far from viewport, clear cache
                    Pages[i].PageImage = null;
                }
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering visible pages in continuous scroll mode");
        }
    }

    /// <summary>
    /// Renders a single page.
    /// </summary>
    private async Task RenderPageAsync(PageItemViewModel pageItem)
    {
        if (_document == null)
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
                App.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    using var stream = result.Value;
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    pageItem.PageImage = bitmap;
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering page {PageNumber}", pageItem.PageNumber);
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

        var scrollOffset = MainScrollViewer.VerticalOffset;
        var estimatedPage = Math.Min(Pages.Count, Math.Max(1, (int)(scrollOffset / 900) + 1));

        if (_currentVisiblePage != estimatedPage)
        {
            _currentVisiblePage = estimatedPage;
            CurrentPageIndicator.Text = $"Page {_currentVisiblePage} of {Pages.Count}";

            // Notify parent that current page changed
            CurrentPageChanged?.Invoke(this, _currentVisiblePage);
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
    /// Handles element prepared event to wire up pointer events for text selection.
    /// </summary>
    private void OnPageElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is Grid grid)
        {
            // Find the Image element in the grid
            var image = FindChildByName<Image>(grid, "PageImage");
            if (image != null)
            {
                // Wire up pointer events for text selection
                image.PointerPressed += OnPagePointerPressed;
                image.PointerMoved += OnPagePointerMoved;
                image.PointerReleased += OnPagePointerReleased;
            }
        }
    }

    /// <summary>
    /// Finds a child element by name in a visual tree.
    /// </summary>
    private T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name)
            {
                return element;
            }

            var result = FindChildByName<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Handles pointer pressed on a page image to begin text selection.
    /// </summary>
    private void OnPagePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is not Image image)
        {
            return;
        }

        var properties = e.GetCurrentPoint(image).Properties;

        // Only start selection on left-click
        if (properties.IsLeftButtonPressed)
        {
            var point = e.GetCurrentPoint(image).Position;
            var pageIndex = (int)(image.Tag ?? -1);

            if (pageIndex >= 0)
            {
                _isSelecting = true;
                _selectionStartPoint = point;
                _selectionPageIndex = pageIndex;

                // Find and show selection rectangle
                var grid = image.Parent as Grid;
                if (grid != null)
                {
                    var canvas = FindChildByName<Canvas>(grid, "SelectionCanvas");
                    if (canvas != null)
                    {
                        _activeSelectionRectangle = FindChildByName<Microsoft.UI.Xaml.Shapes.Rectangle>(canvas, "SelectionRectangle");
                        if (_activeSelectionRectangle != null)
                        {
                            _activeSelectionRectangle.Visibility = Visibility.Visible;
                            _activeSelectionRectangle.Width = 0;
                            _activeSelectionRectangle.Height = 0;
                            Canvas.SetLeft(_activeSelectionRectangle, point.X);
                            Canvas.SetTop(_activeSelectionRectangle, point.Y);
                        }
                    }
                }

                image.CapturePointer(e.Pointer);

                // Notify selection started
                TextSelectionStarted?.Invoke(this, new TextSelectionEventArgs
                {
                    PageIndex = pageIndex,
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
    private void OnPagePointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isSelecting || sender is not Image image)
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
    private void OnPagePointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isSelecting || sender is not Image image)
        {
            return;
        }

        var point = e.GetCurrentPoint(image).Position;

        image.ReleasePointerCapture(e.Pointer);

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
}

/// <summary>
/// Event args for text selection events.
/// </summary>
public class TextSelectionEventArgs : EventArgs
{
    public int PageIndex { get; set; }
    public Windows.Foundation.Point StartPoint { get; set; }
    public Windows.Foundation.Point EndPoint { get; set; }
}

/// <summary>
/// View model for a page item in continuous scroll mode.
/// </summary>
public class PageItemViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private int _pageNumber;
    private int _pageIndex;
    private bool _isLoading;
    private BitmapImage? _pageImage;

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

    public BitmapImage? PageImage
    {
        get => _pageImage;
        set => SetProperty(ref _pageImage, value);
    }
}
