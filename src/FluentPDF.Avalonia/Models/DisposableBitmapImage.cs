using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace FluentPDF.Avalonia.Models;

/// <summary>
/// Wrapper for Avalonia Bitmap that implements IDisposable to work with LruCache.
/// </summary>
public sealed class DisposableBitmapImage : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the wrapped Bitmap.
    /// </summary>
    public Bitmap Image { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableBitmapImage"/> class.
    /// </summary>
    /// <param name="image">The Bitmap to wrap.</param>
    public DisposableBitmapImage(Bitmap image)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    /// <summary>
    /// Disposes the wrapped Bitmap to release memory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Dispose the bitmap to release memory
        Image?.Dispose();
        _disposed = true;
    }
}
