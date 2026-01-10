using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Controls;

/// <summary>
/// User control for displaying PDF bookmarks in a hierarchical tree view.
/// Provides navigation to bookmarked pages when items are selected.
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
        var app = (App)Application.Current;
        ViewModel = app.Services.GetRequiredService<BookmarksViewModel>();
    }

    /// <summary>
    /// Handles bookmark item invocation (clicked/tapped).
    /// Navigates to the bookmarked page.
    /// </summary>
    private void BookmarksTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is BookmarkNode bookmark)
        {
            // Execute navigation command
            ViewModel.NavigateToBookmarkCommand.Execute(bookmark);
        }
    }
}
