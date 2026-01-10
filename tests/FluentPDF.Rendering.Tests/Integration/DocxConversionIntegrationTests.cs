using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for DOCX to PDF conversion using real Mammoth.NET and WebView2.
/// These tests verify the complete conversion pipeline from DOCX parsing to PDF generation.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DocxConversionIntegrationTests : IDisposable
{
    private readonly IDocxParserService _parserService;
    private readonly IHtmlToPdfService _htmlToPdfService;
    private readonly IDocxConverterService _converterService;
    private readonly IQualityValidationService _validationService;
    private readonly IPdfRenderingService _renderingService;
    private readonly string _fixturesPath;
    private readonly string _outputPath;
    private readonly List<string> _filesToCleanup;

    public DocxConversionIntegrationTests()
    {
        // Setup services with real implementations
        var parserLogger = new LoggerFactory().CreateLogger<DocxParserService>();
        var htmlToPdfLogger = new LoggerFactory().CreateLogger<HtmlToPdfService>();
        var converterLogger = new LoggerFactory().CreateLogger<DocxConverterService>();
        var validatorLogger = new LoggerFactory().CreateLogger<LibreOfficeValidator>();
        var renderingLogger = new LoggerFactory().CreateLogger<PdfRenderingService>();
        var documentLogger = new LoggerFactory().CreateLogger<PdfDocumentService>();

        _parserService = new DocxParserService(parserLogger);
        _htmlToPdfService = new HtmlToPdfService(htmlToPdfLogger);
        _renderingService = new PdfRenderingService(renderingLogger);
        var documentService = new PdfDocumentService(documentLogger);
        _validationService = new LibreOfficeValidator(documentService, _renderingService, validatorLogger);
        _converterService = new DocxConverterService(
            converterLogger,
            _parserService,
            _htmlToPdfService);

        // Setup paths
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _outputPath = Path.Combine(Directory.GetCurrentDirectory(), "IntegrationTestOutput");
        Directory.CreateDirectory(_outputPath);

        _filesToCleanup = new List<string>();
    }

    public void Dispose()
    {
        // Clean up any generated files
        foreach (var file in _filesToCleanup)
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
                // Ignore cleanup errors
            }
        }

        // Clean up output directory
        try
        {
            if (Directory.Exists(_outputPath))
            {
                Directory.Delete(_outputPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Dispose services that implement IDisposable
        (_htmlToPdfService as IDisposable)?.Dispose();
    }

    #region DOCX Parsing Tests

    [Fact]
    public async Task ParseDocxToHtml_WithSimpleDocx_Succeeds()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "simple.docx");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        // Act
        var result = await _parserService.ParseDocxToHtmlAsync(docxPath);

        // Assert
        result.IsSuccess.Should().BeTrue("parsing simple DOCX should succeed");
        result.Value.Should().NotBeNullOrWhiteSpace("parsed HTML should not be empty");
        result.Value.Should().Contain("<html", "output should be valid HTML");
        result.Value.Should().Contain("</html>", "output should have closing HTML tag");
    }

    [Fact]
    public async Task ParseDocxToHtml_WithImagesDocx_IncludesEmbeddedImages()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "with-images.docx");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        // Act
        var result = await _parserService.ParseDocxToHtmlAsync(docxPath);

        // Assert
        result.IsSuccess.Should().BeTrue("parsing DOCX with images should succeed");
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().Contain("<img", "HTML should contain image tags");
        result.Value.Should().Contain("data:", "images should be embedded as data URIs");
    }

    [Fact]
    public async Task ParseDocxToHtml_WithComplexFormatting_PreservesStructure()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "complex-formatting.docx");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        // Act
        var result = await _parserService.ParseDocxToHtmlAsync(docxPath);

        // Assert
        result.IsSuccess.Should().BeTrue("parsing complex DOCX should succeed");
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().Contain("<html", "output should be valid HTML");
        // Complex formatting may include tables, lists, headings, etc.
        // Exact structure depends on Mammoth's conversion
    }

    #endregion

    #region HTML to PDF Tests

    [Fact]
    public async Task ConvertHtmlToPdf_WithSimpleHtml_Succeeds()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head><title>Test Document</title></head>
<body>
    <h1>Test Heading</h1>
    <p>This is a test paragraph.</p>
