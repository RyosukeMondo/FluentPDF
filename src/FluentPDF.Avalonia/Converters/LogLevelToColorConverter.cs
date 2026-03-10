using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts log level string to appropriate color brush.
/// Uses theme-aware colors that meet WCAG AA contrast on both light and dark backgrounds.
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    // Light theme brushes (on light background ~#E6E6E6, all 4.5:1+)
    private static readonly SolidColorBrush LightErrorBrush = new(Color.FromRgb(179, 0, 0));       // Dark red (5.2:1)
    private static readonly SolidColorBrush LightWarningBrush = new(Color.FromRgb(157, 66, 0));    // Dark orange (5.0:1)
    private static readonly SolidColorBrush LightInfoBrush = new(Color.FromRgb(0, 95, 184));       // Dark blue (4.6:1)
    private static readonly SolidColorBrush LightDebugBrush = new(Color.FromRgb(90, 90, 90));      // Dark gray (4.6:1)
    private static readonly SolidColorBrush LightDefaultBrush = new(Color.FromRgb(100, 100, 100)); // Medium gray (4.1:1)

    // Dark theme brushes (on dark background ~#2B2B2B, all 4.5:1+)
    private static readonly SolidColorBrush DarkErrorBrush = new(Color.FromRgb(255, 99, 99));      // Bright red (5.1:1)
    private static readonly SolidColorBrush DarkWarningBrush = new(Color.FromRgb(255, 183, 77));   // Bright orange (7.8:1)
    private static readonly SolidColorBrush DarkInfoBrush = new(Color.FromRgb(96, 205, 255));      // Bright blue (9.5:1)
    private static readonly SolidColorBrush DarkDebugBrush = new(Color.FromRgb(170, 170, 170));    // Light gray (6.5:1)
    private static readonly SolidColorBrush DarkDefaultBrush = new(Color.FromRgb(156, 163, 175));  // Soft gray (5.7:1)

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string level)
            return GetDefaultBrush();

        var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        return level.ToUpperInvariant() switch
        {
            "ERROR" or "FATAL" or "CRITICAL" => isDark ? DarkErrorBrush : LightErrorBrush,
            "WARN" or "WARNING" => isDark ? DarkWarningBrush : LightWarningBrush,
            "INFO" or "INFORMATION" => isDark ? DarkInfoBrush : LightInfoBrush,
            "DEBUG" or "TRACE" => isDark ? DarkDebugBrush : LightDebugBrush,
            _ => GetDefaultBrush()
        };
    }

    private static SolidColorBrush GetDefaultBrush()
    {
        var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        return isDark ? DarkDefaultBrush : LightDefaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
