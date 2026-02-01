using Avalonia.Controls;
using FluentResults;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Defines animation types for page transitions.
/// </summary>
public enum PageTransitionType
{
    /// <summary>
    /// Slide animation with 350ms duration.
    /// </summary>
    Slide,

    /// <summary>
    /// Fade animation with 300ms duration.
    /// </summary>
    Fade,

    /// <summary>
    /// Zoom animation with 400ms duration.
    /// </summary>
    Zoom
}

/// <summary>
/// Defines slide direction for panel animations.
/// </summary>
public enum SlideDirection
{
    /// <summary>
    /// Slide from top to bottom.
    /// </summary>
    FromTop,

    /// <summary>
    /// Slide from bottom to top.
    /// </summary>
    FromBottom,

    /// <summary>
    /// Slide from left to right.
    /// </summary>
    FromLeft,

    /// <summary>
    /// Slide from right to left.
    /// </summary>
    FromRight
}

/// <summary>
/// Provides centralized animation orchestration with accessibility support.
/// All animations respect the Windows reduced motion setting and maintain 60 FPS.
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Gets whether animations are currently enabled.
    /// Returns false if reduced motion is detected or animations are disabled.
    /// </summary>
    bool IsMotionEnabled { get; }

    /// <summary>
    /// Observes changes to motion enabled state as a reactive stream.
    /// Emits when reduced motion setting changes or motion is disabled.
    /// </summary>
    /// <returns>Observable stream of motion enabled state changes.</returns>
    IObservable<bool> ObserveMotionState();

    /// <summary>
    /// Animates a page transition with the specified type.
    /// Duration depends on transition type:
    /// - Slide: 350ms
    /// - Fade: 300ms
    /// - Zoom: 400ms
    /// </summary>
    /// <param name="control">The control to animate.</param>
    /// <param name="transitionType">The type of page transition.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>Success result, or failure with error details.</returns>
    Task<Result> AnimatePageTransitionAsync(
        Control control,
        PageTransitionType transitionType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates a panel sliding in or out with 250ms duration.
    /// </summary>
    /// <param name="control">The control to animate.</param>
    /// <param name="direction">The slide direction.</param>
    /// <param name="slideIn">True to slide in, false to slide out.</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>Success result, or failure with error details.</returns>
    Task<Result> AnimatePanelSlideAsync(
        Control control,
        SlideDirection direction,
        bool slideIn = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Animates a fade in or fade out effect.
    /// </summary>
    /// <param name="control">The control to animate.</param>
    /// <param name="fadeIn">True to fade in, false to fade out.</param>
    /// <param name="durationMs">Duration in milliseconds (default 300ms).</param>
    /// <param name="cancellationToken">Token to cancel the animation.</param>
    /// <returns>Success result, or failure with error details.</returns>
    Task<Result> AnimateFadeAsync(
        Control control,
        bool fadeIn = true,
        int durationMs = 300,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables animations.
    /// This does not override the reduced motion accessibility setting.
    /// </summary>
    /// <param name="enabled">True to enable animations, false to disable.</param>
    /// <returns>Success result, or failure with error details.</returns>
    Result SetMotionEnabled(bool enabled);

    /// <summary>
    /// Checks if the Windows reduced motion setting is enabled.
    /// </summary>
    /// <returns>True if reduced motion is enabled, false otherwise.</returns>
    bool IsReducedMotionEnabled();
}
