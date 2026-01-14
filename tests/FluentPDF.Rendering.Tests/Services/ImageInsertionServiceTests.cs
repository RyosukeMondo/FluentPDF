using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Drawing;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for ImageInsertionService.
/// Tests image insertion, manipulation operations, and error handling scenarios.
/// </summary>
public sealed class ImageInsertionServiceTests
{
    private readonly Mock<ILogger<ImageInsertionService>> _mockLogger;
    private readonly ImageInsertionService _service;

    public ImageInsertionServiceTests()
    {
        _mockLogger = new Mock<ILogger<ImageInsertionService>>();
        _service = new ImageInsertionService(_mockLogger.Object);

        // Ensure PDFium is initialized
        PdfiumInterop.Initialize();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ImageInsertionService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region InsertImageAsync Tests

    [Fact]
    public async Task InsertImageAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Arrange
        var imagePath = "test.png";
        var position = new PointF(100, 100);

        // Act & Assert
        var act = async () => await _service.InsertImageAsync(null!, 0, imagePath, position);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task InsertImageAsync_WithNullImagePath_ThrowsArgumentException()
    {
        // Arrange
        var mockHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var position = new PointF(100, 100);

        // Act & Assert
        var act = async () => await _service.InsertImageAsync(document, 0, null!, position);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("imagePath");
    }

    [Fact]
    public async Task InsertImageAsync_WithEmptyImagePath_ThrowsArgumentException()
    {
        // Arrange
        var mockHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var position = new PointF(100, 100);

        // Act & Assert
        var act = async () => await _service.InsertImageAsync(document, 0, "", position);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("imagePath");
    }

    [Fact]
    public async Task InsertImageAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var mockHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var imagePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.png");
        var position = new PointF(100, 100);

        // Act
        var result = await _service.InsertImageAsync(document, 0, imagePath, position);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("IMAGE_FILE_NOT_FOUND");
        error.Message.Should().Contain(imagePath);
    }

    [Fact]
    public async Task InsertImageAsync_WithInvalidDocumentHandle_ReturnsError()
    {
        // Arrange
        var invalidHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = invalidHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Create a temporary test image file
        var imagePath = CreateTestImage("test.png");

        try
        {
            var position = new PointF(100, 100);

            // Act
            var result = await _service.InsertImageAsync(document, 0, imagePath, position);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
            var error = result.Errors[0] as PdfError;
            error.Should().NotBeNull();
            error!.ErrorCode.Should().Be("IMAGE_INVALID_HANDLE");
        }
        finally
        {
            CleanupTestImage(imagePath);
        }
    }

    #endregion

    #region MoveImageAsync Tests

    [Fact]
    public async Task MoveImageAsync_WithNullImage_ThrowsArgumentNullException()
    {
        // Arrange
        var newPosition = new PointF(200, 200);

        // Act & Assert
        var act = async () => await _service.MoveImageAsync(null!, newPosition);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("image");
    }

    [Fact]
    public async Task MoveImageAsync_WithInvalidHandle_ReturnsError()
    {
        // Arrange
        var image = new ImageObject
        {
            PageIndex = 0,
            Position = new PointF(100, 100),
            Size = new SizeF(200, 150),
            RotationDegrees = 0,
            SourcePath = "test.png",
            PdfiumHandle = IntPtr.Zero
        };
        var newPosition = new PointF(200, 200);

        // Act
        var result = await _service.MoveImageAsync(image, newPosition);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("IMAGE_INVALID_HANDLE");
    }

    #endregion

    #region ScaleImageAsync Tests

    [Fact]
    public async Task ScaleImageAsync_WithNullImage_ThrowsArgumentNullException()
    {
        // Arrange
        var newSize = new SizeF(300, 250);

        // Act & Assert
        var act = async () => await _service.ScaleImageAsync(null!, newSize);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("image");
    }

    [Fact]
    public async Task ScaleImageAsync_WithSizeBelowMinimum_ReturnsError()
    {
        // Arrange
        var image = new ImageObject
        {
            PageIndex = 0,
            Position = new PointF(100, 100),
            Size = new SizeF(200, 150),
            RotationDegrees = 0,
            SourcePath = "test.png",
            PdfiumHandle = new IntPtr(12345)
        };
        var newSize = new SizeF(5, 5); // Below minimum of 10x10

        // Act
        var result = await _service.ScaleImageAsync(image, newSize);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("IMAGE_SIZE_TOO_SMALL");
        error.Message.Should().Contain("10");
    }

    [Fact]
    public async Task ScaleImageAsync_WithInvalidHandle_ReturnsError()
    {
        // Arrange
        var image = new ImageObject
        {
            PageIndex = 0,
            Position = new PointF(100, 100),
            Size = new SizeF(200, 150),
            RotationDegrees = 0,
            SourcePath = "test.png",
            PdfiumHandle = IntPtr.Zero
        };
        var newSize = new SizeF(300, 250);

        // Act
        var result = await _service.ScaleImageAsync(image, newSize);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("IMAGE_INVALID_HANDLE");
    }

    #endregion

    #region RotateImageAsync Tests

    [Fact]
    public async Task RotateImageAsync_WithNullImage_ThrowsArgumentNullException()
    {
        // Arrange
        var angleDegrees = 90f;

        // Act & Assert
        var act = async () => await _service.RotateImageAsync(null!, angleDegrees);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("image");
    }

    [Fact]
    public async Task RotateImageAsync_WithInvalidHandle_ReturnsError()
    {
        // Arrange
        var image = new ImageObject
        {
            PageIndex = 0,
            Position = new PointF(100, 100),
            Size = new SizeF(200, 150),
            RotationDegrees = 0,
            SourcePath = "test.png",
            PdfiumHandle = IntPtr.Zero
        };
        var angleDegrees = 90f;

        // Act
        var result = await _service.RotateImageAsync(image, angleDegrees);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("IMAGE_INVALID_HANDLE");
    }

    #endregion

    #region DeleteImageAsync Tests

    [Fact]
    public async Task DeleteImageAsync_WithNullImage_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.DeleteImageAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("image");
    }

    [Fact]
    public async Task DeleteImageAsync_WithValidImage_Succeeds()
    {
        // Arrange
        var image = new ImageObject
        {
            PageIndex = 0,
            Position = new PointF(100, 100),
            Size = new SizeF(200, 150),
            RotationDegrees = 0,
            SourcePath = "test.png",
            PdfiumHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.DeleteImageAsync(image);

        // Assert
        result.IsSuccess.Should().BeTrue();
        image.PdfiumHandle.Should().Be(IntPtr.Zero);
    }

    #endregion

    #region Helper Methods

    private string CreateTestImage(string fileName)
    {
        var testDataDir = Path.Combine(Path.GetTempPath(), "FluentPDF_Test_Images");
        Directory.CreateDirectory(testDataDir);

        var filePath = Path.Combine(testDataDir, fileName);

        // Create a minimal 1x1 PNG image
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(1, 1);
        image[0, 0] = SixLabors.ImageSharp.Color.Red;
        image.SaveAsPng(filePath);

        return filePath;
    }

    private void CleanupTestImage(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);

                // Try to cleanup directory if empty
                var directory = Path.GetDirectoryName(filePath);
                if (directory != null && Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory);
                    if (files.Length == 0)
                    {
                        Directory.Delete(directory);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion
}
