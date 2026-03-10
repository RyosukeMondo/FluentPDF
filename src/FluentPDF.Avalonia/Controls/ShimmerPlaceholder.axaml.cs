using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Threading;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Animated shimmer placeholder for lazy-loaded content.
/// Provides skeleton loading visual feedback with an animated
/// gradient sweep (like modern web skeleton loaders).
/// </summary>
public partial class ShimmerPlaceholder : UserControl
{
    public static new readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ShimmerPlaceholder, CornerRadius>(
            nameof(CornerRadius), new CornerRadius(0));

    private CancellationTokenSource? _animationCts;

    public new CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public ShimmerPlaceholder()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        StartShimmerAnimation();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;
    }

    private void StartShimmerAnimation()
    {
        var shimmerRect = this.FindControl<Rectangle>("ShimmerRectangle");
        if (shimmerRect == null) return;

        var targetWidth = Bounds.Width > 0 ? Bounds.Width : 400;

        // Animate Canvas.Left on the shimmer rectangle (not the transform).
        // RunAsync requires a Visual, so we animate the Rectangle directly.
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1.8),
            IterationCount = IterationCount.Infinite,
            Easing = new LinearEasing(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Canvas.LeftProperty, -200.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Canvas.LeftProperty, targetWidth + 200.0)
                    }
                }
            }
        };

        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();

        _ = animation.RunAsync(shimmerRect, _animationCts.Token);
    }
}
