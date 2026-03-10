using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

public partial class FormFieldViewModel
{
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
}
