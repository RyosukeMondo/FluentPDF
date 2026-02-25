using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using FluentResults;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreAnimationService = FluentPDF.Core.Services.IAnimationService;
using CorePageTransitionDirection = FluentPDF.Core.Services.PageTransitionDirection;
using CoreSlideDirection = FluentPDF.Core.Services.SlideDirection;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Defines animation types for page transitions.
/// </summary>
public enum PageTransitionType
{
    /// <summary>Slide animation with 350ms duration.</summary>
    Slide,

    /// <summary>Fade animation with 300ms duration.</summary>
    Fade,

    /// <summary>Zoom animation with 400ms duration.</summary>
    Zoom
}

/// <summary>
/// Defines slide direction for panel animations.
/// </summary>
public enum SlideDirection
{
    /// <summary>Slide from top to bottom.</summary>
    FromTop,

    /// <summary>Slide from bottom to top.</summary>
    FromBottom,

    /// <summary>Slide from left to right.</summary>
    FromLeft,

    /// <summary>Slide from right to left.</summary>
    FromRight
}

/// <summary>
/// Implements centralized animation orchestration with accessibility support.
/// Detects reduced motion settings and provides GPU-accelerated animations.
/// Implements both the Core IAnimationService (for ViewModel injection) and
/// provides Avalonia-specific animation methods using Control types.
/// </summary>
public sealed class AnimationService : CoreAnimationService, IDisposable
{
    private readonly ILogger<AnimationService> _logger;
    private readonly BehaviorSubject<bool> _motionStateSubject;
    private bool _disposed;
    private bool _manuallyDisabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking animation operations.</param>
    public AnimationService(ILogger<AnimationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var reducedMotion = IsReducedMotionEnabled();
        var initialState = !reducedMotion;
        _motionStateSubject = new BehaviorSubject<bool>(initialState);
        _manuallyDisabled = false;

        var correlationId = Guid.NewGuid();
        Log.Information(
            "AnimationService initialized. ReducedMotion: {ReducedMotion}, MotionEnabled: {MotionEnabled} [CorrelationId: {CorrelationId}]",
            reducedMotion, initialState, correlationId);
    }

    /// <inheritdoc/>
    public bool IsMotionEnabled => _motionStateSubject.Value && !_manuallyDisabled;

    /// <summary>
    /// Observes changes to motion enabled state as a reactive stream.
    /// </summary>
    public IObservable<bool> ObserveMotionState()
    {
        return _motionStateSubject.AsObservable();
    }

    #region Core IAnimationService implementation (for ViewModel injection)

    /// <inheritdoc/>
    async Task CoreAnimationService.AnimatePageTransitionAsync(
        object? target,
        CorePageTransitionDirection direction,
        CancellationToken cancellationToken)
    {
        if (target is Control control)
        {
            var transitionType = direction == CorePageTransitionDirection.Jump
                ? PageTransitionType.Fade
                : PageTransitionType.Slide;
            await AnimatePageTransitionAsync(control, transitionType, cancellationToken);
        }
        // When target is null (ViewModel calls), this is a no-op
    }

    /// <inheritdoc/>
    async Task CoreAnimationService.AnimatePanelSlideAsync(
        object panel,
        CoreSlideDirection direction,
        CancellationToken cancellationToken)
    {
        if (panel is Control control)
        {
            var slideDir = direction switch
            {
                CoreSlideDirection.FromTop => SlideDirection.FromTop,
                CoreSlideDirection.FromBottom => SlideDirection.FromBottom,
                CoreSlideDirection.FromLeft => SlideDirection.FromLeft,
                CoreSlideDirection.FromRight => SlideDirection.FromRight,
                // "To" directions map to slide-out with matching origin
                CoreSlideDirection.ToTop => SlideDirection.FromTop,
                CoreSlideDirection.ToBottom => SlideDirection.FromBottom,
                CoreSlideDirection.ToLeft => SlideDirection.FromLeft,
                CoreSlideDirection.ToRight => SlideDirection.FromRight,
                _ => SlideDirection.FromLeft
            };
            var slideIn = direction is CoreSlideDirection.FromTop or CoreSlideDirection.FromBottom
                or CoreSlideDirection.FromLeft or CoreSlideDirection.FromRight;
            await AnimatePanelSlideAsync(control, slideDir, slideIn, cancellationToken);
        }
    }

    /// <inheritdoc/>
    async Task CoreAnimationService.AnimateFadeAsync(
        object element,
        double targetOpacity,
        int durationMs,
        CancellationToken cancellationToken)
    {
        if (element is Control control)
        {
            await AnimateFadeAsync(control, targetOpacity > 0.5, durationMs, cancellationToken);
        }
    }

    /// <inheritdoc/>
    void CoreAnimationService.SetMotionEnabled(bool enabled)
    {
        SetMotionEnabled(enabled);
    }

