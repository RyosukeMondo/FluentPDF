using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FluentPDF.Avalonia.Helpers;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Drawing canvas pointer-moved, pointer-released handlers, and shape commit logic.
/// </summary>
public partial class PdfViewerPage
{
    private void OnDrawingCanvasPointerMoved(
        object? sender, PointerEventArgs e)
    {
        if (HandleLassoDrag(e)) return;
        if (HandleMarqueeDrag(e)) return;
        if (HandleMultiMoveDrag(e)) return;
        if (HandleResizeDrag(e)) return;
        if (HandleSingleObjectDrag(e)) return;

        if (!_isDrawing || _viewModel == null || PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        UpdateDrawingPreview(point);
        e.Handled = true;
    }

    private void OnDrawingCanvasPointerReleased(
        object? sender, PointerReleasedEventArgs e)
    {
        if (PdfImage == null) { _isDrawing = false; return; }

        var releasePoint = e.GetCurrentPoint(PdfImage).Position;

        if (HandleLassoRelease(e)) return;
        if (HandleMarqueeRelease(e, releasePoint)) return;
        if (HandleMultiMoveRelease(e, releasePoint)) return;
        if (HandleResizeRelease(e)) return;
        if (HandleSingleObjectMoveRelease(e)) return;

        if (!_isDrawing || _viewModel == null || PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        _isDrawing = false;

        // Keep preview visible until re-render completes to avoid flicker
        _ = CommitDrawingAsync(point);
        e.Handled = true;
    }

    private async Task CommitDrawingAsync(Point endPoint)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return;

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var tool = _viewModel.ActiveDrawingTool;
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
            var strokeColor = _viewModel.DrawingStrokeColor;
            var fillColor = _viewModel.DrawingFillColor;
            var strokeWidth = _viewModel.DrawingStrokeWidth;

            var (pdfStart, pdfEnd) = ConvertScreenToPdfPoint(
                _drawStartPoint, endPoint);
            if (pdfStart == null || pdfEnd == null)
                return;

            var start = pdfStart.Value;
            var end = pdfEnd.Value;

            bool success = await CommitShapeAsync(
                shapeService, tool, docId, pageIndex,
                start, end,
                fillColor, strokeColor, strokeWidth);

            if (success)
            {
                _logger.LogInformation(
                    "Shape {Tool} committed on page {Page}", tool, pageIndex);
                RecordUndoForShape(
                    tool, docId, pageIndex,
                    start, end,
                    fillColor, strokeColor, strokeWidth);
                await _viewModel.RefreshCurrentPageSilentAsync();
            }
            else
            {
                _logger.LogWarning(
                    "Shape {Tool} failed on page {Page} (docId={DocId})",
                    tool, pageIndex, docId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit drawing shape");
        }
        finally
        {
            if (_drawingPreview != null && DrawingCanvas != null)
            {
                DrawingCanvas.Children.Remove(_drawingPreview);
                _drawingPreview = null;
            }
        }
    }

    // ── PointerMoved helpers ─────────────────────────────────────────

    private bool HandleLassoDrag(PointerEventArgs e)
    {
        if (_viewModel?.ActiveDrawingTool != DrawingTool.Lasso
            || _selectionManager?.IsLassoActive != true
            || PdfImage == null)
            return false;

        var props = e.GetCurrentPoint(DrawingCanvas).Properties;
        if (!props.IsLeftButtonPressed) return false;

        _selectionManager.UpdateLasso(e.GetCurrentPoint(PdfImage).Position);
        e.Handled = true;
        return true;
    }

    private bool HandleMarqueeDrag(PointerEventArgs e)
    {
        if (_viewModel?.ActiveDrawingTool != DrawingTool.Select
            || _selectionManager?.IsMarqueeActive != true
            || PdfImage == null)
            return false;

        var props = e.GetCurrentPoint(DrawingCanvas).Properties;
        if (!props.IsLeftButtonPressed) return false;

        _selectionManager.UpdateMarquee(e.GetCurrentPoint(PdfImage).Position);
        e.Handled = true;
        return true;
    }

