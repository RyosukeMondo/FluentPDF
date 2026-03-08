using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class InspectionTools
{
    [McpServerTool(Name = "pdf_get_text"), Description("Extract text from a page.")]
    public static async Task<string> GetText(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("1-based page number")] int pageNumber)
    {
        return await client.ExtractTextAsync(documentId, pageNumber);
    }

    [McpServerTool(Name = "pdf_get_objects"), Description("Get all objects on the current page.")]
    public static async Task<string> GetObjects(FluentPdfClient client)
    {
        return await client.GetObjectsAsync();
    }

    [McpServerTool(Name = "pdf_get_object_detail"), Description("Hit-test at PDF coordinates to get object details.")]
    public static async Task<string> GetObjectDetail(
        FluentPdfClient client,
        [Description("X coordinate in PDF points")] double x,
        [Description("Y coordinate in PDF points")] double y)
    {
        return await client.HitTestAsync(x, y);
    }
}
