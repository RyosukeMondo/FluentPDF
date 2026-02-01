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

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Implements centralized animation orchestration with accessibility support.
/// Detects reduced motion settings and provides GPU-accelerated animations.
/// Automatically adapts to performance: disables animations if FPS &lt; 30 for &gt; 60 frames,
/// re-enables when FPS &gt; 45 for 5 seconds.
/// </summary>
public sealed class AnimationService : IAnimationService, IDisposable
{
    private readonly ILogger<AnimationService> _logger;
    private readonly BehaviorSubject<bool> _motionStateSubject;
    private readonly IDisposable? _performanceSubscription;
    private bool _disposed;
    private bool _manuallyDisabled;
    private bool _performanceDisabled;
    private int _lowFpsFrameCount;
    private int _highFpsFrameCount;
    private const int LowFpsThreshold = 30;
    private const int HighFpsThreshold = 45;
    private const int LowFpsFramesToDisable = 60; // ~1 second at 60 FPS
    private const int HighFpsFramesToEnable = 300; // ~5 seconds at 60 FPS

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking animation operations.</param>
    /// <param name="performanceMonitor">Optional performance monitor for adaptive quality.</param>
    public AnimationService(
        ILogger<AnimationService> logger,
        IPerformanceMonitor? performanceMonitor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var reducedMotion = IsReducedMotionEnabled();
        var initialState = !reducedMotion;
        _motionStateSubject = new BehaviorSubject<bool>(initialState);
        _manuallyDisabled = false;
        _performanceDisabled = false;
        _lowFpsFrameCount = 0;
        _highFpsFrameCount = 0;

        // Subscribe to performance monitor if available
        if (performanceMonitor != null)
        {
            _performanceSubscription = performanceMonitor.MetricsStream
                .Subscribe(
                    metrics => OnPerformanceMetrics(metrics),
                    ex =>
                    {
                        var correlationId = Guid.NewGuid();
                        Log.Error(ex,
                            "Performance monitoring stream error [CorrelationId: {CorrelationId}]",
                            correlationId);
                    });

            var perfCorrelationId = Guid.NewGuid();
            Log.Information(
                "AnimationService subscribed to PerformanceMonitor [CorrelationId: {CorrelationId}]",
                perfCorrelationId);
        }

        var correlationId = Guid.NewGuid();
        Log.Information(
            "AnimationService initialized. ReducedMotion: {ReducedMotion}, MotionEnabled: {MotionEnabled}, PerformanceMonitoring: {PerformanceMonitoring} [CorrelationId: {CorrelationId}]",
            reducedMotion, initialState, performanceMonitor != null, correlationId);
    }

    /// <inheritdoc/>
    public bool IsMotionEnabled => _motionStateSubject.Value && !_manuallyDisabled && !_performanceDisabled;

    /// <inheritdoc/>
    public IObservable<bool> ObserveMotionState()
    {
        return _motionStateSubject.AsObservable();
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
            var effectiveState = enabled && !reducedMotion && !_performanceDisabled;

            _motionStateSubject.OnNext(effectiveState);

            Log.Information(
                "Motion state updated. ManuallyDisabled: {ManuallyDisabled}, ReducedMotion: {ReducedMotion}, PerformanceDisabled: {PerformanceDisabled}, Effective: {Effective} [CorrelationId: {CorrelationId}]",
                _manuallyDisabled, reducedMotion, _performanceDisabled, effectiveState, correlationId);

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

    /// <inheritdoc/>
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
    /// Handles performance metrics updates from PerformanceMonitor.
    /// Implements hysteresis logic for adaptive animation quality:
    /// - Disables animations if FPS &lt; 30 for &gt; 60 consecutive frames (~1 second)
    /// - Re-enables animations if FPS &gt; 45 for &gt; 300 consecutive frames (~5 seconds)
    /// </summary>
    /// <param name="metrics">The latest performance metrics.</param>
    private void OnPerformanceMetrics(PerformanceMetrics metrics)
    {
        try
        {
            var fps = metrics.CurrentFps;

            // Track low FPS frames
            if (fps < LowFpsThreshold)
            {
                _lowFpsFrameCount++;
                _highFpsFrameCount = 0; // Reset high FPS counter

                // Disable animations if sustained low FPS detected
                if (!_performanceDisabled && _lowFpsFrameCount >= LowFpsFramesToDisable)
                {
                    _performanceDisabled = true;
                    var correlationId = Guid.NewGuid();

                    Log.Warning(
                        "Performance degradation detected: FPS {Fps:F1} < {Threshold} for {FrameCount} frames (~{Duration}s). Disabling animations. [CorrelationId: {CorrelationId}]",
                        fps,
                        LowFpsThreshold,
                        _lowFpsFrameCount,
                        _lowFpsFrameCount / 60.0,
                        correlationId);

                    // Update motion state to notify observers
                    var effectiveState = !IsReducedMotionEnabled() && !_manuallyDisabled && !_performanceDisabled;
                    _motionStateSubject.OnNext(effectiveState);

                    Log.Information(
                        "Animations disabled due to performance. EffectiveMotionState: {EffectiveState} [CorrelationId: {CorrelationId}]",
                        effectiveState,
                        correlationId);
                }
            }
            // Track high FPS frames for re-enabling
            else if (fps >= HighFpsThreshold)
            {
                _highFpsFrameCount++;
                _lowFpsFrameCount = 0; // Reset low FPS counter

                // Re-enable animations if sustained high FPS detected
                if (_performanceDisabled && _highFpsFrameCount >= HighFpsFramesToEnable)
                {
                    _performanceDisabled = false;
                    _highFpsFrameCount = 0;
                    var correlationId = Guid.NewGuid();

                    Log.Information(
                        "Performance recovered: FPS {Fps:F1} > {Threshold} for {FrameCount} frames (~{Duration}s). Re-enabling animations. [CorrelationId: {CorrelationId}]",
                        fps,
                        HighFpsThreshold,
                        HighFpsFramesToEnable,
                        HighFpsFramesToEnable / 60.0,
                        correlationId);

                    // Update motion state to notify observers
                    var effectiveState = !IsReducedMotionEnabled() && !_manuallyDisabled && !_performanceDisabled;
                    _motionStateSubject.OnNext(effectiveState);

                    Log.Information(
                        "Animations re-enabled after performance recovery. EffectiveMotionState: {EffectiveState} [CorrelationId: {CorrelationId}]",
                        effectiveState,
                        correlationId);
                }
            }
            // In the middle range (30-45 FPS) - reset both counters
            else
            {
                _lowFpsFrameCount = 0;
                _highFpsFrameCount = 0;
            }
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex,
                "Error processing performance metrics [CorrelationId: {CorrelationId}]",
                correlationId);
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

        _performanceSubscription?.Dispose();
        _motionStateSubject?.Dispose();
        _disposed = true;

        var correlationId = Guid.NewGuid();
        Log.Information(
            "AnimationService disposed [CorrelationId: {CorrelationId}]",
            correlationId);
    }
}
