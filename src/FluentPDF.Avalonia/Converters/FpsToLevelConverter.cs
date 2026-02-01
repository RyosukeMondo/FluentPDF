using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts FPS value to a performance level description.
/// Used in diagnostics panel to display performance status text.
/// </summary>
/// <remarks>
/// FPS thresholds:
/// - Excellent: 60+ FPS
/// - Good: 45-59 FPS
/// - Fair: 30-44 FPS
/// - Poor: 15-29 FPS
/// - Critical: Less than 15 FPS
/// </remarks>
public sealed class FpsToLevelConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML via x:Static.
    /// </summary>
    public static readonly FpsToLevelConverter Instance = new();

    /// <summary>
    /// Converts FPS double value to performance level string.
    /// </summary>
    /// <param name="value">The FPS value as a double.</param>
    /// <param name="targetType">Target type (should be string).</param>
    /// <param name="parameter">Optional parameter (unused).</param>
    /// <param name="culture">Culture info for formatting.</param>
    /// <returns>Performance level description string.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double fps)
        {
            return "Unknown";
        }

        return fps switch
        {
            >= 60 => "Excellent",
            >= 45 => "Good",
            >= 30 => "Fair",
            >= 15 => "Poor",
            _ => "Critical"
        };
    }

    /// <summary>
    /// Not implemented - one-way converter only.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("FpsToLevelConverter is a one-way converter.");
    }
}
