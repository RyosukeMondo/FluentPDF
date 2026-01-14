namespace FluentPDF.Validation.Models;

/// <summary>
/// Result from QPDF structural validation.
/// </summary>
public sealed class QpdfResult
{
    /// <summary>
    /// Gets the validation status (Pass/Fail).
    /// </summary>
    public required ValidationStatus Status { get; init; }

    /// <summary>
    /// Gets the list of structural errors found by QPDF.
    /// Empty if validation passed.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of warnings (non-critical issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether the PDF passed structural validation.
    /// </summary>
    public bool IsValid => Status == ValidationStatus.Pass;
}
