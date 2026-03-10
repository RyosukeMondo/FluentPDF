using System.Drawing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Helper methods for watermark application: per-page handlers, image loading, positioning.
/// </summary>
public sealed partial class WatermarkService
{
    private void ApplyTextWatermarkToPage(
        SafePdfDocumentHandle docHandle,
        SafePdfPageHandle pageHandle,
        int pageNum,
        IntPtr font,
        TextWatermarkConfig config,
        Guid correlationId)
    {
        var textObject = PdfiumInterop.CreateTextObject(docHandle, font, config.FontSize);
        if (textObject == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create text object for page {PageNumber}. CorrelationId={CorrelationId}", pageNum, correlationId);
            return;
        }

        if (!PdfiumInterop.SetTextObjectText(textObject, config.Text))
        {
            PdfiumInterop.DestroyPageObject(textObject);
            _logger.LogWarning("Failed to set text for page {PageNumber}. CorrelationId={CorrelationId}", pageNum, correlationId);
            return;
        }

        var alpha = (uint)(config.Opacity * 255);
        PdfiumInterop.SetPageObjectFillColor(textObject, (uint)config.Color.R, (uint)config.Color.G, (uint)config.Color.B, alpha);

        var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
        var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);
        var position = CalculatePosition(config.Position, config.CustomX, config.CustomY, pageWidth, pageHeight);

        if (!SetTransformMatrix(textObject, config.FontSize, config.FontSize, config.RotationDegrees, position))
        {
            PdfiumInterop.DestroyPageObject(textObject);
            _logger.LogWarning("Failed to set transformation matrix for page {PageNumber}. CorrelationId={CorrelationId}", pageNum, correlationId);
            return;
        }

