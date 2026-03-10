namespace FluentPDF.Core.Services;

/// <summary>
/// Caches thumbnail images for recently opened PDF files.
/// </summary>
public interface IThumbnailCacheService
{
    /// <summary>
    /// Gets the cached thumbnail PNG path for a file, or null if not cached.
    /// </summary>
    string? GetCachedThumbnailPath(string filePath);

    /// <summary>
    /// Generates and caches a thumbnail for the given PDF file.
    /// Returns the path to the cached PNG, or null on failure.
    /// </summary>
    Task<string?> GenerateThumbnailAsync(string filePath);

    /// <summary>
    /// Removes stale cache entries for files no longer in the recent list.
    /// </summary>
    void PruneStaleThumbnails(IEnumerable<string> activeFilePaths);
}