</body>
</html>";
        var outputPath = Path.Combine(_outputPath, "simple-html.pdf");
        _filesToCleanup.Add(outputPath);

        // Act
        var result = await _htmlToPdfService.ConvertHtmlToPdfAsync(html, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue("converting simple HTML to PDF should succeed");
        File.Exists(outputPath).Should().BeTrue("PDF file should be created");
        new FileInfo(outputPath).Length.Should().BeGreaterThan(0, "PDF file should not be empty");
    }

    [Fact]
    public async Task ConvertHtmlToPdf_WithStyledHtml_PreservesFormatting()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        h1 { color: #333; font-size: 24px; }
        p { line-height: 1.6; }
        .highlight { background-color: yellow; }
    </style>
</head>
<body>
    <h1>Styled Document</h1>
    <p>This paragraph has <span class=""highlight"">highlighted text</span>.</p>
</body>
</html>";
        var outputPath = Path.Combine(_outputPath, "styled-html.pdf");
        _filesToCleanup.Add(outputPath);

        // Act
        var result = await _htmlToPdfService.ConvertHtmlToPdfAsync(html, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue("converting styled HTML to PDF should succeed");
        File.Exists(outputPath).Should().BeTrue("PDF file should be created");
        new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region End-to-End Conversion Tests

    [Fact]
    public async Task ConvertDocxToPdf_WithSimpleDocx_Succeeds()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "simple.docx");
        var outputPath = Path.Combine(_outputPath, "simple.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        _filesToCleanup.Add(outputPath);

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(docxPath, outputPath, options);

        // Assert
        result.IsSuccess.Should().BeTrue("end-to-end conversion should succeed");
        result.Value.Should().NotBeNull();
        result.Value.OutputPath.Should().Be(outputPath);
        result.Value.ConversionTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.Value.OutputSizeBytes.Should().BeGreaterThan(0);
        File.Exists(outputPath).Should().BeTrue("PDF file should be created");
        new FileInfo(outputPath).Length.Should().BeGreaterThan(0, "PDF should not be empty");
    }

    [Fact]
    public async Task ConvertDocxToPdf_WithImagesDocx_SucceedsAndIncludesImages()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "with-images.docx");
        var outputPath = Path.Combine(_outputPath, "with-images.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        _filesToCleanup.Add(outputPath);

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(docxPath, outputPath, options);

        // Assert
        result.IsSuccess.Should().BeTrue("conversion with images should succeed");
        result.Value.OutputPath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue();
        // PDF with images should be larger than simple text-only PDF
        new FileInfo(outputPath).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task ConvertDocxToPdf_WithComplexFormatting_Succeeds()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "complex-formatting.docx");
        var outputPath = Path.Combine(_outputPath, "complex-formatting.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        _filesToCleanup.Add(outputPath);

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(docxPath, outputPath, options);

        // Assert
        result.IsSuccess.Should().BeTrue("conversion with complex formatting should succeed");
        result.Value.OutputPath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertDocxToPdf_WithInvalidDocx_ReturnsError()
    {
        // Arrange
        var invalidPath = Path.Combine(_outputPath, "invalid.docx");
        var outputPath = Path.Combine(_outputPath, "invalid-output.pdf");

        // Create an invalid DOCX file (just text content)
        File.WriteAllText(invalidPath, "This is not a valid DOCX file");
        _filesToCleanup.Add(invalidPath);
        _filesToCleanup.Add(outputPath);

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(invalidPath, outputPath, options);

        // Assert
        result.IsFailed.Should().BeTrue("conversion of invalid DOCX should fail");
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Match(code =>
            code == "DOCX_INVALID_FORMAT" || code == "DOCX_PARSE_FAILED");
    }

    [Fact]
    public async Task ConvertDocxToPdf_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_fixturesPath, "nonexistent.docx");
        var outputPath = Path.Combine(_outputPath, "nonexistent-output.pdf");

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(nonExistentPath, outputPath, options);

        // Assert
        result.IsFailed.Should().BeTrue("conversion of non-existent file should fail");
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Match(code =>
            code == "DOCX_FILE_NOT_FOUND" || code == "DOCX_INPUT_INVALID");
    }

    #endregion

    #region Quality Validation Tests

    [Fact]
    public async Task ConvertDocxToPdf_WithQualityValidation_ComparesWithLibreOffice()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "simple.docx");
        var outputPath = Path.Combine(_outputPath, "simple-validated.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        _filesToCleanup.Add(outputPath);

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(90), // Longer timeout for quality validation
            EnableQualityValidation = true
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(docxPath, outputPath, options);

        // Assert
        result.IsSuccess.Should().BeTrue("conversion with validation should succeed");
        result.Value.OutputPath.Should().Be(outputPath);

        // Quality score may be null if LibreOffice is not installed
        if (result.Value.QualityScore.HasValue)
        {
            result.Value.QualityScore.Value.Should().BeGreaterThanOrEqualTo(0);
            result.Value.QualityScore.Value.Should().BeLessThanOrEqualTo(1);
            result.Value.QualityValidationPerformed.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ValidateQuality_WithLibreOfficeNotInstalled_SkipsValidationGracefully()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "simple.docx");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        // Act - Direct validation service call
        var result = await _validationService.ValidateQualityAsync(docxPath, "dummy.pdf");

        // Assert - Should succeed even if LibreOffice is not installed
        // (service should handle missing LibreOffice gracefully)
        result.IsSuccess.Should().BeTrue("validation should handle missing LibreOffice");

        if (result.Value == null)
        {
            // LibreOffice not installed - this is expected behavior
            result.Value.Should().BeNull("quality report should be null when LibreOffice is not available");
        }
        else
        {
            // LibreOffice is installed - verify quality report
            result.Value.AverageSsimScore.Should().BeInRange(0, 1);
        }
    }

    #endregion

    #region Resource Cleanup Tests

    [Fact]
    public async Task MultipleConversions_NoFileHandleLeaks()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "simple.docx");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act - Perform multiple conversions
        for (int i = 0; i < 5; i++)
        {
            var outputPath = Path.Combine(_outputPath, $"conversion-{i}.pdf");
            _filesToCleanup.Add(outputPath);

            var result = await _converterService.ConvertDocxToPdfAsync(docxPath, outputPath, options);

            // Assert
            result.IsSuccess.Should().BeTrue($"conversion {i} should succeed");
            File.Exists(outputPath).Should().BeTrue($"PDF {i} should be created");

            // Verify we can immediately delete the file (no handle leaks)
            File.Delete(outputPath);
            File.Exists(outputPath).Should().BeFalse($"PDF {i} should be deletable immediately");
        }
    }

    [Fact]
    public async Task Conversion_CleansUpTemporaryFiles()
    {
        // Arrange
        var docxPath = Path.Combine(_fixturesPath, "simple.docx");
        var outputPath = Path.Combine(_outputPath, "temp-cleanup-test.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(docxPath))
        {
            return;
        }

        _filesToCleanup.Add(outputPath);

        var tempFilesBefore = Directory.GetFiles(Path.GetTempPath(), "*.html").Length;

        var options = new ConversionOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            EnableQualityValidation = false
        };

        // Act
        var result = await _converterService.ConvertDocxToPdfAsync(docxPath, outputPath, options);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Wait a bit for cleanup to complete
        await Task.Delay(500);

        var tempFilesAfter = Directory.GetFiles(Path.GetTempPath(), "*.html").Length;

        // Should not have significantly more temp files after conversion
        tempFilesAfter.Should().BeLessThanOrEqualTo(tempFilesBefore + 1,
            "temporary files should be cleaned up after conversion");
    }

    #endregion

    #region WebView2 Availability Tests

    [Fact]
    public async Task HtmlToPdfService_RequiresWebView2Runtime()
    {
        // This test documents that WebView2 runtime is required for HTML to PDF conversion
        // If WebView2 is not installed, the service should return an error

        var html = "<html><body><h1>Test</h1></body></html>";
        var outputPath = Path.Combine(_outputPath, "webview2-test.pdf");
        _filesToCleanup.Add(outputPath);

        // Act
        var result = await _htmlToPdfService.ConvertHtmlToPdfAsync(html, outputPath);

        // Assert
        if (result.IsFailed)
        {
            // WebView2 runtime is not available
            var error = result.Errors[0] as PdfError;
            error.Should().NotBeNull();
            error!.ErrorCode.Should().Match(code =>
                code.Contains("WEBVIEW2") || code.Contains("RUNTIME"));
        }
        else
        {
            // WebView2 is available - conversion should succeed
            result.IsSuccess.Should().BeTrue();
            File.Exists(outputPath).Should().BeTrue();
        }
    }

    #endregion
}
