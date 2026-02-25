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

    /// <summary>
    /// Shows a text input dialog and returns the entered value, or null if cancelled.
    /// </summary>
    public static async Task<string?> ShowTextInputDialogAsync(
        Window owner, string title, string prompt, string defaultValue = "")
    {
        var dialog = CreateDialog(title, 400, 200, owner);
        string? result = null;

        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock
        {
            Text = prompt,
            TextWrapping = TextWrapping.Wrap
        });

        var textBox = new TextBox { Text = defaultValue };
        panel.Children.Add(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var okButton = new Button { Content = "OK", Width = 100 };
        okButton.Click += (s, e) => { result = textBox.Text; dialog.Close(); };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>
    /// Shows a watermark configuration dialog.
    /// Returns (text, opacity, position) or null if cancelled.
    /// </summary>
    public static async Task<WatermarkDialogResult?> ShowWatermarkDialogAsync(Window owner)
    {
        var dialog = CreateDialog("Add Text Watermark", 420, 380, owner);
        WatermarkDialogResult? result = null;

        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock { Text = "Watermark Text:" });
        var textBox = new TextBox { Text = "CONFIDENTIAL" };
        panel.Children.Add(textBox);

        panel.Children.Add(new TextBlock { Text = "Opacity (0.1 - 1.0):" });
        var opacitySlider = new Slider { Minimum = 0.1, Maximum = 1.0, Value = 0.3, TickFrequency = 0.1 };
        panel.Children.Add(opacitySlider);

        panel.Children.Add(new TextBlock { Text = "Font Size:" });
        var fontSizeBox = new NumericUpDown { Minimum = 12, Maximum = 144, Value = 72, Increment = 6 };
        panel.Children.Add(fontSizeBox);

        panel.Children.Add(new TextBlock { Text = "Position:" });
        var positionCombo = new ComboBox();
        positionCombo.Items.Add("Center");
        positionCombo.Items.Add("Top Left");
        positionCombo.Items.Add("Top Right");
        positionCombo.Items.Add("Bottom Left");
        positionCombo.Items.Add("Bottom Right");
        positionCombo.SelectedIndex = 0;
        panel.Children.Add(positionCombo);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var okButton = new Button { Content = "Apply", Width = 100 };
        okButton.Click += (s, e) =>
        {
            result = new WatermarkDialogResult
            {
                Text = textBox.Text ?? "WATERMARK",
                Opacity = (float)opacitySlider.Value,
                FontSize = (float)(fontSizeBox.Value ?? 72),
                PositionIndex = positionCombo.SelectedIndex
            };
            dialog.Close();
        };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>
    /// Shows a stamp type selection dialog.
    /// Returns the selected stamp type or null if cancelled.
    /// </summary>
    public static async Task<StampDialogResult?> ShowStampDialogAsync(Window owner)
    {
        var dialog = CreateDialog("Add Stamp", 400, 250, owner);
        StampDialogResult? result = null;

        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock { Text = "Stamp Type:" });
        var stampCombo = new ComboBox();
        stampCombo.Items.Add("Approved");
        stampCombo.Items.Add("Rejected");
        stampCombo.Items.Add("Draft");
        stampCombo.Items.Add("Final");
        stampCombo.Items.Add("Confidential");
        stampCombo.Items.Add("For Review");
        stampCombo.Items.Add("Copy");
        stampCombo.SelectedIndex = 0;
        panel.Children.Add(stampCombo);

        panel.Children.Add(new TextBlock { Text = "Page (current page will be used)" });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var okButton = new Button { Content = "Apply", Width = 100 };
        okButton.Click += (s, e) =>
        {
            result = new StampDialogResult { StampTypeIndex = stampCombo.SelectedIndex };
            dialog.Close();
        };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>
    /// Shows a security/encryption settings dialog.
    /// Returns the encryption settings or null if cancelled.
    /// </summary>
    public static async Task<SecurityDialogResult?> ShowSecurityDialogAsync(Window owner)
    {
        var dialog = CreateDialog("Document Security", 420, 380, owner);
        SecurityDialogResult? result = null;

        var panel = CreateDialogPanel();

        panel.Children.Add(new TextBlock { Text = "Owner Password (required):" });
        var ownerPasswordBox = new TextBox { PasswordChar = '*' };
        panel.Children.Add(ownerPasswordBox);

        panel.Children.Add(new TextBlock { Text = "User Password (optional, to open):" });
        var userPasswordBox = new TextBox { PasswordChar = '*' };
        panel.Children.Add(userPasswordBox);

        panel.Children.Add(new TextBlock { Text = "Encryption Strength:" });
        var strengthCombo = new ComboBox();
        strengthCombo.Items.Add("AES-128");
        strengthCombo.Items.Add("AES-256");
        strengthCombo.SelectedIndex = 1;
        panel.Children.Add(strengthCombo);

        var allowPrintCheck = new CheckBox { Content = "Allow Printing", IsChecked = true };
        panel.Children.Add(allowPrintCheck);

        var allowCopyCheck = new CheckBox { Content = "Allow Copying", IsChecked = true };
        panel.Children.Add(allowCopyCheck);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var okButton = new Button { Content = "Encrypt", Width = 100 };
        okButton.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(ownerPasswordBox.Text))
            {
                // Simple inline validation
                ownerPasswordBox.Watermark = "Required!";
                return;
            }
            result = new SecurityDialogResult
            {
                OwnerPassword = ownerPasswordBox.Text!,
                UserPassword = string.IsNullOrWhiteSpace(userPasswordBox.Text) ? null : userPasswordBox.Text,
                UseAes256 = strengthCombo.SelectedIndex == 1,
                AllowPrint = allowPrintCheck.IsChecked == true,
                AllowCopy = allowCopyCheck.IsChecked == true
            };
            dialog.Close();
        };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttonPanel.Children.Add(okButton);
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

/// <summary>
/// Result from the watermark configuration dialog.
/// </summary>
public sealed class WatermarkDialogResult
{
    public string Text { get; init; } = "WATERMARK";
    public float Opacity { get; init; } = 0.3f;
    public float FontSize { get; init; } = 72f;
    public int PositionIndex { get; init; }
}

/// <summary>
/// Result from the stamp selection dialog.
/// </summary>
public sealed class StampDialogResult
{
    public int StampTypeIndex { get; init; }
}

/// <summary>
/// Result from the security settings dialog.
/// </summary>
public sealed class SecurityDialogResult
{
    public string OwnerPassword { get; init; } = string.Empty;
    public string? UserPassword { get; init; }
    public bool UseAes256 { get; init; } = true;
    public bool AllowPrint { get; init; } = true;
    public bool AllowCopy { get; init; } = true;
}
