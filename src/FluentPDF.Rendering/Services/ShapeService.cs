using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for drawing shapes on PDF pages using PDFium page objects.
/// Supports rectangles, circles (Bezier approximation), lines, freehand paths, and text.
/// All methods execute synchronously on the calling thread because PDFium is not thread-safe.
/// </summary>
public sealed class ShapeService : IShapeService
{
    private readonly ILogger<ShapeService> _logger;
    private readonly Func<string, PdfDocument?> _documentResolver;
    private readonly ContentStreamPatcher _patcher;

    /// <summary>
    /// Maps documentId to original file path (at load time) so we can patch from the original.
    /// </summary>
    private readonly Dictionary<string, string> _originalPaths = new();

    public ShapeService(
        ILogger<ShapeService> logger,
        Func<string, PdfDocument?> documentResolver,
        ContentStreamPatcher patcher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
        _patcher = patcher ?? throw new ArgumentNullException(nameof(patcher));
    }

    /// <summary>
    /// Saves the document by patching content streams via QPDF (bypasses GenerateContent).
    /// Returns true if save succeeded.
    /// </summary>
    public void FlushDirtyPages(string documentId)
    {
        // No-op: we no longer call GenerateContent.
        // All changes are tracked in ContentStreamPatcher and applied at save time.
        _logger.LogInformation("FlushDirtyPages called for {DocId} - using content stream patching (no GenerateContent)", documentId);
    }

    /// <summary>
    /// Saves via QPDF content stream patching. Call this instead of FPDF_SaveAsCopy.
    /// </summary>
    public bool SaveWithContentStreamPatching(string documentId, string outputPath)
    {
        if (!_patcher.HasPendingChanges)
        {
            _logger.LogInformation("No content stream changes to patch for {DocId}", documentId);
            return false; // Caller should fall back to normal save
        }

        var inputPath = documentId; // documentId is the file path
        return _patcher.SaveWithPatches(inputPath, outputPath);
    }

    public ContentStreamPatcher Patcher => _patcher;

    public Task<bool> AddRectangleAsync(
        string documentId, int pageNumber,
        double x, double y, double width, double height,
        string fillColor = "#FF0000", string strokeColor = "#000000",
        float strokeWidth = 1f, float opacity = 1f)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var rect = PdfiumInterop.CreateRectObject((float)x, (float)y, (float)width, (float)height);
            if (rect == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to create rectangle object for document {DocumentId}, page {Page}", documentId, pageNumber);
                return Task.FromResult(false);
            }

            var (fr, fg, fb, fa) = ParseColor(fillColor, opacity);
            var (sr, sg, sb, sa) = ParseColor(strokeColor, opacity);

            PdfiumInterop.SetPageObjectFillColor(rect, fr, fg, fb, fa);
            PdfiumInterop.SetPageObjectStrokeColor(rect, sr, sg, sb, sa);
            PdfiumInterop.SetStrokeWidth(rect, strokeWidth);
            PdfiumInterop.SetPathDrawMode(rect, fillMode: 1, stroke: true);

            PdfiumInterop.InsertPageObject(pageHandle, rect);
            // Record operators for QPDF save (no GenerateContent — corrupts CIDFont)
            _patcher.RecordNewObjectOperators(pageNumber,
                PdfOperatorWriter.Rectangle((float)x, (float)y, (float)width, (float)height,
                    fr, fg, fb, fa, sr, sg, sb, sa, strokeWidth));

