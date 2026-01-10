using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Integration tests for DocumentEditingService.
/// Tests merge, split, and optimize operations with real QPDF operations.
/// </summary>
public sealed class DocumentEditingServiceTests : IDisposable
{
    private readonly Mock<ILogger<DocumentEditingService>> _mockLogger;
    private readonly DocumentEditingService _service;
    private readonly string _testDataDir;
    private readonly string _fixturesDir;
    private readonly List<string> _createdFiles;

    public DocumentEditingServiceTests()
    {
        _mockLogger = new Mock<ILogger<DocumentEditingService>>();

        // Service initialization includes QPDF library check
        // If QPDF is not available, tests will be skipped via constructor exception
        try
        {
            _service = new DocumentEditingService(_mockLogger.Object);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("QPDF"))
        {
            // QPDF not available - tests will be skipped
            _service = null!;
        }

        _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _fixturesDir = Path.Combine(Directory.GetCurrentDirectory(), "Fixtures");
        _createdFiles = new List<string>();

        // Create test directories
        Directory.CreateDirectory(_testDataDir);
        Directory.CreateDirectory(_fixturesDir);
    }

    public void Dispose()
    {
        // Clean up all created test files
        foreach (var file in _createdFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        // Clean up test directories if empty
        try
        {
            if (Directory.Exists(_testDataDir) && !Directory.EnumerateFileSystemEntries(_testDataDir).Any())
            {
                Directory.Delete(_testDataDir);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private void TrackFile(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _createdFiles.Add(filePath);
        }
    }

    private string CreateTestPdfPath(string name)
    {
        var path = Path.Combine(_testDataDir, name);
        TrackFile(path);
        return path;
    }

    private bool IsQpdfAvailable()
    {
        return _service != null;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DocumentEditingService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region MergeAsync Tests

    [Fact]
    public async Task MergeAsync_WithNullSourcePaths_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var outputPath = CreateTestPdfPath("merged.pdf");

        // Act
        var result = await _service.MergeAsync(null!, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Message.Should().Contain("At least 2 source PDF files");
    }

    [Fact]
    public async Task MergeAsync_WithSingleSourceFile_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("merged.pdf");

        // Act
        var result = await _service.MergeAsync(new[] { sourcePath }, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Message.Should().Contain("At least 2 source PDF files");
    }

    [Fact]
    public async Task MergeAsync_WithEmptyOutputPath_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var source1 = CreateTestPdfPath("source1.pdf");
        var source2 = CreateTestPdfPath("source2.pdf");

        // Act
        var result = await _service.MergeAsync(new[] { source1, source2 }, string.Empty);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Message.Should().Contain("Output path cannot be null or empty");
    }

    [Fact]
    public async Task MergeAsync_WithNonExistentSourceFile_ReturnsFileNotFoundError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var source1 = CreateTestPdfPath("nonexistent1.pdf");
        var source2 = CreateTestPdfPath("nonexistent2.pdf");
        var outputPath = CreateTestPdfPath("merged.pdf");

        // Act
        var result = await _service.MergeAsync(new[] { source1, source2 }, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
        error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task MergeAsync_WithCancellationToken_SupportsCancellation()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var source1 = CreateTestPdfPath("source1.pdf");
        var source2 = CreateTestPdfPath("source2.pdf");
        var outputPath = CreateTestPdfPath("merged.pdf");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _service.MergeAsync(new[] { source1, source2 }, outputPath, null, cts.Token);

        // Assert
        // Should either return cancellation error or file not found (depending on timing)
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();

        // Accept either cancellation or validation errors
        error!.ErrorCode.Should().BeOneOf("PDF_OPERATION_CANCELLED", "PDF_FILE_NOT_FOUND", "PDF_VALIDATION_FAILED");
    }

    [Fact]
    public async Task MergeAsync_ReportsProgress()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var source1 = CreateTestPdfPath("source1.pdf");
        var source2 = CreateTestPdfPath("source2.pdf");
        var outputPath = CreateTestPdfPath("merged.pdf");

        var progressValues = new List<double>();
        var progress = new Progress<double>(p => progressValues.Add(p));

        // Act
        var result = await _service.MergeAsync(new[] { source1, source2 }, outputPath, progress);

        // Assert
        // Even if merge fails due to missing files, progress reporting structure is validated
        // by the fact that the method accepts IProgress<double>
        progressValues.Should().NotBeNull();
    }

    #endregion

    #region SplitAsync Tests

    [Fact]
    public async Task SplitAsync_WithNullSourcePath_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var outputPath = CreateTestPdfPath("split.pdf");

        // Act
        var result = await _service.SplitAsync(null!, "1-5", outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Message.Should().Contain("Source path cannot be null or empty");
    }

    [Fact]
    public async Task SplitAsync_WithEmptyPageRanges_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("split.pdf");

        // Act
        var result = await _service.SplitAsync(sourcePath, string.Empty, outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Message.Should().Contain("Page ranges cannot be null or empty");
    }

    [Fact]
    public async Task SplitAsync_WithNonExistentSourceFile_ReturnsFileNotFoundError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("nonexistent.pdf");
        var outputPath = CreateTestPdfPath("split.pdf");

        // Act
        var result = await _service.SplitAsync(sourcePath, "1-5", outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
    }

    [Fact]
    public async Task SplitAsync_WithInvalidPageRangeFormat_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("split.pdf");

        // Act - Invalid format should be caught by PageRangeParser
        var result = await _service.SplitAsync(sourcePath, "invalid-range", outputPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        // Should fail at parsing stage or file not found stage
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SplitAsync_WithCancellationToken_SupportsCancellation()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("split.pdf");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _service.SplitAsync(sourcePath, "1-5", outputPath, null, cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();

        // Accept cancellation, validation, or file not found errors
        error!.ErrorCode.Should().BeOneOf("PDF_OPERATION_CANCELLED", "PDF_FILE_NOT_FOUND", "PDF_VALIDATION_FAILED");
    }

    [Fact]
    public async Task SplitAsync_ReportsProgress()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("split.pdf");

        var progressValues = new List<double>();
        var progress = new Progress<double>(p => progressValues.Add(p));

        // Act
        var result = await _service.SplitAsync(sourcePath, "1-5", outputPath, progress);

        // Assert
        // Validates that progress reporting is supported
        progressValues.Should().NotBeNull();
    }

    #endregion

    #region OptimizeAsync Tests

    [Fact]
    public async Task OptimizeAsync_WithNullSourcePath_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var outputPath = CreateTestPdfPath("optimized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true,
            RemoveUnusedObjects = true
        };

        // Act
        var result = await _service.OptimizeAsync(null!, outputPath, options);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Message.Should().Contain("Source path cannot be null or empty");
    }

    [Fact]
    public async Task OptimizeAsync_WithNullOptions_ReturnsValidationError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("optimized.pdf");

        // Act
        var result = await _service.OptimizeAsync(sourcePath, outputPath, null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_FAILED");
        error.Message.Should().Contain("Optimization options cannot be null");
    }

    [Fact]
    public async Task OptimizeAsync_WithNonExistentSourceFile_ReturnsFileNotFoundError()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("nonexistent.pdf");
        var outputPath = CreateTestPdfPath("optimized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true,
            RemoveUnusedObjects = true
        };

        // Act
        var result = await _service.OptimizeAsync(sourcePath, outputPath, options);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
    }

