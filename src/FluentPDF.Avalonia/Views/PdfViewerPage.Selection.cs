using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FluentPDF.Avalonia.Helpers;
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
/// Select tool: hit-testing, selection highlight, multi-select, delete, move,
/// marquee/lasso selection, context menu, undo/redo, and select-all.
/// </summary>
public partial class PdfViewerPage
{
    private bool IsOriginalObject(PageObjectInfo obj)
    {
        if (_viewModel == null) return false;
        var pageIndex = _viewModel.CurrentPageNumber - 1;
        if (_viewModel.OriginalObjectCounts.TryGetValue(pageIndex, out var originalCount))
            return obj.Index < originalCount;
        return true; // Assume original if not tracked
    }

    private async Task HandleSelectClickAsync(Point screenPoint, bool shiftHeld = false, bool altHeld = false)
    {
        if (_viewModel?.CurrentDocument == null || PdfImage == null)
            return;

        var pdfCoords = ScreenPointToPdfCoords(screenPoint);
        if (pdfCoords == null)
        {
            ClearSelection();
            return;
        }

        var modifier = SelectionManager.GetModifier(shiftHeld, altHeld);

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

            var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));

            PageObjectInfo? hit = null;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var obj = objects[i];
                if (pdfCoords.Value.pdfX >= obj.Left && pdfCoords.Value.pdfX <= obj.Right &&
                    pdfCoords.Value.pdfY >= obj.Bottom && pdfCoords.Value.pdfY <= obj.Top)
                {
                    // Skip locked original objects entirely
                    if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(obj))
                        continue;
                    hit = obj;
                    break;
                }
            }

            if (hit != null)
            {
                // Check if clicked on already-selected object -> prepare multi-move
                var alreadySelected = _selectedObjects.Any(o => o.Index == hit.Index);

                if (modifier == SelectionModifier.Add || modifier == SelectionModifier.Subtract)
                {
                    if (modifier == SelectionModifier.Subtract)
                    {
                        // Alt+click: remove from selection
                        _selectedObjects.RemoveAll(o => o.Index == hit.Index);
                    }
                    else
                    {
                        // Shift+click: toggle in selection
                        var existing = _selectedObjects.FindIndex(o => o.Index == hit.Index);
                        if (existing >= 0)
                            _selectedObjects.RemoveAt(existing);
                        else
                            _selectedObjects.Add(hit);
                    }
                    _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
                    _dragStartPoint = screenPoint;
                    _originalDragStart = screenPoint;
                    ShowMultiSelectionHighlights();
                    _logger.LogInformation("Multi-select: {Count} objects selected", _selectedObjects.Count);
                }
                else if (alreadySelected && _selectedObjects.Count > 1)
                {
                    // Click on already-selected object with multiple selected -> prepare multi-move
                    _dragStartPoint = screenPoint;
                    _originalDragStart = screenPoint;
                    if (_selectionManager != null)
                    {
                        _selectionManager.StartMultiMove(screenPoint, _selectedObjects);
                        foreach (var h in _multiSelectionHighlights)
                            _selectionManager.RegisterMoveHighlight(h);
                    }
                }
                else
                {
                    // Plain click: single select
                    _selectedObjects.Clear();
                    _selectedObjects.Add(hit);
                    _selectedObject = hit;
                    _dragStartPoint = screenPoint;
                    _originalDragStart = screenPoint;
                    ShowSelectionHighlight(hit);
                    _logger.LogInformation("Selected object #{Index} type={Type}", hit.Index, hit.Type);
                }
            }
            else
            {
                // No hit - start marquee selection on empty space
                if (modifier == SelectionModifier.Replace)
                    ClearSelection();

                if (_selectionManager != null)
                    _selectionManager.StartMarquee(screenPoint, modifier);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select page object");
        }
    }

    private void ShowSelectionHighlight(PageObjectInfo obj)
    {
        ClearSelectionHighlight();
        if (DrawingCanvas == null || PdfImage == null) return;

        var topLeft = PdfCoordsToScreen(obj.Left, obj.Top);
        var bottomRight = PdfCoordsToScreen(obj.Right, obj.Bottom);
        if (topLeft == null || bottomRight == null) return;

        var x = Math.Min(topLeft.Value.X, bottomRight.Value.X);
        var y = Math.Min(topLeft.Value.Y, bottomRight.Value.Y);
        var w = Math.Abs(bottomRight.Value.X - topLeft.Value.X);
        var h = Math.Abs(bottomRight.Value.Y - topLeft.Value.Y);

        var isOrig = IsOriginalObject(obj);
        var strokeColor = isOrig
            ? Color.FromArgb(200, 255, 140, 0)   // Orange for original
            : Color.FromArgb(200, 0, 120, 215);   // Blue for user-added
        var fillColor = isOrig
            ? Color.FromArgb(15, 255, 140, 0)
            : Color.FromArgb(30, 0, 120, 215);

        _selectionHighlight = new Rectangle
        {
            Stroke = new SolidColorBrush(strokeColor),
            StrokeThickness = 2,
            StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 4, 2 },
            Fill = new SolidColorBrush(fillColor),
            Width = w,
            Height = h,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_selectionHighlight, x);
        Canvas.SetTop(_selectionHighlight, y);
        DrawingCanvas.Children.Add(_selectionHighlight);

        // Add resize handles (4 corners)
        AddResizeHandles(x, y, w, h);
    }

    public void ClearSelection()
    {
        _selectedObject = null;
        _selectedObjects.Clear();
        ClearSelectionHighlight();
        ClearMultiSelectionHighlights();
    }

    private void ShowMultiSelectionHighlights()
    {
        ClearSelectionHighlight();
        ClearMultiSelectionHighlights();
        if (DrawingCanvas == null || PdfImage == null) return;

        foreach (var obj in _selectedObjects)
        {
            var topLeft = PdfCoordsToScreen(obj.Left, obj.Top);
            var bottomRight = PdfCoordsToScreen(obj.Right, obj.Bottom);
            if (topLeft == null || bottomRight == null) continue;

            var x = Math.Min(topLeft.Value.X, bottomRight.Value.X);
            var y = Math.Min(topLeft.Value.Y, bottomRight.Value.Y);
            var w = Math.Abs(bottomRight.Value.X - topLeft.Value.X);
            var h = Math.Abs(bottomRight.Value.Y - topLeft.Value.Y);

            var isOrig = IsOriginalObject(obj);
            var strokeColor = isOrig
                ? Color.FromArgb(200, 255, 140, 0)
                : Color.FromArgb(200, 0, 120, 215);
            var fillColor = isOrig
                ? Color.FromArgb(15, 255, 140, 0)
                : Color.FromArgb(30, 0, 120, 215);

            var rect = new Rectangle
            {
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 2,
                StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 4, 2 },
                Fill = new SolidColorBrush(fillColor),
                Width = w,
                Height = h,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            DrawingCanvas.Children.Add(rect);
            _multiSelectionHighlights.Add(rect);
        }
    }

    private void ClearMultiSelectionHighlights()
    {
        if (DrawingCanvas == null) return;
        foreach (var h in _multiSelectionHighlights)
            DrawingCanvas.Children.Remove(h);
        _multiSelectionHighlights.Clear();
    }

    private void ClearSelectionHighlight()
    {
        RemoveResizeHandles();
        if (_selectionHighlight != null && DrawingCanvas != null)
        {
            DrawingCanvas.Children.Remove(_selectionHighlight);
            _selectionHighlight = null;
        }
    }
}
