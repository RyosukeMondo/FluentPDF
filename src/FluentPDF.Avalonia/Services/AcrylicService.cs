using FluentPDF.Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Service implementation for managing acrylic material rendering and quality.
/// Provides centralized control over GPU-accelerated backdrop blur effects.
/// </summary>
/// <remarks>
/// Task 1.2: GPU-accelerated acrylic materials with adaptive quality.
/// Requirements: 1.1 (Acrylic effects), 1.6.2 (Performance optimization).
/// Integrates with PerformanceMonitor for automatic quality adjustment.
/// </remarks>
public sealed class AcrylicService : IAcrylicService
{
    private readonly AcrylicMaterial _acrylicMaterial;
    private readonly ILogger<AcrylicService> _logger;
    private bool _isHighContrastMode;

    /// <inheritdoc/>
    public bool IsGpuAvailable => _acrylicMaterial.IsGpuAccelerationAvailable;

    /// <inheritdoc/>
    public AcrylicMaterial.QualityLevel CurrentQuality => _acrylicMaterial.CurrentQuality;

    /// <inheritdoc/>
    public double MemoryOverheadMB => _acrylicMaterial.GetMemoryOverheadMB();

    /// <summary>
    /// Initializes a new instance of the <see cref="AcrylicService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public AcrylicService(ILogger<AcrylicService> logger)
    {
        _logger = logger;
        _acrylicMaterial = new AcrylicMaterial(
            logger as ILogger<AcrylicMaterial>);

        _logger.LogInformation(
            "AcrylicService initialized. GPU available: {GpuAvailable}, Quality: {Quality}",
            IsGpuAvailable, CurrentQuality);
    }

    /// <inheritdoc/>
    public void AdjustQualityForPerformance(int currentFps)
    {
        if (_isHighContrastMode)
        {
            // Don't adjust quality in high contrast mode
            return;
        }

        _acrylicMaterial.AdjustQualityForPerformance(currentFps);

        _logger.LogDebug(
            "Adjusted acrylic quality for FPS={Fps}, Quality={Quality}",
            currentFps, CurrentQuality);
    }

    /// <inheritdoc/>
    public void EnableHighContrastMode()
    {
        if (_isHighContrastMode)
        {
            return;
        }

        _acrylicMaterial.EnableHighContrastMode();
        _isHighContrastMode = true;

        _logger.LogInformation("High contrast mode enabled for acrylic materials");
    }

    /// <inheritdoc/>
    public void DisableHighContrastMode()
    {
        if (!_isHighContrastMode)
        {
            return;
        }

        // Reset to auto-detected quality
        _isHighContrastMode = false;

        _logger.LogInformation("High contrast mode disabled, acrylic materials restored");
    }
}
