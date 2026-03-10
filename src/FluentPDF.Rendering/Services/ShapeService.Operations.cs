using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

public sealed partial class ShapeService
{
    public Task<List<PageObjectInfo>> GetPageObjectsAsync(string documentId, PageIndex pageIndex)
    {
        var result = new List<PageObjectInfo>();
        var (docHandle, pageHandle, isCached) = ResolvePageForRead(documentId, pageIndex);
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

    public Task<bool> RemovePageObjectAsync(string documentId, PageIndex pageIndex, int objectIndex)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero)
        {
            _logger.LogWarning("Object {Index} not found on page {Page}", objectIndex, pageIndex);
            return Task.FromResult(false);
        }

        var removed = PdfiumInterop.RemovePageObject(pageHandle, obj);
        if (removed)
        {
            PdfiumInterop.GenerateContent(pageHandle);
            ReindexAfterRemoval(pageIndex, objectIndex);
            _logger.LogInformation("Removed object {Index} from page {Page}", objectIndex, pageIndex);
        }
        return Task.FromResult(removed);
    }

    public Task<bool> MovePageObjectAsync(string documentId, PageIndex pageIndex, int objectIndex, float deltaX, float deltaY)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
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
                RecordMatrixChange(pageIndex.Value, a, b, c, d, e, f, a, b, c, d, e + deltaX, f + deltaY);
            }
            else
            {
                PdfiumInterop.TransformPageObject(obj, 1, 0, 0, 1, deltaX, deltaY);
            }

            _logger.LogInformation("Moved object {Index} on page {Page} by ({DX}, {DY})", objectIndex, pageIndex, deltaX, deltaY);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move object {Index} on page {Page}", objectIndex, pageIndex);
            return Task.FromResult(false);
        }
    }

    public Task<int> MovePageObjectsBatchAsync(string documentId, PageIndex pageIndex, int[] objectIndices, float deltaX, float deltaY)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
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
                    RecordMatrixChange(pageIndex.Value, a, b, c, d, e, f, a, b, c, d, e + deltaX, f + deltaY);
                }
                else
                {
                    PdfiumInterop.TransformPageObject(obj, 1, 0, 0, 1, deltaX, deltaY);
                }
                moved++;
            }

            _logger.LogInformation("Batch moved {Count} objects on page {Page}", moved, pageIndex);
            return Task.FromResult(moved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch move objects on page {Page}", pageIndex);
            return Task.FromResult(0);
        }
    }

    public Task<bool> ResizePageObjectAsync(string documentId, PageIndex pageIndex, int objectIndex,
        float scaleX, float scaleY, float anchorPdfX, float anchorPdfY)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
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
                RecordMatrixChange(pageIndex.Value, a, b, c, d, e, f, newA, newB, newC, newD, newE, newF);
            }
            else
            {
                PdfiumInterop.TransformPageObject(obj, scaleX, 0, 0, scaleY,
                    anchorPdfX * (1 - scaleX), anchorPdfY * (1 - scaleY));
            }

            _logger.LogInformation("Resized object {Index} on page {Page}", objectIndex, pageIndex);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize object {Index}", objectIndex);
            return Task.FromResult(false);
        }
    }

    public Task<PageObjectProperties?> GetPageObjectPropertiesAsync(string documentId, PageIndex pageIndex, int objectIndex)
    {
        var (docHandle, pageHandle, isCached) = ResolvePageForRead(documentId, pageIndex);
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

    public Task<bool> SetPageObjectStrokeColorAsync(string documentId, PageIndex pageIndex, int objectIndex, string color)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero) return Task.FromResult(false);

        var (r, g, b, a) = ParseColor(color);
        PdfiumInterop.SetPageObjectStrokeColor(obj, r, g, b, a);
        return Task.FromResult(true);
    }

    public Task<bool> SetPageObjectFillColorAsync(string documentId, PageIndex pageIndex, int objectIndex, string color)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero) return Task.FromResult(false);

        var (r, g, b, a) = ParseColor(color);
        PdfiumInterop.SetPageObjectFillColor(obj, r, g, b, a);
        return Task.FromResult(true);
    }

    public Task<bool> SetPageObjectStrokeWidthAsync(string documentId, PageIndex pageIndex, int objectIndex, float width)
    {
        var (docHandle, pageHandle) = ResolvePageForMutation(documentId, pageIndex);
        if (pageHandle == null) return Task.FromResult(false);

        var obj = PdfiumInterop.GetPageObject(pageHandle, objectIndex);
        if (obj == IntPtr.Zero) return Task.FromResult(false);

        PdfiumInterop.SetStrokeWidth(obj, width);
        return Task.FromResult(true);
    }

    #region Shape Tracking

    public List<ShapeMetadata> GetTrackedShapes(PageIndex? pageIndex = null, string? source = null)
    {
        return _tracked.Values
            .Where(t => (!pageIndex.HasValue || t.Meta.PageIndex == pageIndex.Value)
                     && (source == null || t.Meta.Source == source))
            .Select(t => t.Meta)
            .OrderBy(m => m.CreatedAt)
            .ToList();
    }

    public Task<bool> RemoveTrackedShapeAsync(string documentId, string shapeId)
    {
        if (!_tracked.TryGetValue(shapeId, out var entry))
        {
            _logger.LogWarning("Tracked shape {Id} not found", shapeId);
            return Task.FromResult(false);
        }

        var result = RemovePageObjectAsync(documentId, entry.Meta.PageIndex, entry.PageObjectIndex);
        return result;
    }

    private string TrackShape(string shapeType, PageIndex pageIndex,
        float left, float bottom, float right, float top,
        string fillColor, string strokeColor, float strokeWidth,
        string source, int pageObjectIndex)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var meta = new ShapeMetadata(id, shapeType, pageIndex, left, bottom, right, top,
            fillColor, strokeColor, strokeWidth, source, DateTime.UtcNow);
        _tracked[id] = (meta, pageObjectIndex);
        return id;
    }

    private void ReindexAfterRemoval(PageIndex pageIndex, int removedIndex)
    {
        var toRemove = _tracked.Where(kv => kv.Value.Meta.PageIndex == pageIndex && kv.Value.PageObjectIndex == removedIndex)
            .Select(kv => kv.Key).FirstOrDefault();
        if (toRemove != null) _tracked.Remove(toRemove);

        foreach (var key in _tracked.Keys.ToList())
        {
            var (meta, idx) = _tracked[key];
            if (meta.PageIndex == pageIndex && idx > removedIndex)
                _tracked[key] = (meta, idx - 1);
        }
    }

    #endregion

    #region Page Resolution

    /// <summary>
    /// Resolves a page for mutation. PageIndex is 0-based — used directly for PDFium.
    /// The handle is cached so in-memory changes persist for rendering.
    /// Caller must NOT dispose the returned handle.
    /// </summary>
    private (SafePdfDocumentHandle? docHandle, SafePdfPageHandle? pageHandle) ResolvePageForMutation(
        string documentId, PageIndex pageIndex)
    {
        var doc = _documentResolver(documentId);
        if (doc == null) return (null, null);

        var docHandle = (SafePdfDocumentHandle)doc.Handle;
        if (docHandle.IsInvalid) return (null, null);

        var (pageHandle, isCached) = _pageCache.GetOrLoad(docHandle, pageIndex.Value);
        if (pageHandle.IsInvalid)
        {
            if (!isCached) pageHandle.Dispose();
            return (docHandle, null);
        }

        if (!isCached)
            _pageCache.CacheModifiedPage(docHandle, pageIndex.Value, pageHandle);

        return (docHandle, pageHandle);
    }

    /// <summary>
    /// Resolves a page for read-only access. PageIndex is 0-based — used directly for PDFium.
    /// If cached (modified), borrows the handle. If not, opens a new handle that caller must dispose.
    /// </summary>
    private (SafePdfDocumentHandle? docHandle, SafePdfPageHandle? pageHandle, bool isCached) ResolvePageForRead(
        string documentId, PageIndex pageIndex)
    {
        var doc = _documentResolver(documentId);
        if (doc == null) return (null, null, false);

        var docHandle = (SafePdfDocumentHandle)doc.Handle;
        if (docHandle.IsInvalid) return (null, null, false);

        var (pageHandle, isCached) = _pageCache.GetOrLoad(docHandle, pageIndex.Value);
        if (pageHandle.IsInvalid)
        {
            if (!isCached) pageHandle.Dispose();
            return (docHandle, null, false);
        }

        return (docHandle, pageHandle, isCached);
    }

    #endregion

    private void RecordMatrixChange(int pageIndex,
        float origA, float origB, float origC, float origD, float origE, float origF,
        float newA, float newB, float newC, float newD, float newE, float newF)
    {
        _patcher.RecordMatrixPatch(pageIndex, new MatrixPatch(
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
