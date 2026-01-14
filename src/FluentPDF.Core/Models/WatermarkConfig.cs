using System.Drawing;

namespace FluentPDF.Core.Models;

/// <summary>
/// Configuration for text-based watermarks.
/// Allows customization of text content, font, color, and positioning.
/// </summary>
public class TextWatermarkConfig
{
    /// <summary>
    /// Gets or sets the text content of the watermark.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the font family name for the watermark text.
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// Gets or sets the font size in points (12-144).
    /// </summary>
    public float FontSize { get; set; } = 72f;

    /// <summary>
    /// Gets or sets the color of the watermark text.
    /// </summary>
    public Color Color { get; set; } = Color.Gray;

    /// <summary>
    /// Gets or sets the opacity of the watermark (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the rotation angle in degrees (-180 to 180).
    /// Positive values rotate clockwise, negative values counterclockwise.
    /// </summary>
    public float RotationDegrees { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the position of the watermark on the page.
    /// </summary>
    public WatermarkPosition Position { get; set; } = WatermarkPosition.Center;

    /// <summary>
    /// Gets or sets custom X coordinate when Position is Custom (0-100 percentage).
    /// </summary>
    public float CustomX { get; set; } = 50f;

    /// <summary>
    /// Gets or sets custom Y coordinate when Position is Custom (0-100 percentage).
    /// </summary>
    public float CustomY { get; set; } = 50f;

    /// <summary>
    /// Gets or sets whether the watermark should render behind page content.
    /// When false, watermark renders above content.
    /// </summary>
    public bool BehindContent { get; set; } = true;
}

/// <summary>
/// Configuration for image-based watermarks.
/// Allows customization of image source, scaling, and positioning.
/// </summary>
public class ImageWatermarkConfig
{
    /// <summary>
    /// Gets or sets the file path to the watermark image.
    /// Supports PNG, JPG, BMP formats. PNG alpha channel is preserved.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scale factor for the image (0.1 to 2.0).
    /// 1.0 means original size, values less than 1.0 shrink, greater than 1.0 enlarge.
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the opacity of the watermark (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the rotation angle in degrees (-180 to 180).
    /// Positive values rotate clockwise, negative values counterclockwise.
    /// </summary>
    public float RotationDegrees { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the position of the watermark on the page.
    /// </summary>
    public WatermarkPosition Position { get; set; } = WatermarkPosition.Center;

    /// <summary>
    /// Gets or sets custom X coordinate when Position is Custom (0-100 percentage).
    /// </summary>
    public float CustomX { get; set; } = 50f;

    /// <summary>
    /// Gets or sets custom Y coordinate when Position is Custom (0-100 percentage).
    /// </summary>
    public float CustomY { get; set; } = 50f;

    /// <summary>
    /// Gets or sets whether the watermark should render behind page content.
    /// When false, watermark renders above content.
    /// </summary>
    public bool BehindContent { get; set; } = true;
}

/// <summary>
/// Defines preset positions for watermark placement on a page.
/// </summary>
public enum WatermarkPosition
{
    /// <summary>
    /// Center of the page.
    /// </summary>
    Center = 0,

    /// <summary>
    /// Top-left corner of the page.
    /// </summary>
    TopLeft = 1,

    /// <summary>
    /// Top-right corner of the page.
    /// </summary>
    TopRight = 2,

    /// <summary>
    /// Bottom-left corner of the page.
    /// </summary>
    BottomLeft = 3,

    /// <summary>
    /// Bottom-right corner of the page.
    /// </summary>
    BottomRight = 4,

    /// <summary>
    /// Custom position using X/Y coordinates.
    /// </summary>
    Custom = 5
}

/// <summary>
/// Defines the type of page range for watermark application.
/// </summary>
public enum PageRangeType
{
    /// <summary>
    /// All pages in the document.
    /// </summary>
    All = 0,

    /// <summary>
    /// Only the currently active page.
    /// </summary>
    CurrentPage = 1,

    /// <summary>
    /// Custom page range (e.g., "1-5, 10, 15-20").
    /// </summary>
    Custom = 2,

    /// <summary>
    /// All odd-numbered pages.
    /// </summary>
    OddPages = 3,

    /// <summary>
    /// All even-numbered pages.
    /// </summary>
    EvenPages = 4
}

/// <summary>
/// Represents a range of pages for watermark application.
/// Supports various range types including all pages, specific pages, odd/even pages.
/// </summary>
public class WatermarkPageRange
{
    /// <summary>
    /// Gets or sets the type of page range.
    /// </summary>
    public PageRangeType Type { get; set; } = PageRangeType.All;

    /// <summary>
    /// Gets or sets the specific pages when Type is Custom.
    /// Pages are 1-based (first page is 1).
    /// </summary>
    public int[] SpecificPages { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the current page number when Type is CurrentPage.
    /// Page is 1-based (first page is 1).
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Creates a page range for all pages.
    /// </summary>
    public static WatermarkPageRange All => new() { Type = PageRangeType.All };

    /// <summary>
    /// Creates a page range for odd pages.
    /// </summary>
    public static WatermarkPageRange OddPages => new() { Type = PageRangeType.OddPages };

    /// <summary>
    /// Creates a page range for even pages.
    /// </summary>
    public static WatermarkPageRange EvenPages => new() { Type = PageRangeType.EvenPages };

    /// <summary>
    /// Creates a page range for the current page.
    /// </summary>
    /// <param name="pageNumber">The current page number (1-based).</param>
    public static WatermarkPageRange Current(int pageNumber) => new()
    {
        Type = PageRangeType.CurrentPage,
        CurrentPage = pageNumber
    };

    /// <summary>
    /// Parses a page range string into a WatermarkPageRange object.
    /// Supports formats like "1-5, 10, 15-20".
    /// </summary>
    /// <param name="rangeString">The page range string to parse.</param>
    /// <returns>A WatermarkPageRange object representing the parsed range.</returns>
    /// <exception cref="ArgumentException">Thrown when the range string is invalid.</exception>
    public static WatermarkPageRange Parse(string rangeString)
    {
        if (string.IsNullOrWhiteSpace(rangeString))
        {
            throw new ArgumentException("Page range string cannot be empty.", nameof(rangeString));
        }

        var pages = new List<int>();
        var parts = rangeString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var rangeParts = part.Split('-', StringSplitOptions.TrimEntries);
                if (rangeParts.Length != 2 ||
                    !int.TryParse(rangeParts[0], out var start) ||
                    !int.TryParse(rangeParts[1], out var end) ||
                    start < 1 || end < 1 || start > end)
                {
                    throw new ArgumentException($"Invalid page range: {part}", nameof(rangeString));
                }

                for (int i = start; i <= end; i++)
                {
                    if (!pages.Contains(i))
                    {
                        pages.Add(i);
                    }
                }
            }
            else
            {
                if (!int.TryParse(part, out var pageNum) || pageNum < 1)
                {
                    throw new ArgumentException($"Invalid page number: {part}", nameof(rangeString));
                }

                if (!pages.Contains(pageNum))
                {
                    pages.Add(pageNum);
                }
            }
        }

        pages.Sort();

        return new WatermarkPageRange
        {
            Type = PageRangeType.Custom,
            SpecificPages = pages.ToArray()
        };
    }

    /// <summary>
    /// Gets the list of page numbers this range represents for a given document.
    /// </summary>
    /// <param name="totalPages">Total number of pages in the document.</param>
    /// <returns>Array of page numbers (1-based).</returns>
    public int[] GetPages(int totalPages)
    {
        return Type switch
        {
            PageRangeType.All => Enumerable.Range(1, totalPages).ToArray(),
            PageRangeType.CurrentPage => new[] { CurrentPage },
            PageRangeType.Custom => SpecificPages.Where(p => p >= 1 && p <= totalPages).ToArray(),
            PageRangeType.OddPages => Enumerable.Range(1, totalPages).Where(p => p % 2 == 1).ToArray(),
            PageRangeType.EvenPages => Enumerable.Range(1, totalPages).Where(p => p % 2 == 0).ToArray(),
            _ => Array.Empty<int>()
        };
    }
}
