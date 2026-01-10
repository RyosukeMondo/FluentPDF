namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a single text match found during a PDF search operation.
/// Contains the match location, text content, and bounding box coordinates.
/// </summary>
/// <param name="PageNumber">The zero-based page number where the match was found.</param>
/// <param name="CharIndex">The character index within the page where the match starts.</param>
/// <param name="Length">The length of the matched text in characters.</param>
/// <param name="Text">The actual matched text content.</param>
/// <param name="BoundingBox">The bounding rectangle of the match in PDF coordinates.</param>
public readonly record struct SearchMatch(
    int PageNumber,
    int CharIndex,
    int Length,
    string Text,
    PdfRectangle BoundingBox)
{
    /// <summary>
    /// Gets the character index immediately after the match.
    /// </summary>
    public int EndIndex => CharIndex + Length;

    /// <summary>
    /// Validates that the search match has valid properties.
    /// </summary>
    /// <returns>True if all properties are within valid ranges.</returns>
    public bool IsValid()
    {
        return PageNumber >= 0
            && CharIndex >= 0
            && Length > 0
            && !string.IsNullOrEmpty(Text)
            && Text.Length == Length
            && BoundingBox.IsValid();
    }

    /// <summary>
    /// Determines if this match overlaps with another match on the same page.
    /// </summary>
    /// <param name="other">The other search match to compare with.</param>
    /// <returns>True if both matches are on the same page and their character ranges overlap.</returns>
    public bool OverlapsWith(SearchMatch other)
    {
        if (PageNumber != other.PageNumber)
        {
            return false;
        }

        return CharIndex < other.EndIndex && other.CharIndex < EndIndex;
    }
}
