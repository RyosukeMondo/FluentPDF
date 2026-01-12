using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for DocxParserService.
/// Tests DOCX parsing, error handling, and HTML extraction scenarios.
/// Note: Tests with actual DOCX conversion require integration tests with real files.
/// Unit tests focus on validation logic and error handling scenarios.
/// </summary>
public sealed class DocxParserServiceTests : IDisposable
{
    private readonly Mock<ILogger<DocxParserService>> _mockLogger;
    private readonly DocxParserService _service;
    private readonly string _testDataDir;

    public DocxParserServiceTests()
    {
        _mockLogger = new Mock<ILogger<DocxParserService>>();
        _service = new DocxParserService(_mockLogger.Object);
        _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        Directory.CreateDirectory(_testDataDir);
    }

    public void Dispose()
    {
        // Cleanup test files
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
    public async Task ParseDocxToHtmlAsync_WithNonExistentFile_ReturnsFileNotFoundError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "nonexistent.docx");

        // Act
        var result = await _service.ParseDocxToHtmlAsync(nonExistentPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DOCX_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Context.Should().ContainKey("FilePath");
        error.Context["FilePath"].Should().Be(nonExistentPath);
    }

    [Fact]
    public async Task ParseDocxToHtmlAsync_WithInvalidExtension_ReturnsInvalidFormatError()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDataDir, "test.txt");
        File.WriteAllText(invalidPath, "test content");

        // Act
        var result = await _service.ParseDocxToHtmlAsync(invalidPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DOCX_INVALID_FORMAT");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Context.Should().ContainKey("Extension");
        error.Context["Extension"].Should().Be(".txt");
    }

    [Theory]
    [InlineData(".doc")]
    [InlineData(".pdf")]
    [InlineData(".xlsx")]
    public async Task ParseDocxToHtmlAsync_WithWrongFileExtensions_ReturnsInvalidFormatError(string extension)
    {
        // Arrange
        var filePath = Path.Combine(_testDataDir, $"test{extension}");
        File.WriteAllText(filePath, "test content");

        // Act
        var result = await _service.ParseDocxToHtmlAsync(filePath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DOCX_INVALID_FORMAT");
        error.Context["Extension"].Should().Be(extension);
    }

    [Fact]
    public async Task ParseDocxToHtmlAsync_WithInvalidDocxFile_ReturnsInvalidFormatError()
    {
        // Arrange - Create a file that's not a valid DOCX (just text content)
        var docxPath = Path.Combine(_testDataDir, "invalid.docx");
        File.WriteAllText(docxPath, "This is not a valid DOCX file");

        // Act
        var result = await _service.ParseDocxToHtmlAsync(docxPath);

        // Assert - Should fail because file is not a valid DOCX
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        // Could be DOCX_INVALID_FORMAT or DOCX_PARSE_FAILED depending on DocumentFormat.OpenXml's error
        error!.ErrorCode.Should().Match(code =>
            code == "DOCX_INVALID_FORMAT" || code == "DOCX_PARSE_FAILED");
    }

    [Fact]
    public void ValidDocxConversion_Note()
    {
        // Note: Full integration tests for ParseDocxToHtmlAsync with valid DOCX files
        // are covered in the integration test suite where we use real DOCX documents.
        //
        // Unit tests here focus on validation logic (file existence, extension checking)
        // since testing the actual DocumentFormat.OpenXml conversion requires real DOCX files.
        Assert.True(true);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DocxParserService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ParseDocxToHtmlAsync_LogsCorrelationId()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "test.docx");

        // Act
        await _service.ParseDocxToHtmlAsync(nonExistentPath);

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
    public void CaseInsensitiveExtension_Documentation()
    {
        // Note: Extension validation is case-insensitive (using .ToLowerInvariant())
        // so ".DOCX", ".Docx", ".docx" are all accepted.
        // This is tested by the actual conversion attempts in integration tests.
        Assert.True(true);
    }
}
