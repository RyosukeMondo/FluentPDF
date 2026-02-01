using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts a boolean value to Visibility (Avalonia: bool).
/// True converts to true (visible), False converts to false (collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue; // In Avalonia, IsVisible is bool, not enum
        }
        return false;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool visibility)
        {
            return visibility;
        }
        return false;
    }
}
