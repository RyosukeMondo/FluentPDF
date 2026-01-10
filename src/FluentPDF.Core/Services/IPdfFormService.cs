using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF form field detection, manipulation, and persistence.
/// Provides methods for reading form metadata, updating field values, and saving form data.
/// </summary>
public interface IPdfFormService
{
    /// <summary>
    /// Gets all form fields on a specific page of the PDF document.
    /// Fields are returned sorted by tab order for keyboard navigation.
    /// </summary>
    /// <param name="document">The PDF document to query.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <returns>
    /// A result containing a read-only list of form fields, or an error if the operation fails.
    /// Returns an empty list if the page has no form fields.
    /// </returns>
    Task<Result<IReadOnlyList<PdfFormField>>> GetFormFieldsAsync(
        PdfDocument document,
        int pageNumber);

    /// <summary>
    /// Finds the form field at the specified page coordinates (hit testing).
    /// Useful for mouse hover and click interactions.
    /// </summary>
    /// <param name="document">The PDF document to query.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="x">X coordinate in page coordinate system.</param>
    /// <param name="y">Y coordinate in page coordinate system.</param>
    /// <returns>
    /// A result containing the form field at the point, or null if no field exists there.
    /// Returns an error if the operation fails.
    /// </returns>
    Task<Result<PdfFormField?>> GetFormFieldAtPointAsync(
        PdfDocument document,
        int pageNumber,
        double x,
        double y);

    /// <summary>
    /// Updates the text value of a form field.
    /// Validates against max length, read-only status, and format constraints.
    /// </summary>
    /// <param name="field">The form field to update.</param>
    /// <param name="value">The new value to set.</param>
    /// <returns>
    /// A successful result if the value was set, or an error if validation fails.
    /// </returns>
    Task<Result> SetFieldValueAsync(PdfFormField field, string value);

    /// <summary>
    /// Sets the checked state of a checkbox or radio button.
    /// For radio buttons, automatically unchecks other buttons in the same group.
    /// </summary>
    /// <param name="field">The form field (checkbox or radio button) to update.</param>
    /// <param name="isChecked">True to check the field; false to uncheck.</param>
    /// <returns>
    /// A successful result if the state was updated, or an error if the operation fails.
    /// </returns>
    Task<Result> SetCheckboxStateAsync(PdfFormField field, bool isChecked);

    /// <summary>
    /// Saves the PDF document with all form field modifications persisted.
    /// Uses PDFium's save API to write the updated document to disk.
    /// </summary>
    /// <param name="document">The PDF document with modified form fields.</param>
    /// <param name="outputPath">The file path where the document should be saved.</param>
    /// <returns>
    /// A successful result if the document was saved, or an error if the save operation fails.
    /// </returns>
    Task<Result> SaveFormDataAsync(PdfDocument document, string outputPath);

    /// <summary>
    /// Sorts form fields by their tab order for keyboard navigation.
    /// Falls back to spatial ordering (top-to-bottom, left-to-right) if tab order is not defined.
    /// </summary>
    /// <param name="fields">The collection of form fields to sort.</param>
    /// <returns>
    /// A result containing the sorted list of fields, or an error if the operation fails.
    /// </returns>
    Result<IReadOnlyList<PdfFormField>> GetFieldsInTabOrder(
        IReadOnlyList<PdfFormField> fields);
}
