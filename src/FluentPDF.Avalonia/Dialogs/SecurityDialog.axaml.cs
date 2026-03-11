using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentPDF.Avalonia.Helpers;

namespace FluentPDF.Avalonia.Dialogs;

public partial class SecurityDialog : Window
{
    public SecurityDialog()
    {
        InitializeComponent();
    }

    private void OnEncryptClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OwnerPasswordBox.Text))
        {
            OwnerPasswordBox.Watermark = "Required!";
            return;
        }

        Close(new SecurityDialogResult
        {
            OwnerPassword = OwnerPasswordBox.Text!,
            UserPassword = string.IsNullOrWhiteSpace(UserPasswordBox.Text)
                ? null : UserPasswordBox.Text,
            UseAes256 = StrengthCombo.SelectedIndex == 1,
            AllowPrint = AllowPrintCheck.IsChecked == true,
            AllowCopy = AllowCopyCheck.IsChecked == true
        });
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
