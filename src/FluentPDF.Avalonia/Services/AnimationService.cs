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
public sealed partial class AnimationService : CoreAnimationService, IDisposable
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
