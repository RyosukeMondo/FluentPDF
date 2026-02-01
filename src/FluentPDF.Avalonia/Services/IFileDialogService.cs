using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Cross-platform file dialog service interface.
/// Provides file picker functionality using Avalonia's storage provider.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Opens a file picker dialog for selecting a single file.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="filters">File type filters to apply.</param>
    /// <returns>Selected file path, or null if cancelled.</returns>
    Task<string?> OpenFileAsync(string title, IEnumerable<FileDialogFilter> filters);

    /// <summary>
    /// Opens a file picker dialog for selecting multiple files.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="filters">File type filters to apply.</param>
    /// <returns>Collection of selected file paths, or empty if cancelled.</returns>
    Task<IEnumerable<string>> OpenFilesAsync(string title, IEnumerable<FileDialogFilter> filters);

    /// <summary>
    /// Opens a save file dialog.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="defaultFileName">Default file name to suggest.</param>
    /// <param name="filters">File type filters to apply.</param>
    /// <returns>Selected save path, or null if cancelled.</returns>
    Task<string?> SaveFileAsync(string title, string? defaultFileName, IEnumerable<FileDialogFilter> filters);

    /// <summary>
    /// Opens a folder picker dialog.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <returns>Selected folder path, or null if cancelled.</returns>
    Task<string?> OpenFolderAsync(string title);
}

/// <summary>
/// File dialog filter for specific file extensions.
/// </summary>
public class FileDialogFilter
{
    /// <summary>
    /// Gets or sets the display name for this filter (e.g., "PDF Documents").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of file extensions (without dots, e.g., "pdf", "txt").
    /// </summary>
    public List<string> Extensions { get; set; } = new();
}
