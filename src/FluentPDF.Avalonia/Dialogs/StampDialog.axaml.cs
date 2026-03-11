using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentPDF.Avalonia.Helpers;

namespace FluentPDF.Avalonia.Dialogs;

public partial class StampDialog : Window
{
    public StampDialog()
    {
        InitializeComponent();
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        Close(new StampDialogResult { StampTypeIndex = StampCombo.SelectedIndex });
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
