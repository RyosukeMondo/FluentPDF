using FluentPDF.Core.Models;
using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a FieldValidationError to a user-friendly error message string.
/// Formats the message based on the error type.
/// </summary>
public class ValidationErrorToStringConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not FieldValidationError error)
        {
            return string.Empty;
        }

        return error.ErrorType switch
        {
            ValidationErrorType.RequiredFieldEmpty =>
                $"Field '{error.FieldName}' is required",

            ValidationErrorType.MaxLengthExceeded =>
                $"Field '{error.FieldName}' exceeds maximum length",

            ValidationErrorType.InvalidFormat =>
                $"Field '{error.FieldName}' has invalid format",

            ValidationErrorType.ReadOnlyFieldModified =>
                $"Field '{error.FieldName}' is read-only and cannot be modified",

            ValidationErrorType.InvalidValue =>
                $"Field '{error.FieldName}' has an invalid value",

            _ => error.Message
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        string language)
    {
        throw new NotImplementedException(
            "ConvertBack is not supported for ValidationErrorToStringConverter");
    }
}
