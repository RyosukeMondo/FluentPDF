using FluentPDF.App.Interfaces;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FluentPDF.App.Services.RenderingStrategies;

/// <summary>
/// Primary rendering strategy that uses ImageSharp to decode PNG and WriteableBitmap for WinUI display.
/// This approach avoids WinUI's buggy InMemoryRandomAccessStream and BitmapDecoder APIs.
/// </summary>
/// <remarks>
/// Workaround for WinUI 3 InMemoryRandomAccessStream crash issues.
/// See: https://github.com/microsoft/microsoft-ui-xaml/issues/7052
/// </remarks>
public sealed class WriteableBitmapRenderingStrategy : IRenderingStrategy
{
    private readonly ILogger<WriteableBitmapRenderingStrategy>? _logger;

    /// <summary>
    /// Initializes a new instance without logging (for backward compatibility).
    /// </summary>
    public WriteableBitmapRenderingStrategy()
    {
    }

    /// <summary>
    /// Initializes a new instance with logging support.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WriteableBitmapRenderingStrategy(ILogger<WriteableBitmapRenderingStrategy> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string StrategyName => "WriteableBitmap + ImageSharp";

    /// <inheritdoc/>
    public int Priority => 0; // Highest priority - try this first

    /// <inheritdoc/>
    public async Task<ImageSource?> TryRenderAsync(Stream pngStream, RenderContext context)
    {
        try
        {
            _logger?.LogDebug(
                "WriteableBitmapRenderingStrategy: Starting render. StreamLength={StreamLength}, StreamPosition={StreamPosition}, CanRead={CanRead}, CanSeek={CanSeek}",
                pngStream.Length, pngStream.Position, pngStream.CanRead, pngStream.CanSeek);

            // Reset stream position to beginning
            pngStream.Seek(0, SeekOrigin.Begin);

            // Decode PNG using ImageSharp instead of WinUI's BitmapDecoder
            // This avoids all WinUI image decoding APIs that have reliability issues
            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Bgra32>(pngStream);

            _logger?.LogDebug(
                "WriteableBitmapRenderingStrategy: Image decoded. Width={Width}, Height={Height}",
                image.Width, image.Height);

            // Extract pixel data from ImageSharp image (can be done off UI thread)
            var pixelData = new byte[image.Width * image.Height * 4]; // BGRA32 = 4 bytes per pixel
            image.CopyPixelDataTo(pixelData);

            var width = image.Width;
            var height = image.Height;

            // All WriteableBitmap operations MUST happen on UI thread
            if (App.MainWindow?.DispatcherQueue == null)
            {
                _logger?.LogError("WriteableBitmapRenderingStrategy: No DispatcherQueue available");
                return null;
            }

            var tcs = new TaskCompletionSource<WriteableBitmap?>();

            _logger?.LogDebug(
                "WriteableBitmapRenderingStrategy: Queueing WriteableBitmap creation on UI thread. Width={Width}, Height={Height}, PixelDataLength={PixelDataLength}",
                width, height, pixelData.Length);

            var queued = App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    _logger?.LogDebug("WriteableBitmapRenderingStrategy: Creating WriteableBitmap on UI thread");

                    // Create WriteableBitmap on UI thread
                    var writeableBitmap = new WriteableBitmap(width, height);

                    _logger?.LogDebug("WriteableBitmapRenderingStrategy: WriteableBitmap created, copying pixel data");

                    // Copy pixel data to WriteableBitmap on UI thread
                    using (var bufferAccessor = writeableBitmap.PixelBuffer.AsStream())
                    {
                        bufferAccessor.Write(pixelData, 0, pixelData.Length);
                    }

                    _logger?.LogDebug("WriteableBitmapRenderingStrategy: Pixel data copied, invalidating bitmap");

                    // Invalidate to trigger UI update
                    writeableBitmap.Invalidate();

                    _logger?.LogDebug("WriteableBitmapRenderingStrategy: WriteableBitmap ready");

                    tcs.SetResult(writeableBitmap);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "WriteableBitmapRenderingStrategy: Failed on UI thread. Error={ErrorMessage}", ex.Message);
                    tcs.SetException(ex); // Propagate the exception instead of silently failing
                }
            });

            if (!queued)
            {
                _logger?.LogError("WriteableBitmapRenderingStrategy: Failed to queue on UI thread");
                return null;
            }

            var result = await tcs.Task;

            if (result != null)
            {
                _logger?.LogDebug(
                    "WriteableBitmapRenderingStrategy: Render completed successfully. BitmapWidth={Width}, BitmapHeight={Height}",
                    result.PixelWidth, result.PixelHeight);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "WriteableBitmapRenderingStrategy: Failed to render. StreamLength={StreamLength}, Page={PageNumber}, Error={ErrorMessage}",
                pngStream?.Length ?? 0, context?.PageNumber ?? 0, ex.Message);
            // Return null to indicate failure - caller will try next strategy
            return null;
        }
    }
}
