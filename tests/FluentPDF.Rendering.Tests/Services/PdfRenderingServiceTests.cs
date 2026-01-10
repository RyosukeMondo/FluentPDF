using FluentAssertions;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for PdfRenderingService.
/// Note: Most rendering logic requires integration tests with real PDFium since
/// SafePdfDocumentHandle cannot be mocked (it's a sealed SafeHandle class).
/// See PdfViewerIntegrationTests for comprehensive rendering tests.
/// </summary>
public class PdfRenderingServiceTests
{
    private readonly Mock<ILogger<PdfRenderingService>> _mockLogger;
    private readonly PdfRenderingService _service;

    public PdfRenderingServiceTests()
    {
        _mockLogger = new Mock<ILogger<PdfRenderingService>>();
        _service = new PdfRenderingService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new PdfRenderingService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task RenderPageAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await _service.RenderPageAsync(null!, 1, 1.0);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    // NOTE: Additional tests for rendering logic, page validation, zoom levels, etc.
    // require real PDFium handles and are covered in integration tests.
    // See tests/FluentPDF.Rendering.Tests/Integration/PdfViewerIntegrationTests.cs
}
