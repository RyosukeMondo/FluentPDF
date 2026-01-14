using System.Collections.Specialized;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace FluentPDF.App.Controls;

/// <summary>
/// Transparent canvas overlay for rendering and interacting with PDF annotations.
/// Handles pointer events for drawing, selection, and manipulation of annotations.
/// </summary>
public sealed partial class AnnotationLayer : UserControl
{
    /// <summary>
    /// Dependency property for the AnnotationViewModel.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(AnnotationViewModel),
            typeof(AnnotationLayer),
            new PropertyMetadata(null, OnViewModelChanged));

    /// <summary>
    /// Dependency property for the zoom level of the PDF viewer.
    /// </summary>
    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(
            nameof(ZoomLevel),
            typeof(double),
            typeof(AnnotationLayer),
            new PropertyMetadata(1.0, OnZoomLevelChanged));

    private System.Drawing.PointF? _drawStartPoint;
    private readonly List<System.Drawing.PointF> _currentInkPoints = new();
    private Shape? _currentShape;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnotationLayer"/> class.
    /// </summary>
    public AnnotationLayer()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the AnnotationViewModel for this layer.
    /// </summary>
    public AnnotationViewModel? ViewModel
    {
        get => (AnnotationViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom level for coordinate transformation.
    /// </summary>
    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    /// <summary>
    /// Handles changes to the ViewModel property.
    /// </summary>
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var layer = (AnnotationLayer)d;

        if (e.OldValue is AnnotationViewModel oldViewModel)
        {
            oldViewModel.Annotations.CollectionChanged -= layer.OnAnnotationsCollectionChanged;
        }

        if (e.NewValue is AnnotationViewModel newViewModel)
        {
            newViewModel.Annotations.CollectionChanged += layer.OnAnnotationsCollectionChanged;
            layer.RenderAllAnnotations();
        }
    }

    /// <summary>
    /// Handles changes to the ZoomLevel property.
    /// </summary>
    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var layer = (AnnotationLayer)d;
        layer.RenderAllAnnotations();
    }

    /// <summary>
    /// Handles collection changes in the Annotations collection.
    /// </summary>
    private void OnAnnotationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderAllAnnotations();
    }

    /// <summary>
    /// Renders all annotations on the canvas.
    /// </summary>
    private void RenderAllAnnotations()
    {
        AnnotationCanvas.Children.Clear();

        if (ViewModel == null)
        {
            return;
        }

        foreach (var annotation in ViewModel.Annotations)
        {
            RenderAnnotation(annotation);
        }
    }

    /// <summary>
    /// Renders a single annotation on the canvas.
    /// </summary>
    /// <param name="annotation">The annotation to render.</param>
    private void RenderAnnotation(Annotation annotation)
    {
        Shape? shape = annotation.Type switch
        {
            AnnotationType.Highlight => CreateHighlightShape(annotation),
            AnnotationType.Underline => CreateUnderlineShape(annotation),
            AnnotationType.StrikeOut => CreateStrikeOutShape(annotation),
            AnnotationType.Square => CreateRectangleShape(annotation),
            AnnotationType.Circle => CreateCircleShape(annotation),
            AnnotationType.Ink => CreateInkShape(annotation),
            AnnotationType.Text => CreateCommentShape(annotation),
            _ => null
        };

        if (shape != null)
        {
            shape.Tag = annotation;
            shape.PointerPressed += OnAnnotationShapePressed;
            AnnotationCanvas.Children.Add(shape);

            // Add selection indicator if selected
            if (annotation.IsSelected)
            {
                AddSelectionIndicator(annotation);
            }
        }
    }

    /// <summary>
    /// Creates a highlight shape for a text markup annotation.
    /// </summary>
    private Shape CreateHighlightShape(Annotation annotation)
    {
        var rect = new Rectangle
        {
            Width = (annotation.Bounds.Right - annotation.Bounds.Left) * ZoomLevel,
            Height = (annotation.Bounds.Bottom - annotation.Bounds.Top) * ZoomLevel,
            Fill = new SolidColorBrush(ConvertColor(annotation.FillColor)),
            Opacity = annotation.Opacity
        };

        Canvas.SetLeft(rect, annotation.Bounds.Left * ZoomLevel);
        Canvas.SetTop(rect, annotation.Bounds.Top * ZoomLevel);

        return rect;
    }

    /// <summary>
    /// Creates an underline shape for a text markup annotation.
    /// </summary>
    private Shape CreateUnderlineShape(Annotation annotation)
    {
        var line = new Rectangle
        {
            Width = (annotation.Bounds.Right - annotation.Bounds.Left) * ZoomLevel,
            Height = annotation.StrokeWidth * ZoomLevel,
            Fill = new SolidColorBrush(ConvertColor(annotation.StrokeColor)),
            Opacity = annotation.Opacity
        };

        Canvas.SetLeft(line, annotation.Bounds.Left * ZoomLevel);
        Canvas.SetTop(line, annotation.Bounds.Bottom * ZoomLevel);

        return line;
    }

    /// <summary>
    /// Creates a strikeout shape for a text markup annotation.
    /// </summary>
    private Shape CreateStrikeOutShape(Annotation annotation)
    {
        var line = new Rectangle
        {
            Width = (annotation.Bounds.Right - annotation.Bounds.Left) * ZoomLevel,
            Height = annotation.StrokeWidth * ZoomLevel,
            Fill = new SolidColorBrush(ConvertColor(annotation.StrokeColor)),
            Opacity = annotation.Opacity
        };

        var centerY = (annotation.Bounds.Top + annotation.Bounds.Bottom) / 2;
        Canvas.SetLeft(line, annotation.Bounds.Left * ZoomLevel);
        Canvas.SetTop(line, centerY * ZoomLevel);

        return line;
    }

    /// <summary>
    /// Creates a rectangle shape for a square annotation.
    /// </summary>
    private Shape CreateRectangleShape(Annotation annotation)
    {
        var rect = new Rectangle
        {
            Width = (annotation.Bounds.Right - annotation.Bounds.Left) * ZoomLevel,
            Height = (annotation.Bounds.Bottom - annotation.Bounds.Top) * ZoomLevel,
            Stroke = new SolidColorBrush(ConvertColor(annotation.StrokeColor)),
            StrokeThickness = annotation.StrokeWidth * ZoomLevel,
            Fill = annotation.FillColor.A > 0
                ? new SolidColorBrush(ConvertColor(annotation.FillColor))
                : null,
            Opacity = annotation.Opacity
        };

        Canvas.SetLeft(rect, annotation.Bounds.Left * ZoomLevel);
        Canvas.SetTop(rect, annotation.Bounds.Top * ZoomLevel);

        return rect;
    }

    /// <summary>
    /// Creates a circle shape for a circle annotation.
    /// </summary>
    private Shape CreateCircleShape(Annotation annotation)
    {
        var width = (annotation.Bounds.Right - annotation.Bounds.Left) * ZoomLevel;
        var height = (annotation.Bounds.Bottom - annotation.Bounds.Top) * ZoomLevel;

        var ellipse = new Ellipse
        {
            Width = width,
            Height = height,
            Stroke = new SolidColorBrush(ConvertColor(annotation.StrokeColor)),
            StrokeThickness = annotation.StrokeWidth * ZoomLevel,
            Fill = annotation.FillColor.A > 0
                ? new SolidColorBrush(ConvertColor(annotation.FillColor))
                : null,
            Opacity = annotation.Opacity
        };

        Canvas.SetLeft(ellipse, annotation.Bounds.Left * ZoomLevel);
        Canvas.SetTop(ellipse, annotation.Bounds.Top * ZoomLevel);

        return ellipse;
    }

    /// <summary>
    /// Creates an ink shape for a freehand annotation.
    /// </summary>
    private Shape? CreateInkShape(Annotation annotation)
    {
        if (annotation.InkPoints.Count < 2)
        {
            return null;
        }

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(ConvertColor(annotation.StrokeColor)),
            StrokeThickness = annotation.StrokeWidth * ZoomLevel,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = annotation.Opacity
        };

        foreach (var point in annotation.InkPoints)
        {
            polyline.Points.Add(new Windows.Foundation.Point(
                point.X * ZoomLevel,
                point.Y * ZoomLevel));
        }

        return polyline;
    }

    /// <summary>
    /// Creates a comment indicator for a text annotation.
    /// </summary>
    private Shape CreateCommentShape(Annotation annotation)
    {
        var size = 24 * ZoomLevel;
        var ellipse = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(ConvertColor(annotation.FillColor)),
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 1 * ZoomLevel,
            Opacity = annotation.Opacity
        };

        Canvas.SetLeft(ellipse, annotation.Bounds.Left * ZoomLevel);
        Canvas.SetTop(ellipse, annotation.Bounds.Top * ZoomLevel);

        return ellipse;
    }

    /// <summary>
    /// Adds a selection indicator around the selected annotation.
    /// </summary>
    private void AddSelectionIndicator(Annotation annotation)
    {
        var padding = 4 * ZoomLevel;
        var rect = new Rectangle
        {
            Width = (annotation.Bounds.Right - annotation.Bounds.Left) * ZoomLevel + padding * 2,
            Height = (annotation.Bounds.Bottom - annotation.Bounds.Top) * ZoomLevel + padding * 2,
            Stroke = new SolidColorBrush(Colors.Blue),
            StrokeThickness = 2 * ZoomLevel,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            Fill = null
        };

        Canvas.SetLeft(rect, annotation.Bounds.Left * ZoomLevel - padding);
        Canvas.SetTop(rect, annotation.Bounds.Top * ZoomLevel - padding);

        AnnotationCanvas.Children.Add(rect);
    }

    /// <summary>
    /// Handles pointer pressed event on the canvas.
    /// </summary>
    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(AnnotationCanvas);
        var pdfPoint = new System.Drawing.PointF(
            (float)(point.Position.X / ZoomLevel),
            (float)(point.Position.Y / ZoomLevel));

        _drawStartPoint = pdfPoint;

        if (ViewModel.ActiveTool == AnnotationTool.Freehand)
        {
            _currentInkPoints.Clear();
            _currentInkPoints.Add(pdfPoint);
        }
        else if (ViewModel.ActiveTool != AnnotationTool.None)
        {
            // Create a temporary shape for visual feedback
            CreateTemporaryShape(pdfPoint, pdfPoint);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer moved event on the canvas.
    /// </summary>
    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel == null || !_drawStartPoint.HasValue)
        {
            return;
        }

        var point = e.GetCurrentPoint(AnnotationCanvas);
        var pdfPoint = new System.Drawing.PointF(
            (float)(point.Position.X / ZoomLevel),
            (float)(point.Position.Y / ZoomLevel));

        if (ViewModel.ActiveTool == AnnotationTool.Freehand)
        {
            _currentInkPoints.Add(pdfPoint);
            UpdateTemporaryInkShape();
        }
        else if (ViewModel.ActiveTool != AnnotationTool.None)
        {
            UpdateTemporaryShape(_drawStartPoint.Value, pdfPoint);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer released event on the canvas.
    /// </summary>
    private async void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel == null || !_drawStartPoint.HasValue)
        {
            return;
        }

        var point = e.GetCurrentPoint(AnnotationCanvas);
        var pdfPoint = new System.Drawing.PointF(
            (float)(point.Position.X / ZoomLevel),
            (float)(point.Position.Y / ZoomLevel));

        // Remove temporary shape
        if (_currentShape != null)
        {
            AnnotationCanvas.Children.Remove(_currentShape);
            _currentShape = null;
        }

        if (ViewModel.ActiveTool == AnnotationTool.Freehand && _currentInkPoints.Count > 1)
        {
            // Create ink annotation
            await ViewModel.CreateInkAnnotationCommand.ExecuteAsync(_currentInkPoints.ToList());
            _currentInkPoints.Clear();
        }
        else if (ViewModel.ActiveTool != AnnotationTool.None && ViewModel.ActiveTool != AnnotationTool.Freehand)
        {
            // Create regular annotation
            var bounds = new PdfRectangle
            {
                Left = Math.Min(_drawStartPoint.Value.X, pdfPoint.X),
                Top = Math.Min(_drawStartPoint.Value.Y, pdfPoint.Y),
                Right = Math.Max(_drawStartPoint.Value.X, pdfPoint.X),
                Bottom = Math.Max(_drawStartPoint.Value.Y, pdfPoint.Y)
            };

            // Only create annotation if bounds are meaningful (not just a click)
            if (Math.Abs(bounds.Right - bounds.Left) > 1 && Math.Abs(bounds.Bottom - bounds.Top) > 1)
            {
                await ViewModel.CreateAnnotationCommand.ExecuteAsync(bounds);
            }
        }

        _drawStartPoint = null;
        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer exited event on the canvas.
    /// </summary>
    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Cancel current drawing operation if pointer leaves canvas
        if (_currentShape != null)
        {
            AnnotationCanvas.Children.Remove(_currentShape);
            _currentShape = null;
        }

        _drawStartPoint = null;
        _currentInkPoints.Clear();
    }

    /// <summary>
    /// Handles pointer pressed event on an annotation shape.
    /// </summary>
    private void OnAnnotationShapePressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Shape shape && shape.Tag is Annotation annotation && ViewModel != null)
        {
            ViewModel.SelectAnnotationCommand.Execute(annotation);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Creates a temporary shape for visual feedback during drawing.
    /// </summary>
    private void CreateTemporaryShape(System.Drawing.PointF start, System.Drawing.PointF end)
    {
        if (_currentShape != null)
        {
            AnnotationCanvas.Children.Remove(_currentShape);
        }

        var color = ViewModel?.SelectedColor ?? Colors.Yellow;
        var brush = new SolidColorBrush(color);
        var opacity = ViewModel?.Opacity ?? 0.5;

        _currentShape = ViewModel?.ActiveTool switch
        {
            AnnotationTool.Rectangle or AnnotationTool.Highlight => new Rectangle
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(color) { Opacity = opacity }
            },
            AnnotationTool.Circle => new Ellipse
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(color) { Opacity = opacity }
            },
            AnnotationTool.Underline or AnnotationTool.Strikethrough => new Rectangle
            {
                Fill = brush,
                Opacity = opacity
            },
            _ => null
        };

        if (_currentShape != null)
        {
            AnnotationCanvas.Children.Add(_currentShape);
            UpdateTemporaryShape(start, end);
        }
    }

    /// <summary>
    /// Updates the temporary shape during drawing.
    /// </summary>
    private void UpdateTemporaryShape(System.Drawing.PointF start, System.Drawing.PointF end)
    {
        if (_currentShape == null || ViewModel == null)
        {
            return;
        }

        var left = Math.Min(start.X, end.X) * ZoomLevel;
        var top = Math.Min(start.Y, end.Y) * ZoomLevel;
        var width = Math.Abs(end.X - start.X) * ZoomLevel;
        var height = Math.Abs(end.Y - start.Y) * ZoomLevel;

        if (ViewModel.ActiveTool == AnnotationTool.Underline)
        {
            height = (ViewModel.StrokeWidth * ZoomLevel);
            top = end.Y * ZoomLevel;
        }
        else if (ViewModel.ActiveTool == AnnotationTool.Strikethrough)
        {
            height = (ViewModel.StrokeWidth * ZoomLevel);
            top = ((start.Y + end.Y) / 2) * ZoomLevel;
        }

        _currentShape.Width = width;
        _currentShape.Height = height;
        Canvas.SetLeft(_currentShape, left);
        Canvas.SetTop(_currentShape, top);
    }

    /// <summary>
    /// Updates the temporary ink shape during freehand drawing.
    /// </summary>
    private void UpdateTemporaryInkShape()
    {
        if (_currentInkPoints.Count < 2 || ViewModel == null)
        {
            return;
        }

        if (_currentShape != null)
        {
            AnnotationCanvas.Children.Remove(_currentShape);
        }

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(ViewModel.SelectedColor),
            StrokeThickness = ViewModel.StrokeWidth * ZoomLevel,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = ViewModel.Opacity
        };

        foreach (var point in _currentInkPoints)
        {
            polyline.Points.Add(new Windows.Foundation.Point(
                point.X * ZoomLevel,
                point.Y * ZoomLevel));
        }

        _currentShape = polyline;
        AnnotationCanvas.Children.Add(_currentShape);
    }

    /// <summary>
    /// Converts System.Drawing.Color to Windows.UI.Color.
    /// </summary>
    private static Windows.UI.Color ConvertColor(System.Drawing.Color color)
    {
        return Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
