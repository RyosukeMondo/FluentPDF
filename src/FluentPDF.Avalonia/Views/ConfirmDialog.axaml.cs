using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FluentPDF.Avalonia.Views;

public partial class ConfirmDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message)
        : this()
    {
        Title = title;

        var titleBlock = this.FindControl<TextBlock>("TitleTextBlock");
        var messageBlock = this.FindControl<TextBlock>("MessageTextBlock");

        if (titleBlock != null)
            titleBlock.Text = title;

        if (messageBlock != null)
            messageBlock.Text = message;
    }

    private void OnYesClick(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close(true);
    }

    private void OnNoClick(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close(false);
    }

    public static async Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var dialog = new ConfirmDialog(title, message);
        var result = await dialog.ShowDialog<bool?>(owner);
        return result ?? false;
    }
}
