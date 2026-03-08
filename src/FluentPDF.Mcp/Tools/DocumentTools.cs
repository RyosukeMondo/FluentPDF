using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class DocumentTools
{
    [McpServerTool(Name = "pdf_open"), Description("Open a PDF file in the FluentPDF UI. Returns tab/session info.")]
    public static async Task<string> Open(
        FluentPdfClient client,
        [Description("Absolute path to the PDF file")] string path)
    {
        return await client.OpenAsync(path);
    }

    [McpServerTool(Name = "pdf_save"), Description("Save the current document.")]
    public static async Task<string> Save(FluentPdfClient client)
    {
        return await client.SaveAsync();
    }

    [McpServerTool(Name = "pdf_close"), Description("Close the active tab.")]
    public static async Task<string> Close(FluentPdfClient client)
    {
        return await client.CloseTabAsync();
    }

    [McpServerTool(Name = "pdf_gui_state"), Description("Get full UI state: tabs, current page, zoom, panels.")]
    public static async Task<string> GuiState(FluentPdfClient client)
    {
        return await client.GetStateAsync();
    }
}
