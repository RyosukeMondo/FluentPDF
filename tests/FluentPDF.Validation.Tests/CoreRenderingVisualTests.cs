using FluentPDF.Rendering.Tests.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Validation.Tests;

/// <summary>
/// Visual regression tests for core PDF rendering functionality.
/// Tests basic rendering accuracy and consistency across different PDF features.
/// </summary>
[Trait("Category", "VisualRegression")]
public class CoreRenderingVisualTests : VisualRegressionTestBase
{
    private const string TestCategory = "CoreRendering";

    public CoreRenderingVisualTests()
        : base(
            new HeadlessRenderingService(CreateLogger<HeadlessRenderingService>()),
            new VisualComparisonService(CreateLogger<VisualComparisonService>()),
            new BaselineManager(CreateLogger<BaselineManager>()))
    {
    }

    [Fact]
    public async Task SimpleTextRendering_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "simple-text.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "SimpleTextRendering",
            pageNumber: 1,
            threshold: 0.95);
    }

    [Fact]
    public async Task ComplexLayoutRendering_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "complex-layout.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "ComplexLayoutRendering",
            pageNumber: 1,
            threshold: 0.95);
    }

    [Fact]
    public async Task ImagesAndGraphics_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "images-graphics.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "ImagesAndGraphics",
            pageNumber: 1,
            threshold: 0.93); // Slightly lower threshold for images due to compression artifacts
    }

    [Fact]
    public async Task MultiPageDocument_FirstPage_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "multi-page.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "MultiPageDocument_FirstPage",
            pageNumber: 1,
            threshold: 0.95);
    }

    [Fact]
    public async Task MultiPageDocument_SecondPage_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "multi-page.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "MultiPageDocument_SecondPage",
            pageNumber: 2,
            threshold: 0.95);
    }

    [Fact]
    public async Task FontRendering_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "various-fonts.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "FontRendering",
            pageNumber: 1,
            threshold: 0.95);
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
