namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a validation error for a specific form field.
/// Contains the field name, error type, and user-friendly error message.
/// </summary>
public sealed class FieldValidationError
{
    /// <summary>
    /// Gets the name of the field that failed validation.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Gets the type of validation error.
    /// </summary>
    public required ValidationErrorType ErrorType { get; init; }

    /// <summary>
    /// Gets the user-friendly error message describing the validation failure.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the current value that failed validation (if applicable).
    /// </summary>
    public string? AttemptedValue { get; init; }
}

/// <summary>
/// Defines the types of validation errors that can occur.
/// </summary>
public enum ValidationErrorType
{
    /// <summary>
    /// A required field was left empty.
    /// </summary>
    RequiredFieldEmpty,

    /// <summary>
    /// The field value exceeds the maximum allowed length.
    /// </summary>
    MaxLengthExceeded,

    /// <summary>
    /// The field value does not match the required format/pattern.
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// Attempted to modify a read-only field.
    /// </summary>
    ReadOnlyFieldModified,

    /// <summary>
    /// The field value is invalid for the field type.
    /// </summary>
    InvalidValue
}
