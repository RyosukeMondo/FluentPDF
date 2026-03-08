using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class PageTools
{
    [McpServerTool(Name = "pdf_rotate_page"), Description("Rotate the current page clockwise or counter-clockwise.")]
    public static async Task<string> RotatePage(
        FluentPdfClient client,
        [Description("Direction: rotate_cw or rotate_ccw")] string direction)
    {
        return await client.PageOpAsync(direction);
    }

    [McpServerTool(Name = "pdf_delete_page"), Description("Delete the current page.")]
    public static async Task<string> DeletePage(FluentPdfClient client)
    {
        return await client.PageOpAsync("delete");
    }

    [McpServerTool(Name = "pdf_insert_blank"), Description("Insert a blank page after the current page.")]
    public static async Task<string> InsertBlank(FluentPdfClient client)
    {
        return await client.PageOpAsync("insert_blank");
    }
}
