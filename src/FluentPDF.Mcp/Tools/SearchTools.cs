using System.ComponentModel;
using System.Text;
using System.Text.Json;
using FluentPDF.Mcp.Services;
using ModelContextProtocol.Server;

namespace FluentPDF.Mcp.Tools;

[McpServerToolType]
public sealed class SearchTools
{
    [McpServerTool(Name = "pdf_search_keyword"), Description("Search for text across all pages. Returns match counts and snippets per page.")]
    public static async Task<string> SearchKeyword(
        FluentPdfClient client,
        [Description("Search query text")] string query,
        [Description("Case sensitive (default false)")] bool caseSensitive = false,
        [Description("Whole word only (default false)")] bool wholeWord = false)
    {
        return await client.SearchAsync(query, caseSensitive, wholeWord);
    }

    [McpServerTool(Name = "pdf_find_pages"), Description("Find pages relevant to a topic. Extracts text from all pages for AI analysis. Use this when you need to find which pages discuss a specific topic.")]
    public static async Task<string> FindPages(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("Number of pages in the document")] int pageCount)
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
        return results.ToString();
    }

    [McpServerTool(Name = "pdf_summarize_page"), Description("Extract text from a specific page. Returns raw text for AI summarization.")]
    public static async Task<string> SummarizePage(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("1-based page number")] int pageNumber)
    {
        return await client.ExtractTextAsync(documentId, pageNumber);
    }

    [McpServerTool(Name = "pdf_highlight_relevant"), Description("Add highlight annotations to specific regions on a page.")]
    public static async Task<string> HighlightRelevant(
        FluentPdfClient client,
        [Description("1-based page number")] int pageNumber,
        [Description("Left coordinate in PDF points")] double left,
        [Description("Bottom coordinate in PDF points")] double bottom,
        [Description("Right coordinate in PDF points")] double right,
        [Description("Top coordinate in PDF points")] double top,
        [Description("Highlight color hex (default #FFFF00)")] string color = "#FFFF00")
    {
        return await client.HighlightAsync(pageNumber, left, bottom, right, top, color);
    }

    [McpServerTool(Name = "pdf_get_metadata"), Description("Get document metadata: title, author, dates, page count, permissions.")]
    public static async Task<string> GetMetadata(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId)
    {
        return await client.GetMetadataAsync(documentId);
    }

    [McpServerTool(Name = "pdf_list_annotations"), Description("List all annotations on a page: highlights, notes, stamps, etc.")]
    public static async Task<string> ListAnnotations(
        FluentPdfClient client,
        [Description("Document/session ID")] string documentId,
        [Description("1-based page number")] int pageNumber)
    {
        return await client.ListAnnotationsAsync(documentId, pageNumber);
    }
}
