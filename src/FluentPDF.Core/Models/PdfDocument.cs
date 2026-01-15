namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a loaded PDF document with its metadata and native handle.
/// Implements IDisposable to ensure proper cleanup of native resources.
/// </summary>
public sealed class PdfDocument : IDisposable
{
    /// <summary>
    /// Gets the full file path to the PDF document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the total number of pages in the PDF document.
    /// </summary>
    public required int PageCount { get; init; }

    /// <summary>
    /// Gets the native PDFium document handle.
    /// This handle is automatically cleaned up when the object is disposed.
    /// </summary>
    public required IDisposable Handle { get; init; }

    /// <summary>
    /// Gets the timestamp when the document was loaded.
    /// </summary>
    public required DateTime LoadedAt { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Releases the native PDFium document handle and associated resources.
    /// </summary>
    public void Dispose()
    {
        Handle?.Dispose();
        GC.SuppressFinalize(this);
    }
}
