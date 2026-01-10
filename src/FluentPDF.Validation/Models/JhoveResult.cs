namespace FluentPDF.Validation.Models;

/// <summary>
/// Result from JHOVE format validation and characterization.
/// </summary>
public sealed class JhoveResult
{
    /// <summary>
    /// Gets the PDF format version (e.g., "1.7", "1.4").
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// Gets the validity status (Well-Formed, Valid, Not-Valid).
    /// </summary>
    public required string Validity { get; init; }

    /// <summary>
    /// Gets the validation status derived from validity.
    /// </summary>
    public required ValidationStatus Status { get; init; }

    /// <summary>
    /// Gets the document title metadata.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the document author metadata.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the document creation date.
    /// </summary>
    public DateTime? CreationDate { get; init; }

    /// <summary>
    /// Gets the document modification date.
    /// </summary>
    public DateTime? ModificationDate { get; init; }

    /// <summary>
    /// Gets the page count.
    /// </summary>
    public int? PageCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the PDF is encrypted.
    /// </summary>
    public bool IsEncrypted { get; init; }

    /// <summary>
    /// Gets the list of validation messages.
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether the PDF is well-formed and valid.
    /// </summary>
    public bool IsValid => Validity.Equals("Valid", StringComparison.OrdinalIgnoreCase) ||
                           Validity.Equals("Well-Formed", StringComparison.OrdinalIgnoreCase) ||
                           Validity.Equals("Well-Formed and valid", StringComparison.OrdinalIgnoreCase);
}