        InsertPageObject(pageHandle, textObject, config.BehindContent);
        PdfiumInterop.MarkPageObjectDirty(pageHandle, textObject);
    }

    private void ApplyImageWatermarkToPage(
        SafePdfDocumentHandle docHandle,
        SafePdfPageHandle pageHandle,
        int pageNum,
        ImageWatermarkConfig config,
        Guid correlationId)
    {
        var imageObject = PdfiumInterop.CreateImageObject(docHandle);
        if (imageObject == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create image object for page {PageNumber}. CorrelationId={CorrelationId}", pageNum, correlationId);
            return;
        }

        var loadResult = LoadImageToObject(config.ImagePath, imageObject);
        if (loadResult.IsFailed)
        {
            PdfiumInterop.DestroyPageObject(imageObject);
            _logger.LogWarning("Failed to load image for page {PageNumber}. CorrelationId={CorrelationId}", pageNum, correlationId);
            return;
        }

        var imageSize = loadResult.Value;
        var pageWidth = PdfiumInterop.GetPageWidth(pageHandle);
        var pageHeight = PdfiumInterop.GetPageHeight(pageHandle);
        var position = CalculatePosition(config.Position, config.CustomX, config.CustomY, pageWidth, pageHeight);

        var scaledWidth = imageSize.Width * config.Scale;
        var scaledHeight = imageSize.Height * config.Scale;

        if (!SetTransformMatrix(imageObject, scaledWidth, scaledHeight, config.RotationDegrees, position))
        {
            PdfiumInterop.DestroyPageObject(imageObject);
            _logger.LogWarning("Failed to set transformation matrix for page {PageNumber}. CorrelationId={CorrelationId}", pageNum, correlationId);
            return;
        }

        InsertPageObject(pageHandle, imageObject, config.BehindContent);
        PdfiumInterop.MarkPageObjectDirty(pageHandle, imageObject);
    }

    private static bool SetTransformMatrix(IntPtr pageObject, double scaleX, double scaleY, float rotationDegrees, PointF position)
    {
        var angle = rotationDegrees * Math.PI / 180.0;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        return PdfiumInterop.SetPageObjectMatrix(pageObject,
            scaleX * cos, scaleX * sin, -scaleY * sin, scaleY * cos, position.X, position.Y);
    }

    private void InsertPageObject(SafePdfPageHandle pageHandle, IntPtr pageObject, bool behindContent)
    {
        if (behindContent)
            InsertObjectBehindContent(pageHandle, pageObject);
        else
            PdfiumInterop.InsertPageObject(pageHandle, pageObject);
    }

    private PointF CalculatePosition(WatermarkPosition position, float customX, float customY, double pageWidth, double pageHeight) =>
        position switch
        {
            WatermarkPosition.Center => new PointF((float)(pageWidth / 2), (float)(pageHeight / 2)),
            WatermarkPosition.TopLeft => new PointF((float)(pageWidth * 0.1), (float)(pageHeight * 0.9)),
            WatermarkPosition.TopRight => new PointF((float)(pageWidth * 0.9), (float)(pageHeight * 0.9)),
            WatermarkPosition.BottomLeft => new PointF((float)(pageWidth * 0.1), (float)(pageHeight * 0.1)),
            WatermarkPosition.BottomRight => new PointF((float)(pageWidth * 0.9), (float)(pageHeight * 0.1)),
            WatermarkPosition.Custom => new PointF((float)(pageWidth * customX / 100.0), (float)(pageHeight * customY / 100.0)),
            _ => new PointF((float)(pageWidth / 2), (float)(pageHeight / 2))
        };

    private void InsertObjectBehindContent(SafePdfPageHandle page, IntPtr pageObject)
    {
        PdfiumInterop.InsertPageObject(page, pageObject);
    }

    private Result<SizeF> LoadImageToObject(string imagePath, IntPtr imageObject)
    {
        try
        {
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            if (extension is ".jpg" or ".jpeg")
            {
                return PdfiumInterop.LoadJpegFile(Array.Empty<IntPtr>(), 0, imageObject, imagePath)
                    ? Result.Ok(GetImageDimensions(imageObject))
                    : Result.Fail<SizeF>("Failed to load JPEG image.");
            }

            return LoadNonJpegImage(imagePath, imageObject);
        }
        catch (Exception ex)
        {
            return Result.Fail<SizeF>($"Failed to load image: {ex.Message}");
        }
    }

    private static Result<SizeF> LoadNonJpegImage(string imagePath, IntPtr imageObject)
    {
        using var image = SixLabors.ImageSharp.Image.Load(imagePath);
        var width = image.Width;
        var height = image.Height;
        bool hasAlpha = image.PixelType.BitsPerPixel == 32;

        var pdfBitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha);
        if (pdfBitmap == IntPtr.Zero)
            return Result.Fail<SizeF>("Failed to create PDFium bitmap.");

        try
        {
            var buffer = PdfiumInterop.GetBitmapBuffer(pdfBitmap);
            var stride = PdfiumInterop.GetBitmapStride(pdfBitmap);

            if (hasAlpha)
                CopyRgba32Pixels(image.CloneAs<Rgba32>(), buffer, stride, width, height);
            else
                CopyRgb24Pixels(image.CloneAs<Rgb24>(), buffer, stride, width, height);

            return PdfiumInterop.SetImageBitmap(Array.Empty<IntPtr>(), 0, imageObject, pdfBitmap)
                ? Result.Ok(new SizeF(width, height))
                : Result.Fail<SizeF>("Failed to set bitmap on image object.");
        }
        finally
        {
            PdfiumInterop.DestroyBitmap(pdfBitmap);
        }
    }

    private static void CopyRgba32Pixels(SixLabors.ImageSharp.Image<Rgba32> image, IntPtr buffer, int stride, int width, int height)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                var destRow = buffer + (height - 1 - y) * stride;
                for (int x = 0; x < width; x++)
                {
                    var p = pixelRow[x];
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 0, p.B);
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 1, p.G);
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 2, p.R);
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 4 + 3, p.A);
                }
            }
        });
    }

    private static void CopyRgb24Pixels(SixLabors.ImageSharp.Image<Rgb24> image, IntPtr buffer, int stride, int width, int height)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                var destRow = buffer + (height - 1 - y) * stride;
                for (int x = 0; x < width; x++)
                {
                    var p = pixelRow[x];
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 3 + 0, p.B);
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 3 + 1, p.G);
                    System.Runtime.InteropServices.Marshal.WriteByte(destRow, x * 3 + 2, p.R);
                }
            }
        });
    }

    private SizeF GetImageDimensions(IntPtr imageObject)
    {
        return PdfiumInterop.GetPageObjectBounds(imageObject, out var left, out var bottom, out var right, out var top)
            ? new SizeF(right - left, top - bottom)
            : SizeF.Empty;
    }

    private string MapFontName(string fontFamily) => fontFamily.ToLowerInvariant() switch
    {
        "arial" => "Helvetica",
        "times" or "times new roman" => "Times-Roman",
        "courier" or "courier new" => "Courier",
        _ => "Helvetica"
    };

    private PdfError CreateError(string code, string message, string? filePath, Guid correlationId, Exception? exception = null)
    {
        var error = new PdfError(code, message, ErrorCategory.Rendering, ErrorSeverity.Error)
            .WithContext("CorrelationId", correlationId);

        if (filePath != null)
            error = error.WithContext("FilePath", filePath);
        if (exception != null)
            error = error.WithContext("Exception", exception.ToString());

        return error;
    }
}
