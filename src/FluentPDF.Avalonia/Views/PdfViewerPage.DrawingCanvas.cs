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
        // Handle lasso tool drag
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Lasso && _selectionManager?.IsLassoActive == true && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                _selectionManager.UpdateLasso(pt);
                e.Handled = true;
                return;
            }
        }

        // Handle marquee selection drag (Select tool, started on empty space)
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Select && _selectionManager?.IsMarqueeActive == true && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                _selectionManager.UpdateMarquee(pt);
                e.Handled = true;
                return;
            }
        }

        // Handle multi-object move (Select tool, dragging selected objects)
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Select && _selectionManager?.IsMultiMoving == true && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                _selectionManager.UpdateMultiMove(pt);
                e.Handled = true;
                return;
            }
        }

        // Handle resize drag
        if (_isResizing && _selectionHighlight != null && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetCurrentPoint(PdfImage).Position;
                var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                var newRect = ComputeResizedRect(pt, shiftHeld, altHeld);

                // Update visual preview
                Canvas.SetLeft(_selectionHighlight, newRect.X);
                Canvas.SetTop(_selectionHighlight, newRect.Y);
                _selectionHighlight.Width = newRect.Width;
                _selectionHighlight.Height = newRect.Height;

                // Update handle positions to match new rect
                UpdateResizeHandlePositions(newRect.X, newRect.Y, newRect.Width, newRect.Height);
                e.Handled = true;
                return;
            }
        }

        // Handle single-object drag-to-move for Select tool (legacy single select path)
        if (_viewModel?.ActiveDrawingTool == DrawingTool.Select && _selectedObject != null && _selectedObjects.Count <= 1 && PdfImage != null)
        {
            var props = e.GetCurrentPoint(DrawingCanvas).Properties;
            if (props.IsLeftButtonPressed)
            {
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
                return;
            }
        }

        if (!_isDrawing || _viewModel == null || PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;
        var tool = _viewModel.ActiveDrawingTool;

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

        e.Handled = true;
    }

    private void OnDrawingCanvasPointerReleased(
        object? sender, PointerReleasedEventArgs e)
    {
        if (PdfImage == null) { _isDrawing = false; return; }

        var releasePoint = e.GetCurrentPoint(PdfImage).Position;

        // Finish lasso selection
        if (_selectionManager?.IsLassoActive == true && _viewModel != null)
        {
            _ = FinishLassoSelectionAsync();
            e.Handled = true;
            return;
        }

        // Finish marquee selection
        if (_selectionManager?.IsMarqueeActive == true && _viewModel != null)
        {
            _ = FinishMarqueeSelectionAsync(releasePoint);
            e.Handled = true;
            return;
        }

        // Finish multi-object move
        if (_selectionManager?.IsMultiMoving == true && _viewModel != null)
        {
            var delta = _selectionManager.FinishMultiMove(releasePoint);
            if (delta != null)
                _ = MoveSelectedObjectsAsync(delta.Value.deltaPdfX, delta.Value.deltaPdfY);
            e.Handled = true;
            return;
        }

        // Commit resize
        if (_isResizing && _selectedObject != null && _viewModel != null && _selectionHighlight != null)
        {
            _isResizing = false;
            var origW = _resizeOriginalRect.Width;
            var origH = _resizeOriginalRect.Height;
            var newW = _selectionHighlight.Width;
            var newH = _selectionHighlight.Height;

            if (origW > 0 && origH > 0 && (Math.Abs(newW - origW) > 1 || Math.Abs(newH - origH) > 1))
            {
                var scaleX = (float)(newW / origW);
                var scaleY = (float)(newH / origH);

                // Determine anchor in PDF coords (opposite corner of dragged handle)
                var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                float anchorPdfX, anchorPdfY;

                if (altHeld)
                {
                    // Alt: anchor at center
                    anchorPdfX = (_selectedObject.Left + _selectedObject.Right) / 2;
                    anchorPdfY = (_selectedObject.Bottom + _selectedObject.Top) / 2;
                }
                else
                {
                    // Anchor at opposite corner
                    (anchorPdfX, anchorPdfY) = _activeHandle switch
                    {
                        HandlePosition.TopLeft => (_selectedObject.Right, _selectedObject.Bottom),
                        HandlePosition.TopRight => (_selectedObject.Left, _selectedObject.Bottom),
                        HandlePosition.BottomLeft => (_selectedObject.Right, _selectedObject.Top),
                        HandlePosition.BottomRight => (_selectedObject.Left, _selectedObject.Top),
                        HandlePosition.MiddleLeft => (_selectedObject.Right, (_selectedObject.Bottom + _selectedObject.Top) / 2),
                        HandlePosition.MiddleRight => (_selectedObject.Left, (_selectedObject.Bottom + _selectedObject.Top) / 2),
                        HandlePosition.TopMiddle => ((_selectedObject.Left + _selectedObject.Right) / 2, _selectedObject.Bottom),
                        HandlePosition.BottomMiddle => ((_selectedObject.Left + _selectedObject.Right) / 2, _selectedObject.Top),
                        _ => (_selectedObject.Left, _selectedObject.Bottom)
                    };
                }

                _ = CommitResizeAsync(scaleX, scaleY, anchorPdfX, anchorPdfY);
            }
            e.Handled = true;
            return;
        }

        // Commit single-object drag-to-move for Select tool
        if (_isDraggingSelection && _selectedObject != null && _viewModel != null)
        {
            _isDraggingSelection = false;
            if (_selectedObject != null)
            {
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
            }
            e.Handled = true;
            return;
        }

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

            bool success = false;

            switch (tool)
            {
                case DrawingTool.Rectangle:
                    var rx = Math.Min(pdfStart.Value.X, pdfEnd.Value.X);
                    var ry = Math.Min(pdfStart.Value.Y, pdfEnd.Value.Y);
                    var rw = Math.Abs(pdfEnd.Value.X - pdfStart.Value.X);
                    var rh = Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y);
                    if (rw > 1 && rh > 1)
                        success = await shapeService.AddRectangleAsync(
                            docId, pageIndex, rx, ry, rw, rh,
                            fillColor, strokeColor, strokeWidth) != null;
                    break;

                case DrawingTool.Circle:
                    var cx = (pdfStart.Value.X + pdfEnd.Value.X) / 2;
                    var cy = (pdfStart.Value.Y + pdfEnd.Value.Y) / 2;
                    var radX = Math.Abs(pdfEnd.Value.X - pdfStart.Value.X) / 2;
                    var radY = Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y) / 2;
                    var radius = Math.Max(radX, radY);
                    if (radius > 1)
                        success = await shapeService.AddCircleAsync(
                            docId, pageIndex, cx, cy, radius,
                            fillColor, strokeColor, strokeWidth) != null;
                    break;

                case DrawingTool.Line:
                    success = await shapeService.AddLineAsync(
                        docId, pageIndex,
                        pdfStart.Value.X, pdfStart.Value.Y,
                        pdfEnd.Value.X, pdfEnd.Value.Y,
                        strokeColor, strokeWidth) != null;
                    break;

                case DrawingTool.Freehand:
                    if (_drawingPoints.Count >= 2)
                    {
                        var pdfPts = ConvertPointsToPdfArray(_drawingPoints);
                        if (pdfPts != null && pdfPts.Length >= 4)
                            success = await shapeService.AddFreehandPathAsync(
                                docId, pageIndex, pdfPts,
                                strokeColor, strokeWidth) != null;
                    }
                    break;

                case DrawingTool.Text:
                    success = await shapeService.AddTextAsync(
                        docId, pageIndex,
                        pdfStart.Value.X, pdfStart.Value.Y,
                        "Text", 12f, "Helvetica", strokeColor) != null;
                    break;
            }

            if (success)
            {
                _logger.LogInformation("Shape {Tool} committed on page {Page}", tool, pageIndex);

                // Record undo action
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
                    X = pdfStart.Value.X,
                    Y = pdfStart.Value.Y,
                    X2 = pdfEnd.Value.X,
                    Y2 = pdfEnd.Value.Y,
                    Width = Math.Abs(pdfEnd.Value.X - pdfStart.Value.X),
                    Height = Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y),
                    Radius = Math.Max(Math.Abs(pdfEnd.Value.X - pdfStart.Value.X),
                                      Math.Abs(pdfEnd.Value.Y - pdfStart.Value.Y)) / 2,
                    FillColor = fillColor,
                    StrokeColor = strokeColor,
                    StrokeWidth = strokeWidth,
                    Points = tool == DrawingTool.Freehand ? ConvertPointsToPdfArray(_drawingPoints) : null,
                    Text = tool == DrawingTool.Text ? "Text" : null,
                };
                undoService.Push(new AddShapeAction(docId, pageIndex, creationData));

                // Silent refresh: re-render without showing loading overlay to avoid flicker
                await _viewModel.RefreshCurrentPageSilentAsync();
            }
            else
            {
                _logger.LogWarning("Shape {Tool} failed on page {Page} (docId={DocId})",
                    tool, pageIndex, docId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit drawing shape");
        }
        finally
        {
            // Remove drawing preview now that re-render is complete
            if (_drawingPreview != null && DrawingCanvas != null)
            {
                DrawingCanvas.Children.Remove(_drawingPreview);
                _drawingPreview = null;
            }
        }
    }
}
