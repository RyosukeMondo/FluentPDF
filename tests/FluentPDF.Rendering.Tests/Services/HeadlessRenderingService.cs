using FluentPDF.Core.ErrorHandling;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Headless PDF rendering service using Win2D for CI-compatible screenshot generation.
/// Renders PDF pages to PNG files without requiring a UI context.
/// </summary>
public sealed class HeadlessRenderingService : IHeadlessRenderingService
{
    private readonly ILogger<HeadlessRenderingService> _logger;
    private readonly CanvasDevice _canvasDevice;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessRenderingService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public HeadlessRenderingService(ILogger<HeadlessRenderingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize PDFium library
        if (!PdfiumInterop.Initialize())
        {
            throw new InvalidOperationException("Failed to initialize PDFium library.");
        }

        // Get shared Canvas device for headless rendering
        try
        {
            _canvasDevice = CanvasDevice.GetSharedDevice();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Win2D CanvasDevice");
            throw new InvalidOperationException("Failed to initialize Win2D CanvasDevice. Ensure Win2D is properly installed.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RenderPageToFileAsync(
        string pdfPath,
        int pageNumber,
        string outputPath,
        int dpi = 96,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeadlessRenderingService));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Rendering PDF page to file. CorrelationId={CorrelationId}, PdfPath={PdfPath}, PageNumber={PageNumber}, OutputPath={OutputPath}, DPI={DPI}",
            correlationId, pdfPath, pageNumber, outputPath, dpi);

