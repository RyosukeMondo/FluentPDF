// Copyright (c) 2025 FluentPDF. All rights reserved.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FluentPDF.App.Controls;

/// <summary>
/// Two-page viewer for displaying PDF pages side-by-side like a book.
/// Odd pages on the right, even pages on the left.
/// </summary>
public sealed partial class TwoPageViewer : UserControl
{
    private readonly ILogger<TwoPageViewer> _logger;
    private readonly IPdfRenderingService _renderingService;
    private PdfDocument? _document;
    private double _zoomLevel = 1.0;
    private int _currentPage = 1;

    public TwoPageViewer()
    {
        InitializeComponent();

        // Get services from DI container
        _logger = App.GetService<ILogger<TwoPageViewer>>();
        _renderingService = App.GetService<IPdfRenderingService>();
    }

    /// <summary>
    /// Loads a PDF document and displays the first page(s).
    /// </summary>
    public async Task LoadDocumentAsync(PdfDocument document, int pageNumber, double zoomLevel)
    {
        _document = document;
        _zoomLevel = zoomLevel;
        _currentPage = pageNumber;

        await RenderCurrentPagesAsync();
    }

    /// <summary>
    /// Updates the zoom level and re-renders current pages.
    /// </summary>
    public async Task UpdateZoomAsync(double zoomLevel)
    {
        if (Math.Abs(_zoomLevel - zoomLevel) < 0.001)
        {
            return;
        }

        _zoomLevel = zoomLevel;
        await RenderCurrentPagesAsync();
    }

    /// <summary>
    /// Navigates to a specific page pair.
    /// </summary>
    public async Task GoToPageAsync(int pageNumber)
    {
        if (_document == null || pageNumber < 1 || pageNumber > _document.PageCount)
        {
            return;
        }

        _currentPage = pageNumber;
        await RenderCurrentPagesAsync();
    }

    /// <summary>
    /// Renders the current page pair (left and right pages).
    /// </summary>
    private async Task RenderCurrentPagesAsync()
    {
        if (_document == null)
        {
            return;
        }

        try
        {
            // Determine left and right page numbers
            // Odd pages go on the right, even pages go on the left
            int leftPageNumber = 0;
            int rightPageNumber = 0;

            if (_currentPage % 2 == 1)
            {
                // Current page is odd - show on right
                rightPageNumber = _currentPage;
                leftPageNumber = _currentPage > 1 ? _currentPage - 1 : 0;
            }
            else
            {
                // Current page is even - show on left
                leftPageNumber = _currentPage;
                rightPageNumber = _currentPage < _document.PageCount ? _currentPage + 1 : 0;
            }

            // Render left page
            if (leftPageNumber > 0)
            {
                await RenderPageAsync(leftPageNumber, true);
                LeftPageBorder.Visibility = Visibility.Visible;
                LeftPageNumber.Text = leftPageNumber.ToString();
            }
            else
            {
                LeftPageBorder.Visibility = Visibility.Collapsed;
            }

            // Render right page
            if (rightPageNumber > 0)
            {
                await RenderPageAsync(rightPageNumber, false);
                RightPageBorder.Visibility = Visibility.Visible;
                RightPageNumber.Text = rightPageNumber.ToString();
            }
            else
            {
                RightPageBorder.Visibility = Visibility.Collapsed;
            }

            // Update page range indicator
            UpdatePageRangeIndicator(leftPageNumber, rightPageNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering pages in two-page mode");
        }
    }

    /// <summary>
    /// Renders a single page to either left or right position.
    /// </summary>
    private async Task RenderPageAsync(int pageNumber, bool isLeftPage)
    {
        if (_document == null)
        {
            return;
        }

        try
        {
            // Show loading indicator
            if (isLeftPage)
            {
                LeftPageLoader.Visibility = Visibility.Visible;
            }
            else
            {
                RightPageLoader.Visibility = Visibility.Visible;
            }

            var result = await _renderingService.RenderPageAsync(
                _document,
                pageNumber,
                _zoomLevel);

            if (result.IsSuccess && result.Value != null)
            {
                await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
                {
                    using var stream = result.Value;
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

                    if (isLeftPage)
                    {
                        LeftPageImage.Source = bitmap;
                    }
                    else
                    {
                        RightPageImage.Source = bitmap;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering page {PageNumber}", pageNumber);
        }
        finally
        {
            // Hide loading indicator
            if (isLeftPage)
            {
                LeftPageLoader.Visibility = Visibility.Collapsed;
            }
            else
            {
                RightPageLoader.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// Updates the page range indicator text.
    /// </summary>
    private void UpdatePageRangeIndicator(int leftPage, int rightPage)
    {
        if (_document == null)
        {
            return;
        }

        if (leftPage > 0 && rightPage > 0)
        {
            PageRangeIndicator.Text = $"Pages {leftPage}-{rightPage} of {_document.PageCount}";
        }
        else if (rightPage > 0)
        {
            PageRangeIndicator.Text = $"Page {rightPage} of {_document.PageCount}";
        }
        else if (leftPage > 0)
        {
            PageRangeIndicator.Text = $"Page {leftPage} of {_document.PageCount}";
        }
    }

    /// <summary>
    /// Gets the current page number being displayed.
    /// </summary>
    public int CurrentPage => _currentPage;
}
