using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for FormFieldViewModel demonstrating headless MVVM testing.
/// Verifies form field loading, editing, validation, navigation, and dirty tracking.
/// </summary>
public class FormFieldViewModelTests
{
    private readonly Mock<IPdfFormService> _formServiceMock;
    private readonly Mock<IFormValidationService> _validationServiceMock;
    private readonly Mock<ILogger<FormFieldViewModel>> _loggerMock;

    public FormFieldViewModelTests()
    {
        _formServiceMock = new Mock<IPdfFormService>();
        _validationServiceMock = new Mock<IFormValidationService>();
        _loggerMock = new Mock<ILogger<FormFieldViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.FormFields.Should().BeEmpty("no fields loaded initially");
        viewModel.FocusedField.Should().BeNull("no field focused initially");
        viewModel.HasFormFields.Should().BeFalse("no fields loaded");
        viewModel.IsModified.Should().BeFalse("no modifications made");
        viewModel.ValidationMessage.Should().BeNull("no validation run");
        viewModel.IsLoading.Should().BeFalse("not loading initially");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenFormServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new FormFieldViewModel(
            null!,
            _validationServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("formService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenValidationServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new FormFieldViewModel(
            _formServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validationService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new FormFieldViewModel(
            _formServiceMock.Object,
            _validationServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LoadFormFieldsAsync_ShouldPopulateFormFields_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        // Act
        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        // Assert
        viewModel.FormFields.Should().HaveCount(3);
        viewModel.HasFormFields.Should().BeTrue();
        viewModel.FormFields[0].Name.Should().Be("Field1");
        viewModel.FormFields[1].Name.Should().Be("Field2");
        viewModel.FormFields[2].Name.Should().Be("Field3");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadFormFieldsAsync_ShouldClearFields_WhenServiceFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Fail("Failed to load fields"));

        // Act
        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        // Assert
        viewModel.FormFields.Should().BeEmpty();
        viewModel.HasFormFields.Should().BeFalse();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadFormFieldsAsync_ShouldSetIsLoadingToTrue_DuringExecution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var wasLoadingDuringExecution = false;

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(() =>
            {
                wasLoadingDuringExecution = viewModel.IsLoading;
                return Result.Ok<IReadOnlyList<PdfFormField>>(new List<PdfFormField>());
            });

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(It.IsAny<IReadOnlyList<PdfFormField>>()))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(new List<PdfFormField>()));

        // Act
        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        // Assert
        wasLoadingDuringExecution.Should().BeTrue("IsLoading should be set during execution");
        viewModel.IsLoading.Should().BeFalse("IsLoading should be reset after execution");
    }

    [Fact]
    public async Task LoadFormFieldsAsync_ShouldHandleInvalidParameter_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadFormFieldsCommand.ExecuteAsync(null);

        // Assert
        viewModel.FormFields.Should().BeEmpty();
        viewModel.HasFormFields.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateFieldValueAsync_ShouldUpdateField_WhenValidationPasses()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var field = CreateTextField("TestField", "");

        _validationServiceMock
            .Setup(x => x.ValidateProposedValue(field, "New Value"))
            .Returns(FormValidationResult.Success());

        _formServiceMock
            .Setup(x => x.SetFieldValueAsync(field, "New Value"))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.UpdateFieldValueAsync((field, "New Value"));

        // Assert
        field.Value.Should().Be("New Value");
        viewModel.IsModified.Should().BeTrue();
        viewModel.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public async Task UpdateFieldValueAsync_ShouldNotUpdateField_WhenValidationFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var field = CreateTextField("TestField", "");
        var validationError = new FieldValidationError(
            "TestField",
            ValidationErrorType.MaxLengthExceeded,
            "Value too long");

        _validationServiceMock
            .Setup(x => x.ValidateProposedValue(field, "Too Long Value"))
            .Returns(FormValidationResult.Failure(validationError));

        // Act
        await viewModel.UpdateFieldValueAsync((field, "Too Long Value"));

        // Assert
        field.Value.Should().BeEmpty();
        viewModel.IsModified.Should().BeFalse();
        viewModel.ValidationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateFieldValueAsync_ShouldSetValidationMessage_WhenServiceFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var field = CreateTextField("TestField", "");

        _validationServiceMock
            .Setup(x => x.ValidateProposedValue(field, "Value"))
            .Returns(FormValidationResult.Success());

        _formServiceMock
            .Setup(x => x.SetFieldValueAsync(field, "Value"))
            .ReturnsAsync(Result.Fail("Service error"));

        // Act
        await viewModel.UpdateFieldValueAsync((field, "Value"));

