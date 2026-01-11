using FluentPDF.App.Models;
using FluentPDF.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}
