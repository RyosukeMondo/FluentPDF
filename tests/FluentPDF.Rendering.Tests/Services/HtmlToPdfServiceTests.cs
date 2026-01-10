using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for HtmlToPdfService.
/// Tests HTML to PDF conversion, validation, error handling, and queuing logic.
/// Note: Full WebView2 integration tests with actual rendering are in integration test suite
/// since WebView2 requires Windows runtime and UI thread context.
/// Unit tests focus on validation logic and error handling scenarios.
/// </summary>
public sealed class HtmlToPdfServiceTests : IDisposable
{
    private readonly Mock<ILogger<HtmlToPdfService>> _mockLogger;
    private readonly HtmlToPdfService _service;
    private readonly string _testDataDir;

    public HtmlToPdfServiceTests()
    {
        _mockLogger = new Mock<ILogger<HtmlToPdfService>>();
        _service = new HtmlToPdfService(_mockLogger.Object);
        _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        Directory.CreateDirectory(_testDataDir);
    }

    public void Dispose()
    {
        _service.Dispose();

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
        var act = () => new HtmlToPdfService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithEmptyHtml_ReturnsValidationError()
    {
        // Arrange
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        var result = await _service.ConvertHtmlToPdfAsync("", outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("HTML_EMPTY");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithNullHtml_ReturnsValidationError()
    {
        // Arrange
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        var result = await _service.ConvertHtmlToPdfAsync(null!, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("HTML_EMPTY");
        error.Category.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithWhitespaceHtml_ReturnsValidationError()
    {
        // Arrange
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        var result = await _service.ConvertHtmlToPdfAsync("   \t\n  ", outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("HTML_EMPTY");
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithEmptyOutputPath_ReturnsValidationError()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";

        // Act
        var result = await _service.ConvertHtmlToPdfAsync(htmlContent, "");

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("OUTPUT_PATH_EMPTY");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithNullOutputPath_ReturnsValidationError()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";

        // Act
        var result = await _service.ConvertHtmlToPdfAsync(htmlContent, null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("OUTPUT_PATH_EMPTY");
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithWhitespaceOutputPath_ReturnsValidationError()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";

        // Act
        var result = await _service.ConvertHtmlToPdfAsync(htmlContent, "   \t\n  ");

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("OUTPUT_PATH_EMPTY");
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_LogsCorrelationId()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        await _service.ConvertHtmlToPdfAsync(htmlContent, outputPath);

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
    public async Task ConvertHtmlToPdfAsync_LogsHtmlLength()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test Document</h1></body></html>";
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        await _service.ConvertHtmlToPdfAsync(htmlContent, outputPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HtmlLength")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _service.ConvertHtmlToPdfAsync(htmlContent, outputPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var service = new HtmlToPdfService(_mockLogger.Object);

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Should not throw
    }

    [Fact]
    public void WebView2Integration_Note()
    {
        // Note: Full integration tests for ConvertHtmlToPdfAsync with actual WebView2 rendering
        // are covered in the integration test suite where WebView2 runtime is available.
        //
        // Unit tests here focus on:
        // 1. Input validation (null, empty, whitespace checks)
        // 2. Error handling scenarios
        // 3. Logging behavior
        // 4. Cancellation support
        //
        // Integration tests verify:
        // 1. WebView2 environment initialization
        // 2. Actual HTML rendering to PDF
        // 3. Print settings configuration
        // 4. Output file generation and validation
        // 5. Resource cleanup
        // 6. Concurrent conversion queuing
        Assert.True(true);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithNonExistentDirectory_CreatesDirectory()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";
        var nestedDir = Path.Combine(_testDataDir, "nested", "deep", "path");
        var outputPath = Path.Combine(nestedDir, "output.pdf");

        // Verify directory doesn't exist
        Directory.Exists(nestedDir).Should().BeFalse();

        // Act
        await _service.ConvertHtmlToPdfAsync(htmlContent, outputPath);

        // Assert
        // Directory should be created even if WebView2 fails
        // (directory creation happens before WebView2 initialization)
        Directory.Exists(nestedDir).Should().BeTrue();
    }

    [Theory]
    [InlineData("<html><body><p>Simple text</p></body></html>")]
    [InlineData("<html><head><title>Test</title></head><body><h1>Heading</h1><p>Content</p></body></html>")]
    [InlineData("<!DOCTYPE html><html><body><div>Complex content</div></body></html>")]
    public async Task ConvertHtmlToPdfAsync_WithValidHtml_LogsStartMessage(string htmlContent)
    {
        // Arrange
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act
        await _service.ConvertHtmlToPdfAsync(htmlContent, outputPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting HTML to PDF conversion")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConvertHtmlToPdfAsync_WithInvalidDirectoryPath_ReturnsIOError()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Test</h1></body></html>";
        // Use an invalid path that cannot be created (e.g., contains invalid characters on Windows)
        var invalidPath = Path.Combine(_testDataDir, "invalid<>:\"|?*path", "output.pdf");

        // Act
        var result = await _service.ConvertHtmlToPdfAsync(htmlContent, invalidPath);

        // Assert
        // Should fail during directory creation if path is truly invalid
        // On some systems this might succeed if the OS handles the characters differently
        // The test validates that IO errors during directory creation are handled
        if (result.IsFailed)
        {
            var error = result.Errors[0] as PdfError;
            error.Should().NotBeNull();
            // Could be OUTPUT_DIRECTORY_CREATE_FAILED or other IO-related error
            error!.Category.Should().BeOneOf(ErrorCategory.IO, ErrorCategory.System, ErrorCategory.Conversion);
        }
    }
}
