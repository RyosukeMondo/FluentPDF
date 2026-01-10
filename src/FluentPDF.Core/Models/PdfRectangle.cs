namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a rectangle in PDF coordinate space (origin at bottom-left).
/// Used for form field bounds and other positional elements.
/// </summary>
/// <param name="Left">The left coordinate.</param>
/// <param name="Bottom">The bottom coordinate.</param>
/// <param name="Right">The right coordinate.</param>
/// <param name="Top">The top coordinate.</param>
public readonly record struct PdfRectangle(
    double Left,
    double Bottom,
    double Right,
    double Top)
{
    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    public double Width => Right - Left;

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    public double Height => Top - Bottom;

    /// <summary>
    /// Determines if this rectangle contains the specified point.
    /// </summary>
    /// <param name="x">The x-coordinate to test.</param>
    /// <param name="y">The y-coordinate to test.</param>
    /// <returns>True if the point is within the rectangle bounds.</returns>
    public bool Contains(double x, double y)
    {
        return x >= Left && x <= Right && y >= Bottom && y <= Top;
    }

    /// <summary>
    /// Validates that the rectangle has valid dimensions.
    /// </summary>
    /// <returns>True if Right > Left and Top > Bottom.</returns>
    public bool IsValid()
    {
        return Right > Left && Top > Bottom;
    }
}
