using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentPDF.Avalonia.Models;
using FluentPDF.Core.ViewModels;
using FluentPDF.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// User control for displaying PDF page thumbnails in a sidebar.
/// Provides visual navigation with lazy loading and virtualization.
/// </summary>
public partial class ThumbnailsSidebar : UserControl
{
    private const int ItemHeight = 228; // 220 (Grid height) + 8 (spacing)

    /// <summary>
    /// Gets the view model for this control.
    /// </summary>
    public ThumbnailsViewModel? ViewModel => DataContext as ThumbnailsViewModel;

    public ThumbnailsSidebar()
    {
        InitializeComponent();

        // Attach keyboard shortcuts
        this.KeyDown += ThumbnailsSidebar_KeyDown;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Setup scroll viewer for lazy loading
        var scrollViewer = this.FindControl<ScrollViewer>("ThumbnailsScrollViewer");
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += OnThumbnailsScrollChanged;
        }
    }

    /// <summary>
    /// Handles keyboard shortcuts for page operations.
    /// </summary>
    private async void ThumbnailsSidebar_KeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        var ctrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Delete:
                // Delete key - delete selected pages
                if (ViewModel.DeletePagesCommand?.CanExecute(null) == true)
                {
                    await ExecuteCommandAsync(ViewModel.DeletePagesCommand);
                    e.Handled = true;
                }
                break;

            case Key.R when ctrlPressed && shiftPressed:
                // Ctrl+Shift+R - rotate left
                if (ViewModel.RotateLeftCommand?.CanExecute(null) == true)
                {
                    await ExecuteCommandAsync(ViewModel.RotateLeftCommand);
                    e.Handled = true;
                }
                break;

            case Key.R when ctrlPressed:
                // Ctrl+R - rotate right
                if (ViewModel.RotateRightCommand?.CanExecute(null) == true)
                {
                    await ExecuteCommandAsync(ViewModel.RotateRightCommand);
                    e.Handled = true;
                }
                break;

            case Key.A when ctrlPressed:
                // Ctrl+A - select all
                if (ViewModel.SelectAllCommand?.CanExecute(null) == true)
                {
                    ViewModel.SelectAllCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Handles scroll viewer changes to implement lazy loading.
    /// Loads thumbnails for visible items as the user scrolls.
    /// </summary>
    private async void OnThumbnailsScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (ViewModel?.Thumbnails == null || ViewModel.Thumbnails.Count == 0)
        {
            return;
        }

        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer == null)
        {
            return;
        }

        // Calculate visible range based on scroll position
        var verticalOffset = scrollViewer.Offset.Y;
        var viewportHeight = scrollViewer.Viewport.Height;

        // Calculate indices with buffer (load items slightly outside viewport)
        var startIndex = Math.Max(0, (int)(verticalOffset / ItemHeight) - 2);
        var endIndex = Math.Min(
            ViewModel.Thumbnails.Count,
            (int)((verticalOffset + viewportHeight) / ItemHeight) + 3
        );

        // Load thumbnails for visible range
        if (ViewModel.LoadVisibleThumbnailsAsync != null)
        {
            await ViewModel.LoadVisibleThumbnailsAsync(startIndex, endIndex);
        }
    }

    /// <summary>
    /// Executes an async command if it's available.
    /// </summary>
    private async Task ExecuteCommandAsync(System.Windows.Input.ICommand command)
    {
        if (command is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null);
        }
        else if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>
    /// Helper method to find visual child by name.
    /// </summary>
    private T? FindChildByName<T>(Control parent, string name) where T : Control
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is T control && control.Name == name)
            {
                return control;
            }

            if (child is Control childControl)
            {
                var result = FindChildByName<T>(childControl, name);
                if (result != null)
                {
                    return result;
                }
            }
        }
        return null;
    }
}
