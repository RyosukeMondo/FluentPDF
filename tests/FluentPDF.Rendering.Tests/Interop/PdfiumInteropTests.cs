using FluentAssertions;
using FluentPDF.Rendering.Interop;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop;

/// <summary>
/// Unit tests for PDFium P/Invoke layer.
/// These tests verify basic PDFium functionality without requiring actual PDF files.
/// Integration tests with real PDFs are in the Integration folder.
/// </summary>
public class PdfiumInteropTests : IDisposable
{
    private bool _isInitialized;

    public PdfiumInteropTests()
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
    public void Initialize_ShouldSucceed()
    {
        // Arrange & Act is done in constructor

        // Assert
        _isInitialized.Should().BeTrue("PDFium library should initialize successfully");
    }

    [Fact]
    public void Initialize_CalledTwice_ShouldNotFail()
    {
        // Arrange - already initialized in constructor

        // Act
        var result = PdfiumInterop.Initialize();

        // Assert
        result.Should().BeTrue("calling Initialize twice should be safe");
    }

    [Fact]
    public void LoadDocument_WithNullPath_ShouldReturnInvalidHandle()
    {
        // Arrange
        string? nullPath = null;

        // Act
        Action act = () => PdfiumInterop.LoadDocument(nullPath!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadDocument_WithNonExistentFile_ShouldReturnInvalidHandle()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent_file_12345.pdf";

        // Act
        using var handle = PdfiumInterop.LoadDocument(nonExistentPath);

        // Assert
        handle.Should().NotBeNull();
        handle.IsInvalid.Should().BeTrue("loading a non-existent file should return an invalid handle");

        // Verify error code
        var errorCode = PdfiumInterop.GetLastError();
        errorCode.Should().Be(PdfiumInterop.ErrorCodes.File, "error code should indicate file error");
    }

    [Fact]
    public void GetPageCount_WithInvalidHandle_ShouldReturnZero()
    {
        // Arrange
        using var invalidHandle = new SafePdfDocumentHandle();

        // Act
        var pageCount = PdfiumInterop.GetPageCount(invalidHandle);

        // Assert
        pageCount.Should().Be(0, "invalid handle should return 0 pages");
    }

    [Fact]
    public void LoadPage_WithInvalidDocument_ShouldThrowException()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();

        // Act
        Action act = () => PdfiumInterop.LoadPage(invalidDocument, 0);

        // Assert
        act.Should().Throw<ArgumentException>("loading page from invalid document should throw");
    }

    [Fact]
    public void GetPageWidth_WithInvalidHandle_ShouldReturnZero()
    {
        // Arrange
        using var invalidPage = new SafePdfPageHandle();

        // Act
        var width = PdfiumInterop.GetPageWidth(invalidPage);

        // Assert
        width.Should().Be(0, "invalid page handle should return 0 width");
    }

    [Fact]
    public void GetPageHeight_WithInvalidHandle_ShouldReturnZero()
    {
        // Arrange
        using var invalidPage = new SafePdfPageHandle();

        // Act
        var height = PdfiumInterop.GetPageHeight(invalidPage);

        // Assert
        height.Should().Be(0, "invalid page handle should return 0 height");
    }

    [Fact]
    public void CreateBitmap_WithValidDimensions_ShouldReturnValidHandle()
    {
        // Arrange
        var width = 800;
        var height = 600;

        // Act
        var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);

        try
        {
            // Assert
            bitmap.Should().NotBe(IntPtr.Zero, "bitmap should be created successfully");

            // Verify we can get buffer
            var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
            buffer.Should().NotBe(IntPtr.Zero, "bitmap buffer should be valid");

            // Verify stride
            var stride = PdfiumInterop.GetBitmapStride(bitmap);
            stride.Should().BeGreaterThan(0, "bitmap stride should be positive");
        }
        finally
        {
            // Clean up
            PdfiumInterop.DestroyBitmap(bitmap);
        }
    }

    [Fact]
    public void DestroyBitmap_WithNullHandle_ShouldNotThrow()
    {
        // Arrange
        var nullBitmap = IntPtr.Zero;

        // Act
        Action act = () => PdfiumInterop.DestroyBitmap(nullBitmap);

        // Assert
        act.Should().NotThrow("destroying null bitmap should be safe");
    }

    [Fact]
    public void FillBitmap_WithValidBitmap_ShouldNotThrow()
    {
        // Arrange
        var bitmap = PdfiumInterop.CreateBitmap(100, 100, hasAlpha: true);

        try
        {
            // Act
            Action act = () => PdfiumInterop.FillBitmap(bitmap, 0xFFFFFFFF);

            // Assert
            act.Should().NotThrow("filling bitmap should succeed");
        }
        finally
        {
            PdfiumInterop.DestroyBitmap(bitmap);
        }
    }

    [Fact]
    public void SafePdfDocumentHandle_WhenDisposed_ShouldReleaseResources()
    {
        // Arrange
        SafePdfDocumentHandle? handle = null;

        // Act
        using (handle = new SafePdfDocumentHandle())
        {
            handle.IsInvalid.Should().BeTrue("new handle without initialization should be invalid");
        }

        // Assert
        handle.IsClosed.Should().BeTrue("handle should be closed after dispose");
    }

    [Fact]
    public void SafePdfPageHandle_WhenDisposed_ShouldReleaseResources()
    {
        // Arrange
        SafePdfPageHandle? handle = null;

        // Act
        using (handle = new SafePdfPageHandle())
        {
            handle.IsInvalid.Should().BeTrue("new handle without initialization should be invalid");
        }

        // Assert
        handle.IsClosed.Should().BeTrue("handle should be closed after dispose");
    }

    [Fact]
    public void RenderPageBitmap_WithInvalidBitmap_ShouldThrowException()
    {
        // Arrange
        using var invalidPage = new SafePdfPageHandle();
        var invalidBitmap = IntPtr.Zero;

        // Act
        Action act = () => PdfiumInterop.RenderPageBitmap(
            invalidBitmap,
            invalidPage,
            startX: 0,
            startY: 0,
            sizeX: 100,
            sizeY: 100,
            rotate: 0,
            flags: PdfiumInterop.RenderFlags.Normal);

        // Assert
        act.Should().Throw<ArgumentException>("rendering with invalid bitmap should throw");
    }

    [Fact]
    public void RenderFlags_ShouldHaveExpectedValues()
    {
        // Assert - verify flag constants are defined correctly
        PdfiumInterop.RenderFlags.Normal.Should().Be(0);
        PdfiumInterop.RenderFlags.Annotations.Should().Be(0x01);
        PdfiumInterop.RenderFlags.LcdText.Should().Be(0x02);
        PdfiumInterop.RenderFlags.Grayscale.Should().Be(0x40);
        PdfiumInterop.RenderFlags.Printing.Should().Be(0x800);
    }

    [Fact]
    public void ErrorCodes_ShouldHaveExpectedValues()
    {
        // Assert - verify error code constants
        PdfiumInterop.ErrorCodes.Success.Should().Be(0);
        PdfiumInterop.ErrorCodes.Unknown.Should().Be(1);
        PdfiumInterop.ErrorCodes.File.Should().Be(2);
        PdfiumInterop.ErrorCodes.Format.Should().Be(3);
        PdfiumInterop.ErrorCodes.Password.Should().Be(4);
        PdfiumInterop.ErrorCodes.Security.Should().Be(5);
        PdfiumInterop.ErrorCodes.Page.Should().Be(6);
    }
}
