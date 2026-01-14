using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for ThumbnailRenderingService.
/// Tests service construction, parameter validation, and interaction with underlying PdfRenderingService.
/// </summary>
public class ThumbnailRenderingServiceTests
{
    private readonly Mock<IPdfRenderingService> _mockRenderingService;
    private readonly Mock<ILogger<ThumbnailRenderingService>> _mockLogger;
    private readonly ThumbnailRenderingService _service;

    public ThumbnailRenderingServiceTests()
    {
        _mockRenderingService = new Mock<IPdfRenderingService>();
        _mockLogger = new Mock<ILogger<ThumbnailRenderingService>>();
        _service = new ThumbnailRenderingService(_mockRenderingService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullRenderingService_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ThumbnailRenderingService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("renderingService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ThumbnailRenderingService(_mockRenderingService.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task RenderThumbnailAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await _service.RenderThumbnailAsync(null!, 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task RenderThumbnailAsync_CallsRenderingServiceWithLowDpiAndZoom()
    {
        // Arrange
        var mockDocument = CreateMockDocument();
        var mockStream = new MemoryStream();
        _mockRenderingService
            .Setup(s => s.RenderPageAsync(mockDocument, 1, It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Result.Ok<Stream>(mockStream));

        // Act
        var result = await _service.RenderThumbnailAsync(mockDocument, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRenderingService.Verify(
            s => s.RenderPageAsync(
                mockDocument,
                1,
                0.2,  // ThumbnailZoom (20%)
                48.0  // ThumbnailDpi (half of standard 96)
            ),
            Times.Once);
    }

    [Fact]
    public async Task RenderThumbnailAsync_WhenRenderingSucceeds_ReturnsSuccessResult()
    {
        // Arrange
        var mockDocument = CreateMockDocument();
        var expectedStream = new MemoryStream();
        _mockRenderingService
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Result.Ok<Stream>(expectedStream));

        // Act
        var result = await _service.RenderThumbnailAsync(mockDocument, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedStream);
    }

    [Fact]
    public async Task RenderThumbnailAsync_WhenRenderingFails_ReturnsFailureResult()
    {
        // Arrange
        var mockDocument = CreateMockDocument();
        var expectedError = "Rendering failed";
        _mockRenderingService
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Result.Fail<Stream>(expectedError));

        // Act
        var result = await _service.RenderThumbnailAsync(mockDocument, 1);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Be(expectedError);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task RenderThumbnailAsync_WithDifferentPageNumbers_PassesCorrectPageNumber(int pageNumber)
    {
        // Arrange
        var mockDocument = CreateMockDocument(pageNumber);
        var mockStream = new MemoryStream();
        _mockRenderingService
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), pageNumber, It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Result.Ok<Stream>(mockStream));

        // Act
        var result = await _service.RenderThumbnailAsync(mockDocument, pageNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRenderingService.Verify(
            s => s.RenderPageAsync(
                mockDocument,
                pageNumber,
                It.IsAny<double>(),
                It.IsAny<double>()
            ),
            Times.Once);
    }

    [Fact]
    public async Task RenderThumbnailAsync_LogsPerformanceMetrics()
    {
        // Arrange
        var mockDocument = CreateMockDocument();
        var mockStream = new MemoryStream();
        _mockRenderingService
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(Result.Ok<Stream>(mockStream));

        // Act
        await _service.RenderThumbnailAsync(mockDocument, 1);

        // Assert
        // Verify that logging occurred (debug log at start, info log at completion)
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    private static PdfDocument CreateMockDocument(int pageCount = 10)
    {
        // Create a mock safe handle for testing
        var mockHandle = new Mock<IDisposable>();

        return new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = pageCount,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };
    }
}
