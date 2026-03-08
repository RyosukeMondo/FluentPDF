using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FluentPDF.Mcp.Services;

public sealed class FluentPdfClient
{
    private readonly HttpClient _http;

    public FluentPdfClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("FluentPdf");
    }

    // Document
    public Task<string> OpenAsync(string path) =>
        PostJsonAsync("/api/gui/open", new { filePath = path });

    public Task<string> SaveAsync() =>
        PostJsonAsync("/api/gui/save", new { });

    public Task<string> CloseTabAsync() =>
        PostJsonAsync("/api/gui/close-tab", new { });

    // GUI state
    public Task<string> GetStateAsync() =>
        GetAsync("/api/gui/state");

    // Text
    public Task<string> ExtractTextAsync(string documentId, int pageNumber) =>
        PostJsonAsync("/api/text/extract", new { documentId, pageNumber });

    // Objects
    public Task<string> GetObjectsAsync() =>
        GetAsync("/api/gui/interact/objects");

    public Task<string> HitTestAsync(double x, double y) =>
        PostJsonAsync("/api/gui/interact/hit-test", new { x, y });

    // Render
    public Task<string> RenderPageAsync(string documentId, int pageIndex, int dpi = 150) =>
        GetAsync($"/api/render/{documentId}/{pageIndex}?dpi={dpi}");

    public Task<string> ScreenshotAsync() =>
        GetAsync("/api/gui/screenshot");

    // Navigation
    public Task<string> NavigateAsync(object body) =>
        PostJsonAsync("/api/gui/navigate", body);

    public Task<string> ZoomAsync(object body) =>
        PostJsonAsync("/api/gui/zoom", body);

    public Task<string> TogglePanelAsync(string panel) =>
        PostJsonAsync("/api/gui/toggle", new { panel });

    // Interaction
    public Task<string> ClickAsync(double x, double y) =>
        PostJsonAsync("/api/gui/interact/click", new { x, y });

    public Task<string> MoveSelectionAsync(double deltaX, double deltaY) =>
        PostJsonAsync("/api/gui/interact/selection/move", new { deltaX, deltaY });

    public Task<string> DeleteSelectionAsync() =>
        DeleteAsync("/api/gui/interact/selection");

    // Mutation
    public Task<string> AddTextAsync(double x, double y, string text, float fontSize, string fontName) =>
        PostJsonAsync("/api/gui/interact/text", new { pdfX = x, pdfY = y, text, fontSize, fontName });

    public Task<string> DrawShapeAsync(string shape, double x, double y, double width, double height, string fillColor, string strokeColor, float strokeWidth) =>
        PostJsonAsync("/api/gui/draw-shape", new { shape, x, y, width, height, fillColor, strokeColor, strokeWidth });

    // Shapes
    public Task<string> ListShapesAsync(int? page = null, string? source = null)
    {
        var query = "/api/shapes";
        var parts = new List<string>();
        if (page.HasValue) parts.Add($"page={page.Value}");
        if (source != null) parts.Add($"source={source}");
        if (parts.Count > 0) query += "?" + string.Join("&", parts);
        return GetAsync(query);
    }

    public Task<string> DeleteShapeAsync(string id, string? documentId = null)
    {
        var query = $"/api/shapes/{id}";
        if (documentId != null) query += $"?documentId={documentId}";
        return DeleteAsync(query);
    }

    // Page operations
    public Task<string> PageOpAsync(string action) =>
        PostJsonAsync("/api/gui/page-op", new { action });

    // Search
    public Task<string> SearchAsync(string query, bool caseSensitive = false, bool wholeWord = false) =>
        PostJsonAsync("/api/gui/search", new { query, caseSensitive, wholeWord });

    public Task<string> GetSearchResultsAsync() =>
        GetAsync("/api/gui/search/results");

    // Highlight
    public Task<string> HighlightAsync(int pageNumber, double left, double bottom, double right, double top, string color) =>
        PostJsonAsync("/api/gui/highlight", new { pageNumber, bounds = new { left, bottom, right, top }, color });

    // Metadata
    public Task<string> GetMetadataAsync(string documentId) =>
        GetAsync($"/api/document/{documentId}/metadata");

    // Annotations
    public Task<string> ListAnnotationsAsync(string documentId, int pageNumber) =>
        GetAsync($"/api/annotations/{documentId}/{pageNumber}");

    // Text extraction (all pages)
    public Task<string> ExtractAllTextAsync(string documentId) =>
        PostJsonAsync("/api/text/extract", new { documentId, pageNumber = -1 });

    // --- HTTP helpers ---

    private async Task<string> GetAsync(string path)
    {
        try
        {
            var resp = await _http.GetAsync(path);
            return await resp.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new { error = $"Connection failed: {ex.Message}. Is FluentPDF.Avalonia running with --api-server?" });
        }
    }

    private async Task<string> PostJsonAsync(string path, object body)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(path, content);
            return await resp.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new { error = $"Connection failed: {ex.Message}. Is FluentPDF.Avalonia running with --api-server?" });
        }
    }

    private async Task<string> DeleteAsync(string path)
    {
        try
        {
            var resp = await _http.DeleteAsync(path);
            return await resp.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new { error = $"Connection failed: {ex.Message}. Is FluentPDF.Avalonia running with --api-server?" });
        }
    }
}