    #endregion

    /// <summary>
    /// Animates a page transition with the specified type.
    /// </summary>
    public async Task<Result> AnimatePageTransitionAsync(
        Control control,
        PageTransitionType transitionType,
        CancellationToken cancellationToken = default)
    {
        if (control == null)
        {
            return Result.Fail("Control cannot be null");
        }

        if (!IsMotionEnabled)
        {
            return Result.Ok();
        }

        try
        {
            var correlationId = Guid.NewGuid();
            Log.Debug(
                "Starting page transition animation: {Type} [CorrelationId: {CorrelationId}]",
                transitionType, correlationId);

            var result = transitionType switch
            {
                PageTransitionType.Slide => await AnimateSlideTransitionAsync(
                    control, 350, cancellationToken),
                PageTransitionType.Fade => await AnimateFadeAsync(
                    control, true, 300, cancellationToken),
                PageTransitionType.Zoom => await AnimateZoomTransitionAsync(
                    control, 400, cancellationToken),
                _ => Result.Fail($"Unknown transition type: {transitionType}")
            };

            if (result.IsSuccess)
            {
                Log.Debug(
                    "Page transition completed: {Type} [CorrelationId: {CorrelationId}]",
                    transitionType, correlationId);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Page transition animation cancelled");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex,
                "Failed to animate page transition [CorrelationId: {CorrelationId}]",
                correlationId);
            return Result.Fail($"Failed to animate page transition: {ex.Message}");
        }
    }

