using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Public automation API: methods exposed for REST API endpoints and testing.
/// </summary>
public partial class PdfViewerPage
{
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
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));

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
            _selectedObjects.Clear();
            _selectedObjects.Add(hit);
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
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        var objIndex = _selectedObject.Index;

        try
        {
            var success = await shapeService.RemovePageObjectAsync(docId, pageIndex, objIndex);
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
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
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
                    success = await shapeService.AddRectangleAsync(docId, pageIndex, rx, ry, rw, rh, fillColor, strokeColor, strokeWidth) != null;
                break;
            case DrawingTool.Circle:
                var cx = (startX + endX) / 2;
                var cy = (startY + endY) / 2;
                var radius = Math.Max(Math.Abs(endX - startX), Math.Abs(endY - startY)) / 2;
                if (radius > 1)
                    success = await shapeService.AddCircleAsync(docId, pageIndex, cx, cy, radius, fillColor, strokeColor, strokeWidth) != null;
                break;
            case DrawingTool.Line:
                success = await shapeService.AddLineAsync(docId, pageIndex, startX, startY, endX, endY, strokeColor, strokeWidth) != null;
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
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        var color = _viewModel.DrawingStrokeColor;

        var id = await shapeService.AddTextAsync(docId, pageIndex, pdfX, pdfY, text, fontSize, fontName, color);
        if (id != null)
            await _viewModel.RefreshCurrentPageAsync();
        return id != null;
    }

    /// <summary>List all page objects on the current page.</summary>
    public async Task<List<PageObjectInfo>> GetCurrentPageObjectsAsync()
    {
        if (_viewModel?.CurrentDocument == null) return new List<PageObjectInfo>();

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        return await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));
    }

    /// <summary>Hit-test at PDF coordinates without selecting.</summary>
    public async Task<PageObjectInfo?> HitTestAtPdfCoordsAsync(float pdfX, float pdfY)
    {
        if (_viewModel?.CurrentDocument == null) return null;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            var obj = objects[i];
            if (pdfX >= obj.Left && pdfX <= obj.Right &&
                pdfY >= obj.Bottom && pdfY <= obj.Top)
                return obj;
        }
        return null;
    }
}
