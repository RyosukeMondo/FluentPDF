using System.ComponentModel;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class InspectionTools
{
    [McpServerTool(Name = "pdf_get_text"),
     Description(
        "Extract all text from a specific page of the document. Returns the " +
        "raw text content which you can read, summarize, or analyze.")]
    public static async Task<string> GetText(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("Page number (starting from 1)")] int pageNumber)
    {
        var json = await client.ExtractTextAsync(documentId, pageNumber);
        return ResponseFormatter.FormatGetText(json);
    }

    [McpServerTool(Name = "pdf_get_objects"),
     Description(
        "List all objects (text blocks, images, shapes, paths) on the " +
        "current page. Returns each object's index, type, and bounding box " +
        "coordinates. Use this to understand the page layout.")]
    public static async Task<string> GetObjects(FluentPdfClient client)
    {
        var json = await client.GetObjectsAsync();
        return ResponseFormatter.FormatGetObjects(json);
    }

    [McpServerTool(Name = "pdf_get_object_detail"),
     Description(
        "Get detailed information about a specific object on the current " +
        "page by its index. Returns the object type (text, image, path, " +
        "shading, form), bounding box with width and height. Use " +
        "pdf_get_objects first to discover available object indices.")]
    public static async Task<string> GetObjectDetail(
        FluentPdfClient client,
        [Description("Zero-based index of the object on the current page")] int objectIndex)
    {
        var json = await client.GetObjectDetailAsync(objectIndex);
        return ResponseFormatter.FormatObjectDetail(json);
    }
}
