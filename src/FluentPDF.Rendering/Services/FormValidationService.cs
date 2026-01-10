using System.Text.RegularExpressions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for validating PDF form field values against their constraints.
/// Implements validation rules for required fields, length limits, format patterns, and read-only restrictions.
/// </summary>
public sealed class FormValidationService : IFormValidationService
{
    private readonly ILogger<FormValidationService> _logger;

    public FormValidationService(ILogger<FormValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public FormValidationResult ValidateField(PdfFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        var errors = new List<FieldValidationError>();

        // Validate required field
        if (field.IsRequired && IsFieldEmpty(field))
        {
            errors.Add(CreateRequiredFieldError(field));
        }

        // Validate max length for text fields
        if (field.Type == FormFieldType.Text &&
            field.MaxLength.HasValue &&
            !string.IsNullOrEmpty(field.Value) &&
            field.Value.Length > field.MaxLength.Value)
        {
            errors.Add(CreateMaxLengthError(field));
        }

        // Validate format mask for text fields
        if (field.Type == FormFieldType.Text &&
            !string.IsNullOrEmpty(field.FormatMask) &&
            !string.IsNullOrEmpty(field.Value) &&
            !ValidateFormatMask(field.Value, field.FormatMask))
        {
            errors.Add(CreateInvalidFormatError(field));
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Field '{FieldName}' validation failed with {ErrorCount} error(s)",
                field.Name,
                errors.Count);
        }

        return errors.Count == 0
            ? FormValidationResult.Success()
            : FormValidationResult.Failure(errors);
    }

    /// <inheritdoc />
    public FormValidationResult ValidateAllFields(
        IReadOnlyList<PdfFormField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var allErrors = new List<FieldValidationError>();

        foreach (var field in fields)
        {
            var result = ValidateField(field);
            if (!result.IsValid)
            {
                allErrors.AddRange(result.Errors);
            }
        }

        if (allErrors.Count > 0)
        {
            _logger.LogWarning(
                "Form validation failed. {TotalFields} fields checked, " +
                "{InvalidFields} fields invalid, {TotalErrors} total errors",
                fields.Count,
                allErrors.Select(e => e.FieldName).Distinct().Count(),
                allErrors.Count);
        }
        else
        {
            _logger.LogInformation(
                "Form validation succeeded. {TotalFields} fields validated",
                fields.Count);
        }

        return allErrors.Count == 0
            ? FormValidationResult.Success()
            : FormValidationResult.Failure(allErrors);
    }

    /// <inheritdoc />
    public FormValidationResult ValidateProposedValue(
        PdfFormField field,
        string? proposedValue)
    {
        ArgumentNullException.ThrowIfNull(field);

        var errors = new List<FieldValidationError>();

        // Check if trying to modify read-only field
        if (field.IsReadOnly && field.Value != proposedValue)
        {
            errors.Add(CreateReadOnlyError(field, proposedValue));
        }

        // Create temporary field with proposed value for validation
        var tempField = new PdfFormField
        {
            Name = field.Name,
            Type = field.Type,
            PageNumber = field.PageNumber,
            Bounds = field.Bounds,
            TabOrder = field.TabOrder,
            Value = proposedValue,
            IsChecked = field.IsChecked,
            IsRequired = field.IsRequired,
            IsReadOnly = field.IsReadOnly,
            MaxLength = field.MaxLength,
            FormatMask = field.FormatMask,
            GroupName = field.GroupName,
            NativeHandle = field.NativeHandle
        };

        // Validate the temporary field
        var validationResult = ValidateField(tempField);
        if (!validationResult.IsValid)
        {
            errors.AddRange(validationResult.Errors);
        }

        return errors.Count == 0
            ? FormValidationResult.Success()
            : FormValidationResult.Failure(errors);
    }

    private static bool IsFieldEmpty(PdfFormField field)
    {
        return field.Type switch
        {
            FormFieldType.Text => string.IsNullOrWhiteSpace(field.Value),
            FormFieldType.Checkbox => field.IsChecked is null or false,
            FormFieldType.RadioButton => field.IsChecked is null or false,
            _ => false
        };
    }

    private static bool ValidateFormatMask(string value, string formatMask)
    {
        try
        {
            var regex = new Regex(formatMask, RegexOptions.None,
                TimeSpan.FromMilliseconds(100));
            return regex.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // Invalid regex pattern - treat as validation failure
            return false;
        }
    }

    private static FieldValidationError CreateRequiredFieldError(
        PdfFormField field)
    {
        return new FieldValidationError
        {
            FieldName = field.Name,
            ErrorType = ValidationErrorType.RequiredFieldEmpty,
            Message = $"Field '{field.Name}' is required",
            AttemptedValue = field.Value
        };
    }

    private static FieldValidationError CreateMaxLengthError(
        PdfFormField field)
    {
        return new FieldValidationError
        {
            FieldName = field.Name,
            ErrorType = ValidationErrorType.MaxLengthExceeded,
            Message = $"Field '{field.Name}' exceeds maximum length of " +
                      $"{field.MaxLength} characters (current: {field.Value?.Length})",
            AttemptedValue = field.Value
        };
    }

    private static FieldValidationError CreateInvalidFormatError(
        PdfFormField field)
    {
        return new FieldValidationError
        {
            FieldName = field.Name,
            ErrorType = ValidationErrorType.InvalidFormat,
            Message = $"Field '{field.Name}' has an invalid format. " +
                      $"Expected pattern: {field.FormatMask}",
            AttemptedValue = field.Value
        };
    }

    private static FieldValidationError CreateReadOnlyError(
        PdfFormField field,
        string? proposedValue)
    {
        return new FieldValidationError
        {
            FieldName = field.Name,
            ErrorType = ValidationErrorType.ReadOnlyFieldModified,
            Message = $"Field '{field.Name}' is read-only and cannot be modified",
            AttemptedValue = proposedValue
        };
    }
}
