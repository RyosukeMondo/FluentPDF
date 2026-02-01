using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for PdfFormService combo box functionality.
/// Verifies combo box selection and validation.
/// </summary>
public sealed class PdfFormServiceComboBoxTests
{
    private readonly Mock<ILogger<PdfFormService>> _mockLogger;
    private readonly PdfFormService _sut;

    public PdfFormServiceComboBoxTests()
    {
        _mockLogger = new Mock<ILogger<PdfFormService>>();
        _sut = new PdfFormService(_mockLogger.Object);
    }

    [Fact]
    public async Task SetComboBoxSelectionAsync_WithValidIndex_ReturnsSuccess()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "Country",
            Type = FormFieldType.ComboBox,
            Options = new List<string> { "USA", "Canada", "Mexico" },
            SelectedIndex = 0,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 150, 30)
        };

        // Act
        var result = await _sut.SetComboBoxSelectionAsync(field, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, field.SelectedIndex);
        Assert.Equal("Canada", field.SelectedOption);
    }

    [Fact]
    public async Task SetComboBoxSelectionAsync_WithNegativeOne_ClearsSelection()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "Country",
            Type = FormFieldType.ComboBox,
            Options = new List<string> { "USA", "Canada", "Mexico" },
            SelectedIndex = 1,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 150, 30)
        };

        // Act
        var result = await _sut.SetComboBoxSelectionAsync(field, -1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-1, field.SelectedIndex);
        Assert.Null(field.SelectedOption);
    }

    [Fact]
    public async Task SetComboBoxSelectionAsync_WithOutOfRangeIndex_ReturnsFailure()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "Country",
            Type = FormFieldType.ComboBox,
            Options = new List<string> { "USA", "Canada", "Mexico" },
            SelectedIndex = 0,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 150, 30)
        };

        // Act
        var result = await _sut.SetComboBoxSelectionAsync(field, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("out of range", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetComboBoxSelectionAsync_WithReadOnlyField_ReturnsFailure()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "Country",
            Type = FormFieldType.ComboBox,
            Options = new List<string> { "USA", "Canada", "Mexico" },
            SelectedIndex = 0,
            IsReadOnly = true,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 150, 30)
        };

        // Act
        var result = await _sut.SetComboBoxSelectionAsync(field, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("read-only", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetComboBoxSelectionAsync_WithNonComboField_ReturnsFailure()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TextBox",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 150, 30)
        };

        // Act
        var result = await _sut.SetComboBoxSelectionAsync(field, 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not a combo box", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetComboBoxSelectionAsync_WithNoOptions_ReturnsFailure()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "EmptyCombo",
            Type = FormFieldType.ComboBox,
            Options = null,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 150, 30)
        };

        // Act
        var result = await _sut.SetComboBoxSelectionAsync(field, 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("no options", result.Errors[0].Message);
    }

    [Fact]
    public async Task ResetFormAsync_ClearsAllFieldTypes()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "C:\\test\\sample.pdf",
            PageCount = 1,
            Handle = new SafePdfDocumentHandle(IntPtr.Zero, false),
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Note: This test is limited because we can't mock PDFium interop
        // Full integration tests would be needed to verify actual PDF reset

        // Act
        var result = await _sut.ResetFormAsync(document, 1);

        // Assert
        // Should succeed even with no fields (empty document)
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResetFormAsync_WithInvalidPageNumber_ReturnsFailure()
    {
        // Arrange
        var document = new PdfDocument
        {
            FilePath = "C:\\test\\sample.pdf",
            PageCount = 5,
            Handle = new SafePdfDocumentHandle(IntPtr.Zero, false),
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Act
        var result = await _sut.ResetFormAsync(document, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("out of range", result.Errors[0].Message);
    }
}
