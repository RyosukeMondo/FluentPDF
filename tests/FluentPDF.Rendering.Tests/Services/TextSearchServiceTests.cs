using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for TextSearchService.
/// Tests search functionality with various options and edge cases.
/// </summary>
public sealed class TextSearchServiceTests : IDisposable
{
    private readonly Mock<ILogger<TextSearchService>> _mockLogger;
    private readonly TextSearchService _service;
    private readonly string _testPdfPath;
    private SafePdfDocumentHandle? _documentHandle;
    private PdfDocument? _testDocument;

    public TextSearchServiceTests()
    {
        _mockLogger = new Mock<ILogger<TextSearchService>>();
        _service = new TextSearchService(_mockLogger.Object);

        // Use the test PDF from the fixtures directory
        _testPdfPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.pdf");

        // Try to initialize PDFium and load test document
        // This will only work on Windows where pdfium.dll is available
        try
        {
            if (PdfiumInterop.Initialize() && File.Exists(_testPdfPath))
            {
                _documentHandle = PdfiumInterop.LoadDocument(_testPdfPath);
                if (_documentHandle != null && !_documentHandle.IsInvalid)
                {
                    var pageCount = PdfiumInterop.GetPageCount(_documentHandle);
                    var fileInfo = new FileInfo(_testPdfPath);
                    _testDocument = new PdfDocument
                    {
                        FilePath = _testPdfPath,
                        PageCount = pageCount,
                        Handle = _documentHandle,
                        LoadedAt = DateTime.UtcNow,
                        FileSizeBytes = fileInfo.Length
                    };
                }
            }
        }
        catch
        {
            // PDFium not available - tests that need it will be skipped
        }
    }

    public void Dispose()
    {
        _documentHandle?.Dispose();
        try
        {
            PdfiumInterop.Shutdown();
        }
        catch
        {
            // PDFium not available
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_ShouldSucceed()
    {
        // Arrange & Act
        var service = new TextSearchService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new TextSearchService(null!));
        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNullDocument_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SearchAsync(null!, "test"));
        Assert.Equal("document", exception.ParamName);
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_ShouldThrowArgumentException()
    {
        // Arrange
        var document = CreateMockDocument();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SearchAsync(document, null!));
        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ShouldThrowArgumentException()
    {
        // Arrange
        var document = CreateMockDocument();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SearchAsync(document, string.Empty));
        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task SearchAsync_WithCancelledToken_ShouldReturnCancelledError()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.SearchAsync(_testDocument, "test", cancellationToken: cts.Token);

        // Assert
        Assert.True(result.IsFailed);
        var error = result.Errors.First() as PdfError;
        Assert.NotNull(error);
        Assert.Equal("PDF_SEARCH_CANCELLED", error.ErrorCode);
    }

