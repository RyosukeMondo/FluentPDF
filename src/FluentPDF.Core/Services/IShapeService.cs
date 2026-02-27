namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for drawing shapes on PDF pages.
/// Supports rectangles, circles, lines, freehand paths, and text objects.
/// </summary>
public interface IShapeService
{
    Task<bool> AddRectangleAsync(string documentId, int pageNumber, double x, double y, double width, double height,
        string fillColor = "#FF0000", string strokeColor = "#000000", float strokeWidth = 1f, float opacity = 1f);

    Task<bool> AddCircleAsync(string documentId, int pageNumber, double centerX, double centerY, double radius,
        string fillColor = "#0000FF", string strokeColor = "#000000", float strokeWidth = 1f, float opacity = 1f);

    Task<bool> AddLineAsync(string documentId, int pageNumber, double x1, double y1, double x2, double y2,
        string strokeColor = "#000000", float strokeWidth = 2f);

    Task<bool> AddFreehandPathAsync(string documentId, int pageNumber, double[] points,
        string strokeColor = "#000000", float strokeWidth = 2f);

    Task<bool> AddTextAsync(string documentId, int pageNumber, double x, double y, string text,
        float fontSize = 12f, string fontName = "Helvetica", string color = "#000000");

    /// <summary>
    /// Lists page objects with their bounds (type, index, left, bottom, right, top in PDF coords).
    /// </summary>
    Task<List<PageObjectInfo>> GetPageObjectsAsync(string documentId, int pageNumber);

    /// <summary>
    /// Removes a page object by index, regenerates content, and returns success.
    /// </summary>
    Task<bool> RemovePageObjectAsync(string documentId, int pageNumber, int objectIndex);

    /// <summary>Moves a page object by delta in PDF coordinates.</summary>
    Task<bool> MovePageObjectAsync(string documentId, int pageNumber, int objectIndex, float deltaX, float deltaY);

    /// <summary>Gets stroke/fill colors and stroke width of a page object.</summary>
    Task<PageObjectProperties?> GetPageObjectPropertiesAsync(string documentId, int pageNumber, int objectIndex);

    /// <summary>Sets stroke color of a page object.</summary>
    Task<bool> SetPageObjectStrokeColorAsync(string documentId, int pageNumber, int objectIndex, string color);

    /// <summary>Sets fill color of a page object.</summary>
    Task<bool> SetPageObjectFillColorAsync(string documentId, int pageNumber, int objectIndex, string color);

    /// <summary>Sets stroke width of a page object.</summary>
    Task<bool> SetPageObjectStrokeWidthAsync(string documentId, int pageNumber, int objectIndex, float width);
}

/// <summary>
/// Describes a page object's type and bounding box.
/// </summary>
public record PageObjectInfo(int Index, int Type, float Left, float Bottom, float Right, float Top);

/// <summary>Properties of a page object (colors, stroke width).</summary>
public record PageObjectProperties(string StrokeColor, string FillColor, float StrokeWidth);