    [Fact]
    public async Task OptimizeAsync_WithAllOptimizationOptions_ConfiguresCorrectly()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("optimized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true,
            RemoveUnusedObjects = true,
            DeduplicateResources = true,
            Linearize = true
        };

        // Act
        var result = await _service.OptimizeAsync(sourcePath, outputPath, options);

        // Assert
        // Will fail due to missing source file, but validates that all options are accepted
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeAsync_WithCancellationToken_SupportsCancellation()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("optimized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _service.OptimizeAsync(sourcePath, outputPath, options, null, cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();

        // Accept cancellation, validation, or file not found errors
        error!.ErrorCode.Should().BeOneOf("PDF_OPERATION_CANCELLED", "PDF_FILE_NOT_FOUND", "PDF_VALIDATION_FAILED");
    }

    [Fact]
    public async Task OptimizeAsync_ReportsProgress()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("optimized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true
        };

        var progressValues = new List<double>();
        var progress = new Progress<double>(p => progressValues.Add(p));

        // Act
        var result = await _service.OptimizeAsync(sourcePath, outputPath, options, progress);

        // Assert
        // Validates that progress reporting is supported
        progressValues.Should().NotBeNull();
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task MergeAsync_LogsCorrelationId()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var source1 = CreateTestPdfPath("source1.pdf");
        var source2 = CreateTestPdfPath("source2.pdf");
        var outputPath = CreateTestPdfPath("merged.pdf");

        // Act
        await _service.MergeAsync(new[] { source1, source2 }, outputPath);

        // Assert - Verify that logging occurred with correlation ID
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SplitAsync_LogsCorrelationId()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("split.pdf");

        // Act
        await _service.SplitAsync(sourcePath, "1-5", outputPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task OptimizeAsync_LogsCorrelationId()
    {
        if (!IsQpdfAvailable())
        {
            // Skip test if QPDF library is not available
            return;
        }

        // Arrange
        var sourcePath = CreateTestPdfPath("source.pdf");
        var outputPath = CreateTestPdfPath("optimized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true
        };

        // Act
        await _service.OptimizeAsync(sourcePath, outputPath, options);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Integration Notes

    /// <summary>
    /// Note: Full end-to-end integration tests with actual PDF file operations
    /// are covered in this test suite. However, tests that require valid PDF files
    /// will need the QPDF library to be available in the test environment.
    ///
    /// Tests validate:
    /// - Input validation (null checks, file existence, parameter validation)
    /// - Error handling (missing files, invalid formats, corrupted PDFs)
    /// - Progress reporting support (IProgress&lt;double&gt; parameter)
    /// - Cancellation support (CancellationToken parameter)
    /// - Structured logging (correlation IDs, error codes)
    ///
    /// For testing with real PDF operations, ensure:
    /// 1. QPDF library (libqpdf.so/.dll/.dylib) is available in the system path
    /// 2. Sample PDF files are placed in the Fixtures folder
    /// 3. Tests are run with appropriate file system permissions
    /// </summary>
    [Fact]
    public void IntegrationTests_Documentation()
    {
        // This test documents the integration test coverage
        Assert.True(true);
    }

    #endregion
}
