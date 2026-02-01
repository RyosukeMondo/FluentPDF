using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts a count value to a boolean for visibility binding.
/// Returns true when count equals the parameter value, otherwise false.
/// Used to show UI elements based on collection counts.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count && parameter is string paramStr && int.TryParse(paramStr, out int targetCount))
        {
            return count == targetCount;
        }

        return false;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
