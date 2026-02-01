using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using CoreAnimationService = FluentPDF.Core.Services.IAnimationService;
using CorePageTransitionDirection = FluentPDF.Core.Services.PageTransitionDirection;
using CoreSlideDirection = FluentPDF.Core.Services.SlideDirection;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Adapter that implements Core.Services.IAnimationService using Avalonia's AnimationService.
/// </summary>
public sealed class AvaloniaAnimationServiceAdapter : CoreAnimationService
{
    private readonly IAnimationService _avaloniaService;
    private readonly ILogger<AvaloniaAnimationServiceAdapter>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaAnimationServiceAdapter"/> class.
    /// </summary>
    /// <param name="avaloniaService">The Avalonia animation service to wrap.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public AvaloniaAnimationServiceAdapter(
        IAnimationService avaloniaService,
        ILogger<AvaloniaAnimationServiceAdapter>? logger = null)
    {
        _avaloniaService = avaloniaService ?? throw new ArgumentNullException(nameof(avaloniaService));
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsMotionEnabled => _avaloniaService.IsMotionEnabled;

    /// <inheritdoc/>
    public bool IsReducedMotionEnabled() => _avaloniaService.IsReducedMotionEnabled();

    /// <inheritdoc/>
    public void SetMotionEnabled(bool enabled)
    {
        _avaloniaService.SetMotionEnabled(enabled);
    }

    /// <inheritdoc/>
    public async Task AnimatePageTransitionAsync(
        object? target,
        CorePageTransitionDirection direction,
        CancellationToken cancellationToken = default)
    {
        if (target is not Control control)
        {
            _logger?.LogDebug("Target is not an Avalonia Control, skipping animation");
            return;
        }

        var avaloniaDirection = direction switch
        {
            CorePageTransitionDirection.Forward => PageTransitionType.Slide,
            CorePageTransitionDirection.Backward => PageTransitionType.Slide,
            CorePageTransitionDirection.Jump => PageTransitionType.Fade,
            _ => PageTransitionType.Fade
        };

        await _avaloniaService.AnimatePageTransitionAsync(control, avaloniaDirection, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AnimatePanelSlideAsync(
        object panel,
        CoreSlideDirection direction,
        CancellationToken cancellationToken = default)
    {
        if (panel is not Control control)
        {
            _logger?.LogDebug("Panel is not an Avalonia Control, skipping animation");
            return;
        }

        var avaloniaDirection = direction switch
        {
            CoreSlideDirection.FromTop => SlideDirection.FromTop,
            CoreSlideDirection.FromBottom => SlideDirection.FromBottom,
            CoreSlideDirection.FromLeft => SlideDirection.FromLeft,
            CoreSlideDirection.FromRight => SlideDirection.FromRight,
            CoreSlideDirection.ToTop => SlideDirection.FromTop,
            CoreSlideDirection.ToBottom => SlideDirection.FromBottom,
            CoreSlideDirection.ToLeft => SlideDirection.FromLeft,
            CoreSlideDirection.ToRight => SlideDirection.FromRight,
            _ => SlideDirection.FromLeft
        };

        var slideIn = direction is CoreSlideDirection.FromTop or CoreSlideDirection.FromBottom
            or CoreSlideDirection.FromLeft or CoreSlideDirection.FromRight;

        await _avaloniaService.AnimatePanelSlideAsync(control, avaloniaDirection, slideIn, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AnimateFadeAsync(
        object element,
        double targetOpacity,
        int durationMs = 300,
        CancellationToken cancellationToken = default)
    {
        if (element is not Control control)
        {
            _logger?.LogDebug("Element is not an Avalonia Control, skipping animation");
            return;
        }

        var fadeIn = targetOpacity > 0.5;
        await _avaloniaService.AnimateFadeAsync(control, fadeIn, durationMs, cancellationToken);
    }
}
