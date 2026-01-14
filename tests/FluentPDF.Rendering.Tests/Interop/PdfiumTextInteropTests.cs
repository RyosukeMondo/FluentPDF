using FluentAssertions;
using FluentPDF.Rendering.Interop;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop;

/// <summary>
/// Unit tests for PDFium text extraction P/Invoke layer.
/// These tests verify basic text functionality without requiring actual PDF files.
/// Integration tests with real PDFs are in the Integration folder.
/// </summary>
public class PdfiumTextInteropTests : IDisposable
{
    private bool _isInitialized;

    public PdfiumTextInteropTests()
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
    public void LoadTextPage_WithInvalidPage_ShouldThrowException()
    {
        // Arrange
        using var invalidPage = new SafePdfPageHandle();

        // Act
        Action act = () => PdfiumInterop.LoadTextPage(invalidPage);

        // Assert
        act.Should().Throw<ArgumentException>("loading text page from invalid page should throw");
    }

    [Fact]
    public void LoadTextPage_WithNullPage_ShouldThrowException()
    {
        // Arrange
        SafePdfPageHandle? nullPage = null;

        // Act
        Action act = () => PdfiumInterop.LoadTextPage(nullPage!);

        // Assert
        act.Should().Throw<ArgumentException>("loading text page from null page should throw");
    }

    [Fact]
    public void GetTextCharCount_WithInvalidHandle_ShouldReturnZero()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        var count = PdfiumInterop.GetTextCharCount(invalidTextPage);

        // Assert
        count.Should().Be(0, "invalid text page handle should return 0 characters");
    }

    [Fact]
    public void GetTextCharCount_WithNullHandle_ShouldReturnZero()
    {
        // Arrange
        SafePdfTextPageHandle? nullHandle = null;

        // Act
        var count = PdfiumInterop.GetTextCharCount(nullHandle!);

        // Assert
        count.Should().Be(0, "null text page handle should return 0 characters");
    }

    [Fact]
    public void GetText_WithInvalidHandle_ShouldReturnEmptyString()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        var text = PdfiumInterop.GetText(invalidTextPage, startIndex: 0, count: 10);

        // Assert
        text.Should().BeEmpty("invalid text page handle should return empty string");
    }

    [Fact]
    public void GetText_WithNullHandle_ShouldReturnEmptyString()
    {
        // Arrange
        SafePdfTextPageHandle? nullHandle = null;

        // Act
        var text = PdfiumInterop.GetText(nullHandle!, startIndex: 0, count: 10);

        // Assert
        text.Should().BeEmpty("null text page handle should return empty string");
    }

    [Fact]
    public void GetText_WithZeroCount_ShouldReturnEmptyString()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        var text = PdfiumInterop.GetText(invalidTextPage, startIndex: 0, count: 0);

        // Assert
        text.Should().BeEmpty("zero count should return empty string");
    }

    [Fact]
    public void GetText_WithNegativeCount_ShouldReturnEmptyString()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        var text = PdfiumInterop.GetText(invalidTextPage, startIndex: 0, count: -5);

        // Assert
        text.Should().BeEmpty("negative count should return empty string");
    }

    [Fact]
    public void GetCharBox_WithInvalidHandle_ShouldReturnFalse()
    {
        // Arrange
        using var invalidTextPage = new SafePdfTextPageHandle();

        // Act
        var result = PdfiumInterop.GetCharBox(
            invalidTextPage,
            charIndex: 0,
            out var left,
            out var top,
            out var right,
            out var bottom);

        // Assert
        result.Should().BeFalse("invalid text page handle should return false");
        left.Should().Be(0);
        top.Should().Be(0);
        right.Should().Be(0);
        bottom.Should().Be(0);
    }

    [Fact]
    public void GetCharBox_WithNullHandle_ShouldReturnFalse()
    {
        // Arrange
        SafePdfTextPageHandle? nullHandle = null;

        // Act
        var result = PdfiumInterop.GetCharBox(
            nullHandle!,
            charIndex: 0,
            out var left,
            out var top,
            out var right,
            out var bottom);

        // Assert
        result.Should().BeFalse("null text page handle should return false");
        left.Should().Be(0);
        top.Should().Be(0);
        right.Should().Be(0);
        bottom.Should().Be(0);
    }

    [Fact]
    public void SafePdfTextPageHandle_WhenDisposed_ShouldReleaseResources()
    {
        // Arrange
        SafePdfTextPageHandle? handle = null;

        // Act
        using (handle = new SafePdfTextPageHandle())
        {
            handle.IsInvalid.Should().BeTrue("new handle without initialization should be invalid");
        }

        // Assert
        handle.IsClosed.Should().BeTrue("handle should be closed after dispose");
    }

    [Fact]
    public void SafePdfTextPageHandle_MultipleDispose_ShouldNotThrow()
    {
        // Arrange
        var handle = new SafePdfTextPageHandle();

        // Act
        Action act = () =>
        {
            handle.Dispose();
            handle.Dispose();
        };

        // Assert
        act.Should().NotThrow("multiple dispose should be safe");
    }
}