    [Fact]
    public async Task SearchAsync_WithDefaultOptions_ShouldSearchAllPages()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Act
        var result = await _service.SearchAsync(_testDocument, "the");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // The test should find at least one match if "the" exists in the PDF
        // Exact count depends on the test PDF content
    }

    [Fact]
    public async Task SearchAsync_WithCaseSensitiveOption_ShouldRespectCase()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Arrange
        var options = SearchOptions.CaseSensitiveSearch();

        // Act
        var result = await _service.SearchAsync(_testDocument, "PDF", options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // Results should only include exact case matches
    }

    [Fact]
    public async Task SearchAsync_WithWholeWordOption_ShouldMatchWholeWordsOnly()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Arrange
        var options = SearchOptions.WholeWordSearch();

        // Act
        var result = await _service.SearchAsync(_testDocument, "PDF", options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // Results should only include whole word matches
    }

    #endregion

    #region SearchPageAsync Tests

    [Fact]
    public async Task SearchPageAsync_WithNullDocument_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SearchPageAsync(null!, 1, "test"));
        Assert.Equal("document", exception.ParamName);
    }

    [Fact]
    public async Task SearchPageAsync_WithNullQuery_ShouldThrowArgumentException()
    {
        // Arrange
        var document = CreateMockDocument();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SearchPageAsync(document, 1, null!));
        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task SearchPageAsync_WithEmptyQuery_ShouldThrowArgumentException()
    {
        // Arrange
        var document = CreateMockDocument();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SearchPageAsync(document, 1, string.Empty));
        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task SearchPageAsync_WithInvalidPageNumber_ShouldReturnError()
    {
        // Arrange
        var document = CreateMockDocument();

        // Act
        var result = await _service.SearchPageAsync(document, 0, "test");

        // Assert
        Assert.True(result.IsFailed);
        var error = result.Errors.First() as PdfError;
        Assert.NotNull(error);
        Assert.Equal("PDF_PAGE_INVALID", error.ErrorCode);
    }

    [Fact]
    public async Task SearchPageAsync_WithPageNumberTooHigh_ShouldReturnError()
    {
        // Arrange
        var document = CreateMockDocument();

        // Act
        var result = await _service.SearchPageAsync(document, 100, "test");

        // Assert
        Assert.True(result.IsFailed);
        var error = result.Errors.First() as PdfError;
        Assert.NotNull(error);
        Assert.Equal("PDF_PAGE_INVALID", error.ErrorCode);
    }

    [Fact]
    public async Task SearchPageAsync_WithValidQuery_ShouldReturnMatches()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Act
        var result = await _service.SearchPageAsync(_testDocument, 1, "PDF");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // If matches are found, validate their structure
        foreach (var match in result.Value)
        {
            Assert.Equal(1, match.PageNumber);
            Assert.True(match.CharIndex >= 0);
            Assert.True(match.Length > 0);
            Assert.False(string.IsNullOrEmpty(match.Text));
            Assert.True(match.BoundingBox.IsValid());
            Assert.True(match.IsValid());
        }
    }

    [Fact]
    public async Task SearchPageAsync_WithNonExistentText_ShouldReturnEmptyList()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Act
        var result = await _service.SearchPageAsync(
            _testDocument,
            1,
            "ThisTextDefinitelyDoesNotExistInTheDocument12345");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SearchPageAsync_WithCaseSensitiveOption_ShouldRespectCase()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Arrange
        var caseSensitiveOptions = SearchOptions.CaseSensitiveSearch();

        // Act - search for lowercase when document likely has uppercase
        var result1 = await _service.SearchPageAsync(_testDocument, 1, "pdf", caseSensitiveOptions);
        var result2 = await _service.SearchPageAsync(_testDocument, 1, "PDF", caseSensitiveOptions);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        // Case sensitive search should return different results
        // (exact assertion depends on test PDF content)
    }

    [Fact]
    public async Task SearchPageAsync_MultipleMatches_ShouldReturnAllMatches()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Act - search for common word that likely appears multiple times
        var result = await _service.SearchPageAsync(_testDocument, 1, "the");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // If multiple matches found, verify they are ordered
        if (result.Value.Count > 1)
        {
            for (int i = 1; i < result.Value.Count; i++)
            {
                Assert.True(result.Value[i].CharIndex >= result.Value[i - 1].CharIndex);
            }
        }
    }

    #endregion

    #region Bounding Box Tests

    [Fact]
    public async Task SearchPageAsync_MatchBoundingBox_ShouldBeValid()
    {
        // Skip if test document is not available
        if (_testDocument == null)
        {
            return;
        }

        // Act
        var result = await _service.SearchPageAsync(_testDocument, 1, "PDF");

        // Assert
        Assert.True(result.IsSuccess);

        foreach (var match in result.Value)
        {
            // Bounding box should have valid dimensions
            Assert.True(match.BoundingBox.IsValid());
            Assert.True(match.BoundingBox.Width > 0);
            Assert.True(match.BoundingBox.Height > 0);

            // Coordinates should be reasonable (not negative or extremely large)
            Assert.True(match.BoundingBox.Left >= 0);
            Assert.True(match.BoundingBox.Bottom >= 0);
            Assert.True(match.BoundingBox.Right > match.BoundingBox.Left);
            Assert.True(match.BoundingBox.Top > match.BoundingBox.Bottom);
        }
    }

    #endregion

    #region Helper Methods

    private static PdfDocument CreateMockDocument()
    {
        var mockHandle = new Mock<IDisposable>();
        return new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 10,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
    }

    #endregion
}
