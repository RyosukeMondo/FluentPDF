using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace FluentPDF.Avalonia.Views;

public enum MessageDialogType
{
    Information,
    Warning,
    Error,
    Success
}

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string title, string message, MessageDialogType dialogType = MessageDialogType.Information)
        : this()
    {
        Title = title;

        var titleBlock = this.FindControl<TextBlock>("TitleTextBlock");
        var messageBlock = this.FindControl<TextBlock>("MessageTextBlock");
        var icon = this.FindControl<PathIcon>("DialogIcon");

        if (titleBlock != null)
            titleBlock.Text = title;

        if (messageBlock != null)
            messageBlock.Text = message;

        if (icon != null)
        {
            switch (dialogType)
            {
                case MessageDialogType.Information:
                    icon.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");
                    icon.Foreground = GetSemanticBrush("InfoBrush");
                    break;

                case MessageDialogType.Warning:
                    icon.Data = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");
                    icon.Foreground = GetSemanticBrush("WarningBrush");
                    break;

                case MessageDialogType.Error:
                    icon.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z");
                    icon.Foreground = GetSemanticBrush("ErrorBrush");
                    break;

                case MessageDialogType.Success:
                    icon.Data = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z");
                    icon.Foreground = GetSemanticBrush("SuccessBrush");
                    break;
            }
        }
    }

    /// <summary>
    /// Retrieves a semantic brush from application resources, with a fallback.
    /// Uses theme-aware semantic colors that meet WCAG contrast requirements.
    /// </summary>
    private static IBrush GetSemanticBrush(string resourceKey)
    {
        if (Application.Current != null &&
            Application.Current.TryFindResource(resourceKey, out var resource) &&
            resource is IBrush brush)
        {
            return brush;
        }

        // Fallback if resource not found
        return resourceKey switch
        {
            "InfoBrush" => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            "WarningBrush" => new SolidColorBrush(Color.FromRgb(202, 80, 16)),
            "ErrorBrush" => new SolidColorBrush(Color.FromRgb(209, 52, 56)),
            "SuccessBrush" => new SolidColorBrush(Color.FromRgb(16, 124, 16)),
            _ => Brushes.Gray
        };
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public static async Task ShowAsync(Window owner, string title, string message, MessageDialogType dialogType = MessageDialogType.Information)
    {
        var dialog = new MessageDialog(title, message, dialogType);
        await dialog.ShowDialog(owner);
    }
}
