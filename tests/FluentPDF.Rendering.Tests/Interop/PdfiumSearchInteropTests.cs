using FluentAssertions;
using FluentPDF.Rendering.Interop;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop;

/// <summary>
/// Unit tests for PDFium search P/Invoke layer.
/// These tests verify basic search functionality without requiring actual PDF files.
/// Integration tests with real PDFs are in the Integration folder.
/// </summary>
public class PdfiumSearchInteropTests : IDisposable
{
    private bool _isInitialized;

    public PdfiumSearchInteropTests()
    {
        _isInitialized = PdfiumInterop.Initialize();
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void StartTextSearch_WithInvalidTextPage_ShouldThrowException()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        Action act = () => PdfiumInterop.StartTextSearch(invalidTextPage, "test", PdfiumInterop.SearchFlags.None);

        // Assert
        act.Should().Throw<ArgumentException>("starting search on invalid text page should throw");
    }

    [Fact]
    public void StartTextSearch_WithNullTextPage_ShouldThrowException()
    {
        // Arrange
        SafePdfTextPageHandle? nullPage = null;

        // Act
        Action act = () => PdfiumInterop.StartTextSearch(nullPage!, "test", PdfiumInterop.SearchFlags.None);

        // Assert
        act.Should().Throw<ArgumentException>("starting search on null text page should throw");
    }

    [Fact]
    public void StartTextSearch_WithNullQuery_ShouldThrowException()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        Action act = () => PdfiumInterop.StartTextSearch(invalidTextPage, null!, PdfiumInterop.SearchFlags.None);

        // Assert
        act.Should().Throw<ArgumentException>("starting search with null query should throw");
    }

    [Fact]
    public void StartTextSearch_WithEmptyQuery_ShouldThrowException()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        Action act = () => PdfiumInterop.StartTextSearch(invalidTextPage, string.Empty, PdfiumInterop.SearchFlags.None);

        // Assert
        act.Should().Throw<ArgumentException>("starting search with empty query should throw");
    }

    [Fact]
    public void FindNext_WithZeroHandle_ShouldReturnFalse()
    {
        // Arrange
        var zeroHandle = IntPtr.Zero;

        // Act
        var result = PdfiumInterop.FindNext(zeroHandle);

        // Assert
        result.Should().BeFalse("finding next with zero handle should return false");
    }

    [Fact]
    public void FindPrev_WithZeroHandle_ShouldReturnFalse()
    {
        // Arrange
        var zeroHandle = IntPtr.Zero;

        // Act
        var result = PdfiumInterop.FindPrev(zeroHandle);

        // Assert
        result.Should().BeFalse("finding previous with zero handle should return false");
    }

    [Fact]
    public void GetSearchResultIndex_WithZeroHandle_ShouldReturnNegativeOne()
    {
        // Arrange
        var zeroHandle = IntPtr.Zero;

        // Act
        var index = PdfiumInterop.GetSearchResultIndex(zeroHandle);

        // Assert
        index.Should().Be(-1, "getting result index with zero handle should return -1");
    }

    [Fact]
    public void GetSearchResultCount_WithZeroHandle_ShouldReturnZero()
    {
        // Arrange
        var zeroHandle = IntPtr.Zero;

        // Act
        var count = PdfiumInterop.GetSearchResultCount(zeroHandle);

        // Assert
        count.Should().Be(0, "getting result count with zero handle should return 0");
    }

    [Fact]
    public void CloseSearch_WithZeroHandle_ShouldNotThrow()
    {
        // Arrange
        var zeroHandle = IntPtr.Zero;

        // Act
        Action act = () => PdfiumInterop.CloseSearch(zeroHandle);

        // Assert
        act.Should().NotThrow("closing search with zero handle should be safe");
    }

    [Fact]
    public void SearchFlags_None_ShouldBeZero()
    {
        // Assert
        ((uint)PdfiumInterop.SearchFlags.None).Should().Be(0u, "None flag should be 0");
    }

    [Fact]
    public void SearchFlags_MatchCase_ShouldBeOne()
    {
        // Assert
        ((uint)PdfiumInterop.SearchFlags.MatchCase).Should().Be(1u, "MatchCase flag should be 1");
    }

    [Fact]
    public void SearchFlags_MatchWholeWord_ShouldBeTwo()
    {
        // Assert
        ((uint)PdfiumInterop.SearchFlags.MatchWholeWord).Should().Be(2u, "MatchWholeWord flag should be 2");
    }

    [Fact]
    public void SearchFlags_Consecutive_ShouldBeFour()
    {
        // Assert
        ((uint)PdfiumInterop.SearchFlags.Consecutive).Should().Be(4u, "Consecutive flag should be 4");
    }

    [Fact]
    public void SearchFlags_CanBeCombined()
    {
        // Act
        var combined = PdfiumInterop.SearchFlags.MatchCase | PdfiumInterop.SearchFlags.MatchWholeWord;

        // Assert
        ((uint)combined).Should().Be(3u, "flags should be combinable with bitwise OR");
    }
}
