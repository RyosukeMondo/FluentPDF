using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class DocumentTools
{
    [McpServerTool(Name = "pdf_open"),
     Description(
        "Open a PDF file in FluentPDF. The document appears in the live UI " +
        "and you can then navigate, search, annotate, and edit it. Returns " +
        "a session ID and document info.")]
    public static async Task<string> Open(
        FluentPdfClient client,
        [Description("Full file path to the PDF (e.g. C:/Documents/report.pdf)")] string path)
    {
        var json = await client.OpenAsync(path);
        return ResponseFormatter.FormatOpen(json);
    }

    [McpServerTool(Name = "pdf_save"),
     Description(
        "Save any changes made to the current document. This writes all " +
        "edits (annotations, text additions, shape drawings) to disk.")]
    public static async Task<string> Save(FluentPdfClient client)
    {
        var json = await client.SaveAsync();
        return ResponseFormatter.FormatSave(json);
    }

    [McpServerTool(Name = "pdf_close"),
     Description("Close the currently active document tab in FluentPDF.")]
    public static async Task<string> Close(FluentPdfClient client)
    {
        var json = await client.CloseTabAsync();
        return ResponseFormatter.FormatClose(json);
    }

    [McpServerTool(Name = "pdf_gui_state"),
     Description(
        "Get the current state of the FluentPDF application: which tabs " +
        "are open, current page number, zoom level, visible panels, and " +
        "active document info.")]
    public static async Task<string> GuiState(FluentPdfClient client)
    {
        var json = await client.GetStateAsync();
        return ResponseFormatter.FormatGuiState(json);
    }
}
