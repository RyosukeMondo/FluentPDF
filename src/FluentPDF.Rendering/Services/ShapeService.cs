using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for drawing shapes on PDF pages using PDFium page objects.
/// All page parameters use <see cref="PageIndex"/> (0-based) — single source of truth.
/// </summary>
public sealed partial class ShapeService : IShapeService
{
    private readonly ILogger<ShapeService> _logger;
    private readonly Func<string, PdfDocument?> _documentResolver;
    private readonly ContentStreamPatcher _patcher;
    private readonly PageHandleCache _pageCache;
    private readonly Dictionary<string, (ShapeMetadata Meta, int PageObjectIndex)> _tracked = new();

    public ShapeService(
        ILogger<ShapeService> logger,
        Func<string, PdfDocument?> documentResolver,
        ContentStreamPatcher patcher,
        PageHandleCache pageCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
        _patcher = patcher ?? throw new ArgumentNullException(nameof(patcher));
        _pageCache = pageCache ?? throw new ArgumentNullException(nameof(pageCache));
    }

    public void FlushDirtyPages(string documentId)
    {
        var doc = _documentResolver(documentId);
        if (doc == null) return;
        var docHandle = (SafePdfDocumentHandle)doc.Handle;
        var flushed = _pageCache.FlushGenerateContent(docHandle);
        _logger.LogInformation("FlushDirtyPages: called GenerateContent on {Count} cached pages", flushed);
    }

    public bool SaveWithContentStreamPatching(string documentId, string outputPath)
    {
        if (!_patcher.HasPendingChanges)
        {
            _logger.LogInformation("No content stream changes to patch for {DocId}", documentId);
            return false;
        }
        return _patcher.SaveWithPatches(documentId, outputPath);
    }

    public ContentStreamPatcher Patcher => _patcher;
    public PageHandleCache PageCache => _pageCache;

    public Task<string?> AddRectangleAsync(
        string documentId, PageIndex pageIndex,
        double x, double y, double width, double height,
        string fillColor = "#FF0000", string strokeColor = "#000000",
        float strokeWidth = 1f, float opacity = 1f, string source = "user")
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult((string?)null);

        var rect = PdfiumInterop.CreateRectObject((float)x, (float)y, (float)width, (float)height);
        if (rect == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create rectangle for page {Page}", pageIndex);
            return Task.FromResult((string?)null);
        }

        var (fr, fg, fb, fa) = ParseColor(fillColor, opacity);
        var (sr, sg, sb, sa) = ParseColor(strokeColor, opacity);

        PdfiumInterop.SetPageObjectFillColor(rect, fr, fg, fb, fa);
        PdfiumInterop.SetPageObjectStrokeColor(rect, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(rect, strokeWidth);
        PdfiumInterop.SetPathDrawMode(rect, fillMode: 1, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, rect);
        PdfiumInterop.GenerateContent(pageHandle);
        _patcher.RecordNewObjectOperators(pageIndex.Value,
            PdfOperatorWriter.Rectangle((float)x, (float)y, (float)width, (float)height,
                fr, fg, fb, fa, sr, sg, sb, sa, strokeWidth));

        var idx = PdfiumInterop.GetPageObjectCount(pageHandle) - 1;
        var id = TrackShape("rectangle", pageIndex, (float)x, (float)y, (float)(x + width), (float)(y + height),
            fillColor, strokeColor, strokeWidth, source, idx);

        _logger.LogInformation("Added rectangle {Id} to page {Page}", id, pageIndex);
        return Task.FromResult((string?)id);
    }

    public Task<string?> AddCircleAsync(
        string documentId, PageIndex pageIndex,
        double centerX, double centerY, double radius,
        string fillColor = "#0000FF", string strokeColor = "#000000",
        float strokeWidth = 1f, float opacity = 1f, string source = "user")
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult((string?)null);

