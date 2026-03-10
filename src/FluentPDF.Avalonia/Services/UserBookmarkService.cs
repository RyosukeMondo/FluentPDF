using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Persists user bookmarks per PDF file as JSON in local storage.
/// Each PDF file gets its own JSON file keyed by a SHA256 hash of the file path.
/// Storage location: %LOCALAPPDATA%/FluentPDF/bookmarks/
/// </summary>
public sealed class UserBookmarkService : IUserBookmarkService
{
    private readonly ILogger<UserBookmarkService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _bookmarksDirectory;

    /// <summary>
    /// In-memory cache: file path hash -> list of bookmarks.
    /// Avoids repeated disk reads for the same file.
    /// </summary>
    private readonly Dictionary<string, List<UserBookmark>> _cache = new();

    public UserBookmarkService(ILogger<UserBookmarkService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _bookmarksDirectory = Path.Combine(GetSettingsDirectory(), "bookmarks");
        Directory.CreateDirectory(_bookmarksDirectory);

        _logger.LogInformation(
            "UserBookmarkService initialized. Storage: {Path}", _bookmarksDirectory);
    }

    public IReadOnlyList<UserBookmark> GetBookmarks(string filePath)
    {
        ValidateFilePath(filePath);
        var bookmarks = LoadBookmarks(filePath);
        return bookmarks.OrderBy(b => b.PageNumber).ToList().AsReadOnly();
    }

    public void AddBookmark(string filePath, int pageNumber, string? label = null)
    {
        ValidateFilePath(filePath);
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");

        var bookmarks = LoadBookmarks(filePath);

        // Remove existing bookmark for this page (replace semantics)
        bookmarks.RemoveAll(b => b.PageNumber == pageNumber);

        bookmarks.Add(new UserBookmark
        {
            PageNumber = pageNumber,
            Label = label,
            CreatedAt = DateTime.UtcNow
        });

        SaveBookmarks(filePath, bookmarks);
        _logger.LogInformation(
            "Added bookmark: page {Page} in {File}", pageNumber, Path.GetFileName(filePath));
    }

    public void RemoveBookmark(string filePath, int pageNumber)
    {
        ValidateFilePath(filePath);

        var bookmarks = LoadBookmarks(filePath);
        var removed = bookmarks.RemoveAll(b => b.PageNumber == pageNumber);

        if (removed > 0)
        {
            SaveBookmarks(filePath, bookmarks);
            _logger.LogInformation(
                "Removed bookmark: page {Page} in {File}", pageNumber, Path.GetFileName(filePath));
        }
    }

    public bool HasBookmark(string filePath, int pageNumber)
    {
        ValidateFilePath(filePath);
        var bookmarks = LoadBookmarks(filePath);
        return bookmarks.Any(b => b.PageNumber == pageNumber);
    }

    private List<UserBookmark> LoadBookmarks(string filePath)
    {
        var hash = GetFilePathHash(filePath);

        if (_cache.TryGetValue(hash, out var cached))
            return cached;

        var jsonPath = GetBookmarkFilePath(hash);
        if (!File.Exists(jsonPath))
        {
            var empty = new List<UserBookmark>();
            _cache[hash] = empty;
            return empty;
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            var loaded = JsonSerializer.Deserialize<List<UserBookmark>>(json, _jsonOptions)
                         ?? new List<UserBookmark>();
            _cache[hash] = loaded;
            _logger.LogDebug("Loaded {Count} bookmarks for {File}", loaded.Count, Path.GetFileName(filePath));
            return loaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load bookmarks for {File}, starting fresh", filePath);
            var empty = new List<UserBookmark>();
            _cache[hash] = empty;
            return empty;
        }
    }

    private void SaveBookmarks(string filePath, List<UserBookmark> bookmarks)
    {
        var hash = GetFilePathHash(filePath);
        _cache[hash] = bookmarks;

        var jsonPath = GetBookmarkFilePath(hash);
        try
        {
            Directory.CreateDirectory(_bookmarksDirectory);
            var json = JsonSerializer.Serialize(bookmarks, _jsonOptions);
            File.WriteAllText(jsonPath, json);
            _logger.LogDebug("Saved {Count} bookmarks for {File}", bookmarks.Count, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save bookmarks for {File}", filePath);
        }
    }

    private string GetBookmarkFilePath(string hash)
    {
        return Path.Combine(_bookmarksDirectory, $"{hash}.json");
    }

    /// <summary>
    /// Produces a stable, filesystem-safe hash from a file path.
    /// Uses SHA256 truncated to 16 hex chars for brevity.
    /// </summary>
    private static string GetFilePathHash(string filePath)
    {
        var normalized = filePath.Replace('\\', '/').ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
    }

    private static string GetSettingsDirectory()
    {
        string baseDirectory;

        if (OperatingSystem.IsWindows())
        {
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDirectory = Path.Combine(homeDirectory, ".local", "share");
        }

        return Path.Combine(baseDirectory, "FluentPDF");
    }
}
