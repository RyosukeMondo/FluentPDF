namespace FluentPDF.Core.Models;

/// <summary>
/// Represents metadata about a single PDF page.
/// </summary>
public sealed class PdfPage
{
    private int _pageNumber;

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when page number is less than 1.</exception>
    public required int PageNumber
    {
        get => _pageNumber;
        init
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(PageNumber), value, "Page number must be greater than 0.");
            }
            _pageNumber = value;
        }
    }

    /// <summary>
    /// Gets the page width in points (1/72 inch).
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the page height in points (1/72 inch).
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Gets the aspect ratio of the page (Width / Height).
    /// </summary>
    public double AspectRatio => Width / Height;
}
