using FluentAssertions;
using FluentPDF.Rendering.Tests.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for HeadlessRenderingService.
/// Tests headless PDF rendering to PNG files using Win2D.
/// </summary>
public sealed class HeadlessRenderingServiceTests : IDisposable
{
    private readonly Mock<ILogger<HeadlessRenderingService>> _loggerMock;
    private readonly HeadlessRenderingService _service;
    private readonly string _testOutputDir;
    private readonly string _samplePdfPath;

    public HeadlessRenderingServiceTests()
    {
        _loggerMock = new Mock<ILogger<HeadlessRenderingService>>();
        _service = new HeadlessRenderingService(_loggerMock.Object);
        _testOutputDir = Path.Combine(Path.GetTempPath(), "FluentPDF_HeadlessRenderingTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);

        // Create a simple test PDF
        _samplePdfPath = Path.Combine(_testOutputDir, "sample.pdf");
        CreateSamplePdf(_samplePdfPath);
    }

    [Fact]
    public async Task RenderPageToFileAsync_WithValidPdf_ShouldCreatePngFile()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDir, "output.png");

        // Act
        var result = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 1, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();

        var fileInfo = new FileInfo(outputPath);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderPageToFileAsync_WithNonExistentPdf_ShouldFail()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testOutputDir, "nonexistent.pdf");
        var outputPath = Path.Combine(_testOutputDir, "output.png");

        // Act
        var result = await _service.RenderPageToFileAsync(nonExistentPath, pageNumber: 1, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is FluentPDF.Core.ErrorHandling.PdfError);
        File.Exists(outputPath).Should().BeFalse();
    }

    [Fact]
    public async Task RenderPageToFileAsync_WithInvalidPageNumber_ShouldFail()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDir, "output.png");

        // Act
        var result = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 0, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is FluentPDF.Core.ErrorHandling.PdfError);
    }

    [Fact]
    public async Task RenderPageToFileAsync_WithPageNumberExceedingPageCount_ShouldFail()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDir, "output.png");

        // Act
        var result = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 999, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is FluentPDF.Core.ErrorHandling.PdfError);
    }

    [Fact]
    public async Task RenderPageToFileAsync_WithCustomDpi_ShouldCreateLargerFile()
    {
        // Arrange
        var outputPath96 = Path.Combine(_testOutputDir, "output_96dpi.png");
        var outputPath300 = Path.Combine(_testOutputDir, "output_300dpi.png");

        // Act
        var result96 = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 1, outputPath96, dpi: 96);
        var result300 = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 1, outputPath300, dpi: 300);

        // Assert
        result96.IsSuccess.Should().BeTrue();
        result300.IsSuccess.Should().BeTrue();

        var fileSize96 = new FileInfo(outputPath96).Length;
        var fileSize300 = new FileInfo(outputPath300).Length;

        fileSize300.Should().BeGreaterThan(fileSize96);
    }

    [Fact]
    public async Task RenderPageToFileAsync_WithNonExistentOutputDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var newDir = Path.Combine(_testOutputDir, "newdir", "subdir");
        var outputPath = Path.Combine(newDir, "output.png");

        // Act
        var result = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 1, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(newDir).Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task RenderPageToFileAsync_WhenCancelled_ShouldFailGracefully()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDir, "output.png");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 1, outputPath, cancellationToken: cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenCalled_ShouldNotThrow()
    {
        // Act
        var act = () => _service.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RenderPageToFileAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDir, "output.png");
        _service.Dispose();

        // Act
        Func<Task> act = async () => await _service.RenderPageToFileAsync(_samplePdfPath, pageNumber: 1, outputPath);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    public void Dispose()
    {
        try
        {
            _service?.Dispose();

            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void CreateSamplePdf(string path)
    {
        // Create a minimal valid PDF file
        // This is a simple one-page PDF with "Hello World" text
        var pdfContent = @"%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 5 0 R
>>
>>
>>
endobj
4 0 obj
<<
/Length 44
>>
stream
BT
/F1 24 Tf
100 700 Td
(Hello World) Tj
ET
endstream
endobj
5 0 obj
<<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
endobj
xref
0 6
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
0000000262 00000 n
0000000356 00000 n
trailer
<<
/Size 6
/Root 1 0 R
>>
startxref
444
%%EOF";

        File.WriteAllText(path, pdfContent);
    }
}
