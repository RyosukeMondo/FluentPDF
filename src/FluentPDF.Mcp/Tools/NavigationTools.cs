using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class NavigationTools
{
    [McpServerTool(Name = "pdf_navigate"),
     Description(
        "Go to a specific page in the document, or move to the next or " +
        "previous page. The page change is reflected live in the UI.")]
    public static async Task<string> Navigate(
        FluentPdfClient client,
        [Description("Page number to jump to, or 'next'/'previous' to move one page")] string target)
    {
        string json;
        if (int.TryParse(target, out var page))
            json = await client.NavigateAsync(new { page });
        else
            json = await client.NavigateAsync(new { action = target });
        return ResponseFormatter.FormatNavigate(json);
    }

    [McpServerTool(Name = "pdf_zoom"),
     Description(
        "Change the zoom level of the document view. You can set an exact " +
        "percentage or zoom in/out incrementally.")]
    public static async Task<string> Zoom(
        FluentPdfClient client,
        [Description("Zoom percentage (e.g. 100, 150, 200), or 'in'/'out' to step")] string level)
    {
        string json;
        if (double.TryParse(level, out var pct))
            json = await client.ZoomAsync(new { level = pct });
        else
            json = await client.ZoomAsync(new { action = level });
        return ResponseFormatter.FormatZoom(json);
    }

    [McpServerTool(Name = "pdf_toggle_panel"),
     Description(
        "Show or hide a side panel in the FluentPDF interface. Available " +
        "panels: thumbnails (page previews), bookmarks (document outline), " +
        "search (search results).")]
    public static async Task<string> TogglePanel(
        FluentPdfClient client,
        [Description("Panel to toggle: 'thumbnails', 'bookmarks', or 'search'")] string panel)
    {
        var json = await client.TogglePanelAsync(panel);
        return ResponseFormatter.FormatTogglePanel(json);
    }
}
