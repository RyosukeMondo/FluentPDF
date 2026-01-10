using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for TextExtractionService.
/// Tests text extraction from single pages and all pages with caching and error handling.
/// </summary>
public sealed class TextExtractionServiceTests : IDisposable
{
    private readonly Mock<ILogger<TextExtractionService>> _mockLogger;
    private readonly TextExtractionService _service;

    public TextExtractionServiceTests()
    {
        _mockLogger = new Mock<ILogger<TextExtractionService>>();
        _service = new TextExtractionService(_mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup handled by test framework
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TextExtractionService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ExtractTextAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.ExtractTextAsync(null!, 1);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task ExtractTextAsync_WithInvalidPageNumber_ReturnsValidationError()
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
        var resultNegative = await _service.ExtractTextAsync(document, 0);

        // Assert
        resultNegative.IsFailed.Should().BeTrue();
        var error = resultNegative.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_PAGE_INVALID");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Context.Should().ContainKey("PageNumber");
        error.Context["PageNumber"].Should().Be(0);
        error.Context.Should().ContainKey("TotalPages");
        error.Context["TotalPages"].Should().Be(10);

        // Act - Test page number > PageCount
        var resultTooHigh = await _service.ExtractTextAsync(document, 11);

        // Assert
        resultTooHigh.IsFailed.Should().BeTrue();
        var errorHigh = resultTooHigh.Errors[0] as PdfError;
        errorHigh.Should().NotBeNull();
        errorHigh!.ErrorCode.Should().Be("PDF_PAGE_INVALID");
        errorHigh.Context["PageNumber"].Should().Be(11);
    }

    [Fact]
    public async Task ExtractAllTextAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.ExtractAllTextAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public async Task ExtractAllTextAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 100, // Large page count to ensure cancellation
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _service.ExtractAllTextAsync(document, cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_TEXT_EXTRACTION_CANCELLED");
        error.Category.Should().Be(ErrorCategory.System);
        error.Severity.Should().Be(ErrorSeverity.Info);
        error.Context.Should().ContainKey("CompletedPages");
        error.Context.Should().ContainKey("TotalPages");
    }

    [Fact]
    public async Task ExtractTextAsync_ValidatesPageNumberBounds()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 5,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act - Test minimum invalid
        var resultMin = await _service.ExtractTextAsync(document, 0);

        // Assert
        resultMin.IsFailed.Should().BeTrue();
        var errorMin = resultMin.Errors[0] as PdfError;
        errorMin.Should().NotBeNull();
        errorMin!.ErrorCode.Should().Be("PDF_PAGE_INVALID");

        // Act - Test maximum invalid
        var resultMax = await _service.ExtractTextAsync(document, 6);

        // Assert
        resultMax.IsFailed.Should().BeTrue();
        var errorMax = resultMax.Errors[0] as PdfError;
        errorMax.Should().NotBeNull();
        errorMax!.ErrorCode.Should().Be("PDF_PAGE_INVALID");
    }

    [Fact]
    public async Task ExtractAllTextAsync_WithDocumentHavingNoPages_ReturnsEmptyDictionary()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "empty.pdf",
            PageCount = 0,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 100
        };

        // Act
        var result = await _service.ExtractAllTextAsync(document);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
