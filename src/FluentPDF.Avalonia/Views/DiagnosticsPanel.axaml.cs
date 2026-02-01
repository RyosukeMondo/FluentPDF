using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FluentPDF.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Diagnostics panel view displaying real-time performance metrics.
/// Shows FPS (color-coded), memory usage, and frame timing information.
/// Toggle visibility with Ctrl+Shift+D keyboard shortcut.
/// </summary>
/// <remarks>
/// This panel overlays the main content and uses GlassPanel for the liquid glass aesthetic.
/// Metrics are updated every 500ms via the DiagnosticsPanelViewModel.
///
/// Performance thresholds:
/// - FPS: Green (60+), Yellow (30-60), Red (less than 30)
/// - Memory: Warning at 500MB, Critical at 1000MB
/// - Frame timing: Target is 16.67ms for 60 FPS
///
/// The panel automatically handles theme changes and respects accessibility settings.
/// </remarks>
public partial class DiagnosticsPanel : UserControl
{
    private readonly ILogger<DiagnosticsPanel>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsPanel"/> class.
    /// </summary>
    public DiagnosticsPanel()
    {
        InitializeComponent();

        // Try to get logger from app services (may be null in designer)
        try
        {
            _logger = App.GetService<ILogger<DiagnosticsPanel>>();
            _logger?.LogDebug("DiagnosticsPanel view initialized");
        }
        catch
        {
            // Designer mode or services not available - continue without logging
        }
    }

    /// <summary>
    /// Called when the control is attached to the visual tree.
    /// Sets up keyboard shortcut handlers.
    /// </summary>
    /// <param name="e">Event arguments containing visual tree information.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Find the top-level window to attach keyboard handler
        var window = this.VisualRoot as Window;
        if (window != null)
        {
            window.KeyDown += OnWindowKeyDown;
            _logger?.LogDebug("Keyboard shortcut handler attached to window");
        }
    }

    /// <summary>
    /// Called when the control is detached from the visual tree.
    /// Cleans up keyboard shortcut handlers.
    /// </summary>
    /// <param name="e">Event arguments containing visual tree information.</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Remove keyboard handler when detaching
        var window = this.VisualRoot as Window;
        if (window != null)
        {
            window.KeyDown -= OnWindowKeyDown;
            _logger?.LogDebug("Keyboard shortcut handler detached from window");
        }
    }

    /// <summary>
    /// Handles window-level keyboard events to implement Ctrl+Shift+D shortcut.
    /// </summary>
    /// <param name="sender">The window that raised the event.</param>
    /// <param name="e">Keyboard event arguments.</param>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        // Check for Ctrl+Shift+D keyboard shortcut
        if (e.Key == Key.D &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            ToggleVisibility();
            e.Handled = true;

            _logger?.LogInformation("Diagnostics panel toggled via keyboard shortcut (Ctrl+Shift+D)");
        }
    }

    /// <summary>
    /// Toggles the visibility of the diagnostics panel.
    /// Called by keyboard shortcut handler and close button.
    /// </summary>
    private void ToggleVisibility()
    {
        if (DataContext is DiagnosticsPanelViewModel viewModel)
        {
            viewModel.ToggleVisibilityCommand.Execute(null);
            _logger?.LogDebug("Diagnostics panel visibility toggled. New state: {IsVisible}",
                viewModel.IsVisible);
        }
        else
        {
            _logger?.LogWarning("Cannot toggle visibility: DataContext is not DiagnosticsPanelViewModel");
        }
    }

    /// <summary>
    /// Initializes XAML components.
    /// Auto-generated method, do not modify manually.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
