using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for PdfFormService.
/// These tests verify the business logic layer without requiring PDFium.
/// Integration tests with real PDFium are in FormFillingIntegrationTests.
/// </summary>
public sealed class PdfFormServiceTests
{
    private readonly Mock<ILogger<PdfFormService>> _loggerMock;
    private readonly PdfFormService _service;

    public PdfFormServiceTests()
    {
        _loggerMock = new Mock<ILogger<PdfFormService>>();
        _service = new PdfFormService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PdfFormService(null!));
    }

    [Fact]
    public async Task GetFormFieldsAsync_WithNullDocument_ReturnsError()
    {
        // Act
        var result = await _service.GetFormFieldsAsync(null!, 1);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_DOCUMENT", pdfError.ErrorCode);
    }

    [Fact]
    public async Task GetFormFieldsAsync_WithInvalidPageNumber_ReturnsError()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "/test.pdf",
            PageCount = 5,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Act - page 0 (invalid)
        var result1 = await _service.GetFormFieldsAsync(document, 0);

        // Assert
        Assert.True(result1.IsFailed);
        Assert.Contains("out of range", result1.Errors[0].Message);

        // Act - page 6 (out of range)
        var result2 = await _service.GetFormFieldsAsync(document, 6);

        // Assert
        Assert.True(result2.IsFailed);
        Assert.Contains("out of range", result2.Errors[0].Message);
    }

    [Fact]
    public async Task GetFormFieldAtPointAsync_WithNullDocument_ReturnsError()
    {
        // Act
        var result = await _service.GetFormFieldAtPointAsync(null!, 1, 100, 100);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_DOCUMENT", pdfError.ErrorCode);
    }

    [Fact]
    public async Task GetFormFieldAtPointAsync_WithInvalidPageNumber_ReturnsError()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "/test.pdf",
            PageCount = 5,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Act
        var result = await _service.GetFormFieldAtPointAsync(document, 10, 100, 100);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("out of range", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetFieldValueAsync_WithNullField_ReturnsError()
    {
        // Act
        var result = await _service.SetFieldValueAsync(null!, "test");

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_FIELD", pdfError.ErrorCode);
    }

    [Fact]
    public async Task SetFieldValueAsync_WithReadOnlyField_ReturnsError()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "ReadOnlyField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            IsReadOnly = true,
            IsRequired = false,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetFieldValueAsync(field, "test");

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_READONLY_FIELD", pdfError.ErrorCode);
        Assert.Contains("read-only", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetFieldValueAsync_WithValueExceedingMaxLength_ReturnsError()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TextField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            IsReadOnly = false,
            IsRequired = false,
            MaxLength = 10,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetFieldValueAsync(field, "This is a very long value that exceeds the max length");

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_VALUE", pdfError.ErrorCode);
        Assert.Contains("exceeds maximum length", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetFieldValueAsync_WithValidValue_UpdatesFieldAndSucceeds()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TextField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            IsReadOnly = false,
            IsRequired = false,
            MaxLength = 50,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetFieldValueAsync(field, "Valid Value");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Valid Value", field.Value);
    }

    [Fact]
    public async Task SetCheckboxStateAsync_WithNullField_ReturnsError()
    {
        // Act
        var result = await _service.SetCheckboxStateAsync(null!, true);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_FIELD", pdfError.ErrorCode);
    }

    [Fact]
    public async Task SetCheckboxStateAsync_WithNonCheckboxField_ReturnsError()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TextField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            IsReadOnly = false,
            IsRequired = false,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetCheckboxStateAsync(field, true);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_FIELD_TYPE", pdfError.ErrorCode);
        Assert.Contains("not a checkbox", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetCheckboxStateAsync_WithReadOnlyCheckbox_ReturnsError()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "CheckboxField",
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsReadOnly = true,
            IsRequired = false,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetCheckboxStateAsync(field, true);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_READONLY_FIELD", pdfError.ErrorCode);
    }

    [Fact]
    public async Task SetCheckboxStateAsync_WithValidCheckbox_UpdatesStateAndSucceeds()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "CheckboxField",
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsReadOnly = false,
            IsRequired = false,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetCheckboxStateAsync(field, true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(field.IsChecked);
    }

    [Fact]
    public async Task SetCheckboxStateAsync_WithValidRadioButton_UpdatesStateAndSucceeds()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "RadioField",
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsReadOnly = false,
            IsRequired = false,
            NativeHandle = IntPtr.Zero
        };

        // Act
        var result = await _service.SetCheckboxStateAsync(field, true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(field.IsChecked);
    }

    [Fact]
    public async Task SaveFormDataAsync_WithNullDocument_ReturnsError()
    {
        // Act
        var result = await _service.SaveFormDataAsync(null!, "/output.pdf");

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_DOCUMENT", pdfError.ErrorCode);
    }

    [Fact]
    public async Task SaveFormDataAsync_WithNullOutputPath_ReturnsError()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "/test.pdf",
            PageCount = 5,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Act
        var result = await _service.SaveFormDataAsync(document, null!);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_OUTPUT_PATH", pdfError.ErrorCode);
    }

    [Fact]
    public async Task SaveFormDataAsync_WithEmptyOutputPath_ReturnsError()
    {
        // Arrange
        var mockHandle = new Mock<IDisposable>();
        var document = new PdfDocument
        {
            FilePath = "/test.pdf",
            PageCount = 5,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Act
        var result = await _service.SaveFormDataAsync(document, "");

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_OUTPUT_PATH", pdfError.ErrorCode);
    }

    [Fact]
    public void GetFieldsInTabOrder_WithNullFields_ReturnsError()
    {
        // Act
        var result = _service.GetFieldsInTabOrder(null!);

        // Assert
        Assert.True(result.IsFailed);
        var pdfError = Assert.IsType<PdfError>(result.Errors[0]);
        Assert.Equal("FORM_INVALID_FIELDS", pdfError.ErrorCode);
    }

    [Fact]
    public void GetFieldsInTabOrder_WithEmptyFields_ReturnsEmptyList()
    {
        // Arrange
        var fields = new List<PdfFormField>();

        // Act
        var result = _service.GetFieldsInTabOrder(fields);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void GetFieldsInTabOrder_SortsByTabOrderThenSpatial()
    {
        // Arrange
        var fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "Field3",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 150, 100, 170),
                TabOrder = 2,
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            },
            new PdfFormField
            {
                Name = "Field1",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 50, 100, 70),
                TabOrder = 0,
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            },
            new PdfFormField
            {
                Name = "Field2",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 100, 100, 120),
                TabOrder = 1,
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            },
            new PdfFormField
            {
                Name = "Field4NoTab",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 30, 100, 50), // Higher on page (smaller Top)
                TabOrder = -1, // No tab order
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            },
            new PdfFormField
            {
                Name = "Field5NoTab",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 200, 100, 220), // Lower on page
                TabOrder = -1, // No tab order
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            }
        };

        // Act
        var result = _service.GetFieldsInTabOrder(fields);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);

        // Fields with tab order should come first, sorted by tab order
        Assert.Equal("Field1", result.Value[0].Name);
        Assert.Equal("Field2", result.Value[1].Name);
        Assert.Equal("Field3", result.Value[2].Name);

        // Fields without tab order should come after, sorted spatially (top to bottom)
        Assert.Equal("Field4NoTab", result.Value[3].Name);
        Assert.Equal("Field5NoTab", result.Value[4].Name);
    }

    [Fact]
    public void GetFieldsInTabOrder_WithSameTabOrder_SortsByPosition()
    {
        // Arrange
        var fields = new List<PdfFormField>
        {
            new PdfFormField
            {
                Name = "FieldBottom",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 200, 100, 220),
                TabOrder = 0,
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            },
            new PdfFormField
            {
                Name = "FieldTop",
                Type = FormFieldType.Text,
                PageNumber = 1,
                Bounds = new PdfRectangle(0, 50, 100, 70),
                TabOrder = 0,
                IsReadOnly = false,
                IsRequired = false,
                NativeHandle = IntPtr.Zero
            }
        };

        // Act
        var result = _service.GetFieldsInTabOrder(fields);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        // Should be sorted by Top coordinate (top to bottom)
        Assert.Equal("FieldTop", result.Value[0].Name);
        Assert.Equal("FieldBottom", result.Value[1].Name);
    }
}
