using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for exporting PDF pages as PNG or JPG images.
/// Supports single page or page range export with configurable quality settings.
/// </summary>
public interface IImageExportService
{
    /// <summary>
    /// Exports PDF pages to image files (PNG or JPG) with configurable quality settings.
    /// Generates automatic filenames (document_page001.png) in the specified output directory.
    /// </summary>
    /// <param name="document">The PDF document to export pages from.</param>
    /// <param name="outputDirectory">The directory where image files will be saved.</param>
    /// <param name="options">Export options including format, DPI, quality, and page range.</param>
    /// <param name="progress">Optional progress reporter for batch export operations.</param>
    /// <param name="cancellationToken">Token to cancel the export operation.</param>
    /// <returns>A result containing the list of exported file paths, or an error if export failed.</returns>
    Task<Result<ImageExportResult>> ExportAsync(
        PdfDocument document,
        string outputDirectory,
        ImageExportOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for exporting PDF pages as images.
/// </summary>
public sealed record ImageExportOptions
{
    /// <summary>
    /// Gets the image format (PNG or JPG).
    /// Default is PNG (lossless).
    /// </summary>
    public ImageFormat Format { get; init; } = ImageFormat.Png;

    /// <summary>
    /// Gets the resolution in DPI (dots per inch).
    /// Supported values: 72, 96, 150, 300, 600.
    /// Default is 150 DPI (good balance between quality and file size).
    /// </summary>
    public int Dpi { get; init; } = 150;

    /// <summary>
    /// Gets the JPEG quality (1-100).
    /// Only used when Format is JPG. Higher values = better quality, larger file size.
    /// Default is 90.
    /// </summary>
    public int JpegQuality { get; init; } = 90;

    /// <summary>
    /// Gets the page range to export (e.g., "1-5", "1,3,5", "all").
    /// Default is "all" (export all pages).
    /// </summary>
    public string PageRange { get; init; } = "all";

    /// <summary>
    /// Gets the base filename for exported images (without extension).
    /// Files will be named as "{BaseFilename}_page001.{ext}".
    /// If not specified, uses the document filename.
    /// </summary>
    public string? BaseFilename { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing files.
    /// Default is false (will fail if files exist).
    /// </summary>
    public bool OverwriteExisting { get; init; }
}

/// <summary>
/// Image format for export.
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// PNG format (lossless, larger file size).
    /// </summary>
    Png,

    /// <summary>
    /// JPG format (lossy, smaller file size, configurable quality).
    /// </summary>
    Jpg
}

/// <summary>
/// Result of an image export operation.
/// </summary>
public sealed record ImageExportResult
{
    /// <summary>
    /// Gets the list of exported file paths.
    /// </summary>
    public required IReadOnlyList<string> ExportedFiles { get; init; }

    /// <summary>
    /// Gets the total number of pages exported.
    /// </summary>
    public required int PageCount { get; init; }

    /// <summary>
    /// Gets the total processing time.
    /// </summary>
    public required TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the total size of all exported files in bytes.
    /// </summary>
    public required long TotalSize { get; init; }
}
