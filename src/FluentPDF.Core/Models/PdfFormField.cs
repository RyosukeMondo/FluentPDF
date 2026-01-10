namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a form field in a PDF document with all metadata and state.
/// Immutable except for Value and IsChecked which represent user interaction.
/// </summary>
public sealed class PdfFormField
{
    /// <summary>
    /// Gets the fully qualified field name (e.g., "Form.Section.FieldName").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of form field.
    /// </summary>
    public required FormFieldType Type { get; init; }

    /// <summary>
    /// Gets the 1-based page number where this field is located.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the bounding rectangle of the field in PDF coordinates.
    /// </summary>
    public required PdfRectangle Bounds { get; init; }

    /// <summary>
    /// Gets the tab order index for keyboard navigation.
    /// Lower values are visited first. -1 indicates no tab order.
    /// </summary>
    public int TabOrder { get; init; } = -1;

    /// <summary>
    /// Gets or sets the text value of the field (for Text, ComboBox, ListBox).
    /// Null for non-text field types.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the checked state (for Checkbox, RadioButton).
    /// Null for non-checkable field types.
    /// </summary>
    public bool? IsChecked { get; set; }

    /// <summary>
    /// Gets whether this field is required to be filled.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets whether this field is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets the maximum character length for text fields.
    /// Null indicates no limit.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the format mask/pattern for validation (e.g., regex pattern).
    /// Null indicates no format restriction.
    /// </summary>
    public string? FormatMask { get; init; }

    /// <summary>
    /// Gets the group name for radio buttons.
    /// All radio buttons with the same group name are mutually exclusive.
    /// Null for non-radio fields.
    /// </summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// Gets the native PDFium handle for this field.
    /// Used internally by the rendering layer.
    /// </summary>
    public IntPtr NativeHandle { get; init; }

    /// <summary>
    /// Validates that the field has all required properties set correctly.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required properties are invalid.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Field name is required.");
        }

        if (PageNumber < 1)
        {
            throw new InvalidOperationException(
                $"Invalid page number {PageNumber}. Must be >= 1.");
        }

        if (!Bounds.IsValid())
        {
            throw new InvalidOperationException(
                $"Invalid bounds for field '{Name}'. Right must be > Left and Top must be > Bottom.");
        }

        if (MaxLength is < 0)
        {
            throw new InvalidOperationException(
                $"Invalid MaxLength {MaxLength} for field '{Name}'. Must be >= 0 or null.");
        }
    }
}
