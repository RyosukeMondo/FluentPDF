namespace FluentPDF.Core.Models;

/// <summary>
/// Represents the result of validating one or more form fields.
/// Contains a flag indicating overall validity and a collection of validation errors.
/// </summary>
public sealed class FormValidationResult
{
    /// <summary>
    /// Gets whether all validated fields passed validation.
    /// True if no errors were found; false otherwise.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// Empty if all fields are valid.
    /// </summary>
    public IReadOnlyList<FieldValidationError> Errors { get; init; } =
        Array.Empty<FieldValidationError>();

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static FormValidationResult Success() =>
        new FormValidationResult();

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors that occurred.</param>
    /// <returns>A validation result containing the errors.</returns>
    public static FormValidationResult Failure(
        params FieldValidationError[] errors) =>
        new FormValidationResult { Errors = errors };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors that occurred.</param>
    /// <returns>A validation result containing the errors.</returns>
    public static FormValidationResult Failure(
        IEnumerable<FieldValidationError> errors) =>
        new FormValidationResult { Errors = errors.ToList() };

    /// <summary>
    /// Gets a summary message describing all validation errors.
    /// Returns null if validation succeeded.
    /// </summary>
    public string? GetSummaryMessage()
    {
        if (IsValid)
        {
            return null;
        }

        if (Errors.Count == 1)
        {
            return Errors[0].Message;
        }

        return $"{Errors.Count} validation errors found: " +
               string.Join("; ", Errors.Select(e => e.Message));
    }
}
