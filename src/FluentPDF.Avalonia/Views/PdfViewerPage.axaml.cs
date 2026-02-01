using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FluentPDF.Core.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Linq;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Main PDF viewer page that displays the PDF document with toolbar controls.
/// Implements text selection and annotation functionality.
/// </summary>
public partial class PdfViewerPage : UserControl
{
    private PdfViewerViewModel? _viewModel;
    private Point _selectionStartPoint;
    private bool _isSelecting;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerPage"/> class.
    /// Default constructor for design-time.
    /// </summary>
    public PdfViewerPage()
    {
        InitializeComponent();

        // Wire up pointer events for text selection
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerPage"/> class.
    /// Constructor with view model injection.
    /// </summary>
    /// <param name="viewModel">The PDF viewer view model.</param>
    public PdfViewerPage(PdfViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // Wire up pointer events for text selection
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// Handles the Loaded event to wire up event handlers.
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Get ViewModel from DataContext if not already set
        if (_viewModel == null && DataContext is PdfViewerViewModel vm)
        {
            _viewModel = vm;
        }

        // Wire up pointer events on the PDF image
        if (PdfImage != null)
        {
            PdfImage.PointerPressed += OnImagePointerPressed;
            PdfImage.PointerMoved += OnImagePointerMoved;
            PdfImage.PointerReleased += OnImagePointerReleased;
        }

        // Wire up scroll viewer mouse wheel for zoom
        if (PdfScrollViewer != null)
        {
            PdfScrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
        }

        // Wire up keyboard events for annotation shortcuts
        this.KeyDown += OnPageKeyDown;

        // Subscribe to ViewModel property changes
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Handles ViewModel property changes to update UI.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageNumber))
        {
            // Clear selection when page changes
            ClearSelectionRectangle();
        }
    }

    /// <summary>
    /// Handles keyboard events for annotation shortcuts (H/U/S keys).
    /// TODO: Wire up annotation commands when AnnotationViewModel is added to Core.
    /// </summary>
    private void OnPageKeyDown(object? sender, KeyEventArgs e)
    {
        // TODO: Implement annotation keyboard shortcuts when AnnotationViewModel is available
        // For now, just log the key presses
        var logger = App.GetService<ILogger<PdfViewerPage>>();

        switch (e.Key)
        {
            case Key.H when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                logger?.LogInformation("H key pressed - Highlight tool shortcut (not yet implemented)");
                e.Handled = true;
                break;

            case Key.U when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                logger?.LogInformation("U key pressed - Underline tool shortcut (not yet implemented)");
                e.Handled = true;
                break;

            case Key.S when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                logger?.LogInformation("S key pressed - Strikethrough tool shortcut (not yet implemented)");
                e.Handled = true;
                break;
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

        // Only start selection on left-click
        if (properties.IsLeftButtonPressed)
        {
            // TODO: Check if annotation tool is active when AnnotationViewModel is available

            var point = e.GetCurrentPoint(PdfImage).Position;
            _selectionStartPoint = point;
            _isSelecting = true;

            // Begin text selection in ViewModel (if commands exist)
            // Note: These commands need to be added to Core ViewModel
            // For now, track selection state locally

            // Show selection rectangle
            ShowSelectionRectangle(point, point);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved event to update text selection.
    /// </summary>
    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting || PdfImage == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(PdfImage).Position;

        // Update selection rectangle
        ShowSelectionRectangle(_selectionStartPoint, point);

        e.Handled = true;
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

        // End text selection in ViewModel
        // TODO: Call ViewModel.EndTextSelectionCommand when it's added to Core
        // For now, just log the selection
        if (width > 5 && height > 5) // Minimum selection size
        {
            var logger = App.GetService<ILogger<PdfViewerPage>>();
            logger?.LogInformation(
                "Text selection: ({X}, {Y}) - ({Width}x{Height})",
                x, y, width, height);
        }
        else
        {
            // Click without drag - clear selection
            ClearSelectionRectangle();
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

        if (ctrlPressed)
        {
            var delta = e.Delta.Y;
            if (delta > 0)
            {
                // Zoom in
                if (_viewModel.ZoomInCommand.CanExecute(null))
                {
                    _ = _viewModel.ZoomInCommand.ExecuteAsync(null);
                }
            }
            else if (delta < 0)
            {
                // Zoom out
                if (_viewModel.ZoomOutCommand.CanExecute(null))
                {
                    _ = _viewModel.ZoomOutCommand.ExecuteAsync(null);
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Updates the cursor based on the active annotation tool.
    /// TODO: Implement when AnnotationViewModel is added to Core.
    /// </summary>
    private void UpdateCursorForActiveTool()
    {
        // TODO: Wire up cursor changes based on annotation tool when AnnotationViewModel is available
        this.Cursor = Cursor.Default;
    }
}
