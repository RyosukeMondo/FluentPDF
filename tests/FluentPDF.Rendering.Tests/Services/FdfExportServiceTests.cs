using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for FdfExportService.
/// Verifies FDF XML generation and export functionality.
/// </summary>
public sealed class FdfExportServiceTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IPdfFormService> _mockFormService;
    private readonly FdfExportService _sut;

    public FdfExportServiceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockFormService = new Mock<IPdfFormService>();
        _sut = new FdfExportService(_mockLogger.Object);
    }

    [Fact]
    public async Task ExportAsync_WithNoFields_ReturnsFailure()
    {
        // Arrange
        var document = CreateMockDocument();
        var outputPath = Path.GetTempFileName();

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, It.IsAny<int>()))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(Array.Empty<PdfFormField>()));

        // Act
        var result = await _sut.ExportAsync(document, outputPath, _mockFormService.Object);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No form fields found", result.Errors[0].Message);

        // Cleanup
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportAsync_WithTextFields_GeneratesValidFdf()
    {
        // Arrange
        var document = CreateMockDocument();
        var outputPath = Path.GetTempFileName();

        var fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "FirstName",
                Type = FormFieldType.Text,
                Value = "John",
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 100, 20)
            },
            new PdfFormField
            {
                Name = "LastName",
                Type = FormFieldType.Text,
                Value = "Doe",
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 30, 100, 50)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(fields));

        // Act
        var result = await _sut.ExportAsync(document, outputPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(outputPath));

        var fdfContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<xfdf", fdfContent);
        Assert.Contains("<field name=\"FirstName\">", fdfContent);
        Assert.Contains("<value>John</value>", fdfContent);
        Assert.Contains("<field name=\"LastName\">", fdfContent);
        Assert.Contains("<value>Doe</value>", fdfContent);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportAsync_WithCheckboxFields_GeneratesCorrectValues()
    {
        // Arrange
        var document = CreateMockDocument();
        var outputPath = Path.GetTempFileName();

        var fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "AgreeTerms",
                Type = FormFieldType.Checkbox,
                IsChecked = true,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 20, 20)
            },
            new PdfFormField
            {
                Name = "OptOut",
                Type = FormFieldType.Checkbox,
                IsChecked = false,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 30, 20, 50)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(fields));

        // Act
        var result = await _sut.ExportAsync(document, outputPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);

        var fdfContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<field name=\"AgreeTerms\">", fdfContent);
        Assert.Contains("<value>Yes</value>", fdfContent);
        Assert.Contains("<field name=\"OptOut\">", fdfContent);
        Assert.Contains("<value>Off</value>", fdfContent);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportAsync_WithComboBoxFields_ExportsSelectedOption()
    {
        // Arrange
        var document = CreateMockDocument();
        var outputPath = Path.GetTempFileName();

        var fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "Country",
                Type = FormFieldType.ComboBox,
                Options = new List<string> { "USA", "Canada", "Mexico" },
                SelectedIndex = 1, // Canada
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 150, 30)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(fields));

        // Act
        var result = await _sut.ExportAsync(document, outputPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);

        var fdfContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<field name=\"Country\">", fdfContent);
        Assert.Contains("<value>Canada</value>", fdfContent);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportAsync_WithMultiplePages_ExportsAllFields()
    {
        // Arrange
        var document = CreateMockDocument(pageCount: 2);
        var outputPath = Path.GetTempFileName();

        var page1Fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "Field1",
                Type = FormFieldType.Text,
                Value = "Page1Value",
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 100, 20)
            }
        };

        var page2Fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "Field2",
                Type = FormFieldType.Text,
                Value = "Page2Value",
                PageNumber = 2,
                Bounds = new PdfRectangle(0, 0, 100, 20)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(page1Fields));

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 2))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(page2Fields));

        // Act
        var result = await _sut.ExportAsync(document, outputPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);

        var fdfContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Field1", fdfContent);
        Assert.Contains("Page1Value", fdfContent);
        Assert.Contains("Field2", fdfContent);
        Assert.Contains("Page2Value", fdfContent);

        // Cleanup
        File.Delete(outputPath);
    }

    private PdfDocument CreateMockDocument(int pageCount = 1)
    {
        return new PdfDocument
        {
            FilePath = "C:\\test\\sample.pdf",
            PageCount = pageCount,
            Handle = new SafePdfDocumentHandle(IntPtr.Zero, false),
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };
    }
}