    private bool HandleMultiMoveDrag(PointerEventArgs e)
    {
        if (_viewModel?.ActiveDrawingTool != DrawingTool.Select
            || _selectionManager?.IsMultiMoving != true
            || PdfImage == null)
            return false;

        var props = e.GetCurrentPoint(DrawingCanvas).Properties;
        if (!props.IsLeftButtonPressed) return false;

        _selectionManager.UpdateMultiMove(e.GetCurrentPoint(PdfImage).Position);
        e.Handled = true;
        return true;
    }

    private bool HandleResizeDrag(PointerEventArgs e)
    {
        if (!_isResizing || _selectionHighlight == null || PdfImage == null)
            return false;

        var props = e.GetCurrentPoint(DrawingCanvas).Properties;
        if (!props.IsLeftButtonPressed) return false;

        var pt = e.GetCurrentPoint(PdfImage).Position;
        var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        var newRect = ComputeResizedRect(pt, shiftHeld, altHeld);

        Canvas.SetLeft(_selectionHighlight, newRect.X);
        Canvas.SetTop(_selectionHighlight, newRect.Y);
        _selectionHighlight.Width = newRect.Width;
        _selectionHighlight.Height = newRect.Height;
        UpdateResizeHandlePositions(newRect.X, newRect.Y, newRect.Width, newRect.Height);

        e.Handled = true;
        return true;
    }

    private bool HandleSingleObjectDrag(PointerEventArgs e)
    {
        if (_viewModel?.ActiveDrawingTool != DrawingTool.Select
            || _selectedObject == null
            || _selectedObjects.Count > 1
            || PdfImage == null)
            return false;

        var props = e.GetCurrentPoint(DrawingCanvas).Properties;
        if (!props.IsLeftButtonPressed) return false;

        var dragPoint = e.GetCurrentPoint(PdfImage).Position;
        var dx = dragPoint.X - _dragStartPoint.X;
        var dy = dragPoint.Y - _dragStartPoint.Y;
        if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
            _isDraggingSelection = true;

        if (_isDraggingSelection && _selectionHighlight != null)
        {
            Canvas.SetLeft(_selectionHighlight, Canvas.GetLeft(_selectionHighlight) + dx);
            Canvas.SetTop(_selectionHighlight, Canvas.GetTop(_selectionHighlight) + dy);
            foreach (var (h, _) in _resizeHandles)
            {
                Canvas.SetLeft(h, Canvas.GetLeft(h) + dx);
                Canvas.SetTop(h, Canvas.GetTop(h) + dy);
            }
            _dragStartPoint = dragPoint;
        }

        e.Handled = true;
        return true;
    }

    private void UpdateDrawingPreview(Point point)
    {
        var tool = _viewModel!.ActiveDrawingTool;
        switch (tool)
        {
            case DrawingTool.Rectangle when _drawingPreview is Rectangle rect:
                rect.Width = Math.Abs(point.X - _drawStartPoint.X);
                rect.Height = Math.Abs(point.Y - _drawStartPoint.Y);
                Canvas.SetLeft(rect, Math.Min(_drawStartPoint.X, point.X));
                Canvas.SetTop(rect, Math.Min(_drawStartPoint.Y, point.Y));
                break;

            case DrawingTool.Circle when _drawingPreview is Ellipse ellipse:
                ellipse.Width = Math.Abs(point.X - _drawStartPoint.X);
                ellipse.Height = Math.Abs(point.Y - _drawStartPoint.Y);
                Canvas.SetLeft(ellipse, Math.Min(_drawStartPoint.X, point.X));
                Canvas.SetTop(ellipse, Math.Min(_drawStartPoint.Y, point.Y));
                break;

            case DrawingTool.Line when _drawingPreview is Line line:
                line.EndPoint = point;
                break;

            case DrawingTool.Freehand when _drawingPreview is Polyline pl:
                _drawingPoints.Add(point);
                ((global::Avalonia.Collections.AvaloniaList<Point>)pl.Points).Add(point);
                break;
        }
    }

    // ── PointerReleased helpers ──────────────────────────────────────

