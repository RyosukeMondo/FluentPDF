using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Caches PDFium page handles for modified pages so in-memory changes
/// (InsertPageObject, SetMatrix) survive across render cycles.
///
/// Industry-standard PDF editors (Acrobat, Foxit) keep modified pages
/// in memory to avoid re-parsing content streams. This prevents:
/// 1. Loss of in-memory modifications (new shapes, moved objects)
/// 2. CIDFont text corruption from GenerateContent re-serialization
///
/// Thread safety: All access must go through PDFium's single-thread model.
/// </summary>
public sealed class PageHandleCache : IDisposable
{
    private readonly ILogger<PageHandleCache> _logger;
    private readonly Dictionary<(nint docHandle, int pageIndex), SafePdfPageHandle> _cache = new();

    public PageHandleCache(ILogger<PageHandleCache> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a cached page handle or loads a new one.
    /// If cached, the handle is borrowed (caller must NOT dispose it).
    /// If not cached, a new handle is opened and cached.
    /// Returns (handle, isCached) — caller disposes only if !isCached.
    /// </summary>
    public (SafePdfPageHandle handle, bool isCached) GetOrLoad(SafePdfDocumentHandle doc, int pageIndex)
    {
        var key = (doc.DangerousGetHandle(), pageIndex);
        if (_cache.TryGetValue(key, out var cached) && !cached.IsInvalid)
            return (cached, true);

        var handle = PdfiumInterop.LoadPage(doc, pageIndex);
        return (handle, false);
    }

    /// <summary>
    /// Marks a page as modified, caching its handle for future renders.
    /// The cache takes ownership — caller must NOT dispose the handle.
    /// </summary>
    public void CacheModifiedPage(SafePdfDocumentHandle doc, int pageIndex, SafePdfPageHandle handle)
    {
        var key = (doc.DangerousGetHandle(), pageIndex);

        // If there's already a cached handle for this page, close the OLD one
        // (not the new one being cached)
        if (_cache.TryGetValue(key, out var existing) && existing != handle && !existing.IsInvalid)
        {
            existing.Dispose();
        }

        _cache[key] = handle;
        _logger.LogDebug("Cached modified page {PageIndex} (total cached: {Count})", pageIndex, _cache.Count);
    }

    /// <summary>
    /// Checks if a page has a cached handle (has been modified).
    /// </summary>
    public bool HasCached(SafePdfDocumentHandle doc, int pageIndex)
    {
        var key = (doc.DangerousGetHandle(), pageIndex);
        return _cache.TryGetValue(key, out var h) && !h.IsInvalid;
    }

    /// <summary>
    /// Evicts a single page from the cache, disposing its handle.
    /// Called after save (page will be re-rendered from saved file).
    /// </summary>
    public void Evict(SafePdfDocumentHandle doc, int pageIndex)
    {
        var key = (doc.DangerousGetHandle(), pageIndex);
        if (_cache.Remove(key, out var h) && !h.IsInvalid)
        {
            h.Dispose();
            _logger.LogDebug("Evicted cached page {PageIndex}", pageIndex);
        }
    }

    /// <summary>
    /// Evicts all pages for a document. Called on save or document close.
    /// </summary>
    public void EvictAll(SafePdfDocumentHandle? doc = null)
    {
        if (doc == null)
        {
            foreach (var h in _cache.Values)
                if (!h.IsInvalid) h.Dispose();
            _cache.Clear();
            _logger.LogDebug("Evicted all cached pages");
            return;
        }

        var docPtr = doc.DangerousGetHandle();
        var toRemove = _cache.Where(kv => kv.Key.docHandle == docPtr).Select(kv => kv.Key).ToList();
        foreach (var key in toRemove)
        {
            if (_cache.Remove(key, out var h) && !h.IsInvalid)
                h.Dispose();
        }
        _logger.LogDebug("Evicted {Count} cached pages for document", toRemove.Count);
    }

    /// <summary>
    /// Calls FPDFPage_GenerateContent on all cached pages for the given document.
    /// Required before PDFium SaveDocument to serialize in-memory objects.
    /// Returns the number of pages flushed.
    /// </summary>
    public int FlushGenerateContent(SafePdfDocumentHandle doc)
    {
        var docPtr = doc.DangerousGetHandle();
        int count = 0;
        foreach (var (key, handle) in _cache)
        {
            if (key.docHandle == docPtr && !handle.IsInvalid)
            {
                PdfiumInterop.GenerateContent(handle);
                count++;
            }
        }
        return count;
    }

    public void Dispose() => EvictAll();
}
