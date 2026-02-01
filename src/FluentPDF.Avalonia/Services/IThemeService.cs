using Avalonia.Styling;
using FluentResults;
using System;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Provides centralized theme management with runtime theme switching support.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme variant.
    /// </summary>
    ThemeVariant CurrentTheme { get; }

    /// <summary>
    /// Observes theme changes as a reactive stream.
    /// Emits whenever the theme is manually changed or system theme changes.
    /// </summary>
    /// <returns>Observable stream of theme variant changes.</returns>
    IObservable<ThemeVariant> ObserveThemeChanges();

    /// <summary>
    /// Sets the application theme to a specific variant.
    /// Theme changes apply immediately without requiring application restart.
    /// </summary>
    /// <param name="theme">The theme variant to apply (Light, Dark, Default).</param>
    /// <returns>Success result, or failure with error details.</returns>
    Result SetTheme(ThemeVariant theme);

    /// <summary>
    /// Applies the system theme by using ThemeVariant.Default.
    /// The app will automatically follow system light/dark mode changes.
    /// </summary>
    /// <returns>Success result, or failure with error details.</returns>
    Result UseSystemTheme();

    /// <summary>
    /// Gets the current system accent color if available.
    /// </summary>
    /// <returns>Success with color value, or failure if not supported.</returns>
    Result<System.Drawing.Color> GetSystemAccentColor();
}
