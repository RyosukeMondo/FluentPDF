using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FluentPDF.Avalonia.Dialogs;

public partial class TextInputDialog : Window
{
    public TextInputDialog()
    {
        InitializeComponent();
    }

    public TextInputDialog(string title, string prompt, string defaultValue = "") : this()
    {
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close(InputBox.Text);
    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
