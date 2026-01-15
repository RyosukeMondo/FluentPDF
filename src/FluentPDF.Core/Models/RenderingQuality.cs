namespace FluentPDF.Core.Models;

/// <summary>
/// Specifies the rendering quality level for PDF rendering.
/// Higher quality levels produce sharper output but require more memory and processing time.
/// </summary>
public enum RenderingQuality
{
    /// <summary>
    /// Automatically selects quality based on display DPI and zoom level.
    /// This is the recommended default setting.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Low quality rendering (75 DPI equivalent).
    /// Suitable for quick previews or low-end devices.
    /// Minimal memory usage, fastest rendering.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium quality rendering (96 DPI equivalent).
    /// Suitable for standard displays at 100% scaling.
    /// Balanced memory usage and rendering speed.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High quality rendering (144 DPI equivalent).
    /// Suitable for high-DPI displays or 150% scaling.
    /// Increased memory usage, moderate rendering speed.
    /// </summary>
    High = 3,

    /// <summary>
    /// Ultra quality rendering (192+ DPI equivalent).
    /// Suitable for 4K displays, 200% scaling, or professional use.
    /// High memory usage, slower rendering speed.
    /// May cause out-of-memory errors on large documents.
    /// </summary>
    Ultra = 4
}
