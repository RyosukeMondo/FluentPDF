// Copyright (c) 2025 FluentPDF. All rights reserved.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    public ObservableCollection<PageItemViewModel> Pages { get; } = new();

    public ContinuousScrollViewer()
    {
        InitializeComponent();

        // Get services from DI container
        _logger = App.GetService<ILogger<ContinuousScrollViewer>>();
        _renderingService = App.GetService<IPdfRenderingService>();

        PageRepeater.ItemsSource = Pages;
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
    /// </summary>
    private async void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!e.IsIntermediate)
        {
            await RenderVisiblePagesAsync();
            UpdateCurrentPageIndicator();
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

            // Calculate visible range with buffer (render 2 pages above and below viewport)
            var startIndex = Math.Max(0, (int)(scrollOffset / 900) - 2);
            var endIndex = Math.Min(Pages.Count - 1, (int)((scrollOffset + viewportHeight) / 900) + 2);

            for (int i = startIndex; i <= endIndex; i++)
            {
                var page = Pages[i];
                if (page.PageImage == null && !page.IsLoading)
                {
                    page.IsLoading = true;
                    await RenderPageAsync(page);
                    page.IsLoading = false;
                }
            }
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
                await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
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
}

/// <summary>
/// View model for a page item in continuous scroll mode.
/// </summary>
public class PageItemViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
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
