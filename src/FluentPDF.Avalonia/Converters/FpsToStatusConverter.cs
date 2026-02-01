using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts FPS value to a detailed status message.
/// Used in diagnostics panel to provide contextual performance feedback.
/// </summary>
/// <remarks>
/// Provides user-friendly status messages based on FPS thresholds:
/// - 60+: Optimal performance
/// - 45-59: Good performance
/// - 30-44: Acceptable performance
/// - 15-29: Poor performance
/// - Less than 15: Critical performance issues
/// </remarks>
public sealed class FpsToStatusConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML via x:Static.
    /// </summary>
    public static readonly FpsToStatusConverter Instance = new();

    /// <summary>
    /// Converts FPS double value to detailed status message.
    /// </summary>
    /// <param name="value">The FPS value as a double.</param>
    /// <param name="targetType">Target type (should be string).</param>
    /// <param name="parameter">Optional parameter (unused).</param>
    /// <param name="culture">Culture info for formatting.</param>
    /// <returns>Detailed status message string.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double fps)
        {
            return "Unknown Performance";
        }

        return fps switch
        {
            >= 60 => "Optimal Performance",
            >= 45 => "Good Performance",
            >= 30 => "Acceptable Performance",
            >= 15 => "Poor Performance - Consider reducing quality",
            _ => "Critical - Performance issues detected"
        };
    }

    /// <summary>
    /// Not implemented - one-way converter only.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("FpsToStatusConverter is a one-way converter.");
    }
}
