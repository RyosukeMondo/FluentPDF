using FluentPDF.Core.Models;
using Microsoft.UI.Xaml.Media;

namespace FluentPDF.App.Interfaces;

/// <summary>
/// Defines a contract for different PDF rendering strategies that convert PNG streams to WinUI ImageSource objects.
/// Implementations provide different approaches to rendering (e.g., WriteableBitmap, file-based) with varying reliability characteristics.
/// </summary>
public interface IRenderingStrategy
{
    /// <summary>
    /// Gets the name of this rendering strategy for logging and diagnostics.
    /// </summary>
    /// <example>"WriteableBitmap + ImageSharp", "FileBased", "BitmapImage"</example>
    string StrategyName { get; }

    /// <summary>
    /// Gets the priority of this strategy. Lower values are tried first.
    /// Primary strategies should use 0, fallback strategies should use 10+.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to render a PNG stream to a WinUI ImageSource.
    /// </summary>
    /// <param name="pngStream">The PNG-encoded image stream from PDF rendering.</param>
    /// <param name="context">Rendering context containing document and page information.</param>
    /// <returns>
    /// An ImageSource that can be bound to WinUI Image controls, or null if rendering failed.
    /// Implementations should handle all exceptions internally and return null on failure.
    /// </returns>
    /// <remarks>
    /// Implementations must:
    /// - Be stateless and thread-safe
    /// - Reset stream position to 0 before reading
    /// - Handle all exceptions gracefully and return null on failure
    /// - Not dispose the input stream (caller manages lifetime)
    /// - Complete within reasonable time (under 5 seconds for typical pages)
    /// </remarks>
    Task<ImageSource?> TryRenderAsync(Stream pngStream, RenderContext context);
}
