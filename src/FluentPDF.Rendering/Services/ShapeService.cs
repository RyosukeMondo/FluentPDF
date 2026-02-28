using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for drawing shapes on PDF pages using PDFium page objects.
/// Uses PageHandleCache to keep modified pages alive for instant rendering.
/// Uses ContentStreamPatcher + QPDF for save (bypasses GenerateContent to preserve CIDFont text).
/// </summary>
public sealed class ShapeService : IShapeService
{
    private readonly ILogger<ShapeService> _logger;
    private readonly Func<string, PdfDocument?> _documentResolver;
    private readonly ContentStreamPatcher _patcher;
    private readonly PageHandleCache _pageCache;

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
        _logger.LogInformation("FlushDirtyPages: using content stream patching (no GenerateContent)");
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

    public Task<bool> AddRectangleAsync(
        string documentId, int pageNumber,
        double x, double y, double width, double height,
        string fillColor = "#FF0000", string strokeColor = "#000000",
        float strokeWidth = 1f, float opacity = 1f)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var rect = PdfiumInterop.CreateRectObject((float)x, (float)y, (float)width, (float)height);
        if (rect == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create rectangle for page {Page}", pageNumber);
            return Task.FromResult(false);
        }

        var (fr, fg, fb, fa) = ParseColor(fillColor, opacity);
        var (sr, sg, sb, sa) = ParseColor(strokeColor, opacity);

        PdfiumInterop.SetPageObjectFillColor(rect, fr, fg, fb, fa);
        PdfiumInterop.SetPageObjectStrokeColor(rect, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(rect, strokeWidth);
        PdfiumInterop.SetPathDrawMode(rect, fillMode: 1, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, rect);
        _patcher.RecordNewObjectOperators(pageNumber,
            PdfOperatorWriter.Rectangle((float)x, (float)y, (float)width, (float)height,
                fr, fg, fb, fa, sr, sg, sb, sa, strokeWidth));

        _logger.LogInformation("Added rectangle to page {Page}", pageNumber);
        return Task.FromResult(true);
    }

    public Task<bool> AddCircleAsync(
        string documentId, int pageNumber,
        double centerX, double centerY, double radius,
        string fillColor = "#0000FF", string strokeColor = "#000000",
        float strokeWidth = 1f, float opacity = 1f)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        const float k = 0.5523f;
        var cx = (float)centerX;
        var cy = (float)centerY;
        var r = (float)radius;

        var path = PdfiumInterop.CreatePathObject(cx + r, cy);
        if (path == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create circle for page {Page}", pageNumber);
            return Task.FromResult(false);
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
        _patcher.RecordNewObjectOperators(pageNumber,
            PdfOperatorWriter.Circle(cx, cy, r, fr, fg, fb, fa, sr, sg, sb, sa, strokeWidth));

        _logger.LogInformation("Added circle to page {Page}", pageNumber);
        return Task.FromResult(true);
    }

    public Task<bool> AddLineAsync(
        string documentId, int pageNumber,
        double x1, double y1, double x2, double y2,
        string strokeColor = "#000000", float strokeWidth = 2f)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var path = PdfiumInterop.CreatePathObject((float)x1, (float)y1);
        if (path == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create line for page {Page}", pageNumber);
            return Task.FromResult(false);
        }

        PdfiumInterop.PathLineTo(path, (float)x2, (float)y2);
        var (sr, sg, sb, sa) = ParseColor(strokeColor);
        PdfiumInterop.SetPageObjectStrokeColor(path, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(path, strokeWidth);
        PdfiumInterop.SetPathDrawMode(path, fillMode: 0, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, path);
        _patcher.RecordNewObjectOperators(pageNumber,
            PdfOperatorWriter.Line((float)x1, (float)y1, (float)x2, (float)y2, sr, sg, sb, sa, strokeWidth));

        _logger.LogInformation("Added line to page {Page}", pageNumber);
        return Task.FromResult(true);
    }

