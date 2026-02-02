using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FluentPDF.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Diagnostics panel displaying real-time performance metrics.
/// Toggle visibility with Ctrl+Shift+D keyboard shortcut.
/// </summary>
public partial class DiagnosticsPanel : UserControl
{
    private readonly ILogger<DiagnosticsPanel>? _logger;

    public DiagnosticsPanel()
    {
        InitializeComponent();

        try
        {
            _logger = App.GetService<ILogger<DiagnosticsPanel>>();
        }
        catch
        {
            // Designer mode - continue without logging
        }
    }

    /// <summary>
    /// Called when the control is attached to the visual tree.
    /// Sets up keyboard shortcut and close button handlers.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Wire up close button
        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += (s, e) => HidePanel();
        }

        // Attach keyboard shortcut to window
        if (this.VisualRoot is Window window)
        {
            window.KeyDown += OnWindowKeyDown;
        }
    }

    /// <summary>
    /// Called when the control is detached from the visual tree.
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (this.VisualRoot is Window window)
        {
            window.KeyDown -= OnWindowKeyDown;
        }
    }

    /// <summary>
    /// Handles Ctrl+Shift+D keyboard shortcut to toggle visibility.
    /// </summary>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.D &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            ToggleVisibility();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Hides the diagnostics panel.
    /// </summary>
    private void HidePanel()
    {
        if (DataContext is DiagnosticsPanelViewModel viewModel)
        {
            viewModel.IsVisible = false;
        }
    }

    /// <summary>
    /// Toggles the visibility of the diagnostics panel.
    /// </summary>
    private void ToggleVisibility()
    {
        if (DataContext is DiagnosticsPanelViewModel viewModel)
        {
            viewModel.IsVisible = !viewModel.IsVisible;
            _logger?.LogDebug("Diagnostics panel toggled. Visible={IsVisible}", viewModel.IsVisible);
        }
    }
}
