using System.Drawing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for inserting and manipulating images in PDF documents using PDFium.
/// Supports PNG, JPEG, BMP, and GIF formats with position, scale, and rotation operations.
/// </summary>
public sealed partial class ImageInsertionService : IImageInsertionService
{
    private readonly ILogger<ImageInsertionService> _logger;
    private const float MinImageSize = 10f;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageInsertionService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public ImageInsertionService(ILogger<ImageInsertionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<ImageObject>> InsertImageAsync(
        PdfDocument document,
        int pageIndex,
        string imagePath,
        PointF position)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Inserting image. CorrelationId={CorrelationId}, FilePath={FilePath}, PageIndex={PageIndex}, ImagePath={ImagePath}",
            correlationId, document.FilePath, pageIndex, imagePath);

        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    return Result.Fail<ImageObject>(CreateError(
                        "IMAGE_FILE_NOT_FOUND",
                        $"Image file not found: {imagePath}",
                        document.FilePath,
                        correlationId));
                }

                var fileInfo = new FileInfo(imagePath);
                if (fileInfo.Length > 50 * 1024 * 1024)
                {
                    _logger.LogWarning(
                        "Large image file detected. Size={SizeBytes} bytes. CorrelationId={CorrelationId}",
                        fileInfo.Length, correlationId);
                }

                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail<ImageObject>(CreateError(
                        "IMAGE_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                if (pageHandle.IsInvalid)
                {
                    return Result.Fail<ImageObject>(CreateError(
                        "IMAGE_INVALID_PAGE",
                        $"Failed to load page {pageIndex}.",
                        document.FilePath,
                        correlationId));
                }

                var imageObject = PdfiumInterop.CreateImageObject(documentHandle);
                if (imageObject == IntPtr.Zero)
                {
                    return Result.Fail<ImageObject>(CreateError(
                        "IMAGE_CREATE_FAILED",
                        "Failed to create image object.",
                        document.FilePath,
                        correlationId));
                }

                bool imageLoaded = false;
                SizeF imageSize = SizeF.Empty;

                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                if (extension == ".jpg" || extension == ".jpeg")
                {
                    imageLoaded = PdfiumInterop.LoadJpegFile(Array.Empty<IntPtr>(), 0, imageObject, imagePath);
                    if (imageLoaded)
                    {
                        imageSize = GetImageDimensions(imageObject);
                    }
                }
                else
                {
                    var result = LoadImageAsBitmap(imagePath, imageObject);
                    if (result.IsSuccess)
                    {
                        imageLoaded = true;
                        imageSize = result.Value;
                    }
                    else
                    {
                        PdfiumInterop.DestroyPageObject(imageObject);
                        return Result.Fail<ImageObject>(result.Errors);
                    }
                }

                if (!imageLoaded || imageSize.IsEmpty)
                {
                    PdfiumInterop.DestroyPageObject(imageObject);
                    return Result.Fail<ImageObject>(CreateError(
                        "IMAGE_LOAD_FAILED",
                        $"Failed to load image from file: {imagePath}",
                        document.FilePath,
                        correlationId));
                }

                var scaleX = imageSize.Width;
                var scaleY = imageSize.Height;
                var translateX = position.X;
                var translateY = position.Y;

                if (!PdfiumInterop.SetPageObjectMatrix(imageObject, scaleX, 0, 0, scaleY, translateX, translateY))
                {
                    PdfiumInterop.DestroyPageObject(imageObject);
                    return Result.Fail<ImageObject>(CreateError(
                        "IMAGE_MATRIX_FAILED",
                        "Failed to set image transformation matrix.",
                        document.FilePath,
                        correlationId));
                }

                PdfiumInterop.InsertPageObject(pageHandle, imageObject);

                var insertedImage = new ImageObject
                {
                    PageIndex = pageIndex,
                    Position = position,
                    Size = imageSize,
                    RotationDegrees = 0,
                    SourcePath = imagePath,
                    PdfiumHandle = imageObject
                };

                _logger.LogInformation(
                    "Image inserted successfully. CorrelationId={CorrelationId}, ImageId={ImageId}",
                    correlationId, insertedImage.Id);

                return Result.Ok(insertedImage);
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "IMAGE_INSERT_FAILED",
                    $"Failed to insert image: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image insertion failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail<ImageObject>(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> MoveImageAsync(ImageObject image, PointF newPosition)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Moving image. CorrelationId={CorrelationId}, ImageId={ImageId}, NewPosition=({X},{Y})",
            correlationId, image.Id, newPosition.X, newPosition.Y);

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

                var matrix = ComputeImageMatrix(image.Size, newPosition, image.RotationDegrees);

                if (!PdfiumInterop.SetPageObjectMatrix(image.PdfiumHandle, matrix.a, matrix.b, matrix.c, matrix.d, matrix.e, matrix.f))
                {
                    return Result.Fail(CreateError(
                        "IMAGE_MOVE_FAILED",
                        "Failed to update image position matrix.",
                        image.SourcePath,
                        correlationId));
                }

                image.Position = newPosition;
                image.ModifiedDate = DateTime.UtcNow;

                _logger.LogInformation(
                    "Image moved successfully. CorrelationId={CorrelationId}, ImageId={ImageId}",
                    correlationId, image.Id);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "IMAGE_MOVE_FAILED",
                    $"Failed to move image: {ex.Message}",
                    image.SourcePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image move failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> ScaleImageAsync(ImageObject image, SizeF newSize)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Scaling image. CorrelationId={CorrelationId}, ImageId={ImageId}, NewSize=({Width},{Height})",
            correlationId, image.Id, newSize.Width, newSize.Height);

        return await Task.Run(() =>
        {
            try
            {
                if (newSize.Width < MinImageSize || newSize.Height < MinImageSize)
                {
                    return Result.Fail(CreateError(
                        "IMAGE_SIZE_TOO_SMALL",
                        $"Image size must be at least {MinImageSize}x{MinImageSize} points.",
                        image.SourcePath,
                        correlationId));
                }

                if (image.PdfiumHandle == IntPtr.Zero)
                {
                    return Result.Fail(CreateError(
                        "IMAGE_INVALID_HANDLE",
                        "Invalid image handle.",
                        image.SourcePath,
                        correlationId));
                }

                var matrix = ComputeImageMatrix(newSize, image.Position, image.RotationDegrees);

                if (!PdfiumInterop.SetPageObjectMatrix(image.PdfiumHandle, matrix.a, matrix.b, matrix.c, matrix.d, matrix.e, matrix.f))
                {
                    return Result.Fail(CreateError(
                        "IMAGE_SCALE_FAILED",
                        "Failed to update image scale matrix.",
                        image.SourcePath,
                        correlationId));
                }

                image.Size = newSize;
                image.ModifiedDate = DateTime.UtcNow;

                _logger.LogInformation(
                    "Image scaled successfully. CorrelationId={CorrelationId}, ImageId={ImageId}",
                    correlationId, image.Id);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "IMAGE_SCALE_FAILED",
                    $"Failed to scale image: {ex.Message}",
                    image.SourcePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image scale failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }
}
