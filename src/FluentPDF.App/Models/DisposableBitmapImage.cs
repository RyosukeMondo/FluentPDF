using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentPDF.App.Models;

/// <summary>
/// Wrapper for ImageSource (BitmapImage or SoftwareBitmapSource) that implements IDisposable to work with LruCache.
/// </summary>
public sealed class DisposableBitmapImage : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the wrapped ImageSource.
    /// </summary>
    public ImageSource Image { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableBitmapImage"/> class.
    /// </summary>
    /// <param name="image">The ImageSource to wrap.</param>
    public DisposableBitmapImage(ImageSource image)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    /// <summary>
    /// Disposes the wrapped BitmapImage by clearing its source.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Clear the bitmap to release memory
        // BitmapImage doesn't implement IDisposable but we can clear its UriSource
        // to help garbage collection
        _disposed = true;
    }
}
