using FluentPDF.Core.Models;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service for managing recently opened files.
/// </summary>
public interface IRecentFilesService
{
    /// <summary>
    /// Gets the list of recent files in most-recently-used order.
    /// </summary>
    /// <returns>Collection of recent file entries, limited to maximum allowed.</returns>
    IReadOnlyList<RecentFileEntry> GetRecentFiles();

    /// <summary>
    /// Adds a file to the recent files list or updates its timestamp if already present.
    /// </summary>
    /// <param name="filePath">Full path to the file.</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null, empty, or whitespace.</exception>
    void AddRecentFile(string filePath);

    /// <summary>
    /// Removes a file from the recent files list.
    /// </summary>
    /// <param name="filePath">Full path to the file to remove.</param>
    void RemoveRecentFile(string filePath);

    /// <summary>
    /// Clears all recent files.
    /// </summary>
    void ClearRecentFiles();
}
