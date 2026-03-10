using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Text extraction from selection, link detection, and cursor updates.
/// </summary>
public partial class PdfViewerPage
{
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
}
