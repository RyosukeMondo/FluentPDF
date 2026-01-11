using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for AnnotationService.
/// Note: Most tests use mocks to avoid PDFium dependencies.
/// Integration tests with real PDFium are in the Integration folder.
/// </summary>
public class AnnotationServiceTests
{
    private readonly Mock<ILogger<AnnotationService>> _mockLogger;
    private readonly AnnotationService _service;

    public AnnotationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AnnotationService>>();
        _service = new AnnotationService(_mockLogger.Object);
    }

    [Fact]
    public async Task SaveAnnotationsAsync_ThrowsForNullDocument()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SaveAnnotationsAsync(null!, "test.pdf"));
    }

    [Fact]
    public async Task SaveAnnotationsAsync_ThrowsForEmptyFilePath()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SaveAnnotationsAsync(document, ""));
    }

    [Fact]
    public async Task SaveAnnotationsAsync_ReturnsErrorForReadOnlyFile()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"readonly_test_{Guid.NewGuid()}.pdf");

        try
        {
            // Create a test file
            await File.WriteAllTextAsync(testPath, "%PDF-1.4\n%%EOF");

            // Make it read-only
            var fileInfo = new FileInfo(testPath);
            fileInfo.IsReadOnly = true;

            var mockHandle = new Mock<IDisposable>();
            var document = new PdfDocument
            {
                FilePath = testPath,
                PageCount = 1,
                Handle = mockHandle.Object,
                LoadedAt = DateTime.UtcNow,
                FileSizeBytes = 1000
            };

            // Act
            var result = await _service.SaveAnnotationsAsync(document, testPath);

            // Assert
            Assert.True(result.IsFailed);
            var error = result.Errors[0] as FluentPDF.Core.ErrorHandling.PdfError;
            Assert.NotNull(error);
            Assert.Equal("ANNOTATION_FILE_READONLY", error.ErrorCode);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testPath))
            {
                var fileInfo = new FileInfo(testPath);
                fileInfo.IsReadOnly = false;
                File.Delete(testPath);
            }
        }
    }

    [Fact]
    public async Task GetAnnotationsAsync_ThrowsForNullDocument()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.GetAnnotationsAsync(null!, 0));
    }

    [Fact]
    public async Task CreateAnnotationAsync_ThrowsForNullDocument()
    {
        // Arrange
        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(0, 0, 100, 100)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAnnotationAsync(null!, annotation));
    }

    [Fact]
    public async Task CreateAnnotationAsync_ThrowsForNullAnnotation()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAnnotationAsync(document, null!));
    }

    [Fact]
    public async Task UpdateAnnotationAsync_ThrowsForNullDocument()
    {
        // Arrange
        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(0, 0, 100, 100)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAnnotationAsync(null!, annotation));
    }

    [Fact]
    public async Task UpdateAnnotationAsync_ThrowsForNullAnnotation()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = 1,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAnnotationAsync(document, null!));
    }

    [Fact]
    public async Task DeleteAnnotationAsync_ThrowsForNullDocument()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.DeleteAnnotationAsync(null!, 0, 0));
    }

    [Fact]
    public void Constructor_ThrowsForNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnnotationService(null!));
    }
}
