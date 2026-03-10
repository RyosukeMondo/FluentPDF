using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class MutationTools
{
    [McpServerTool(Name = "pdf_click"),
     Description(
        "Click on a specific location in the PDF to select an object " +
        "(text, image, shape) at those coordinates. Returns details about " +
        "the selected object if one is found.")]
    public static async Task<string> Click(
        FluentPdfClient client,
        [Description("Horizontal position in PDF points")] double x,
        [Description("Vertical position in PDF points")] double y)
    {
        return await client.ClickAsync(x, y);
    }

    [McpServerTool(Name = "pdf_move_selection"),
     Description(
        "Move the currently selected object(s) by a specified offset. " +
        "Select an object first using pdf_click, then move it.")]
    public static async Task<string> MoveSelection(
        FluentPdfClient client,
        [Description("Horizontal offset in PDF points (positive = right)")] double deltaX,
        [Description("Vertical offset in PDF points (positive = up)")] double deltaY)
    {
        return await client.MoveSelectionAsync(deltaX, deltaY);
    }

    [McpServerTool(Name = "pdf_delete_selection"),
     Description(
        "Delete the currently selected object(s) from the page. Select " +
        "an object first using pdf_click, then delete it. Original " +
        "document objects may be protected by the lock setting.")]
    public static async Task<string> DeleteSelection(FluentPdfClient client)
    {
        return await client.DeleteSelectionAsync();
    }

    [McpServerTool(Name = "pdf_add_text"),
     Description(
        "Add new text to the current page at a specific position. The text " +
        "appears immediately in the live UI. You can choose the font and " +
        "size.")]
    public static async Task<string> AddText(
        FluentPdfClient client,
        [Description("Horizontal position in PDF points")] double x,
        [Description("Vertical position in PDF points")] double y,
        [Description("The text content to add")] string text,
        [Description("Font size in points (default: 12)")] float fontSize = 12f,
        [Description("Font name (default: Helvetica)")] string fontName = "Helvetica")
    {
        return await client.AddTextAsync(x, y, text, fontSize, fontName);
    }

    [McpServerTool(Name = "pdf_draw_shape"),
     Description(
        "Draw a shape (rectangle, circle, or line) on the current page. " +
        "You can customize the fill color, border color, and line thickness. " +
        "The shape appears immediately in the live UI.")]
    public static async Task<string> DrawShape(
        FluentPdfClient client,
        [Description("Shape to draw: 'Rectangle', 'Circle', or 'Line'")] string shape,
        [Description("Left edge position in PDF points")] double x,
        [Description("Bottom edge position in PDF points")] double y,
        [Description("Width of the shape in PDF points")] double width,
        [Description("Height of the shape in PDF points")] double height,
        [Description("Fill color as hex code (default: #FF0000 red)")] string fillColor = "#FF0000",
        [Description("Border color as hex code (default: #000000 black)")] string strokeColor = "#000000",
        [Description("Border line thickness in points (default: 1)")] float strokeWidth = 1f)
    {
        return await client.DrawShapeAsync(shape, x, y, width, height, fillColor, strokeColor, strokeWidth);
    }

    [McpServerTool(Name = "pdf_list_shapes"),
     Description(
        "List all shapes that have been drawn on the document. You can " +
        "filter by page number or by who created them (AI or user).")]
    public static async Task<string> ListShapes(
        FluentPdfClient client,
        [Description("Filter to a specific page number (optional)")] int? page = null,
        [Description("Filter by creator: 'ai' or 'user' (optional)")] string? source = null)
    {
        return await client.ListShapesAsync(page, source);
    }

    [McpServerTool(Name = "pdf_delete_shape"),
     Description(
        "Delete a previously drawn shape by its ID. Use pdf_list_shapes " +
        "to find the shape ID first.")]
    public static async Task<string> DeleteShape(
        FluentPdfClient client,
        [Description("The shape ID (returned when the shape was drawn)")] string id)
    {
        return await client.DeleteShapeAsync(id);
    }
}
