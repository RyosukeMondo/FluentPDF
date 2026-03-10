using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class RenderTools
{
    [McpServerTool(Name = "pdf_render_page"),
     Description(
        "Render a page of the document as a PNG image. Use this to visually " +
        "inspect page content, verify layout, or check how annotations and " +
        "edits look.")]
    public static async Task<string> RenderPage(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("Page index (starting from 0)")] int pageIndex,
        [Description("Resolution in DPI, higher means sharper (default: 150)")] int dpi = 150)
    {
        var json = await client.RenderPageAsync(documentId, pageIndex, dpi);
        return ResponseFormatter.FormatRenderPage(json, pageIndex, dpi);
    }

    [McpServerTool(Name = "pdf_screenshot"),
     Description(
        "Take a screenshot of the entire FluentPDF application window as " +
        "it appears right now, including toolbars, panels, and the document " +
        "view.")]
    public static async Task<string> Screenshot(FluentPdfClient client)
    {
        var json = await client.ScreenshotAsync();
        return ResponseFormatter.FormatScreenshot(json);
    }
}
