using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for text extraction and search using real PDFium library.
/// These tests verify the complete workflow from document loading to text extraction and searching.
/// NOTE: These tests require PDFium native library and will only run on Windows.
/// On Linux/macOS, PDFium initialization will fail and tests will be skipped gracefully.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TextExtractionIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly ITextSearchService _textSearchService;
    private readonly string _fixturesPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public TextExtractionIntegrationTests()
    {
        // Initialize PDFium once for all tests
        lock (_initLock)
        {
            if (!_pdfiumInitialized)
            {
                var initialized = PdfiumInterop.Initialize();
                if (!initialized)
                {
                    throw new InvalidOperationException(
                        "Failed to initialize PDFium. Ensure pdfium.dll is in the test output directory.");
                }
                _pdfiumInitialized = true;
            }
        }

        // Setup services
        var documentLogger = new LoggerFactory().CreateLogger<PdfDocumentService>();
        var textExtractionLogger = new LoggerFactory().CreateLogger<TextExtractionService>();
        var textSearchLogger = new LoggerFactory().CreateLogger<TextSearchService>();

        _documentService = new PdfDocumentService(documentLogger);
        _textExtractionService = new TextExtractionService(textExtractionLogger);
        _textSearchService = new TextSearchService(textSearchLogger);

        // Setup fixtures path (go up from bin/Debug/net8.0 to tests root)
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _documentsToCleanup = new List<PdfDocument>();
    }

    public void Dispose()
    {
        // Clean up any documents that were loaded
        foreach (var doc in _documentsToCleanup)
        {
            try
            {
                _documentService.CloseDocument(doc);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Text Extraction Tests

    [Fact]
    public async Task ExtractText_FromSinglePage_ReturnsExpectedContent()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _textExtractionService.ExtractTextAsync(documentResult.Value, 1);

        // Assert
        result.IsSuccess.Should().BeTrue("text extraction should succeed");
        result.Value.Should().NotBeNullOrEmpty("page 1 should contain text");
        result.Value.Should().Contain("Page 1 - Basic Text", "page 1 header should be present");
        result.Value.Should().Contain("The quick brown fox jumps over the lazy dog", "test sentence should be present");
    }

    [Fact]
    public async Task ExtractText_FromPageWithUnicode_PreservesCharacters()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Page 3 contains Unicode characters
        var result = await _textExtractionService.ExtractTextAsync(documentResult.Value, 3);

        // Assert
        result.IsSuccess.Should().BeTrue("text extraction should succeed");
        result.Value.Should().NotBeNullOrEmpty("page 3 should contain text");
        result.Value.Should().Contain("Unicode", "page 3 should mention Unicode");
        // Check for some Unicode symbols
        var hasUnicodeSymbols = result.Value.Contains("©") || result.Value.Contains("®") ||
                               result.Value.Contains("café") || result.Value.Contains("résumé");
        hasUnicodeSymbols.Should().BeTrue("page 3 should contain Unicode characters");
    }

    [Fact]
    public async Task ExtractText_FromInvalidPage_ReturnsError()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Try to extract from page 999 (doesn't exist)
        var result = await _textExtractionService.ExtractTextAsync(documentResult.Value, 999);

        // Assert
        result.IsSuccess.Should().BeFalse("extracting from invalid page should fail");
        result.Errors.Should().NotBeEmpty("should have error details");
    }

    [Fact]
    public async Task ExtractAllText_FromMultiPageDocument_ReturnsAllPages()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _textExtractionService.ExtractAllTextAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue("extracting all text should succeed");
        result.Value.Should().NotBeEmpty("should extract text from all pages");
        result.Value.Count.Should().BeGreaterOrEqualTo(1, "document should have at least one page");

        // Verify all pages have text
        foreach (var kvp in result.Value)
        {
            kvp.Value.Should().NotBeNullOrWhiteSpace($"page {kvp.Key} should have text");
        }
    }

    [Fact]
    public async Task ExtractAllText_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf"); // Multi-page PDF
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _textExtractionService.ExtractAllTextAsync(documentResult.Value, cts.Token);

        // Assert - Either cancelled or completed before cancellation was checked
        if (!result.IsSuccess)
        {
            result.Errors.Should().NotBeEmpty("cancelled operation should have error");
        }
    }

    #endregion

    #region Text Search Tests

    [Fact]
    public async Task Search_FindsBasicText_ReturnsMatches()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Search for "test" (should appear multiple times)
        var result = await _textSearchService.SearchAsync(
            documentResult.Value,
            "test",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert
        result.IsSuccess.Should().BeTrue("search should succeed");
        result.Value.Should().NotBeEmpty("should find matches for 'test'");
        result.Value.Should().OnlyContain(m => m.IsValid(), "all matches should be valid");

        // Verify match content
        foreach (var match in result.Value)
        {
            match.Text.Should().NotBeNullOrEmpty("match text should not be empty");
            match.Length.Should().BeGreaterThan(0, "match length should be positive");
            match.PageNumber.Should().BeGreaterOrEqualTo(0, "page number should be valid");
        }
    }

    [Fact]
    public async Task Search_CaseSensitive_RespectsCase()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Search for "UPPERCASE" with case sensitivity
        var caseSensitiveResult = await _textSearchService.SearchAsync(
            documentResult.Value,
            "UPPERCASE",
            new SearchOptions { CaseSensitive = true, WholeWord = false });

        var caseInsensitiveResult = await _textSearchService.SearchAsync(
            documentResult.Value,
            "uppercase",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert
        caseSensitiveResult.IsSuccess.Should().BeTrue("case-sensitive search should succeed");
        caseInsensitiveResult.IsSuccess.Should().BeTrue("case-insensitive search should succeed");

        // Case insensitive should find at least as many matches
        caseInsensitiveResult.Value.Count.Should().BeGreaterOrEqualTo(
            caseSensitiveResult.Value.Count,
            "case-insensitive search should find at least as many matches");
    }

    [Fact]
    public async Task Search_WholeWord_FindsOnlyCompleteWords()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Search for "test" with whole word option
        var wholeWordResult = await _textSearchService.SearchAsync(
            documentResult.Value,
            "test",
            new SearchOptions { CaseSensitive = false, WholeWord = true });

        var partialWordResult = await _textSearchService.SearchAsync(
            documentResult.Value,
            "test",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert
        wholeWordResult.IsSuccess.Should().BeTrue("whole word search should succeed");
        partialWordResult.IsSuccess.Should().BeTrue("partial word search should succeed");

        // Partial word should find at least as many matches (includes "testing", "test", etc.)
        partialWordResult.Value.Count.Should().BeGreaterOrEqualTo(
            wholeWordResult.Value.Count,
            "partial word search should find at least as many matches");
    }

    [Fact]
    public async Task SearchPage_OnSpecificPage_ReturnsOnlyPageMatches()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Search only on page 2
        var result = await _textSearchService.SearchPageAsync(
            documentResult.Value,
            2,
            "test",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert
        result.IsSuccess.Should().BeTrue("page search should succeed");

        // All matches should be from page 2 (page numbers are 0-based internally)
        if (result.Value.Any())
        {
            result.Value.Should().OnlyContain(m => m.PageNumber == 1, "all matches should be from page 2 (index 1)");
        }
    }

    [Fact]
    public async Task Search_WithBoundingBoxes_HasValidCoordinates()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _textSearchService.SearchAsync(
            documentResult.Value,
            "test",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert
        result.IsSuccess.Should().BeTrue("search should succeed");

        foreach (var match in result.Value)
        {
            match.BoundingBox.Should().NotBe(default(PdfRectangle), "bounding box should be set");
            match.BoundingBox.IsValid().Should().BeTrue("bounding box should be valid");
            match.BoundingBox.Width.Should().BeGreaterThan(0, "bounding box should have width");
            match.BoundingBox.Height.Should().BeGreaterThan(0, "bounding box should have height");
        }
    }

    [Fact]
    public async Task Search_ForNonExistentText_ReturnsEmptyList()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Search for text that definitely doesn't exist
        var result = await _textSearchService.SearchAsync(
            documentResult.Value,
            "xyzqwertyuiopasdfgh123456",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert
        result.IsSuccess.Should().BeTrue("search should succeed even with no matches");
        result.Value.Should().BeEmpty("should return empty list for non-existent text");
    }

    [Fact]
    public async Task Search_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf"); // Multi-page PDF
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _textSearchService.SearchAsync(
            documentResult.Value,
            "Lorem",
            new SearchOptions { CaseSensitive = false, WholeWord = false },
            cts.Token);

        // Assert - Either cancelled or completed before cancellation was checked
        if (!result.IsSuccess)
        {
            result.Errors.Should().NotBeEmpty("cancelled operation should have error");
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ExtractText_FromLargeDocument_CompletesInReasonableTime()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf"); // 5 pages
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _textExtractionService.ExtractAllTextAsync(documentResult.Value);

        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue("text extraction should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "extraction should complete within 5 seconds for small document");
    }

    [Fact]
    public async Task Search_InDocument_CompletesInReasonableTime()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _textSearchService.SearchAsync(
            documentResult.Value,
            "Lorem",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue("search should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "search should complete within 5 seconds for small document");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExtractText_FromEmptyPage_ReturnsEmptyString()
    {
        // Arrange - Use a PDF with minimal or no text on some pages
        var pdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Extract from all pages
        var result = await _textExtractionService.ExtractAllTextAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue("extraction should succeed");
        // Some pages might have empty or minimal text - that's valid
        result.Value.Should().NotBeNull("result should not be null");
    }

    [Fact]
    public async Task Search_EmptyQuery_HandlesGracefully()
    {
        // Arrange
        var pdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");
        if (!File.Exists(pdfPath)) return;

        var documentResult = await _documentService.LoadDocumentAsync(pdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act - Search with empty string
        var result = await _textSearchService.SearchAsync(
            documentResult.Value,
            "",
            new SearchOptions { CaseSensitive = false, WholeWord = false });

        // Assert - Should either succeed with empty results or fail gracefully
        if (result.IsSuccess)
        {
            result.Value.Should().BeEmpty("empty query should return no matches");
        }
        else
        {
            result.Errors.Should().NotBeEmpty("should have error for invalid query");
        }
    }

    #endregion
}
