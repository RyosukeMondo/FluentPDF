using FluentAssertions;
using FluentPDF.Core.Models;
using Xunit;

namespace FluentPDF.Core.Tests.Models;

/// <summary>
/// Unit tests for the PdfFormField, PdfRectangle, and FormFieldType models.
/// </summary>
public sealed class PdfFormFieldTests
{
    #region PdfFormField Tests

    [Fact]
    public void PdfFormField_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var field = new PdfFormField
        {
            Name = "FirstName",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        // Assert
        field.Name.Should().Be("FirstName");
        field.Type.Should().Be(FormFieldType.Text);
        field.PageNumber.Should().Be(1);
        field.Bounds.Left.Should().Be(10);
        field.TabOrder.Should().Be(-1);
        field.Value.Should().BeNull();
        field.IsChecked.Should().BeNull();
        field.IsRequired.Should().BeFalse();
        field.IsReadOnly.Should().BeFalse();
        field.MaxLength.Should().BeNull();
        field.FormatMask.Should().BeNull();
        field.GroupName.Should().BeNull();
    }

    [Fact]
    public void PdfFormField_CanBeCreated_WithAllProperties()
    {
        // Arrange & Act
        var field = new PdfFormField
        {
            Name = "Email",
            Type = FormFieldType.Text,
            PageNumber = 2,
            Bounds = new PdfRectangle(50, 100, 200, 130),
            TabOrder = 5,
            Value = "test@example.com",
            IsRequired = true,
            IsReadOnly = false,
            MaxLength = 100,
            FormatMask = @"^[\w\.-]+@[\w\.-]+\.\w+$",
            NativeHandle = new IntPtr(12345)
        };

        // Assert
        field.Name.Should().Be("Email");
        field.Type.Should().Be(FormFieldType.Text);
        field.PageNumber.Should().Be(2);
        field.TabOrder.Should().Be(5);
        field.Value.Should().Be("test@example.com");
        field.IsRequired.Should().BeTrue();
        field.IsReadOnly.Should().BeFalse();
        field.MaxLength.Should().Be(100);
        field.FormatMask.Should().Be(@"^[\w\.-]+@[\w\.-]+\.\w+$");
        field.NativeHandle.Should().Be(new IntPtr(12345));
    }

    [Fact]
    public void PdfFormField_TextField_CanSetAndGetValue()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "Address",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        // Act
        field.Value = "123 Main St";

