using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Abstraction for showing dialogs and file pickers.
/// Allows ViewModels to show dialogs without direct framework dependencies.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an open file dialog for selecting PDF files.
    /// </summary>
    /// <param name="title">Optional title for the dialog.</param>
    /// <param name="filterExtensions">File extensions to filter (e.g., ".pdf", ".xps"). Default is PDF only.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(
        string? title = null,
        IEnumerable<string>? filterExtensions = null);

    /// <summary>
    /// Shows an open file dialog for selecting multiple files.
    /// </summary>
    /// <param name="title">Optional title for the dialog.</param>
    /// <param name="filterExtensions">File extensions to filter.</param>
    /// <returns>The selected file paths, or empty if cancelled.</returns>
    Task<IReadOnlyList<string>> ShowOpenMultipleFilesDialogAsync(
        string? title = null,
        IEnumerable<string>? filterExtensions = null);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <param name="suggestedFileName">Suggested file name.</param>
    /// <param name="title">Optional title for the dialog.</param>
    /// <param name="filterExtensions">File extensions to filter.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(
        string? suggestedFileName = null,
        string? title = null,
        IEnumerable<string>? filterExtensions = null);

    /// <summary>
    /// Shows a folder picker dialog.
    /// </summary>
    /// <param name="title">Optional title for the dialog.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    Task<string?> ShowFolderPickerAsync(string? title = null);

    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No options.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <returns>True if user confirmed, false otherwise.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with custom button labels.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="confirmText">Text for the confirm button.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <returns>True if user clicked confirm, false if cancelled.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText);

    /// <summary>
    /// Shows a text input dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="defaultValue">Default value for the input field.</param>
    /// <returns>The entered text, or null if cancelled.</returns>
    Task<string?> ShowInputDialogAsync(string title, string message, string? defaultValue = null);

    /// <summary>
    /// Shows a password input dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <returns>The entered password, or null if cancelled.</returns>
    Task<string?> ShowPasswordDialogAsync(string title, string message);
}