        const float k = 0.5523f;
        var cx = (float)centerX;
        var cy = (float)centerY;
        var r = (float)radius;

        var path = PdfiumInterop.CreatePathObject(cx + r, cy);
        if (path == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create circle for page {Page}", pageIndex);
            return Task.FromResult((string?)null);
        }

        PdfiumInterop.PathBezierTo(path, cx + r, cy + r * k, cx + r * k, cy + r, cx, cy + r);
        PdfiumInterop.PathBezierTo(path, cx - r * k, cy + r, cx - r, cy + r * k, cx - r, cy);
        PdfiumInterop.PathBezierTo(path, cx - r, cy - r * k, cx - r * k, cy - r, cx, cy - r);
        PdfiumInterop.PathBezierTo(path, cx + r * k, cy - r, cx + r, cy - r * k, cx + r, cy);
        PdfiumInterop.PathClose(path);

        var (fr, fg, fb, fa) = ParseColor(fillColor, opacity);
        var (sr, sg, sb, sa) = ParseColor(strokeColor, opacity);

        PdfiumInterop.SetPageObjectFillColor(path, fr, fg, fb, fa);
        PdfiumInterop.SetPageObjectStrokeColor(path, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(path, strokeWidth);
        PdfiumInterop.SetPathDrawMode(path, fillMode: 1, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, path);
        PdfiumInterop.GenerateContent(pageHandle);
        _patcher.RecordNewObjectOperators(pageIndex.Value,
            PdfOperatorWriter.Circle(cx, cy, r, fr, fg, fb, fa, sr, sg, sb, sa, strokeWidth));

        var idx = PdfiumInterop.GetPageObjectCount(pageHandle) - 1;
        var id = TrackShape("circle", pageIndex, cx - r, cy - r, cx + r, cy + r,
            fillColor, strokeColor, strokeWidth, source, idx);

        _logger.LogInformation("Added circle {Id} to page {Page}", id, pageIndex);
        return Task.FromResult((string?)id);
    }

    public Task<string?> AddLineAsync(
        string documentId, PageIndex pageIndex,
        double x1, double y1, double x2, double y2,
        string strokeColor = "#000000", float strokeWidth = 2f, string source = "user")
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult((string?)null);

        var path = PdfiumInterop.CreatePathObject((float)x1, (float)y1);
        if (path == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create line for page {Page}", pageIndex);
            return Task.FromResult((string?)null);
        }

        PdfiumInterop.PathLineTo(path, (float)x2, (float)y2);
        var (sr, sg, sb, sa) = ParseColor(strokeColor);
        PdfiumInterop.SetPageObjectStrokeColor(path, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(path, strokeWidth);
        PdfiumInterop.SetPathDrawMode(path, fillMode: 0, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, path);
        PdfiumInterop.GenerateContent(pageHandle);
        _patcher.RecordNewObjectOperators(pageIndex.Value,
            PdfOperatorWriter.Line((float)x1, (float)y1, (float)x2, (float)y2, sr, sg, sb, sa, strokeWidth));

        var left = Math.Min((float)x1, (float)x2);
        var bottom = Math.Min((float)y1, (float)y2);
        var right = Math.Max((float)x1, (float)x2);
        var top = Math.Max((float)y1, (float)y2);
        var idx = PdfiumInterop.GetPageObjectCount(pageHandle) - 1;
        var id = TrackShape("line", pageIndex, left, bottom, right, top,
            "", strokeColor, strokeWidth, source, idx);

        _logger.LogInformation("Added line {Id} to page {Page}", id, pageIndex);
        return Task.FromResult((string?)id);
    }

