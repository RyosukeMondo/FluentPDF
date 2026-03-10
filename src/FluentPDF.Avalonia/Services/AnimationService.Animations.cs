using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using FluentResults;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Services;

public sealed partial class AnimationService
{
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
}
