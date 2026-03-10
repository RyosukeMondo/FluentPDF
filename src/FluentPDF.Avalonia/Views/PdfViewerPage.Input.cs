using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Mouse, keyboard, and pointer event handlers for the PDF image and scroll viewer.
/// </summary>
public partial class PdfViewerPage
{
    /// <summary>
    /// Handles keyboard events for annotation shortcuts (H/U/S keys).
    /// Creates text markup annotations when text is selected.
    /// </summary>
    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        // Escape closes drawing toolbar
        if (e.Key == Key.Escape && _viewModel?.IsDrawingToolbarVisible == true)
        {
            ClearSelection();
            _viewModel.CloseDrawingToolbarCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Ctrl+Z undo
        if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = UndoAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+Y redo
        if (e.Key == Key.Y && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = RedoAsync();
            e.Handled = true;
            return;
        }

        // Ctrl+A select all objects on page
        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            _viewModel?.ActiveDrawingTool == DrawingTool.Select)
        {
            _ = SelectAllObjectsAsync();
            e.Handled = true;
            return;
        }

        // Delete selected shape(s)
        if ((e.Key == Key.Delete || e.Key == Key.Back) && (_selectedObject != null || _selectedObjects.Count > 0))
        {
            _ = DeleteSelectedObjectsAsync();
            e.Handled = true;
            return;
        }

        if (_viewModel?.HasSelectedText != true || _viewModel.LastTextSelection == null)
            return;

        AnnotationType? annotationType = e.Key switch
        {
            Key.H when !e.KeyModifiers.HasFlag(KeyModifiers.Control) => AnnotationType.Highlight,
            Key.U when !e.KeyModifiers.HasFlag(KeyModifiers.Control) => AnnotationType.Underline,
            Key.S when !e.KeyModifiers.HasFlag(KeyModifiers.Control) => AnnotationType.StrikeOut,
            _ => null
        };

        if (annotationType.HasValue)
        {
            _ = CreateTextMarkupAnnotationAsync(annotationType.Value);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles Ctrl+C to copy selected text to clipboard.
    /// </summary>
    private void OnPageKeyDown_CopyText(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (_viewModel?.HasSelectedText == true && !string.IsNullOrEmpty(_viewModel.SelectedText))
            {
                _ = CopyToClipboardAsync(_viewModel.SelectedText);
                e.Handled = true;
            }
        }
    }

