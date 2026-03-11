using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FluentPDF.Avalonia.Dialogs;

public partial class WelcomeDialog : Window
{
    public WelcomeDialog()
    {
        InitializeComponent();
    }

    private void OnGetStartedClick(object? sender, RoutedEventArgs e) => Close(false);
    private void OnOpenPdfClick(object? sender, RoutedEventArgs e) => Close(true);
}
