using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Drawing;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

public sealed class WatermarkServiceTests : IDisposable
{
    private readonly Mock<ILogger<WatermarkService>> _mockLogger;
    private readonly Mock<IPdfRenderingService> _mockRenderingService;
    private readonly WatermarkService _service;
    private readonly List<string> _tempFiles = new();

    public WatermarkServiceTests()
    {
        _mockLogger = new Mock<ILogger<WatermarkService>>();
        _mockRenderingService = new Mock<IPdfRenderingService>();
        _service = new WatermarkService(_mockLogger.Object, _mockRenderingService.Object);

        PdfiumInterop.Initialize();
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try { File.Delete(file); } catch { /* Ignore cleanup errors */ }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new WatermarkService(null!, _mockRenderingService.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullRenderingService_ThrowsArgumentNullException()
    {
        var act = () => new WatermarkService(_mockLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("renderingService");
    }

    #endregion

    #region ApplyTextWatermarkAsync Tests

    [Fact]
    public async Task ApplyTextWatermarkAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        var config = new TextWatermarkConfig { Text = "Test" };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(null!, config, pageRange);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task ApplyTextWatermarkAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        var document = CreateMockDocument();
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, null!, pageRange);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public async Task ApplyTextWatermarkAsync_WithEmptyText_ThrowsArgumentException()
    {
        var document = CreateMockDocument();
        var config = new TextWatermarkConfig { Text = "" };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public async Task ApplyTextWatermarkAsync_WithInvalidDocumentHandle_ReturnsError()
    {
        var invalidHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = invalidHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var config = new TextWatermarkConfig { Text = "CONFIDENTIAL" };
        var pageRange = WatermarkPageRange.All;

        var result = await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("WATERMARK_INVALID_HANDLE");
    }

    [Fact]
    public async Task ApplyTextWatermarkAsync_WithNoPages_ReturnsError()
    {
        var invalidHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 10,
            Handle = invalidHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var config = new TextWatermarkConfig { Text = "DRAFT" };
        var pageRange = WatermarkPageRange.Parse("15-20");

        var result = await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("WATERMARK_NO_PAGES");
    }

    [Theory]
    [InlineData("CONFIDENTIAL")]
    [InlineData("DRAFT")]
    [InlineData("© 2026")]
    [InlineData("Test with special chars: Ω α β")]
    public async Task ApplyTextWatermarkAsync_WithDifferentTexts_AcceptsText(string watermarkText)
    {
        var document = CreateMockDocument();
        var config = new TextWatermarkConfig
        {
            Text = watermarkText,
            FontFamily = "Arial",
            FontSize = 72,
            Color = Color.Gray,
            Opacity = 0.5f
        };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(WatermarkPosition.Center)]
    [InlineData(WatermarkPosition.TopLeft)]
    [InlineData(WatermarkPosition.TopRight)]
    [InlineData(WatermarkPosition.BottomLeft)]
    [InlineData(WatermarkPosition.BottomRight)]
    public async Task ApplyTextWatermarkAsync_WithDifferentPositions_AcceptsPosition(WatermarkPosition position)
    {
        var document = CreateMockDocument();
        var config = new TextWatermarkConfig
        {
            Text = "WATERMARK",
            Position = position
        };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyTextWatermarkAsync_WithCustomPosition_AcceptsPosition()
    {
        var document = CreateMockDocument();
        var config = new TextWatermarkConfig
        {
            Text = "CUSTOM",
            Position = WatermarkPosition.Custom,
            CustomX = 25f,
            CustomY = 75f
        };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(45f)]
    [InlineData(90f)]
    [InlineData(-45f)]
    [InlineData(180f)]
    public async Task ApplyTextWatermarkAsync_WithDifferentRotations_AcceptsRotation(float rotation)
    {
        var document = CreateMockDocument();
        var config = new TextWatermarkConfig
        {
            Text = "ROTATED",
            RotationDegrees = rotation
        };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApplyTextWatermarkAsync_WithBehindContentFlag_AcceptsFlag(bool behindContent)
    {
        var document = CreateMockDocument();
        var config = new TextWatermarkConfig
        {
            Text = "BACKGROUND",
            BehindContent = behindContent
        };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyTextWatermarkAsync(document, config, pageRange);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ApplyImageWatermarkAsync Tests

    [Fact]
    public async Task ApplyImageWatermarkAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        var config = new ImageWatermarkConfig { ImagePath = "test.png" };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyImageWatermarkAsync(null!, config, pageRange);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task ApplyImageWatermarkAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        var document = CreateMockDocument();
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyImageWatermarkAsync(document, null!, pageRange);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public async Task ApplyImageWatermarkAsync_WithEmptyImagePath_ThrowsArgumentException()
    {
        var document = CreateMockDocument();
        var config = new ImageWatermarkConfig { ImagePath = "" };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyImageWatermarkAsync(document, config, pageRange);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public async Task ApplyImageWatermarkAsync_WithNonExistentFile_ReturnsError()
    {
        var document = CreateMockDocument();
        var imagePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.png");
        var config = new ImageWatermarkConfig { ImagePath = imagePath };
        var pageRange = WatermarkPageRange.All;

        var result = await _service.ApplyImageWatermarkAsync(document, config, pageRange);

        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("WATERMARK_IMAGE_NOT_FOUND");
        error.Message.Should().Contain(imagePath);
    }

    [Fact]
    public async Task ApplyImageWatermarkAsync_WithInvalidDocumentHandle_ReturnsError()
    {
        var invalidHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = invalidHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var imagePath = CreateTestImage("watermark.png");
        var config = new ImageWatermarkConfig { ImagePath = imagePath };
        var pageRange = WatermarkPageRange.All;

        var result = await _service.ApplyImageWatermarkAsync(document, config, pageRange);

        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("WATERMARK_INVALID_HANDLE");
    }

    [Theory]
    [InlineData(0.1f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    public async Task ApplyImageWatermarkAsync_WithDifferentScales_AcceptsScale(float scale)
    {
        var document = CreateMockDocument();
        var imagePath = CreateTestImage($"scaled_{scale}.png");
        var config = new ImageWatermarkConfig
        {
            ImagePath = imagePath,
            Scale = scale
        };
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.ApplyImageWatermarkAsync(document, config, pageRange);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RemoveWatermarksAsync Tests

    [Fact]
    public async Task RemoveWatermarksAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        var pageRange = WatermarkPageRange.All;

        var act = async () => await _service.RemoveWatermarksAsync(null!, pageRange);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task RemoveWatermarksAsync_WithInvalidDocumentHandle_ReturnsError()
    {
        var invalidHandle = new SafePdfDocumentHandle();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = invalidHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
        var pageRange = WatermarkPageRange.All;

        var result = await _service.RemoveWatermarksAsync(document, pageRange);

        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("WATERMARK_INVALID_HANDLE");
    }

    #endregion

    #region GeneratePreviewAsync Tests

    [Fact]
    public async Task GeneratePreviewAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        var textConfig = new TextWatermarkConfig { Text = "PREVIEW" };

        var act = async () => await _service.GeneratePreviewAsync(null!, 0, textConfig, null);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task GeneratePreviewAsync_WithBothConfigsNull_ThrowsArgumentException()
    {
        var document = CreateMockDocument();

        var act = async () => await _service.GeneratePreviewAsync(document, 0, null, null);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be provided*");
    }

    [Fact]
    public async Task GeneratePreviewAsync_WithBothConfigsProvided_ThrowsArgumentException()
    {
        var document = CreateMockDocument();
        var textConfig = new TextWatermarkConfig { Text = "PREVIEW" };
        var imageConfig = new ImageWatermarkConfig { ImagePath = "test.png" };

        var act = async () => await _service.GeneratePreviewAsync(document, 0, textConfig, imageConfig);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Only one*");
    }

    [Fact]
    public async Task GeneratePreviewAsync_WithTextConfig_CallsRenderingService()
    {
        var document = CreateMockDocument();
        var textConfig = new TextWatermarkConfig { Text = "PREVIEW" };
        var mockStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        _mockRenderingService
            .Setup(x => x.RenderPageAsync(document, 0, 1.0, 150))
            .ReturnsAsync(FluentResults.Result.Ok<Stream>(mockStream));

        var result = await _service.GeneratePreviewAsync(document, 0, textConfig, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _mockRenderingService.Verify(x => x.RenderPageAsync(document, 0, 1.0, 150), Times.Once);
    }

    [Fact]
    public async Task GeneratePreviewAsync_WithImageConfig_CallsRenderingService()
    {
        var document = CreateMockDocument();
        var imagePath = CreateTestImage("preview.png");
        var imageConfig = new ImageWatermarkConfig { ImagePath = imagePath };
        var mockStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        _mockRenderingService
            .Setup(x => x.RenderPageAsync(document, 0, 1.0, 150))
            .ReturnsAsync(FluentResults.Result.Ok<Stream>(mockStream));

        var result = await _service.GeneratePreviewAsync(document, 0, null, imageConfig);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _mockRenderingService.Verify(x => x.RenderPageAsync(document, 0, 1.0, 150), Times.Once);
    }

    [Fact]
    public async Task GeneratePreviewAsync_WhenRenderingFails_ReturnsError()
    {
        var document = CreateMockDocument();
        var textConfig = new TextWatermarkConfig { Text = "PREVIEW" };
        var renderError = new PdfError("RENDER_ERROR", "Render failed", ErrorCategory.Rendering, ErrorSeverity.Error);

        _mockRenderingService
            .Setup(x => x.RenderPageAsync(document, 0, 1.0, 150))
            .ReturnsAsync(FluentResults.Result.Fail<Stream>(renderError));

        var result = await _service.GeneratePreviewAsync(document, 0, textConfig, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(renderError);
    }

    #endregion

    #region Helper Methods

    private PdfDocument CreateMockDocument()
    {
        return new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 5,
            Handle = new SafePdfDocumentHandle(),
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
    }

    private string CreateTestImage(string fileName)
    {
        var testDataDir = Path.Combine(Path.GetTempPath(), "FluentPDF_Watermark_Test");
        Directory.CreateDirectory(testDataDir);

        var filePath = Path.Combine(testDataDir, fileName);
        _tempFiles.Add(filePath);

        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(100, 100);
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                image[x, y] = SixLabors.ImageSharp.Color.Red;
            }
        }
        image.Save(filePath, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

        return filePath;
    }

    #endregion
}