    private async Task CopyToClipboardAsync(string text)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(text);
                _logger.LogInformation("Copied {Length} characters to clipboard", text.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
        }
    }

    /// <summary>
    /// Handles pointer pressed event to begin text selection.
    /// </summary>
    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || PdfImage == null)
        {
            return;
        }

        var properties = e.GetCurrentPoint(PdfImage).Properties;

        // Right-click: show context menu when text is selected
        if (properties.IsRightButtonPressed && _viewModel.HasSelectedText)
        {
            ShowTextSelectionContextMenu(e.GetCurrentPoint(PdfImage).Position);
            e.Handled = true;
            return;
        }

        // Only start selection on left-click
        if (properties.IsLeftButtonPressed)
        {
            var point = e.GetCurrentPoint(PdfImage).Position;
            _selectionStartPoint = point;
            _isSelecting = true;

            // Show selection rectangle
            ShowSelectionRectangle(point, point);

            e.Handled = true;
        }
    }

    private void ShowTextSelectionContextMenu(Point position)
    {
        if (_viewModel == null || PdfImage == null) return;

        var menu = new global::Avalonia.Controls.ContextMenu();

        // Copy
        var copyItem = new global::Avalonia.Controls.MenuItem
        {
            Header = "Copy",
            InputGesture = new KeyGesture(Key.C, KeyModifiers.Control)
        };
        copyItem.Click += (s, e) =>
        {
            if (_viewModel.HasSelectedText)
                _ = CopyToClipboardAsync(_viewModel.SelectedText);
        };
        menu.Items.Add(copyItem);

        menu.Items.Add(new global::Avalonia.Controls.Separator());

        // Highlight colors
        var highlightColors = new[]
        {
            ("Yellow", "#FFFF00", AnnotationType.Highlight),
            ("Green", "#00FF00", AnnotationType.Highlight),
            ("Blue", "#00BFFF", AnnotationType.Highlight),
            ("Pink", "#FF69B4", AnnotationType.Highlight),
        };

        var highlightMenu = new global::Avalonia.Controls.MenuItem { Header = "Highlight" };
        foreach (var (name, color, type) in highlightColors)
        {
            var colorItem = new global::Avalonia.Controls.MenuItem { Header = name };
            var capturedColor = color;
            colorItem.Click += (s, e) => _ = CreateColoredHighlightAsync(capturedColor);
            highlightMenu.Items.Add(colorItem);
        }
        menu.Items.Add(highlightMenu);

        // Underline
        var underlineItem = new global::Avalonia.Controls.MenuItem { Header = "Underline" };
        underlineItem.Click += (s, e) => _ = CreateTextMarkupAnnotationAsync(AnnotationType.Underline);
        menu.Items.Add(underlineItem);

        // Strikethrough
        var strikethroughItem = new global::Avalonia.Controls.MenuItem { Header = "Strikethrough" };
        strikethroughItem.Click += (s, e) => _ = CreateTextMarkupAnnotationAsync(AnnotationType.StrikeOut);
        menu.Items.Add(strikethroughItem);

        menu.Open(PdfImage);
    }

    private async Task CreateColoredHighlightAsync(string color)
    {
        await CreateTextMarkupAnnotationAsync(AnnotationType.Highlight);
        // TODO: Apply color to the annotation once IAnnotationService supports color parameter
    }

    /// <summary>
    /// Handles pointer moved event to update text selection and link cursor.
    /// </summary>
    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (PdfImage == null)
            return;

        var point = e.GetCurrentPoint(PdfImage).Position;

        if (_isSelecting)
        {
            ShowSelectionRectangle(_selectionStartPoint, point);
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Handles pointer released event to end text selection.
    /// </summary>
    private void OnImagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSelecting || _viewModel == null || PdfImage == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(PdfImage).Position;
        _isSelecting = false;

        // Calculate selection bounds
        var x = Math.Min(_selectionStartPoint.X, point.X);
        var y = Math.Min(_selectionStartPoint.Y, point.Y);
        var width = Math.Abs(point.X - _selectionStartPoint.X);
        var height = Math.Abs(point.Y - _selectionStartPoint.Y);

        if (width > 5 && height > 5) // Minimum selection size
        {
            _logger.LogInformation(
                "Text selection: ({X}, {Y}) - ({Width}x{Height})",
                x, y, width, height);

            // Extract text from selection area
            _ = ExtractTextFromSelectionAsync(x, y, width, height);
        }
        else
        {
            // Click without drag - check for link, then clear selection
            _ = HandleLinkClickAsync(point);
            ClearSelectionRectangle();
            _viewModel.SelectedText = string.Empty;
            _viewModel.HasSelectedText = false;
            _viewModel.LastTextSelection = null;
        }

        e.Handled = true;
    }

    /// <summary>
    /// Shows the selection rectangle with the given bounds.
    /// </summary>
    private void ShowSelectionRectangle(Point start, Point end)
    {
        if (SelectionRectangle == null)
        {
            return;
        }

        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        // Update selection rectangle visual
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
        Canvas.SetLeft(SelectionRectangle, x);
        Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.IsVisible = true;
    }

    /// <summary>
    /// Clears the visual selection rectangle.
    /// </summary>
    private void ClearSelectionRectangle()
    {
        if (SelectionRectangle == null)
        {
            return;
        }

        SelectionRectangle.IsVisible = false;
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
    }

    /// <summary>
    /// Handles mouse wheel events for Ctrl+scroll zoom.
    /// </summary>
    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        var ctrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Zoom ONLY with Ctrl+Wheel (as expected by user)
        if (ctrlPressed)
        {
            var delta = e.Delta.Y;
            if (delta > 0)
            {
                // Zoom in
                if (_viewModel.Zoom.ZoomInCommand.CanExecute(null))
                {
                    _ = _viewModel.Zoom.ZoomInCommand.ExecuteAsync(null);
                }
            }
            else if (delta < 0)
            {
                // Zoom out
                if (_viewModel.Zoom.ZoomOutCommand.CanExecute(null))
                {
                    _ = _viewModel.Zoom.ZoomOutCommand.ExecuteAsync(null);
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer pressed for middle button panning.
    /// </summary>
    private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (PdfScrollViewer == null)
            return;

        var point = e.GetCurrentPoint(PdfScrollViewer);

        // Start panning on middle button press
        if (point.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = point.Position;
            _panStartOffset = new Vector(PdfScrollViewer.Offset.X, PdfScrollViewer.Offset.Y);
            e.Pointer.Capture(PdfScrollViewer);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved for middle button panning.
    /// </summary>
    private void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || PdfScrollViewer == null)
            return;

        var currentPoint = e.GetPosition(PdfScrollViewer);
        var delta = _panStartPoint - currentPoint;

        // Pan the scroll viewer
        PdfScrollViewer.Offset = new Vector(
            _panStartOffset.X + delta.X,
            _panStartOffset.Y + delta.Y);

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer released to stop middle button panning.
    /// </summary>
    private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Creates a text markup annotation (highlight, underline, strikethrough) from the current selection.
    /// </summary>
    private async Task CreateTextMarkupAnnotationAsync(AnnotationType type)
    {
        if (_viewModel?.CurrentDocument == null || _viewModel.LastTextSelection == null)
            return;

        try
        {
            var annotationService = App.GetService<IAnnotationService>();
            var selection = _viewModel.LastTextSelection;

            var annotation = new Annotation
            {
                Type = type,
                PageNumber = _viewModel.CurrentPageNumber - 1,
                Bounds = new PdfRectangle(
                    selection.SelectionBounds.Left,
                    selection.SelectionBounds.Top,
                    selection.SelectionBounds.Right,
                    selection.SelectionBounds.Bottom),
                QuadPoints = selection.ToQuadPoints(),
                FillColor = type == AnnotationType.Highlight
                    ? System.Drawing.Color.FromArgb(128, 255, 255, 0)
                    : System.Drawing.Color.FromArgb(255, 255, 0, 0),
                Opacity = type == AnnotationType.Highlight ? 0.5 : 1.0
            };

            var result = await annotationService.CreateAnnotationAsync(
                _viewModel.CurrentDocument, annotation);

            if (result.IsSuccess)
            {
                _logger.LogInformation("{Type} annotation created on page {Page}",
                    type, _viewModel.CurrentPageNumber);
                await _viewModel.RefreshCurrentPageAsync();
            }
            else
            {
                _logger.LogError("Failed to create {Type} annotation: {Error}",
                    type, result.Errors.FirstOrDefault()?.Message ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {Type} annotation", type);
        }
    }
}
