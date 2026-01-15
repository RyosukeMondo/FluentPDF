using FluentPDF.Core.Models;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for validating PDF form field values against their constraints.
/// Provides validation for required fields, length limits, format patterns, and read-only restrictions.
/// </summary>
public interface IFormValidationService
{
    /// <summary>
    /// Validates a single form field against its constraints.
    /// Checks for required field empty, max length exceeded, format violations, and read-only modifications.
    /// </summary>
    /// <param name="field">The form field to validate.</param>
    /// <returns>
    /// A validation result indicating success or containing validation errors.
    /// </returns>
    FormValidationResult ValidateField(PdfFormField field);

    /// <summary>
    /// Validates all form fields in a collection.
    /// Aggregates all validation errors from individual fields.
    /// </summary>
    /// <param name="fields">The collection of form fields to validate.</param>
    /// <returns>
    /// A validation result indicating success or containing all validation errors.
    /// Returns success if all fields are valid.
    /// </returns>
    FormValidationResult ValidateAllFields(IReadOnlyList<PdfFormField> fields);

    /// <summary>
    /// Validates that a proposed value is acceptable for a form field.
    /// Does not modify the field; only checks if the value would be valid.
    /// </summary>
    /// <param name="field">The form field to validate against.</param>
    /// <param name="proposedValue">The value to validate.</param>
    /// <returns>
    /// A validation result indicating whether the proposed value is valid.
    /// </returns>
    FormValidationResult ValidateProposedValue(
        PdfFormField field,
        string? proposedValue);
}
