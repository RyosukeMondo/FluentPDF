using System.Drawing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

public sealed partial class ImageInsertionService
{
    /// <inheritdoc />
    public async Task<Result> RotateImageAsync(ImageObject image, float angleDegrees)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Rotating image. CorrelationId={CorrelationId}, ImageId={ImageId}, Angle={Angle}",
            correlationId, image.Id, angleDegrees);

        return await Task.Run(() =>
        {
            try
            {
                if (image.PdfiumHandle == IntPtr.Zero)
                {
                    return Result.Fail(CreateError(
                        "IMAGE_INVALID_HANDLE",
                        "Invalid image handle.",
                        image.SourcePath,
                        correlationId));
                }

                var newRotation = image.RotationDegrees + angleDegrees;
                var matrix = ComputeImageMatrix(image.Size, image.Position, newRotation);

                if (!PdfiumInterop.SetPageObjectMatrix(image.PdfiumHandle, matrix.a, matrix.b, matrix.c, matrix.d, matrix.e, matrix.f))
                {
                    return Result.Fail(CreateError(
                        "IMAGE_ROTATE_FAILED",
                        "Failed to update image rotation matrix.",
                        image.SourcePath,
                        correlationId));
                }

                image.RotationDegrees = newRotation;
                image.ModifiedDate = DateTime.UtcNow;

                _logger.LogInformation(
                    "Image rotated successfully. CorrelationId={CorrelationId}, ImageId={ImageId}",
                    correlationId, image.Id);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "IMAGE_ROTATE_FAILED",
                    $"Failed to rotate image: {ex.Message}",
                    image.SourcePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image rotate failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteImageAsync(ImageObject image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Deleting image. CorrelationId={CorrelationId}, ImageId={ImageId}",
            correlationId, image.Id);

        return await Task.Run(() =>
        {
            try
            {
                if (image.PdfiumHandle != IntPtr.Zero)
                {
                    PdfiumInterop.DestroyPageObject(image.PdfiumHandle);
                    image.PdfiumHandle = IntPtr.Zero;
                }

                _logger.LogInformation(
                    "Image deleted successfully. CorrelationId={CorrelationId}, ImageId={ImageId}",
                    correlationId, image.Id);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "IMAGE_DELETE_FAILED",
                    $"Failed to delete image: {ex.Message}",
                    image.SourcePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image delete failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <summary>
    /// Computes the image transformation matrix for a given size, position, and rotation.
    /// </summary>
    private static (double a, double b, double c, double d, double e, double f) ComputeImageMatrix(
        SizeF size, PointF position, float rotationDegrees)
    {
        var angle = rotationDegrees * Math.PI / 180.0;
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        var scaleX = (double)size.Width;
        var scaleY = (double)size.Height;

        return (
            a: scaleX * cos,
            b: scaleX * sin,
            c: -scaleY * sin,
            d: scaleY * cos,
            e: position.X,
            f: position.Y
        );
    }

    private Result<SizeF> LoadImageAsBitmap(string imagePath, IntPtr imageObject)
    {
        try
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
        catch (Exception ex)
        {
            return Result.Fail<SizeF>($"Failed to load image as bitmap: {ex.Message}");
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
