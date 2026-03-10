using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Resize handles: creation, hit-testing, drag computation, and commit.
/// </summary>
public partial class PdfViewerPage
{
    private readonly List<(Rectangle rect, HandlePosition position)> _resizeHandles = new();
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
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        var result = await shapeService.ResizePageObjectAsync(docId, pageIndex, _selectedObject.Index, scaleX, scaleY, anchorPdfX, anchorPdfY);
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
            var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
            var success = await shapeService.ResizePageObjectAsync(
                docId, pageIndex, _selectedObject.Index, scaleX, scaleY, anchorPdfX, anchorPdfY);
            if (success)
            {
                // Refresh and re-select the resized object
                await _viewModel.RefreshCurrentPageSilentAsync();
                var objects = await shapeService.GetPageObjectsAsync(docId, pageIndex);
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
}
