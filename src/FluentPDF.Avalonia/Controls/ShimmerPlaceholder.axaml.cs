using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;
using System;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Animated shimmer placeholder for lazy-loaded content.
/// Uses a DispatcherTimer to sweep a gradient rectangle across the control.
/// Avalonia's Animation.RunAsync does not support IterationCount.Infinite,
/// so we drive the animation manually.
/// </summary>
public partial class ShimmerPlaceholder : UserControl
{
    public static new readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ShimmerPlaceholder, CornerRadius>(
            nameof(CornerRadius), new CornerRadius(0));

    private DispatcherTimer? _shimmerTimer;
    private double _shimmerPosition = -200;

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
        _shimmerTimer?.Stop();
        _shimmerTimer = null;
    }

    private void StartShimmerAnimation()
    {
        var shimmerRect = this.FindControl<Rectangle>("ShimmerRectangle");
        if (shimmerRect == null) return;

        const double durationSeconds = 1.8;
        const int fps = 60;
        const double shimmerWidth = 200.0;

        _shimmerPosition = -shimmerWidth;

        _shimmerTimer?.Stop();
        _shimmerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / fps) };
        _shimmerTimer.Tick += (_, _) =>
        {
            var totalWidth = Bounds.Width > 0 ? Bounds.Width : 400;
            var totalDistance = totalWidth + 2 * shimmerWidth;
            var step = totalDistance / (durationSeconds * fps);

            _shimmerPosition += step;
            if (_shimmerPosition > totalWidth + shimmerWidth)
                _shimmerPosition = -shimmerWidth;

            Canvas.SetLeft(shimmerRect, _shimmerPosition);
        };
        _shimmerTimer.Start();
    }
}