    public Task<string?> AddFreehandPathAsync(
        string documentId, PageIndex pageIndex, double[] points,
        string strokeColor = "#000000", float strokeWidth = 2f, string source = "user")
    {
        if (points == null || points.Length < 4 || points.Length % 2 != 0)
        {
            _logger.LogWarning("Invalid points for freehand: {Length}", points?.Length ?? 0);
            return Task.FromResult((string?)null);
        }

        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult((string?)null);

        var path = PdfiumInterop.CreatePathObject((float)points[0], (float)points[1]);
        if (path == IntPtr.Zero) return Task.FromResult((string?)null);

        for (int i = 2; i < points.Length; i += 2)
            PdfiumInterop.PathLineTo(path, (float)points[i], (float)points[i + 1]);

        var (sr, sg, sb, sa) = ParseColor(strokeColor);
        PdfiumInterop.SetPageObjectStrokeColor(path, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(path, strokeWidth);
        PdfiumInterop.SetPathDrawMode(path, fillMode: 0, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, path);
        PdfiumInterop.GenerateContent(pageHandle);
        _patcher.RecordNewObjectOperators(pageIndex.Value,
            PdfOperatorWriter.FreehandPath(points, sr, sg, sb, sa, strokeWidth));

        float fhLeft = float.MaxValue, fhBottom = float.MaxValue, fhRight = float.MinValue, fhTop = float.MinValue;
        for (int i = 0; i < points.Length; i += 2)
        {
            fhLeft = Math.Min(fhLeft, (float)points[i]);
            fhRight = Math.Max(fhRight, (float)points[i]);
            fhBottom = Math.Min(fhBottom, (float)points[i + 1]);
            fhTop = Math.Max(fhTop, (float)points[i + 1]);
        }
        var idx = PdfiumInterop.GetPageObjectCount(pageHandle) - 1;
        var id = TrackShape("freehand", pageIndex, fhLeft, fhBottom, fhRight, fhTop,
            "", strokeColor, strokeWidth, source, idx);

        _logger.LogInformation("Added freehand {Id} ({Count} points) to page {Page}", id, points.Length / 2, pageIndex);
        return Task.FromResult((string?)id);
    }

    public Task<string?> AddTextAsync(
        string documentId, PageIndex pageIndex,
        double x, double y, string text,
        float fontSize = 12f, string fontName = "Helvetica", string color = "#000000", string source = "user")
    {
        if (string.IsNullOrEmpty(text)) return Task.FromResult((string?)null);

        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null || docHandle == null) return Task.FromResult((string?)null);

        var font = PdfiumInterop.LoadStandardFont(docHandle, fontName);
        if (font == IntPtr.Zero) return Task.FromResult((string?)null);

        var textObj = PdfiumInterop.CreateTextObject(docHandle, font, fontSize);
        if (textObj == IntPtr.Zero) return Task.FromResult((string?)null);

        if (!PdfiumInterop.SetTextObjectText(textObj, text))
        {
            PdfiumInterop.DestroyPageObject(textObj);
            return Task.FromResult((string?)null);
        }

        var (cr, cg, cb, ca) = ParseColor(color);
        PdfiumInterop.SetPageObjectFillColor(textObj, cr, cg, cb, ca);
        PdfiumInterop.TransformPageObject(textObj, 1, 0, 0, 1, x, y);

        PdfiumInterop.InsertPageObject(pageHandle, textObj);
        PdfiumInterop.GenerateContent(pageHandle);
        var fontResourceName = fontName switch
        {
            "Helvetica" => "Helv",
            "Times-Roman" or "Times New Roman" => "TiRo",
            "Courier" => "Cour",
            _ => "Helv"
        };
        _patcher.RecordNewObjectOperators(pageIndex.Value,
            PdfOperatorWriter.Text((float)x, (float)y, text, fontSize, fontResourceName, cr, cg, cb, ca));

        var idx = PdfiumInterop.GetPageObjectCount(pageHandle) - 1;
        var id = TrackShape("text", pageIndex, (float)x, (float)y, (float)x, (float)y,
            color, "", 0, source, idx);

        _logger.LogInformation("Added text {Id} to page {Page}", id, pageIndex);
        return Task.FromResult((string?)id);
    }
}
