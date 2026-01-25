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
    /// Event raised when settings are changed and persisted.
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// Event raised immediately when the theme changes, before persistence.
    /// This allows immediate UI updates without waiting for debounced save.
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;

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

    /// <summary>
    /// Notifies that the theme has changed and should be applied immediately.
    /// Call this when the user changes the theme setting.
    /// </summary>
    /// <param name="theme">The new theme to apply.</param>
    void NotifyThemeChanged(AppTheme theme);
}
