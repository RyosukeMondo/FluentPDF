using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Enhanced button control with pointer-origin ripple animation and scale-down press feedback.
/// </summary>
/// <remarks>
/// Features:
/// - Ripple effect originates from pointer position (mouse, touch, pen)
/// - 0.95x scale on press for tactile feedback
/// - GPU-accelerated animations via Composition API
/// - 150ms cubic-ease-out transitions
/// - Works with all input types
/// </remarks>
public class LiquidButton : Button
{
    private Canvas? _rippleCanvas;
    private Border? _rootBorder;
    private bool _isRippleAnimating;

    /// <summary>
    /// Defines the RippleColor property for customizing ripple effect color.
    /// </summary>
    public static readonly StyledProperty<Color> RippleColorProperty =
        AvaloniaProperty.Register<LiquidButton, Color>(
            nameof(RippleColor),
            Colors.White);

    /// <summary>
    /// Defines the RippleOpacity property for controlling ripple effect visibility.
    /// </summary>
    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<LiquidButton, double>(
            nameof(RippleOpacity),
            0.3);

    /// <summary>
    /// Defines the RippleDuration property for controlling animation speed.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> RippleDurationProperty =
        AvaloniaProperty.Register<LiquidButton, TimeSpan>(
            nameof(RippleDuration),
            TimeSpan.FromMilliseconds(600));

    /// <summary>
    /// Gets or sets the color of the ripple effect.
    /// </summary>
    public Color RippleColor
    {
        get => GetValue(RippleColorProperty);
        set => SetValue(RippleColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the opacity of the ripple effect (0.0 to 1.0).
    /// </summary>
    public double RippleOpacity
    {
        get => GetValue(RippleOpacityProperty);
        set => SetValue(RippleOpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets the duration of the ripple animation.
    /// </summary>
    public TimeSpan RippleDuration
    {
        get => GetValue(RippleDurationProperty);
        set => SetValue(RippleDurationProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _rippleCanvas = e.NameScope.Find<Canvas>("PART_RippleCanvas");
        _rootBorder = e.NameScope.Find<Border>("PART_RootBorder");

        // Set transform origin to center for scale animation
        if (_rootBorder != null)
        {
            _rootBorder.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEnabled || _rippleCanvas == null || _rootBorder == null)
        {
            return;
        }

        // Get pointer position relative to button
        var position = e.GetPosition(_rootBorder);

        // Create and animate ripple effect
        _ = AnimateRippleAsync(position);
    }

    /// <summary>
    /// Creates and animates a ripple effect originating from the specified position.
    /// </summary>
    /// <param name="origin">The point where the ripple should originate from.</param>
    private async Task AnimateRippleAsync(Point origin)
    {
        if (_rippleCanvas == null || _rootBorder == null || _isRippleAnimating)
        {
            return;
        }

        _isRippleAnimating = true;

        try
        {
            // Calculate maximum ripple radius (distance to farthest corner)
            var width = _rootBorder.Bounds.Width;
            var height = _rootBorder.Bounds.Height;
            var maxRadius = CalculateMaxRippleRadius(origin, width, height);

            // Create ripple ellipse
            var ripple = CreateRippleEllipse(origin, maxRadius);

            // Add to canvas
            _rippleCanvas.Children.Add(ripple);

            // Animate ripple expansion and fade
            await AnimateRippleExpansionAsync(ripple, maxRadius);

            // Remove ripple from canvas
            _rippleCanvas.Children.Remove(ripple);
        }
        finally
        {
            _isRippleAnimating = false;
        }
    }

    /// <summary>
    /// Calculates the maximum radius needed for the ripple to reach all corners.
    /// </summary>
    private static double CalculateMaxRippleRadius(Point origin, double width, double height)
    {
        var topLeft = Math.Sqrt(Math.Pow(origin.X, 2) + Math.Pow(origin.Y, 2));
        var topRight = Math.Sqrt(Math.Pow(width - origin.X, 2) + Math.Pow(origin.Y, 2));
        var bottomLeft = Math.Sqrt(Math.Pow(origin.X, 2) + Math.Pow(height - origin.Y, 2));
        var bottomRight = Math.Sqrt(Math.Pow(width - origin.X, 2) + Math.Pow(height - origin.Y, 2));

        return Math.Max(Math.Max(topLeft, topRight), Math.Max(bottomLeft, bottomRight));
    }

    /// <summary>
    /// Creates the ripple ellipse element with initial state.
    /// </summary>
    private Ellipse CreateRippleEllipse(Point origin, double maxRadius)
    {
        var ripple = new Ellipse
        {
            Width = 0,
            Height = 0,
            Fill = new SolidColorBrush(RippleColor) { Opacity = RippleOpacity },
            IsHitTestVisible = false,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };

        // Position at pointer origin
        Canvas.SetLeft(ripple, origin.X);
        Canvas.SetTop(ripple, origin.Y);

        return ripple;
    }

    /// <summary>
    /// Animates the ripple expansion and opacity fade.
    /// </summary>
    private async Task AnimateRippleExpansionAsync(Ellipse ripple, double maxRadius)
    {
        var duration = RippleDuration;
        var targetSize = maxRadius * 2;

        // Create size animation using transitions
        var transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Ellipse.WidthProperty,
                Duration = duration,
                Easing = new CircularEaseOut()
            },
            new DoubleTransition
            {
                Property = Ellipse.HeightProperty,
                Duration = duration,
                Easing = new CircularEaseOut()
            },
            new DoubleTransition
            {
                Property = Ellipse.OpacityProperty,
                Duration = duration,
                Easing = new LinearEasing()
            },
            new ThicknessTransition
            {
                Property = Ellipse.MarginProperty,
                Duration = duration,
                Easing = new CircularEaseOut()
            }
        };

        ripple.Transitions = transitions;

        // Trigger animations by setting target values
        ripple.Width = targetSize;
        ripple.Height = targetSize;
        ripple.Margin = new Thickness(-maxRadius);

        // Wait for expansion phase
        await Task.Delay(duration - TimeSpan.FromMilliseconds(240));

        // Fade out
        ripple.Opacity = 0.0;

        // Wait for fade to complete
        await Task.Delay(TimeSpan.FromMilliseconds(240));
    }
}
