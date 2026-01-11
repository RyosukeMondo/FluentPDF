namespace FluentPDF.Core.Models;

/// <summary>
/// Represents display configuration information for HiDPI rendering.
/// Contains display scaling, effective DPI, and helper properties for rendering decisions.
/// </summary>
public sealed class DisplayInfo
{
    /// <summary>
    /// Gets the rasterization scale factor of the display.
    /// For example: 1.0 = 100% scaling, 1.5 = 150% scaling, 2.0 = 200% scaling.
    /// </summary>
    public required double RasterizationScale { get; init; }

    /// <summary>
    /// Gets the effective DPI (dots per inch) for rendering.
    /// This accounts for display scaling, zoom level, and quality settings.
    /// Standard DPI is 96. Values range from 50 to 576 DPI.
    /// </summary>
    public required double EffectiveDpi { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a high-DPI display.
    /// True when RasterizationScale is greater than 1.0.
    /// </summary>
    public bool IsHighDpi => RasterizationScale > 1.0;

    /// <summary>
    /// Gets the display scaling as a percentage.
    /// For example: 100%, 150%, 200%.
    /// </summary>
    public int ScalingPercentage => (int)Math.Round(RasterizationScale * 100);

    /// <summary>
    /// Creates a DisplayInfo instance with the specified parameters.
    /// </summary>
    /// <param name="rasterizationScale">The rasterization scale factor (must be positive).</param>
    /// <param name="effectiveDpi">The effective DPI for rendering (must be between 50 and 576).</param>
    /// <returns>A new DisplayInfo instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when rasterizationScale is not positive or effectiveDpi is outside valid range.
    /// </exception>
    public static DisplayInfo Create(double rasterizationScale, double effectiveDpi)
    {
        if (rasterizationScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rasterizationScale),
                rasterizationScale,
                "Rasterization scale must be positive.");
        }

        if (effectiveDpi < 50 || effectiveDpi > 576)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveDpi),
                effectiveDpi,
                "Effective DPI must be between 50 and 576.");
        }

        return new DisplayInfo
        {
            RasterizationScale = rasterizationScale,
            EffectiveDpi = effectiveDpi
        };
    }

    /// <summary>
    /// Creates a DisplayInfo instance for a standard display (96 DPI, 100% scaling).
    /// </summary>
    /// <returns>A DisplayInfo instance representing a standard display.</returns>
    public static DisplayInfo Standard() => new()
    {
        RasterizationScale = 1.0,
        EffectiveDpi = 96.0
    };

    /// <summary>
    /// Creates a DisplayInfo instance for a high-DPI display with the specified scale.
    /// </summary>
    /// <param name="scale">The scaling factor (e.g., 1.5 for 150%, 2.0 for 200%).</param>
    /// <returns>A DisplayInfo instance with calculated effective DPI.</returns>
    public static DisplayInfo FromScale(double scale)
    {
        const double baseDpi = 96.0;
        var effectiveDpi = baseDpi * scale;
        return Create(scale, effectiveDpi);
    }

    /// <summary>
    /// Calculates the scale multiplier for the given quality level.
    /// </summary>
    /// <param name="quality">The rendering quality level.</param>
    /// <returns>The DPI multiplier for the quality level.</returns>
    public static double GetQualityMultiplier(RenderingQuality quality) => quality switch
    {
        RenderingQuality.Low => 0.78125,  // 75 DPI / 96 DPI
        RenderingQuality.Medium => 1.0,   // 96 DPI
        RenderingQuality.High => 1.5,     // 144 DPI
        RenderingQuality.Ultra => 2.0,    // 192 DPI
        RenderingQuality.Auto => 1.0,     // Base multiplier, adjusted dynamically
        _ => 1.0
    };
}
