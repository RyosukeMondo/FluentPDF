using FluentPDF.Core.Models;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service for managing application settings and user preferences.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// Loads settings from persistent storage.
    /// If settings file doesn't exist or is corrupted, returns default settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    Task LoadAsync();

    /// <summary>
    /// Saves the current settings to persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveAsync();

    /// <summary>
    /// Resets all settings to their default values and saves.
    /// </summary>
    /// <returns>A task that represents the asynchronous reset operation.</returns>
    Task ResetToDefaultsAsync();
}
