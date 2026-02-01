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
/// Unit tests for FdfImportService.
/// Verifies FDF XML parsing and form field population.
/// </summary>
public sealed class FdfImportServiceTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IPdfFormService> _mockFormService;
    private readonly FdfImportService _sut;

    public FdfImportServiceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockFormService = new Mock<IPdfFormService>();
        _sut = new FdfImportService(_mockLogger.Object);
    }

    [Fact]
    public async Task ImportAsync_WithValidFdf_SetsFieldValues()
    {
        // Arrange
        var document = CreateMockDocument();
        var fdfPath = CreateTestFdf(new Dictionary<string, string>
        {
            { "FirstName", "John" },
            { "LastName", "Doe" }
        });

        var pdfFields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "FirstName",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 100, 20)
            },
            new PdfFormField
            {
                Name = "LastName",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 30, 100, 50)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(pdfFields));

        _mockFormService
            .Setup(x => x.SetFieldValueAsync(It.IsAny<PdfFormField>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.ImportAsync(document, fdfPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);

        _mockFormService.Verify(
            x => x.SetFieldValueAsync(
                It.Is<PdfFormField>(f => f.Name == "FirstName"),
                "John"),
            Times.Once);

        _mockFormService.Verify(
            x => x.SetFieldValueAsync(
                It.Is<PdfFormField>(f => f.Name == "LastName"),
                "Doe"),
            Times.Once);

        // Cleanup
        File.Delete(fdfPath);
    }

    [Fact]
    public async Task ImportAsync_WithCheckboxValues_SetsCheckedState()
    {
        // Arrange
        var document = CreateMockDocument();
        var fdfPath = CreateTestFdf(new Dictionary<string, string>
        {
            { "AgreeTerms", "Yes" },
            { "OptOut", "Off" }
        });

        var pdfFields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "AgreeTerms",
                Type = FormFieldType.Checkbox,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 20, 20)
            },
            new PdfFormField
            {
                Name = "OptOut",
                Type = FormFieldType.Checkbox,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 30, 20, 50)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(pdfFields));

        _mockFormService
            .Setup(x => x.SetCheckboxStateAsync(It.IsAny<PdfFormField>(), It.IsAny<bool>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.ImportAsync(document, fdfPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);

        _mockFormService.Verify(
            x => x.SetCheckboxStateAsync(
                It.Is<PdfFormField>(f => f.Name == "AgreeTerms"),
                true),
            Times.Once);

        _mockFormService.Verify(
            x => x.SetCheckboxStateAsync(
                It.Is<PdfFormField>(f => f.Name == "OptOut"),
                false),
            Times.Once);

        // Cleanup
        File.Delete(fdfPath);
    }

    [Fact]
    public async Task ImportAsync_WithComboBoxValue_SetsSelection()
    {
        // Arrange
        var document = CreateMockDocument();
        var fdfPath = CreateTestFdf(new Dictionary<string, string>
        {
            { "Country", "Canada" }
        });

        var pdfFields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "Country",
                Type = FormFieldType.ComboBox,
                Options = new List<string> { "USA", "Canada", "Mexico" },
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 150, 30)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(pdfFields));

        _mockFormService
            .Setup(x => x.SetComboBoxSelectionAsync(It.IsAny<PdfFormField>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.ImportAsync(document, fdfPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess);

        _mockFormService.Verify(
            x => x.SetComboBoxSelectionAsync(
                It.Is<PdfFormField>(f => f.Name == "Country"),
                1), // Index of "Canada"
            Times.Once);

        // Cleanup
        File.Delete(fdfPath);
    }

    [Fact]
    public async Task ImportAsync_WithMissingField_SkipsAndContinues()
    {
        // Arrange
        var document = CreateMockDocument();
        var fdfPath = CreateTestFdf(new Dictionary<string, string>
        {
            { "ExistingField", "Value1" },
            { "NonExistentField", "Value2" }
        });

        var pdfFields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "ExistingField",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 0, 100, 20)
            }
        };

        _mockFormService
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(pdfFields));

        _mockFormService
            .Setup(x => x.SetFieldValueAsync(It.IsAny<PdfFormField>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.ImportAsync(document, fdfPath, _mockFormService.Object);

        // Assert
        Assert.True(result.IsSuccess); // Should succeed despite missing field

        _mockFormService.Verify(
            x => x.SetFieldValueAsync(
                It.Is<PdfFormField>(f => f.Name == "ExistingField"),
                "Value1"),
            Times.Once);

        // Cleanup
        File.Delete(fdfPath);
    }

    [Fact]
    public async Task ImportAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var document = CreateMockDocument();
        var fdfPath = "C:\\nonexistent\\file.fdf";

        // Act
        var result = await _sut.ImportAsync(document, fdfPath, _mockFormService.Object);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Errors[0].Message);
    }

    private PdfDocument CreateMockDocument()
    {
        return new PdfDocument
        {
            FilePath = "C:\\test\\sample.pdf",
            PageCount = 1,
            Handle = new SafePdfDocumentHandle(IntPtr.Zero, false),
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };
    }

    private string CreateTestFdf(Dictionary<string, string> fields)
    {
        var fdfPath = Path.GetTempFileName();
        var fdfContent = GenerateFdfXml(fields);
        File.WriteAllText(fdfPath, fdfContent);
        return fdfPath;
    }

    private string GenerateFdfXml(Dictionary<string, string> fields)
    {
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xfdf xmlns=""http://ns.adobe.com/xfdf/"" xml:space=""preserve"">
  <f href=""sample.pdf"" />
  <fields>";

        foreach (var (name, value) in fields)
        {
            xml += $@"
    <field name=""{name}"">
      <value>{value}</value>
    </field>";
        }

        xml += @"
  </fields>
</xfdf>";

        return xml;
    }
}
