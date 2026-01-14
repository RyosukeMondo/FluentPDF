namespace FluentPDF.Validation.Models;

/// <summary>
/// Represents a PDF/A compliance validation error from VeraPDF.
/// </summary>
public sealed class VeraPdfError
{
    /// <summary>
    /// Gets the validation rule reference (e.g., "6.1.2-1").
    /// </summary>
    public required string RuleReference { get; init; }

    /// <summary>
    /// Gets the human-readable error description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the page number where the error occurred (1-based).
    /// Null if the error is not associated with a specific page.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Gets the object reference in the PDF structure.
    /// </summary>
    public string? ObjectReference { get; init; }
}
