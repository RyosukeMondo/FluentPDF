using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FluentPDF.Avalonia.Converters;

/// <summary>
/// Converts a boolean to a border brush (selected vs. unselected state).
/// </summary>
public class BoolToBorderBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected
                ? new SolidColorBrush(Color.Parse("#0078D4")) // Accent blue for selected
                : new SolidColorBrush(Colors.Transparent);     // Transparent for unselected
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
