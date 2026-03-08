using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class RenderTools
{
    [McpServerTool(Name = "pdf_render_page"), Description("Render a page to PNG. Returns image data.")]
    public static async Task<string> RenderPage(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("0-based page index")] int pageIndex,
        [Description("DPI (default 150)")] int dpi = 150)
    {
        return await client.RenderPageAsync(documentId, pageIndex, dpi);
    }

    [McpServerTool(Name = "pdf_screenshot"), Description("Capture a screenshot of the live FluentPDF UI.")]
    public static async Task<string> Screenshot(FluentPdfClient client)
    {
        return await client.ScreenshotAsync();
    }
}
