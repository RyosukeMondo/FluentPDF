using System.Text;
using System.Text.Json;

namespace FluentPDF.Mcp;

/// <summary>
/// Converts raw JSON API responses into human-readable natural language
/// that is easier for LLMs and humans to parse at a glance.
/// </summary>
public static partial class ResponseFormatter
{
    private static readonly JsonDocumentOptions JsonOpts = new()
    {
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Attempt to parse a JSON string into a JsonElement. Returns false if not valid JSON.
    /// </summary>
    private static bool TryParse(string json, out JsonElement root)
    {
        root = default;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(json, JsonOpts);
            root = doc.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetString(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
    }

    private static int? GetInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
    }

    private static double? GetDouble(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
    }

    private static bool? GetBool(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;
    }

    /// <summary>
    /// Check if a JSON response contains an error, and return a humanized error message.
    /// Returns null if no error is found.
    /// </summary>
    private static string? FormatError(JsonElement root)
    {
        var error = GetString(root, "error");
        if (error != null)
            return $"Error: {error}";

        if (GetBool(root, "success") == false)
        {
            error = GetString(root, "error") ?? "Unknown error";
            return $"Error: {error}";
        }

        return null;
    }

    public static string FormatOpen(string json)
    {
        if (!TryParse(json, out var root))
            return json;

        var err = FormatError(root);
        if (err != null) return err;

        var file = GetString(root, "activeFile") ?? "document";
        var pages = GetInt(root, "pageCount");
        var tabs = GetInt(root, "tabCount");

        var sb = new StringBuilder();
        sb.Append($"Opened \"{file}\"");
        if (pages.HasValue)
            sb.Append($" — {pages.Value} page{Plural(pages.Value)}");
        if (tabs.HasValue && tabs.Value > 1)
            sb.Append($" ({tabs.Value} tabs open)");
        sb.Append('.');
        return sb.ToString();
    }

    public static string FormatGuiState(string json)
    {
        if (!TryParse(json, out var root))
            return json;

        var err = FormatError(root);
        if (err != null) return err;

        var sb = new StringBuilder();

        // Viewer info
        if (root.TryGetProperty("viewer", out var viewer) && viewer.ValueKind == JsonValueKind.Object)
        {
            var page = GetInt(viewer, "currentPage");
            var total = GetInt(viewer, "totalPages");
            var zoom = GetDouble(viewer, "zoomLevel");

            if (page.HasValue && total.HasValue)
                sb.Append($"Currently viewing page {page.Value} of {total.Value}");

            if (zoom.HasValue)
                sb.Append($" at {zoom.Value:F0}% zoom");

            sb.Append('.');

            var sidebar = GetBool(viewer, "sidebarVisible");
            var bookmarks = GetBool(viewer, "bookmarksVisible");
            var search = GetBool(viewer, "searchVisible");
            var panels = new List<string>();
            if (sidebar == true) panels.Add("thumbnails");
            if (bookmarks == true) panels.Add("bookmarks");
            if (search == true) panels.Add("search");

            if (panels.Count > 0)
                sb.Append($" Panels open: {string.Join(", ", panels)}.");

            var loading = GetBool(viewer, "isLoading");
            if (loading == true)
                sb.Append(" Document is still loading.");
        }
        else
        {
            sb.Append("No document is currently open.");
        }

        // Tab info
        if (root.TryGetProperty("activeTab", out var tab) && tab.ValueKind == JsonValueKind.Object)
        {
            var fileName = GetString(tab, "fileName");
            var unsaved = GetBool(tab, "hasUnsavedChanges");
            if (fileName != null)
            {
                sb.Append($" Active file: \"{fileName}\"");
                if (unsaved == true)
                    sb.Append(" (unsaved changes)");
                sb.Append('.');
            }
        }

        var tabCount = GetInt(root, "tabCount");
        if (tabCount.HasValue && tabCount.Value > 1)
            sb.Append($" {tabCount.Value} tabs open.");

        return sb.ToString();
    }

    public static string FormatSearch(string json)
    {
        if (!TryParse(json, out var root))
            return json;

        var err = FormatError(root);
        if (err != null) return err;

        var total = GetInt(root, "totalMatches");
        var query = GetString(root, "query");

        if (total == null || total == 0)
            return $"No matches found for \"{query ?? "?"}\".";

        // Count distinct pages from pageGroups
        int pageCount = 0;
        if (root.TryGetProperty("pageGroups", out var pg) && pg.ValueKind == JsonValueKind.Array)
            pageCount = pg.GetArrayLength();

        var sb = new StringBuilder();
        sb.Append($"Found {total.Value} match{Plural(total.Value, "es")} for \"{query}\"");
        if (pageCount > 0)
            sb.Append($" across {pageCount} page{Plural(pageCount)}");
        sb.Append('.');

        // Show page breakdown if reasonable
        if (root.TryGetProperty("pageGroups", out var groups) && groups.ValueKind == JsonValueKind.Array)
        {
            var entries = new List<string>();
            foreach (var g in groups.EnumerateArray())
            {
                var pn = GetInt(g, "pageNumber");
                var mc = GetInt(g, "matchCount");
                if (pn.HasValue && mc.HasValue)
                    entries.Add($"p.{pn.Value}: {mc.Value}");
                if (entries.Count >= 10) { entries.Add("..."); break; }
            }
            if (entries.Count > 0)
                sb.Append($" Breakdown: {string.Join(", ", entries)}.");
        }

        return sb.ToString();
    }

    public static string FormatMetadata(string json)
    {
        if (!TryParse(json, out var root))
            return json;

        var err = FormatError(root);
        if (err != null) return err;

        var sb = new StringBuilder();

        var title = GetString(root, "title");
        var author = GetString(root, "author");
        var pages = GetInt(root, "pageCount");
        var creator = GetString(root, "creator");
        var producer = GetString(root, "producer");
        var subject = GetString(root, "subject");
        var keywords = GetString(root, "keywords");
        var created = GetString(root, "creationDate");
        var modified = GetString(root, "modificationDate");
        var version = GetString(root, "pdfVersion");
        var fileSize = GetInt(root, "fileSize") ?? GetInt(root, "fileSizeBytes");

        if (pages.HasValue)
            sb.Append($"This {pages.Value}-page document");
        else
            sb.Append("This document");

        if (!string.IsNullOrWhiteSpace(title))
            sb.Append($" \"{title}\"");

        if (!string.IsNullOrWhiteSpace(author))
            sb.Append($" was created by {author}");

        if (!string.IsNullOrWhiteSpace(created))
            sb.Append($" on {created}");

        sb.Append('.');

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(creator)) details.Add($"Creator: {creator}");
        if (!string.IsNullOrWhiteSpace(producer)) details.Add($"Producer: {producer}");
        if (!string.IsNullOrWhiteSpace(subject)) details.Add($"Subject: {subject}");
        if (!string.IsNullOrWhiteSpace(keywords)) details.Add($"Keywords: {keywords}");
        if (!string.IsNullOrWhiteSpace(modified)) details.Add($"Modified: {modified}");
        if (!string.IsNullOrWhiteSpace(version)) details.Add($"PDF version: {version}");
        if (fileSize.HasValue) details.Add($"Size: {FormatFileSize(fileSize.Value)}");

        if (details.Count > 0)
            sb.Append($" {string.Join(". ", details)}.");

        return sb.ToString();
    }

