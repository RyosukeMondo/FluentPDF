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
}