    public Task<bool> AddFreehandPathAsync(
        string documentId, int pageNumber, double[] points,
        string strokeColor = "#000000", float strokeWidth = 2f)
    {
        if (points == null || points.Length < 4 || points.Length % 2 != 0)
        {
            _logger.LogWarning("Invalid points for freehand: {Length}", points?.Length ?? 0);
            return Task.FromResult(false);
        }

        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var path = PdfiumInterop.CreatePathObject((float)points[0], (float)points[1]);
        if (path == IntPtr.Zero) return Task.FromResult(false);

        for (int i = 2; i < points.Length; i += 2)
            PdfiumInterop.PathLineTo(path, (float)points[i], (float)points[i + 1]);

        var (sr, sg, sb, sa) = ParseColor(strokeColor);
        PdfiumInterop.SetPageObjectStrokeColor(path, sr, sg, sb, sa);
        PdfiumInterop.SetStrokeWidth(path, strokeWidth);
        PdfiumInterop.SetPathDrawMode(path, fillMode: 0, stroke: true);

        PdfiumInterop.InsertPageObject(pageHandle, path);
        _patcher.RecordNewObjectOperators(pageNumber,
            PdfOperatorWriter.FreehandPath(points, sr, sg, sb, sa, strokeWidth));

        _logger.LogInformation("Added freehand ({Count} points) to page {Page}", points.Length / 2, pageNumber);
        return Task.FromResult(true);
    }

    public Task<bool> AddTextAsync(
        string documentId, int pageNumber,
        double x, double y, string text,
        float fontSize = 12f, string fontName = "Helvetica", string color = "#000000")
    {
        if (string.IsNullOrEmpty(text)) return Task.FromResult(false);

        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null || docHandle == null) return Task.FromResult(false);

        var font = PdfiumInterop.LoadStandardFont(docHandle, fontName);
        if (font == IntPtr.Zero) return Task.FromResult(false);

        var textObj = PdfiumInterop.CreateTextObject(docHandle, font, fontSize);
        if (textObj == IntPtr.Zero) return Task.FromResult(false);

        if (!PdfiumInterop.SetTextObjectText(textObj, text))
        {
            PdfiumInterop.DestroyPageObject(textObj);
            return Task.FromResult(false);
        }

        var (cr, cg, cb, ca) = ParseColor(color);
        PdfiumInterop.SetPageObjectFillColor(textObj, cr, cg, cb, ca);
        PdfiumInterop.TransformPageObject(textObj, 1, 0, 0, 1, x, y);

        PdfiumInterop.InsertPageObject(pageHandle, textObj);
        var fontResourceName = fontName switch
        {
            "Helvetica" => "Helv",
            "Times-Roman" or "Times New Roman" => "TiRo",
            "Courier" => "Cour",
            _ => "Helv"
        };
        _patcher.RecordNewObjectOperators(pageNumber,
            PdfOperatorWriter.Text((float)x, (float)y, text, fontSize, fontResourceName, cr, cg, cb, ca));

