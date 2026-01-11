using FluentPDF.App.Models;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace FluentPDF.App.Controls;

/// <summary>
/// User control for displaying PDF page thumbnails in a sidebar.
/// Provides visual navigation with lazy loading and virtualization.
/// </summary>
public sealed partial class ThumbnailsSidebar : UserControl
{
    private const int ItemHeight = 228; // 220 (Grid height) + 8 (spacing)

    /// <summary>
    /// Gets the view model for this control.
    /// </summary>
    public ThumbnailsViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailsSidebar"/> class.
    /// </summary>
    public ThumbnailsSidebar()
    {
        InitializeComponent();

        // Resolve ViewModel from DI container
        var app = (App)Application.Current;
        ViewModel = app.Services.GetRequiredService<ThumbnailsViewModel>();
    }

    /// <summary>
    /// Handles thumbnail button loaded event to set accessibility properties.
    /// </summary>
    private void ThumbnailButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ThumbnailItem item)
        {
            // Set accessibility name for screen readers
            AutomationProperties.SetName(button, $"Page {item.PageNumber} thumbnail");
            AutomationProperties.SetAutomationId(button, $"ThumbnailPage_{item.PageNumber}");

            // Add arrow key navigation handler
            button.KeyDown += ThumbnailButton_KeyDown;
        }
    }

    /// <summary>
    /// Handles arrow key navigation between thumbnails.
    /// </summary>
    private void ThumbnailButton_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ThumbnailItem item)
        {
            return;
        }

        var currentIndex = ViewModel.Thumbnails.IndexOf(item);
        Button? targetButton = null;

        switch (e.Key)
        {
            case VirtualKey.Up:
                // Move to previous thumbnail
                if (currentIndex > 0)
                {
                    var targetItem = ViewModel.Thumbnails[currentIndex - 1];
                    targetButton = FindButtonForItem(targetItem);
                }
                e.Handled = true;
                break;

            case VirtualKey.Down:
                // Move to next thumbnail
                if (currentIndex < ViewModel.Thumbnails.Count - 1)
                {
                    var targetItem = ViewModel.Thumbnails[currentIndex + 1];
                    targetButton = FindButtonForItem(targetItem);
                }
                e.Handled = true;
                break;
        }

        // Set focus on target button
        if (targetButton != null)
        {
            targetButton.Focus(FocusState.Keyboard);
        }
    }

    /// <summary>
    /// Finds the button control for a given thumbnail item.
    /// </summary>
    private Button? FindButtonForItem(ThumbnailItem item)
    {
        // Try to find the realized element in the ItemsRepeater
        var element = ThumbnailsRepeater.TryGetElement(ViewModel.Thumbnails.IndexOf(item));
        return element as Button;
    }

    /// <summary>
    /// Handles thumbnail button click to navigate to the selected page.
    /// </summary>
    private void ThumbnailButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ThumbnailItem item)
        {
            // Execute navigation command
            ViewModel.NavigateToPageCommand.Execute(item.PageNumber);
        }
    }

    /// <summary>
    /// Handles keyboard accelerator (Enter/Space) to navigate to the focused thumbnail.
    /// </summary>
    private void ThumbnailKeyboardAccelerator_Invoked(
        Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
        Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.Element is Button button && button.DataContext is ThumbnailItem item)
        {
            // Execute navigation command
            ViewModel.NavigateToPageCommand.Execute(item.PageNumber);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Handles scroll viewer view changes to implement lazy loading.
    /// Loads thumbnails for visible items as the user scrolls.
    /// </summary>
    private async void ThumbnailsScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (ViewModel.Thumbnails.Count == 0)
        {
            return;
        }

        // Calculate visible range based on scroll position
        var verticalOffset = ThumbnailsScrollViewer.VerticalOffset;
        var viewportHeight = ThumbnailsScrollViewer.ViewportHeight;

        // Calculate indices with buffer (load items slightly outside viewport)
        var startIndex = Math.Max(0, (int)(verticalOffset / ItemHeight) - 2);
        var endIndex = Math.Min(
            ViewModel.Thumbnails.Count,
            (int)((verticalOffset + viewportHeight) / ItemHeight) + 3
        );

        // Load thumbnails for visible range
        await ViewModel.LoadVisibleThumbnailsAsync(startIndex, endIndex);
    }

    /// <summary>
    /// Handles right-click on thumbnail to select it before showing context menu.
    /// </summary>
    private void ThumbnailButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ThumbnailItem item)
        {
            // Select the right-clicked item if not already selected
            if (!item.IsSelected)
            {
                // Clear other selections and select this item
                ViewModel.NavigateToPageCommand.Execute(item.PageNumber);
            }
        }
    }

    /// <summary>
    /// Handles Rotate Right 90° menu item click.
    /// </summary>
    private async void RotateRight_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RotateRightCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Handles Rotate Left 90° menu item click.
    /// </summary>
    private async void RotateLeft_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RotateLeftCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Handles Rotate 180° menu item click.
    /// </summary>
    private async void Rotate180_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.Rotate180Command.ExecuteAsync(null);
    }

    /// <summary>
    /// Handles Delete menu item click.
    /// </summary>
    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeletePagesCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Handles Insert Blank Page (Same Size) menu item click.
    /// </summary>
    private async void InsertBlankPageSameSize_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.SameAsCurrent);
    }

    /// <summary>
    /// Handles Insert Blank Page (Letter) menu item click.
    /// </summary>
    private async void InsertBlankPageLetter_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.Letter);
    }

    /// <summary>
    /// Handles Insert Blank Page (A4) menu item click.
    /// </summary>
    private async void InsertBlankPageA4_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.A4);
    }

    /// <summary>
    /// Handles Insert Blank Page (Legal) menu item click.
    /// </summary>
    private async void InsertBlankPageLegal_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.Legal);
    }
}
