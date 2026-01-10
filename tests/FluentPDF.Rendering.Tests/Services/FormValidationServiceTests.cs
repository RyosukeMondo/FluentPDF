using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

public sealed class FormValidationServiceTests
{
    private readonly FormValidationService _sut;
    private readonly Mock<ILogger<FormValidationService>> _loggerMock;

    public FormValidationServiceTests()
    {
        _loggerMock = new Mock<ILogger<FormValidationService>>();
        _sut = new FormValidationService(_loggerMock.Object);
    }

    #region ValidateField Tests

    [Fact]
    public void ValidateField_WithNullField_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.ValidateField(null!));
    }

    [Fact]
    public void ValidateField_WithValidTextField_ReturnsSuccess()
    {
        // Arrange
        var field = CreateTextField("Name", "John Doe");

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateField_WithRequiredEmptyTextField_ReturnsError()
    {
        // Arrange
        var field = CreateTextField("Name", null, isRequired: true);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorType.RequiredFieldEmpty,
            result.Errors[0].ErrorType);
        Assert.Equal("Name", result.Errors[0].FieldName);
        Assert.Contains("required", result.Errors[0].Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateField_WithRequiredWhitespaceTextField_ReturnsError()
    {
        // Arrange
        var field = CreateTextField("Name", "   ", isRequired: true);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorType.RequiredFieldEmpty,
            result.Errors[0].ErrorType);
    }

    [Fact]
    public void ValidateField_WithTextExceedingMaxLength_ReturnsError()
    {
        // Arrange
        var field = CreateTextField(
            "Name",
            "This is a very long text that exceeds the limit",
            maxLength: 10);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorType.MaxLengthExceeded,
            result.Errors[0].ErrorType);
        Assert.Contains("exceeds maximum length", result.Errors[0].Message);
        Assert.Contains("10", result.Errors[0].Message);
    }

    [Fact]
    public void ValidateField_WithTextAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var field = CreateTextField("Name", "12345", maxLength: 5);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateField_WithInvalidFormatMask_ReturnsError()
    {
        // Arrange - Phone number format (US)
        var field = CreateTextField(
            "Phone",
            "invalid-phone",
            formatMask: @"^\d{3}-\d{3}-\d{4}$");

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorType.InvalidFormat,
            result.Errors[0].ErrorType);
        Assert.Contains("invalid format", result.Errors[0].Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateField_WithValidFormatMask_ReturnsSuccess()
    {
        // Arrange - Phone number format (US)
        var field = CreateTextField(
            "Phone",
            "555-123-4567",
            formatMask: @"^\d{3}-\d{3}-\d{4}$");

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateField_WithEmailFormatMask_ValidatesCorrectly()
    {
        // Arrange - Simple email regex
        var validField = CreateTextField(
            "Email",
            "user@example.com",
            formatMask: @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        var invalidField = CreateTextField(
            "Email",
            "invalid-email",
            formatMask: @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        // Act
        var validResult = _sut.ValidateField(validField);
        var invalidResult = _sut.ValidateField(invalidField);

        // Assert
        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
    }

    [Fact]
    public void ValidateField_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange - Required field that's also too long
        var field = CreateTextField(
            "Name",
            "",
            isRequired: true,
            maxLength: 10);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors); // Only required field error
        Assert.Equal(ValidationErrorType.RequiredFieldEmpty,
            result.Errors[0].ErrorType);
    }

    [Fact]
    public void ValidateField_WithRequiredCheckbox_ValidatesCorrectly()
    {
        // Arrange
        var uncheckedField = CreateCheckboxField("Accept", false,
            isRequired: true);
        var checkedField = CreateCheckboxField("Accept", true,
            isRequired: true);

        // Act
        var uncheckedResult = _sut.ValidateField(uncheckedField);
        var checkedResult = _sut.ValidateField(checkedField);

        // Assert
        Assert.False(uncheckedResult.IsValid);
        Assert.Equal(ValidationErrorType.RequiredFieldEmpty,
            uncheckedResult.Errors[0].ErrorType);
        Assert.True(checkedResult.IsValid);
    }

    [Fact]
    public void ValidateField_WithRequiredRadioButton_ValidatesCorrectly()
    {
        // Arrange
        var unselectedField = CreateRadioButtonField("Option", false,
            isRequired: true);
        var selectedField = CreateRadioButtonField("Option", true,
            isRequired: true);

        // Act
        var unselectedResult = _sut.ValidateField(unselectedField);
        var selectedResult = _sut.ValidateField(selectedField);

        // Assert
        Assert.False(unselectedResult.IsValid);
        Assert.True(selectedResult.IsValid);
    }

    [Fact]
    public void ValidateField_WithEmptyFormatMask_DoesNotValidateFormat()
    {
        // Arrange
        var field = CreateTextField("Name", "any value", formatMask: "");

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateField_WithInvalidRegexPattern_ReturnsFalse()
    {
        // Arrange - Invalid regex pattern
        var field = CreateTextField(
            "Name",
            "value",
            formatMask: "[invalid(regex");

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationErrorType.InvalidFormat,
            result.Errors[0].ErrorType);
    }

    #endregion

    #region ValidateAllFields Tests

    [Fact]
    public void ValidateAllFields_WithNullFields_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _sut.ValidateAllFields(null!));
    }

    [Fact]
    public void ValidateAllFields_WithEmptyList_ReturnsSuccess()
    {
        // Arrange
        var fields = Array.Empty<PdfFormField>();

        // Act
        var result = _sut.ValidateAllFields(fields);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateAllFields_WithAllValidFields_ReturnsSuccess()
    {
        // Arrange
        var fields = new[]
        {
            CreateTextField("Name", "John Doe"),
            CreateTextField("Email", "john@example.com"),
            CreateCheckboxField("Accept", true)
        };

        // Act
        var result = _sut.ValidateAllFields(fields);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateAllFields_WithSomeInvalidFields_ReturnsAllErrors()
    {
        // Arrange
        var fields = new[]
        {
            CreateTextField("Name", "", isRequired: true), // Error
            CreateTextField("Email", "john@example.com"),
            CreateTextField("Phone", "toolongvalue", maxLength: 5) // Error
        };

        // Act
        var result = _sut.ValidateAllFields(fields);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.FieldName == "Name" &&
                 e.ErrorType == ValidationErrorType.RequiredFieldEmpty);
        Assert.Contains(result.Errors,
            e => e.FieldName == "Phone" &&
                 e.ErrorType == ValidationErrorType.MaxLengthExceeded);
    }

    [Fact]
    public void ValidateAllFields_WithMultipleErrorsPerField_AggregatesAll()
    {
        // Arrange
        var fields = new[]
        {
            CreateTextField("Field1", "", isRequired: true),
            CreateTextField("Field2", "toolong", maxLength: 3),
            CreateTextField("Field3", "invalid",
                formatMask: @"^\d+$") // Numbers only
        };

        // Act
        var result = _sut.ValidateAllFields(fields);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    #endregion

    #region ValidateProposedValue Tests

    [Fact]
    public void ValidateProposedValue_WithNullField_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _sut.ValidateProposedValue(null!, "value"));
    }

    [Fact]
    public void ValidateProposedValue_WithValidValue_ReturnsSuccess()
    {
        // Arrange
        var field = CreateTextField("Name", "OldValue");

        // Act
        var result = _sut.ValidateProposedValue(field, "NewValue");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProposedValue_WithReadOnlyField_ReturnsError()
    {
        // Arrange
        var field = CreateTextField("Name", "OldValue", isReadOnly: true);

        // Act
        var result = _sut.ValidateProposedValue(field, "NewValue");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorType.ReadOnlyFieldModified,
            result.Errors[0].ErrorType);
        Assert.Contains("read-only", result.Errors[0].Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateProposedValue_WithReadOnlyFieldSameValue_ReturnsSuccess()
    {
        // Arrange
        var field = CreateTextField("Name", "Value", isReadOnly: true);

        // Act
        var result = _sut.ValidateProposedValue(field, "Value");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProposedValue_WithRequiredFieldEmptyValue_ReturnsError()
    {
        // Arrange
        var field = CreateTextField("Name", "Current", isRequired: true);

        // Act
        var result = _sut.ValidateProposedValue(field, "");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorType == ValidationErrorType.RequiredFieldEmpty);
    }

    [Fact]
    public void ValidateProposedValue_ExceedingMaxLength_ReturnsError()
    {
        // Arrange
        var field = CreateTextField("Name", "OK", maxLength: 5);

        // Act
        var result = _sut.ValidateProposedValue(field, "TooLongValue");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorType == ValidationErrorType.MaxLengthExceeded);
    }

    [Fact]
    public void ValidateProposedValue_WithInvalidFormat_ReturnsError()
    {
        // Arrange
        var field = CreateTextField(
            "Phone",
            "555-123-4567",
            formatMask: @"^\d{3}-\d{3}-\d{4}$");

        // Act
        var result = _sut.ValidateProposedValue(field, "invalid");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.ErrorType == ValidationErrorType.InvalidFormat);
    }

    [Fact]
    public void ValidateProposedValue_WithNullProposedValue_ValidatesAsEmpty()
    {
        // Arrange
        var requiredField = CreateTextField("Name", "Current",
            isRequired: true);
        var optionalField = CreateTextField("Name", "Current");

        // Act
        var requiredResult = _sut.ValidateProposedValue(requiredField, null);
        var optionalResult = _sut.ValidateProposedValue(optionalField, null);

        // Assert
        Assert.False(requiredResult.IsValid);
        Assert.True(optionalResult.IsValid);
    }

    #endregion

    #region FormValidationResult Tests

    [Fact]
    public void FormValidationResult_GetSummaryMessage_WithNoErrors_ReturnsNull()
    {
        // Arrange
        var result = FormValidationResult.Success();

        // Act
        var message = result.GetSummaryMessage();

        // Assert
        Assert.Null(message);
    }

    [Fact]
    public void FormValidationResult_GetSummaryMessage_WithSingleError_ReturnsErrorMessage()
    {
        // Arrange
        var error = new FieldValidationError
        {
            FieldName = "Name",
            ErrorType = ValidationErrorType.RequiredFieldEmpty,
            Message = "Field 'Name' is required"
        };
        var result = FormValidationResult.Failure(error);

        // Act
        var message = result.GetSummaryMessage();

        // Assert
        Assert.Equal("Field 'Name' is required", message);
    }

    [Fact]
    public void FormValidationResult_GetSummaryMessage_WithMultipleErrors_ReturnsSummary()
    {
        // Arrange
        var errors = new[]
        {
            new FieldValidationError
            {
                FieldName = "Name",
                ErrorType = ValidationErrorType.RequiredFieldEmpty,
                Message = "Field 'Name' is required"
            },
            new FieldValidationError
            {
                FieldName = "Email",
                ErrorType = ValidationErrorType.InvalidFormat,
                Message = "Field 'Email' has an invalid format"
            }
        };
        var result = FormValidationResult.Failure(errors);

        // Act
        var message = result.GetSummaryMessage();

        // Assert
        Assert.Contains("2 validation errors", message);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValidateField_WithSpecialCharactersInValue_ValidatesCorrectly()
    {
        // Arrange
        var field = CreateTextField(
            "Name",
            "Ñoño & O'Brien-Smith",
            maxLength: 50);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateField_WithUnicodeCharacters_CountsCorrectly()
    {
        // Arrange - Emoji and other Unicode
        var field = CreateTextField("Name", "Hello 👋 World", maxLength: 20);

        // Act
        var result = _sut.ValidateField(field);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateField_WithBoundaryMaxLength_ValidatesCorrectly()
    {
        // Arrange
        var exactField = CreateTextField("Name", "12345", maxLength: 5);
        var overField = CreateTextField("Name", "123456", maxLength: 5);
        var underField = CreateTextField("Name", "1234", maxLength: 5);

        // Act
        var exactResult = _sut.ValidateField(exactField);
        var overResult = _sut.ValidateField(overField);
        var underResult = _sut.ValidateField(underField);

        // Assert
        Assert.True(exactResult.IsValid);
        Assert.False(overResult.IsValid);
        Assert.True(underResult.IsValid);
    }

    #endregion

    #region Helper Methods

    private static PdfFormField CreateTextField(
        string name,
        string? value,
        bool isRequired = false,
        bool isReadOnly = false,
        int? maxLength = null,
        string? formatMask = null)
    {
        return new PdfFormField
        {
            Name = name,
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            Value = value,
            IsRequired = isRequired,
            IsReadOnly = isReadOnly,
            MaxLength = maxLength,
            FormatMask = formatMask,
            NativeHandle = IntPtr.Zero
        };
    }

    private static PdfFormField CreateCheckboxField(
        string name,
        bool isChecked,
        bool isRequired = false)
    {
        return new PdfFormField
        {
            Name = name,
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsChecked = isChecked,
            IsRequired = isRequired,
            IsReadOnly = false,
            NativeHandle = IntPtr.Zero
        };
    }

    private static PdfFormField CreateRadioButtonField(
        string name,
        bool isChecked,
        bool isRequired = false,
        string? groupName = null)
    {
        return new PdfFormField
        {
            Name = name,
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsChecked = isChecked,
            IsRequired = isRequired,
            IsReadOnly = false,
            GroupName = groupName ?? "RadioGroup",
            NativeHandle = IntPtr.Zero
        };
    }

    #endregion
}
