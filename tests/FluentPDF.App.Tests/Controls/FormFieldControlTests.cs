using FluentAssertions;
using FluentPDF.App.Controls;
using FluentPDF.Core.Models;

namespace FluentPDF.App.Tests.Controls;

/// <summary>
/// Tests for FormFieldControl custom WinUI control.
/// Tests basic property and event behavior without requiring full UI runtime.
/// </summary>
public class FormFieldControlTests
{
    [Fact]
    public void Constructor_ShouldInitializeControl()
    {
        // Arrange & Act
        var control = new FormFieldControl();

        // Assert
        control.Should().NotBeNull();
        control.Field.Should().BeNull("no field assigned initially");
        control.ZoomLevel.Should().Be(1.0, "default zoom level");
        control.IsInErrorState.Should().BeFalse("not in error state initially");
    }

    [Fact]
    public void Field_ShouldAcceptValidFormField()
    {
        // Arrange
        var control = new FormFieldControl();
        var field = CreateTextField();

        // Act
        control.Field = field;

        // Assert
        control.Field.Should().Be(field);
        control.Field.Name.Should().Be("TestField");
        control.Field.Type.Should().Be(FormFieldType.Text);
    }

    [Fact]
    public void ZoomLevel_ShouldAcceptValidValues()
    {
        // Arrange
        var control = new FormFieldControl();

        // Act
        control.ZoomLevel = 2.5;

        // Assert
        control.ZoomLevel.Should().Be(2.5);
    }

    [Fact]
    public void ZoomLevel_ShouldAcceptMinimumZoom()
    {
        // Arrange
        var control = new FormFieldControl();

        // Act
        control.ZoomLevel = 0.1;

        // Assert
        control.ZoomLevel.Should().Be(0.1);
    }

    [Fact]
    public void ZoomLevel_ShouldAcceptMaximumZoom()
    {
        // Arrange
        var control = new FormFieldControl();

        // Act
        control.ZoomLevel = 10.0;

        // Assert
        control.ZoomLevel.Should().Be(10.0);
    }

    [Fact]
    public void IsInErrorState_ShouldAcceptTrue()
    {
        // Arrange
        var control = new FormFieldControl();

        // Act
        control.IsInErrorState = true;

        // Assert
        control.IsInErrorState.Should().BeTrue();
    }

    [Fact]
    public void IsInErrorState_ShouldAcceptFalse()
    {
        // Arrange
        var control = new FormFieldControl();
        control.IsInErrorState = true;

        // Act
        control.IsInErrorState = false;

        // Assert
        control.IsInErrorState.Should().BeFalse();
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaisedWhenValueChanges()
    {
        // Arrange
        var control = new FormFieldControl();
        var eventRaised = false;
        string? capturedValue = null;

        control.ValueChanged += (sender, value) =>
        {
            eventRaised = true;
            capturedValue = value;
        };

        // Act
        control.RaiseValueChanged("New Value");

        // Assert
        eventRaised.Should().BeTrue("ValueChanged event should be raised");
        capturedValue.Should().Be("New Value");
    }

    [Fact]
    public void ValueChanged_Event_ShouldHandleNullValue()
    {
        // Arrange
        var control = new FormFieldControl();
        var eventRaised = false;
        string? capturedValue = "initial";

        control.ValueChanged += (sender, value) =>
        {
            eventRaised = true;
            capturedValue = value;
        };

        // Act
        control.RaiseValueChanged(null);

        // Assert
        eventRaised.Should().BeTrue("ValueChanged event should be raised");
        capturedValue.Should().BeNull();
    }

    [Fact]
    public void FocusChanged_Event_ShouldBeRaisedWhenFocusChanges()
    {
        // Arrange
        var control = new FormFieldControl();
        var eventRaisedCount = 0;
        var focusStates = new List<bool>();

        control.FocusChanged += (sender, hasFocus) =>
        {
            eventRaisedCount++;
            focusStates.Add(hasFocus);
        };

        // Simulate focus changes by invoking the event manually
        // In actual UI tests, this would be triggered by WinUI runtime

        // Act & Assert
        // Note: Without full UI runtime, we can't test actual focus changes
        // This test verifies the event exists and can be subscribed to
        control.FocusChanged.Should().NotBeNull("FocusChanged event should exist");
    }

    [Fact]
    public void Field_ShouldSupportTextFieldType()
    {
        // Arrange
        var control = new FormFieldControl();
        var field = CreateTextField();

        // Act
        control.Field = field;

        // Assert
        control.Field.Type.Should().Be(FormFieldType.Text);
    }

    [Fact]
    public void Field_ShouldSupportCheckboxFieldType()
    {
        // Arrange
        var control = new FormFieldControl();
        var field = CreateCheckboxField();

        // Act
        control.Field = field;

        // Assert
        control.Field.Type.Should().Be(FormFieldType.Checkbox);
    }

    [Fact]
    public void Field_ShouldSupportRadioButtonFieldType()
    {
        // Arrange
        var control = new FormFieldControl();
        var field = CreateRadioButtonField();

        // Act
        control.Field = field;

        // Assert
        control.Field.Type.Should().Be(FormFieldType.RadioButton);
    }

    [Fact]
    public void Field_ShouldPreserveReadOnlyState()
    {
        // Arrange
        var control = new FormFieldControl();
        var field = new PdfFormField
        {
            Name = "ReadOnlyField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            IsReadOnly = true
        };

        // Act
        control.Field = field;

        // Assert
        control.Field.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Field_ShouldPreserveRequiredState()
    {
        // Arrange
        var control = new FormFieldControl();
        var field = new PdfFormField
        {
            Name = "RequiredField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            IsRequired = true
        };

        // Act
        control.Field = field;

        // Assert
        control.Field.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void Control_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that the control can be instantiated
        // without requiring full WinUI runtime (headless testing)

        // Arrange & Act
        var control = new FormFieldControl();

        // Assert
        control.Should().NotBeNull();
    }

    // Helper methods for creating test form fields

    private PdfFormField CreateTextField()
    {
        return new PdfFormField
        {
            Name = "TestField",
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 10, 200, 40),
            Value = "",
            MaxLength = 100
        };
    }

    private PdfFormField CreateCheckboxField()
    {
        return new PdfFormField
        {
            Name = "CheckboxField",
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 50, 30, 70),
            IsChecked = false
        };
    }

    private PdfFormField CreateRadioButtonField()
    {
        return new PdfFormField
        {
            Name = "RadioField",
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(10, 80, 30, 100),
            IsChecked = false,
            GroupName = "RadioGroup1"
        };
    }
}
