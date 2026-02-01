namespace FluentPDF.Core.Services;

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

/// <summary>
/// UI-framework agnostic abstraction for animation services.
/// Allows ViewModels to request animations without direct framework dependencies.
/// The actual UI element is passed as object to avoid framework coupling.
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Gets a value indicating whether motion animations are enabled.
    /// Respects system accessibility settings (e.g., "Reduce motion").
    /// </summary>
    bool IsMotionEnabled { get; }

    /// <summary>
    /// Checks if the system's reduced motion setting is enabled.
    /// </summary>
    /// <returns>True if reduced motion is enabled, false otherwise.</returns>
    bool IsReducedMotionEnabled();

    /// <summary>
    /// Enables or disables animations programmatically.
    /// This does not override system accessibility settings.
    /// </summary>
    /// <param name="enabled">True to enable animations, false to disable.</param>
    void SetMotionEnabled(bool enabled);

    /// <summary>
    /// Animates a page transition.
    /// </summary>
    /// <param name="target">The UI element to animate (framework-specific type).</param>
    /// <param name="direction">Direction of the transition.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>A task that completes when the animation finishes or is cancelled.</returns>
    Task AnimatePageTransitionAsync(
        object? target,
        PageTransitionDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates a panel sliding in or out.
    /// </summary>
    /// <param name="panel">The panel element to animate (framework-specific type).</param>
    /// <param name="direction">Direction to slide.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>A task that completes when the animation finishes.</returns>
    Task AnimatePanelSlideAsync(
        object panel,
        SlideDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates an element's opacity.
    /// </summary>
    /// <param name="element">The element to fade (framework-specific type).</param>
    /// <param name="targetOpacity">Target opacity (0.0 to 1.0).</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>A task that completes when the animation finishes.</returns>
    Task AnimateFadeAsync(
        object element,
        double targetOpacity,
        int durationMs = 300,
        CancellationToken cancellationToken = default);
}
