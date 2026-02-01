using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Manages recently opened files with persistent cross-platform storage.
/// </summary>
/// <remarks>
/// Stores recent files in platform-specific local storage as JSON.
/// - Windows: %LOCALAPPDATA%\FluentPDF\recent-files.json
/// - macOS: ~/Library/Application Support/FluentPDF/recent-files.json
/// - Linux: ~/.local/share/FluentPDF/recent-files.json
///
/// Maintains MRU (most recently used) ordering with a maximum of 10 items.
/// Validates file paths on load, removing non-existent files automatically.
/// </remarks>
public sealed class RecentFilesService : IRecentFilesService
{
    private const int MaxRecentFiles = 10;
    private const string RecentFilesFileName = "recent-files.json";

    private readonly ILogger<RecentFilesService> _logger;
    private readonly List<RecentFileEntry> _recentFiles = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _recentFilesPath;
    private bool _isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentFilesService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking operations.</param>
    public RecentFilesService(ILogger<RecentFilesService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var settingsDirectory = GetSettingsDirectory();
        _recentFilesPath = Path.Combine(settingsDirectory, RecentFilesFileName);

        _logger.LogInformation("Recent files path: {Path}", _recentFilesPath);
    }

    /// <inheritdoc/>
    public IReadOnlyList<RecentFileEntry> GetRecentFiles()
    {
        // Lazy load if not already loaded
        if (!_isLoaded)
        {
            // Use Task.Run to avoid deadlock on UI thread
            Task.Run(async () => await LoadAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        _logger.LogDebug("Getting {Count} recent files", _recentFiles.Count);
        return _recentFiles.AsReadOnly();
    }

    /// <inheritdoc/>
    public void AddRecentFile(string filePath)
    {
        // Use Task.Run to avoid deadlock on UI thread
        Task.Run(async () => await AddAsync(filePath).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void RemoveRecentFile(string filePath)
    {
        // Use Task.Run to avoid deadlock on UI thread
        Task.Run(async () => await RemoveAsync(filePath).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void ClearRecentFiles()
    {
        // Use Task.Run to avoid deadlock on UI thread
        Task.Run(async () => await ClearAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Loads recent files from persistent storage (internal async implementation).
    /// </summary>
    private async Task LoadAsync()
    {
        try
        {
            _logger.LogDebug("Loading recent files from {Path}", _recentFilesPath);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_recentFilesPath)!);

            if (!File.Exists(_recentFilesPath))
            {
                _logger.LogInformation("Recent files file not found, starting with empty list");
                _isLoaded = true;
                return;
            }

            var json = await File.ReadAllTextAsync(_recentFilesPath).ConfigureAwait(false);
            var loadedFiles = JsonSerializer.Deserialize<List<RecentFileEntry>>(json, _jsonOptions);

            if (loadedFiles == null)
            {
                _logger.LogWarning("Failed to deserialize recent files, starting with empty list");
                _isLoaded = true;
                return;
            }

            // Validate and filter out non-existent files
            _recentFiles.Clear();
            foreach (var file in loadedFiles)
            {
                if (File.Exists(file.FilePath))
                {
                    _recentFiles.Add(file);
                }
                else
                {
                    _logger.LogDebug("Removing non-existent file from recent files: {Path}", file.FilePath);
                }
            }

            _logger.LogInformation("Loaded {Count} recent files", _recentFiles.Count);
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent files, starting with empty list");
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Adds a file to recent files (internal async implementation).
    /// </summary>
    private async Task AddAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        // Ensure loaded
        if (!_isLoaded)
        {
            await LoadAsync().ConfigureAwait(false);
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Cannot add non-existent file to recent files: {Path}", filePath);
            return;
        }

        try
        {
            _logger.LogDebug("Adding file to recent files: {Path}", filePath);

            // Remove existing entry if present (to update timestamp)
            var existing = _recentFiles.FirstOrDefault(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _recentFiles.Remove(existing);
            }

            // Add to front of list (MRU)
            var recentFile = new RecentFileEntry
            {
                FilePath = filePath,
                LastAccessed = DateTime.UtcNow
            };
            _recentFiles.Insert(0, recentFile);

            // Trim to max size
            while (_recentFiles.Count > MaxRecentFiles)
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }

            await SaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add file to recent files: {Path}", filePath);
        }
    }

    /// <summary>
    /// Removes a file from recent files (internal async implementation).
    /// </summary>
    private async Task RemoveAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        // Ensure loaded
        if (!_isLoaded)
        {
            await LoadAsync().ConfigureAwait(false);
        }

        try
        {
            _logger.LogDebug("Removing file from recent files: {Path}", filePath);

            var existing = _recentFiles.FirstOrDefault(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                _recentFiles.Remove(existing);
                await SaveAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove file from recent files: {Path}", filePath);
        }
    }

    /// <summary>
    /// Clears all recent files (internal async implementation).
    /// </summary>
    private async Task ClearAsync()
    {
        try
        {
            _logger.LogInformation("Clearing all recent files");

            _recentFiles.Clear();
            await SaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear recent files");
        }
    }

    /// <summary>
    /// Saves recent files to persistent storage.
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            _logger.LogDebug("Saving recent files to {Path}", _recentFilesPath);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_recentFilesPath)!);

            var json = JsonSerializer.Serialize(_recentFiles, _jsonOptions);
            await File.WriteAllTextAsync(_recentFilesPath, json).ConfigureAwait(false);

            _logger.LogDebug("Recent files saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save recent files");
        }
    }

    /// <summary>
    /// Gets the platform-specific settings directory.
    /// </summary>
    private static string GetSettingsDirectory()
    {
        string baseDirectory;

        if (OperatingSystem.IsWindows())
        {
            // Windows: %LOCALAPPDATA%\FluentPDF
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // macOS: ~/Library/Application Support/FluentPDF
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            // Linux/Unix: ~/.local/share/FluentPDF
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDirectory = Path.Combine(homeDirectory, ".local", "share");
        }

        return Path.Combine(baseDirectory, "FluentPDF");
    }
}
