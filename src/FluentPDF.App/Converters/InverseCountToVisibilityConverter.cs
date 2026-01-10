using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a count value to Visibility.Collapsed when count equals the parameter value, otherwise Visibility.Visible.
/// Used to hide UI elements when collection has a specific count (e.g., hide when empty).
/// </summary>
public class InverseCountToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count && parameter is string paramStr && int.TryParse(paramStr, out int targetCount))
        {
            return count == targetCount ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
