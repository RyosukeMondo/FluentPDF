using System.Collections.Specialized;
using System.Drawing;
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
/// Transparent canvas overlay for rendering and interacting with inserted PDF images.
/// Handles pointer events for selection, positioning, scaling, and rotation of images.
/// </summary>
public sealed partial class ImageManipulationOverlay : UserControl
{
    /// <summary>
    /// Dependency property for the ImageInsertionViewModel.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ImageInsertionViewModel),
            typeof(ImageManipulationOverlay),
            new PropertyMetadata(null, OnViewModelChanged));

    /// <summary>
    /// Dependency property for the zoom level of the PDF viewer.
    /// </summary>
    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(
            nameof(ZoomLevel),
            typeof(double),
            typeof(ImageManipulationOverlay),
            new PropertyMetadata(1.0, OnZoomLevelChanged));

    private const double HandleSize = 8.0;
    private const double RotationHandleOffset = 20.0;
    private const double SelectionPadding = 4.0;

    private enum ManipulationMode
    {
        None,
        Move,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight,
        ResizeTop,
        ResizeBottom,
        ResizeLeft,
        ResizeRight,
        Rotate
    }

    private ManipulationMode _currentMode = ManipulationMode.None;
    private PointF _manipulationStartPoint;
    private PointF _imageStartPosition;
    private SizeF _imageStartSize;
    private float _imageStartRotation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageManipulationOverlay"/> class.
    /// </summary>
    public ImageManipulationOverlay()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the ImageInsertionViewModel for this layer.
    /// </summary>
    public ImageInsertionViewModel? ViewModel
    {
        get => (ImageInsertionViewModel?)GetValue(ViewModelProperty);
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
        var overlay = (ImageManipulationOverlay)d;

        if (e.OldValue is ImageInsertionViewModel oldViewModel)
        {
            oldViewModel.InsertedImages.CollectionChanged -= overlay.OnImagesCollectionChanged;
            oldViewModel.PropertyChanged -= overlay.OnViewModelPropertyChanged;
        }

        if (e.NewValue is ImageInsertionViewModel newViewModel)
        {
            newViewModel.InsertedImages.CollectionChanged += overlay.OnImagesCollectionChanged;
            newViewModel.PropertyChanged += overlay.OnViewModelPropertyChanged;
            overlay.RenderAllImages();
        }
    }

    /// <summary>
    /// Handles changes to the ZoomLevel property.
    /// </summary>
    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var overlay = (ImageManipulationOverlay)d;
        overlay.RenderAllImages();
    }

    /// <summary>
    /// Handles collection changes in the InsertedImages collection.
    /// </summary>
    private void OnImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderAllImages();
    }

    /// <summary>
    /// Handles property changes in the ViewModel (e.g., SelectedImage).
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageInsertionViewModel.SelectedImage))
        {
            RenderAllImages();
        }
    }

    /// <summary>
    /// Renders all images on the canvas.
    /// </summary>
    private void RenderAllImages()
    {
        ManipulationCanvas.Children.Clear();

        if (ViewModel == null)
        {
            return;
        }

        foreach (var image in ViewModel.InsertedImages)
        {
            RenderImage(image);
        }
    }

    /// <summary>
    /// Renders a single image on the canvas.
    /// </summary>
    /// <param name="image">The image to render.</param>
    private void RenderImage(ImageObject image)
    {
        // Draw image placeholder (rectangle representing the image)
        var imageRect = CreateImageRectangle(image);
        ManipulationCanvas.Children.Add(imageRect);

        // Add selection handles if selected
        if (image.IsSelected)
        {
            AddSelectionHandles(image);
        }
    }

    /// <summary>
    /// Creates a visual rectangle representing the image.
    /// </summary>
    private Rectangle CreateImageRectangle(ImageObject image)
    {
        var rect = new Rectangle
        {
            Width = image.Size.Width * ZoomLevel,
            Height = image.Size.Height * ZoomLevel,
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            Fill = new SolidColorBrush(Colors.LightGray) { Opacity = 0.3 },
            Tag = image
        };

        Canvas.SetLeft(rect, image.Position.X * ZoomLevel);
        Canvas.SetTop(rect, image.Position.Y * ZoomLevel);

        rect.PointerPressed += OnImageRectanglePressed;

        return rect;
    }

    /// <summary>
    /// Adds selection handles around the selected image.
    /// </summary>
    private void AddSelectionHandles(ImageObject image)
    {
        var left = image.Position.X * ZoomLevel - SelectionPadding;
        var top = image.Position.Y * ZoomLevel - SelectionPadding;
        var width = image.Size.Width * ZoomLevel + SelectionPadding * 2;
        var height = image.Size.Height * ZoomLevel + SelectionPadding * 2;

        // Selection rectangle
        var selectionRect = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = new SolidColorBrush(Colors.Blue),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            Fill = null
        };
        Canvas.SetLeft(selectionRect, left);
        Canvas.SetTop(selectionRect, top);
        ManipulationCanvas.Children.Add(selectionRect);

        // Corner resize handles
        AddHandle(left, top, ManipulationMode.ResizeTopLeft);
        AddHandle(left + width, top, ManipulationMode.ResizeTopRight);
        AddHandle(left, top + height, ManipulationMode.ResizeBottomLeft);
        AddHandle(left + width, top + height, ManipulationMode.ResizeBottomRight);

        // Edge resize handles
        AddHandle(left + width / 2, top, ManipulationMode.ResizeTop);
        AddHandle(left + width / 2, top + height, ManipulationMode.ResizeBottom);
        AddHandle(left, top + height / 2, ManipulationMode.ResizeLeft);
        AddHandle(left + width, top + height / 2, ManipulationMode.ResizeRight);

        // Rotation handle
        var rotationHandle = new Ellipse
        {
            Width = HandleSize,
            Height = HandleSize,
            Fill = new SolidColorBrush(Colors.Green),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
            Tag = ManipulationMode.Rotate,
            Cursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand)
        };
        Canvas.SetLeft(rotationHandle, left + width / 2 - HandleSize / 2);
        Canvas.SetTop(rotationHandle, top - RotationHandleOffset - HandleSize / 2);
        rotationHandle.PointerPressed += OnHandlePressed;
        ManipulationCanvas.Children.Add(rotationHandle);
    }

    /// <summary>
    /// Adds a resize handle at the specified position.
    /// </summary>
    private void AddHandle(double x, double y, ManipulationMode mode)
    {
        var handle = new Rectangle
        {
            Width = HandleSize,
            Height = HandleSize,
            Fill = new SolidColorBrush(Colors.White),
            Stroke = new SolidColorBrush(Colors.Blue),
            StrokeThickness = 2,
            Tag = mode,
            Cursor = GetCursorForMode(mode)
        };
        Canvas.SetLeft(handle, x - HandleSize / 2);
        Canvas.SetTop(handle, y - HandleSize / 2);
        handle.PointerPressed += OnHandlePressed;
        ManipulationCanvas.Children.Add(handle);
    }

    /// <summary>
    /// Gets the appropriate cursor for a manipulation mode.
    /// </summary>
    private Microsoft.UI.Input.InputSystemCursor GetCursorForMode(ManipulationMode mode)
    {
        return mode switch
        {
            ManipulationMode.ResizeTopLeft or ManipulationMode.ResizeBottomRight =>
                Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeNorthwestSoutheast),
            ManipulationMode.ResizeTopRight or ManipulationMode.ResizeBottomLeft =>
                Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeNortheastSouthwest),
            ManipulationMode.ResizeTop or ManipulationMode.ResizeBottom =>
                Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth),
            ManipulationMode.ResizeLeft or ManipulationMode.ResizeRight =>
                Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast),
            _ => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow)
        };
    }

    /// <summary>
    /// Handles pointer pressed event on an image rectangle.
    /// </summary>
    private void OnImageRectanglePressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Rectangle rect && rect.Tag is ImageObject image && ViewModel != null)
        {
            ViewModel.SelectImageCommand.Execute(image);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer pressed event on the canvas.
    /// </summary>
    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel?.SelectedImage == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(ManipulationCanvas);
        var pdfPoint = new PointF(
            (float)(point.Position.X / ZoomLevel),
            (float)(point.Position.Y / ZoomLevel));

        // Check if clicking on the image area to start move operation
        var image = ViewModel.SelectedImage;
        if (IsPointInImage(pdfPoint, image))
        {
            _currentMode = ManipulationMode.Move;
            _manipulationStartPoint = pdfPoint;
            _imageStartPosition = image.Position;
            ManipulationCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer pressed event on a manipulation handle.
    /// </summary>
    private void OnHandlePressed(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel?.SelectedImage == null)
        {
            return;
        }

        if (sender is FrameworkElement handle && handle.Tag is ManipulationMode mode)
        {
            _currentMode = mode;
            var point = e.GetCurrentPoint(ManipulationCanvas);
            _manipulationStartPoint = new PointF(
                (float)(point.Position.X / ZoomLevel),
                (float)(point.Position.Y / ZoomLevel));
            _imageStartPosition = ViewModel.SelectedImage.Position;
            _imageStartSize = ViewModel.SelectedImage.Size;
            _imageStartRotation = ViewModel.SelectedImage.RotationDegrees;
            ManipulationCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved event on the canvas.
    /// </summary>
    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_currentMode == ManipulationMode.None || ViewModel?.SelectedImage == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(ManipulationCanvas);
        var currentPoint = new PointF(
            (float)(point.Position.X / ZoomLevel),
            (float)(point.Position.Y / ZoomLevel));

        switch (_currentMode)
        {
            case ManipulationMode.Move:
                HandleMove(currentPoint);
                break;
            case ManipulationMode.ResizeTopLeft:
            case ManipulationMode.ResizeTopRight:
            case ManipulationMode.ResizeBottomLeft:
            case ManipulationMode.ResizeBottomRight:
            case ManipulationMode.ResizeTop:
            case ManipulationMode.ResizeBottom:
            case ManipulationMode.ResizeLeft:
            case ManipulationMode.ResizeRight:
                HandleResize(currentPoint);
                break;
            case ManipulationMode.Rotate:
                HandleRotate(currentPoint);
                break;
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer released event on the canvas.
    /// </summary>
    private async void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_currentMode == ManipulationMode.None || ViewModel?.SelectedImage == null)
        {
            return;
        }

        var image = ViewModel.SelectedImage;

        // Apply the final transformation
        switch (_currentMode)
        {
            case ManipulationMode.Move:
                if (image.Position != _imageStartPosition)
                {
                    await ViewModel.MoveImageCommand.ExecuteAsync(image.Position);
                }
                break;
            case ManipulationMode.ResizeTopLeft:
            case ManipulationMode.ResizeTopRight:
            case ManipulationMode.ResizeBottomLeft:
            case ManipulationMode.ResizeBottomRight:
            case ManipulationMode.ResizeTop:
            case ManipulationMode.ResizeBottom:
            case ManipulationMode.ResizeLeft:
            case ManipulationMode.ResizeRight:
                if (image.Size != _imageStartSize)
                {
                    await ViewModel.ScaleImageCommand.ExecuteAsync(image.Size);
                }
                break;
            case ManipulationMode.Rotate:
                if (image.RotationDegrees != _imageStartRotation)
                {
                    var rotationDelta = image.RotationDegrees - _imageStartRotation;
                    await ViewModel.RotateImageCommand.ExecuteAsync(rotationDelta);
                }
                break;
        }

        _currentMode = ManipulationMode.None;
        ManipulationCanvas.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    /// <summary>
    /// Handles pointer exited event on the canvas.
    /// </summary>
    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Keep manipulation active even if pointer exits canvas
        // This allows for smoother dragging experience
    }

    /// <summary>
    /// Handles move operation.
    /// </summary>
    private void HandleMove(PointF currentPoint)
    {
        if (ViewModel?.SelectedImage == null)
        {
            return;
        }

        var delta = new PointF(
            currentPoint.X - _manipulationStartPoint.X,
            currentPoint.Y - _manipulationStartPoint.Y);

        var newPosition = new PointF(
            _imageStartPosition.X + delta.X,
            _imageStartPosition.Y + delta.Y);

        // Update position for real-time preview
        ViewModel.SelectedImage.Position = newPosition;
        RenderAllImages();
    }

    /// <summary>
    /// Handles resize operation.
    /// </summary>
    private void HandleResize(PointF currentPoint)
    {
        if (ViewModel?.SelectedImage == null)
        {
            return;
        }

        var delta = new PointF(
            currentPoint.X - _manipulationStartPoint.X,
            currentPoint.Y - _manipulationStartPoint.Y);

        var newSize = _imageStartSize;
        var newPosition = _imageStartPosition;

        switch (_currentMode)
        {
            case ManipulationMode.ResizeBottomRight:
                newSize = new SizeF(
                    Math.Max(10, _imageStartSize.Width + delta.X),
                    Math.Max(10, _imageStartSize.Height + delta.Y));
                break;
            case ManipulationMode.ResizeBottomLeft:
                newSize = new SizeF(
                    Math.Max(10, _imageStartSize.Width - delta.X),
                    Math.Max(10, _imageStartSize.Height + delta.Y));
                newPosition = new PointF(_imageStartPosition.X + delta.X, _imageStartPosition.Y);
                break;
            case ManipulationMode.ResizeTopRight:
                newSize = new SizeF(
                    Math.Max(10, _imageStartSize.Width + delta.X),
                    Math.Max(10, _imageStartSize.Height - delta.Y));
                newPosition = new PointF(_imageStartPosition.X, _imageStartPosition.Y + delta.Y);
                break;
            case ManipulationMode.ResizeTopLeft:
                newSize = new SizeF(
                    Math.Max(10, _imageStartSize.Width - delta.X),
                    Math.Max(10, _imageStartSize.Height - delta.Y));
                newPosition = new PointF(_imageStartPosition.X + delta.X, _imageStartPosition.Y + delta.Y);
                break;
            case ManipulationMode.ResizeRight:
                newSize = new SizeF(Math.Max(10, _imageStartSize.Width + delta.X), _imageStartSize.Height);
                break;
            case ManipulationMode.ResizeLeft:
                newSize = new SizeF(Math.Max(10, _imageStartSize.Width - delta.X), _imageStartSize.Height);
                newPosition = new PointF(_imageStartPosition.X + delta.X, _imageStartPosition.Y);
                break;
            case ManipulationMode.ResizeBottom:
                newSize = new SizeF(_imageStartSize.Width, Math.Max(10, _imageStartSize.Height + delta.Y));
                break;
            case ManipulationMode.ResizeTop:
                newSize = new SizeF(_imageStartSize.Width, Math.Max(10, _imageStartSize.Height - delta.Y));
                newPosition = new PointF(_imageStartPosition.X, _imageStartPosition.Y + delta.Y);
                break;
        }

        // Update size and position for real-time preview
        ViewModel.SelectedImage.Size = newSize;
        ViewModel.SelectedImage.Position = newPosition;
        RenderAllImages();
    }

    /// <summary>
    /// Handles rotate operation.
    /// </summary>
    private void HandleRotate(PointF currentPoint)
    {
        if (ViewModel?.SelectedImage == null)
        {
            return;
        }

        var image = ViewModel.SelectedImage;
        var center = new PointF(
            image.Position.X + image.Size.Width / 2,
            image.Position.Y + image.Size.Height / 2);

        var startAngle = Math.Atan2(
            _manipulationStartPoint.Y - center.Y,
            _manipulationStartPoint.X - center.X);
        var currentAngle = Math.Atan2(
            currentPoint.Y - center.Y,
            currentPoint.X - center.X);

        var deltaAngle = (currentAngle - startAngle) * (180.0 / Math.PI);
        var newRotation = (float)(_imageStartRotation + deltaAngle);

        // Update rotation for real-time preview
        image.RotationDegrees = newRotation;
        RenderAllImages();
    }

    /// <summary>
    /// Checks if a point is within the image bounds.
    /// </summary>
    private bool IsPointInImage(PointF point, ImageObject image)
    {
        return point.X >= image.Position.X &&
               point.X <= image.Position.X + image.Size.Width &&
               point.Y >= image.Position.Y &&
               point.Y <= image.Position.Y + image.Size.Height;
    }
}
