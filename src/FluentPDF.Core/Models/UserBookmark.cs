namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a user-created bookmark on a specific page of a PDF document.
/// Unlike PDF TOC bookmarks (BookmarkNode), these are personal bookmarks
/// that persist across sessions via local storage.
/// </summary>
public sealed class UserBookmark
{
    /// <summary>
    /// Gets the 1-based page number this bookmark refers to.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the optional user-provided label for this bookmark.
    /// When null or empty, the UI displays "Page {PageNumber}".
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this bookmark was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the display label: the user label if set, otherwise "Page {PageNumber}".
    /// Not serialized — computed at runtime.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel => string.IsNullOrWhiteSpace(Label)
        ? $"Page {PageNumber}"
        : Label;
}
