using System.Linq;
using FluentPDF.App.Models;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
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

    /// <summary>
    /// Handles drag starting event to set up drag data.
    /// </summary>
    private void ThumbnailButton_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is Button button && button.DataContext is ThumbnailItem item)
        {
            // Get all selected page indices
            var selectedIndices = ViewModel.Thumbnails
                .Where(t => t.IsSelected)
                .Select(t => t.PageNumber - 1) // Convert to zero-based index
                .ToArray();

            if (selectedIndices.Length == 0)
            {
                // If nothing selected, select the item being dragged
                selectedIndices = new[] { item.PageNumber - 1 };
            }

            // Store the indices in the data package
            args.Data.Properties.Add("PageIndices", selectedIndices);
            args.Data.RequestedOperation = DataPackageOperation.Move;

            // Set drag UI with page count
            var count = selectedIndices.Length;
            args.DragUI.SetContentFromDataPackage();
        }
    }

    /// <summary>
    /// Handles drag over event to show drop indicator.
    /// </summary>
    private void ThumbnailButton_DragOver(object sender, DragEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ThumbnailItem item)
        {
            return;
        }

        // Check if we have page indices in the drag data
        if (!e.DataView.Properties.ContainsKey("PageIndices"))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Move;

        // Show drop indicator at top or bottom based on cursor position
        var position = e.GetPosition(button);
        var halfHeight = button.ActualHeight / 2;

        // Find the drop indicators in the button's template
        var grid = FindChild<Grid>(button);
        if (grid != null)
        {
            var topIndicator = grid.FindName("DropIndicatorTop") as Border;
            var bottomIndicator = grid.FindName("DropIndicatorBottom") as Border;

            if (topIndicator != null && bottomIndicator != null)
            {
                if (position.Y < halfHeight)
                {
                    // Show top indicator
                    topIndicator.Visibility = Visibility.Visible;
                    bottomIndicator.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Show bottom indicator
                    topIndicator.Visibility = Visibility.Collapsed;
                    bottomIndicator.Visibility = Visibility.Visible;
                }
            }
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles drop event to reorder pages.
    /// </summary>
    private async void ThumbnailButton_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ThumbnailItem targetItem)
        {
            return;
        }

        // Hide drop indicators
        HideDropIndicators(button);

        // Get the dragged page indices
        if (e.DataView.Properties.TryGetValue("PageIndices", out var indicesObj) &&
            indicesObj is int[] sourceIndices)
        {
            // Determine target index based on drop position
            var position = e.GetPosition(button);
            var halfHeight = button.ActualHeight / 2;
            var targetIndex = targetItem.PageNumber - 1; // Convert to zero-based

            // If dropping in bottom half, insert after the target
            if (position.Y >= halfHeight)
            {
                targetIndex++;
            }

            // Perform the reorder operation
            await ViewModel.MovePagesTo(sourceIndices, targetIndex);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles drag leave event to hide drop indicators.
    /// </summary>
    private void ThumbnailButton_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Button button)
        {
            HideDropIndicators(button);
        }
    }

    /// <summary>
    /// Hides drop indicators for a thumbnail button.
    /// </summary>
    private void HideDropIndicators(Button button)
    {
        var grid = FindChild<Grid>(button);
        if (grid != null)
        {
            var topIndicator = grid.FindName("DropIndicatorTop") as Border;
            var bottomIndicator = grid.FindName("DropIndicatorBottom") as Border;

            if (topIndicator != null)
            {
                topIndicator.Visibility = Visibility.Collapsed;
            }
            if (bottomIndicator != null)
            {
                bottomIndicator.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// Finds a child element of a specific type in the visual tree.
    /// </summary>
    private T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
