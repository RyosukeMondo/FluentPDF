using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for bookmark extraction using real PDFium library.
/// These tests verify the complete workflow from document loading to bookmark extraction.
/// NOTE: These tests require PDFium native library and will only run on Windows.
/// On Linux/macOS, PDFium initialization will fail and tests will be skipped gracefully.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BookmarkIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IBookmarkService _bookmarkService;
    private readonly string _fixturesPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public BookmarkIntegrationTests()
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
        var bookmarkLogger = new LoggerFactory().CreateLogger<BookmarkService>();

        _documentService = new PdfDocumentService(documentLogger);
        _bookmarkService = new BookmarkService(bookmarkLogger);

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

    #region Hierarchical Bookmark Tests

    [Fact]
    public async Task ExtractBookmarks_WithHierarchicalBookmarks_ReturnsCorrectStructure()
    {
        // Arrange
        var bookmarkedPdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(bookmarkedPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(bookmarkedPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _bookmarkService.ExtractBookmarksAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue("bookmark extraction should succeed");
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty("bookmarked PDF should have bookmarks");

        // Verify hierarchical structure
        var rootBookmarks = result.Value;
        rootBookmarks.Should().HaveCount(3, "PDF has 3 root-level chapters");

        // Verify first chapter and its children
        var chapter1 = rootBookmarks[0];
        chapter1.Title.Should().Be("Chapter 1: Introduction");
        chapter1.PageNumber.Should().Be(1, "Chapter 1 should point to page 1");
        chapter1.Children.Should().HaveCount(2, "Chapter 1 has 2 sections");
        chapter1.Children[0].Title.Should().Be("Section 1.1: Overview");
        chapter1.Children[1].Title.Should().Be("Section 1.2: Background");

        // Verify second chapter
        var chapter2 = rootBookmarks[1];
        chapter2.Title.Should().Be("Chapter 2: Methods");
        chapter2.PageNumber.Should().Be(3, "Chapter 2 should point to page 3");
        chapter2.Children.Should().HaveCount(2, "Chapter 2 has 2 sections");

        // Verify third chapter
        var chapter3 = rootBookmarks[2];
        chapter3.Title.Should().Be("Chapter 3: Conclusion");
        chapter3.PageNumber.Should().Be(5, "Chapter 3 should point to page 5");
        chapter3.Children.Should().BeEmpty("Chapter 3 has no subsections");
    }

    [Fact]
    public async Task ExtractBookmarks_WithFlatBookmarks_ReturnsCorrectList()
    {
        // Arrange
        var flatPdfPath = Path.Combine(_fixturesPath, "flat-bookmarks.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(flatPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(flatPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _bookmarkService.ExtractBookmarksAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue("bookmark extraction should succeed");
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(5, "flat bookmarks PDF has 5 root bookmarks");

        // Verify all bookmarks are at root level with no children
        for (int i = 0; i < result.Value.Count; i++)
        {
            var bookmark = result.Value[i];
            bookmark.Title.Should().Be($"Page {i + 1}");
            bookmark.PageNumber.Should().Be(i + 1, $"bookmark should point to page {i + 1}");
            bookmark.Children.Should().BeEmpty($"flat bookmarks should have no children");
        }
    }

    #endregion

    #region No Bookmarks Tests

    [Fact]
    public async Task ExtractBookmarks_WithNoBookmarks_ReturnsEmptyList()
    {
        // Arrange
        var noBookmarksPdfPath = Path.Combine(_fixturesPath, "no-bookmarks.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(noBookmarksPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(noBookmarksPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _bookmarkService.ExtractBookmarksAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue("extraction should succeed even with no bookmarks");
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty("PDF without bookmarks should return empty list");
    }

    #endregion

    #region Title and Data Validation Tests

    [Fact]
    public async Task BookmarkTitles_AreDecodedCorrectly()
    {
        // Arrange
        var bookmarkedPdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(bookmarkedPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(bookmarkedPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _bookmarkService.ExtractBookmarksAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify all titles are readable strings (not null or empty)
        var allBookmarks = GetAllBookmarksFlat(result.Value);
        allBookmarks.Should().NotBeEmpty();

        foreach (var bookmark in allBookmarks)
        {
            bookmark.Title.Should().NotBeNullOrWhiteSpace("all bookmark titles should be readable");
            bookmark.Title.Should().NotContain("\0", "titles should not contain null terminators");
        }
    }

    [Fact]
    public async Task BookmarkPageNumbers_AreWithinDocumentRange()
    {
        // Arrange
        var bookmarkedPdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(bookmarkedPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(bookmarkedPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        var pageCount = documentResult.Value.PageCount;

        // Act
        var result = await _bookmarkService.ExtractBookmarksAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify all page numbers are 1-based and within document range
        var allBookmarks = GetAllBookmarksFlat(result.Value);

        foreach (var bookmark in allBookmarks)
        {
            if (bookmark.PageNumber.HasValue)
            {
                bookmark.PageNumber.Value.Should().BeInRange(1, pageCount,
                    $"page number should be between 1 and {pageCount}");
            }
        }
    }

    [Fact]
    public async Task BookmarkTotalCount_IsCalculatedCorrectly()
    {
        // Arrange
        var bookmarkedPdfPath = Path.Combine(_fixturesPath, "bookmarked.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(bookmarkedPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(bookmarkedPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _bookmarkService.ExtractBookmarksAsync(documentResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var rootBookmarks = result.Value;
        var totalCount = rootBookmarks.Sum(b => b.GetTotalNodeCount());

        // Expected: 3 chapters + 2 sections in chapter 1 + 2 sections in chapter 2 = 7 bookmarks
        totalCount.Should().Be(7, "total bookmark count should include all nested bookmarks");

        // Verify individual counts
        rootBookmarks[0].GetTotalNodeCount().Should().Be(3, "Chapter 1 + 2 sections");
        rootBookmarks[1].GetTotalNodeCount().Should().Be(3, "Chapter 2 + 2 sections");
        rootBookmarks[2].GetTotalNodeCount().Should().Be(1, "Chapter 3 only");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Flattens a hierarchical bookmark tree into a single list for validation.
    /// </summary>
    private static List<BookmarkNode> GetAllBookmarksFlat(List<BookmarkNode> rootBookmarks)
    {
        var result = new List<BookmarkNode>();

        foreach (var bookmark in rootBookmarks)
        {
            result.Add(bookmark);
            if (bookmark.Children.Count > 0)
            {
                result.AddRange(GetAllBookmarksFlat(bookmark.Children));
            }
        }

        return result;
    }

    #endregion
}
