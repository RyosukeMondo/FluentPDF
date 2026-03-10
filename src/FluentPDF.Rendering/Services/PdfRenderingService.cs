using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for rendering PDF pages to PNG streams using PDFium.
/// Implements asynchronous rendering with performance monitoring and comprehensive error handling.
/// Returns cross-platform streams that can be converted to platform-specific image types by the consuming code.
/// </summary>
public sealed partial class PdfRenderingService : IPdfRenderingService
{
    private readonly ILogger<PdfRenderingService> _logger;
    private readonly PageHandleCache? _pageCache;
    private static readonly ActivitySource _activitySource = new("FluentPDF.Rendering");
    private static readonly SemaphoreSlim _pdfiumSemaphore = new(1, 1); // PDFium is not thread-safe
    private const int SlowRenderThresholdMs = 2000;
    private const double StandardDpi = 96.0;
    private const double HighDpiThreshold = 144.0; // 1.5x scaling

    public PdfRenderingService(ILogger<PdfRenderingService> logger, PageHandleCache? pageCache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pageCache = pageCache;
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> RenderPageAsync(
        PdfDocument document,
        int pageNumber,
        double zoomLevel,
        double dpi = 96)
    {
        using var activity = _activitySource.StartActivity("RenderPage");

        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Add activity tags
        activity?.SetTag("page.number", pageNumber);
        activity?.SetTag("zoom.level", zoomLevel);
        activity?.SetTag("dpi", dpi);
        activity?.SetTag("correlation.id", correlationId.ToString());

        _logger.LogInformation(
            "Starting page render. CorrelationId={CorrelationId}, PageNumber={PageNumber}, ZoomLevel={ZoomLevel}, Dpi={Dpi}",
            correlationId, pageNumber, zoomLevel, dpi);

        // Validate page number
        var validationError = ValidatePageNumber(document, pageNumber, correlationId, activity);
        if (validationError != null)
        {
            return Result.Fail(validationError);
        }

        return await Task.Run(async () =>
        {
            SafePdfPageHandle? pageHandle = null;
            IntPtr bitmap = IntPtr.Zero;
            bool pageFromCache = false;

            // PDFium is not thread-safe - serialize all operations
            await _pdfiumSemaphore.WaitAsync();
            try
            {
                try
                {
                    // Load page (0-based index) — use cached handle if page was modified
                    using (var loadPageActivity = _activitySource.StartActivity("LoadPage"))
                    {
                        loadPageActivity?.SetTag("page.number", pageNumber);

                        var loadResult = LoadPageHandle(document, pageNumber, correlationId, loadPageActivity, activity);
                        if (loadResult.IsFailed)
                        {
                            return Result.Fail(loadResult.Errors);
                        }
                        (pageHandle, pageFromCache) = loadResult.Value;
                    }

                // Get page dimensions
                var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
                var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);

                // Calculate output size
                var scaleFactor = (dpi / 72.0) * zoomLevel;
                var outputWidth = (int)(pageWidth * scaleFactor);
                var outputHeight = (int)(pageHeight * scaleFactor);

                _logger.LogDebug(
                    "Calculated render dimensions. CorrelationId={CorrelationId}, PageWidth={PageWidth}, PageHeight={PageHeight}, OutputWidth={OutputWidth}, OutputHeight={OutputHeight}",
                    correlationId, pageWidth, pageHeight, outputWidth, outputHeight);

                // Validate dimensions
                var dimError = ValidateDimensions(outputWidth, outputHeight, zoomLevel, correlationId);
                if (dimError != null) return Result.Fail(dimError);

                // Render bitmap with OOM fallback
                var bitmapResult = CreateAndRenderBitmap(
                    pageHandle, pageWidth, pageHeight, outputWidth, outputHeight,
                    dpi, zoomLevel, correlationId, activity);
                if (bitmapResult.IsFailed) return Result.Fail(bitmapResult.Errors);

                var (bmp, effectiveWidth, effectiveHeight, effectiveDpi) = bitmapResult.Value;
                bitmap = bmp;

                // Convert bitmap to PNG stream
                Stream imageStream;
                using (var convertImageActivity = _activitySource.StartActivity("ConvertToImage"))
                {
                    convertImageActivity?.SetTag("output.format", "PNG");
                    imageStream = await ConvertToPngStreamAsync(bitmap, effectiveWidth, effectiveHeight);
                }

                stopwatch.Stop();
                activity?.SetTag("render.time.ms", stopwatch.ElapsedMilliseconds);

                LogRenderPerformance(correlationId, pageNumber, stopwatch.ElapsedMilliseconds,
                    zoomLevel, dpi, effectiveDpi, effectiveWidth, effectiveHeight);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result.Ok<Stream>(imageStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return HandleRenderException(ex, pageNumber, zoomLevel, correlationId, stopwatch.ElapsedMilliseconds, activity);
            }
                finally
                {
                    // Clean up resources
                    if (bitmap != IntPtr.Zero)
                    {
                        PdfiumInterop.DestroyBitmap(bitmap);
                    }

                    if (!pageFromCache) pageHandle?.Dispose();
                }
            }
            finally
            {
                _pdfiumSemaphore.Release();
            }
        });
    }

    /// <inheritdoc />
    public Result<(double Width, double Height)> GetPageSize(PdfDocument document, int pageNumber)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (pageNumber < 1 || pageNumber > document.PageCount)
            return Result.Fail($"Page {pageNumber} out of range 1-{document.PageCount}");

        try
        {
            using var page = PdfiumInterop.LoadPage((SafePdfDocumentHandle)document.Handle, pageNumber - 1);
            if (page.IsInvalid)
                return Result.Fail("Failed to load page");

            var w = PdfiumInterop.GetPageWidth(page);
            var h = PdfiumInterop.GetPageHeight(page);
            return Result.Ok((w, h));
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to get page size: {ex.Message}");
        }
    }

    private static Task<Stream> ConvertToPngStreamAsync(IntPtr bitmap, int width, int height)
    {
        // Get bitmap buffer
        var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
        var stride = PdfiumInterop.GetBitmapStride(bitmap);

        // PDFium uses BGRA format, copy to byte array
        var byteCount = stride * height;
        var pixelData = new byte[byteCount];
        Marshal.Copy(buffer, pixelData, 0, byteCount);

        // Create image from BGRA pixel data using ImageSharp
        var image = Image.LoadPixelData<Bgra32>(pixelData, width, height);

        // Encode to PNG stream
        var memoryStream = new MemoryStream();
        image.Save(memoryStream, new PngEncoder());

        memoryStream.Seek(0, SeekOrigin.Begin);

        return Task.FromResult<Stream>(memoryStream);
    }
}
