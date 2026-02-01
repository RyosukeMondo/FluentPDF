using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts a count value to false (collapsed) when count equals the parameter value, otherwise true (visible).
/// Used to hide UI elements when collection has a specific count (e.g., hide when empty).
/// </summary>
public class InverseCountToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count && parameter is string paramStr && int.TryParse(paramStr, out int targetCount))
        {
            return count != targetCount;
        }

        return true;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
