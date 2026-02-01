using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Services.RenderingStrategies;

/// <summary>
/// Primary rendering strategy that uses Avalonia's Bitmap class with Skia backend.
/// This is the recommended approach for Avalonia applications.
/// </summary>
public sealed class SkiaRenderingStrategy : IRenderingStrategy
{
    private readonly ILogger<SkiaRenderingStrategy>? _logger;

    /// <summary>
    /// Initializes a new instance without logging (for backward compatibility).
    /// </summary>
    public SkiaRenderingStrategy()
    {
    }

    /// <summary>
    /// Initializes a new instance with logging support.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SkiaRenderingStrategy(ILogger<SkiaRenderingStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string StrategyName => "Skia + Bitmap";

    /// <inheritdoc/>
    public int Priority => 0; // Highest priority - try this first

    /// <inheritdoc/>
    public async Task<IImage?> TryRenderAsync(Stream pngStream, RenderContext context)
    {
        try
        {
            _logger?.LogDebug(
                "SkiaRenderingStrategy: Starting render. StreamLength={StreamLength}, StreamPosition={StreamPosition}, CanRead={CanRead}, CanSeek={CanSeek}",
                pngStream.Length, pngStream.Position, pngStream.CanRead, pngStream.CanSeek);

            // Reset stream position to beginning
            pngStream.Seek(0, SeekOrigin.Begin);

            // Avalonia's Bitmap constructor can read directly from a stream
            // Skia will handle PNG decoding automatically
            var bitmap = await Task.Run(() =>
            {
                try
                {
                    return new Bitmap(pngStream);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "SkiaRenderingStrategy: Failed to create Bitmap from stream");
                    return null;
                }
            });

            if (bitmap == null)
            {
                _logger?.LogError("SkiaRenderingStrategy: Bitmap creation returned null");
                return null;
            }

            _logger?.LogDebug(
                "SkiaRenderingStrategy: Render completed successfully. BitmapWidth={Width}, BitmapHeight={Height}, DpiX={DpiX}, DpiY={DpiY}",
                bitmap.PixelSize.Width, bitmap.PixelSize.Height, bitmap.Dpi.X, bitmap.Dpi.Y);

            return bitmap;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "SkiaRenderingStrategy: Failed to render. StreamLength={StreamLength}, Page={PageNumber}, Error={ErrorMessage}",
                pngStream?.Length ?? 0, context?.PageNumber ?? 0, ex.Message);
            // Return null to indicate failure - caller will try next strategy
            return null;
        }
    }
}
