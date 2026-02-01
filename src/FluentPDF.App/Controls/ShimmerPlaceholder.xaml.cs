using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace FluentPDF.App.Controls;

/// <summary>
/// Animated shimmer placeholder for lazy-loaded content.
/// Provides visual feedback during thumbnail loading.
/// </summary>
public sealed partial class ShimmerPlaceholder : UserControl
{
    private Storyboard? _shimmerStoryboard;
    private TranslateTransform? _shimmerTransform;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShimmerPlaceholder"/> class.
    /// </summary>
    public ShimmerPlaceholder()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Starts shimmer animation when control is loaded.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Create animation programmatically to avoid XAML stub generator issues
        _shimmerTransform = new TranslateTransform { X = -200 };
        ShimmerRectangle.RenderTransform = _shimmerTransform;

        _shimmerStoryboard = new Storyboard
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        var animation = new DoubleAnimation
        {
            From = -200,
            To = 200,
            Duration = new Duration(TimeSpan.FromSeconds(1.5)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, _shimmerTransform);
        Storyboard.SetTargetProperty(animation, "X");
        _shimmerStoryboard.Children.Add(animation);
        _shimmerStoryboard.Begin();
    }

    /// <summary>
    /// Stops shimmer animation when control is unloaded.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _shimmerStoryboard?.Stop();
        _shimmerStoryboard = null;
        _shimmerTransform = null;
    }
}
