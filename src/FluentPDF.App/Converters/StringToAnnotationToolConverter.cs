using FluentPDF.App.ViewModels;
using Microsoft.UI.Xaml.Data;

namespace FluentPDF.App.Converters;

/// <summary>
/// Converts a string representation of an annotation tool to the AnnotationTool enum value.
/// </summary>
public sealed class StringToAnnotationToolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Forward conversion (enum to string) - not typically needed
        if (value is AnnotationTool tool)
        {
            return tool.ToString();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // Backward conversion (string to enum) - this is what we need
        if (parameter is string toolName && Enum.TryParse<AnnotationTool>(toolName, out var tool))
        {
            return tool;
        }
        return AnnotationTool.None;
    }
}
