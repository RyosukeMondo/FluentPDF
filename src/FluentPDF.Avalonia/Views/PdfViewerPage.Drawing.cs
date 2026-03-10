using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FluentPDF.Avalonia.Helpers;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Drawing toolbar setup, canvas pointer handlers, and shape commit logic.
/// </summary>
public partial class PdfViewerPage
{
    private void SetupDrawingToolbar()
    {
        var toolButtons = new (string Name, DrawingTool Tool)[]
        {
            ("DrawToolPan", DrawingTool.None),
            ("DrawToolSelect", DrawingTool.Select),
            ("DrawToolLasso", DrawingTool.Lasso),
            ("DrawToolRectangle", DrawingTool.Rectangle),
            ("DrawToolCircle", DrawingTool.Circle),
            ("DrawToolLine", DrawingTool.Line),
            ("DrawToolFreehand", DrawingTool.Freehand),
            ("DrawToolText", DrawingTool.Text),
        };

        foreach (var (name, tool) in toolButtons)
        {
            var toggle = this.FindControl<global::Avalonia.Controls.Primitives.ToggleButton>(name);
            if (toggle == null) continue;

            var capturedTool = tool;
            toggle.Click += (s, e) =>
            {
                if (_viewModel == null) return;

                if (capturedTool == DrawingTool.None)
                {
                    _viewModel.ActiveDrawingTool = DrawingTool.None;
                }
                else
                {
                    _viewModel.SetDrawingToolCommand.Execute(capturedTool.ToString());
                }
                UpdateDrawingToolToggleStates();
            };
        }

        // Wire flyout buttons for advanced drawing tools (progressive disclosure)
        var flyoutToolButtons = new (string FlyoutButtonName, DrawingTool Tool)[]
        {
            ("DrawToolCircleFlyout", DrawingTool.Circle),
            ("DrawToolFreehandFlyout", DrawingTool.Freehand),
            ("DrawToolLassoFlyout", DrawingTool.Lasso),
            ("DrawToolPanFlyout", DrawingTool.None),
        };

        foreach (var (buttonName, tool) in flyoutToolButtons)
        {
            var btn = this.FindControl<Button>(buttonName);
            if (btn == null) continue;

            var capturedTool = tool;
            btn.Click += (s, e) =>
            {
                if (_viewModel == null) return;

                if (capturedTool == DrawingTool.None)
                {
                    _viewModel.ActiveDrawingTool = DrawingTool.None;
                }
                else
                {
                    _viewModel.SetDrawingToolCommand.Execute(capturedTool.ToString());
                }
                UpdateDrawingToolToggleStates();

                // Close the flyout after selection
                var moreButton = this.FindControl<Button>("DrawToolMoreButton");
                if (moreButton?.Flyout is global::Avalonia.Controls.Flyout flyout)
                    flyout.Hide();
            };
        }

        // Containment mode toggle (intersect vs fully contained)
        var containmentToggle = this.FindControl<global::Avalonia.Controls.Primitives.ToggleButton>("ContainmentModeToggle");
        if (containmentToggle != null)
        {
            containmentToggle.Click += (s, e) =>
            {
                if (_selectionManager == null) return;
                _selectionManager.ContainmentMode = containmentToggle.IsChecked == true
                    ? SelectionContainment.FullyContained
                    : SelectionContainment.Intersect;
                if (_viewModel != null)
                    _viewModel.StatusMessage = _selectionManager.ContainmentMode == SelectionContainment.FullyContained
                        ? "Selection: Fully Contained" : "Selection: Intersect";
            };
        }

        // Stroke color popup
        var strokeBtn = this.FindControl<Button>("StrokeColorButton");
        var strokePopup = this.FindControl<global::Avalonia.Controls.Primitives.Popup>("StrokeColorPopup");
        if (strokeBtn != null && strokePopup != null)
        {
            strokeBtn.Click += (s, e) => strokePopup.IsOpen = !strokePopup.IsOpen;
        }

        // Wire stroke color swatch clicks
        WireColorSwatches("ColorSwatch", color =>
        {
            if (_viewModel != null) _viewModel.DrawingStrokeColor = color;
            if (strokePopup != null) strokePopup.IsOpen = false;
        });

        // Fill color popup
        var fillBtn = this.FindControl<Button>("FillColorButton");
        var fillPopup = this.FindControl<global::Avalonia.Controls.Primitives.Popup>("FillColorPopup");
        if (fillBtn != null && fillPopup != null)
        {
            fillBtn.Click += (s, e) => fillPopup.IsOpen = !fillPopup.IsOpen;
        }

        WireColorSwatches("FillColorSwatch", color =>
        {
            if (_viewModel != null) _viewModel.DrawingFillColor = color;
            if (fillPopup != null) fillPopup.IsOpen = false;
        });

        // Stroke width combo
        var widthCombo = this.FindControl<ComboBox>("StrokeWidthCombo");
        if (widthCombo != null)
        {
            widthCombo.SelectionChanged += (s, e) =>
            {
                if (_viewModel == null) return;
                if (widthCombo.SelectedItem is ComboBoxItem item && item.Tag is string tagStr
                    && float.TryParse(tagStr, out var w))
                {
                    _viewModel.DrawingStrokeWidth = w;
                }
            };
        }

        // Subscribe to ActiveDrawingTool changes to sync toggle states
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PdfViewerViewModel.ActiveDrawingTool))
                {
                    Dispatcher.UIThread.Post(UpdateDrawingToolToggleStates);
                    // Lazily track original object count when any drawing tool is first activated
                    if (_viewModel?.ActiveDrawingTool != DrawingTool.None)
                        _ = TrackOriginalObjectCountAsync();
                }
            };
        }
    }

    private void WireColorSwatches(string className, Action<string> onColorSelected)
    {
        // Find buttons by walking the visual tree from the popups
        var popups = new[] { "StrokeColorPopup", "FillColorPopup" };
        foreach (var popupName in popups)
        {
            var popup = this.FindControl<global::Avalonia.Controls.Primitives.Popup>(popupName);
            if (popup?.Child is not Border border) continue;
            if (border.Child is not WrapPanel wrap) continue;

            foreach (var child in wrap.Children)
            {
                if (child is Button btn && btn.Classes.Contains(className) && btn.Tag is string color)
                {
                    var capturedColor = color;
                    btn.Click += (s, e) => onColorSelected(capturedColor);
                }
            }
        }
    }

    private void UpdateDrawingToolToggleStates()
    {
        var activeTool = _viewModel?.ActiveDrawingTool ?? DrawingTool.None;
        var mapping = new (string Name, DrawingTool Tool)[]
        {
            ("DrawToolPan", DrawingTool.None),
            ("DrawToolSelect", DrawingTool.Select),
            ("DrawToolLasso", DrawingTool.Lasso),
            ("DrawToolRectangle", DrawingTool.Rectangle),
            ("DrawToolCircle", DrawingTool.Circle),
            ("DrawToolLine", DrawingTool.Line),
            ("DrawToolFreehand", DrawingTool.Freehand),
            ("DrawToolText", DrawingTool.Text),
        };

        foreach (var (name, tool) in mapping)
        {
            var toggle = this.FindControl<global::Avalonia.Controls.Primitives.ToggleButton>(name);
            if (toggle != null)
                toggle.IsChecked = activeTool == tool;
        }
    }

    private void OnDrawingCanvasPointerPressed(
        object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || DrawingCanvas == null || PdfImage == null)
            return;
        if (_viewModel.ActiveDrawingTool == DrawingTool.None)
            return;

        var properties = e.GetCurrentPoint(DrawingCanvas).Properties;
        var point = e.GetCurrentPoint(PdfImage).Position;

        if (HandleSelectToolPressed(e, properties, point))
            return;

        if (!properties.IsLeftButtonPressed)
            return;

        if (HandleLassoToolPressed(e, point))
            return;

        _drawStartPoint = point;
        _isDrawing = true;
        _drawingPoints.Clear();
        _drawingPoints.Add(point);

        StartDrawingShape(_viewModel.ActiveDrawingTool, point);
        e.Handled = true;
    }

    private bool HandleSelectToolPressed(
        PointerPressedEventArgs e, PointerPointProperties properties, Point point)
    {
        if (_viewModel!.ActiveDrawingTool != DrawingTool.Select)
            return false;

        if (properties.IsRightButtonPressed && _selectedObject != null)
        {
            ShowShapeContextMenu(point);
            e.Handled = true;
            return true;
        }

        if (!properties.IsLeftButtonPressed)
            return true; // Not left-click on Select tool — consume but do nothing

        _isDraggingSelection = false;
        var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        _ = HandleSelectClickAsync(point, shiftHeld, altHeld);
        e.Handled = true;
        return true;
    }

    private bool HandleLassoToolPressed(PointerPressedEventArgs e, Point point)
    {
        if (_viewModel!.ActiveDrawingTool != DrawingTool.Lasso
            || _selectionManager == null)
            return false;

        var shiftHeld = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var altHeld = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        var modifier = SelectionManager.GetModifier(shiftHeld, altHeld);
        _selectionManager.StartLasso(point, modifier);
        e.Handled = true;
        return true;
    }

    private void StartDrawingShape(DrawingTool tool, Point point)
    {
        var strokeBrush = ParseBrush(_viewModel!.DrawingStrokeColor);
        var strokeWidth = _viewModel.DrawingStrokeWidth;

        switch (tool)
        {
            case DrawingTool.Rectangle:
                _drawingPreview = new Rectangle
                {
                    Stroke = strokeBrush,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    StrokeThickness = strokeWidth,
                    Width = 0, Height = 0
                };
                Canvas.SetLeft(_drawingPreview, point.X);
                Canvas.SetTop(_drawingPreview, point.Y);
                DrawingCanvas!.Children.Add(_drawingPreview);
                break;

            case DrawingTool.Circle:
                _drawingPreview = new Ellipse
                {
                    Stroke = strokeBrush,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    StrokeThickness = strokeWidth,
                    Width = 0, Height = 0
                };
                Canvas.SetLeft(_drawingPreview, point.X);
                Canvas.SetTop(_drawingPreview, point.Y);
                DrawingCanvas!.Children.Add(_drawingPreview);
                break;

            case DrawingTool.Line:
                _drawingPreview = new Line
                {
                    Stroke = strokeBrush,
                    StrokeThickness = strokeWidth,
                    StartPoint = point,
                    EndPoint = point
                };
                DrawingCanvas!.Children.Add(_drawingPreview);
                break;

            case DrawingTool.Freehand:
                _drawingPreview = new Polyline
                {
                    Stroke = strokeBrush,
                    StrokeThickness = strokeWidth,
                    Points = new global::Avalonia.Collections.AvaloniaList<Point> { point }
                };
                DrawingCanvas!.Children.Add(_drawingPreview);
                break;

            case DrawingTool.Text:
                _drawingPreview = null;
                break;
        }
    }
}
