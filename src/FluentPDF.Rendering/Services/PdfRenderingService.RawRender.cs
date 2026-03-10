using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace FluentPDF.Rendering.Services;

public sealed partial class PdfRenderingService
{
    /// <inheritdoc />
    public async Task<Result<RawBitmapData>> RenderPageToRawAsync(
        PdfDocument document,
        int pageNumber,
        double zoomLevel,
        double dpi = 96)
    {
        using var activity = _activitySource.StartActivity("RenderPageToRaw");

        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        activity?.SetTag("page.number", pageNumber);
        activity?.SetTag("zoom.level", zoomLevel);
        activity?.SetTag("dpi", dpi);
        activity?.SetTag("correlation.id", correlationId.ToString());

        _logger.LogInformation(
            "Starting raw page render. CorrelationId={CorrelationId}, PageNumber={PageNumber}, ZoomLevel={ZoomLevel}, Dpi={Dpi}",
            correlationId, pageNumber, zoomLevel, dpi);

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

            await _pdfiumSemaphore.WaitAsync();
            try
            {
                try
                {
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

                var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
                var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);

                var scaleFactor = (dpi / 72.0) * zoomLevel;
                var outputWidth = (int)(pageWidth * scaleFactor);
                var outputHeight = (int)(pageHeight * scaleFactor);

                var dimError = ValidateDimensions(outputWidth, outputHeight, zoomLevel, correlationId);
                if (dimError != null) return Result.Fail(dimError);

                // Render bitmap with OOM fallback
                var bitmapResult = CreateAndRenderBitmap(
                    pageHandle, pageWidth, pageHeight, outputWidth, outputHeight,
                    dpi, zoomLevel, correlationId, activity);
                if (bitmapResult.IsFailed) return Result.Fail(bitmapResult.Errors);

                var (bmp, effectiveWidth, effectiveHeight, effectiveDpi) = bitmapResult.Value;
                bitmap = bmp;

                // Extract raw pixels directly (no PNG encode/decode)
                var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
                var stride = PdfiumInterop.GetBitmapStride(bitmap);
                var byteCount = stride * effectiveHeight;
                var pixelData = new byte[byteCount];
                Marshal.Copy(buffer, pixelData, 0, byteCount);

                stopwatch.Stop();
                activity?.SetTag("render.time.ms", stopwatch.ElapsedMilliseconds);

                if (stopwatch.ElapsedMilliseconds > SlowRenderThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow raw page render. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}",
                        correlationId, pageNumber, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "Raw page rendered successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}",
                        correlationId, pageNumber, stopwatch.ElapsedMilliseconds);
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result.Ok(new RawBitmapData(pixelData, effectiveWidth, effectiveHeight, stride));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return HandleRenderException<RawBitmapData>(ex, pageNumber, zoomLevel, correlationId, stopwatch.ElapsedMilliseconds, activity);
            }
                finally
                {
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

    #region Shared Helpers

    private PdfError? ValidatePageNumber(PdfDocument document, int pageNumber, Guid correlationId, Activity? activity)
    {
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
            return error;
        }
        return null;
    }

    private Result<(SafePdfPageHandle handle, bool fromCache)> LoadPageHandle(
        PdfDocument document, int pageNumber, Guid correlationId,
        Activity? loadPageActivity, Activity? parentActivity)
    {
        SafePdfPageHandle pageHandle;
        bool pageFromCache = false;
        var documentHandle = (SafePdfDocumentHandle)document.Handle;

        if (_pageCache != null)
        {
            var (h, cached) = _pageCache.GetOrLoad(documentHandle, pageNumber - 1);
            pageHandle = h;
            pageFromCache = cached;
        }
        else
        {
            pageHandle = PdfiumInterop.LoadPage(documentHandle, pageNumber - 1);
        }

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
            parentActivity?.SetStatus(ActivityStatusCode.Error, error.Message);
            return Result.Fail(error);
        }

