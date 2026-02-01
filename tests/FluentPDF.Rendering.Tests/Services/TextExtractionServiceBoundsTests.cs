using System.Drawing;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for TextExtractionService bounds-based text extraction.
/// Tests extracting text within specific rectangular bounds on a PDF page.
/// </summary>
public sealed class TextExtractionServiceBoundsTests : IDisposable
{
    private readonly TextExtractionService _service;
    private readonly Mock<ILogger<TextExtractionService>> _mockLogger;
    private SafePdfDocumentHandle? _documentHandle;
    private const string TestPdfPath = "../../../../Fixtures/sample-with-text.pdf";

    public TextExtractionServiceBoundsTests()
    {
        _mockLogger = new Mock<ILogger<TextExtractionService>>();
        _service = new TextExtractionService(_mockLogger.Object);

        // Initialize PDFium
        if (!PdfiumInterop.IsInitialized)
        {
            PdfiumInterop.Initialize();
        }
    }

    [Fact]
    public async Task ExtractTextInBoundsAsync_WithValidBounds_ReturnsTextSelection()
    {
        // Arrange
        var document = await LoadTestDocumentAsync();
        var bounds = new RectangleF(100, 100, 200, 100); // Sample bounds

        // Act
        var result = await _service.ExtractTextInBoundsAsync(document, 1, bounds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Text);
        Assert.Equal(bounds, result.Value.SelectionBounds);
        Assert.Equal(0, result.Value.PageNumber); // 0-based page number
    }

    [Fact]
    public async Task ExtractTextInBoundsAsync_WithEmptyBounds_ReturnsEmptySelection()
    {
        // Arrange
        var document = await LoadTestDocumentAsync();
        var bounds = new RectangleF(0, 0, 10, 10); // Small bounds with likely no text

        // Act
        var result = await _service.ExtractTextInBoundsAsync(document, 1, bounds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Text);
        Assert.Empty(result.Value.CharacterBounds);
    }

    [Fact]
    public async Task ExtractTextInBoundsAsync_WithInvalidPage_ReturnsError()
    {
        // Arrange
        var document = await LoadTestDocumentAsync();
        var bounds = new RectangleF(100, 100, 200, 100);

        // Act
        var result = await _service.ExtractTextInBoundsAsync(document, 999, bounds);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("out of range", result.Errors[0].Message);
    }

    [Fact]
    public async Task ExtractTextInBoundsAsync_WithNullDocument_ThrowsException()
    {
        // Arrange
        var bounds = new RectangleF(100, 100, 200, 100);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ExtractTextInBoundsAsync(null!, 1, bounds));
    }

    [Fact]
    public async Task ExtractTextInBoundsAsync_ReturnsCharacterBounds()
    {
        // Arrange
        var document = await LoadTestDocumentAsync();
        var bounds = new RectangleF(50, 50, 400, 300); // Larger bounds to capture text

        // Act
        var result = await _service.ExtractTextInBoundsAsync(document, 1, bounds);

        // Assert
        Assert.True(result.IsSuccess);
        if (result.Value.HasText)
        {
            Assert.NotEmpty(result.Value.CharacterBounds);
            Assert.Equal(result.Value.Text.Length, result.Value.CharacterBounds.Count);

            // Verify all character bounds intersect with selection bounds
            foreach (var charBound in result.Value.CharacterBounds)
            {
                Assert.True(bounds.IntersectsWith(charBound));
            }
        }
    }

    [Fact]
    public async Task ExtractTextInBoundsAsync_Performance_CompletesWithin100Ms()
    {
        // Arrange
        var document = await LoadTestDocumentAsync();
        var bounds = new RectangleF(100, 100, 200, 100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _service.ExtractTextInBoundsAsync(document, 1, bounds);

        // Assert
        stopwatch.Stop();
        Assert.True(result.IsSuccess);
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"Extraction took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }

    private async Task<PdfDocument> LoadTestDocumentAsync()
    {
        var fullPath = Path.GetFullPath(TestPdfPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Test PDF not found. Expected at: {fullPath}");
        }

        _documentHandle = PdfiumInterop.LoadDocument(fullPath);
        if (_documentHandle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to load test PDF document");
        }

        var pageCount = PdfiumInterop.GetPageCount(_documentHandle);

        var fileInfo = new FileInfo(fullPath);

        return await Task.FromResult(new PdfDocument
        {
            FilePath = fullPath,
            PageCount = pageCount,
            Handle = _documentHandle,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = fileInfo.Length
        });
    }

    public void Dispose()
    {
        _documentHandle?.Dispose();
    }
}
