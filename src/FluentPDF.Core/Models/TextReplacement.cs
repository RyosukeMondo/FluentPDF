namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a text replacement operation in a PDF document.
/// Contains the original match, replacement text, and annotation details for undo support.
/// </summary>
/// <param name="Match">The original search match that was replaced.</param>
/// <param name="ReplacementText">The new text that replaced the match.</param>
/// <param name="PageNumber">The zero-based page number where replacement occurred.</param>
/// <param name="AnnotationIndex">The annotation index created for this replacement (for undo support).</param>
/// <param name="Timestamp">When the replacement was performed.</param>
public readonly record struct TextReplacement(
    SearchMatch Match,
    string ReplacementText,
    int PageNumber,
    int AnnotationIndex,
    DateTime Timestamp)
{
    /// <summary>
    /// Gets the original text that was replaced.
    /// </summary>
    public string OriginalText => Match.Text;

    /// <summary>
    /// Gets the bounding box where the replacement occurred.
    /// </summary>
    public PdfRectangle BoundingBox => Match.BoundingBox;

    /// <summary>
    /// Validates that the replacement has valid properties.
    /// </summary>
    /// <returns>True if all properties are within valid ranges.</returns>
    public bool IsValid()
    {
        return Match.IsValid()
            && !string.IsNullOrEmpty(ReplacementText)
            && PageNumber >= 0
            && AnnotationIndex >= 0
            && Timestamp != default;
    }
}