        // Assert
        viewModel.ValidationMessage.Should().Be("Service error");
        viewModel.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleCheckboxAsync_ShouldToggleCheckbox_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var field = CreateCheckboxField("TestCheckbox", false);

        _formServiceMock
            .Setup(x => x.SetCheckboxStateAsync(field, true))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ToggleCheckboxAsync(field);

        // Assert
        field.IsChecked.Should().BeTrue();
        viewModel.IsModified.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleCheckboxAsync_ShouldUncheckOtherRadioButtons_InSameGroup()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();

        var radio1 = CreateRadioButtonField("Radio1", "Group1", true);
        var radio2 = CreateRadioButtonField("Radio2", "Group1", false);
        var radio3 = CreateRadioButtonField("Radio3", "Group1", false);
        var testFields = new List<PdfFormField> { radio1, radio2, radio3 };

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        _formServiceMock
            .Setup(x => x.SetCheckboxStateAsync(radio2, true))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ToggleCheckboxAsync(radio2);

        // Assert
        radio2.IsChecked.Should().BeTrue();
        radio1.IsChecked.Should().BeFalse();
        radio3.IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleCheckboxAsync_ShouldNotToggle_WhenFieldIsNotCheckbox()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var field = CreateTextField("TestField", "");

        // Act
        await viewModel.ToggleCheckboxAsync(field);

        // Assert
        _formServiceMock.Verify(
            x => x.SetCheckboxStateAsync(It.IsAny<PdfFormField>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task ToggleCheckboxAsync_ShouldHandleNullField_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.ToggleCheckboxAsync(null);

        // Assert
        _formServiceMock.Verify(
            x => x.SetCheckboxStateAsync(It.IsAny<PdfFormField>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveFormAsync_ShouldSaveForm_WhenValidationPasses()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.IsModified = true;

        _validationServiceMock
            .Setup(x => x.ValidateAllFields(It.IsAny<IReadOnlyList<PdfFormField>>()))
            .Returns(FormValidationResult.Success());

        _formServiceMock
            .Setup(x => x.SaveFormDataAsync(document, "/output/path.pdf"))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.SaveFormCommand.ExecuteAsync("/output/path.pdf");

        // Assert
        viewModel.IsModified.Should().BeFalse();
        viewModel.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public async Task SaveFormAsync_ShouldNotSave_WhenValidationFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.IsModified = true;

        var validationError = new FieldValidationError(
            "Field1",
            ValidationErrorType.RequiredFieldEmpty,
            "Field is required");

        _validationServiceMock
            .Setup(x => x.ValidateAllFields(It.IsAny<IReadOnlyList<PdfFormField>>()))
            .Returns(FormValidationResult.Failure(validationError));

        // Act
        await viewModel.SaveFormCommand.ExecuteAsync("/output/path.pdf");

        // Assert
        viewModel.IsModified.Should().BeTrue();
        viewModel.ValidationMessage.Should().NotBeNullOrEmpty();
        _formServiceMock.Verify(
            x => x.SaveFormDataAsync(It.IsAny<PdfDocument>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SaveFormCommand_CanExecute_ShouldBeFalse_WhenNotModified()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.SaveFormCommand.CanExecute("/output/path.pdf");

        // Assert
        canExecute.Should().BeFalse("form is not modified");
    }

    [Fact]
    public async Task SaveFormCommand_CanExecute_ShouldBeTrue_WhenModified()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.IsModified = true;

        // Act
        var canExecute = viewModel.SaveFormCommand.CanExecute("/output/path.pdf");

        // Assert
        canExecute.Should().BeTrue("form is modified and has fields");
    }

    [Fact]
    public void ValidateFormCommand_ShouldSetValidationMessage_WhenValidationFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var validationError = new FieldValidationError(
            "Field1",
            ValidationErrorType.RequiredFieldEmpty,
            "Field is required");

        _validationServiceMock
            .Setup(x => x.ValidateAllFields(It.IsAny<IReadOnlyList<PdfFormField>>()))
            .Returns(FormValidationResult.Failure(validationError));

        // Act
        viewModel.ValidateFormCommand.Execute(null);

        // Assert
        viewModel.ValidationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateFormCommand_ShouldClearValidationMessage_WhenValidationPasses()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ValidationMessage = "Previous error";

        _validationServiceMock
            .Setup(x => x.ValidateAllFields(It.IsAny<IReadOnlyList<PdfFormField>>()))
            .Returns(FormValidationResult.Success());

        // Act
        viewModel.ValidateFormCommand.Execute(null);

        // Assert
        viewModel.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public async Task FocusNextFieldCommand_ShouldMoveFocusToNextField()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.FocusedField = viewModel.FormFields[0];

        // Act
        viewModel.FocusNextFieldCommand.Execute(null);

        // Assert
        viewModel.FocusedField.Should().Be(viewModel.FormFields[1]);
    }

    [Fact]
    public async Task FocusNextFieldCommand_ShouldWrapAround_WhenAtLastField()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.FocusedField = viewModel.FormFields[2];

        // Act
        viewModel.FocusNextFieldCommand.Execute(null);

        // Assert
        viewModel.FocusedField.Should().Be(viewModel.FormFields[0]);
    }

    [Fact]
    public async Task FocusPreviousFieldCommand_ShouldMoveFocusToPreviousField()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.FocusedField = viewModel.FormFields[1];

        // Act
        viewModel.FocusPreviousFieldCommand.Execute(null);

        // Assert
        viewModel.FocusedField.Should().Be(viewModel.FormFields[0]);
    }

    [Fact]
    public async Task FocusPreviousFieldCommand_ShouldWrapAround_WhenAtFirstField()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testFields = CreateTestFormFields();

        _formServiceMock
            .Setup(x => x.GetFormFieldsAsync(document, 1))
            .ReturnsAsync(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        _formServiceMock
            .Setup(x => x.GetFieldsInTabOrder(testFields))
            .Returns(Result.Ok<IReadOnlyList<PdfFormField>>(testFields));

        await viewModel.LoadFormFieldsCommand.ExecuteAsync((document, 1));

        viewModel.FocusedField = viewModel.FormFields[0];

        // Act
        viewModel.FocusPreviousFieldCommand.Execute(null);

        // Assert
        viewModel.FocusedField.Should().Be(viewModel.FormFields[2]);
    }

    [Fact]
    public void FocusNextFieldCommand_CanExecute_ShouldBeFalse_WhenNoFields()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.FocusNextFieldCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("no fields loaded");
    }