            _logger.LogInformation("Added rectangle to document {DocumentId}, page {Page}", documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> AddCircleAsync(
        string documentId, int pageNumber,
        double centerX, double centerY, double radius,
        string fillColor = "#0000FF", string strokeColor = "#000000",
        float strokeWidth = 1f, float opacity = 1f)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            // Approximate circle with 4 cubic Bezier curves.
            // Control point factor for circle approximation: 4*(sqrt(2)-1)/3 ≈ 0.5523
            const float k = 0.5523f;
            var cx = (float)centerX;
            var cy = (float)centerY;
            var r = (float)radius;

            var path = PdfiumInterop.CreatePathObject(cx + r, cy);
            if (path == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to create circle path for document {DocumentId}, page {Page}", documentId, pageNumber);
                return Task.FromResult(false);
            }

            // Top-right quadrant
            PdfiumInterop.PathBezierTo(path, cx + r, cy + r * k, cx + r * k, cy + r, cx, cy + r);
            // Top-left quadrant
            PdfiumInterop.PathBezierTo(path, cx - r * k, cy + r, cx - r, cy + r * k, cx - r, cy);
            // Bottom-left quadrant
            PdfiumInterop.PathBezierTo(path, cx - r, cy - r * k, cx - r * k, cy - r, cx, cy - r);
            // Bottom-right quadrant
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

            _logger.LogInformation("Added circle to document {DocumentId}, page {Page}", documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> AddLineAsync(
        string documentId, int pageNumber,
        double x1, double y1, double x2, double y2,
        string strokeColor = "#000000", float strokeWidth = 2f)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var path = PdfiumInterop.CreatePathObject((float)x1, (float)y1);
            if (path == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to create line path for document {DocumentId}, page {Page}", documentId, pageNumber);
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

            _logger.LogInformation("Added line to document {DocumentId}, page {Page}", documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> AddFreehandPathAsync(
        string documentId, int pageNumber, double[] points,
        string strokeColor = "#000000", float strokeWidth = 2f)
    {
        if (points == null || points.Length < 4 || points.Length % 2 != 0)
        {
            _logger.LogWarning("Invalid points array for freehand path: need at least 2 points (4 values), got {Length}", points?.Length ?? 0);
            return Task.FromResult(false);
        }

        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var path = PdfiumInterop.CreatePathObject((float)points[0], (float)points[1]);
            if (path == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to create freehand path for document {DocumentId}, page {Page}", documentId, pageNumber);
                return Task.FromResult(false);
            }

            for (int i = 2; i < points.Length; i += 2)
            {
                PdfiumInterop.PathLineTo(path, (float)points[i], (float)points[i + 1]);
            }

            var (sr, sg, sb, sa) = ParseColor(strokeColor);

            PdfiumInterop.SetPageObjectStrokeColor(path, sr, sg, sb, sa);
            PdfiumInterop.SetStrokeWidth(path, strokeWidth);
            PdfiumInterop.SetPathDrawMode(path, fillMode: 0, stroke: true);

            PdfiumInterop.InsertPageObject(pageHandle, path);
            _patcher.RecordNewObjectOperators(pageNumber,
                PdfOperatorWriter.FreehandPath(points, sr, sg, sb, sa, strokeWidth));

            _logger.LogInformation("Added freehand path ({PointCount} points) to document {DocumentId}, page {Page}",
                points.Length / 2, documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> AddTextAsync(
        string documentId, int pageNumber,
        double x, double y, string text,
        float fontSize = 12f, string fontName = "Helvetica", string color = "#000000")
    {
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning("Empty text provided for AddTextAsync");
            return Task.FromResult(false);
        }

        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null || docHandle == null) return Task.FromResult(false);

        try
        {
            var font = PdfiumInterop.LoadStandardFont(docHandle, fontName);
            if (font == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to load font {FontName}", fontName);
                return Task.FromResult(false);
            }

            var textObj = PdfiumInterop.CreateTextObject(docHandle, font, fontSize);
            if (textObj == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to create text object for document {DocumentId}, page {Page}", documentId, pageNumber);
                return Task.FromResult(false);
            }

            if (!PdfiumInterop.SetTextObjectText(textObj, text))
            {
                PdfiumInterop.DestroyPageObject(textObj);
                _logger.LogWarning("Failed to set text content");
                return Task.FromResult(false);
            }

            var (cr, cg, cb, ca) = ParseColor(color);
            PdfiumInterop.SetPageObjectFillColor(textObj, cr, cg, cb, ca);

            // Position using identity matrix with translation
            PdfiumInterop.TransformPageObject(textObj, 1, 0, 0, 1, x, y);

            PdfiumInterop.InsertPageObject(pageHandle, textObj);
            // Map PDFium font name to PDF resource name
            var fontResourceName = fontName switch
            {
                "Helvetica" => "Helv",
                "Times-Roman" or "Times New Roman" => "TiRo",
                "Courier" => "Cour",
                _ => "Helv"
            };
            _patcher.RecordNewObjectOperators(pageNumber,
                PdfOperatorWriter.Text((float)x, (float)y, text, fontSize, fontResourceName, cr, cg, cb, ca));

            _logger.LogInformation("Added text to document {DocumentId}, page {Page}", documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<List<PageObjectInfo>> GetPageObjectsAsync(string documentId, int pageNumber)
    {
        var result = new List<PageObjectInfo>();
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
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
                {
                    result.Add(new PageObjectInfo(i, type, left, bottom, right, top));
                }
            }
        }
        finally
        {
            pageHandle.Dispose();
        }

        return Task.FromResult(result);
    }

    public Task<bool> RemovePageObjectAsync(string documentId, int pageNumber, int objectIndex)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null)
        {
            _logger.LogWarning("RemovePageObject: ResolvePage returned null for {DocumentId} page {Page}", documentId, pageNumber);
            return Task.FromResult(false);
        }

        try
        {
            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero)
            {
                _logger.LogWarning("Page object {Index} not found on page {Page}", objectIndex, pageNumber);
                return Task.FromResult(false);
            }

            var removed = PdfiumInterop.RemovePageObject(pageHandle, obj);
            if (removed)
            {
                PdfiumInterop.GenerateContent(pageHandle);
                _logger.LogInformation("Removed page object {Index} from page {Page}", objectIndex, pageNumber);
            }
            else
            {
                _logger.LogWarning("FPDFPage_RemoveObject returned false for index {Index} on page {Page}", objectIndex, pageNumber);
            }
            return Task.FromResult(removed);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> MovePageObjectAsync(string documentId, int pageNumber, int objectIndex, float deltaX, float deltaY)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var count = PdfiumInterop.GetPageObjectCount(pageHandle);
            if (objectIndex < 0 || objectIndex >= count)
            {
                _logger.LogWarning("Object index {Index} out of range (count={Count}) on page {Page}", objectIndex, count, pageNumber);
                return Task.FromResult(false);
            }

            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero)
            {
                _logger.LogWarning("Page object {Index} not found on page {Page}", objectIndex, pageNumber);
                return Task.FromResult(false);
            }

            // Use GetMatrix/SetMatrix to preserve text encoding (Transform corrupts CIDFont Japanese text)
            if (PdfiumInterop.GetPageObjectMatrix(obj, out var ma, out var mb, out var mc, out var md, out var me, out var mf))
            {
                PdfiumInterop.SetPageObjectMatrix(obj, ma, mb, mc, md, me + deltaX, mf + deltaY);
                RecordMatrixChange(pageNumber, ma, mb, mc, md, me, mf, ma, mb, mc, md, me + deltaX, mf + deltaY);
            }
            else
            {
                // Fallback for objects that don't support GetMatrix
                PdfiumInterop.TransformPageObject(obj, 1, 0, 0, 1, deltaX, deltaY);
            }
            MarkPageDirty(documentId, pageNumber);
            _logger.LogInformation("Moved page object {Index} on page {Page} by ({DeltaX}, {DeltaY})", objectIndex, pageNumber, deltaX, deltaY);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move page object {Index} on page {Page}", objectIndex, pageNumber);
            return Task.FromResult(false);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<int> MovePageObjectsBatchAsync(string documentId, int pageNumber, int[] objectIndices, float deltaX, float deltaY)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
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

                if (PdfiumInterop.GetPageObjectMatrix(obj, out var ma, out var mb, out var mc, out var md, out var me, out var mf))
                {
                    PdfiumInterop.SetPageObjectMatrix(obj, ma, mb, mc, md, me + deltaX, mf + deltaY);
                    RecordMatrixChange(pageNumber, ma, mb, mc, md, me, mf, ma, mb, mc, md, me + deltaX, mf + deltaY);
                }
                else
                {
                    PdfiumInterop.TransformPageObject(obj, 1, 0, 0, 1, deltaX, deltaY);
                }
                moved++;
            }

            if (moved > 0)
                MarkPageDirty(documentId, pageNumber);

            _logger.LogInformation("Batch moved {Count} objects on page {Page} by ({DeltaX}, {DeltaY})", moved, pageNumber, deltaX, deltaY);
            return Task.FromResult(moved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch move objects on page {Page}", pageNumber);
            return Task.FromResult(0);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> ResizePageObjectAsync(string documentId, int pageNumber, int objectIndex,
        float scaleX, float scaleY, float anchorPdfX, float anchorPdfY)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var count = PdfiumInterop.GetPageObjectCount(pageHandle);
            if (objectIndex < 0 || objectIndex >= count)
                return Task.FromResult(false);

            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero) return Task.FromResult(false);

            // Clamp to prevent zero/negative scale
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

            MarkPageDirty(documentId, pageNumber);
            _logger.LogInformation("Resized object {Index} on page {Page} by ({ScaleX}, {ScaleY}) anchored at ({AnchorX}, {AnchorY})",
                objectIndex, pageNumber, scaleX, scaleY, anchorPdfX, anchorPdfY);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize object {Index} on page {Page}", objectIndex, pageNumber);
            return Task.FromResult(false);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<PageObjectProperties?> GetPageObjectPropertiesAsync(string documentId, int pageNumber, int objectIndex)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult((PageObjectProperties?)null);

        try
        {
            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero)
            {
                _logger.LogWarning("Page object {Index} not found on page {Page}", objectIndex, pageNumber);
                return Task.FromResult((PageObjectProperties?)null);
            }

            PdfiumInterop.GetPageObjectStrokeColor(obj, out var sr, out var sg, out var sb, out var sa);
            PdfiumInterop.GetPageObjectFillColor(obj, out var fr, out var fg, out var fb, out var fa);
            PdfiumInterop.GetStrokeWidth(obj, out var strokeWidth);

            var strokeColor = $"#{sr:X2}{sg:X2}{sb:X2}{sa:X2}";
            var fillColor = $"#{fr:X2}{fg:X2}{fb:X2}{fa:X2}";

            return Task.FromResult((PageObjectProperties?)new PageObjectProperties(strokeColor, fillColor, strokeWidth));
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> SetPageObjectStrokeColorAsync(string documentId, int pageNumber, int objectIndex, string color)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero)
            {
                _logger.LogWarning("Page object {Index} not found on page {Page}", objectIndex, pageNumber);
                return Task.FromResult(false);
            }

            var (r, g, b, a) = ParseColor(color);
            PdfiumInterop.SetPageObjectStrokeColor(obj, r, g, b, a);
            MarkPageDirty(documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> SetPageObjectFillColorAsync(string documentId, int pageNumber, int objectIndex, string color)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero)
            {
                _logger.LogWarning("Page object {Index} not found on page {Page}", objectIndex, pageNumber);
                return Task.FromResult(false);
            }

            var (r, g, b, a) = ParseColor(color);
            PdfiumInterop.SetPageObjectFillColor(obj, r, g, b, a);
            MarkPageDirty(documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    public Task<bool> SetPageObjectStrokeWidthAsync(string documentId, int pageNumber, int objectIndex, float width)
    {
        var (doc, docHandle, pageHandle) = ResolvePage(documentId, pageNumber);
        if (pageHandle == null) return Task.FromResult(false);

        try
        {
            var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
            if (obj == IntPtr.Zero)
            {
                _logger.LogWarning("Page object {Index} not found on page {Page}", objectIndex, pageNumber);
                return Task.FromResult(false);
            }

            PdfiumInterop.SetStrokeWidth(obj, width);
            MarkPageDirty(documentId, pageNumber);
            return Task.FromResult(true);
        }
        finally
        {
            pageHandle.Dispose();
        }
    }

    private (PdfDocument? doc, SafePdfDocumentHandle? docHandle, SafePdfPageHandle? pageHandle) ResolvePage(
        string documentId, int pageNumber)
    {
        var doc = _documentResolver(documentId);
        if (doc == null)
        {
            _logger.LogWarning("Document not found: {DocumentId}", documentId);
            return (null, null, null);
        }

        var docHandle = (SafePdfDocumentHandle)doc.Handle;
        if (docHandle.IsInvalid)
        {
            _logger.LogWarning("Invalid document handle for {DocumentId}", documentId);
            return (doc, null, null);
        }

        var pageHandle = PdfiumInterop.LoadPage(docHandle, pageNumber);
        if (pageHandle.IsInvalid)
        {
            _logger.LogWarning("Failed to load page {Page} for document {DocumentId}", pageNumber, documentId);
            pageHandle.Dispose();
            return (doc, docHandle, null);
        }

        return (doc, docHandle, pageHandle);
    }

    /// <summary>
    /// Records that a page has been modified so content stream patching picks it up at save time.
    /// For move/resize, records matrix patches so QPDF can update the content stream.
    /// </summary>
    private void MarkPageDirty(string documentId, int pageNumber)
    {
        // Currently a no-op marker — matrix patches are recorded individually in Move/Resize.
        // This exists to signal that the page needs content stream patching at save time.
        _logger.LogDebug("Page {Page} marked dirty for document {DocId}", pageNumber, documentId);
    }

    /// <summary>
    /// Records a matrix patch for a moved/resized object so QPDF can update the content stream at save time.
    /// </summary>
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
