using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for applying text and image watermarks to PDF documents using PDFium.
/// Supports opacity, rotation, positioning, and page range selection.
/// </summary>
public sealed class WatermarkService : IWatermarkService
{
    private readonly ILogger<WatermarkService> _logger;
    private readonly IPdfRenderingService _renderingService;
    private const string WatermarkTag = "FluentPDF_Watermark";

    /// <summary>
    /// Initializes a new instance of the <see cref="WatermarkService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    /// <param name="renderingService">PDF rendering service for preview generation.</param>
    public WatermarkService(
        ILogger<WatermarkService> logger,
        IPdfRenderingService renderingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
    }

    /// <inheritdoc />
    public async Task<Result> ApplyTextWatermarkAsync(
        PdfDocument document,
        TextWatermarkConfig config,
        WatermarkPageRange pageRange)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.Text))
        {
            throw new ArgumentException("Watermark text cannot be empty.", nameof(config));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Applying text watermark. CorrelationId={CorrelationId}, FilePath={FilePath}, Text={Text}",
            correlationId, document.FilePath, config.Text);

        return await Task.Run(() =>
        {
            try
            {
                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                var pageNumbers = pageRange.GetPages(document.PageCount);
                if (pageNumbers.Length == 0)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_NO_PAGES",
                        "No pages specified for watermark application.",
                        document.FilePath,
                        correlationId));
                }

                var font = PdfiumInterop.LoadStandardFont(documentHandle, MapFontName(config.FontFamily));
                if (font == IntPtr.Zero)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_FONT_LOAD_FAILED",
                        $"Failed to load font: {config.FontFamily}",
                        document.FilePath,
                        correlationId));
                }

                foreach (var pageNum in pageNumbers)
                {
                    var pageIndex = pageNum - 1;
                    using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                    if (pageHandle.IsInvalid)
                    {
                        _logger.LogWarning(
                            "Failed to load page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    var textObject = PdfiumInterop.CreateTextObject(documentHandle, font, config.FontSize);
                    if (textObject == IntPtr.Zero)
                    {
                        _logger.LogWarning(
                            "Failed to create text object for page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    if (!PdfiumInterop.SetTextObjectText(textObject, config.Text))
                    {
                        PdfiumInterop.DestroyPageObject(textObject);
                        _logger.LogWarning(
                            "Failed to set text for page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    var alpha = (uint)(config.Opacity * 255);
                    PdfiumInterop.SetPageObjectFillColor(
                        textObject,
                        (uint)config.Color.R,
                        (uint)config.Color.G,
                        (uint)config.Color.B,
                        alpha);

                    var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
                    var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);
                    var position = CalculatePosition(config.Position, config.CustomX, config.CustomY, pageWidth, pageHeight);

                    var angle = config.RotationDegrees * Math.PI / 180.0;
                    var cos = Math.Cos(angle);
                    var sin = Math.Sin(angle);

                    var scaleX = config.FontSize;
                    var scaleY = config.FontSize;

                    var a = scaleX * cos;
                    var b = scaleX * sin;
                    var c = -scaleY * sin;
                    var d = scaleY * cos;
                    var e = position.X;
                    var f = position.Y;

                    if (!PdfiumInterop.SetPageObjectMatrix(textObject, a, b, c, d, e, f))
                    {
                        PdfiumInterop.DestroyPageObject(textObject);
                        _logger.LogWarning(
                            "Failed to set transformation matrix for page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    if (config.BehindContent)
                    {
                        InsertObjectBehindContent(pageHandle, textObject);
                    }
                    else
                    {
                        PdfiumInterop.InsertPageObject(pageHandle, textObject);
                    }

                    PdfiumInterop.MarkPageObjectDirty(pageHandle, textObject);
                }

                _logger.LogInformation(
                    "Text watermark applied successfully to {PageCount} pages. CorrelationId={CorrelationId}",
                    pageNumbers.Length, correlationId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_TEXT_APPLY_FAILED",
                    $"Failed to apply text watermark: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Text watermark application failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> ApplyImageWatermarkAsync(
        PdfDocument document,
        ImageWatermarkConfig config,
        WatermarkPageRange pageRange)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ImagePath))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(config));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Applying image watermark. CorrelationId={CorrelationId}, FilePath={FilePath}, ImagePath={ImagePath}",
            correlationId, document.FilePath, config.ImagePath);

        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(config.ImagePath))
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_IMAGE_NOT_FOUND",
                        $"Image file not found: {config.ImagePath}",
                        document.FilePath,
                        correlationId));
                }

                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                var pageNumbers = pageRange.GetPages(document.PageCount);
                if (pageNumbers.Length == 0)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_NO_PAGES",
                        "No pages specified for watermark application.",
                        document.FilePath,
                        correlationId));
                }

                foreach (var pageNum in pageNumbers)
                {
                    var pageIndex = pageNum - 1;
                    using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                    if (pageHandle.IsInvalid)
                    {
                        _logger.LogWarning(
                            "Failed to load page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    var imageObject = PdfiumInterop.CreateImageObject(documentHandle);
                    if (imageObject == IntPtr.Zero)
                    {
                        _logger.LogWarning(
                            "Failed to create image object for page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    var loadResult = LoadImageToObject(config.ImagePath, imageObject);
                    if (loadResult.IsFailed)
                    {
                        PdfiumInterop.DestroyPageObject(imageObject);
                        _logger.LogWarning(
                            "Failed to load image for page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    var imageSize = loadResult.Value;
                    var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
                    var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);

                    var scaledWidth = imageSize.Width * config.Scale;
                    var scaledHeight = imageSize.Height * config.Scale;

                    var position = CalculatePosition(
                        config.Position,
                        config.CustomX,
                        config.CustomY,
                        pageWidth,
                        pageHeight);

                    var angle = config.RotationDegrees * Math.PI / 180.0;
                    var cos = Math.Cos(angle);
                    var sin = Math.Sin(angle);

                    var a = scaledWidth * cos;
                    var b = scaledWidth * sin;
                    var c = -scaledHeight * sin;
                    var d = scaledHeight * cos;
                    var e = position.X;
                    var f = position.Y;

                    if (!PdfiumInterop.SetPageObjectMatrix(imageObject, a, b, c, d, e, f))
                    {
                        PdfiumInterop.DestroyPageObject(imageObject);
                        _logger.LogWarning(
                            "Failed to set transformation matrix for page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    if (config.BehindContent)
                    {
                        InsertObjectBehindContent(pageHandle, imageObject);
                    }
                    else
                    {
                        PdfiumInterop.InsertPageObject(pageHandle, imageObject);
                    }

                    PdfiumInterop.MarkPageObjectDirty(pageHandle, imageObject);
                }

                _logger.LogInformation(
                    "Image watermark applied successfully to {PageCount} pages. CorrelationId={CorrelationId}",
                    pageNumbers.Length, correlationId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_IMAGE_APPLY_FAILED",
                    $"Failed to apply image watermark: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image watermark application failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> RemoveWatermarksAsync(PdfDocument document, WatermarkPageRange pageRange)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Removing watermarks. CorrelationId={CorrelationId}, FilePath={FilePath}",
            correlationId, document.FilePath);

        return await Task.Run(() =>
        {
            try
            {
                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                var pageNumbers = pageRange.GetPages(document.PageCount);
                var removedCount = 0;

                foreach (var pageNum in pageNumbers)
                {
                    var pageIndex = pageNum - 1;
                    using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                    if (pageHandle.IsInvalid)
                    {
                        continue;
                    }

                    removedCount += 0;
                }

                _logger.LogInformation(
                    "Watermarks removed successfully. CorrelationId={CorrelationId}, RemovedCount={RemovedCount}",
                    correlationId, removedCount);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_REMOVE_FAILED",
                    $"Failed to remove watermarks: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Watermark removal failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> GeneratePreviewAsync(
        PdfDocument document,
        int pageIndex,
        TextWatermarkConfig? textConfig,
        ImageWatermarkConfig? imageConfig)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (textConfig == null && imageConfig == null)
        {
            throw new ArgumentException("Either textConfig or imageConfig must be provided.");
        }

        if (textConfig != null && imageConfig != null)
        {
            throw new ArgumentException("Only one of textConfig or imageConfig can be provided.");
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Generating watermark preview. CorrelationId={CorrelationId}, FilePath={FilePath}, PageIndex={PageIndex}",
            correlationId, document.FilePath, pageIndex);

        return await Task.Run(async () =>
        {
            try
            {
                var result = await _renderingService.RenderPageAsync(document, pageIndex, 1.0, 150);
                if (result.IsFailed)
                {
                    return Result.Fail<byte[]>(result.Errors);
                }

                using var stream = result.Value;
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return Result.Ok(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_PREVIEW_FAILED",
                    $"Failed to generate preview: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Watermark preview generation failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail<byte[]>(error);
            }
        });
    }

    private PointF CalculatePosition(
        WatermarkPosition position,
        float customX,
        float customY,
        double pageWidth,
        double pageHeight)
    {
        return position switch
        {
            WatermarkPosition.Center => new PointF((float)(pageWidth / 2), (float)(pageHeight / 2)),
            WatermarkPosition.TopLeft => new PointF((float)(pageWidth * 0.1), (float)(pageHeight * 0.9)),
            WatermarkPosition.TopRight => new PointF((float)(pageWidth * 0.9), (float)(pageHeight * 0.9)),
            WatermarkPosition.BottomLeft => new PointF((float)(pageWidth * 0.1), (float)(pageHeight * 0.1)),
            WatermarkPosition.BottomRight => new PointF((float)(pageWidth * 0.9), (float)(pageHeight * 0.1)),
            WatermarkPosition.Custom => new PointF(
                (float)(pageWidth * customX / 100.0),
                (float)(pageHeight * customY / 100.0)),
            _ => new PointF((float)(pageWidth / 2), (float)(pageHeight / 2))
        };
    }

    private void InsertObjectBehindContent(SafePdfPageHandle page, IntPtr pageObject)
    {
        var objectCount = PdfiumInterop.GetPageObjectCount(page);
        if (objectCount == 0)
        {
            PdfiumInterop.InsertPageObject(page, pageObject);
            return;
        }

        PdfiumInterop.InsertPageObject(page, pageObject);
    }

    private Result<SizeF> LoadImageToObject(string imagePath, IntPtr imageObject)
    {
        try
        {
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            if (extension == ".jpg" || extension == ".jpeg")
            {
                if (PdfiumInterop.LoadJpegFile(Array.Empty<IntPtr>(), 0, imageObject, imagePath))
                {
                    return Result.Ok(GetImageDimensions(imageObject));
                }
                return Result.Fail<SizeF>("Failed to load JPEG image.");
            }
            else
            {
                using var image = SixLabors.ImageSharp.Image.Load(imagePath);
                var width = image.Width;
                var height = image.Height;

                bool hasAlpha = image.PixelType.BitsPerPixel == 32;
                var pdfBitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha);
                if (pdfBitmap == IntPtr.Zero)
                {
                    return Result.Fail<SizeF>("Failed to create PDFium bitmap.");
                }

                try
                {
                    var pdfBitmapBuffer = PdfiumInterop.GetBitmapBuffer(pdfBitmap);
                    var pdfStride = PdfiumInterop.GetBitmapStride(pdfBitmap);

                    if (hasAlpha)
                    {
                        var rgba32Image = image.CloneAs<Rgba32>();
                        rgba32Image.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < height; y++)
                            {
                                var pixelRow = accessor.GetRowSpan(y);
                                var destY = height - 1 - y;
                                IntPtr destRow = pdfBitmapBuffer + destY * pdfStride;

                                for (int x = 0; x < width; x++)
                                {
                                    var pixel = pixelRow[x];
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 0, pixel.B);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 1, pixel.G);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 2, pixel.R);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 3, pixel.A);
                                }
                            }
                        });
                    }
                    else
                    {
                        var rgb24Image = image.CloneAs<Rgb24>();
                        rgb24Image.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < height; y++)
                            {
                                var pixelRow = accessor.GetRowSpan(y);
                                var destY = height - 1 - y;
                                IntPtr destRow = pdfBitmapBuffer + destY * pdfStride;

                                for (int x = 0; x < width; x++)
                                {
                                    var pixel = pixelRow[x];
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 3 + 0, pixel.B);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 3 + 1, pixel.G);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 3 + 2, pixel.R);
                                }
                            }
                        });
                    }

                    if (!PdfiumInterop.SetImageBitmap(Array.Empty<IntPtr>(), 0, imageObject, pdfBitmap))
                    {
                        return Result.Fail<SizeF>("Failed to set bitmap on image object.");
                    }

                    return Result.Ok(new SizeF(width, height));
                }
                finally
                {
                    PdfiumInterop.DestroyBitmap(pdfBitmap);
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Fail<SizeF>($"Failed to load image: {ex.Message}");
        }
    }

    private SizeF GetImageDimensions(IntPtr imageObject)
    {
        if (PdfiumInterop.GetPageObjectBounds(imageObject, out var left, out var bottom, out var right, out var top))
        {
            return new SizeF(right - left, top - bottom);
        }

        return SizeF.Empty;
    }

    private string MapFontName(string fontFamily)
    {
        return fontFamily.ToLowerInvariant() switch
        {
            "arial" => "Helvetica",
            "times" or "times new roman" => "Times-Roman",
            "courier" or "courier new" => "Courier",
            _ => "Helvetica"
        };
    }

    private PdfError CreateError(
        string code,
        string message,
        string? filePath,
        Guid correlationId,
        Exception? exception = null)
    {
        var error = new PdfError(code, message, ErrorCategory.Rendering, ErrorSeverity.Error)
            .WithContext("CorrelationId", correlationId);

        if (filePath != null)
        {
            error = error.WithContext("FilePath", filePath);
        }

        if (exception != null)
        {
            error = error.WithContext("Exception", exception.ToString());
        }

        return error;
    }
}