        // Validate input
        if (!File.Exists(pdfPath))
        {
            var error = new PdfError(
                "PDF_FILE_NOT_FOUND",
                $"PDF file not found: {pdfPath}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("PdfPath", pdfPath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError("PDF file not found. CorrelationId={CorrelationId}, PdfPath={PdfPath}", correlationId, pdfPath);
            return Result.Fail(error);
        }

        if (pageNumber < 1)
        {
            var error = new PdfError(
                "PAGE_INVALID",
                $"Page number must be >= 1. Got: {pageNumber}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("PageNumber", pageNumber)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError("Invalid page number. CorrelationId={CorrelationId}, PageNumber={PageNumber}", correlationId, pageNumber);
            return Result.Fail(error);
        }

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            try
            {
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "IO_ERROR",
                    $"Failed to create output directory: {ex.Message}",
                    ErrorCategory.IO,
                    ErrorSeverity.Error)
                    .WithContext("OutputPath", outputPath)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogError(ex, "Failed to create output directory. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail(error);
            }
        }

        // Render on background thread
        return await Task.Run(async () =>
        {
            SafePdfDocumentHandle? documentHandle = null;
            SafePdfPageHandle? pageHandle = null;
            IntPtr bitmapHandle = IntPtr.Zero;

            try
            {
                // Load PDF document
                documentHandle = PdfiumInterop.LoadDocument(pdfPath);
                if (documentHandle.IsInvalid)
                {
                    var errorCode = PdfiumInterop.GetLastError();
                    var error = new PdfError(
                        "PDF_LOAD_FAILED",
                        $"Failed to load PDF. PDFium error code: {errorCode}",
                        ErrorCategory.External,
                        ErrorSeverity.Error)
                        .WithContext("PdfPath", pdfPath)
                        .WithContext("ErrorCode", errorCode)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError("Failed to load PDF. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}", correlationId, errorCode);
                    return Result.Fail(error);
                }

                // Validate page number
                var pageCount = PdfiumInterop.GetPageCount(documentHandle);
                if (pageNumber > pageCount)
                {
                    var error = new PdfError(
                        "PAGE_INVALID",
                        $"Page number {pageNumber} exceeds document page count {pageCount}",
                        ErrorCategory.Validation,
                        ErrorSeverity.Error)
                        .WithContext("PageNumber", pageNumber)
                        .WithContext("PageCount", pageCount)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError("Invalid page number. CorrelationId={CorrelationId}, PageNumber={PageNumber}, PageCount={PageCount}",
                        correlationId, pageNumber, pageCount);
                    return Result.Fail(error);
                }

                // Load page (0-based index)
                pageHandle = PdfiumInterop.LoadPage(documentHandle, pageNumber - 1);
                if (pageHandle.IsInvalid)
                {
                    var error = new PdfError(
                        "PAGE_LOAD_FAILED",
                        $"Failed to load page {pageNumber}",
                        ErrorCategory.External,
                        ErrorSeverity.Error)
                        .WithContext("PageNumber", pageNumber)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError("Failed to load page. CorrelationId={CorrelationId}, PageNumber={PageNumber}", correlationId, pageNumber);
                    return Result.Fail(error);
                }

                // Get page dimensions and calculate pixel size
                var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
                var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);
                var scale = dpi / 72.0; // 72 DPI is PDF standard
                var pixelWidth = (int)(pageWidth * scale);
                var pixelHeight = (int)(pageHeight * scale);

                _logger.LogDebug(
                    "Page dimensions: {Width}x{Height} points, {PixelWidth}x{PixelHeight} pixels at {DPI} DPI",
                    pageWidth, pageHeight, pixelWidth, pixelHeight, dpi);

                // Create PDFium bitmap
                bitmapHandle = PdfiumInterop.CreateBitmap(pixelWidth, pixelHeight, hasAlpha: true);
                if (bitmapHandle == IntPtr.Zero)
                {
                    var error = new PdfError(
                        "RENDERING_FAILED",
                        "Failed to create bitmap",
                        ErrorCategory.External,
                        ErrorSeverity.Error)
                        .WithContext("Width", pixelWidth)
                        .WithContext("Height", pixelHeight)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError("Failed to create bitmap. CorrelationId={CorrelationId}", correlationId);
                    return Result.Fail(error);
                }

                // Fill with white background
                PdfiumInterop.FillBitmap(bitmapHandle, 0xFFFFFFFF);

                // Render page to bitmap
                PdfiumInterop.RenderPageBitmap(
                    bitmapHandle,
                    pageHandle,
                    startX: 0,
                    startY: 0,
                    sizeX: pixelWidth,
                    sizeY: pixelHeight,
                    rotate: 0,
                    flags: 0); // 0 = normal rendering with antialiasing

                // Get bitmap buffer
                var bufferPtr = PdfiumInterop.GetBitmapBuffer(bitmapHandle);
                var stride = PdfiumInterop.GetBitmapStride(bitmapHandle);
                var bufferSize = stride * pixelHeight;

                // Copy bitmap data to managed array
                var bitmapData = new byte[bufferSize];
                Marshal.Copy(bufferPtr, bitmapData, 0, bufferSize);

                // Convert BGRA to RGBA (PDFium uses BGRA, Win2D expects RGBA)
                for (int i = 0; i < bufferSize; i += 4)
                {
                    var b = bitmapData[i];
                    var r = bitmapData[i + 2];
                    bitmapData[i] = r;
                    bitmapData[i + 2] = b;
                }

                // Save using Win2D
                await SaveBitmapAsync(bitmapData, pixelWidth, pixelHeight, outputPath, cancellationToken);

                _logger.LogInformation(
                    "Successfully rendered PDF page to file. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);

                return Result.Ok();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Rendering operation cancelled. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail(new PdfError(
                    "OPERATION_CANCELLED",
                    "Rendering operation was cancelled",
                    ErrorCategory.User,
                    ErrorSeverity.Warning)
                    .WithContext("CorrelationId", correlationId));
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "RENDERING_FAILED",
                    $"Unexpected error during rendering: {ex.Message}",
                    ErrorCategory.External,
                    ErrorSeverity.Error)
                    .WithContext("Exception", ex.GetType().Name)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogError(ex, "Unexpected error during rendering. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail(error);
            }
            finally
            {
                // Clean up resources
                if (bitmapHandle != IntPtr.Zero)
                {
                    PdfiumInterop.DestroyBitmap(bitmapHandle);
                }
                pageHandle?.Dispose();
                documentHandle?.Dispose();
            }
        }, cancellationToken);
    }

    private async Task SaveBitmapAsync(byte[] pixels, int width, int height, string outputPath, CancellationToken cancellationToken)
    {
        // Use Win2D CanvasRenderTarget to save the bitmap
        using var renderTarget = new CanvasRenderTarget(_canvasDevice, width, height, 96);

        // Set pixel data
        renderTarget.SetPixelBytes(pixels);

        // Save to PNG file
        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        await renderTarget.SaveAsync(fileStream.AsRandomAccessStream(), CanvasBitmapFileFormat.Png);
    }

    /// <summary>
    /// Disposes the service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _canvasDevice?.Dispose();
        _disposed = true;
    }
}