    private bool HandleLassoRelease(PointerReleasedEventArgs e)
    {
        if (_selectionManager?.IsLassoActive != true || _viewModel == null)
            return false;

        _ = FinishLassoSelectionAsync();
        e.Handled = true;
        return true;
    }

    private bool HandleMarqueeRelease(
        PointerReleasedEventArgs e, Point releasePoint)
    {
        if (_selectionManager?.IsMarqueeActive != true || _viewModel == null)
            return false;

        _ = FinishMarqueeSelectionAsync(releasePoint);
        e.Handled = true;
        return true;
    }

    private bool HandleMultiMoveRelease(
        PointerReleasedEventArgs e, Point releasePoint)
    {
        if (_selectionManager?.IsMultiMoving != true || _viewModel == null)
            return false;

        var delta = _selectionManager.FinishMultiMove(releasePoint);
        if (delta != null)
            _ = MoveSelectedObjectsAsync(delta.Value.deltaPdfX, delta.Value.deltaPdfY);
        e.Handled = true;
        return true;
    }

    private bool HandleResizeRelease(PointerReleasedEventArgs e)
    {
        if (!_isResizing || _selectedObject == null
            || _viewModel == null || _selectionHighlight == null)
            return false;

        _isResizing = false;
        var origW = _resizeOriginalRect.Width;
        var origH = _resizeOriginalRect.Height;
        var newW = _selectionHighlight.Width;
        var newH = _selectionHighlight.Height;

        if (origW > 0 && origH > 0
            && (Math.Abs(newW - origW) > 1 || Math.Abs(newH - origH) > 1))
        {
            var scaleX = (float)(newW / origW);
            var scaleY = (float)(newH / origH);
            var (anchorX, anchorY) = ComputeResizeAnchor(e);
            _ = CommitResizeAsync(scaleX, scaleY, anchorX, anchorY);
        }

        e.Handled = true;
        return true;
    }

    private (float anchorPdfX, float anchorPdfY) ComputeResizeAnchor(
        PointerReleasedEventArgs e)
    {
        var obj = _selectedObject!;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            return (
                (obj.Left + obj.Right) / 2,
                (obj.Bottom + obj.Top) / 2);
        }

        return _activeHandle switch
        {
            HandlePosition.TopLeft => (obj.Right, obj.Bottom),
            HandlePosition.TopRight => (obj.Left, obj.Bottom),
            HandlePosition.BottomLeft => (obj.Right, obj.Top),
            HandlePosition.BottomRight => (obj.Left, obj.Top),
            HandlePosition.MiddleLeft => (obj.Right, (obj.Bottom + obj.Top) / 2),
            HandlePosition.MiddleRight => (obj.Left, (obj.Bottom + obj.Top) / 2),
            HandlePosition.TopMiddle => ((obj.Left + obj.Right) / 2, obj.Bottom),
            HandlePosition.BottomMiddle => ((obj.Left + obj.Right) / 2, obj.Top),
            _ => (obj.Left, obj.Bottom)
        };
    }

    private bool HandleSingleObjectMoveRelease(PointerReleasedEventArgs e)
    {
        if (!_isDraggingSelection || _selectedObject == null || _viewModel == null)
            return false;

        _isDraggingSelection = false;
        var currentTL = PdfCoordsToScreen(_selectedObject.Left, _selectedObject.Top);
        if (currentTL != null && _selectionHighlight != null)
        {
            var actualX = Canvas.GetLeft(_selectionHighlight);
            var actualY = Canvas.GetTop(_selectionHighlight);
            var screenDx = (float)(actualX - currentTL.Value.X);
            var screenDy = (float)(actualY - currentTL.Value.Y);

            var pdfOrigin = ScreenPointToPdfCoords(new Point(0, 0));
            var pdfDelta = ScreenPointToPdfCoords(new Point(screenDx, screenDy));
            if (pdfOrigin != null && pdfDelta != null)
            {
                var deltaPdfX = pdfDelta.Value.pdfX - pdfOrigin.Value.pdfX;
                var deltaPdfY = pdfDelta.Value.pdfY - pdfOrigin.Value.pdfY;
                _ = MoveSelectedObjectAsync(deltaPdfX, deltaPdfY);
            }
        }

        e.Handled = true;
        return true;
    }

