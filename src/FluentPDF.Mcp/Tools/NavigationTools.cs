using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class NavigationTools
{
    [McpServerTool(Name = "pdf_navigate"), Description("Navigate to a page number or use next/previous.")]
    public static async Task<string> Navigate(
        FluentPdfClient client,
        [Description("Page number, or 'next'/'previous'")] string target)
    {
        if (int.TryParse(target, out var page))
            return await client.NavigateAsync(new { page });
        return await client.NavigateAsync(new { action = target });
    }

    [McpServerTool(Name = "pdf_zoom"), Description("Set zoom level or use in/out.")]
    public static async Task<string> Zoom(
        FluentPdfClient client,
        [Description("Zoom percentage (e.g. 150), or 'in'/'out'")] string level)
    {
        if (double.TryParse(level, out var pct))
            return await client.ZoomAsync(new { level = pct });
        return await client.ZoomAsync(new { action = level });
    }

    [McpServerTool(Name = "pdf_toggle_panel"), Description("Toggle a UI panel: thumbnails, bookmarks, or search.")]
    public static async Task<string> TogglePanel(
        FluentPdfClient client,
        [Description("Panel name: thumbnails, bookmarks, or search")] string panel)
    {
        return await client.TogglePanelAsync(panel);
    }
}
