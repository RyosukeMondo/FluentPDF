using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts log level string to appropriate color brush.
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(220, 38, 38));    // Red
    private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(245, 158, 11)); // Orange
    private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(59, 130, 246));    // Blue
    private static readonly SolidColorBrush DebugBrush = new(Color.FromRgb(107, 114, 128));  // Gray
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(156, 163, 175)); // Light Gray

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string level)
            return DefaultBrush;

        return level.ToUpperInvariant() switch
        {
            "ERROR" or "FATAL" or "CRITICAL" => ErrorBrush,
            "WARN" or "WARNING" => WarningBrush,
            "INFO" or "INFORMATION" => InfoBrush,
            "DEBUG" or "TRACE" => DebugBrush,
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
