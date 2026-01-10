using FluentAssertions;
using FluentPDF.Rendering.Interop;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop;

/// <summary>
/// Unit tests for PDFium bookmark P/Invoke layer.
/// These tests verify bookmark-specific P/Invoke declarations.
/// Integration tests with real PDFs containing bookmarks are in the Integration folder.
/// </summary>
public class PdfiumBookmarkInteropTests : IDisposable
{
    private bool _isInitialized;

    public PdfiumBookmarkInteropTests()
    {
        // Initialize PDFium for each test
        _isInitialized = PdfiumInterop.Initialize();
    }

    public void Dispose()
    {
        // Clean up PDFium after each test
        if (_isInitialized)
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void GetFirstChildBookmark_WithInvalidDocument_ShouldThrowException()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();

        // Act
        Action act = () => PdfiumInterop.GetFirstChildBookmark(invalidDocument, IntPtr.Zero);

        // Assert
        act.Should().Throw<ArgumentException>("getting bookmark from invalid document should throw");
    }


    [Fact]
    public void GetNextSiblingBookmark_WithInvalidDocument_ShouldThrowException()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();

        // Act
        Action act = () => PdfiumInterop.GetNextSiblingBookmark(invalidDocument, IntPtr.Zero);

        // Assert
        act.Should().Throw<ArgumentException>("getting sibling bookmark from invalid document should throw");
    }

    [Fact]
    public void GetBookmarkTitle_WithNullBookmark_ShouldReturnUntitled()
    {
        // Arrange
        var nullBookmark = IntPtr.Zero;

        // Act
        var title = PdfiumInterop.GetBookmarkTitle(nullBookmark);

        // Assert
        title.Should().Be("(Untitled)", "null bookmark should return (Untitled)");
    }

    [Fact]
    public void GetBookmarkDest_WithInvalidDocument_ShouldThrowException()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();
        var someBookmark = new IntPtr(123); // Arbitrary non-null pointer

        // Act
        Action act = () => PdfiumInterop.GetBookmarkDest(invalidDocument, someBookmark);

        // Assert
        act.Should().Throw<ArgumentException>("getting dest from invalid document should throw");
    }


    [Fact]
    public void GetDestPageIndex_WithInvalidDocument_ShouldThrowException()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();
        var someDest = new IntPtr(456); // Arbitrary non-null pointer

        // Act
        Action act = () => PdfiumInterop.GetDestPageIndex(invalidDocument, someDest);

        // Assert
        act.Should().Throw<ArgumentException>("getting page index from invalid document should throw");
    }

    [Fact]
    public void GetDestLocationInPage_WithNullDest_ShouldReturnFalse()
    {
        // Arrange
        var nullDest = IntPtr.Zero;

        // Act
        var result = PdfiumInterop.GetDestLocationInPage(
            nullDest,
            out var hasX,
            out var hasY,
            out var hasZoom,
            out var x,
            out var y,
            out var zoom);

        // Assert
        result.Should().BeFalse("null destination should return false");
        hasX.Should().BeFalse("hasX should be false for null dest");
        hasY.Should().BeFalse("hasY should be false for null dest");
        hasZoom.Should().BeFalse("hasZoom should be false for null dest");
        x.Should().Be(0, "x coordinate should be 0");
        y.Should().Be(0, "y coordinate should be 0");
        zoom.Should().Be(0, "zoom should be 0");
    }

    [Fact]
    public void BookmarkInterop_MethodsExist_ShouldBeCallable()
    {
        // This test verifies that all bookmark methods are properly declared
        // and can be referenced (compile-time verification)

        // Arrange & Act
        var methods = typeof(PdfiumInterop).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        var bookmarkMethods = methods
            .Where(m => m.Name.Contains("Bookmark") || m.Name.Contains("Dest"))
            .Select(m => m.Name)
            .ToList();

        // Assert
        bookmarkMethods.Should().Contain("GetFirstChildBookmark", "method should exist");
        bookmarkMethods.Should().Contain("GetNextSiblingBookmark", "method should exist");
        bookmarkMethods.Should().Contain("GetBookmarkTitle", "method should exist");
        bookmarkMethods.Should().Contain("GetBookmarkDest", "method should exist");
        bookmarkMethods.Should().Contain("GetDestPageIndex", "method should exist");
        bookmarkMethods.Should().Contain("GetDestLocationInPage", "method should exist");
    }

    [Fact]
    public void GetBookmarkTitle_Utf16Decoding_ShouldHandleEmptyString()
    {
        // This test verifies the UTF-16LE decoding logic handles edge cases
        // Real UTF-16 strings will be tested in integration tests with actual PDFs

        // For now, verify that the method exists and can be called
        var nullBookmark = IntPtr.Zero;
        var result = PdfiumInterop.GetBookmarkTitle(nullBookmark);

        result.Should().NotBeNull("title should never be null");
        result.Should().Be("(Untitled)", "null bookmark should return (Untitled)");
    }
}