        return Result.Ok((pageHandle, pageFromCache));
    }

    private PdfError? ValidateDimensions(int outputWidth, int outputHeight, double zoomLevel, Guid correlationId)
    {
        if (outputWidth <= 0 || outputHeight <= 0 || outputWidth > 8192 || outputHeight > 8192)
        {
            _logger.LogError(
                "Invalid output dimensions for rendering. CorrelationId={CorrelationId}, Width={Width}, Height={Height}",
                correlationId, outputWidth, outputHeight);

            return new PdfError(
                "PDF_RENDERING_FAILED",
                $"Invalid output dimensions: {outputWidth}x{outputHeight}. Page may be too large or zoom level too high.",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("OutputWidth", outputWidth)
                .WithContext("OutputHeight", outputHeight)
                .WithContext("ZoomLevel", zoomLevel)
                .WithContext("CorrelationId", correlationId);
        }
        return null;
    }

    private Result<(IntPtr bitmap, int effectiveWidth, int effectiveHeight, double effectiveDpi)> CreateAndRenderBitmap(
        SafePdfPageHandle pageHandle, double pageWidth, double pageHeight,
        int outputWidth, int outputHeight,
        double dpi, double zoomLevel, Guid correlationId, Activity? activity)
    {
        var effectiveDpi = dpi;
        var effectiveWidth = outputWidth;
        var effectiveHeight = outputHeight;
        var attemptedFallback = false;

        using var renderBitmapActivity = _activitySource.StartActivity("RenderBitmap");
        renderBitmapActivity?.SetTag("output.width", outputWidth);
        renderBitmapActivity?.SetTag("output.height", outputHeight);

        var bitmap = PdfiumInterop.CreateBitmap(effectiveWidth, effectiveHeight, hasAlpha: true);

        // If bitmap creation fails and we're at high DPI, try fallback to standard DPI
        if (bitmap == IntPtr.Zero && dpi > StandardDpi)
        {
            attemptedFallback = true;
            effectiveDpi = StandardDpi;
            var fallbackScaleFactor = (effectiveDpi / 72.0) * zoomLevel;
            effectiveWidth = (int)(pageWidth * fallbackScaleFactor);
            effectiveHeight = (int)(pageHeight * fallbackScaleFactor);

            _logger.LogWarning(
                "Out of memory at high DPI, attempting fallback. CorrelationId={CorrelationId}, OriginalDpi={OriginalDpi}, FallbackDpi={FallbackDpi}",
                correlationId, dpi, effectiveDpi);

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
                .WithContext("CorrelationId", correlationId);

            renderBitmapActivity?.SetStatus(ActivityStatusCode.Error, error.Message);
            activity?.SetStatus(ActivityStatusCode.Error, error.Message);
            return Result.Fail(error);
        }

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
            flags: PdfiumInterop.RenderFlags.Normal | PdfiumInterop.RenderFlags.Annotations);

        return Result.Ok((bitmap, effectiveWidth, effectiveHeight, effectiveDpi));
    }

    private void LogRenderPerformance(
        Guid correlationId, int pageNumber, long elapsedMs,
        double zoomLevel, double requestedDpi, double effectiveDpi,
        int effectiveWidth, int effectiveHeight)
    {
        var isHighDpi = requestedDpi >= HighDpiThreshold;

        if (elapsedMs > SlowRenderThresholdMs)
        {
            _logger.LogWarning(
                "Slow page render detected. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}, ZoomLevel={ZoomLevel}, Dpi={Dpi}, IsHighDpi={IsHighDpi}, OutputSize={OutputWidth}x{OutputHeight}",
                correlationId, pageNumber, elapsedMs, zoomLevel, requestedDpi, isHighDpi, effectiveWidth, effectiveHeight);
        }
        else if (isHighDpi)
        {
            _logger.LogInformation(
                "High-DPI page rendered successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}, Dpi={Dpi}, OutputSize={OutputWidth}x{OutputHeight}",
                correlationId, pageNumber, elapsedMs, requestedDpi, effectiveWidth, effectiveHeight);
        }
        else
        {
            _logger.LogInformation(
                "Page rendered successfully. CorrelationId={CorrelationId}, PageNumber={PageNumber}, RenderTimeMs={RenderTimeMs}",
                correlationId, pageNumber, elapsedMs);
        }
    }

    private Result<T> HandleRenderException<T>(
        Exception ex, int pageNumber, double zoomLevel,
        Guid correlationId, long elapsedMs, Activity? activity)
    {
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
            correlationId, pageNumber, elapsedMs);

        activity?.AddException(ex);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

        return Result.Fail(error);
    }

    // Non-generic overload for Result<Stream> return type
    private Result<Stream> HandleRenderException(
        Exception ex, int pageNumber, double zoomLevel,
        Guid correlationId, long elapsedMs, Activity? activity)
    {
        return HandleRenderException<Stream>(ex, pageNumber, zoomLevel, correlationId, elapsedMs, activity);
    }

    #endregion
}
