using Avalonia.Controls;
using FluentPDF.Avalonia.Dialogs;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Thin facade for showing XAML-based dialogs.
/// All dialog UI is defined in /Dialogs/*.axaml with DynamicResource theme brushes.
/// </summary>
public static class DialogHelper
{
    public static async Task ShowErrorDialogAsync(Window owner, string title, string message)
    {
        var dialog = new ErrorDialog(title, message);
        await dialog.ShowDialog(owner);
    }

    public static async Task<bool> ShowConfirmationDialogAsync(
        Window owner, string title, string message)
    {
        var dialog = new ConfirmationDialog(title, message);
        return await dialog.ShowDialog<bool>(owner);
    }

    public static async Task<SaveConfirmationResult> ShowSaveConfirmationDialogAsync(
        Window owner, string filename)
    {
        var dialog = new SaveConfirmationDialog(filename);
        return await dialog.ShowDialog<SaveConfirmationResult>(owner);
    }

    public static async Task<string?> ShowTextInputDialogAsync(
        Window owner, string title, string prompt, string defaultValue = "")
    {
        var dialog = new TextInputDialog(title, prompt, defaultValue);
        return await dialog.ShowDialog<string?>(owner);
    }

    public static async Task<WatermarkDialogResult?> ShowWatermarkDialogAsync(Window owner)
    {
        var dialog = new WatermarkDialog();
        return await dialog.ShowDialog<WatermarkDialogResult?>(owner);
    }

    public static async Task<StampDialogResult?> ShowStampDialogAsync(Window owner)
    {
        var dialog = new StampDialog();
        return await dialog.ShowDialog<StampDialogResult?>(owner);
    }

    public static async Task<SecurityDialogResult?> ShowSecurityDialogAsync(Window owner)
    {
        var dialog = new SecurityDialog();
        return await dialog.ShowDialog<SecurityDialogResult?>(owner);
    }

    public static async Task<bool> ShowWelcomeDialogAsync(Window owner)
    {
        var dialog = new WelcomeDialog();
        return await dialog.ShowDialog<bool>(owner);
    }
}

/// <summary>Result of save confirmation dialog.</summary>
public enum SaveConfirmationResult
{
    Save,
    DontSave,
    Cancel
}

/// <summary>Result from the watermark configuration dialog.</summary>
public sealed class WatermarkDialogResult
{
    public string Text { get; init; } = "WATERMARK";
    public float Opacity { get; init; } = 0.3f;
    public float FontSize { get; init; } = 72f;
    public int PositionIndex { get; init; }
}

/// <summary>Result from the stamp selection dialog.</summary>
public sealed class StampDialogResult
{
    public int StampTypeIndex { get; init; }
}

/// <summary>Result from the security settings dialog.</summary>
public sealed class SecurityDialogResult
{
    public string OwnerPassword { get; init; } = string.Empty;
    public string? UserPassword { get; init; }
    public bool UseAes256 { get; init; } = true;
    public bool AllowPrint { get; init; } = true;
    public bool AllowCopy { get; init; } = true;
}
