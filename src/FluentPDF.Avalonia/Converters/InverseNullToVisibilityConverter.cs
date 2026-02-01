using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts a null value to true (visible), and non-null to false (collapsed).
/// Used to show empty state messages when data is not available.
/// </summary>
public class InverseNullToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
