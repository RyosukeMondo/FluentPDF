using Avalonia.Controls;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Rendering, thumbnail, page operations, and save logic.
/// </summary>
public partial class PdfViewerPage
{
    /// <summary>
    /// Renders a PDF page to an Avalonia bitmap.
    /// Called by PdfViewerViewModel to render the current page.
    /// </summary>
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

                // Generate content for all modified pages so PDFium serializes in-memory objects
                var shapeService = App.GetService<IShapeService>();
                shapeService.FlushDirtyPages(document.FilePath);
                if (shapeService is FluentPDF.Rendering.Services.ShapeService ss)
                    ss.PageCache.EvictAll(docHandle);

                // Save to temp file first, then replace original (avoids file lock conflicts)
                var tempSavePath = targetPath + ".saving.tmp";
                var success = PdfiumInterop.SaveDocument(docHandle, tempSavePath);
                if (success)
                {
                    try
                    {
                        System.IO.File.Copy(tempSavePath, targetPath, overwrite: true);
                        _logger.LogInformation("Document saved to {Path}", targetPath);
                    }
                    catch (Exception exCopy)
                    {
                        _logger.LogError(exCopy, "Failed to replace original with saved file");
                        success = false;
                    }
                    finally
                    {
                        try { System.IO.File.Delete(tempSavePath); } catch { }
                    }
                }
                else
                {
                    _logger.LogError("PDFium SaveDocument failed for {Path}", targetPath);
                    try { System.IO.File.Delete(tempSavePath); } catch { }
                }
                return success;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed");
            return false;
        }
    }
}
