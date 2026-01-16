using FluentPDF.App.Interfaces;
using FluentPDF.Core.Models;
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
    /// <inheritdoc/>
    public string StrategyName => "WriteableBitmap + ImageSharp";

    /// <inheritdoc/>
    public int Priority => 0; // Highest priority - try this first

    /// <inheritdoc/>
    public async Task<ImageSource?> TryRenderAsync(Stream pngStream, RenderContext context)
    {
        try
        {
            // Reset stream position to beginning
            pngStream.Seek(0, SeekOrigin.Begin);

            // Decode PNG using ImageSharp instead of WinUI's BitmapDecoder
            // This avoids all WinUI image decoding APIs that have reliability issues
            var image = await Image.LoadAsync<Bgra32>(pngStream);

            // Create WriteableBitmap with the same dimensions
            var writeableBitmap = new WriteableBitmap(image.Width, image.Height);

            // Copy pixel data directly from ImageSharp image to WriteableBitmap
            // This uses unsafe code to get maximum performance and avoid extra allocations
            using (var bufferAccessor = writeableBitmap.PixelBuffer.AsStream())
            {
                var pixelData = new byte[image.Width * image.Height * 4]; // BGRA32 = 4 bytes per pixel
                image.CopyPixelDataTo(pixelData);
                bufferAccessor.Write(pixelData, 0, pixelData.Length);
            }

            // Invalidate the bitmap to trigger UI update
            writeableBitmap.Invalidate();

            // Clean up ImageSharp image
            image.Dispose();

            return writeableBitmap;
        }
        catch (Exception)
        {
            // Swallow all exceptions and return null to indicate failure
            // Caller (RenderingCoordinator) will log the failure and try next strategy
            return null;
        }
    }
}
