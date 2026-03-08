using FluentPDF.Core.Models;

namespace FluentPDF.Core.Services;

/// <summary>Metadata for a tracked shape added via ShapeService.</summary>
public record ShapeMetadata(
    string Id,
    string ShapeType,
    PageIndex PageIndex,
    float Left, float Bottom, float Right, float Top,
    string FillColor,
    string StrokeColor,
    float StrokeWidth,
    string Source,
    DateTime CreatedAt);

/// <summary>
/// Service contract for drawing shapes on PDF pages.
/// All page parameters use <see cref="PageIndex"/> (0-based) to prevent off-by-one bugs.
/// All Add* methods return a GUID string on success, null on failure.
/// </summary>
public interface IShapeService
{
    Task<string?> AddRectangleAsync(string documentId, PageIndex pageIndex, double x, double y, double width, double height,
        string fillColor = "#FF0000", string strokeColor = "#000000", float strokeWidth = 1f, float opacity = 1f, string source = "user");

    Task<string?> AddCircleAsync(string documentId, PageIndex pageIndex, double centerX, double centerY, double radius,
        string fillColor = "#0000FF", string strokeColor = "#000000", float strokeWidth = 1f, float opacity = 1f, string source = "user");

    Task<string?> AddLineAsync(string documentId, PageIndex pageIndex, double x1, double y1, double x2, double y2,
        string strokeColor = "#000000", float strokeWidth = 2f, string source = "user");

    Task<string?> AddFreehandPathAsync(string documentId, PageIndex pageIndex, double[] points,
        string strokeColor = "#000000", float strokeWidth = 2f, string source = "user");

    Task<string?> AddTextAsync(string documentId, PageIndex pageIndex, double x, double y, string text,
        float fontSize = 12f, string fontName = "Helvetica", string color = "#000000", string source = "user");

    /// <summary>Lists tracked shapes, optionally filtered by page index and/or source.</summary>
    List<ShapeMetadata> GetTrackedShapes(PageIndex? pageIndex = null, string? source = null);

    /// <summary>Removes a tracked shape by its ID.</summary>
    Task<bool> RemoveTrackedShapeAsync(string documentId, string shapeId);

    Task<List<PageObjectInfo>> GetPageObjectsAsync(string documentId, PageIndex pageIndex);

    Task<bool> RemovePageObjectAsync(string documentId, PageIndex pageIndex, int objectIndex);

    Task<bool> MovePageObjectAsync(string documentId, PageIndex pageIndex, int objectIndex, float deltaX, float deltaY);

    Task<int> MovePageObjectsBatchAsync(string documentId, PageIndex pageIndex, int[] objectIndices, float deltaX, float deltaY);

    Task<bool> ResizePageObjectAsync(string documentId, PageIndex pageIndex, int objectIndex,
        float scaleX, float scaleY, float anchorPdfX, float anchorPdfY);

    void FlushDirtyPages(string documentId);

    Task<PageObjectProperties?> GetPageObjectPropertiesAsync(string documentId, PageIndex pageIndex, int objectIndex);

    Task<bool> SetPageObjectStrokeColorAsync(string documentId, PageIndex pageIndex, int objectIndex, string color);

    Task<bool> SetPageObjectFillColorAsync(string documentId, PageIndex pageIndex, int objectIndex, string color);

    Task<bool> SetPageObjectStrokeWidthAsync(string documentId, PageIndex pageIndex, int objectIndex, float width);
}

/// <summary>
/// Describes a page object's type and bounding box.
/// </summary>
public record PageObjectInfo(int Index, int Type, float Left, float Bottom, float Right, float Top);

/// <summary>Properties of a page object (colors, stroke width).</summary>
public record PageObjectProperties(string StrokeColor, string FillColor, float StrokeWidth);
