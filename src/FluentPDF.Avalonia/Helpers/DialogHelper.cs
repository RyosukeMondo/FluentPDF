using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Helper class for creating standard dialogs.
/// Reduces code duplication in MainWindow.
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Shows an error dialog with OK button.
    /// </summary>
    public static async Task ShowErrorDialogAsync(Window owner, string title, string message)
    {
        var dialog = CreateDialog(title, 400, 200, owner);
        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        var button = new Button
        {
            Content = "OK",
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        button.Click += (s, e) => dialog.Close();

        panel.Children.Add(button);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    public static async Task<bool> ShowConfirmationDialogAsync(Window owner, string title, string message)
    {
        var dialog = CreateDialog(title, 400, 180, owner);
        var result = false;

        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var yesButton = new Button { Content = "Yes", Width = 100 };
        yesButton.Click += (s, e) => { result = true; dialog.Close(); };

        var noButton = new Button { Content = "No", Width = 100 };
        noButton.Click += (s, e) => { result = false; dialog.Close(); };

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>
    /// Shows a save confirmation dialog with Save/Don't Save/Cancel buttons.
    /// </summary>
    public static async Task<SaveConfirmationResult> ShowSaveConfirmationDialogAsync(
        Window owner,
        string filename)
    {
        var dialog = CreateDialog("Unsaved Changes", 400, 200, owner);
        var result = SaveConfirmationResult.Cancel;

        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock
        {
            Text = $"Do you want to save changes to {filename}?",
            TextWrapping = TextWrapping.Wrap
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var saveButton = new Button { Content = "Save", Width = 100 };
        saveButton.Click += (s, e) => { result = SaveConfirmationResult.Save; dialog.Close(); };

        var dontSaveButton = new Button { Content = "Don't Save", Width = 100 };
        dontSaveButton.Click += (s, e) => { result = SaveConfirmationResult.DontSave; dialog.Close(); };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) => { result = SaveConfirmationResult.Cancel; dialog.Close(); };

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(dontSaveButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }

    private static Window CreateDialog(string title, double width, double height, Window owner)
    {
        return new Window
        {
            Width = width,
            Height = height,
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
    }

    private static StackPanel CreateDialogPanel()
    {
        return new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };
    }
}

/// <summary>
/// Result of save confirmation dialog.
/// </summary>
public enum SaveConfirmationResult
{
    Save,
    DontSave,
    Cancel
}
