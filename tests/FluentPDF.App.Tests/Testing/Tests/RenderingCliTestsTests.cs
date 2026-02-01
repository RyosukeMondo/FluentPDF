using FluentAssertions;
using FluentPDF.App.Testing;
using FluentPDF.App.Testing.Tests;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;

namespace FluentPDF.App.Tests.Testing.Tests;

/// <summary>
/// Unit tests for rendering CLI test implementations.
/// Verifies PageRenderCliTest, ThumbnailAllPagesCliTest, TextExtractionCliTest,
/// FormFieldRenderCliTest, and BatchRenderCliTest.
/// </summary>
public sealed class RenderingCliTestsTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IPdfDocumentService> _mockDocumentService;
    private readonly Mock<IPdfRenderingService> _mockRenderingService;
    private readonly Mock<IThumbnailRenderingService> _mockThumbnailService;
    private readonly Mock<ITextExtractionService> _mockTextExtractionService;
    private readonly Mock<IPdfFormService> _mockFormService;

    public RenderingCliTestsTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        _mockLogger = new Mock<ILogger>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockDocumentService = new Mock<IPdfDocumentService>();
        _mockRenderingService = new Mock<IPdfRenderingService>();
        _mockThumbnailService = new Mock<IThumbnailRenderingService>();
        _mockTextExtractionService = new Mock<ITextExtractionService>();
        _mockFormService = new Mock<IPdfFormService>();

        // Setup service provider
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IPdfDocumentService)))
            .Returns(_mockDocumentService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IPdfRenderingService)))
            .Returns(_mockRenderingService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IThumbnailRenderingService)))
            .Returns(_mockThumbnailService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ITextExtractionService)))
            .Returns(_mockTextExtractionService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IPdfFormService)))
            .Returns(_mockFormService.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region PageRenderCliTest Tests

    [Fact]
    public void PageRenderCliTest_HasCorrectMetadata()
    {
        // Arrange & Act
        var test = new PageRenderCliTest();

        // Assert
        test.Name.Should().Be("page-render");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PageRenderCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new PageRenderCliTest();

        // Act & Assert
        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task PageRenderCliTest_RunAsync_WithMissingPdf_ReturnsFailure()
    {
        // Arrange
        var test = new PageRenderCliTest();
        var context = CreateTestContext();
        context.Data["TestPdfPath"] = Path.Combine(_tempDirectory, "nonexistent.pdf");

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task PageRenderCliTest_RunAsync_WithSuccessfulRender_ReturnsSuccess()
    {
        // Arrange
        var test = new PageRenderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageNumber"] = 1;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var imageStream = CreateTestImageStream(width: 800, height: 600);
        _mockRenderingService.Setup(s => s.RenderPageAsync(mockDocument, 1, 1.0, 96))
            .ReturnsAsync(Result.Ok(imageStream));

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("ImageWidth");
        result.Outputs.Should().ContainKey("ImageHeight");
        result.Outputs.Should().ContainKey("FileSizeKB");
        result.Outputs.Should().ContainKey("RenderTimeMs");
        result.Outputs["PageNumber"].Should().Be(1);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task PageRenderCliTest_RunAsync_WithInvalidPageNumber_ReturnsFailure()
    {
        // Arrange
        var test = new PageRenderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageNumber"] = 999; // Invalid page number

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid page number");
    }

    [Fact]
    public async Task PageRenderCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new PageRenderCliTest();

        // Act & Assert
        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task PageRenderCliTest_VerifyAsync_WithSuccessfulResult_ReturnsTrue()
    {
        // Arrange
        var test = new PageRenderCliTest();
        var outputFile = Path.Combine(_tempDirectory, "test_page.png");
        await File.WriteAllBytesAsync(outputFile, new byte[2048]); // Create a test file > 1KB

        var result = new CliTestResult
        {
            TestName = "page-render",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["ImageWidth"] = 800,
                ["ImageHeight"] = 600,
                ["FileSizeKB"] = 2.0,
                ["RenderTimeMs"] = 50.0
            }
        };

        // Act
        var verified = await test.VerifyAsync(result);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public async Task PageRenderCliTest_VerifyAsync_WithSmallFile_ReturnsFalse()
    {
        // Arrange
        var test = new PageRenderCliTest();
        var result = new CliTestResult
        {
            TestName = "page-render",
            Success = true,
            Outputs =
            {
                ["FileSizeKB"] = 0.5 // Too small, requirement is >1KB
            }
        };

        // Act
        var verified = await test.VerifyAsync(result);

        // Assert
        verified.Should().BeFalse();
    }

    #endregion

    #region ThumbnailAllPagesCliTest Tests

    [Fact]
    public void ThumbnailAllPagesCliTest_HasCorrectMetadata()
    {
        // Arrange & Act
        var test = new ThumbnailAllPagesCliTest();

        // Assert
        test.Name.Should().Be("thumbnail-all-pages");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ThumbnailAllPagesCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new ThumbnailAllPagesCliTest();

        // Act & Assert
        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task ThumbnailAllPagesCliTest_RunAsync_WithSuccessfulGeneration_ReturnsSuccess()
    {
        // Arrange
        var test = new ThumbnailAllPagesCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        // Setup thumbnail generation for each page
        for (int i = 1; i <= 3; i++)
        {
            var thumbnailStream = CreateTestImageStream(width: 200, height: 150);
            _mockThumbnailService.Setup(s => s.GenerateThumbnailAsync(mockDocument, i))
                .ReturnsAsync(Result.Ok(thumbnailStream));
        }

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("PageCount");
        result.Outputs.Should().ContainKey("SuccessCount");
        result.Outputs.Should().ContainKey("FailCount");
        result.Outputs.Should().ContainKey("ThumbnailPaths");
        result.Outputs["PageCount"].Should().Be(3);
        result.Outputs["SuccessCount"].Should().Be(3);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task ThumbnailAllPagesCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new ThumbnailAllPagesCliTest();

        // Act & Assert
        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    #endregion

    #region TextExtractionCliTest Tests

    [Fact]
    public void TextExtractionCliTest_HasCorrectMetadata()
    {
        // Arrange & Act
        var test = new TextExtractionCliTest();

        // Assert
        test.Name.Should().Be("text-extraction");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TextExtractionCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new TextExtractionCliTest();

        // Act & Assert
        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task TextExtractionCliTest_RunAsync_WithSuccessfulExtraction_ReturnsSuccess()
    {
        // Arrange
        var test = new TextExtractionCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("sample-with-text.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 1);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var extractedText = "This is sample text extracted from the PDF document.";
        _mockTextExtractionService.Setup(s => s.ExtractTextAsync(mockDocument, 1))
            .ReturnsAsync(Result.Ok(extractedText));

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("CharacterCount");
        result.Outputs.Should().ContainKey("PageCount");
        result.Outputs["CharacterCount"].Should().Be(extractedText.Length);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task TextExtractionCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new TextExtractionCliTest();

        // Act & Assert
        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task TextExtractionCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        // Arrange
        var test = new TextExtractionCliTest();
        var outputFile = Path.Combine(_tempDirectory, "extracted_text.txt");
        await File.WriteAllTextAsync(outputFile, "Sample extracted text");

        var result = new CliTestResult
        {
            TestName = "text-extraction",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["CharacterCount"] = 50,
                ["PageCount"] = 1
            }
        };

        // Act
        var verified = await test.VerifyAsync(result);

        // Assert
        verified.Should().BeTrue();
    }

    #endregion

    #region FormFieldRenderCliTest Tests

    [Fact]
    public void FormFieldRenderCliTest_HasCorrectMetadata()
    {
        // Arrange & Act
        var test = new FormFieldRenderCliTest();

        // Assert
        test.Name.Should().Be("form-field-render");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FormFieldRenderCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new FormFieldRenderCliTest();

        // Act & Assert
        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task FormFieldRenderCliTest_RunAsync_WithFormPdf_ReturnsSuccess()
    {
        // Arrange
        var test = new FormFieldRenderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("sample-form.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 1);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        _mockFormService.Setup(s => s.HasForms(mockDocument))
            .Returns(true);

        _mockFormService.Setup(s => s.GetFormFieldCount(mockDocument))
            .Returns(5);

        var imageStream = CreateTestImageStream(width: 800, height: 600);
        _mockRenderingService.Setup(s => s.RenderPageAsync(mockDocument, 1, 1.0, 96))
            .ReturnsAsync(Result.Ok(imageStream));

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("HasForms");
        result.Outputs.Should().ContainKey("FormFieldCount");
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs["HasForms"].Should().Be(true);
        result.Outputs["FormFieldCount"].Should().Be(5);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task FormFieldRenderCliTest_RunAsync_WithNonFormPdf_HandlesGracefully()
    {
        // Arrange
        var test = new FormFieldRenderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 1);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        _mockFormService.Setup(s => s.HasForms(mockDocument))
            .Returns(false);

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("HasForms");
        result.Outputs["HasForms"].Should().Be(false);
        result.Outputs["FormFieldCount"].Should().Be(0);
    }

    [Fact]
    public async Task FormFieldRenderCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new FormFieldRenderCliTest();

        // Act & Assert
        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    #endregion

    #region BatchRenderCliTest Tests

    [Fact]
    public void BatchRenderCliTest_HasCorrectMetadata()
    {
        // Arrange & Act
        var test = new BatchRenderCliTest();

        // Assert
        test.Name.Should().Be("batch-render");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BatchRenderCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new BatchRenderCliTest();

        // Act & Assert
        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task BatchRenderCliTest_RunAsync_WithSuccessfulBatch_ReturnsSuccess()
    {
        // Arrange
        var test = new BatchRenderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        // Setup rendering for each page
        for (int i = 1; i <= 3; i++)
        {
            var imageStream = CreateTestImageStream(width: 800, height: 600);
            _mockRenderingService.Setup(s => s.RenderPageAsync(mockDocument, i, 1.0, 96))
                .ReturnsAsync(Result.Ok(imageStream));
        }

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("PageCount");
        result.Outputs.Should().ContainKey("SuccessCount");
        result.Outputs.Should().ContainKey("FailCount");
        result.Outputs.Should().ContainKey("SuccessRate");
        result.Outputs.Should().ContainKey("TotalTimeMs");
        result.Outputs.Should().ContainKey("AverageTimeMs");
        result.Outputs["PageCount"].Should().Be(3);
        result.Outputs["SuccessCount"].Should().Be(3);
        result.Outputs["FailCount"].Should().Be(0);
        result.Outputs["SuccessRate"].Should().Be(100.0);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task BatchRenderCliTest_RunAsync_WithPartialFailure_ContinuesRendering()
    {
        // Arrange
        var test = new BatchRenderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        // Page 1 succeeds
        var imageStream1 = CreateTestImageStream(width: 800, height: 600);
        _mockRenderingService.Setup(s => s.RenderPageAsync(mockDocument, 1, 1.0, 96))
            .ReturnsAsync(Result.Ok(imageStream1));

        // Page 2 fails
        _mockRenderingService.Setup(s => s.RenderPageAsync(mockDocument, 2, 1.0, 96))
            .ReturnsAsync(Result.Fail("Rendering failed"));

        // Page 3 succeeds
        var imageStream3 = CreateTestImageStream(width: 800, height: 600);
        _mockRenderingService.Setup(s => s.RenderPageAsync(mockDocument, 3, 1.0, 96))
            .ReturnsAsync(Result.Ok(imageStream3));

        // Act
        var result = await test.RunAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Test succeeds if at least some pages rendered
        result.Outputs["PageCount"].Should().Be(3);
        result.Outputs["SuccessCount"].Should().Be(2);
        result.Outputs["FailCount"].Should().Be(1);
        result.Outputs["ExitCode"].Should().Be(1); // Exit code 1 indicates partial failure
        result.ErrorMessage.Should().Contain("failed to render");
    }

    [Fact]
    public async Task BatchRenderCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var test = new BatchRenderCliTest();

        // Act & Assert
        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task BatchRenderCliTest_VerifyAsync_WithFullSuccess_ReturnsTrue()
    {
        // Arrange
        var test = new BatchRenderCliTest();
        var renderedPaths = new List<string>
        {
            Path.Combine(_tempDirectory, "page_0001.png"),
            Path.Combine(_tempDirectory, "page_0002.png"),
            Path.Combine(_tempDirectory, "page_0003.png")
        };

        // Create test files
        foreach (var path in renderedPaths)
        {
            await File.WriteAllBytesAsync(path, new byte[2048]);
        }

        var result = new CliTestResult
        {
            TestName = "batch-render",
            Success = true,
            Outputs =
            {
                ["PageCount"] = 3,
                ["SuccessCount"] = 3,
                ["RenderedPaths"] = renderedPaths,
                ["SuccessRate"] = 100.0,
                ["TotalBytes"] = 6144L,
                ["TotalTimeMs"] = 150.0,
                ["AverageTimeMs"] = 50.0
            }
        };

        // Act
        var verified = await test.VerifyAsync(result);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public async Task BatchRenderCliTest_VerifyAsync_WithPartialSuccess_ReturnsFalse()
    {
        // Arrange
        var test = new BatchRenderCliTest();
        var result = new CliTestResult
        {
            TestName = "batch-render",
            Success = true,
            Outputs =
            {
                ["PageCount"] = 3,
                ["SuccessCount"] = 2, // Not all pages rendered successfully
                ["SuccessRate"] = 66.7
            }
        };

        // Act
        var verified = await test.VerifyAsync(result);

        // Assert
        verified.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private CliTestContext CreateTestContext()
    {
        return new CliTestContext(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _tempDirectory
        );
    }

    private PdfDocument CreateMockDocument(int pageCount)
    {
        return new PdfDocument
        {
            PageCount = pageCount,
            FilePath = "test.pdf",
            FileSizeBytes = 1024L,
            LoadedAt = DateTime.UtcNow,
            Handle = Mock.Of<IDisposable>()
        };
    }

    private string GetTestPdfPath(string filename)
    {
        var fixturesPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..",
            "Fixtures",
            filename
        );
        return Path.GetFullPath(fixturesPath);
    }

    private MemoryStream CreateTestImageStream(int width, int height)
    {
        // Create a simple 1x1 pixel PNG image in memory
        var ms = new MemoryStream();
        using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height))
        {
            image.SaveAsPng(ms);
        }
        ms.Position = 0;
        return ms;
    }

    #endregion
}
