using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using System;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Animated shimmer placeholder for lazy-loaded content.
/// Provides skeleton loading visual feedback with an animated
/// gradient sweep (like modern web skeleton loaders).
/// </summary>
public partial class ShimmerPlaceholder : UserControl
{
    /// <summary>
    /// Defines the CornerRadius property for rounded skeleton shapes.
    /// </summary>
    public static new readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ShimmerPlaceholder, CornerRadius>(
            nameof(CornerRadius), new CornerRadius(0));

    private Animation? _shimmerAnimation;

    /// <summary>
    /// Gets or sets the corner radius of the shimmer placeholder.
    /// </summary>
    public new CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShimmerPlaceholder"/> class.
    /// </summary>
    public ShimmerPlaceholder()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    /// <summary>
    /// Starts shimmer animation when control is attached to visual tree.
    /// </summary>
    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        StartShimmerAnimation();
    }

    /// <summary>
    /// Stops shimmer animation when control is detached from visual tree.
    /// </summary>
    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _shimmerAnimation = null;
    }

    private void StartShimmerAnimation()
    {
        var shimmerRect = this.FindControl<Rectangle>("ShimmerRectangle");
        if (shimmerRect == null) return;

        var targetWidth = Bounds.Width > 0 ? Bounds.Width : 400;

        _shimmerAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1.8),
            IterationCount = IterationCount.Infinite,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, -200.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, targetWidth + 200.0)
                    }
                }
            },
            Easing = new LinearEasing()
        };

        var transform = shimmerRect.RenderTransform as TranslateTransform;
        if (transform != null)
        {
            _ = _shimmerAnimation.RunAsync(transform);
        }
    }
}
