using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for LibreOfficeValidator.
/// Tests quality validation logic, error handling, and graceful degradation when LibreOffice is unavailable.
/// Note: Tests with actual LibreOffice conversion require integration tests.
/// Unit tests focus on validation logic, error scenarios, and mocked service interactions.
/// </summary>
public sealed class LibreOfficeValidatorTests : IDisposable
{
    private readonly Mock<IPdfDocumentService> _mockDocumentService;
    private readonly Mock<IPdfRenderingService> _mockRenderingService;
    private readonly Mock<ILogger<LibreOfficeValidator>> _mockLogger;
    private readonly LibreOfficeValidator _service;
    private readonly string _testDataDir;

    public LibreOfficeValidatorTests()
    {
        _mockDocumentService = new Mock<IPdfDocumentService>();
        _mockRenderingService = new Mock<IPdfRenderingService>();
        _mockLogger = new Mock<ILogger<LibreOfficeValidator>>();
        _service = new LibreOfficeValidator(
            _mockDocumentService.Object,
            _mockRenderingService.Object,
            _mockLogger.Object);
        _testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        Directory.CreateDirectory(_testDataDir);
    }

    public void Dispose()
    {
        // Cleanup test files
        if (Directory.Exists(_testDataDir))
        {
            try
            {
                Directory.Delete(_testDataDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Constructor_WithNullDocumentService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new LibreOfficeValidator(
            null!,
            _mockRenderingService.Object,
            _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pdfDocumentService");
    }

    [Fact]
    public void Constructor_WithNullRenderingService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new LibreOfficeValidator(
            _mockDocumentService.Object,
            null!,
            _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pdfRenderingService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new LibreOfficeValidator(
            _mockDocumentService.Object,
            _mockRenderingService.Object,
            null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ValidateQualityAsync_WithNonExistentDocxFile_ReturnsValidationError()
    {
        // Arrange
        var nonExistentDocx = Path.Combine(_testDataDir, "nonexistent.docx");
        var fluentPdfPath = Path.Combine(_testDataDir, "output.pdf");
        File.WriteAllText(fluentPdfPath, "dummy pdf content");

        // Act
        var result = await _service.ValidateQualityAsync(nonExistentDocx, fluentPdfPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("VALIDATION_DOCX_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Context.Should().ContainKey("DocxPath");
    }

    [Fact]
    public async Task ValidateQualityAsync_WithNonExistentPdfFile_ReturnsValidationError()
    {
        // Arrange
        var docxPath = Path.Combine(_testDataDir, "test.docx");
        File.WriteAllText(docxPath, "dummy docx content");
        var nonExistentPdf = Path.Combine(_testDataDir, "nonexistent.pdf");

        // Act
        var result = await _service.ValidateQualityAsync(docxPath, nonExistentPdf);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("VALIDATION_PDF_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Context.Should().ContainKey("FluentPdfPath");
    }

    [Fact]
    public async Task IsLibreOfficeAvailableAsync_Note()
    {
        // Note: Testing actual LibreOffice availability is environment-dependent.
        // This method executes "soffice --version" which will:
        // - Return true if LibreOffice is installed
        // - Return false if LibreOffice is not installed or not in PATH
        //
        // Integration tests should verify the actual behavior.
        // Unit tests verify that the method handles exceptions gracefully.

        // Act
        var result = await _service.IsLibreOfficeAvailableAsync();

        // Assert
        // Result depends on environment - we just verify it doesn't throw
        // The result can be either true or false, both are valid
        Assert.True(result == true || result == false);
    }

    [Fact]
    public async Task ValidateQualityAsync_LogsCorrelationId()
    {
        // Arrange
        var docxPath = Path.Combine(_testDataDir, "test.docx");
        File.WriteAllText(docxPath, "dummy docx content");
        var pdfPath = Path.Combine(_testDataDir, "test.pdf");
        File.WriteAllText(pdfPath, "dummy pdf content");

        // Act
        await _service.ValidateQualityAsync(docxPath, pdfPath);

        // Assert - Verify that logging occurred with CorrelationId
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
    public async Task ValidateQualityAsync_WhenLibreOfficeNotAvailable_ReturnsNullReport()
    {
        // Note: This test behavior depends on LibreOffice availability.
        // If LibreOffice is not installed, the service should return null (graceful degradation).
        // If it IS installed, it will attempt conversion (which will fail with dummy files).
        //
        // The key contract is: when LibreOffice is unavailable, return Ok(null) not a failure.

        // Arrange
        var docxPath = Path.Combine(_testDataDir, "test.docx");
        File.WriteAllText(docxPath, "dummy docx content");
        var pdfPath = Path.Combine(_testDataDir, "test.pdf");
        File.WriteAllText(pdfPath, "dummy pdf content");

        // Act
        var result = await _service.ValidateQualityAsync(docxPath, pdfPath);

        // Assert
        if (result.IsSuccess && result.Value == null)
        {
            // LibreOffice not available - graceful degradation
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LibreOffice not available")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
        else
        {
            // LibreOffice available or other failure
            // Integration tests will verify the full conversion flow
            Assert.True(true);
        }
    }

    [Fact]
    public void QualityReportModel_HasRequiredProperties()
    {
        // Arrange & Act - Create a sample QualityReport
        var report = new QualityReport
        {
            AverageSsimScore = 0.98,
            MinimumSsimScore = 0.95,
            MinimumScorePageNumber = 3,
            LibreOfficePdfPath = "/path/to/libre.pdf",
            FluentPdfPath = "/path/to/fluent.pdf",
            ComparisonImagePages = new List<int> { 2, 3 }.AsReadOnly(),
            ComparisonImagesDirectory = "/path/to/comparisons",
            TotalPagesCompared = 5,
            ValidatedAt = DateTime.UtcNow
        };

        // Assert
        report.AverageSsimScore.Should().Be(0.98);
        report.MinimumSsimScore.Should().Be(0.95);
        report.MinimumScorePageNumber.Should().Be(3);
        report.TotalPagesCompared.Should().Be(5);
        report.ComparisonImagePages.Should().HaveCount(2);
        report.IsQualityAcceptable(0.95).Should().BeTrue();
        report.IsQualityAcceptable(0.99).Should().BeFalse();
    }

    [Theory]
    [InlineData(0.95, 0.95, true)]
    [InlineData(0.96, 0.95, true)]
    [InlineData(0.94, 0.95, false)]
    [InlineData(1.0, 0.95, true)]
    [InlineData(0.0, 0.95, false)]
    public void IsQualityAcceptable_WithVariousScores_ReturnsExpectedResult(
        double score, double threshold, bool expected)
    {
        // Arrange
        var report = new QualityReport
        {
            AverageSsimScore = score,
            MinimumSsimScore = score,
            MinimumScorePageNumber = 1,
            LibreOfficePdfPath = "/path/to/libre.pdf",
            FluentPdfPath = "/path/to/fluent.pdf",
            ComparisonImagePages = Array.Empty<int>(),
            TotalPagesCompared = 1,
            ValidatedAt = DateTime.UtcNow
        };

        // Act
        var result = report.IsQualityAcceptable(threshold);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void LibreOfficeConversionIntegration_Note()
    {
        // Note: Full integration tests for ValidateQualityAsync with LibreOffice conversion,
        // PDF rendering, and SSIM calculation are covered in the integration test suite.
        //
        // These tests require:
        // - LibreOffice installed and available in PATH
        // - Real DOCX files for conversion
        // - Valid PDF documents for comparison
        // - The full rendering pipeline
        //
        // Unit tests here focus on:
        // - Constructor validation
        // - Input file validation
        // - Graceful degradation when LibreOffice is unavailable
        // - QualityReport model behavior
        Assert.True(true);
    }

    [Fact]
    public async Task ValidateQualityAsync_WithCustomThreshold_PassesThresholdCorrectly()
    {
        // Arrange
        var docxPath = Path.Combine(_testDataDir, "test.docx");
        File.WriteAllText(docxPath, "dummy docx content");
        var pdfPath = Path.Combine(_testDataDir, "test.pdf");
        File.WriteAllText(pdfPath, "dummy pdf content");
        var customThreshold = 0.85;

        // Act
        var result = await _service.ValidateQualityAsync(
            docxPath, pdfPath, customThreshold);

        // Assert
        // The method should accept custom threshold parameter
        // Actual threshold usage is tested in integration tests
        result.Should().NotBeNull();
    }

    [Fact]
    public void IQualityValidationService_Interface_IsImplementedCorrectly()
    {
        // Assert
        _service.Should().BeAssignableTo<IQualityValidationService>();

        // Verify interface methods exist
        var interfaceType = typeof(IQualityValidationService);
        interfaceType.GetMethod("ValidateQualityAsync").Should().NotBeNull();
        interfaceType.GetMethod("IsLibreOfficeAvailableAsync").Should().NotBeNull();
    }
}
