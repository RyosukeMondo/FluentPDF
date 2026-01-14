using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts enum values to integers and vice versa for binding to RadioButtons.
/// </summary>
public class EnumToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return 0;
        }

        return (int)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return 0;
        }

        return Enum.ToObject(targetType, value);
    }
}
