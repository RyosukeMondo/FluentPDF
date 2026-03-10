using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

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
