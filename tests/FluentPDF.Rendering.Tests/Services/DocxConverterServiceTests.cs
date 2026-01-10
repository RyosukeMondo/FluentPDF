using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for DocxConverterService.
/// Tests conversion orchestration, error handling, validation, and timeout scenarios.
/// Uses mocked dependencies to isolate the orchestration logic.
/// </summary>
public sealed class DocxConverterServiceTests : IDisposable
{
    private readonly Mock<ILogger<DocxConverterService>> _mockLogger;
    private readonly Mock<IDocxParserService> _mockDocxParser;
    private readonly Mock<IHtmlToPdfService> _mockHtmlToPdf;
    private readonly DocxConverterService _service;
    private readonly string _testDataDir;

    public DocxConverterServiceTests()
    {
        _mockLogger = new Mock<ILogger<DocxConverterService>>();
        _mockDocxParser = new Mock<IDocxParserService>();
        _mockHtmlToPdf = new Mock<IHtmlToPdfService>();
        _service = new DocxConverterService(
            _mockLogger.Object,
            _mockDocxParser.Object,
            _mockHtmlToPdf.Object);

        _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Converter");
        Directory.CreateDirectory(_testDataDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDir))
        {
            try
            {
                Directory.Delete(_testDataDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DocxConverterService(null!, _mockDocxParser.Object, _mockHtmlToPdf.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullDocxParser_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DocxConverterService(_mockLogger.Object, null!, _mockHtmlToPdf.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("docxParser");
    }

    [Fact]
    public void Constructor_WithNullHtmlToPdf_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DocxConverterService(_mockLogger.Object, _mockDocxParser.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("htmlToPdf");
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithNonExistentFile_ReturnsFileNotFoundError()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "nonexistent.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DOCX_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Context.Should().ContainKey("InputPath");
        error.Context["InputPath"].Should().Be(inputPath);

        // Verify services were not called
        _mockDocxParser.VerifyNoOtherCalls();
        _mockHtmlToPdf.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    [InlineData(".xlsx")]
    public async Task ConvertDocxToPdfAsync_WithInvalidExtension_ReturnsInvalidFormatError(string extension)
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, $"file{extension}");
        File.WriteAllText(inputPath, "test content");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DOCX_INVALID_FORMAT");
        error.Context.Should().ContainKey("Extension");
        error.Context["Extension"].Should().Be(extension);

        // Verify services were not called
        _mockDocxParser.VerifyNoOtherCalls();
        _mockHtmlToPdf.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConvertDocxToPdfAsync_WithInvalidOutputPath_ReturnsInvalidError(string? outputPath)
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        File.WriteAllText(inputPath, "test content");

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath!);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("OUTPUT_PATH_INVALID");
        error.Category.Should().Be(ErrorCategory.Validation);

        // Verify services were not called
        _mockDocxParser.VerifyNoOtherCalls();
        _mockHtmlToPdf.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithSuccessfulConversion_ReturnsConversionResult()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test docx content");

        var htmlContent = "<html><body>Test content</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(outputPath))
            .Callback(() => File.WriteAllText(outputPath, "fake pdf content"));

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var conversionResult = result.Value;
        conversionResult.Should().NotBeNull();
        conversionResult.OutputPath.Should().Be(outputPath);
        conversionResult.SourcePath.Should().Be(inputPath);
        conversionResult.ConversionTime.Should().BeGreaterThan(TimeSpan.Zero);
        conversionResult.SourceSizeBytes.Should().BeGreaterThan(0);
        conversionResult.OutputSizeBytes.Should().BeGreaterThan(0);
        conversionResult.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify services were called in order
        _mockDocxParser.Verify(x => x.ParseDocxToHtmlAsync(inputPath), Times.Once);
        _mockHtmlToPdf.Verify(x => x.ConvertHtmlToPdfAsync(
            htmlContent,
            outputPath,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithParsingFailure_ReturnsParseError()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var parseError = new PdfError(
            "DOCX_PARSE_FAILED",
            "Failed to parse DOCX",
            ErrorCategory.Conversion,
            ErrorSeverity.Error);

        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Fail(parseError));

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DOCX_PARSE_FAILED");

        // Verify HTML to PDF was never called
        _mockDocxParser.Verify(x => x.ParseDocxToHtmlAsync(inputPath), Times.Once);
        _mockHtmlToPdf.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithRenderingFailure_ReturnsRenderError()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        var renderError = new PdfError(
            "HTML_TO_PDF_FAILED",
            "Failed to render PDF",
            ErrorCategory.Conversion,
            ErrorSeverity.Error);

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(renderError));

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("HTML_TO_PDF_FAILED");

        // Verify both services were called
        _mockDocxParser.Verify(x => x.ParseDocxToHtmlAsync(inputPath), Times.Once);
        _mockHtmlToPdf.Verify(x => x.ConvertHtmlToPdfAsync(
            htmlContent,
            outputPath,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithCustomOptions_UsesSpecifiedTimeout()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(outputPath))
            .Callback(() => File.WriteAllText(outputPath, "fake pdf"));

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath, options);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify services were called
        _mockDocxParser.Verify(x => x.ParseDocxToHtmlAsync(inputPath), Times.Once);
        _mockHtmlToPdf.Verify(x => x.ConvertHtmlToPdfAsync(
            htmlContent,
            outputPath,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithNullOptions_UsesDefaults()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(outputPath))
            .Callback(() => File.WriteAllText(outputPath, "fake pdf"));

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockDocxParser.Verify(x => x.ParseDocxToHtmlAsync(inputPath), Times.Once);
        _mockHtmlToPdf.Verify(x => x.ConvertHtmlToPdfAsync(
            htmlContent,
            outputPath,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath, null, cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("CONVERSION_CANCELLED");
        error.Category.Should().Be(ErrorCategory.Conversion);
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(1) // Very short timeout
        };

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath, options);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("CONVERSION_TIMEOUT");
        error.Category.Should().Be(ErrorCategory.Conversion);
        error.Context.Should().ContainKey("Timeout");
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_CreatesOutputDirectory_IfNotExists()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputDir = Path.Combine(_testDataDir, "new", "nested", "dir");
        var outputPath = Path.Combine(outputDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(outputPath))
            .Callback(() => File.WriteAllText(outputPath, "fake pdf"));

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(outputDir).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_LogsCorrelationId()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(outputPath))
            .Callback(() => File.WriteAllText(outputPath, "fake pdf"));

        // Act
        await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ConvertDocxToPdfAsync_WithOutputFileSizeError_ContinuesSuccessfully()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.docx");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(inputPath, "test content");

        var htmlContent = "<html><body>Test</body></html>";
        _mockDocxParser.Setup(x => x.ParseDocxToHtmlAsync(inputPath))
            .ReturnsAsync(Result.Ok(htmlContent));

        _mockHtmlToPdf.Setup(x => x.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(outputPath));
        // Don't create the output file to trigger error reading size

        // Act
        var result = await _service.ConvertDocxToPdfAsync(inputPath, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OutputSizeBytes.Should().Be(0); // Should default to 0 on error
    }
}
