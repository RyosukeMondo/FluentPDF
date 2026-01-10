using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a boolean value to Visibility with inverse logic.
/// False converts to Visibility.Visible, True converts to Visibility.Collapsed.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Collapsed;
        }
        return true;
    }
}
