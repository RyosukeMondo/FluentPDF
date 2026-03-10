using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using FluentPDF.Core.ViewModels;

namespace FluentPDF.Avalonia.Controls;

public partial class BookmarksPanel : UserControl
{
    public BookmarksPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private BookmarksViewModel? _subscribedBookmarksVm;
    private PdfViewerViewModel? _subscribedViewerVm;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous
        if (_subscribedBookmarksVm != null)
        {
            _subscribedBookmarksVm.PropertyChanged -= OnBookmarksVmPropertyChanged;
            _subscribedBookmarksVm = null;
        }

        if (DataContext is BookmarksViewModel bookmarksVm)
        {
            _subscribedBookmarksVm = bookmarksVm;
            bookmarksVm.PropertyChanged += OnBookmarksVmPropertyChanged;
        }

        // Find parent PdfViewerViewModel for user bookmarks
        SubscribeToParentViewModel();
        UpdateEmptyStateVisibility();
    }

    private void SubscribeToParentViewModel()
    {
        if (_subscribedViewerVm != null)
        {
            _subscribedViewerVm.UserBookmarks.CollectionChanged -= OnUserBookmarksChanged;
            _subscribedViewerVm = null;
        }

        // Walk up the visual tree to find the PdfViewerViewModel
        var parent = this.Parent;
        while (parent != null)
        {
            if (parent.DataContext is PdfViewerViewModel viewerVm)
            {
                _subscribedViewerVm = viewerVm;
                viewerVm.UserBookmarks.CollectionChanged += OnUserBookmarksChanged;
                break;
            }
            parent = (parent as Control)?.Parent;
        }
    }

    private void OnBookmarksVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BookmarksViewModel.HasBookmarks))
        {
            UpdateEmptyStateVisibility();
        }
    }

    private void OnUserBookmarksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyStateVisibility();
    }

    private void UpdateEmptyStateVisibility()
    {
        var emptyState = this.FindControl<StackPanel>("EmptyState");
        if (emptyState == null) return;

        var hasDocBookmarks = _subscribedBookmarksVm?.HasBookmarks == true;
        var hasUserBookmarks = _subscribedViewerVm?.UserBookmarks.Count > 0;

        emptyState.IsVisible = !hasDocBookmarks && !hasUserBookmarks;
    }
}
