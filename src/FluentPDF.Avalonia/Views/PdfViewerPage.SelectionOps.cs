using Avalonia;
using Avalonia.Controls;
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
/// Selection operations: delete, move, marquee/lasso finish, context menu,
/// property changes, undo/redo, and select-all.
/// </summary>
public partial class PdfViewerPage
{
    private async Task DeleteSelectedObjectsAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var toDelete = _selectedObjects.Count > 0
            ? _selectedObjects.ToList()
            : (_selectedObject != null ? new List<PageObjectInfo> { _selectedObject } : new List<PageObjectInfo>());

        if (toDelete.Count == 0) return;

        // Check lock for all
        foreach (var obj in toDelete)
        {
            if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(obj))
            {
                _logger.LogInformation("Cannot delete locked original object #{Index}", obj.Index);
                _viewModel.StatusMessage = "Some original PDF objects are locked. Unlock to delete.";
                return;
            }
        }

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var undoService = App.GetService<IUndoRedoService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

            // Delete in reverse index order to avoid index shifting
            var sorted = toDelete.OrderByDescending(o => o.Index).ToList();
            int deletedCount = 0;

            foreach (var obj in sorted)
            {
                // Get properties before deletion for undo
                var props = await shapeService.GetPageObjectPropertiesAsync(docId, pageIndex, obj.Index);

                var success = await shapeService.RemovePageObjectAsync(docId, pageIndex, obj.Index);
                if (success)
                {
                    deletedCount++;

                    // Push undo action with shape data for recreation
                    undoService.Push(new DeleteShapeAction(docId, pageIndex,
                        new ShapeCreationData
                        {
                            ShapeType = DrawingShapeType.Rectangle, // Generic - we store bounds
                            X = obj.Left,
                            Y = obj.Bottom,
                            Width = obj.Right - obj.Left,
                            Height = obj.Top - obj.Bottom,
                            StrokeColor = props?.StrokeColor ?? "#000000",
                            FillColor = props?.FillColor ?? "#00000000",
                            StrokeWidth = props?.StrokeWidth ?? 2f
                        }));
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} page objects", deletedCount);
                ClearSelection();
                await _viewModel.RefreshCurrentPageAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete selected objects");
        }
    }

    private async Task DeleteSelectedObjectAsync()
    {
        await DeleteSelectedObjectsAsync();
    }

    private async Task MoveSelectedObjectAsync(float deltaPdfX, float deltaPdfY)
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null)
            return;

        if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(_selectedObject))
        {
            _viewModel.StatusMessage = "Original PDF objects are locked. Unlock to move.";
            return;
        }

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

            var success = await shapeService.MovePageObjectAsync(
                docId, pageIndex, _selectedObject.Index, deltaPdfX, deltaPdfY);
            if (success)
            {
                // Record undo
                var undoService = App.GetService<IUndoRedoService>();
                undoService.Push(new MoveShapeAction(docId, pageIndex, _selectedObject.Index, deltaPdfX, deltaPdfY));

                // Update selected object bounds
                _selectedObject = _selectedObject with
                {
                    Left = _selectedObject.Left + deltaPdfX,
                    Right = _selectedObject.Right + deltaPdfX,
                    Bottom = _selectedObject.Bottom + deltaPdfY,
                    Top = _selectedObject.Top + deltaPdfY
                };
                ShowSelectionHighlight(_selectedObject);
                await _viewModel.RefreshCurrentPageSilentAsync();
            }
            else
            {
                _viewModel.StatusMessage = "Failed to move object. It may have been invalidated.";
                ClearSelectionHighlight();
                _selectedObject = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move selected object");
        }
    }

    private async Task MoveSelectedObjectsAsync(float deltaPdfX, float deltaPdfY)
    {
        if (_selectedObjects.Count == 0 || _viewModel?.CurrentDocument == null)
            return;

        foreach (var obj in _selectedObjects)
        {
            if (_viewModel.IsOriginalObjectsLocked && IsOriginalObject(obj))
            {
                _viewModel.StatusMessage = "Some original objects are locked. Unlock to move.";
                return;
            }
        }

        try
        {
            var shapeService = App.GetService<IShapeService>();
            var undoService = App.GetService<IUndoRedoService>();
            var docId = _viewModel.CurrentDocument.FilePath;
            var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

            // Batch move: single GenerateContent call to prevent index shifting and text corruption
            var indices = _selectedObjects.Select(o => o.Index).ToArray();
            int movedCount = await shapeService.MovePageObjectsBatchAsync(
                docId, pageIndex, indices, deltaPdfX, deltaPdfY);

            if (movedCount > 0)
            {
                for (int i = 0; i < _selectedObjects.Count; i++)
                {
                    var obj = _selectedObjects[i];
                    undoService.Push(new MoveShapeAction(docId, pageIndex, obj.Index, deltaPdfX, deltaPdfY));
                    _selectedObjects[i] = obj with
                    {
                        Left = obj.Left + deltaPdfX,
                        Right = obj.Right + deltaPdfX,
                        Bottom = obj.Bottom + deltaPdfY,
                        Top = obj.Top + deltaPdfY
                    };
                }
            }

            _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;

            if (movedCount > 0)
            {
                ShowMultiSelectionHighlights();
                await _viewModel.RefreshCurrentPageSilentAsync();
                _logger.LogInformation("Moved {Count} objects", movedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move selected objects");
        }
    }

    private async Task FinishMarqueeSelectionAsync(Point endPoint)
    {
        if (_selectionManager == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));

        var result = _selectionManager.FinishMarquee(endPoint, objects, _selectedObjects.ToList());
        ApplySelectionResult(result);
    }

    private async Task FinishLassoSelectionAsync()
    {
        if (_selectionManager == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));

        var result = _selectionManager.FinishLasso(objects, _selectedObjects.ToList());
        ApplySelectionResult(result);
    }

    private void ApplySelectionResult(MarqueeResult result)
    {
        _selectedObjects.Clear();
        var filtered = _viewModel?.IsOriginalObjectsLocked == true
            ? result.SelectedObjects.Where(o => !IsOriginalObject(o)).ToList()
            : result.SelectedObjects;
        _selectedObjects.AddRange(filtered);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;

        if (_selectedObjects.Count == 1)
            ShowSelectionHighlight(_selectedObjects[0]);
        else if (_selectedObjects.Count > 1)
            ShowMultiSelectionHighlights();
        else
            ClearSelection();

        _logger.LogInformation("Selection: {Count} objects", _selectedObjects.Count);
    }

    /// <summary>Gets the list of currently selected objects.</summary>
    public List<PageObjectInfo> GetSelectedObjects() => _selectedObjects.ToList();

    /// <summary>Gets/sets the selection containment mode.</summary>
    public SelectionContainment ContainmentMode
    {
        get => _selectionManager?.ContainmentMode ?? SelectionContainment.Intersect;
        set { if (_selectionManager != null) _selectionManager.ContainmentMode = value; }
    }

    private void ShowShapeContextMenu(Point screenPoint)
    {
        if (_selectedObject == null || _viewModel == null || DrawingCanvas == null) return;

        var isOrig = IsOriginalObject(_selectedObject);
        var isLocked = _viewModel.IsOriginalObjectsLocked && isOrig;

        var menu = new global::Avalonia.Controls.ContextMenu();

        var deleteItem = new global::Avalonia.Controls.MenuItem { Header = "Delete" };
        deleteItem.IsEnabled = !isLocked;
        deleteItem.Click += (s, e) => _ = DeleteSelectedObjectsAsync();
        menu.Items.Add(deleteItem);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        var strokeRedItem = new global::Avalonia.Controls.MenuItem { Header = "Stroke: Red" };
        strokeRedItem.IsEnabled = !isLocked;
        strokeRedItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("stroke", "#FF0000");
        menu.Items.Add(strokeRedItem);

        var strokeBlueItem = new global::Avalonia.Controls.MenuItem { Header = "Stroke: Blue" };
        strokeBlueItem.IsEnabled = !isLocked;
        strokeBlueItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("stroke", "#0000FF");
        menu.Items.Add(strokeBlueItem);

        var strokeBlackItem = new global::Avalonia.Controls.MenuItem { Header = "Stroke: Black" };
        strokeBlackItem.IsEnabled = !isLocked;
        strokeBlackItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("stroke", "#000000");
        menu.Items.Add(strokeBlackItem);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        var fillRedItem = new global::Avalonia.Controls.MenuItem { Header = "Fill: Red" };
        fillRedItem.IsEnabled = !isLocked;
        fillRedItem.Click += (s, e) => _ = ChangeSelectedPropertyAsync("fill", "#FF000080");
        menu.Items.Add(fillRedItem);

        var fillTransparent = new global::Avalonia.Controls.MenuItem { Header = "Fill: Transparent" };
        fillTransparent.IsEnabled = !isLocked;
        fillTransparent.Click += (s, e) => _ = ChangeSelectedPropertyAsync("fill", "#00000000");
        menu.Items.Add(fillTransparent);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        var propsItem = new global::Avalonia.Controls.MenuItem { Header = "Properties..." };
        propsItem.Click += (s, e) => _ = ShowPropertiesAsync();
        menu.Items.Add(propsItem);

        menu.Open(DrawingCanvas);
    }

    private async Task ChangeSelectedPropertyAsync(string property, string value)
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

        bool success = property switch
        {
            "stroke" => await shapeService.SetPageObjectStrokeColorAsync(docId, pageIndex, _selectedObject.Index, value),
            "fill" => await shapeService.SetPageObjectFillColorAsync(docId, pageIndex, _selectedObject.Index, value),
            _ => false
        };

        if (success)
            await _viewModel.RefreshCurrentPageAsync();
    }

    private async Task ShowPropertiesAsync()
    {
        if (_selectedObject == null || _viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);
        var props = await shapeService.GetPageObjectPropertiesAsync(docId, pageIndex, _selectedObject.Index);

        if (props != null)
        {
            _viewModel.StatusMessage = $"Object #{_selectedObject.Index}: " +
                $"Type={_selectedObject.Type}, Stroke={props.StrokeColor}, Fill={props.FillColor}, Width={props.StrokeWidth:F1}";
        }
    }

    private async Task UndoAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var undoService = App.GetService<IUndoRedoService>();
        var action = undoService.Undo();
        if (action == null) return;

        var shapeService = App.GetService<IShapeService>();

        switch (action)
        {
            case AddShapeAction addAction:
                // Undo add = remove last object on the page
                var objects = await shapeService.GetPageObjectsAsync(addAction.DocumentId, addAction.PageIndex);
                if (objects.Count > 0)
                {
                    await shapeService.RemovePageObjectAsync(addAction.DocumentId, addAction.PageIndex, objects[^1].Index);
                }
                break;

            case DeleteShapeAction deleteAction:
                // Undo delete = re-create the shape
                var d = deleteAction.CreationData;
                await shapeService.AddRectangleAsync(
                    deleteAction.DocumentId, deleteAction.PageIndex,
                    d.X, d.Y, d.Width, d.Height,
                    d.FillColor, d.StrokeColor, d.StrokeWidth);
                break;

            case MoveShapeAction moveAction:
                // Undo move = move back
                await shapeService.MovePageObjectAsync(
                    moveAction.DocumentId, moveAction.PageIndex,
                    moveAction.ObjectIndex, -moveAction.DeltaX, -moveAction.DeltaY);
                break;
        }

        ClearSelection();
        await _viewModel.RefreshCurrentPageAsync();
        _logger.LogInformation("Undo: {ActionType}", action.ActionType);
    }

    private async Task RedoAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var undoService = App.GetService<IUndoRedoService>();
        var action = undoService.Redo();
        if (action == null) return;

        var shapeService = App.GetService<IShapeService>();

        switch (action)
        {
            case AddShapeAction addAction:
                // Redo add = re-create the shape
                var d = addAction.CreationData;
                switch (d.ShapeType)
                {
                    case DrawingShapeType.Rectangle:
                        var rx = Math.Min(d.X, d.X2);
                        var ry = Math.Min(d.Y, d.Y2);
                        await shapeService.AddRectangleAsync(
                            addAction.DocumentId, addAction.PageIndex,
                            rx, ry, d.Width, d.Height,
                            d.FillColor, d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Circle:
                        var cx = (d.X + d.X2) / 2;
                        var cy = (d.Y + d.Y2) / 2;
                        await shapeService.AddCircleAsync(
                            addAction.DocumentId, addAction.PageIndex,
                            cx, cy, d.Radius,
                            d.FillColor, d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Line:
                        await shapeService.AddLineAsync(
                            addAction.DocumentId, addAction.PageIndex,
                            d.X, d.Y, d.X2, d.Y2,
                            d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Freehand when d.Points != null:
                        await shapeService.AddFreehandPathAsync(
                            addAction.DocumentId, addAction.PageIndex,
                            d.Points, d.StrokeColor, d.StrokeWidth);
                        break;
                    case DrawingShapeType.Text:
                        await shapeService.AddTextAsync(
                            addAction.DocumentId, addAction.PageIndex,
                            d.X, d.Y, d.Text ?? "Text",
                            d.FontSize, d.FontName, d.StrokeColor);
                        break;
                }
                break;

            case DeleteShapeAction deleteAction:
                // Redo delete = remove again (find by bounds)
                var objs = await shapeService.GetPageObjectsAsync(deleteAction.DocumentId, deleteAction.PageIndex);
                if (objs.Count > 0)
                    await shapeService.RemovePageObjectAsync(deleteAction.DocumentId, deleteAction.PageIndex, objs[^1].Index);
                break;

            case MoveShapeAction moveAction:
                // Redo move = move forward again
                await shapeService.MovePageObjectAsync(
                    moveAction.DocumentId, moveAction.PageIndex,
                    moveAction.ObjectIndex, moveAction.DeltaX, moveAction.DeltaY);
                break;
        }

        ClearSelection();
        await _viewModel.RefreshCurrentPageAsync();
        _logger.LogInformation("Redo: {ActionType}", action.ActionType);
    }

    private async Task SelectAllObjectsAsync()
    {
        if (_viewModel?.CurrentDocument == null) return;

        var shapeService = App.GetService<IShapeService>();
        var docId = _viewModel.CurrentDocument.FilePath;
        var pageIndex = new PageIndex(_viewModel.CurrentPageNumber - 1);

        var objects = await Task.Run(async () => await shapeService.GetPageObjectsAsync(docId, pageIndex));

        _selectedObjects.Clear();
        var selectable = _viewModel.IsOriginalObjectsLocked
            ? objects.Where(o => !IsOriginalObject(o)).ToList()
            : objects;
        _selectedObjects.AddRange(selectable);
        _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        ShowMultiSelectionHighlights();
        _logger.LogInformation("Selected all {Count} objects on page (filtered from {Total})",
            _selectedObjects.Count, objects.Count);
    }
}
