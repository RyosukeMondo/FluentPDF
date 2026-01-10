using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a count value to Visibility.Visible when count equals the parameter value, otherwise Visibility.Collapsed.
/// Used to show UI elements based on collection counts.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count && parameter is string paramStr && int.TryParse(paramStr, out int targetCount))
        {
            return count == targetCount ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
