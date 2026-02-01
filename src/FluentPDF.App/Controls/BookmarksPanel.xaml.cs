using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace FluentPDF.App.Controls;

/// <summary>
/// User control for displaying PDF bookmarks in a hierarchical tree view with liquid glass aesthetic.
/// Provides navigation to bookmarked pages and smooth expand/collapse animations (200ms ease-out).
/// </summary>
public sealed partial class BookmarksPanel : UserControl
{
    /// <summary>
    /// Gets the view model for this control.
    /// </summary>
    public BookmarksViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarksPanel"/> class.
    /// </summary>
    public BookmarksPanel()
    {
        InitializeComponent();

        // Resolve ViewModel from DI container
        ViewModel = App.GetService<BookmarksViewModel>();

        // Subscribe to TreeViewItem expand/collapse events for smooth animations
        BookmarksTreeView.Loaded += OnTreeViewLoaded;
    }

    /// <summary>
    /// Handles TreeView loaded event to attach expand/collapse animations.
    /// </summary>
    private void OnTreeViewLoaded(object sender, RoutedEventArgs e)
    {
        // Attach animation handlers to all TreeViewItems
        AttachAnimationsToTreeViewItems(BookmarksTreeView);
    }

    /// <summary>
    /// Recursively attaches expand/collapse animations to TreeViewItems.
    /// </summary>
    private void AttachAnimationsToTreeViewItems(DependencyObject parent)
    {
        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is TreeViewItem treeViewItem)
            {
                // Note: WinUI 3 TreeViewItem doesn't have Expanding/Collapsed events
                // Animations are handled via XAML visual states instead
            }

            // Recurse through visual tree
            AttachAnimationsToTreeViewItems(child);
        }
    }

    /// <summary>
    /// Handles bookmark item invocation (clicked/tapped).
    /// Navigates to the bookmarked page while preserving bookmark navigation.
    /// </summary>
    private void BookmarksTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is BookmarkNode bookmark)
        {
            // Execute navigation command (preserves existing functionality)
            ViewModel.NavigateToBookmarkCommand.Execute(bookmark);
        }
    }
}
