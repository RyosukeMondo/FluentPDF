using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentResults;
using Xunit;

namespace FluentPDF.Core.Tests.ErrorHandling;

/// <summary>
/// Tests for PdfError class to verify error creation, metadata population, and context handling.
/// </summary>
public class PdfErrorTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        const string errorCode = "PDF_LOAD_FILE_NOT_FOUND";
        const string message = "The specified PDF file was not found";
        const ErrorCategory category = ErrorCategory.IO;
        const ErrorSeverity severity = ErrorSeverity.Error;

        // Act
        var error = new PdfError(errorCode, message, category, severity);

        // Assert
        error.ErrorCode.Should().Be(errorCode);
        error.Message.Should().Be(message);
        error.Category.Should().Be(category);
        error.Severity.Should().Be(severity);
        error.Context.Should().NotBeNull();
        error.Context.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldPopulateMetadataForAIAnalysis()
    {
        // Arrange
        const string errorCode = "PDF_RENDER_MEMORY_EXHAUSTED";
        const string message = "Insufficient memory to render PDF";
        const ErrorCategory category = ErrorCategory.Rendering;
        const ErrorSeverity severity = ErrorSeverity.Critical;

        // Act
        var error = new PdfError(errorCode, message, category, severity);

        // Assert
        error.Metadata.Should().ContainKey("ErrorCode");
        error.Metadata["ErrorCode"].Should().Be(errorCode);
        error.Metadata.Should().ContainKey("Category");
        error.Metadata["Category"].Should().Be("Rendering");
        error.Metadata.Should().ContainKey("Severity");
        error.Metadata["Severity"].Should().Be("Critical");
    }

    [Fact]
    public void WithContext_ShouldAddContextEntry()
    {
        // Arrange
        var error = new PdfError(
            "PDF_LOAD_INVALID_FORMAT",
            "Invalid PDF format",
            ErrorCategory.Validation,
            ErrorSeverity.Error);

        // Act
        error.WithContext("FilePath", "/path/to/file.pdf");

        // Assert
        error.Context.Should().ContainKey("FilePath");
        error.Context["FilePath"].Should().Be("/path/to/file.pdf");
    }

    [Fact]
    public void WithContext_ShouldAddToMetadata()
    {
        // Arrange
        var error = new PdfError(
            "PDF_RENDER_PAGE_FAILED",
            "Failed to render page",
            ErrorCategory.Rendering,
            ErrorSeverity.Error);

        // Act
        error.WithContext("PageNumber", 5);

        // Assert
        error.Metadata.Should().ContainKey("Context.PageNumber");
        error.Metadata["Context.PageNumber"].Should().Be(5);
    }

    [Fact]
    public void WithContext_ShouldSupportFluentChaining()
    {
        // Arrange
        var error = new PdfError(
            "PDF_CONVERSION_FAILED",
            "PDF conversion failed",
            ErrorCategory.Conversion,
            ErrorSeverity.Error);

        // Act
        var result = error
            .WithContext("SourceFormat", "PDF")
            .WithContext("TargetFormat", "PNG")
            .WithContext("PageCount", 10);

        // Assert
        result.Should().BeSameAs(error);
        error.Context.Should().HaveCount(3);
        error.Context["SourceFormat"].Should().Be("PDF");
        error.Context["TargetFormat"].Should().Be("PNG");
        error.Context["PageCount"].Should().Be(10);
    }

    [Fact]
    public void PdfError_ShouldWorkWithFluentResults()
    {
        // Arrange
        var error = new PdfError(
            "PDF_SECURITY_ACCESS_DENIED",
            "Access denied: encrypted PDF",
            ErrorCategory.Security,
            ErrorSeverity.Warning);

        // Act
        var result = Result.Fail(error);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<PdfError>();
        var pdfError = (PdfError)result.Errors[0];
        pdfError.ErrorCode.Should().Be("PDF_SECURITY_ACCESS_DENIED");
    }

    [Fact]
    public void PdfError_ShouldSupportErrorChaining()
    {
        // Arrange
        var innerError = new PdfError(
            "PDF_PDFIUM_INIT_FAILED",
            "Failed to initialize PDFium",
            ErrorCategory.System,
            ErrorSeverity.Critical);

        var outerError = new PdfError(
            "PDF_RENDER_ENGINE_UNAVAILABLE",
            "Rendering engine unavailable",
            ErrorCategory.Rendering,
            ErrorSeverity.Critical);

        // Act
        outerError.CausedBy(innerError);

        // Assert
        outerError.Reasons.Should().HaveCount(1);
        outerError.Reasons[0].Should().BeSameAs(innerError);
    }

    [Fact]
    public void PdfError_ShouldAllowMultipleContextEntries()
    {
        // Arrange
        var error = new PdfError(
            "PDF_LOAD_CORRUPTED",
            "Corrupted PDF structure",
            ErrorCategory.Validation,
            ErrorSeverity.Error);

        // Act
        error.WithContext("FilePath", "/docs/report.pdf")
             .WithContext("FileSize", 1024000)
             .WithContext("Offset", 512)
             .WithContext("ExpectedMagicBytes", "PDF-1.7")
             .WithContext("ActualBytes", "JFIF");

        // Assert
        error.Context.Should().HaveCount(5);
        error.Metadata.Keys.Where(k => k.StartsWith("Context.")).Should().HaveCount(5);
    }

    [Fact]
    public void AllErrorCategories_ShouldBeRepresented()
    {
        // This test ensures all error categories can be used
        var categories = new[]
        {
            ErrorCategory.Validation,
            ErrorCategory.System,
            ErrorCategory.Security,
            ErrorCategory.IO,
            ErrorCategory.Rendering,
            ErrorCategory.Conversion
        };

        foreach (var category in categories)
        {
            // Act
            var error = new PdfError("TEST_CODE", "Test message", category, ErrorSeverity.Info);

            // Assert
            error.Category.Should().Be(category);
            error.Metadata["Category"].Should().Be(category.ToString());
        }
    }

    [Fact]
    public void AllErrorSeverities_ShouldBeRepresented()
    {
        // This test ensures all severity levels can be used
        var severities = new[]
        {
            ErrorSeverity.Critical,
            ErrorSeverity.Error,
            ErrorSeverity.Warning,
            ErrorSeverity.Info
        };

        foreach (var severity in severities)
        {
            // Act
            var error = new PdfError("TEST_CODE", "Test message", ErrorCategory.System, severity);

            // Assert
            error.Severity.Should().Be(severity);
            error.Metadata["Severity"].Should().Be(severity.ToString());
        }
    }
}
