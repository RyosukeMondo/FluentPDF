using System.ComponentModel;
using System.Text;
using System.Text.Json;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class SearchTools
{
    [McpServerTool(Name = "pdf_search_keyword"),
     Description(
        "Search for text in the open document and return every match with its " +
        "page number, bounding box coordinates, and a surrounding text snippet " +
        "for context. Use this to find specific words, phrases, or section " +
        "headings across all pages.")]
    public static async Task<string> SearchKeyword(
        FluentPdfClient client,
        [Description("The text to search for (word, phrase, or partial text)")] string query,
        [Description("Match exact letter casing (default: false)")] bool caseSensitive = false,
        [Description("Match whole words only, not partial matches (default: false)")] bool wholeWord = false)
    {
        var json = await client.SearchAsync(query, caseSensitive, wholeWord);
        return ResponseFormatter.FormatSearch(json);
    }

    [McpServerTool(Name = "pdf_find_pages"),
     Description(
        "Read the text content of every page in the document so you can " +
        "determine which pages discuss a specific topic. Returns raw text " +
        "per page (up to 50 pages) for your analysis.")]
    public static async Task<string> FindPages(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("Total number of pages in the document")] int pageCount)
    {
        var results = new StringBuilder();
        results.Append("{\"pages\":[");
        var cap = Math.Min(pageCount, 50);
        for (int i = 1; i <= cap; i++)
        {
            if (i > 1) results.Append(',');
            var text = await client.ExtractTextAsync(documentId, i);
            results.Append($"{{\"page\":{i},\"text\":{JsonSerializer.Serialize(text)}}}");
        }
        results.Append("]}");
        return ResponseFormatter.FormatFindPages(results.ToString());
    }

    [McpServerTool(Name = "pdf_summarize_page"),
     Description(
        "Extract all text from a specific page. Use this to read page " +
        "content, summarize it, or answer questions about what is on a " +
        "particular page.")]
    public static async Task<string> SummarizePage(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("Page number (starting from 1)")] int pageNumber)
    {
        var json = await client.ExtractTextAsync(documentId, pageNumber);
        return ResponseFormatter.FormatSummarizePage(json, pageNumber);
    }

    [McpServerTool(Name = "pdf_highlight_relevant"),
     Description(
        "Highlight a region on a page with a colored overlay. Use this to " +
        "draw attention to important passages, search results, or areas of " +
        "interest. The highlight appears immediately in the live UI.")]
    public static async Task<string> HighlightRelevant(
        FluentPdfClient client,
        [Description("Page number (starting from 1)")] int pageNumber,
        [Description("Left edge of the highlight area (PDF points)")] double left,
        [Description("Bottom edge of the highlight area (PDF points)")] double bottom,
        [Description("Right edge of the highlight area (PDF points)")] double right,
        [Description("Top edge of the highlight area (PDF points)")] double top,
        [Description("Highlight color as hex code (default: yellow #FFFF00)")] string color = "#FFFF00")
    {
        var json = await client.HighlightAsync(pageNumber, left, bottom, right, top, color);
        return ResponseFormatter.FormatHighlight(json);
    }

    [McpServerTool(Name = "pdf_get_metadata"),
     Description(
        "Get information about the PDF document: title, author, subject, " +
        "keywords, creation and modification dates, page count, file size, " +
        "PDF version, and security/permission details.")]
    public static async Task<string> GetMetadata(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId)
    {
        var json = await client.GetMetadataAsync(documentId);
        return ResponseFormatter.FormatMetadata(json);
    }

    [McpServerTool(Name = "pdf_list_annotations"),
     Description(
        "List all annotations in the document (highlights, notes, stamps, " +
        "underlines, etc.) across all pages. Returns each annotation's type, " +
        "page number, position, color, content text, and author. Provide a " +
        "page number to filter to a single page, or omit it to get all pages.")]
    public static async Task<string> ListAnnotations(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("Page number to filter (omit for all pages, or 0-based index for a single page)")] int? pageNumber = null)
    {
        string json;
        if (pageNumber.HasValue)
            json = await client.ListAnnotationsAsync(documentId, pageNumber.Value);
        else
            json = await client.ListAllAnnotationsAsync(documentId);
        return ResponseFormatter.FormatAnnotations(json);
    }
}
