namespace FluentPDF.Validation.Models;

/// <summary>
/// Result from VeraPDF PDF/A compliance validation.
/// </summary>
public sealed class VeraPdfResult
{
    /// <summary>
    /// Gets a value indicating whether the PDF is compliant with the detected PDF/A flavour.
    /// </summary>
    public required bool IsCompliant { get; init; }

    /// <summary>
    /// Gets the PDF/A flavour (conformance level) detected or validated against.
    /// </summary>
    public required PdfFlavour Flavour { get; init; }

    /// <summary>
    /// Gets the validation status.
    /// </summary>
    public required ValidationStatus Status { get; init; }

    /// <summary>
    /// Gets the list of validation errors found.
    /// Empty if the PDF is compliant.
    /// </summary>
    public required IReadOnlyList<VeraPdfError> Errors { get; init; } = Array.Empty<VeraPdfError>();

    /// <summary>
    /// Gets the total number of validation checks performed.
    /// </summary>
    public int TotalChecks { get; init; }

    /// <summary>
    /// Gets the number of failed validation checks.
    /// </summary>
    public int FailedChecks { get; init; }
}
