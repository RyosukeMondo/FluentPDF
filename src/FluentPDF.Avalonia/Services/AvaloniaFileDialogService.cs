using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Avalonia implementation of file dialog service using StorageProvider API.
/// Provides cross-platform file picker dialogs via Avalonia's storage abstraction.
/// </summary>
public class AvaloniaFileDialogService : IFileDialogService
{
    private readonly ILogger<AvaloniaFileDialogService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaFileDialogService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public AvaloniaFileDialogService(ILogger<AvaloniaFileDialogService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the main application window for dialog parent.
    /// </summary>
    /// <returns>Main window, or null if not available.</returns>
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        _logger?.LogWarning("Application lifetime is not classic desktop style");
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> OpenFileAsync(string title, IEnumerable<FileDialogFilter> filters)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available for file picker");
            return null;
        }

        try
        {
            var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(ext => $"*.{ext}").ToArray()
            }).ToList();

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var result = await window.StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                var selectedPath = result[0].Path.LocalPath;
                _logger?.LogInformation("File selected: {FilePath}", selectedPath);
                return selectedPath;
            }

            _logger?.LogInformation("File picker cancelled by user");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open file picker dialog");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> OpenFilesAsync(string title, IEnumerable<FileDialogFilter> filters)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available for files picker");
            return Array.Empty<string>();
        }

        try
        {
            var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(ext => $"*.{ext}").ToArray()
            }).ToList();

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = fileTypes
            };

            var result = await window.StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                var selectedPaths = result.Select(file => file.Path.LocalPath).ToArray();
                _logger?.LogInformation("Files selected: {Count}", selectedPaths.Length);
                return selectedPaths;
            }

            _logger?.LogInformation("Files picker cancelled by user");
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open files picker dialog");
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> SaveFileAsync(string title, string? defaultFileName, IEnumerable<FileDialogFilter> filters)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available for save dialog");
            return null;
        }

        try
        {
            var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(ext => $"*.{ext}").ToArray()
            }).ToList();

            var options = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultFileName,
                FileTypeChoices = fileTypes,
                ShowOverwritePrompt = true
            };

            var result = await window.StorageProvider.SaveFilePickerAsync(options);

            if (result != null)
            {
                var savePath = result.Path.LocalPath;
                _logger?.LogInformation("Save location selected: {FilePath}", savePath);
                return savePath;
            }

            _logger?.LogInformation("Save dialog cancelled by user");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open save file dialog");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> OpenFolderAsync(string title)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available for folder picker");
            return null;
        }

        try
        {
            var options = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            var result = await window.StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                var folderPath = result[0].Path.LocalPath;
                _logger?.LogInformation("Folder selected: {FolderPath}", folderPath);
                return folderPath;
            }

            _logger?.LogInformation("Folder picker cancelled by user");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open folder picker dialog");
            return null;
        }
    }
}