    // ── CommitDrawingAsync helpers ────────────────────────────────────

    private async Task<bool> CommitShapeAsync(
        IShapeService shapeService, DrawingTool tool,
        string docId, PageIndex pageIndex,
        System.Drawing.PointF pdfStart, System.Drawing.PointF pdfEnd,
        string fillColor, string strokeColor, float strokeWidth)
    {
        switch (tool)
        {
            case DrawingTool.Rectangle:
                var rx = Math.Min(pdfStart.X, pdfEnd.X);
                var ry = Math.Min(pdfStart.Y, pdfEnd.Y);
                var rw = Math.Abs(pdfEnd.X - pdfStart.X);
                var rh = Math.Abs(pdfEnd.Y - pdfStart.Y);
                return rw > 1 && rh > 1
                    && await shapeService.AddRectangleAsync(
                        docId, pageIndex, rx, ry, rw, rh,
                        fillColor, strokeColor, strokeWidth) != null;

            case DrawingTool.Circle:
                var cx = (pdfStart.X + pdfEnd.X) / 2;
                var cy = (pdfStart.Y + pdfEnd.Y) / 2;
                var radX = Math.Abs(pdfEnd.X - pdfStart.X) / 2;
                var radY = Math.Abs(pdfEnd.Y - pdfStart.Y) / 2;
                var radius = Math.Max(radX, radY);
                return radius > 1
                    && await shapeService.AddCircleAsync(
                        docId, pageIndex, cx, cy, radius,
                        fillColor, strokeColor, strokeWidth) != null;

            case DrawingTool.Line:
                return await shapeService.AddLineAsync(
                    docId, pageIndex,
                    pdfStart.X, pdfStart.Y,
                    pdfEnd.X, pdfEnd.Y,
                    strokeColor, strokeWidth) != null;

            case DrawingTool.Freehand:
                if (_drawingPoints.Count < 2) return false;
                var pdfPts = ConvertPointsToPdfArray(_drawingPoints);
                return pdfPts != null && pdfPts.Length >= 4
                    && await shapeService.AddFreehandPathAsync(
                        docId, pageIndex, pdfPts,
                        strokeColor, strokeWidth) != null;

            case DrawingTool.Text:
                return await shapeService.AddTextAsync(
                    docId, pageIndex,
                    pdfStart.X, pdfStart.Y,
                    "Text", 12f, "Helvetica", strokeColor) != null;

            default:
                return false;
        }
    }

    private void RecordUndoForShape(
        DrawingTool tool, string docId, PageIndex pageIndex,
        System.Drawing.PointF pdfStart, System.Drawing.PointF pdfEnd,
        string fillColor, string strokeColor, float strokeWidth)
    {
        var undoService = App.GetService<IUndoRedoService>();
        var creationData = new ShapeCreationData
        {
            ShapeType = tool switch
            {
                DrawingTool.Rectangle => DrawingShapeType.Rectangle,
                DrawingTool.Circle => DrawingShapeType.Circle,
                DrawingTool.Line => DrawingShapeType.Line,
                DrawingTool.Freehand => DrawingShapeType.Freehand,
                DrawingTool.Text => DrawingShapeType.Text,
                _ => DrawingShapeType.Rectangle
            },
            X = pdfStart.X,
            Y = pdfStart.Y,
            X2 = pdfEnd.X,
            Y2 = pdfEnd.Y,
            Width = Math.Abs(pdfEnd.X - pdfStart.X),
            Height = Math.Abs(pdfEnd.Y - pdfStart.Y),
            Radius = Math.Max(
                Math.Abs(pdfEnd.X - pdfStart.X),
                Math.Abs(pdfEnd.Y - pdfStart.Y)) / 2,
            FillColor = fillColor,
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth,
            Points = tool == DrawingTool.Freehand
                ? ConvertPointsToPdfArray(_drawingPoints) : null,
            Text = tool == DrawingTool.Text ? "Text" : null,
        };
        undoService.Push(new AddShapeAction(docId, pageIndex, creationData));
    }
}
