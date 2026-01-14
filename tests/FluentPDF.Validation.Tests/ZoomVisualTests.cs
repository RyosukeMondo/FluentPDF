using FluentPDF.Rendering.Tests.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Validation.Tests;

/// <summary>
/// Visual regression tests for PDF zoom functionality.
/// Tests rendering accuracy at different DPI levels to ensure zoom quality.
/// </summary>
[Trait("Category", "VisualRegression")]
public class ZoomVisualTests : VisualRegressionTestBase
{
    private const string TestCategory = "Zoom";

    public ZoomVisualTests()
        : base(
            new HeadlessRenderingService(CreateLogger<HeadlessRenderingService>()),
            new VisualComparisonService(CreateLogger<VisualComparisonService>()),
            new BaselineManager(CreateLogger<BaselineManager>()))
    {
    }

    [Fact]
    public async Task Zoom100Percent_96DPI_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "simple-text.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "Zoom100Percent_96DPI",
            pageNumber: 1,
            threshold: 0.95,
            dpi: 96);
    }

    [Fact]
    public async Task Zoom150Percent_144DPI_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "simple-text.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "Zoom150Percent_144DPI",
            pageNumber: 1,
            threshold: 0.95,
            dpi: 144);
    }

    [Fact]
    public async Task Zoom200Percent_192DPI_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "simple-text.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "Zoom200Percent_192DPI",
            pageNumber: 1,
            threshold: 0.95,
            dpi: 192);
    }

    [Fact]
    public async Task Zoom300Percent_288DPI_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "simple-text.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "Zoom300Percent_288DPI",
            pageNumber: 1,
            threshold: 0.95,
            dpi: 288);
    }

    [Fact]
    public async Task ComplexLayout_HighDPI_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "complex-layout.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "ComplexLayout_HighDPI",
            pageNumber: 1,
            threshold: 0.93, // Slightly lower threshold for complex rendering at high DPI
            dpi: 192);
    }

    [Fact]
    public async Task ImagesAndGraphics_HighDPI_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "images-graphics.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "ImagesAndGraphics_HighDPI",
            pageNumber: 1,
            threshold: 0.90, // Lower threshold for high-DPI image rendering
            dpi: 192);
    }

    private static ILogger<T> CreateLogger<T>()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        return loggerFactory.CreateLogger<T>();
    }
}
