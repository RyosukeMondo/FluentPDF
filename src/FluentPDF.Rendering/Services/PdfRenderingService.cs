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
public sealed class PdfRenderingService : IPdfRenderingService
{
    private readonly ILogger<PdfRenderingService> _logger;
    private static readonly ActivitySource ActivitySource = new("FluentPDF.Rendering");
    private const int SlowRenderThresholdMs = 2000;
    private const double StandardDpi = 96.0;
    private const double HighDpiThreshold = 144.0; // 1.5x scaling

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfRenderingService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging and performance monitoring.</param>
    public PdfRenderingService(ILogger<PdfRenderingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> RenderPageAsync(
        PdfDocument document,
        int pageNumber,
        double zoomLevel,
        double dpi = 96)
    {
        using var activity = ActivitySource.StartActivity("RenderPage");

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
        if (pageNumber < 1 || pageNumber > document.PageCount)
        {
            var error = new PdfError(
                "PDF_PAGE_INVALID",
                $"Page number {pageNumber} is out of range. Valid range: 1-{document.PageCount}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("PageNumber", pageNumber)
                .WithContext("TotalPages", document.PageCount)
                .WithContext("CorrelationId", correlationId);

            _logger.LogWarning(
                "Invalid page number for rendering. CorrelationId={CorrelationId}, PageNumber={PageNumber}, TotalPages={TotalPages}",
                correlationId, pageNumber, document.PageCount);

            activity?.SetStatus(ActivityStatusCode.Error, error.Message);
            return Result.Fail(error);
        }

        return await Task.Run(async () =>
        {
            SafePdfPageHandle? pageHandle = null;
            IntPtr bitmap = IntPtr.Zero;

            try
            {
                // Load page (0-based index)
                using (var loadPageActivity = ActivitySource.StartActivity("LoadPage"))
                {
                    loadPageActivity?.SetTag("page.number", pageNumber);

                    // Cast handle to SafePdfDocumentHandle
                    var documentHandle = (SafePdfDocumentHandle)document.Handle;

                    pageHandle = PdfiumInterop.LoadPage(documentHandle, pageNumber - 1);

                    if (pageHandle.IsInvalid)
                    {
                        var error = new PdfError(
                            "PDF_PAGE_INVALID",
                            $"Failed to load page {pageNumber} for rendering.",
                            ErrorCategory.Rendering,
                            ErrorSeverity.Error)
                            .WithContext("PageNumber", pageNumber)
                            .WithContext("CorrelationId", correlationId);

                        _logger.LogError(
                            "Failed to load page for rendering. CorrelationId={CorrelationId}, PageNumber={PageNumber}",
                            correlationId, pageNumber);

                        loadPageActivity?.SetStatus(ActivityStatusCode.Error, error.Message);
                        activity?.SetStatus(ActivityStatusCode.Error, error.Message);
                        return Result.Fail(error);
                    }
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
                if (outputWidth <= 0 || outputHeight <= 0 || outputWidth > 8192 || outputHeight > 8192)
                {
                    var error = new PdfError(
                        "PDF_RENDERING_FAILED",
                        $"Invalid output dimensions: {outputWidth}x{outputHeight}. Page may be too large or zoom level too high.",
                        ErrorCategory.Validation,
                        ErrorSeverity.Error)
                        .WithContext("OutputWidth", outputWidth)
                        .WithContext("OutputHeight", outputHeight)
                        .WithContext("ZoomLevel", zoomLevel)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError(
                        "Invalid output dimensions for rendering. CorrelationId={CorrelationId}, Width={Width}, Height={Height}",
                        correlationId, outputWidth, outputHeight);

                    return Result.Fail(error);
                }

                // Variables for tracking effective render dimensions (may differ if OOM fallback occurs)
                var effectiveDpi = dpi;
                var effectiveWidth = outputWidth;
                var effectiveHeight = outputHeight;
                var attemptedFallback = false;

                // Render page to bitmap
                using (var renderBitmapActivity = ActivitySource.StartActivity("RenderBitmap"))
                {
                    renderBitmapActivity?.SetTag("output.width", outputWidth);
                    renderBitmapActivity?.SetTag("output.height", outputHeight);

                    // Try to create bitmap with OOM fallback

                    bitmap = PdfiumInterop.CreateBitmap(effectiveWidth, effectiveHeight, hasAlpha: true);

                    // If bitmap creation fails and we're at high DPI, try fallback to standard DPI
                    if (bitmap == IntPtr.Zero && dpi > StandardDpi)
                    {
                        attemptedFallback = true;
                        effectiveDpi = StandardDpi;
                        var fallbackScaleFactor = (effectiveDpi / 72.0) * zoomLevel;
                        effectiveWidth = (int)(pageWidth * fallbackScaleFactor);
                        effectiveHeight = (int)(pageHeight * fallbackScaleFactor);

                        _logger.LogWarning(
                            "Out of memory at high DPI, attempting fallback. CorrelationId={CorrelationId}, OriginalDpi={OriginalDpi}, FallbackDpi={FallbackDpi}, OriginalSize={OriginalWidth}x{OriginalHeight}, FallbackSize={FallbackWidth}x{FallbackHeight}",
                            correlationId, dpi, effectiveDpi, outputWidth, outputHeight, effectiveWidth, effectiveHeight);

                        bitmap = PdfiumInterop.CreateBitmap(effectiveWidth, effectiveHeight, hasAlpha: true);
                    }

                    if (bitmap == IntPtr.Zero)
                    {
                        var error = new PdfError(
                            "PDF_OUT_OF_MEMORY",
                            attemptedFallback
                                ? "Failed to create bitmap even at standard DPI. Out of memory or dimensions too large."
                                : "Failed to create bitmap for rendering. Out of memory or dimensions too large.",
                            ErrorCategory.System,
                            ErrorSeverity.Error)
                            .WithContext("OutputWidth", outputWidth)
                            .WithContext("OutputHeight", outputHeight)
                            .WithContext("RequestedDpi", dpi)
                            .WithContext("AttemptedFallback", attemptedFallback)
                            .WithContext("CorrelationId", correlationId);

                        _logger.LogError(
                            "Failed to create bitmap. CorrelationId={CorrelationId}, Width={Width}, Height={Height}, Dpi={Dpi}, AttemptedFallback={AttemptedFallback}",
                            correlationId, outputWidth, outputHeight, dpi, attemptedFallback);

                        renderBitmapActivity?.SetStatus(ActivityStatusCode.Error, error.Message);
                        activity?.SetStatus(ActivityStatusCode.Error, error.Message);
                        return Result.Fail(error);
                    }

                    // Log if fallback was successful
                    if (attemptedFallback)
                    {
                        _logger.LogInformation(
                            "Successfully created bitmap at fallback DPI. CorrelationId={CorrelationId}, FallbackDpi={FallbackDpi}, Size={Width}x{Height}",
                            correlationId, effectiveDpi, effectiveWidth, effectiveHeight);

                        renderBitmapActivity?.SetTag("fallback.applied", true);
                        renderBitmapActivity?.SetTag("fallback.dpi", effectiveDpi);
                    }

                    // Fill bitmap with white background (ARGB: 0xFFFFFFFF)
                    PdfiumInterop.FillBitmap(bitmap, 0xFFFFFFFF);

                    // Render page to bitmap
                    PdfiumInterop.RenderPageBitmap(
                        bitmap,
                        pageHandle,
                        startX: 0,
                        startY: 0,
                        sizeX: effectiveWidth,
                        sizeY: effectiveHeight,
                        rotate: 0,
                        flags: PdfiumInterop.RenderFlags.Normal);
                }

                // Convert bitmap to PNG stream
                Stream imageStream;
                using (var convertImageActivity = ActivitySource.StartActivity("ConvertToImage"))
                {
                    convertImageActivity?.SetTag("output.format", "PNG");
                    imageStream = await ConvertToPngStreamAsync(bitmap, effectiveWidth, effectiveHeight);
                }

                stopwatch.Stop();

                // Add render time to activity tags
                activity?.SetTag("render.time.ms", stopwatch.ElapsedMilliseconds);

                // Log performance metrics
                var isHighDpi = dpi >= HighDpiThreshold;

                if (stopwatch.ElapsedMilliseconds > SlowRenderThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow page render detected. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}, ZoomLevel={ZoomLevel}, Dpi={Dpi}, IsHighDpi={IsHighDpi}, OutputSize={OutputWidth}x{OutputHeight}",
                        correlationId, pageNumber, stopwatch.ElapsedMilliseconds, zoomLevel, dpi, isHighDpi, effectiveWidth, effectiveHeight);
                }
                else if (isHighDpi)
                {
                    // Log high-DPI renders separately for performance monitoring
                    _logger.LogInformation(
                        "High-DPI page rendered successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}, Dpi={Dpi}, OutputSize={OutputWidth}x{OutputHeight}",
                        correlationId, pageNumber, stopwatch.ElapsedMilliseconds, dpi, effectiveWidth, effectiveHeight);
                }
                else
                {
                    _logger.LogInformation(
                        "Page rendered successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}",
                        correlationId, pageNumber, stopwatch.ElapsedMilliseconds);
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result.Ok<Stream>(imageStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var error = new PdfError(
                    "PDF_RENDERING_FAILED",
                    $"Failed to render page: {ex.Message}",
                    ErrorCategory.System,
                    ErrorSeverity.Error)
                    .WithContext("PageNumber", pageNumber)
                    .WithContext("ZoomLevel", zoomLevel)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogError(ex,
                    "Failed to render page. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}",
                    correlationId, pageNumber, stopwatch.ElapsedMilliseconds);

                // Record exception in activity
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                return Result.Fail(error);
            }
            finally
            {
                // Clean up resources
                if (bitmap != IntPtr.Zero)
                {
                    PdfiumInterop.DestroyBitmap(bitmap);
                }

                pageHandle?.Dispose();
            }
        });
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
