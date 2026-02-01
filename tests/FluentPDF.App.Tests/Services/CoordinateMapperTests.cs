using System.Drawing;
using FluentPDF.App.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.App.Tests.Services;

/// <summary>
/// Unit tests for CoordinateMapper service.
/// Tests conversion between screen coordinates and PDF page coordinates.
/// </summary>
public sealed class CoordinateMapperTests
{
    private readonly CoordinateMapper _mapper;
    private readonly Mock<ILogger<CoordinateMapper>> _mockLogger;

    public CoordinateMapperTests()
    {
        _mockLogger = new Mock<ILogger<CoordinateMapper>>();
        _mapper = new CoordinateMapper(_mockLogger.Object);
    }

    [Theory]
    [InlineData(0, 0, 1.0, 612, 792, 0, 792)] // Top-left at 100% zoom
    [InlineData(612, 792, 1.0, 612, 792, 612, 0)] // Bottom-right at 100% zoom
    [InlineData(0, 0, 2.0, 612, 792, 0, 792)] // Top-left at 200% zoom
    [InlineData(1224, 1584, 2.0, 612, 792, 612, 0)] // Bottom-right at 200% zoom
    public void ScreenToPdf_WithVariousInputs_ConvertsCorrectly(
        int screenX, int screenY,
        double zoom, double pageWidth, double pageHeight,
        float expectedPdfX, float expectedPdfY)
    {
        // Arrange
        var screenPoint = new Point(screenX, screenY);

        // Act
        var pdfPoint = _mapper.ScreenToPdf(
            screenPoint, 0, zoom, pageWidth, pageHeight);

        // Assert
        Assert.Equal(expectedPdfX, pdfPoint.X, 0.01f);
        Assert.Equal(expectedPdfY, pdfPoint.Y, 0.01f);
    }

    [Theory]
    [InlineData(0, 792, 1.0, 612, 792, 0, 0)] // Top-left in PDF -> top-left on screen
    [InlineData(612, 0, 1.0, 612, 792, 612, 792)] // Bottom-right in PDF -> bottom-right on screen
    [InlineData(306, 396, 1.0, 612, 792, 306, 396)] // Center point at 100% zoom
    [InlineData(306, 396, 2.0, 612, 792, 612, 792)] // Center point at 200% zoom
    public void PdfToScreen_WithVariousInputs_ConvertsCorrectly(
        float pdfX, float pdfY,
        double zoom, double pageWidth, double pageHeight,
        int expectedScreenX, int expectedScreenY)
    {
        // Arrange
        var pdfPoint = new PointF(pdfX, pdfY);

        // Act
        var screenPoint = _mapper.PdfToScreen(
            pdfPoint, 0, zoom, pageWidth, pageHeight);

        // Assert
        Assert.Equal(expectedScreenX, screenPoint.X);
        Assert.Equal(expectedScreenY, screenPoint.Y);
    }

    [Fact]
    public void ScreenToPdf_RoundTrip_ReturnsOriginalPoint()
    {
        // Arrange
        var originalScreen = new Point(200, 300);
        const double zoom = 1.5;
        const double pageWidth = 612;
        const double pageHeight = 792;

        // Act
        var pdfPoint = _mapper.ScreenToPdf(
            originalScreen, 0, zoom, pageWidth, pageHeight);
        var screenPoint = _mapper.PdfToScreen(
            pdfPoint, 0, zoom, pageWidth, pageHeight);

        // Assert - allow 1px tolerance due to rounding
        Assert.True(Math.Abs(originalScreen.X - screenPoint.X) <= 1);
        Assert.True(Math.Abs(originalScreen.Y - screenPoint.Y) <= 1);
    }

    [Fact]
    public void ScreenToPdf_WithZeroZoom_ThrowsException()
    {
        // Arrange
        var screenPoint = new Point(100, 100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _mapper.ScreenToPdf(screenPoint, 0, 0, 612, 792));
    }

    [Fact]
    public void ScreenToPdf_WithNegativeZoom_ThrowsException()
    {
        // Arrange
        var screenPoint = new Point(100, 100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _mapper.ScreenToPdf(screenPoint, 0, -1.0, 612, 792));
    }

    [Fact]
    public void ScreenToPdf_WithZeroPageWidth_ThrowsException()
    {
        // Arrange
        var screenPoint = new Point(100, 100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _mapper.ScreenToPdf(screenPoint, 0, 1.0, 0, 792));
    }

    [Fact]
    public void ScreenToPdf_WithZeroPageHeight_ThrowsException()
    {
        // Arrange
        var screenPoint = new Point(100, 100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _mapper.ScreenToPdf(screenPoint, 0, 1.0, 612, 0));
    }

    [Fact]
    public void PdfToScreen_WithZeroZoom_ThrowsException()
    {
        // Arrange
        var pdfPoint = new PointF(100, 100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _mapper.PdfToScreen(pdfPoint, 0, 0, 612, 792));
    }

    [Theory]
    [InlineData(0.5)] // 50% zoom
    [InlineData(1.0)] // 100% zoom
    [InlineData(1.5)] // 150% zoom
    [InlineData(2.0)] // 200% zoom
    [InlineData(4.0)] // 400% zoom
    public void ScreenToPdf_WithVariousZoomLevels_ScalesCorrectly(double zoom)
    {
        // Arrange
        var screenPoint = new Point(100, 100);
        const double pageWidth = 612;
        const double pageHeight = 792;

        // Act
        var pdfPoint = _mapper.ScreenToPdf(
            screenPoint, 0, zoom, pageWidth, pageHeight);

        // Assert - verify scaling relationship
        var expectedPdfX = screenPoint.X / zoom;
        var expectedPdfY = pageHeight - (screenPoint.Y / zoom);
        Assert.Equal(expectedPdfX, pdfPoint.X, 0.01f);
        Assert.Equal(expectedPdfY, pdfPoint.Y, 0.01f);
    }

    [Fact]
    public void ScreenToPdf_AccuracyWithin1Px_MeetsRequirement()
    {
        // Arrange - Test NFR2: Coordinate mapping accuracy within 1px
        var screenPoint = new Point(123, 456);
        const double zoom = 1.333; // Non-standard zoom
        const double pageWidth = 595; // A4 width
        const double pageHeight = 842; // A4 height

        // Act
        var pdfPoint = _mapper.ScreenToPdf(
            screenPoint, 0, zoom, pageWidth, pageHeight);
        var backToScreen = _mapper.PdfToScreen(
            pdfPoint, 0, zoom, pageWidth, pageHeight);

        // Assert - within 1px accuracy
        var xDiff = Math.Abs(screenPoint.X - backToScreen.X);
        var yDiff = Math.Abs(screenPoint.Y - backToScreen.Y);
        Assert.True(xDiff <= 1, $"X difference {xDiff}px exceeds 1px tolerance");
        Assert.True(yDiff <= 1, $"Y difference {yDiff}px exceeds 1px tolerance");
    }
}