        // Assert
        field.Value.Should().Be("123 Main St");
    }

    [Fact]
    public void PdfFormField_CheckboxField_CanSetAndGetIsChecked()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "AgreeToTerms",
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 30, 40)
        };

        // Act
        field.IsChecked = true;

        // Assert
        field.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void PdfFormField_RadioButton_HasGroupName()
    {
        // Arrange & Act
        var field = new PdfFormField
        {
            Name = "PaymentMethod.CreditCard",
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 30, 40),
            GroupName = "PaymentMethod"
        };

        // Assert
        field.GroupName.Should().Be("PaymentMethod");
        field.Type.Should().Be(FormFieldType.RadioButton);
    }

    [Fact]
    public void PdfFormField_Validate_ThrowsException_WhenNameIsEmpty()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Field name is required.");
    }

    [Fact]
    public void PdfFormField_Validate_ThrowsException_WhenNameIsWhitespace()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "   ",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Field name is required.");
    }

    [Fact]
    public void PdfFormField_Validate_ThrowsException_WhenPageNumberIsZero()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TestField",
            Type = FormFieldType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid page number 0. Must be >= 1.");
    }

    [Fact]
    public void PdfFormField_Validate_ThrowsException_WhenPageNumberIsNegative()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TestField",
            Type = FormFieldType.Text,
            PageNumber = -1,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid page number -1. Must be >= 1.");
    }

    [Fact]
    public void PdfFormField_Validate_ThrowsException_WhenBoundsAreInvalid()
    {
        // Arrange - Right <= Left
        var field = new PdfFormField
        {
            Name = "TestField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(100, 20, 50, 50) // Right < Left
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid bounds*Right must be > Left*");
    }

    [Fact]
    public void PdfFormField_Validate_ThrowsException_WhenMaxLengthIsNegative()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "TestField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50),
            MaxLength = -5
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid MaxLength -5*Must be >= 0 or null*");
    }

    [Fact]
    public void PdfFormField_Validate_Succeeds_WhenAllPropertiesValid()
    {
        // Arrange
        var field = new PdfFormField
        {
            Name = "ValidField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50),
            MaxLength = 50
        };

        // Act
        Action act = () => field.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region PdfRectangle Tests

    [Fact]
    public void PdfRectangle_CanBeCreated_WithCoordinates()
    {
        // Arrange & Act
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Assert
        rect.Left.Should().Be(10);
        rect.Bottom.Should().Be(20);
        rect.Right.Should().Be(100);
        rect.Top.Should().Be(80);
    }

    [Fact]
    public void PdfRectangle_CalculatesWidth_Correctly()
    {
        // Arrange
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Act
        var width = rect.Width;

        // Assert
        width.Should().Be(90); // 100 - 10
    }

    [Fact]
    public void PdfRectangle_CalculatesHeight_Correctly()
    {
        // Arrange
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Act
        var height = rect.Height;

        // Assert
        height.Should().Be(60); // 80 - 20
    }

    [Fact]
    public void PdfRectangle_Contains_ReturnsTrue_WhenPointInside()
    {
        // Arrange
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Act
        var result = rect.Contains(50, 50);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PdfRectangle_Contains_ReturnsFalse_WhenPointOutside()
    {
        // Arrange
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Act & Assert
        rect.Contains(5, 50).Should().BeFalse();   // Left of rect
        rect.Contains(150, 50).Should().BeFalse(); // Right of rect
        rect.Contains(50, 10).Should().BeFalse();  // Below rect
        rect.Contains(50, 100).Should().BeFalse(); // Above rect
    }

    [Fact]
    public void PdfRectangle_Contains_ReturnsTrue_WhenPointOnEdge()
    {
        // Arrange
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Act & Assert
        rect.Contains(10, 50).Should().BeTrue();  // Left edge
        rect.Contains(100, 50).Should().BeTrue(); // Right edge
        rect.Contains(50, 20).Should().BeTrue();  // Bottom edge
        rect.Contains(50, 80).Should().BeTrue();  // Top edge
    }

    [Fact]
    public void PdfRectangle_IsValid_ReturnsTrue_WhenDimensionsValid()
    {
        // Arrange
        var rect = new PdfRectangle(10, 20, 100, 80);

        // Act
        var isValid = rect.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PdfRectangle_IsValid_ReturnsFalse_WhenRightLessThanLeft()
    {
        // Arrange
        var rect = new PdfRectangle(100, 20, 10, 80); // Right < Left

        // Act
        var isValid = rect.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PdfRectangle_IsValid_ReturnsFalse_WhenTopLessThanBottom()
    {
        // Arrange
        var rect = new PdfRectangle(10, 80, 100, 20); // Top < Bottom

        // Act
        var isValid = rect.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PdfRectangle_IsValid_ReturnsFalse_WhenRightEqualsLeft()
    {
        // Arrange
        var rect = new PdfRectangle(50, 20, 50, 80); // Right == Left

        // Act
        var isValid = rect.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void PdfRectangle_IsValueType_StructEquality()
    {
        // Arrange
        var rect1 = new PdfRectangle(10, 20, 100, 80);
        var rect2 = new PdfRectangle(10, 20, 100, 80);
        var rect3 = new PdfRectangle(10, 20, 100, 81);

        // Act & Assert
        rect1.Should().Be(rect2);           // Equal values
        rect1.Should().NotBe(rect3);        // Different values
        rect1.Equals(rect2).Should().BeTrue();
    }

    #endregion

    #region FormFieldType Tests

    [Fact]
    public void FormFieldType_HasExpectedValues()
    {
        // Assert
        ((int)FormFieldType.Unknown).Should().Be(0);
        ((int)FormFieldType.PushButton).Should().Be(1);
        ((int)FormFieldType.Checkbox).Should().Be(2);
        ((int)FormFieldType.RadioButton).Should().Be(3);
        ((int)FormFieldType.ComboBox).Should().Be(4);
        ((int)FormFieldType.ListBox).Should().Be(5);
        ((int)FormFieldType.Text).Should().Be(6);
        ((int)FormFieldType.Signature).Should().Be(7);
    }

    [Fact]
    public void FormFieldType_CanBeAssignedToField()
    {
        // Arrange & Act
        var textField = new PdfFormField
        {
            Name = "Name",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 100, 50)
        };

        var checkboxField = new PdfFormField
        {
            Name = "Agree",
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 20, 30, 40)
        };

        // Assert
        textField.Type.Should().Be(FormFieldType.Text);
        checkboxField.Type.Should().Be(FormFieldType.Checkbox);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PdfFormField_ComplexScenario_TextFieldWithValidation()
    {
        // Arrange & Act
        var field = new PdfFormField
        {
            Name = "Form.Contact.PhoneNumber",
            Type = FormFieldType.Text,
            PageNumber = 3,
            Bounds = new PdfRectangle(50, 100, 250, 130),
            TabOrder = 10,
            Value = "555-1234",
            IsRequired = true,
            IsReadOnly = false,
            MaxLength = 12,
            FormatMask = @"^\d{3}-\d{4}$"
        };

        // Assert
        field.Validate(); // Should not throw
        field.Name.Should().Be("Form.Contact.PhoneNumber");
        field.Value.Should().Be("555-1234");
        field.IsRequired.Should().BeTrue();
        field.MaxLength.Should().Be(12);
    }

    [Fact]
    public void PdfFormField_ComplexScenario_RadioButtonGroup()
    {
        // Arrange & Act
        var radio1 = new PdfFormField
        {
            Name = "Gender.Male",
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(50, 100, 70, 120),
            GroupName = "Gender",
            IsChecked = true
        };

        var radio2 = new PdfFormField
        {
            Name = "Gender.Female",
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(100, 100, 120, 120),
            GroupName = "Gender",
            IsChecked = false
        };

        // Assert
        radio1.GroupName.Should().Be(radio2.GroupName);
        radio1.IsChecked.Should().BeTrue();
        radio2.IsChecked.Should().BeFalse();
    }

    #endregion
}
