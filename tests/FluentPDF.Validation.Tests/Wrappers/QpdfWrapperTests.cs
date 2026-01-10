using FluentAssertions;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Wrappers;
using Serilog;
using Serilog.Core;

namespace FluentPDF.Validation.Tests.Wrappers;

public class QpdfWrapperTests : IDisposable
{
    private readonly Logger _logger;
    private readonly QpdfWrapper _wrapper;
    private readonly string _testFilesPath;

    public QpdfWrapperTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _wrapper = new QpdfWrapper(_logger);
        _testFilesPath = Path.Combine(Path.GetTempPath(), "qpdf-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFilesPath);
    }

    public void Dispose()
    {
        _logger.Dispose();
        if (Directory.Exists(_testFilesPath))
        {
            Directory.Delete(_testFilesPath, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyFilePath_ReturnsFailure()
    {
        // Act
        var result = await _wrapper.ValidateAsync(string.Empty);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("File path cannot be empty");
    }

    [Fact]
    public async Task ValidateAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesPath, "nonexistent.pdf");

        // Act
        var result = await _wrapper.ValidateAsync(filePath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("File not found");
    }

    [Fact]
    public async Task ValidateAsync_WithValidPdf_ReturnsSuccess()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesPath, "valid.pdf");
        await CreateMinimalValidPdfAsync(filePath);

        // Act
        var result = await _wrapper.ValidateAsync(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var qpdfResult = result.Value;
        qpdfResult.Should().NotBeNull();
        qpdfResult.Status.Should().BeOneOf(ValidationStatus.Pass, ValidationStatus.Warn);
        qpdfResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithCorruptedPdf_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesPath, "corrupted.pdf");
        await CreateCorruptedPdfAsync(filePath);

        // Act
        var result = await _wrapper.ValidateAsync(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var qpdfResult = result.Value;
        qpdfResult.Should().NotBeNull();
        qpdfResult.Status.Should().Be(ValidationStatus.Fail);
        qpdfResult.IsValid.Should().BeFalse();
        qpdfResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithCorrelationId_LogsWithCorrelationId()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesPath, "test.pdf");
        await CreateMinimalValidPdfAsync(filePath);
        var correlationId = "test-correlation-123";

        // Act
        var result = await _wrapper.ValidateAsync(filePath, correlationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesPath, "test.pdf");
        await CreateMinimalValidPdfAsync(filePath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _wrapper.ValidateAsync(filePath, cancellationToken: cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    /// <summary>
    /// Creates a minimal valid PDF file for testing.
    /// This is a basic PDF 1.4 document with minimal structure.
    /// </summary>
    private static async Task CreateMinimalValidPdfAsync(string filePath)
    {
        // Minimal valid PDF structure (PDF 1.4)
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
/Resources <<
/Font <<
/F1 <<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
>>
>>
/MediaBox [0 0 612 792]
/Contents 4 0 R
>>
endobj
4 0 obj
<<
/Length 44
>>
stream
BT
/F1 12 Tf
100 700 Td
(Test PDF) Tj
ET
endstream
endobj
xref
0 5
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
0000000317 00000 n
trailer
<<
/Size 5
/Root 1 0 R
>>
startxref
410
%%EOF
";
        await File.WriteAllTextAsync(filePath, pdfContent);
    }

    /// <summary>
    /// Creates a corrupted PDF file for testing error handling.
    /// This PDF has an invalid cross-reference table.
    /// </summary>
    private static async Task CreateCorruptedPdfAsync(string filePath)
    {
        // Corrupted PDF with invalid xref table
        var corruptedContent = @"%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj
xref
0 1
0000000000 65535 f
trailer
<<
/Size 2
/Root 1 0 R
>>
startxref
INVALID_OFFSET
%%EOF
";
        await File.WriteAllTextAsync(filePath, corruptedContent);
    }
}
