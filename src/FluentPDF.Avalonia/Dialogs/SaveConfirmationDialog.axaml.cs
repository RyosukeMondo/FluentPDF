using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentPDF.Avalonia.Helpers;

namespace FluentPDF.Avalonia.Dialogs;

public partial class SaveConfirmationDialog : Window
{
    public SaveConfirmationDialog()
    {
        InitializeComponent();
    }

    public SaveConfirmationDialog(string filename) : this()
    {
        MessageText.Text = $"Do you want to save changes to {filename}?";
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
        => Close(SaveConfirmationResult.Save);

    private void OnDontSaveClick(object? sender, RoutedEventArgs e)
        => Close(SaveConfirmationResult.DontSave);

    private void OnCancelClick(object? sender, RoutedEventArgs e)
        => Close(SaveConfirmationResult.Cancel);
}
