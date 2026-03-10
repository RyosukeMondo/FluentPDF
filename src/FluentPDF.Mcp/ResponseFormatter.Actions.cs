using System.Text;
using System.Text.Json;

namespace FluentPDF.Mcp;

/// <summary>
/// Formatters for navigation, mutation, render, and page operation responses.
/// </summary>
public static partial class ResponseFormatter
{
    public static string FormatRenderPage(string json, int pageIndex, int dpi)
    {
        if (TryParse(json, out var root))
        {
            var err = FormatError(root);
            if (err != null) return err;

            var width = GetInt(root, "width");
            var height = GetInt(root, "height");
            if (width.HasValue && height.HasValue)
                return $"Rendered page {pageIndex + 1} at {dpi} DPI ({width.Value}x{height.Value} pixels).";
        }

        if (json.Length > 100)
            return $"Rendered page {pageIndex + 1} at {dpi} DPI (image data returned).";

        return json;
    }

    public static string FormatNavigate(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var page = GetInt(root, "currentPage");
        var total = GetInt(root, "totalPages");

        if (page.HasValue && total.HasValue)
            return $"Navigated to page {page.Value} of {total.Value}.";
        if (page.HasValue)
            return $"Navigated to page {page.Value}.";
        return "Navigation complete.";
    }

    public static string FormatZoom(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var level = GetDouble(root, "zoomLevel");
        if (level.HasValue)
            return $"Zoom set to {level.Value:F0}%.";
        return "Zoom updated.";
    }

    public static string FormatTogglePanel(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var panels = new List<string>();
        if (GetBool(root, "sidebarVisible") == true) panels.Add("thumbnails");
        if (GetBool(root, "bookmarksVisible") == true) panels.Add("bookmarks");
        if (GetBool(root, "searchVisible") == true) panels.Add("search");

        if (panels.Count > 0)
            return $"Panel toggled. Currently visible: {string.Join(", ", panels)}.";
        return "Panel toggled. All panels are now hidden.";
    }

    public static string FormatSave(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var path = GetString(root, "filePath");
        if (path != null)
            return $"Document saved to \"{path}\".";
        return "Document saved.";
    }

    public static string FormatClose(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var remaining = GetInt(root, "remainingTabs");
        if (remaining.HasValue)
            return $"Tab closed. {remaining.Value} tab{Plural(remaining.Value)} remaining.";
        return "Tab closed.";
    }

    public static string FormatClick(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var selected = GetBool(root, "selected");
        if (selected == true)
        {
            var idx = GetInt(root, "objectIndex");
            var type = GetInt(root, "objectType");
            var typeName = type switch
            {
                1 => "text", 2 => "path", 3 => "image",
                4 => "shading", 5 => "form", _ => "object"
            };
            return idx.HasValue
                ? $"Selected {typeName} object at index {idx.Value}."
                : $"Selected a {typeName} object.";
        }
        return "No object found at that location.";
    }

    public static string FormatMoveSelection(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var moved = GetInt(root, "movedCount");
        if (moved.HasValue)
            return $"Moved {moved.Value} object{Plural(moved.Value)}.";
        return "Selection moved.";
    }

    public static string FormatDeleteSelection(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var idx = GetInt(root, "deletedIndex");
        var type = GetString(root, "deletedType");
        if (idx.HasValue)
            return $"Deleted object at index {idx.Value}{(type != null ? $" (type: {type})" : "")}.";
        return "Selection deleted.";
    }

    public static string FormatAddText(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        if (GetBool(root, "success") == true)
            return "Text added to the page.";
        return json;
    }

    public static string FormatDrawShape(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var shape = GetString(root, "shape") ?? "shape";
        var id = GetString(root, "id");
        var page = GetInt(root, "page");

        var sb = new StringBuilder();
        sb.Append($"Drew a {shape.ToLower()}");
        if (page.HasValue) sb.Append($" on page {page.Value}");
        if (id != null) sb.Append($" (id: {id})");
        sb.Append('.');
        return sb.ToString();
    }

    public static string FormatListShapes(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var count = root.GetArrayLength();
            return count == 0 ? "No shapes found." : $"{count} shape{Plural(count)} found.";
        }

        var total = GetInt(root, "count") ?? GetInt(root, "total");
        if (total.HasValue)
            return $"{total.Value} shape{Plural(total.Value)} found.";
        return json;
    }

    public static string FormatDeleteShape(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        if (GetBool(root, "success") == true) return "Shape deleted.";
        return json;
    }

    public static string FormatPageOp(string json, string action)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var page = GetInt(root, "currentPage");
        var total = GetInt(root, "totalPages");
        var actionDesc = action.ToLowerInvariant() switch
        {
            "rotate_cw" => "Rotated page clockwise",
            "rotate_ccw" => "Rotated page counter-clockwise",
            "delete" => "Deleted page",
            "insert_blank" => "Inserted blank page",
            _ => $"Performed {action}"
        };

        if (page.HasValue && total.HasValue)
            return $"{actionDesc}. Now on page {page.Value} of {total.Value}.";
        return $"{actionDesc}.";
    }

    public static string FormatHighlight(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        if (GetBool(root, "success") == true) return "Highlight added to the page.";
        return json;
    }

    public static string FormatScreenshot(string json)
    {
        if (TryParse(json, out var root))
        {
            var err = FormatError(root);
            if (err != null) return err;
        }
        if (json.Length > 100) return "Screenshot captured.";
        return json;
    }

    public static string FormatObjectDetail(string json)
    {
        if (!TryParse(json, out var root)) return json;
        var err = FormatError(root);
        if (err != null) return err;

        var idx = GetInt(root, "index");
        var type = GetString(root, "type");

        var sb = new StringBuilder();
        sb.Append($"Object {idx ?? 0}: {type ?? "unknown"}");

        if (root.TryGetProperty("boundingBox", out var bb) && bb.ValueKind == JsonValueKind.Object)
        {
            var w = GetDouble(bb, "width");
            var h = GetDouble(bb, "height");
            if (w.HasValue && h.HasValue)
                sb.Append($" ({w.Value:F1} x {h.Value:F1} points)");
        }

        sb.Append('.');
        return sb.ToString();
    }

    public static string FormatFindPages(string json)
    {
        if (!TryParse(json, out var root)) return json;

        if (root.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
        {
            var count = pages.GetArrayLength();
            var sb = new StringBuilder();
            sb.AppendLine($"Extracted text from {count} page{Plural(count)}:");
            sb.AppendLine();

            foreach (var page in pages.EnumerateArray())
            {
                var pageNum = GetInt(page, "page");
                var text = GetString(page, "text");
                if (pageNum.HasValue && text != null)
                {
                    var preview = text.Length > 200 ? text[..200] + "..." : text;
                    sb.AppendLine($"--- Page {pageNum.Value} ---");
                    sb.AppendLine(preview.Trim());
                    sb.AppendLine();
                }
            }
            return sb.ToString().TrimEnd();
        }
        return json;
    }

    public static string FormatSummarizePage(string json, int pageNumber)
    {
        var text = FormatGetText(json);
        if (string.IsNullOrWhiteSpace(text))
            return $"Page {pageNumber} contains no extractable text.";
        return text;
    }

    private static string Plural(int count, string suffix = "s") => count == 1 ? "" : suffix;

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}
