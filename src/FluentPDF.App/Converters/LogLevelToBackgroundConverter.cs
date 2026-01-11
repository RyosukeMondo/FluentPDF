using FluentPDF.Core.Observability;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a LogLevel enum value to a corresponding background color brush.
/// Error logs use light red, Warning logs use light yellow, others use transparent.
/// </summary>
public class LogLevelToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromArgb(40, 255, 100, 100));
    private static readonly SolidColorBrush WarningBrush = new(Color.FromArgb(40, 255, 200, 100));
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => ErrorBrush,
                LogLevel.Critical => ErrorBrush,
                LogLevel.Warning => WarningBrush,
                _ => TransparentBrush
            };
        }

        return TransparentBrush;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for LogLevelToBackgroundConverter");
    }
}
