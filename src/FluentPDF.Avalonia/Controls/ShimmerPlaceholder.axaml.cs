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
/// Provides visual feedback during thumbnail loading.
/// </summary>
public partial class ShimmerPlaceholder : UserControl
{
    private Animation? _shimmerAnimation;

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
        var shimmerRect = this.FindControl<Rectangle>("ShimmerRectangle");
        if (shimmerRect == null) return;

        // Create shimmer animation
        _shimmerAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1.5),
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
                        new Setter(TranslateTransform.XProperty, Bounds.Width + 200.0)
                    }
                }
            },
            Easing = new CubicEaseOut()
        };

        // Start animation
        var transform = shimmerRect.RenderTransform as TranslateTransform;
        if (transform != null)
        {
            _ = _shimmerAnimation.RunAsync(transform);
        }
    }

    /// <summary>
    /// Stops shimmer animation when control is detached from visual tree.
    /// </summary>
    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Avalonia animations clean up automatically
        _shimmerAnimation = null;
    }
}
