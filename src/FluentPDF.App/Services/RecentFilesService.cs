using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace FluentPDF.App.Services;

/// <summary>
/// Manages recently opened files with persistent storage.
/// </summary>
/// <remarks>
/// Stores recent files in ApplicationData.LocalSettings as JSON.
/// Maintains MRU (most recently used) ordering with a maximum of 10 items.
/// Validates file paths on load, removing non-existent files automatically.
/// </remarks>
public sealed class RecentFilesService : IRecentFilesService
{
    private const string StorageKey = "RecentFiles";
    private const int MaxRecentFiles = 10;

    private readonly ILogger<RecentFilesService> _logger;
    private readonly ApplicationDataContainer? _settings;
    private List<RecentFileEntry> _recentFiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentFilesService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking recent files operations.</param>
    public RecentFilesService(ILogger<RecentFilesService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recentFiles = new List<RecentFileEntry>();

        try
        {
            _settings = ApplicationData.Current.LocalSettings;
            LoadRecentFiles();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to access ApplicationData.LocalSettings. Recent files will not be persisted.");
            // _settings will remain null, and we'll operate without persistence
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<RecentFileEntry> GetRecentFiles()
    {
        _logger.LogDebug("Getting {Count} recent files", _recentFiles.Count);
        return _recentFiles.AsReadOnly();
    }

    /// <inheritdoc/>
    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        _logger.LogInformation("Adding recent file: {FilePath}", filePath);

        // Remove existing entry if present (will be re-added at top)
        _recentFiles.RemoveAll(entry =>
            string.Equals(entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        // Add to top of list with current timestamp
        var newEntry = new RecentFileEntry
        {
            FilePath = filePath,
            LastAccessed = DateTime.UtcNow
        };
        _recentFiles.Insert(0, newEntry);

        // Enforce max items limit
        if (_recentFiles.Count > MaxRecentFiles)
        {
            var removed = _recentFiles.Count - MaxRecentFiles;
            _recentFiles.RemoveRange(MaxRecentFiles, removed);
            _logger.LogDebug("Removed {Count} oldest entries to maintain limit", removed);
        }

        SaveRecentFiles();
        _logger.LogDebug("Recent file added successfully. Total count: {Count}", _recentFiles.Count);
    }

    /// <inheritdoc/>
    public void RemoveRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        _logger.LogInformation("Removing recent file: {FilePath}", filePath);

        var removed = _recentFiles.RemoveAll(entry =>
            string.Equals(entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (removed > 0)
        {
            SaveRecentFiles();
            _logger.LogDebug("Removed {Count} entries for path: {FilePath}", removed, filePath);
        }
        else
        {
            _logger.LogDebug("No entries found to remove for path: {FilePath}", filePath);
        }
    }

    /// <inheritdoc/>
    public void ClearRecentFiles()
    {
        _logger.LogInformation("Clearing all recent files");

        var count = _recentFiles.Count;
        _recentFiles.Clear();
        SaveRecentFiles();

        _logger.LogDebug("Cleared {Count} recent files", count);
    }

    private void LoadRecentFiles()
    {
        if (_settings is null) return;

        try
        {
            if (_settings.Values.TryGetValue(StorageKey, out var storedValue) &&
                storedValue is string json)
            {
                _logger.LogDebug("Loading recent files from storage");

                var entries = JsonSerializer.Deserialize<List<StoredEntry>>(json);
                if (entries != null)
                {
                    // Validate file existence and convert to RecentFileEntry
                    _recentFiles = entries
                        .Where(entry => ValidateFilePath(entry.FilePath))
                        .Select(entry => new RecentFileEntry
                        {
                            FilePath = entry.FilePath,
                            LastAccessed = entry.LastAccessed
                        })
                        .ToList();

                    var removedCount = entries.Count - _recentFiles.Count;
                    if (removedCount > 0)
                    {
                        _logger.LogInformation("Removed {Count} non-existent files from recent list", removedCount);
                        SaveRecentFiles(); // Persist the cleaned list
                    }

                    _logger.LogInformation("Loaded {Count} recent files", _recentFiles.Count);
                }
            }
            else
            {
                _logger.LogDebug("No recent files found in storage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent files from storage");
            _recentFiles = new List<RecentFileEntry>();
        }
    }

    private void SaveRecentFiles()
    {
        if (_settings is null) return;

        try
        {
            var entries = _recentFiles.Select(entry => new StoredEntry
            {
                FilePath = entry.FilePath,
                LastAccessed = entry.LastAccessed
            }).ToList();

            var json = JsonSerializer.Serialize(entries);
            _settings.Values[StorageKey] = json;

            _logger.LogDebug("Saved {Count} recent files to storage", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save recent files to storage");
        }
    }

    private bool ValidateFilePath(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var exists = File.Exists(filePath);
            if (!exists)
            {
                _logger.LogDebug("File no longer exists: {FilePath}", filePath);
            }
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error validating file path: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Internal storage format for serialization.
    /// </summary>
    private sealed class StoredEntry
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastAccessed { get; set; }
    }
}
