using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for watermark feature.
/// Tests the interaction between WatermarkViewModel and IWatermarkService.
/// </summary>
public sealed class WatermarkTests : IDisposable
{
    private readonly Mock<IWatermarkService> _watermarkServiceMock;
    private readonly Mock<ILogger<WatermarkViewModel>> _loggerMock;
    private readonly PdfDocument _testDocument;
    private readonly byte[] _samplePreviewImage;

    public WatermarkTests()
    {
        _watermarkServiceMock = new Mock<IWatermarkService>();
        _loggerMock = new Mock<ILogger<WatermarkViewModel>>();

        _testDocument = new PdfDocument
        {
            FilePath = Path.Combine(Path.GetTempPath(), "test-watermark.pdf"),
            PageCount = 10
        };

        // Create a simple 1x1 PNG image as sample preview
        _samplePreviewImage = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }; // PNG header
    }

    public void Dispose()
    {
        // Cleanup is minimal for mocked tests
    }

    [Fact]
    public void Initialize_DefaultConfiguration_SetsDefaults()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SelectedType.Should().Be(WatermarkType.Text);
        viewModel.PageRangeType.Should().Be(PageRangeType.All);
        viewModel.TextConfig.Should().NotBeNull();
        viewModel.TextConfig.FontFamily.Should().Be("Arial");
        viewModel.TextConfig.FontSize.Should().Be(72f);
        viewModel.TextConfig.Opacity.Should().Be(0.5f);
        viewModel.ImageConfig.Should().NotBeNull();
        viewModel.IsLoading.Should().BeFalse();
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyTextWatermark_ValidConfig_CallsService()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "CONFIDENTIAL";
        viewModel.PageRangeType = PageRangeType.All;

        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        _watermarkServiceMock.Verify(
            s => s.ApplyTextWatermarkAsync(
                _testDocument,
                It.Is<TextWatermarkConfig>(c => c.Text == "CONFIDENTIAL"),
                It.IsAny<WatermarkPageRange>()),
            Times.Once);

        viewModel.HasUnsavedChanges.Should().BeTrue();
        viewModel.DialogApplied.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyImageWatermark_ValidConfig_CallsService()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.SelectedType = WatermarkType.Image;
        viewModel.ImageConfig.ImagePath = "/path/to/logo.png";
        viewModel.ImageConfig.Scale = 1.5f;
        viewModel.PageRangeType = PageRangeType.CurrentPage;

        _watermarkServiceMock
            .Setup(s => s.ApplyImageWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<ImageWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        _watermarkServiceMock.Verify(
            s => s.ApplyImageWatermarkAsync(
                _testDocument,
                It.Is<ImageWatermarkConfig>(c => c.ImagePath == "/path/to/logo.png" && c.Scale == 1.5f),
                It.IsAny<WatermarkPageRange>()),
            Times.Once);

        viewModel.HasUnsavedChanges.Should().BeTrue();
        viewModel.DialogApplied.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyWatermark_ServiceFails_SetsErrorState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "DRAFT";

        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .ReturnsAsync(Result.Fail("Failed to apply watermark"));

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        viewModel.HasUnsavedChanges.Should().BeFalse();
        viewModel.DialogApplied.Should().BeFalse();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task GeneratePreview_ValidConfig_UpdatesPreviewImage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "CONFIDENTIAL";

        _watermarkServiceMock
            .Setup(s => s.GeneratePreviewAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                It.IsAny<TextWatermarkConfig>(),
                null))
            .ReturnsAsync(Result.Ok(_samplePreviewImage));

        // Act
        await viewModel.GeneratePreviewCommand.ExecuteAsync(null);

        // Assert
        viewModel.PreviewImage.Should().NotBeNull();
        viewModel.PreviewImage.Should().BeEquivalentTo(_samplePreviewImage);
        viewModel.HasPreview.Should().BeTrue();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task GeneratePreview_TextWatermark_PassesCorrectConfig()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 3);

        viewModel.SelectedType = WatermarkType.Text;
        viewModel.TextConfig.Text = "DRAFT";
        viewModel.TextConfig.FontSize = 96f;
        viewModel.TextConfig.Opacity = 0.3f;

        _watermarkServiceMock
            .Setup(s => s.GeneratePreviewAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                It.IsAny<TextWatermarkConfig>(),
                null))
            .ReturnsAsync(Result.Ok(_samplePreviewImage));

        // Act
        await viewModel.GeneratePreviewCommand.ExecuteAsync(null);

        // Assert
        _watermarkServiceMock.Verify(
            s => s.GeneratePreviewAsync(
                _testDocument,
                2, // 0-based index
                It.Is<TextWatermarkConfig>(c =>
                    c.Text == "DRAFT" &&
                    c.FontSize == 96f &&
                    c.Opacity == 0.3f),
                null),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePreview_ImageWatermark_PassesCorrectConfig()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 2);

        viewModel.SelectedType = WatermarkType.Image;
        viewModel.ImageConfig.ImagePath = "/path/to/logo.png";
        viewModel.ImageConfig.Scale = 0.5f;
        viewModel.ImageConfig.RotationDegrees = 45f;

        _watermarkServiceMock
            .Setup(s => s.GeneratePreviewAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                null,
                It.IsAny<ImageWatermarkConfig>()))
            .ReturnsAsync(Result.Ok(_samplePreviewImage));

        // Act
        await viewModel.GeneratePreviewCommand.ExecuteAsync(null);

        // Assert
        _watermarkServiceMock.Verify(
            s => s.GeneratePreviewAsync(
                _testDocument,
                1, // 0-based index
                null,
                It.Is<ImageWatermarkConfig>(c =>
                    c.ImagePath == "/path/to/logo.png" &&
                    c.Scale == 0.5f &&
                    c.RotationDegrees == 45f)),
            Times.Once);
    }

    [Fact]
    public async Task ApplyPreset_Confidential_ConfiguresCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetupPreviewGeneration();

        // Act
        await viewModel.ApplyPresetCommand.ExecuteAsync("CONFIDENTIAL");

        // Assert
        viewModel.TextConfig.Text.Should().Be("CONFIDENTIAL");
        viewModel.TextConfig.Color.Should().Be(Color.Red);
        viewModel.TextConfig.FontSize.Should().Be(72f);
        viewModel.TextConfig.RotationDegrees.Should().Be(45f);
        viewModel.SelectedType.Should().Be(WatermarkType.Text);
    }

    [Fact]
    public async Task ApplyPreset_Draft_ConfiguresCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetupPreviewGeneration();

        // Act
        await viewModel.ApplyPresetCommand.ExecuteAsync("DRAFT");

        // Assert
        viewModel.TextConfig.Text.Should().Be("DRAFT");
        viewModel.TextConfig.Color.Should().Be(Color.Gray);
        viewModel.TextConfig.FontSize.Should().Be(96f);
        viewModel.TextConfig.RotationDegrees.Should().Be(45f);
    }

    [Fact]
    public async Task ApplyPreset_Copy_ConfiguresCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetupPreviewGeneration();

        // Act
        await viewModel.ApplyPresetCommand.ExecuteAsync("COPY");

        // Assert
        viewModel.TextConfig.Text.Should().Be("COPY");
        viewModel.TextConfig.Color.Should().Be(Color.Blue);
        viewModel.TextConfig.FontSize.Should().Be(72f);
        viewModel.TextConfig.RotationDegrees.Should().Be(45f);
    }

    [Fact]
    public async Task ApplyPreset_Approved_ConfiguresCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        SetupPreviewGeneration();

        // Act
        await viewModel.ApplyPresetCommand.ExecuteAsync("APPROVED");

        // Assert
        viewModel.TextConfig.Text.Should().Be("APPROVED");
        viewModel.TextConfig.Color.Should().Be(Color.Green);
        viewModel.TextConfig.FontSize.Should().Be(72f);
        viewModel.TextConfig.RotationDegrees.Should().Be(0f);
        viewModel.TextConfig.Position.Should().Be(WatermarkPosition.Center);
    }

    [Fact]
    public async Task ApplyWatermark_AllPages_UsesAllPagesRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "TEST";
        viewModel.PageRangeType = PageRangeType.All;

        WatermarkPageRange? capturedRange = null;
        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .Callback<PdfDocument, TextWatermarkConfig, WatermarkPageRange>((_, _, range) => capturedRange = range)
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        capturedRange.Should().NotBeNull();
        capturedRange!.Type.Should().Be(PageRangeType.All);
    }

    [Fact]
    public async Task ApplyWatermark_CurrentPage_UsesCurrentPageRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 5);

        viewModel.TextConfig.Text = "TEST";
        viewModel.PageRangeType = PageRangeType.CurrentPage;

        WatermarkPageRange? capturedRange = null;
        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .Callback<PdfDocument, TextWatermarkConfig, WatermarkPageRange>((_, _, range) => capturedRange = range)
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        capturedRange.Should().NotBeNull();
        capturedRange!.Type.Should().Be(PageRangeType.CurrentPage);
        capturedRange.CurrentPage.Should().Be(5);
    }

    [Fact]
    public async Task ApplyWatermark_OddPages_UsesOddPagesRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "TEST";
        viewModel.PageRangeType = PageRangeType.OddPages;

        WatermarkPageRange? capturedRange = null;
        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .Callback<PdfDocument, TextWatermarkConfig, WatermarkPageRange>((_, _, range) => capturedRange = range)
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        capturedRange.Should().NotBeNull();
        capturedRange!.Type.Should().Be(PageRangeType.OddPages);
    }

    [Fact]
    public async Task ApplyWatermark_EvenPages_UsesEvenPagesRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "TEST";
        viewModel.PageRangeType = PageRangeType.EvenPages;

        WatermarkPageRange? capturedRange = null;
        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .Callback<PdfDocument, TextWatermarkConfig, WatermarkPageRange>((_, _, range) => capturedRange = range)
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        capturedRange.Should().NotBeNull();
        capturedRange!.Type.Should().Be(PageRangeType.EvenPages);
    }

    [Fact]
    public async Task ApplyWatermark_CustomRange_ParsesCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.TextConfig.Text = "TEST";
        viewModel.PageRangeType = PageRangeType.Custom;
        viewModel.CustomPageRange = "1-3, 5, 7-9";

        WatermarkPageRange? capturedRange = null;
        _watermarkServiceMock
            .Setup(s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()))
            .Callback<PdfDocument, TextWatermarkConfig, WatermarkPageRange>((_, _, range) => capturedRange = range)
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        capturedRange.Should().NotBeNull();
        capturedRange!.Type.Should().Be(PageRangeType.Custom);
        capturedRange.SpecificPages.Should().BeEquivalentTo(new[] { 1, 2, 3, 5, 7, 8, 9 });
    }

    [Fact]
    public void ValidatePageRange_InvalidFormat_SetsError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);
        viewModel.PageRangeType = PageRangeType.Custom;
        viewModel.CustomPageRange = "1-invalid";

        // Act
        var result = viewModel.ValidatePageRange();

        // Assert
        result.Should().BeFalse();
        viewModel.PageRangeError.Should().NotBeNullOrWhiteSpace();
        viewModel.HasPageRangeError.Should().BeTrue();
    }

    [Fact]
    public void ValidatePageRange_ValidFormat_ClearsError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);
        viewModel.PageRangeType = PageRangeType.Custom;
        viewModel.CustomPageRange = "1-5, 8";
        viewModel.PageRangeError = "Previous error";

        // Act
        var result = viewModel.ValidatePageRange();

        // Assert
        result.Should().BeTrue();
        viewModel.PageRangeError.Should().BeNullOrWhiteSpace();
        viewModel.HasPageRangeError.Should().BeFalse();
    }

    [Fact]
    public void SelectedType_Changed_UpdatesIsTextModeAndIsImageMode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedType = WatermarkType.Text;

        // Assert - Text mode
        viewModel.IsTextMode.Should().BeTrue();
        viewModel.IsImageMode.Should().BeFalse();

        // Act - Switch to image
        viewModel.SelectedType = WatermarkType.Image;

        // Assert - Image mode
        viewModel.IsTextMode.Should().BeFalse();
        viewModel.IsImageMode.Should().BeTrue();
    }

    [Fact]
    public void ImageConfig_PathSet_UpdatesHasImageSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ImageConfig.ImagePath = "";

        // Assert - No image
        viewModel.HasImageSelected.Should().BeFalse();

        // Act
        viewModel.ImageConfig.ImagePath = "/path/to/image.png";

        // Assert - Has image (HasImageSelected checks ImageConfig.ImagePath directly)
        viewModel.HasImageSelected.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyWatermark_NoDocument_DoesNotCallService()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // Don't initialize document

        viewModel.TextConfig.Text = "TEST";

        // Act
        await viewModel.ApplyCommand.ExecuteAsync(null);

        // Assert
        _watermarkServiceMock.Verify(
            s => s.ApplyTextWatermarkAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<WatermarkPageRange>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveWatermarks_ValidDocument_CallsService()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);

        viewModel.PageRangeType = PageRangeType.All;

        _watermarkServiceMock
            .Setup(s => s.RemoveWatermarksAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<WatermarkPageRange>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.RemoveCommand.ExecuteAsync(null);

        // Assert
        _watermarkServiceMock.Verify(
            s => s.RemoveWatermarksAsync(
                _testDocument,
                It.Is<WatermarkPageRange>(r => r.Type == PageRangeType.All)),
            Times.Once);

        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task SetDiagonal_TextWatermark_SetsRotationTo45()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);
        SetupPreviewGeneration();

        viewModel.SelectedType = WatermarkType.Text;
        viewModel.TextConfig.RotationDegrees = 0f;

        // Act
        await viewModel.SetDiagonalCommand.ExecuteAsync(null);

        // Assert
        viewModel.TextConfig.RotationDegrees.Should().Be(45f);
    }

    [Fact]
    public async Task SetDiagonal_ImageWatermark_SetsRotationTo45()
    {
        // Arrange
        var viewModel = CreateViewModel();
        InitializeViewModel(viewModel, 1);
        SetupPreviewGeneration();

        viewModel.SelectedType = WatermarkType.Image;
        viewModel.ImageConfig.RotationDegrees = 0f;

        // Act
        await viewModel.SetDiagonalCommand.ExecuteAsync(null);

        // Assert
        viewModel.ImageConfig.RotationDegrees.Should().Be(45f);
    }

    [Fact]
    public void Initialize_Command_SetsDocumentAndResetsState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DialogApplied = true;
        viewModel.PageRangeError = "Some error";

        // Act
        viewModel.InitializeCommand.Execute((_testDocument, 3, 10));

        // Assert
        viewModel.DialogApplied.Should().BeFalse();
        viewModel.PageRangeError.Should().BeNull();
    }

    private WatermarkViewModel CreateViewModel()
    {
        return new WatermarkViewModel(_watermarkServiceMock.Object, _loggerMock.Object);
    }

    private void InitializeViewModel(WatermarkViewModel viewModel, int pageNumber)
    {
        viewModel.InitializeCommand.Execute((_testDocument, pageNumber, _testDocument.PageCount));
        SetupPreviewGeneration();
    }

    private void SetupPreviewGeneration()
    {
        _watermarkServiceMock
            .Setup(s => s.GeneratePreviewAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                It.IsAny<TextWatermarkConfig>(),
                It.IsAny<ImageWatermarkConfig>()))
            .ReturnsAsync(Result.Ok(_samplePreviewImage));
    }
}
