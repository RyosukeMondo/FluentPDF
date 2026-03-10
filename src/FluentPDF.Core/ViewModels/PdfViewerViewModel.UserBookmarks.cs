using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// User bookmark management: toggle, load, navigate, and track bookmarked state.
/// </summary>
public partial class PdfViewerViewModel
{
    private IUserBookmarkService? _userBookmarkService;

    /// <summary>
    /// Injected by the DI factory after construction (same pattern as Thumbnails, Bookmarks, etc.).
    /// </summary>
    public IUserBookmarkService? UserBookmarkService
    {
        get => _userBookmarkService;
        set => _userBookmarkService = value;
    }

    /// <summary>
    /// Observable collection of user bookmarks for the current document.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<UserBookmark> _userBookmarks = new();

    /// <summary>
    /// Whether the current page has a user bookmark.
    /// </summary>
    [ObservableProperty]
    private bool _isCurrentPageBookmarked;

    /// <summary>
    /// Toggles the user bookmark on the current page.
    /// Adds a bookmark if none exists; removes it if one does.
    /// </summary>
    [RelayCommand]
    private void ToggleUserBookmark()
    {
        if (_currentDocument == null || _userBookmarkService == null)
        {
            _logger.LogWarning("Cannot toggle bookmark: no document or service");
            return;
        }

        var page = CurrentPageNumber;
        var filePath = _currentDocument.FilePath;

        if (_userBookmarkService.HasBookmark(filePath, page))
        {
            _userBookmarkService.RemoveBookmark(filePath, page);
            _logger.LogInformation("Removed user bookmark on page {Page}", page);
        }
        else
        {
            _userBookmarkService.AddBookmark(filePath, page);
            _logger.LogInformation("Added user bookmark on page {Page}", page);
        }

        RefreshUserBookmarks();
    }

    /// <summary>
    /// Navigates to the page of a user bookmark.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToUserBookmarkAsync(UserBookmark? bookmark)
    {
        if (bookmark == null) return;

        if (bookmark.PageNumber != CurrentPageNumber)
        {
            await Navigation.GoToPageCommand.ExecuteAsync(bookmark.PageNumber);
        }
    }

    /// <summary>
    /// Reloads user bookmarks from the service and updates IsCurrentPageBookmarked.
    /// Called after document load and after page changes.
    /// </summary>
    public void RefreshUserBookmarks()
    {
        if (_currentDocument == null || _userBookmarkService == null)
        {
            UserBookmarks.Clear();
            IsCurrentPageBookmarked = false;
            return;
        }

        var filePath = _currentDocument.FilePath;
        var bookmarks = _userBookmarkService.GetBookmarks(filePath);

        UserBookmarks.Clear();
        foreach (var b in bookmarks)
        {
            UserBookmarks.Add(b);
        }

        IsCurrentPageBookmarked = _userBookmarkService.HasBookmark(filePath, CurrentPageNumber);
    }

    /// <summary>
    /// Updates IsCurrentPageBookmarked when the page changes.
    /// Called from OnNavigationPropertyChanged.
    /// </summary>
    private void UpdateCurrentPageBookmarkState()
    {
        if (_currentDocument == null || _userBookmarkService == null)
        {
            IsCurrentPageBookmarked = false;
            return;
        }

        IsCurrentPageBookmarked = _userBookmarkService.HasBookmark(
            _currentDocument.FilePath, CurrentPageNumber);
    }
}
