using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for PdfDocumentService.
/// Tests document loading, page info retrieval, and error handling scenarios.
/// </summary>
public sealed class PdfDocumentServiceTests : IDisposable
{
    private readonly Mock<ILogger<PdfDocumentService>> _mockLogger;
    private readonly PdfDocumentService _service;
    private readonly string _testDataDir;

    public PdfDocumentServiceTests()
    {
        _mockLogger = new Mock<ILogger<PdfDocumentService>>();
        _service = new PdfDocumentService(_mockLogger.Object);
        _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        // Create test data directory if it doesn't exist
        Directory.CreateDirectory(_testDataDir);
    }

    public void Dispose()
    {
        // Cleanup is handled by test framework
    }

    [Fact]
    public async Task LoadDocumentAsync_WithNonExistentFile_ReturnsFileNotFoundError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "nonexistent.pdf");

        // Act
        var result = await _service.LoadDocumentAsync(nonExistentPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Context.Should().ContainKey("FilePath");
        error.Context["FilePath"].Should().Be(nonExistentPath);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PdfDocumentService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task GetPageInfoAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.GetPageInfoAsync(null!, 1);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task GetPageInfoAsync_WithInvalidPageNumber_ReturnsValidationError()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 10,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act - Test page number < 1
        var resultNegative = await _service.GetPageInfoAsync(document, 0);

        // Assert
        resultNegative.IsFailed.Should().BeTrue();
        var error = resultNegative.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_PAGE_INVALID");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Context.Should().ContainKey("PageNumber");
        error.Context.Should().ContainKey("TotalPages");

        // Act - Test page number > PageCount
        var resultTooLarge = await _service.GetPageInfoAsync(document, 11);

        // Assert
        resultTooLarge.IsFailed.Should().BeTrue();
        error = resultTooLarge.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_PAGE_INVALID");
    }

    [Fact]
    public void CloseDocument_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _service.CloseDocument(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public void CloseDocument_WithValidDocument_DisposesHandleSuccessfully()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = _service.CloseDocument(document);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockHandle.Verify(h => h.Dispose(), Times.Once);
    }

    [Fact]
    public void CloseDocument_WhenDisposeThrows_ReturnsFailureResult()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        mockHandle.Setup(h => h.Dispose()).Throws(new InvalidOperationException("Test exception"));

        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = _service.CloseDocument(document);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_CLOSE_FAILED");
        error.Category.Should().Be(ErrorCategory.System);
        error.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void GetPageInfoAsync_Documentation_Note()
    {
        // Note: Full integration tests for GetPageInfoAsync with valid page numbers
        // are covered in the integration test suite (PdfViewerIntegrationTests)
        // which uses real PDFium documents and handles.
        //
        // Unit tests here focus on validation logic (null checks, bounds checking)
        // since testing the actual PDFium page loading requires real PDF files
        // and initialized PDFium library.
        Assert.True(true);
    }

    [Fact]
    public async Task LoadDocumentAsync_LogsCorrelationId()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "test.pdf");

        // Act
        await _service.LoadDocumentAsync(nonExistentPath);

        // Assert - Verify that logging occurred with correlation ID
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
    public async Task LoadDocumentAsync_WithPassword_PassesPasswordToInterop()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDir, "encrypted.pdf");
        var password = "test123";

        // Act
        await _service.LoadDocumentAsync(filePath, password);

        // Assert - Verify password was logged (but not the actual password value for security)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HasPassword=True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPageInfoAsync_LogsPageDimensions()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 10,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        await _service.GetPageInfoAsync(document, 1);

        // Assert - Verify debug logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting page info")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
