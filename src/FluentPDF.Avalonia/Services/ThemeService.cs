using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using FluentResults;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Implements centralized theme management with hot-reload support.
/// Detects system theme changes and provides reactive theme updates.
/// Also supports high contrast mode with runtime resource overrides.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private readonly ILogger<ThemeService> _logger;
    private readonly BehaviorSubject<ThemeVariant> _themeSubject;
    private readonly IDisposable? _systemThemeSubscription;
    private global::Avalonia.Controls.ResourceDictionary? _highContrastResources;
    private bool _highContrastActive;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking theme operations.</param>
    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Get initial theme from Application
        var initialTheme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Default;
        _themeSubject = new BehaviorSubject<ThemeVariant>(initialTheme);

        // Subscribe to system theme changes via Avalonia's ActualThemeVariantChanged
        if (Application.Current != null)
        {
            _systemThemeSubscription = Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    h => Application.Current.ActualThemeVariantChanged += h,
                    h => Application.Current.ActualThemeVariantChanged -= h)
                .Select(_ => Application.Current.ActualThemeVariant)
                .Subscribe(OnSystemThemeChanged);
        }

        // Detect high contrast on startup
        if (DetectSystemHighContrast())
        {
            ApplyHighContrast();
        }

        var correlationId = Guid.NewGuid();
        Log.Information("ThemeService initialized with theme: {Theme}, HighContrast: {HC} [CorrelationId: {CorrelationId}]",
            initialTheme, _highContrastActive, correlationId);
    }

    /// <inheritdoc/>
    public ThemeVariant CurrentTheme => _themeSubject.Value;

    /// <inheritdoc/>
    public IObservable<ThemeVariant> ObserveThemeChanges()
    {
        return _themeSubject.AsObservable();
    }

    /// <inheritdoc/>
    public Result SetTheme(ThemeVariant theme)
    {
        if (theme == null)
        {
            return Result.Fail("Theme variant cannot be null");
        }

        try
        {
            var correlationId = Guid.NewGuid();
            Log.Information("Setting theme to: {Theme} [CorrelationId: {CorrelationId}]",
                theme, correlationId);

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = theme;
                _themeSubject.OnNext(theme);

                Log.Information("Theme applied successfully: {Theme} [CorrelationId: {CorrelationId}]",
                    theme, correlationId);

                return Result.Ok();
            }

            var error = "Application.Current is null, cannot set theme";
            Log.Error("{Error} [CorrelationId: {CorrelationId}]", error, correlationId);
            return Result.Fail(error);
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex, "Failed to set theme [CorrelationId: {CorrelationId}]", correlationId);
            return Result.Fail($"Failed to set theme: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result UseSystemTheme()
    {
        return SetTheme(ThemeVariant.Default);
    }

    /// <inheritdoc/>
    public Result<System.Drawing.Color> GetSystemAccentColor()
    {
        try
        {
            // Windows-specific accent color detection
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var color = GetWindowsAccentColor();
                if (color.HasValue)
                {
                    return Result.Ok(color.Value);
                }
            }

            return Result.Fail<System.Drawing.Color>(
                "System accent color detection not supported on this platform");
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex, "Failed to get system accent color [CorrelationId: {CorrelationId}]",
                correlationId);
            return Result.Fail<System.Drawing.Color>(
                $"Failed to get system accent color: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles system theme changes detected via Avalonia's reactive stream.
    /// </summary>
    private void OnSystemThemeChanged(ThemeVariant newTheme)
    {
        var correlationId = Guid.NewGuid();
        Log.Information("System theme changed to: {Theme} [CorrelationId: {CorrelationId}]",
            newTheme, correlationId);

        _themeSubject.OnNext(newTheme);
    }

    /// <inheritdoc/>
    public bool IsHighContrastActive => _highContrastActive;

    /// <inheritdoc/>
    public Result ApplyHighContrast()
    {
        try
        {
            if (_highContrastActive)
            {
                return Result.Ok();
            }

            var app = Application.Current;
            if (app == null)
            {
                return Result.Fail("Application.Current is null");
            }

            // Load the HighContrast resource dictionary
            _highContrastResources = new global::Avalonia.Controls.ResourceDictionary();
            var source = new Uri("avares://FluentPDF.Avalonia/Styles/Theme/HighContrast.axaml");
            var loaded = (global::Avalonia.Controls.ResourceDictionary)AvaloniaXamlLoader.Load(source);

            foreach (var kvp in loaded)
            {
                _highContrastResources[kvp.Key] = kvp.Value;
            }

            // Merge into application resources (overrides existing keys)
            app.Resources.MergedDictionaries.Add(_highContrastResources);

            _highContrastActive = true;
            Log.Information("High contrast theme applied");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply high contrast theme");
            return Result.Fail($"Failed to apply high contrast: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result RemoveHighContrast()
    {
        try
        {
            if (!_highContrastActive || _highContrastResources == null)
            {
                return Result.Ok();
            }

            var app = Application.Current;
            if (app == null)
            {
                return Result.Fail("Application.Current is null");
            }

            app.Resources.MergedDictionaries.Remove(_highContrastResources);
            _highContrastResources = null;
            _highContrastActive = false;

            Log.Information("High contrast theme removed");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove high contrast theme");
            return Result.Fail($"Failed to remove high contrast: {ex.Message}");
        }
    }

    /// <summary>
    /// Detects whether the system is currently in high contrast mode.
    /// </summary>
    private static bool DetectSystemHighContrast()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return SystemParametersInfoHighContrast();
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Windows-specific high contrast detection via Registry.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static bool SystemParametersInfoHighContrast()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\HighContrast");
            if (key != null)
            {
                var flags = key.GetValue("Flags");
                if (flags is string flagStr && int.TryParse(flagStr, out var flagInt))
                {
                    // Bit 0 (HCF_HIGHCONTRASTON = 1) indicates high contrast is active
                    return (flagInt & 1) != 0;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the Windows system accent color via Registry.
    /// </summary>
    private static System.Drawing.Color? GetWindowsAccentColor()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");

            if (key == null)
            {
                return null;
            }

            var accentColorObj = key.GetValue("AccentColor");
            if (accentColorObj is int accentColorDword)
            {
                // Windows stores color as AABBGGRR, convert to ARGB
                var colorBytes = BitConverter.GetBytes(accentColorDword);
                return System.Drawing.Color.FromArgb(
                    colorBytes[3], // A
                    colorBytes[0], // R
                    colorBytes[1], // G
                    colorBytes[2]  // B
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read Windows accent color from registry");
            return null;
        }
    }

    /// <summary>
    /// Disposes resources used by the ThemeService.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _systemThemeSubscription?.Dispose();
        _themeSubject?.Dispose();

        _disposed = true;
    }
}