    [Fact]
    public void Clear_ShouldResetAllProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.FormFields.Add(CreateTextField("Field1", "Value"));
        viewModel.FocusedField = viewModel.FormFields[0];
        viewModel.HasFormFields = true;
        viewModel.ValidationMessage = "Error";
        viewModel.IsModified = true;

        // Act
        viewModel.Clear();

        // Assert
        viewModel.FormFields.Should().BeEmpty();
        viewModel.FocusedField.Should().BeNull();
        viewModel.HasFormFields.Should().BeFalse();
        viewModel.ValidationMessage.Should().BeNull();
    }

    [Fact]
    public void IsModified_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(FormFieldViewModel.IsModified))
                eventRaised = true;
        };

        // Act
        viewModel.IsModified = true;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.IsModified.Should().BeTrue();
    }

    [Fact]
    public void ViewModel_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that the ViewModel can be instantiated and tested
        // without requiring WinUI runtime (headless testing)

        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Should().BeAssignableTo<INotifyPropertyChanged>();
    }

    private FormFieldViewModel CreateViewModel()
    {
        return new FormFieldViewModel(
            _formServiceMock.Object,
            _validationServiceMock.Object,
            _loggerMock.Object);
    }

    private PdfDocument CreateTestDocument()
    {
        return new PdfDocument
        {
            FilePath = "/test/document.pdf",
            PageCount = 10,
            Handle = IntPtr.Zero
        };
    }

    private List<PdfFormField> CreateTestFormFields()
    {
        return new List<PdfFormField>
        {
            CreateTextField("Field1", ""),
            CreateTextField("Field2", ""),
            CreateTextField("Field3", "")
        };
    }

    private PdfFormField CreateTextField(string name, string value)
    {
        return new PdfFormField
        {
            Name = name,
            Type = FormFieldType.Text,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 100, 20),
            Value = value,
            TabOrder = 0,
            IsRequired = false,
            IsReadOnly = false,
            NativeHandle = IntPtr.Zero
        };
    }

    private PdfFormField CreateCheckboxField(string name, bool isChecked)
    {
        return new PdfFormField
        {
            Name = name,
            Type = FormFieldType.Checkbox,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsChecked = isChecked,
            TabOrder = 0,
            IsRequired = false,
            IsReadOnly = false,
            NativeHandle = IntPtr.Zero
        };
    }

    private PdfFormField CreateRadioButtonField(string name, string groupName, bool isChecked)
    {
        return new PdfFormField
        {
            Name = name,
            Type = FormFieldType.RadioButton,
            PageNumber = 1,
            Bounds = new PdfRectangle(0, 0, 20, 20),
            IsChecked = isChecked,
            GroupName = groupName,
            TabOrder = 0,
            IsRequired = false,
            IsReadOnly = false,
            NativeHandle = IntPtr.Zero
        };
    }
}
