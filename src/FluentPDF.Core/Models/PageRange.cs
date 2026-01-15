namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a range of pages in a PDF document.
/// Pages are 1-based (first page is 1, not 0).
/// </summary>
public record PageRange
{
    /// <summary>
    /// Gets the starting page number (inclusive, 1-based).
    /// </summary>
    public required int StartPage { get; init; }

    /// <summary>
    /// Gets the ending page number (inclusive, 1-based).
    /// For single page ranges, EndPage equals StartPage.
    /// </summary>
    public required int EndPage { get; init; }

    /// <summary>
    /// Gets the total number of pages in this range.
    /// </summary>
    public int PageCount => EndPage - StartPage + 1;

    /// <summary>
    /// Validates that the page range is valid (positive numbers, StartPage &lt;= EndPage).
    /// </summary>
    /// <returns>True if the range is valid, false otherwise.</returns>
    public bool IsValid() => StartPage > 0 && EndPage > 0 && StartPage <= EndPage;
}