    /// <summary>
    /// Animates a panel sliding in or out with 250ms duration.
    /// </summary>
    public async Task<Result> AnimatePanelSlideAsync(
        Control control,
        SlideDirection direction,
        bool slideIn = true,
        CancellationToken cancellationToken = default)
    {
        if (control == null)
        {
            return Result.Fail("Control cannot be null");
        }

        if (!IsMotionEnabled)
        {
            control.Opacity = slideIn ? 1.0 : 0.0;
            control.IsVisible = slideIn;
            return Result.Ok();
        }

        try
        {
            var correlationId = Guid.NewGuid();
            Log.Debug(
                "Starting panel slide animation: {Direction}, SlideIn: {SlideIn} [CorrelationId: {CorrelationId}]",
                direction, slideIn, correlationId);

            var (startX, startY, endX, endY) = GetSlideOffsets(direction, control, slideIn);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters =
                        {
                            new Setter(TranslateTransform.XProperty, startX),
                            new Setter(TranslateTransform.YProperty, startY),
                            new Setter(Visual.OpacityProperty, slideIn ? 0.0 : 1.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters =
                        {
                            new Setter(TranslateTransform.XProperty, endX),
                            new Setter(TranslateTransform.YProperty, endY),
                            new Setter(Visual.OpacityProperty, slideIn ? 1.0 : 0.0)
                        }
                    }
                }
            };

            if (control.RenderTransform is not TranslateTransform)
            {
                control.RenderTransform = new TranslateTransform();
            }

            control.IsVisible = true;
            await animation.RunAsync(control, cancellationToken);

            if (!slideIn)
            {
                control.IsVisible = false;
            }

            Log.Debug(
                "Panel slide completed [CorrelationId: {CorrelationId}]",
                correlationId);

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Panel slide animation cancelled");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex,
                "Failed to animate panel slide [CorrelationId: {CorrelationId}]",
                correlationId);
            return Result.Fail($"Failed to animate panel slide: {ex.Message}");
        }
    }

    /// <summary>
    /// Animates a fade in or fade out effect.
    /// </summary>
    public async Task<Result> AnimateFadeAsync(
        Control control,
        bool fadeIn = true,
        int durationMs = 300,
        CancellationToken cancellationToken = default)
    {
        if (control == null)
        {
            return Result.Fail("Control cannot be null");
        }

        if (durationMs <= 0)
        {
            return Result.Fail("Duration must be positive");
        }

        if (!IsMotionEnabled)
        {
            control.Opacity = fadeIn ? 1.0 : 0.0;
            control.IsVisible = fadeIn;
            return Result.Ok();
        }

        try
        {
            var correlationId = Guid.NewGuid();
            Log.Debug(
                "Starting fade animation: FadeIn: {FadeIn}, Duration: {Duration}ms [CorrelationId: {CorrelationId}]",
                fadeIn, durationMs, correlationId);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(durationMs),
                Easing = new LinearEasing(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, fadeIn ? 0.0 : 1.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, fadeIn ? 1.0 : 0.0)
                        }
                    }
                }
            };

            control.IsVisible = true;
            await animation.RunAsync(control, cancellationToken);

            if (!fadeIn)
            {
                control.IsVisible = false;
            }

            Log.Debug(
                "Fade animation completed [CorrelationId: {CorrelationId}]",
                correlationId);

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Fade animation cancelled");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex,
                "Failed to animate fade [CorrelationId: {CorrelationId}]",
                correlationId);
            return Result.Fail($"Failed to animate fade: {ex.Message}");
        }
    }

    /// <summary>
    /// Enables or disables animations.
    /// </summary>
    public Result SetMotionEnabled(bool enabled)
    {
        try
        {
            var correlationId = Guid.NewGuid();
            Log.Information(
                "Setting motion enabled: {Enabled} [CorrelationId: {CorrelationId}]",
                enabled, correlationId);

            _manuallyDisabled = !enabled;
            var reducedMotion = IsReducedMotionEnabled();
            var effectiveState = enabled && !reducedMotion;

            _motionStateSubject.OnNext(effectiveState);

            Log.Information(
                "Motion state updated. ManuallyDisabled: {ManuallyDisabled}, ReducedMotion: {ReducedMotion}, Effective: {Effective} [CorrelationId: {CorrelationId}]",
                _manuallyDisabled, reducedMotion, effectiveState, correlationId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex,
                "Failed to set motion enabled [CorrelationId: {CorrelationId}]",
                correlationId);
            return Result.Fail($"Failed to set motion enabled: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the system's reduced motion setting is enabled.
    /// </summary>
    public bool IsReducedMotionEnabled()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsReducedMotion();
            }

            // Default to false for non-Windows platforms
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check reduced motion setting, defaulting to false");
            return false;
        }
    }

    /// <summary>
    /// Animates a slide transition for page navigation.
    /// </summary>
    private async Task<Result> AnimateSlideTransitionAsync(
        Control control,
        int durationMs,
        CancellationToken cancellationToken)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, -50.0),
                        new Setter(Visual.OpacityProperty, 0.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, 0.0),
                        new Setter(Visual.OpacityProperty, 1.0)
                    }
                }
            }
        };

        if (control.RenderTransform is not TranslateTransform)
        {
            control.RenderTransform = new TranslateTransform();
        }

        await animation.RunAsync(control, cancellationToken);
        return Result.Ok();
    }

    /// <summary>
    /// Animates a zoom transition for page navigation.
    /// </summary>
    private async Task<Result> AnimateZoomTransitionAsync(
        Control control,
        int durationMs,
        CancellationToken cancellationToken)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, 0.8),
                        new Setter(ScaleTransform.ScaleYProperty, 0.8),
                        new Setter(Visual.OpacityProperty, 0.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, 1.0),
                        new Setter(ScaleTransform.ScaleYProperty, 1.0),
                        new Setter(Visual.OpacityProperty, 1.0)
                    }
                }
            }
        };

        if (control.RenderTransform is not ScaleTransform)
        {
            control.RenderTransform = new ScaleTransform();
            control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }

        await animation.RunAsync(control, cancellationToken);
        return Result.Ok();
    }

    /// <summary>
    /// Gets slide offsets based on direction and slide in/out state.
    /// </summary>
    private static (double startX, double startY, double endX, double endY) GetSlideOffsets(
        SlideDirection direction,
        Control control,
        bool slideIn)
    {
        var distance = direction switch
        {
            SlideDirection.FromTop or SlideDirection.FromBottom => control.Bounds.Height,
            SlideDirection.FromLeft or SlideDirection.FromRight => control.Bounds.Width,
            _ => 100.0
        };

        return direction switch
        {
            SlideDirection.FromTop => slideIn
                ? (-distance, 0.0, 0.0, 0.0)
                : (0.0, 0.0, -distance, 0.0),
            SlideDirection.FromBottom => slideIn
                ? (distance, 0.0, 0.0, 0.0)
                : (0.0, 0.0, distance, 0.0),
            SlideDirection.FromLeft => slideIn
                ? (0.0, -distance, 0.0, 0.0)
                : (0.0, 0.0, 0.0, -distance),
            SlideDirection.FromRight => slideIn
                ? (0.0, distance, 0.0, 0.0)
                : (0.0, 0.0, 0.0, distance),
            _ => (0.0, 0.0, 0.0, 0.0)
        };
    }

    /// <summary>
    /// Checks Windows registry for reduced motion setting.
    /// </summary>
    private static bool GetWindowsReducedMotion()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\ReduceMotion");

            if (key == null)
            {
                return false;
            }

            var value = key.GetValue("ReduceMotion");
            return value is int intValue && intValue == 1;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read Windows reduced motion setting");
            return false;
        }
    }

    /// <summary>
    /// Disposes resources used by the AnimationService.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _motionStateSubject?.Dispose();
        _disposed = true;

        var correlationId = Guid.NewGuid();
        Log.Information(
            "AnimationService disposed [CorrelationId: {CorrelationId}]",
            correlationId);
    }
}
