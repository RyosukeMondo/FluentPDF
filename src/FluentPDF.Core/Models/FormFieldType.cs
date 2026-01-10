namespace FluentPDF.Core.Models;

/// <summary>
/// Represents the type of a form field in a PDF document.
/// Maps to PDFium FPDF_FORMFIELD_* constants.
/// </summary>
public enum FormFieldType
{
    /// <summary>
    /// Unknown or unsupported field type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Push button (non-data field).
    /// </summary>
    PushButton = 1,

    /// <summary>
    /// Checkbox field (on/off state).
    /// </summary>
    Checkbox = 2,

    /// <summary>
    /// Radio button (one selection per group).
    /// </summary>
    RadioButton = 3,

    /// <summary>
    /// Combo box (dropdown list).
    /// </summary>
    ComboBox = 4,

    /// <summary>
    /// List box (scrollable list).
    /// </summary>
    ListBox = 5,

    /// <summary>
    /// Text field (single or multi-line).
    /// </summary>
    Text = 6,

    /// <summary>
    /// Signature field.
    /// </summary>
    Signature = 7
}
