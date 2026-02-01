using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;
using System.Numerics;

namespace FluentPDF.App.Services;

/// <summary>
/// WinUI 3 implementation of IAnimationService using Composition API.
/// Provides GPU-accelerated animations with accessibility support.
/// </summary>
public sealed class AnimationService : IAnimationService
{
    private readonly ILogger<AnimationService> _logger;
    private readonly UISettings _uiSettings;
    private readonly bool _isMotionEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for animation events.</param>
    public AnimationService(ILogger<AnimationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiSettings = new UISettings();

        // Detect Windows "Reduce motion" accessibility setting
        _isMotionEnabled = DetectMotionEnabled();

        _logger.LogInformation(
            "AnimationService initialized. MotionEnabled={IsMotionEnabled}",
            _isMotionEnabled);
    }

    /// <inheritdoc/>
    public bool IsMotionEnabled => _isMotionEnabled;

    /// <inheritdoc/>
    public async Task AnimatePageTransitionAsync(
        UIElement? target,
        PageTransitionDirection direction,
        CancellationToken cancellationToken = default)
    {
        if (target == null)
        {
            _logger.LogDebug("AnimatePageTransition skipped: target is null");
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogDebug(
            "AnimatePageTransition starting. Direction={Direction}, CorrelationId={CorrelationId}",
            direction,
            correlationId);

        if (!_isMotionEnabled)
        {
            _logger.LogDebug(
                "AnimatePageTransition skipped: motion disabled. CorrelationId={CorrelationId}",
                correlationId);
            return;
        }

        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(target);
            var compositor = visual.Compositor;

            // Create animation based on direction
            CompositionAnimation? animation = direction switch
            {
                PageTransitionDirection.Forward => CreateSlideAnimation(
                    compositor, fromX: 50, toX: 0, duration: 350),
                PageTransitionDirection.Backward => CreateSlideAnimation(
                    compositor, fromX: -50, toX: 0, duration: 350),
                PageTransitionDirection.Jump => CreateFadeAnimation(
                    compositor, duration: 300),
                _ => null
            };

            if (animation == null)
            {
                return;
            }

            // Start animation
            visual.StartAnimation("Translation", animation);

            // Wait for animation to complete or be cancelled
            await Task.Delay(
                direction == PageTransitionDirection.Jump ? 300 : 350,
                cancellationToken);

            _logger.LogDebug(
                "AnimatePageTransition completed. CorrelationId={CorrelationId}",
                correlationId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "AnimatePageTransition cancelled. CorrelationId={CorrelationId}",
                correlationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AnimatePageTransition failed. CorrelationId={CorrelationId}",
                correlationId);
        }
    }

    /// <inheritdoc/>
    public async Task AnimatePanelSlideAsync(
        UIElement panel,
        SlideDirection direction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(panel);

        if (!_isMotionEnabled)
        {
            return;
        }

        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(panel);
            var compositor = visual.Compositor;

            // Create slide animation based on direction
            var (fromX, fromY, toX, toY) = direction switch
            {
                SlideDirection.FromTop => (0f, -100f, 0f, 0f),
                SlideDirection.FromBottom => (0f, 100f, 0f, 0f),
                SlideDirection.FromLeft => (-100f, 0f, 0f, 0f),
                SlideDirection.FromRight => (100f, 0f, 0f, 0f),
                SlideDirection.ToTop => (0f, 0f, 0f, -100f),
                SlideDirection.ToBottom => (0f, 0f, 0f, 100f),
                SlideDirection.ToLeft => (0f, 0f, -100f, 0f),
                SlideDirection.ToRight => (0f, 0f, 100f, 0f),
                _ => (0f, 0f, 0f, 0f)
            };

            var animation = compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(0.0f, new Vector3(fromX, fromY, 0));
            animation.InsertKeyFrame(1.0f, new Vector3(toX, toY, 0));
            animation.Duration = TimeSpan.FromMilliseconds(250);

            visual.StartAnimation("Translation", animation);

            await Task.Delay(250, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AnimatePanelSlide failed");
        }
    }

    /// <inheritdoc/>
    public async Task AnimateFadeAsync(
        UIElement element,
        double targetOpacity,
        int durationMs = 300,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!_isMotionEnabled)
        {
            element.Opacity = targetOpacity;
            return;
        }

        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0.0f, (float)element.Opacity);
            animation.InsertKeyFrame(1.0f, (float)targetOpacity);
            animation.Duration = TimeSpan.FromMilliseconds(durationMs);

            visual.StartAnimation("Opacity", animation);

            await Task.Delay(durationMs, cancellationToken);

            element.Opacity = targetOpacity;
        }
        catch (OperationCanceledException)
        {
            element.Opacity = targetOpacity;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AnimateFade failed");
            element.Opacity = targetOpacity;
        }
    }

    /// <summary>
    /// Creates a horizontal slide animation.
    /// </summary>
    private CompositionAnimation CreateSlideAnimation(
        Compositor compositor,
        float fromX,
        float toX,
        int duration)
    {
        var animation = compositor.CreateVector3KeyFrameAnimation();
        animation.InsertKeyFrame(0.0f, new Vector3(fromX, 0, 0));
        animation.InsertKeyFrame(1.0f, new Vector3(toX, 0, 0));
        animation.Duration = TimeSpan.FromMilliseconds(duration);
        animation.Target = "Translation";

        return animation;
    }

    /// <summary>
    /// Creates a fade animation.
    /// </summary>
    private CompositionAnimation CreateFadeAnimation(
        Compositor compositor,
        int duration)
    {
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0.0f, 0.0f);
        animation.InsertKeyFrame(1.0f, 1.0f);
        animation.Duration = TimeSpan.FromMilliseconds(duration);
        animation.Target = "Opacity";

        return animation;
    }

    /// <summary>
    /// Detects whether motion is enabled based on Windows accessibility settings.
    /// </summary>
    private bool DetectMotionEnabled()
    {
        try
        {
            // Check for "Reduce motion" accessibility setting
            // In Windows, this is detected via UISettings.AnimationsEnabled
            var animationsEnabled = _uiSettings.AnimationsEnabled;

            _logger.LogInformation(
                "Detected accessibility settings. AnimationsEnabled={AnimationsEnabled}",
                animationsEnabled);

            return animationsEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to detect motion settings, defaulting to enabled");
            return true;
        }
    }
}
