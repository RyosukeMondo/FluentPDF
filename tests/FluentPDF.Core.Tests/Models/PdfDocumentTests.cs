using FluentAssertions;
using FluentPDF.Core.Models;
using Moq;
using Xunit;

namespace FluentPDF.Core.Tests.Models;

/// <summary>
/// Unit tests for PdfDocument and PdfPage models.
/// </summary>
public sealed class PdfDocumentTests
{
    [Fact]
    public void PdfDocument_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var filePath = "/path/to/test.pdf";
        var pageCount = 10;
        var handle = Mock.Of<IDisposable>();
        var loadedAt = DateTime.UtcNow;
        var fileSize = 1024L;

        // Act
        var document = new PdfDocument
        {
            FilePath = filePath,
            PageCount = pageCount,
            Handle = handle,
            LoadedAt = loadedAt,
            FileSizeBytes = fileSize
        };

        // Assert
        document.FilePath.Should().Be(filePath);
        document.PageCount.Should().Be(pageCount);
        document.Handle.Should().Be(handle);
        document.LoadedAt.Should().Be(loadedAt);
        document.FileSizeBytes.Should().Be(fileSize);
    }

    [Fact]
    public void PdfDocument_Dispose_CleansUpHandle()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "/path/to/test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024L
        };

        // Act
        document.Dispose();

        // Assert
        mockHandle.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact]
    public void PdfPage_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var pageNumber = 1;
        var width = 612.0;
        var height = 792.0;

        // Act
        var page = new PdfPage
        {
            PageNumber = pageNumber,
            Width = width,
            Height = height
        };

        // Assert
        page.PageNumber.Should().Be(pageNumber);
        page.Width.Should().Be(width);
        page.Height.Should().Be(height);
    }

    [Fact]
    public void PdfPage_CalculatesAspectRatio_Correctly()
    {
        // Arrange & Act
        var page = new PdfPage
        {
            PageNumber = 1,
            Width = 800.0,
            Height = 600.0
        };

        // Assert
        page.AspectRatio.Should().BeApproximately(800.0 / 600.0, 0.001);
    }

    [Fact]
    public void PdfPage_AspectRatio_ForPortraitPage()
    {
        // Arrange & Act
        var page = new PdfPage
        {
            PageNumber = 1,
            Width = 612.0,  // US Letter width
            Height = 792.0  // US Letter height
        };

        // Assert
        page.AspectRatio.Should().BeApproximately(0.7727, 0.001);
    }

    [Fact]
    public void PdfPage_AspectRatio_ForLandscapePage()
    {
        // Arrange & Act
        var page = new PdfPage
        {
            PageNumber = 1,
            Width = 792.0,  // US Letter height (landscape)
            Height = 612.0  // US Letter width (landscape)
        };

        // Assert
        page.AspectRatio.Should().BeApproximately(1.294, 0.001);
    }

    [Fact]
    public void PdfPage_ThrowsException_WhenPageNumberIsZero()
    {
        // Arrange & Act
        Action act = () => _ = new PdfPage
        {
            PageNumber = 0,
            Width = 612.0,
            Height = 792.0
        };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("PageNumber")
            .WithMessage("*must be greater than 0*");
    }

    [Fact]
    public void PdfPage_ThrowsException_WhenPageNumberIsNegative()
    {
        // Arrange & Act
        Action act = () => _ = new PdfPage
        {
            PageNumber = -1,
            Width = 612.0,
            Height = 792.0
        };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("PageNumber")
            .WithMessage("*must be greater than 0*");
    }

    [Fact]
    public void PdfDocument_IsDisposable()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "/path/to/test.pdf",
            PageCount = 1,
            Handle = Mock.Of<IDisposable>(),
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024L
        };

        // Act & Assert
        document.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void PdfDocument_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "/path/to/test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024L
        };

        // Act
        Action act = () =>
        {
            document.Dispose();
            document.Dispose();
        };

        // Assert
        act.Should().NotThrow();
        mockHandle.Verify(h => h.Dispose(), Times.Exactly(2));
    }
}