    public static string FormatAnnotations(string json)
    {
        if (!TryParse(json, out var root))
            return json;

        var err = FormatError(root);
        if (err != null) return err;

        // Response might be an array directly or an object with "annotations" property
        JsonElement annotations;
        if (root.ValueKind == JsonValueKind.Array)
        {
            annotations = root;
        }
        else if (root.TryGetProperty("annotations", out var annots) && annots.ValueKind == JsonValueKind.Array)
        {
            annotations = annots;
        }
        else
        {
            // Could be count-based
            var count = GetInt(root, "count") ?? GetInt(root, "totalAnnotations");
            if (count.HasValue)
                return $"{count.Value} annotation{Plural(count.Value)} found.";
            return json;
        }

        int total = annotations.GetArrayLength();
        if (total == 0)
            return "No annotations found.";

        // Count by type
        var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in annotations.EnumerateArray())
        {
            var type = GetString(a, "type") ?? GetString(a, "subtype") ?? "unknown";
            typeCounts.TryGetValue(type, out var c);
            typeCounts[type] = c + 1;
        }

        var breakdown = string.Join(", ", typeCounts.Select(kv => $"{kv.Value} {kv.Key}"));
        return $"{total} annotation{Plural(total)} found ({breakdown}).";
    }

    public static string FormatGetText(string json)
    {
        // Text extraction may return raw text or JSON with a text property
        if (TryParse(json, out var root))
        {
            var err = FormatError(root);
            if (err != null) return err;

            var text = GetString(root, "text");
            if (text != null)
                return text.Trim();
        }

        // Return raw text, just clean up whitespace
        return json.Trim();
    }

    public static string FormatGetObjects(string json)
    {
        if (!TryParse(json, out var root))
            return json;

        var err = FormatError(root);
        if (err != null) return err;

        var count = GetInt(root, "count");

        if (root.TryGetProperty("objects", out var objects) && objects.ValueKind == JsonValueKind.Array)
        {
            var total = count ?? objects.GetArrayLength();
            if (total == 0)
                return "No objects on the current page.";

            // Count by type
            var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var obj in objects.EnumerateArray())
            {
                var typeCode = GetInt(obj, "Type") ?? GetInt(obj, "type");
                var typeName = typeCode switch
                {
                    1 => "text",
                    2 => "path",
                    3 => "image",
                    4 => "shading",
                    5 => "form",
                    _ => GetString(obj, "type") ?? "unknown"
                };
                typeCounts.TryGetValue(typeName, out var c);
                typeCounts[typeName] = c + 1;
            }

            var breakdown = string.Join(", ", typeCounts.Select(kv => $"{kv.Value} {kv.Key}"));
            return $"{total} object{Plural(total)} on the current page ({breakdown}).";
        }

        if (count.HasValue)
            return $"{count.Value} object{Plural(count.Value)} on the current page.";

        return json;
    }
}
