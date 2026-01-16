using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for form field interactions in the PDF viewer.
/// Provides commands for loading, editing, validating, and saving form fields.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class FormFieldViewModel : ObservableObject
{
    private readonly IPdfFormService _formService;
    private readonly IFormValidationService _validationService;
    private readonly ILogger<FormFieldViewModel> _logger;
    private PdfDocument? _currentDocument;
    private int _currentPageNumber = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormFieldViewModel"/> class.
    /// </summary>
    /// <param name="formService">Service for form field operations.</param>
    /// <param name="validationService">Service for form field validation.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public FormFieldViewModel(
        IPdfFormService formService,
        IFormValidationService validationService,
        ILogger<FormFieldViewModel> logger)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("FormFieldViewModel initialized");
    }

    /// <summary>
    /// Gets the collection of form fields for the current page.
    /// Observable collection for UI binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PdfFormField> _formFields = new();

    /// <summary>
    /// Gets or sets the currently focused form field.
    /// Null if no field has focus.
    /// </summary>
    [ObservableProperty]
    private PdfFormField? _focusedField;

    /// <summary>
    /// Gets whether the current page has any form fields.
    /// </summary>
    [ObservableProperty]
    private bool _hasFormFields;

    /// <summary>
    /// Gets whether any form fields have been modified.
    /// Used for dirty tracking to prompt save before closing.
    /// </summary>
    [ObservableProperty]
    private bool _isModified;

    /// <summary>
    /// Gets the validation message to display in the UI.
    /// Null if validation passed or hasn't been run.
    /// </summary>
    [ObservableProperty]
    private string? _validationMessage;

    /// <summary>
    /// Gets whether there are validation errors to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasValidationErrors;

    /// <summary>
    /// Gets whether a form operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets the collection of validation errors.
    /// Observable collection for UI binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FieldValidationError> _validationErrors = new();

    /// <summary>
    /// Loads all form fields for the specified page.
    /// Updates FormFields collection and HasFormFields flag.
    /// </summary>
    /// <param name="parameter">A tuple of (PdfDocument, int) containing the document and 1-based page number.</param>
    [RelayCommand]
    private async Task LoadFormFieldsAsync(object? parameter)
    {
        if (parameter is not (PdfDocument document, int pageNumber))
        {
            _logger.LogWarning("LoadFormFieldsAsync called with invalid parameter");
            return;
        }

        _logger.LogInformation(
            "Loading form fields. PageNumber={PageNumber}",
            pageNumber);

        try
        {
            IsLoading = true;
            _currentDocument = document;
            _currentPageNumber = pageNumber;

            var result = await _formService.GetFormFieldsAsync(document, pageNumber);

            if (result.IsSuccess)
            {
                var sortedResult = _formService.GetFieldsInTabOrder(result.Value);

                if (sortedResult.IsSuccess)
                {
                    FormFields.Clear();
                    foreach (var field in sortedResult.Value)
                    {
                        FormFields.Add(field);
                    }

                    HasFormFields = FormFields.Count > 0;

                    _logger.LogInformation(
                        "Form fields loaded. Count={Count}",
                        FormFields.Count);
                }
                else
                {
                    _logger.LogError(
                        "Failed to sort fields: {Errors}",
                        sortedResult.Errors);
                    FormFields.Clear();
                    HasFormFields = false;
                }
            }
            else
            {
                _logger.LogError(
                    "Failed to load form fields: {Errors}",
                    result.Errors);
                FormFields.Clear();
                HasFormFields = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading form fields");
            FormFields.Clear();
            HasFormFields = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Updates the value of a text field and validates it.
    /// Marks form as modified if validation passes.
    /// </summary>
    /// <param name="parameter">Tuple of (field, newValue).</param>
    [RelayCommand]
    public async Task UpdateFieldValueAsync(object? parameter)
    {
        if (parameter is not (PdfFormField field, string newValue))
        {
            _logger.LogWarning("UpdateFieldValueAsync called with invalid parameter");
            return;
        }

        _logger.LogInformation(
            "Updating field value. FieldName={FieldName}, NewValue={NewValue}",
            field.Name, newValue);

        try
        {
            var validationResult = _validationService.ValidateProposedValue(
                field,
                newValue);

            if (!validationResult.IsValid)
            {
                ValidationMessage = validationResult.GetSummaryMessage();
                HasValidationErrors = true;
                ValidationErrors.Clear();
                foreach (var error in validationResult.Errors)
                {
                    ValidationErrors.Add(error);
                }
                _logger.LogWarning(
                    "Field validation failed. FieldName={FieldName}, Errors={Errors}",
                    field.Name, ValidationMessage);
                return;
            }

            var result = await _formService.SetFieldValueAsync(field, newValue);

            if (result.IsSuccess)
            {
                field.Value = newValue;
                IsModified = true;
                ValidationMessage = null;
                HasValidationErrors = false;
                ValidationErrors.Clear();

                _logger.LogInformation(
                    "Field value updated. FieldName={FieldName}",
                    field.Name);
            }
            else
            {
                ValidationMessage = result.Errors[0].Message;
                _logger.LogError(
                    "Failed to update field value: {Errors}",
                    result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating field value");
            ValidationMessage = $"Error updating field: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggles the checked state of a checkbox or radio button.
    /// For radio buttons, unchecks other buttons in the same group.
    /// </summary>
    /// <param name="field">The checkbox or radio button field to toggle.</param>
    [RelayCommand]
    public async Task ToggleCheckboxAsync(PdfFormField? field)
    {
        if (field == null)
        {
            _logger.LogWarning("ToggleCheckboxAsync called with null field");
            return;
        }

        if (field.Type != FormFieldType.Checkbox &&
            field.Type != FormFieldType.RadioButton)
        {
            _logger.LogWarning(
                "ToggleCheckboxAsync called on non-checkbox field. Type={Type}",
                field.Type);
            return;
        }

        _logger.LogInformation(
            "Toggling checkbox. FieldName={FieldName}, CurrentState={CurrentState}",
            field.Name, field.IsChecked);

        try
        {
            var newState = !(field.IsChecked ?? false);
            var result = await _formService.SetCheckboxStateAsync(field, newState);

            if (result.IsSuccess)
            {
                field.IsChecked = newState;
                IsModified = true;

                if (field.Type == FormFieldType.RadioButton &&
                    newState &&
                    !string.IsNullOrEmpty(field.GroupName))
                {
                    foreach (var otherField in FormFields)
                    {
                        if (otherField != field &&
                            otherField.Type == FormFieldType.RadioButton &&
                            otherField.GroupName == field.GroupName)
                        {
                            otherField.IsChecked = false;
                        }
                    }
                }

                _logger.LogInformation(
                    "Checkbox toggled. FieldName={FieldName}, NewState={NewState}",
                    field.Name, newState);
            }
            else
            {
                ValidationMessage = result.Errors[0].Message;
                _logger.LogError(
                    "Failed to toggle checkbox: {Errors}",
                    result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error toggling checkbox");
            ValidationMessage = $"Error toggling checkbox: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates all form fields and saves the document if validation passes.
    /// </summary>
    /// <param name="outputPath">The file path to save the document to.</param>
    [RelayCommand(CanExecute = nameof(CanSaveForm))]
    private async Task SaveFormAsync(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath) || _currentDocument == null)
        {
            _logger.LogWarning(
                "SaveFormAsync called with invalid parameters. OutputPath={OutputPath}, HasDocument={HasDocument}",
                outputPath, _currentDocument != null);
            return;
        }

        _logger.LogInformation("Saving form. OutputPath={OutputPath}", outputPath);

        try
        {
            IsLoading = true;

            var validationResult = _validationService.ValidateAllFields(
                FormFields.ToList());

            if (!validationResult.IsValid)
            {
                ValidationMessage = validationResult.GetSummaryMessage();
                HasValidationErrors = true;
                ValidationErrors.Clear();
                foreach (var error in validationResult.Errors)
                {
                    ValidationErrors.Add(error);
                }
                _logger.LogWarning(
                    "Form validation failed. Errors={Errors}",
                    ValidationMessage);
                return;
            }

            var result = await _formService.SaveFormDataAsync(
                _currentDocument,
                outputPath);

            if (result.IsSuccess)
            {
                IsModified = false;
                ValidationMessage = null;
                HasValidationErrors = false;
                ValidationErrors.Clear();

                _logger.LogInformation(
                    "Form saved successfully. OutputPath={OutputPath}",
                    outputPath);
            }
            else
            {
                ValidationMessage = result.Errors[0].Message;
                HasValidationErrors = true;
                _logger.LogError(
                    "Failed to save form: {Errors}",
                    result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving form");
            ValidationMessage = $"Error saving form: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Determines whether the SaveForm command can execute.
    /// </summary>
    private bool CanSaveForm() =>
        IsModified &&
        !IsLoading &&
        _currentDocument != null &&
        HasFormFields;

    /// <summary>
    /// Validates all form fields without saving.
    /// Updates ValidationMessage with results.
    /// </summary>
    [RelayCommand]
    private void ValidateForm()
    {
        _logger.LogInformation("Validating form");

        try
        {
            var validationResult = _validationService.ValidateAllFields(
                FormFields.ToList());

            ValidationMessage = validationResult.GetSummaryMessage();
            HasValidationErrors = !validationResult.IsValid;
            ValidationErrors.Clear();
            foreach (var error in validationResult.Errors)
            {
                ValidationErrors.Add(error);
            }

            _logger.LogInformation(
                "Form validation completed. IsValid={IsValid}",
                validationResult.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating form");
            ValidationMessage = $"Error validating form: {ex.Message}";
            HasValidationErrors = true;
        }
    }

    /// <summary>
    /// Moves focus to the next field in tab order.
    /// Wraps around to the first field if at the end.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateFields))]
    private void FocusNextField()
    {
        if (!HasFormFields)
        {
            return;
        }

        _logger.LogDebug("Moving focus to next field");

        try
        {
            if (FocusedField == null)
            {
                FocusedField = FormFields.FirstOrDefault();
            }
            else
            {
                var currentIndex = FormFields.IndexOf(FocusedField);
                if (currentIndex >= 0 && currentIndex < FormFields.Count - 1)
                {
                    FocusedField = FormFields[currentIndex + 1];
                }
                else
                {
                    FocusedField = FormFields.FirstOrDefault();
                }
            }

            _logger.LogDebug(
                "Focus moved to field. FieldName={FieldName}",
                FocusedField?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving focus to next field");
        }
    }

    /// <summary>
    /// Moves focus to the previous field in tab order.
    /// Wraps around to the last field if at the beginning.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateFields))]
    private void FocusPreviousField()
    {
        if (!HasFormFields)
        {
            return;
        }

        _logger.LogDebug("Moving focus to previous field");

        try
        {
            if (FocusedField == null)
            {
                FocusedField = FormFields.LastOrDefault();
            }
            else
            {
                var currentIndex = FormFields.IndexOf(FocusedField);
                if (currentIndex > 0)
                {
                    FocusedField = FormFields[currentIndex - 1];
                }
                else
                {
                    FocusedField = FormFields.LastOrDefault();
                }
            }

            _logger.LogDebug(
                "Focus moved to field. FieldName={FieldName}",
                FocusedField?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving focus to previous field");
        }
    }

    /// <summary>
    /// Determines whether field navigation commands can execute.
    /// </summary>
    private bool CanNavigateFields() => HasFormFields && !IsLoading;

    /// <summary>
    /// Focuses the specified form field by name.
    /// Used when clicking on validation error "Go to field" buttons.
    /// </summary>
    /// <param name="fieldName">The name of the field to focus.</param>
    [RelayCommand]
    private void FocusFieldByName(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        _logger.LogDebug("Focusing field by name. FieldName={FieldName}", fieldName);

        try
        {
            var field = FormFields.FirstOrDefault(
                f => f.Name.Equals(fieldName, StringComparison.Ordinal));

            if (field != null)
            {
                FocusedField = field;
                _logger.LogDebug("Field focused. FieldName={FieldName}", fieldName);
            }
            else
            {
                _logger.LogWarning(
                    "Field not found. FieldName={FieldName}",
                    fieldName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error focusing field by name");
        }
    }

    /// <summary>
    /// Clears all form fields and resets state.
    /// Used when navigating to a different page or closing the document.
    /// </summary>
    public void Clear()
    {
        _logger.LogDebug("Clearing form fields");

        FormFields.Clear();
        FocusedField = null;
        HasFormFields = false;
        ValidationMessage = null;
        HasValidationErrors = false;
        ValidationErrors.Clear();
        _currentDocument = null;
        _currentPageNumber = 1;
    }
}
