using FluentPDF.Core.Observability;
using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a LogLevel enum value to a corresponding Segoe MDL2 icon glyph.
/// </summary>
public class LogLevelToIconConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "\uE8C3",      // DeveloperTools icon
                LogLevel.Debug => "\uE8C3",      // DeveloperTools icon
                LogLevel.Information => "\uE946", // Info icon
                LogLevel.Warning => "\uE7BA",    // Warning icon
                LogLevel.Error => "\uE783",      // Error icon
                LogLevel.Critical => "\uEA39",   // ErrorBadge icon
                _ => "\uE946"                    // Default to Info icon
            };
        }

        return "\uE946"; // Default to Info icon
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("ConvertBack is not supported for LogLevelToIconConverter");
    }
}
