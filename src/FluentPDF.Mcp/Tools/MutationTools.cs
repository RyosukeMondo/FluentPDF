using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class MutationTools
{
    [McpServerTool(Name = "pdf_click"), Description("Click at PDF coordinates to select an object.")]
    public static async Task<string> Click(
        FluentPdfClient client,
        [Description("X coordinate in PDF points")] double x,
        [Description("Y coordinate in PDF points")] double y)
    {
        return await client.ClickAsync(x, y);
    }

    [McpServerTool(Name = "pdf_move_selection"), Description("Move currently selected objects by delta.")]
    public static async Task<string> MoveSelection(
        FluentPdfClient client,
        [Description("X offset in PDF points")] double deltaX,
        [Description("Y offset in PDF points")] double deltaY)
    {
        return await client.MoveSelectionAsync(deltaX, deltaY);
    }

    [McpServerTool(Name = "pdf_delete_selection"), Description("Delete currently selected objects.")]
    public static async Task<string> DeleteSelection(FluentPdfClient client)
    {
        return await client.DeleteSelectionAsync();
    }

    [McpServerTool(Name = "pdf_add_text"), Description("Add text at a position on the current page.")]
    public static async Task<string> AddText(
        FluentPdfClient client,
        [Description("X position in PDF points")] double x,
        [Description("Y position in PDF points")] double y,
        [Description("Text content")] string text,
        [Description("Font size (default 12)")] float fontSize = 12f,
        [Description("Font name (default Helvetica)")] string fontName = "Helvetica")
    {
        return await client.AddTextAsync(x, y, text, fontSize, fontName);
    }

    [McpServerTool(Name = "pdf_draw_shape"), Description("Draw a rectangle, circle, or line on the current page.")]
    public static async Task<string> DrawShape(
        FluentPdfClient client,
        [Description("Shape type: Rectangle, Circle, or Line")] string shape,
        [Description("X position")] double x,
        [Description("Y position")] double y,
        [Description("Width")] double width,
        [Description("Height")] double height,
        [Description("Fill color hex (default #FF0000)")] string fillColor = "#FF0000",
        [Description("Stroke color hex (default #000000)")] string strokeColor = "#000000",
        [Description("Stroke width (default 1)")] float strokeWidth = 1f)
    {
        return await client.DrawShapeAsync(shape, x, y, width, height, fillColor, strokeColor, strokeWidth);
    }

    [McpServerTool(Name = "pdf_list_shapes"), Description("List tracked shapes on the PDF. Filter by page number or source (ai/user).")]
    public static async Task<string> ListShapes(
        FluentPdfClient client,
        [Description("Page number to filter (optional)")] int? page = null,
        [Description("Source filter: ai or user (optional)")] string? source = null)
    {
        return await client.ListShapesAsync(page, source);
    }

    [McpServerTool(Name = "pdf_delete_shape"), Description("Delete a tracked shape by its ID.")]
    public static async Task<string> DeleteShape(
        FluentPdfClient client,
        [Description("Shape ID returned from draw-shape")] string id)
    {
        return await client.DeleteShapeAsync(id);
    }
}
