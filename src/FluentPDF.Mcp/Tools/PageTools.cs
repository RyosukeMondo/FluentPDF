using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class PageTools
{
    [McpServerTool(Name = "pdf_rotate_page"),
     Description(
        "Rotate the current page clockwise or counter-clockwise by 90 " +
        "degrees. The rotation is applied permanently and visible in the " +
        "live UI.")]
    public static async Task<string> RotatePage(
        FluentPdfClient client,
        [Description("Rotation direction: 'rotate_cw' (clockwise) or 'rotate_ccw' (counter-clockwise)")] string direction)
    {
        var json = await client.PageOpAsync(direction);
        return ResponseFormatter.FormatPageOp(json, direction);
    }

    [McpServerTool(Name = "pdf_delete_page"),
     Description(
        "Permanently delete the current page from the document. This " +
        "cannot be undone, so use with care.")]
    public static async Task<string> DeletePage(FluentPdfClient client)
    {
        var json = await client.PageOpAsync("delete");
        return ResponseFormatter.FormatPageOp(json, "delete");
    }

    [McpServerTool(Name = "pdf_insert_blank"),
     Description(
        "Insert a new blank page after the current page. Useful for " +
        "adding space for notes or new content.")]
    public static async Task<string> InsertBlank(FluentPdfClient client)
    {
        var json = await client.PageOpAsync("insert_blank");
        return ResponseFormatter.FormatPageOp(json, "insert_blank");
    }
}
