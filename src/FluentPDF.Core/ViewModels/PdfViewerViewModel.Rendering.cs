using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Rendering, page dimension updates, and side-panel loading for PdfViewerViewModel.
/// </summary>
public partial class PdfViewerViewModel
{
    private async Task RenderCurrentPageAsync()
    {
        if (_currentDocument == null || RenderPageCallback == null) return;

        IsLoading = true;
        StatusMessage = $"Rendering page {CurrentPageNumber}...";

        try
        {
            double effectiveDpi = 96.0;
            if (_dpiDetectionService != null && CurrentDisplayInfo != null)
            {
                var dpiResult = _dpiDetectionService.CalculateEffectiveDpi(
                    CurrentDisplayInfo, ZoomLevel, CurrentRenderingQuality);
                if (dpiResult.IsSuccess) effectiveDpi = dpiResult.Value;
            }

            var imageSource = await RenderPageCallback(
                _currentDocument, CurrentPageNumber, ZoomLevel, effectiveDpi);

            if (imageSource != null)
            {
                CurrentPageImage = imageSource;
                StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";
                _lastRenderedDpi = effectiveDpi;
                _metricsService?.RecordRenderTime(CurrentPageNumber, 0);
                UpdatePageDimensions();
                ExtractPageSummaryAsync(_currentDocument, CurrentPageNumber);
            }
            else
            {
                StatusMessage = "Failed to render page";
                await ShowErrorAsync("Rendering Error", "Failed to render page.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering page");
            StatusMessage = "Unexpected error while rendering page";
            await ShowErrorAsync("Error", $"Failed to render page: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Renders the current page without showing IsLoading overlay (no flicker).
    /// </summary>
    private async Task RenderCurrentPageSilentAsync()
    {
        if (_currentDocument == null || RenderPageCallback == null) return;

        try
        {
            double effectiveDpi = 96.0;
            if (_dpiDetectionService != null && CurrentDisplayInfo != null)
            {
                var dpiResult = _dpiDetectionService.CalculateEffectiveDpi(
                    CurrentDisplayInfo, ZoomLevel, CurrentRenderingQuality);
                if (dpiResult.IsSuccess) effectiveDpi = dpiResult.Value;
            }

            var imageSource = await RenderPageCallback(
                _currentDocument, CurrentPageNumber, ZoomLevel, effectiveDpi);

            if (imageSource != null)
            {
                CurrentPageImage = imageSource;
                StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";
                _lastRenderedDpi = effectiveDpi;
                UpdatePageDimensions();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during silent page render");
        }
    }

    private void UpdatePageDimensions()
    {
        if (_currentDocument == null) return;
        var pageSizeResult = _renderingService.GetPageSize(_currentDocument, CurrentPageNumber);
        if (pageSizeResult.IsSuccess)
        {
            CurrentPageWidth = pageSizeResult.Value.Width;
            CurrentPageHeight = pageSizeResult.Value.Height;
        }
    }

    private async Task LoadSidePanelsAsync()
    {
        if (Thumbnails != null && _currentDocument != null)
        {
            try { await Thumbnails.LoadThumbnailsAsync(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load thumbnails"); }
        }
        if (Bookmarks != null && _currentDocument != null)
        {
            try { await Bookmarks.LoadBookmarksCommand.ExecuteAsync(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load bookmarks"); }
        }
        if (AnnotationsList != null && _currentDocument != null)
        {
            try { await AnnotationsList.LoadAnnotationsCommand.ExecuteAsync(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load annotations list"); }
        }
        if (Metadata != null && _currentDocument != null)
        {
            try { Metadata.UpdateFromDocument(_currentDocument); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load metadata"); }
        }

        // Load user bookmarks for the current document
        try { RefreshUserBookmarks(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to load user bookmarks"); }
    }
}
