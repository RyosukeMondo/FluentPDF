using FluentPDF.Avalonia.Controls;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Service contract for managing acrylic material rendering and quality.
/// </summary>
public interface IAcrylicService
{
    /// <summary>
    /// Gets whether GPU acceleration is available for acrylic effects.
    /// </summary>
    bool IsGpuAvailable { get; }

    /// <summary>
    /// Gets the current quality level of acrylic rendering.
    /// </summary>
    AcrylicMaterial.QualityLevel CurrentQuality { get; }

    /// <summary>
    /// Gets the estimated memory overhead in megabytes.
    /// </summary>
    double MemoryOverheadMB { get; }

    /// <summary>
    /// Adjusts acrylic quality based on current performance metrics.
    /// </summary>
    /// <param name="currentFps">Current frames per second.</param>
    void AdjustQualityForPerformance(int currentFps);

    /// <summary>
    /// Enables high contrast mode, replacing acrylic with solid colors.
    /// </summary>
    void EnableHighContrastMode();

    /// <summary>
    /// Disables high contrast mode, restoring acrylic materials.
    /// </summary>
    void DisableHighContrastMode();
}
