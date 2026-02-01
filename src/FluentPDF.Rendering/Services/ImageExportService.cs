using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for exporting PDF pages as PNG or JPG images.
/// Uses PDFium for rendering and ImageSharp for encoding.
/// </summary>
public sealed class ImageExportService : IImageExportService
{
    private readonly ILogger<ImageExportService> _logger;
    private static readonly SemaphoreSlim _pdfiumSemaphore = new(1, 1); // PDFium is not thread-safe

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageExportService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking export operations.</param>
    public ImageExportService(ILogger<ImageExportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<ImageExportResult>> ExportAsync(
        PdfDocument document,
        string outputDirectory,
        ImageExportOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting image export. CorrelationId={CorrelationId}, Document={Document}, Format={Format}, Dpi={Dpi}, PageRange={PageRange}",
            correlationId, Path.GetFileName(document.FilePath), options.Format, options.Dpi, options.PageRange);

        try
        {
            // Validate options
            var validationResult = ValidateOptions(options, document.PageCount);
            if (validationResult.IsFailed)
            {
                return validationResult.ToResult<ImageExportResult>();
            }

            // Create output directory if it doesn't exist
            if (!Directory.Exists(outputDirectory))
            {
                _logger.LogInformation("Creating output directory: {Directory}", outputDirectory);
                Directory.CreateDirectory(outputDirectory);
            }

            // Parse page range
            var pagesToExport = ParsePageRange(options.PageRange, document.PageCount);
            if (pagesToExport.Count == 0)
            {
                var error = new PdfError(
                    "EXPORT_INVALID_PAGE_RANGE",
                    "No valid pages to export.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("PageRange", options.PageRange)
                    .WithContext("TotalPages", document.PageCount);

                _logger.LogWarning("No valid pages to export. PageRange={PageRange}", options.PageRange);
                return Result.Fail(error);
            }

            _logger.LogInformation("Exporting {Count} pages", pagesToExport.Count);

            // Determine base filename
            var baseFilename = options.BaseFilename
                ?? Path.GetFileNameWithoutExtension(document.FilePath)
                ?? "export";

            // Sanitize filename
            baseFilename = SanitizeFilename(baseFilename);

            // Export pages
            var exportedFiles = new List<string>();
            long totalSize = 0;

            for (int i = 0; i < pagesToExport.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageNumber = pagesToExport[i];
                var pageIndex = i + 1; // Use sequential numbering

                // Generate filename
                var extension = options.Format == ImageFormat.Png ? "png" : "jpg";
                var filename = $"{baseFilename}_page{pageIndex:D3}.{extension}";
                var outputPath = Path.Combine(outputDirectory, filename);

                // Check if file exists
                if (File.Exists(outputPath) && !options.OverwriteExisting)
                {
                    var error = new PdfError(
                        "EXPORT_FILE_EXISTS",
                        $"Output file already exists: {filename}",
                        ErrorCategory.Validation,
                        ErrorSeverity.Error)
                        .WithContext("FilePath", outputPath)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogWarning("Output file already exists: {FilePath}", outputPath);
                    return Result.Fail(error);
                }

                // Export page
                var exportResult = await ExportPageAsync(
                    document,
                    pageNumber,
                    outputPath,
                    options,
                    correlationId);

                if (exportResult.IsFailed)
                {
                    return exportResult.ToResult<ImageExportResult>();
                }

                exportedFiles.Add(outputPath);
                totalSize += new FileInfo(outputPath).Length;

                // Report progress
                var progressPercent = ((double)(i + 1) / pagesToExport.Count) * 100.0;
                progress?.Report(progressPercent);

                _logger.LogDebug(
                    "Exported page {PageNumber} to {Filename} ({Progress:F1}%)",
                    pageNumber, filename, progressPercent);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Image export completed successfully. CorrelationId={CorrelationId}, PageCount={PageCount}, TotalSize={TotalSize} bytes, ProcessingTime={ProcessingTimeMs}ms",
                correlationId, exportedFiles.Count, totalSize, stopwatch.ElapsedMilliseconds);

            var result = new ImageExportResult
            {
                ExportedFiles = exportedFiles,
                PageCount = exportedFiles.Count,
                ProcessingTime = stopwatch.Elapsed,
                TotalSize = totalSize
            };

            return Result.Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Image export cancelled by user. CorrelationId={CorrelationId}", correlationId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var error = new PdfError(
                "EXPORT_FAILED",
                $"Image export failed: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex, "Image export failed. CorrelationId={CorrelationId}", correlationId);
            return Result.Fail(error);
        }
    }

    private async Task<Result> ExportPageAsync(
        PdfDocument document,
        int pageNumber,
        string outputPath,
        ImageExportOptions options,
        Guid correlationId)
    {
        SafePdfPageHandle? pageHandle = null;
        IntPtr bitmap = IntPtr.Zero;

        // PDFium is not thread-safe - serialize all operations
        await _pdfiumSemaphore.WaitAsync();
        try
        {
            // Load page (0-based index)
            var documentHandle = (SafePdfDocumentHandle)document.Handle;
            pageHandle = PdfiumInterop.LoadPage(documentHandle, pageNumber - 1);

            if (pageHandle.IsInvalid)
            {
                var error = new PdfError(
                    "EXPORT_PAGE_LOAD_FAILED",
                    $"Failed to load page {pageNumber} for export.",
                    ErrorCategory.Rendering,
                    ErrorSeverity.Error)
                    .WithContext("PageNumber", pageNumber)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogError("Failed to load page for export. PageNumber={PageNumber}", pageNumber);
                return Result.Fail(error);
            }

            // Get page dimensions
            var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
            var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);

            // Calculate output size based on DPI
            var scaleFactor = options.Dpi / 72.0;
            var outputWidth = (int)(pageWidth * scaleFactor);
            var outputHeight = (int)(pageHeight * scaleFactor);

            _logger.LogDebug(
                "Rendering page {PageNumber} at {Dpi} DPI. Size: {Width}x{Height}",
                pageNumber, options.Dpi, outputWidth, outputHeight);

            // Create bitmap
            bitmap = PdfiumInterop.CreateBitmap(outputWidth, outputHeight, hasAlpha: true);
            if (bitmap == IntPtr.Zero)
            {
                var error = new PdfError(
                    "EXPORT_BITMAP_FAILED",
                    "Failed to create bitmap for page export. Out of memory or dimensions too large.",
                    ErrorCategory.System,
                    ErrorSeverity.Error)
                    .WithContext("OutputWidth", outputWidth)
                    .WithContext("OutputHeight", outputHeight)
                    .WithContext("PageNumber", pageNumber)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogError("Failed to create bitmap for page export. PageNumber={PageNumber}", pageNumber);
                return Result.Fail(error);
            }

            // Fill with white background
            PdfiumInterop.FillBitmap(bitmap, 0xFFFFFFFF);

            // Render page to bitmap
            PdfiumInterop.RenderPageBitmap(
                bitmap,
                pageHandle,
                startX: 0,
                startY: 0,
                sizeX: outputWidth,
                sizeY: outputHeight,
                rotate: 0,
                flags: PdfiumInterop.RenderFlags.Normal);

            // Save bitmap to file
            await SaveBitmapToFileAsync(bitmap, outputWidth, outputHeight, outputPath, options);

            return Result.Ok();
        }
        finally
        {
            // Clean up resources
            if (bitmap != IntPtr.Zero)
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }

            pageHandle?.Dispose();
            _pdfiumSemaphore.Release();
        }
    }

    private static async Task SaveBitmapToFileAsync(
        IntPtr bitmap,
        int width,
        int height,
        string outputPath,
        ImageExportOptions options)
    {
        // Get bitmap buffer
        var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
        var stride = PdfiumInterop.GetBitmapStride(bitmap);

        // PDFium uses BGRA format, copy to byte array
        var byteCount = stride * height;
        var pixelData = new byte[byteCount];
        Marshal.Copy(buffer, pixelData, 0, byteCount);

        // Create image from BGRA pixel data using ImageSharp
        using var image = Image.LoadPixelData<Bgra32>(pixelData, width, height);

        // Save to file based on format
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);

        if (options.Format == ImageFormat.Png)
        {
            await image.SaveAsync(fileStream, new PngEncoder());
        }
        else // JPG
        {
            var jpegEncoder = new JpegEncoder
            {
                Quality = options.JpegQuality
            };
            await image.SaveAsync(fileStream, jpegEncoder);
        }
    }

    private static Result ValidateOptions(ImageExportOptions options, int totalPages)
    {
        // Validate DPI
        var validDpiValues = new[] { 72, 96, 150, 300, 600 };
        if (!validDpiValues.Contains(options.Dpi))
        {
            var error = new PdfError(
                "EXPORT_INVALID_DPI",
                $"Invalid DPI value: {options.Dpi}. Valid values: {string.Join(", ", validDpiValues)}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("Dpi", options.Dpi);

            return Result.Fail(error);
        }

        // Validate JPEG quality
        if (options.Format == ImageFormat.Jpg && (options.JpegQuality < 1 || options.JpegQuality > 100))
        {
            var error = new PdfError(
                "EXPORT_INVALID_QUALITY",
                $"Invalid JPEG quality: {options.JpegQuality}. Valid range: 1-100",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("JpegQuality", options.JpegQuality);

            return Result.Fail(error);
        }

        // Validate page range
        if (string.IsNullOrWhiteSpace(options.PageRange))
        {
            var error = new PdfError(
                "EXPORT_INVALID_PAGE_RANGE",
                "Page range cannot be null or empty.",
                ErrorCategory.Validation,
                ErrorSeverity.Error);

            return Result.Fail(error);
        }

        return Result.Ok();
    }

    private static List<int> ParsePageRange(string pageRange, int totalPages)
    {
        var pages = new HashSet<int>();

        if (pageRange.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Export all pages
            for (int i = 1; i <= totalPages; i++)
            {
                pages.Add(i);
            }
            return pages.OrderBy(p => p).ToList();
        }

        // Parse page range (e.g., "1-5,10,15-20")
        var parts = pageRange.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                // Range (e.g., "1-5")
                var rangeParts = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (rangeParts.Length == 2 &&
                    int.TryParse(rangeParts[0], out var start) &&
                    int.TryParse(rangeParts[1], out var end))
                {
                    for (int i = start; i <= end && i <= totalPages; i++)
                    {
                        if (i >= 1)
                        {
                            pages.Add(i);
                        }
                    }
                }
            }
            else
            {
                // Single page
                if (int.TryParse(part, out var pageNum) && pageNum >= 1 && pageNum <= totalPages)
                {
                    pages.Add(pageNum);
                }
            }
        }

        return pages.OrderBy(p => p).ToList();
    }

    private static string SanitizeFilename(string filename)
    {
        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());

        // Replace spaces with underscores
        sanitized = sanitized.Replace(' ', '_');

        // Limit length
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50];
        }

        return sanitized;
    }
}
