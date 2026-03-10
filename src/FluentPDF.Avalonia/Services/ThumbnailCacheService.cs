using System.Security.Cryptography;
using System.Text;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Caches PDF first-page thumbnails as PNG files on disk.
/// Thumbnails are stored in %LOCALAPPDATA%/FluentPDF/thumbnails/.
/// </summary>
public sealed class ThumbnailCacheService : IThumbnailCacheService
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfRenderingService _renderingService;
    private readonly ILogger<ThumbnailCacheService> _logger;
    private readonly string _cacheDirectory;

    private const int ThumbnailDpi = 36; // Low DPI for small cards
    private const double ThumbnailZoom = 0.25;

    public ThumbnailCacheService(
        IPdfDocumentService documentService,
        IPdfRenderingService renderingService,
        ILogger<ThumbnailCacheService> logger)
    {
        _documentService = documentService;
        _renderingService = renderingService;
        _logger = logger;
        _cacheDirectory = GetCacheDirectory();
        Directory.CreateDirectory(_cacheDirectory);
    }

    public string? GetCachedThumbnailPath(string filePath)
    {
        var pngPath = GetThumbnailPath(filePath);
        if (!File.Exists(pngPath)) return null;

        // Invalidate if PDF is newer than cached thumbnail
        var pdfLastWrite = File.GetLastWriteTimeUtc(filePath);
        var thumbLastWrite = File.GetLastWriteTimeUtc(pngPath);
        if (pdfLastWrite > thumbLastWrite)
        {
            try { File.Delete(pngPath); } catch { }
            return null;
        }

        return pngPath;
    }

    public async Task<string?> GenerateThumbnailAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var pngPath = GetThumbnailPath(filePath);

        // Return cached if valid
        var existing = GetCachedThumbnailPath(filePath);
        if (existing != null) return existing;

        try
        {
            var loadResult = await _documentService.LoadDocumentAsync(filePath);
            if (loadResult.IsFailed)
            {
                _logger.LogWarning("Failed to load {Path} for thumbnail: {Error}",
                    filePath, loadResult.Errors.FirstOrDefault()?.Message);
                return null;
            }

            var document = loadResult.Value;
            try
            {
                var renderResult = await _renderingService.RenderPageAsync(
                    document, 1, ThumbnailZoom, ThumbnailDpi);

                if (renderResult.IsFailed)
                {
                    _logger.LogWarning("Failed to render thumbnail for {Path}", filePath);
                    return null;
                }

                using var pngStream = renderResult.Value;
                await using var fileStream = File.Create(pngPath);
                await pngStream.CopyToAsync(fileStream);

                _logger.LogDebug("Generated thumbnail for {Path}", filePath);
                return pngPath;
            }
            finally
            {
                document.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thumbnail generation failed for {Path}", filePath);
            return null;
        }
    }

    public void PruneStaleThumbnails(IEnumerable<string> activeFilePaths)
    {
        var activeHashes = new HashSet<string>(
            activeFilePaths.Select(p => ComputeHash(p)));

        try
        {
            foreach (var file in Directory.EnumerateFiles(_cacheDirectory, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!activeHashes.Contains(name))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prune stale thumbnails");
        }
    }

    private string GetThumbnailPath(string filePath)
    {
        return Path.Combine(_cacheDirectory, ComputeHash(filePath) + ".png");
    }

    private static string ComputeHash(string filePath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(filePath.ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static string GetCacheDirectory()
    {
        string baseDirectory;
        if (OperatingSystem.IsWindows())
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        else if (OperatingSystem.IsMacOS())
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDirectory = Path.Combine(home, ".local", "share");
        }

        return Path.Combine(baseDirectory, "FluentPDF", "thumbnails");
    }
}
