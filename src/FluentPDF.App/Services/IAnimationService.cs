using Microsoft.UI.Xaml;

namespace FluentPDF.App.Services;

/// <summary>
/// Service for managing UI animations with accessibility support.
/// Provides GPU-accelerated animations while respecting system accessibility settings.
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Gets a value indicating whether motion animations are enabled.
    /// Respects Windows "Reduce motion" accessibility setting.
    /// </summary>
    bool IsMotionEnabled { get; }

    /// <summary>
    /// Animates a page transition between old and new page views.
    /// </summary>
    /// <param name="target">The UI element to animate (typically the page container).</param>
    /// <param name="direction">Direction of the transition (forward or backward).</param>
    /// <param name="cancellationToken">Token to cancel the animation if navigation changes.</param>
    /// <returns>A task that completes when the animation finishes or is cancelled.</returns>
    Task AnimatePageTransitionAsync(
        UIElement? target,
        PageTransitionDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates a panel sliding in or out.
    /// </summary>
    /// <param name="panel">The panel element to animate.</param>
    /// <param name="direction">Direction to slide.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>A task that completes when the animation finishes.</returns>
    Task AnimatePanelSlideAsync(
        UIElement panel,
        SlideDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates an element's opacity.
    /// </summary>
    /// <param name="element">The element to fade.</param>
    /// <param name="targetOpacity">Target opacity (0.0 to 1.0).</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>A task that completes when the animation finishes.</returns>
    Task AnimateFadeAsync(
        UIElement element,
        double targetOpacity,
        int durationMs = 300,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Direction of page transition animation.
/// </summary>
public enum PageTransitionDirection
{
    /// <summary>Moving to next page (slide left).</summary>
    Forward,

    /// <summary>Moving to previous page (slide right).</summary>
    Backward,

    /// <summary>Jump to arbitrary page (fade).</summary>
    Jump
}

/// <summary>
/// Direction for panel slide animations.
/// </summary>
public enum SlideDirection
{
    /// <summary>Slide in from top.</summary>
    FromTop,

    /// <summary>Slide in from bottom.</summary>
    FromBottom,

    /// <summary>Slide in from left.</summary>
    FromLeft,

    /// <summary>Slide in from right.</summary>
    FromRight,

    /// <summary>Slide out to top.</summary>
    ToTop,

    /// <summary>Slide out to bottom.</summary>
    ToBottom,

    /// <summary>Slide out to left.</summary>
    ToLeft,

    /// <summary>Slide out to right.</summary>
    ToRight
}
