using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a zero-based match index to a one-based display value.
/// For example, 0 -> "1", 5 -> "6", -1 -> "0".
/// </summary>
public class MatchCounterConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int index)
        {
            return index >= 0 ? (index + 1).ToString() : "0";
        }

        return "0";
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
