using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for PageOperationsService.
/// Tests focus on validation logic and error handling.
/// </summary>
public class PageOperationsServiceTests : IDisposable
{
    private readonly Mock<ILogger<PageOperationsService>> _mockLogger;
    private readonly IPageOperationsService _service;
    private readonly string _testFilePath;
    private bool _disposed;

    public PageOperationsServiceTests()
    {
        _mockLogger = new Mock<ILogger<PageOperationsService>>();
        _service = new PageOperationsService(_mockLogger.Object);

        // Create a minimal valid PDF for testing
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");
        File.WriteAllText(_testFilePath, "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj 2 0 obj<</Type/Pages/Count 0/Kids[]>>endobj\nxref\n0 3\n0000000000 65535 f\n0000000009 00000 n\n0000000058 00000 n\ntrailer<</Size 3/Root 1 0 R>>\nstartxref\n110\n%%EOF");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsForNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PageOperationsService(null!));
    }

    #endregion

    #region RotatePagesAsync Tests

    [Fact]
    public async Task RotatePagesAsync_FailsForNullDocument()
    {
        // Act
        var result = await _service.RotatePagesAsync(null!, new[] { 0 }, RotationAngle.Rotate90);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("null", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RotatePagesAsync_FailsForNullPageIndices()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.RotatePagesAsync(document, null!, RotationAngle.Rotate90);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No pages specified", result.Errors[0].Message);
    }

    [Fact]
    public async Task RotatePagesAsync_FailsForEmptyPageIndices()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.RotatePagesAsync(document, Array.Empty<int>(), RotationAngle.Rotate90);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No pages specified", result.Errors[0].Message);
    }

    [Fact]
    public async Task RotatePagesAsync_FailsForEmptyFilePath()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "",
            PageCount = 1,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.RotatePagesAsync(document, new[] { 0 }, RotationAngle.Rotate90);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("file path", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RotatePagesAsync_FailsForNonexistentFile()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "/nonexistent/file.pdf",
            PageCount = 1,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.RotatePagesAsync(document, new[] { 0 }, RotationAngle.Rotate90);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(RotationAngle.Rotate90)]
    [InlineData(RotationAngle.Rotate180)]
    [InlineData(RotationAngle.Rotate270)]
    public async Task RotatePagesAsync_FailsForInvalidPdf(RotationAngle angle)
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.RotatePagesAsync(document, new[] { 0 }, angle);

        // Assert - should fail because minimal PDF doesn't have actual pages
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task RotatePagesAsync_HandlesCancellation()
    {
        // Arrange
        var document = CreateTestDocument();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.RotatePagesAsync(document, new[] { 0 }, RotationAngle.Rotate90, cts.Token);

        // Assert
        Assert.True(result.IsFailed);
    }

    #endregion

    #region DeletePagesAsync Tests

    [Fact]
    public async Task DeletePagesAsync_FailsForNullDocument()
    {
        // Act
        var result = await _service.DeletePagesAsync(null!, new[] { 0 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("null", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeletePagesAsync_FailsForNullPageIndices()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.DeletePagesAsync(document, null!);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No pages specified", result.Errors[0].Message);
    }

    [Fact]
    public async Task DeletePagesAsync_FailsForEmptyPageIndices()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.DeletePagesAsync(document, Array.Empty<int>());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No pages specified", result.Errors[0].Message);
    }

    [Fact]
    public async Task DeletePagesAsync_FailsForEmptyFilePath()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "",
            PageCount = 1,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.DeletePagesAsync(document, new[] { 0 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("file path", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeletePagesAsync_FailsForNonexistentFile()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "/nonexistent/file.pdf",
            PageCount = 2,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.DeletePagesAsync(document, new[] { 0 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeletePagesAsync_FailsForInvalidPdf()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.DeletePagesAsync(document, new[] { 0 });

        // Assert - should fail because minimal PDF doesn't have actual pages
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task DeletePagesAsync_HandlesCancellation()
    {
        // Arrange
        var document = CreateTestDocument();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.DeletePagesAsync(document, new[] { 0 }, cts.Token);

        // Assert
        Assert.True(result.IsFailed);
    }

    #endregion

    #region ReorderPagesAsync Tests

    [Fact]
    public async Task ReorderPagesAsync_FailsForNullDocument()
    {
        // Act
        var result = await _service.ReorderPagesAsync(null!, new[] { 0 }, 1);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("null", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReorderPagesAsync_FailsForNullPageIndices()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.ReorderPagesAsync(document, null!, 0);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No pages specified", result.Errors[0].Message);
    }

    [Fact]
    public async Task ReorderPagesAsync_FailsForEmptyPageIndices()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.ReorderPagesAsync(document, Array.Empty<int>(), 0);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No pages specified", result.Errors[0].Message);
    }

    [Fact]
    public async Task ReorderPagesAsync_FailsForEmptyFilePath()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "",
            PageCount = 2,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.ReorderPagesAsync(document, new[] { 0 }, 1);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("file path", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReorderPagesAsync_FailsForNonexistentFile()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "/nonexistent/file.pdf",
            PageCount = 2,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.ReorderPagesAsync(document, new[] { 0 }, 1);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReorderPagesAsync_FailsForInvalidPdf()
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.ReorderPagesAsync(document, new[] { 0 }, 1);

        // Assert - should fail because minimal PDF doesn't have actual pages
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task ReorderPagesAsync_HandlesCancellation()
    {
        // Arrange
        var document = CreateTestDocument();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.ReorderPagesAsync(document, new[] { 0 }, 1, cts.Token);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task ReorderPagesAsync_FailsForInvalidTargetIndex(int targetIndex)
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.ReorderPagesAsync(document, new[] { 0 }, targetIndex);

        // Assert - should fail for out of range target index
        Assert.True(result.IsFailed);
    }

    #endregion

    #region InsertBlankPageAsync Tests

    [Fact]
    public async Task InsertBlankPageAsync_FailsForNullDocument()
    {
        // Act
        var result = await _service.InsertBlankPageAsync(null!, 0, PageSize.Letter);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("null", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InsertBlankPageAsync_FailsForEmptyFilePath()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "",
            PageCount = 1,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.InsertBlankPageAsync(document, 0, PageSize.Letter);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("file path", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InsertBlankPageAsync_FailsForNonexistentFile()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "/nonexistent/file.pdf",
            PageCount = 1,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };

        // Act
        var result = await _service.InsertBlankPageAsync(document, 0, PageSize.A4);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(PageSize.Letter)]
    [InlineData(PageSize.A4)]
    [InlineData(PageSize.Legal)]
    [InlineData(PageSize.SameAsCurrent)]
    public async Task InsertBlankPageAsync_FailsForInvalidPdf(PageSize pageSize)
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.InsertBlankPageAsync(document, 0, pageSize);

        // Assert - should fail because minimal PDF doesn't have actual pages
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task InsertBlankPageAsync_HandlesCancellation()
    {
        // Arrange
        var document = CreateTestDocument();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.InsertBlankPageAsync(document, 0, PageSize.Letter, cts.Token);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task InsertBlankPageAsync_FailsForInvalidInsertIndex(int insertIndex)
    {
        // Arrange
        var document = CreateTestDocument();

        // Act
        var result = await _service.InsertBlankPageAsync(document, insertIndex, PageSize.Letter);

        // Assert - should fail for out of range insert index
        Assert.True(result.IsFailed);
    }

    #endregion

    #region Helper Methods

    private PdfDocument CreateTestDocument()
    {
        return new PdfDocument
        {
            FilePath = _testFilePath,
            PageCount = 2,
            Handle = new Mock<IDisposable>().Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1000
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Cleanup test file
        if (File.Exists(_testFilePath))
        {
            try
            {
                File.Delete(_testFilePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _disposed = true;
    }

    #endregion
}
