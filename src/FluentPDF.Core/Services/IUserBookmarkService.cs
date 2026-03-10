using FluentPDF.Core.Models;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service for managing user-created bookmarks on PDF pages.
/// Bookmarks are persisted per PDF file (keyed by file path hash)
/// in local storage as JSON.
/// </summary>
public interface IUserBookmarkService
{
    /// <summary>
    /// Gets all user bookmarks for a given PDF file, ordered by page number.
    /// </summary>
    /// <param name="filePath">Full path to the PDF file.</param>
    /// <returns>List of user bookmarks for the file.</returns>
    IReadOnlyList<UserBookmark> GetBookmarks(string filePath);

    /// <summary>
    /// Adds a bookmark for a specific page of a PDF file.
    /// If a bookmark already exists for that page, it is replaced.
    /// </summary>
    /// <param name="filePath">Full path to the PDF file.</param>
    /// <param name="pageNumber">1-based page number to bookmark.</param>
    /// <param name="label">Optional label for the bookmark.</param>
    void AddBookmark(string filePath, int pageNumber, string? label = null);

    /// <summary>
    /// Removes the bookmark for a specific page of a PDF file.
    /// No-op if no bookmark exists for that page.
    /// </summary>
    /// <param name="filePath">Full path to the PDF file.</param>
    /// <param name="pageNumber">1-based page number to remove bookmark from.</param>
    void RemoveBookmark(string filePath, int pageNumber);

    /// <summary>
    /// Checks whether a bookmark exists for a specific page of a PDF file.
    /// </summary>
    /// <param name="filePath">Full path to the PDF file.</param>
    /// <param name="pageNumber">1-based page number to check.</param>
    /// <returns>True if a bookmark exists for the page.</returns>
    bool HasBookmark(string filePath, int pageNumber);
}
