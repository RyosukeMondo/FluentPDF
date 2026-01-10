namespace FluentPDF.Core.Models;

/// <summary>
/// Represents an entry in the recent files list.
/// </summary>
public sealed class RecentFileEntry
{
    /// <summary>
    /// Gets the full path to the file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the timestamp when the file was last accessed.
    /// </summary>
    public required DateTime LastAccessed { get; init; }

    /// <summary>
    /// Gets the display name of the file (filename without path).
    /// </summary>
    public string DisplayName => Path.GetFileName(FilePath);
}