        _logger.LogInformation("Added text to page {Page}", pageNumber);
        return Task.FromResult(true);
    }

    public Task<List<PageObjectInfo>> GetPageObjectsAsync(string documentId, int pageNumber)
    {
        var result = new List<PageObjectInfo>();
        var (docHandle, pageHandle, isCached) = ResolvePageForRead(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(result);

        try
        {
            var count = PdfiumInterop.GetPageObjectCount(pageHandle);
            for (int i = 0; i < count; i++)
            {
                var obj = PdfiumInterop.GetPageObject(pageHandle, i);
                if (obj == IntPtr.Zero) continue;

                var type = PdfiumInterop.GetPageObjectType(obj);
                if (PdfiumInterop.GetPageObjectBounds(obj, out var left, out var bottom, out var right, out var top))
                    result.Add(new PageObjectInfo(i, type, left, bottom, right, top));
            }
        }
        finally
        {
            if (!isCached) pageHandle.Dispose();
        }

        return Task.FromResult(result);
    }

    public Task<bool> RemovePageObjectAsync(string documentId, int pageNumber, int objectIndex)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero)
        {
            _logger.LogWarning("Object {Index} not found on page {Page}", objectIndex, pageNumber);
            return Task.FromResult(false);
        }

        var removed = PdfiumInterop.RemovePageObject(pageHandle, obj);
        if (removed)
        {
            // RemovePageObject requires GenerateContent to persist the structural change.
            // This may corrupt CIDFont text on the page. Use with caution on CJK documents.
            PdfiumInterop.GenerateContent(pageHandle);
            _logger.LogInformation("Removed object {Index} from page {Page}", objectIndex, pageNumber);
        }
        return Task.FromResult(removed);
    }

    public Task<bool> MovePageObjectAsync(string documentId, int pageNumber, int objectIndex, float deltaX, float deltaY)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var count = PdfiumInterop.GetPageObjectCount(pageHandle);
            if (objectIndex < 0 || objectIndex >= count) return Task.FromResult(false);

            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero) return Task.FromResult(false);

            if (PdfiumInterop.GetPageObjectMatrix(obj, out var a, out var b, out var c, out var d, out var e, out var f))
            {
                PdfiumInterop.SetPageObjectMatrix(obj, a, b, c, d, e + deltaX, f + deltaY);
                RecordMatrixChange(pageNumber, a, b, c, d, e, f, a, b, c, d, e + deltaX, f + deltaY);
            }
            else
            {
                PdfiumInterop.TransformPageObject(obj, 1, 0, 0, 1, deltaX, deltaY);
            }

            _logger.LogInformation("Moved object {Index} on page {Page} by ({DX}, {DY})", objectIndex, pageNumber, deltaX, deltaY);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move object {Index} on page {Page}", objectIndex, pageNumber);
            return Task.FromResult(false);
        }
    }

    public Task<int> MovePageObjectsBatchAsync(string documentId, int pageNumber, int[] objectIndices, float deltaX, float deltaY)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(0);

        try
        {
            var count = PdfiumInterop.GetPageObjectCount(pageHandle);
            int moved = 0;

            foreach (var idx in objectIndices)
            {
                if (idx < 0 || idx >= count) continue;
                var obj = PdfiumInterop.GetPageObject(pageHandle, idx);
                if (obj == IntPtr.Zero) continue;

                if (PdfiumInterop.GetPageObjectMatrix(obj, out var a, out var b, out var c, out var d, out var e, out var f))
                {
                    PdfiumInterop.SetPageObjectMatrix(obj, a, b, c, d, e + deltaX, f + deltaY);
                    RecordMatrixChange(pageNumber, a, b, c, d, e, f, a, b, c, d, e + deltaX, f + deltaY);
                }
                else
                {
                    PdfiumInterop.TransformPageObject(obj, 1, 0, 0, 1, deltaX, deltaY);
                }
                moved++;
            }

            _logger.LogInformation("Batch moved {Count} objects on page {Page}", moved, pageNumber);
            return Task.FromResult(moved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch move objects on page {Page}", pageNumber);
            return Task.FromResult(0);
        }
    }

    public Task<bool> ResizePageObjectAsync(string documentId, int pageNumber, int objectIndex,
        float scaleX, float scaleY, float anchorPdfX, float anchorPdfY)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var count = PdfiumInterop.GetPageObjectCount(pageHandle);
            if (objectIndex < 0 || objectIndex >= count) return Task.FromResult(false);

            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero) return Task.FromResult(false);

            scaleX = Math.Max(scaleX, 0.05f);
            scaleY = Math.Max(scaleY, 0.05f);

            if (PdfiumInterop.GetPageObjectMatrix(obj, out var a, out var b, out var c, out var d, out var e, out var f))
            {
                var newA = a * scaleX;
                var newB = b * scaleX;
                var newC = c * scaleY;
                var newD = d * scaleY;
                var newE = anchorPdfX + scaleX * (e - anchorPdfX);
                var newF = anchorPdfY + scaleY * (f - anchorPdfY);
                PdfiumInterop.SetPageObjectMatrix(obj, newA, newB, newC, newD, newE, newF);
                RecordMatrixChange(pageNumber, a, b, c, d, e, f, newA, newB, newC, newD, newE, newF);
            }
            else
            {
                PdfiumInterop.TransformPageObject(obj, scaleX, 0, 0, scaleY,
                    anchorPdfX * (1 - scaleX), anchorPdfY * (1 - scaleY));
            }

            _logger.LogInformation("Resized object {Index} on page {Page}", objectIndex, pageNumber);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize object {Index}", objectIndex);
            return Task.FromResult(false);
        }
    }

    public Task<PageObjectProperties?> GetPageObjectPropertiesAsync(string documentId, int pageNumber, int objectIndex)
    {
        var (docHandle, pageHandle, isCached) = ResolvePageForRead(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult((PageObjectProperties?)null);

        try
        {
            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero) return Task.FromResult((PageObjectProperties?)null);

            PdfiumInterop.GetPageObjectStrokeColor(obj, out var sr, out var sg, out var sb, out var sa);
            PdfiumInterop.GetPageObjectFillColor(obj, out var fr, out var fg, out var fb, out var fa);
            PdfiumInterop.GetStrokeWidth(obj, out var strokeWidth);

            return Task.FromResult((PageObjectProperties?)new PageObjectProperties(
                $"#{sr:X2}{sg:X2}{sb:X2}{sa:X2}", $"#{fr:X2}{fg:X2}{fb:X2}{fa:X2}", strokeWidth));
        }
        finally
        {
            if (!isCached) pageHandle.Dispose();
        }
    }

    public Task<bool> SetPageObjectStrokeColorAsync(string documentId, int pageNumber, int objectIndex, string color)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero) return Task.FromResult(false);

        var (r, g, b, a) = ParseColor(color);
        PdfiumInterop.SetPageObjectStrokeColor(obj, r, g, b, a);
        return Task.FromResult(true);
    }

    public Task<bool> SetPageObjectFillColorAsync(string documentId, int pageNumber, int objectIndex, string color)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero) return Task.FromResult(false);

        var (r, g, b, a) = ParseColor(color);
        PdfiumInterop.SetPageObjectFillColor(obj, r, g, b, a);
        return Task.FromResult(true);
    }

    public Task<bool> SetPageObjectStrokeWidthAsync(string documentId, int pageNumber, int objectIndex, float width)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero) return Task.FromResult(false);

        PdfiumInterop.SetStrokeWidth(obj, width);
        return Task.FromResult(true);
    }

    #region Page Resolution

    /// <summary>
    /// Resolves a page for mutation. The handle is cached so in-memory changes
    /// persist for rendering (no GenerateContent needed).
    /// Caller must NOT dispose the returned handle.
    /// </summary>
    private (SafePdfDocumentHandle? docHandle, SafePdfPageHandle? pageHandle) ResolvePageForMutation(
        string documentId, int pageNumber)
    {
        var doc = _documentResolver(documentId);
        if (doc == null) return (null, null);

        var docHandle = (SafePdfDocumentHandle)doc.Handle;
        if (docHandle.IsInvalid) return (null, null);

        var (pageHandle, isCached) = _pageCache.GetOrLoad(docHandle, pageNumber);
        if (pageHandle.IsInvalid)
        {
            if (!isCached) pageHandle.Dispose();
            return (docHandle, null);
        }

        // Cache the handle so it stays alive for subsequent renders
        if (!isCached)
            _pageCache.CacheModifiedPage(docHandle, pageNumber, pageHandle);

        return (docHandle, pageHandle);
    }

    /// <summary>
    /// Resolves a page for read-only access. If cached (modified), borrows the handle.
    /// If not cached, opens a new handle that caller must dispose.
    /// </summary>
    private (SafePdfDocumentHandle? docHandle, SafePdfPageHandle? pageHandle, bool isCached) ResolvePageForRead(
        string documentId, int pageNumber)
    {
        var doc = _documentResolver(documentId);
        if (doc == null) return (null, null, false);

        var docHandle = (SafePdfDocumentHandle)doc.Handle;
        if (docHandle.IsInvalid) return (null, null, false);

        var (pageHandle, isCached) = _pageCache.GetOrLoad(docHandle, pageNumber);
        if (pageHandle.IsInvalid)
        {
            if (!isCached) pageHandle.Dispose();
            return (docHandle, null, false);
        }

        return (docHandle, pageHandle, isCached);
    }

    #endregion

    private void RecordMatrixChange(int pageNumber,
        float origA, float origB, float origC, float origD, float origE, float origF,
        float newA, float newB, float newC, float newD, float newE, float newF)
    {
        _patcher.RecordMatrixPatch(pageNumber, new MatrixPatch(
            origA, origB, origC, origD, origE, origF,
            newA, newB, newC, newD, newE, newF));
    }

    private static (uint r, uint g, uint b, uint a) ParseColor(string hex, float opacity = 1f)
    {
        hex = hex.TrimStart('#');
        uint r = Convert.ToUInt32(hex[..2], 16);
        uint g = Convert.ToUInt32(hex[2..4], 16);
        uint b = Convert.ToUInt32(hex[4..6], 16);
        uint a = hex.Length >= 8 ? Convert.ToUInt32(hex[6..8], 16) : (uint)(opacity * 255);
        return (r, g, b, a);
    }
}
