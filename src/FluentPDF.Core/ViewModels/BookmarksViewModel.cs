using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// View model for the bookmarks panel.
/// UI-framework agnostic implementation that manages bookmark loading, panel visibility, and navigation.
/// </summary>
public partial class BookmarksViewModel : ViewModelBase
{
    private readonly IBookmarkService _bookmarkService;
    private readonly ILogger<BookmarksViewModel> _logger;
    private Func<int, Task>? _navigateToPageAction;

    /// <summary>
    /// Gets or sets the list of root-level bookmarks for the current document.
    /// </summary>
    [ObservableProperty]
    private List<BookmarkNode>? _bookmarks;

    /// <summary>
    /// Gets or sets a value indicating whether the bookmarks panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isPanelVisible = true;

    /// <summary>
    /// Gets or sets the width of the bookmarks panel in pixels.
    /// </summary>
    [ObservableProperty]
    private double _panelWidth = 250;

    /// <summary>
    /// Gets or sets a value indicating whether bookmarks are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets whether the bookmarks sidebar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = false;

    /// <summary>
    /// Gets or sets the message displayed when no bookmarks are available.
    /// </summary>
    [ObservableProperty]
    private string _emptyMessage = "No bookmarks in this document";

    /// <summary>
    /// Gets or sets the currently selected bookmark.
    /// </summary>
    [ObservableProperty]
    private BookmarkNode? _selectedBookmark;

    /// <summary>
    /// Gets a value indicating whether the document has any bookmarks.
    /// </summary>
    public bool HasBookmarks => Bookmarks != null && Bookmarks.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarksViewModel"/> class.
    /// </summary>
    public BookmarksViewModel(
        IBookmarkService bookmarkService,
        ILogger<BookmarksViewModel> logger)
    {
        _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("BookmarksViewModel initialized");
    }

    /// <summary>
    /// Sets the navigation action callback.
    /// </summary>
    public void SetNavigateToPageAction(Func<int, Task> navigateToPageAction)
    {
        _navigateToPageAction = navigateToPageAction ?? throw new ArgumentNullException(nameof(navigateToPageAction));
        _logger.LogDebug("Navigate to page action set");
    }

    /// <summary>
    /// Loads bookmarks from a PDF document.
    /// </summary>
    [RelayCommand]
    private async Task LoadBookmarksAsync(PdfDocument document)
    {
        if (document == null)
        {
            _logger.LogWarning("LoadBookmarksAsync called with null document");
            return;
        }

        _logger.LogInformation("Loading bookmarks from document: {FilePath}", document.FilePath);
        IsLoading = true;

        try
        {
            var result = await _bookmarkService.ExtractBookmarksAsync(document);

            if (result.IsSuccess)
            {
                Bookmarks = result.Value;
                OnPropertyChanged(nameof(HasBookmarks));

                var totalCount = Bookmarks.Sum(b => b.GetTotalNodeCount());
                _logger.LogInformation("Loaded {RootCount} root bookmarks ({TotalCount} total)",
                    Bookmarks.Count, totalCount);

                if (!HasBookmarks)
                {
                    IsPanelVisible = false;
                    _logger.LogInformation("No bookmarks found - panel auto-hidden");
                }
            }
            else
            {
                _logger.LogWarning("Failed to load bookmarks: {Errors}",
                    string.Join(", ", result.Errors));
                Bookmarks = new List<BookmarkNode>();
                OnPropertyChanged(nameof(HasBookmarks));
                IsPanelVisible = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while loading bookmarks");
            Bookmarks = new List<BookmarkNode>();
            OnPropertyChanged(nameof(HasBookmarks));
            IsPanelVisible = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the visibility of the bookmarks panel.
    /// </summary>
    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelVisible = !IsPanelVisible;
        _logger.LogInformation("Bookmarks panel toggled. Visible={Visible}", IsPanelVisible);
    }

    /// <summary>
    /// Navigates to the page specified by a bookmark.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToBookmarkAsync(BookmarkNode bookmark)
    {
        if (bookmark == null)
        {
            _logger.LogWarning("NavigateToBookmarkAsync called with null bookmark");
            return;
        }

        if (bookmark.PageNumber.HasValue)
        {
            _logger.LogInformation("Navigating to bookmark: {Title} (Page {PageNumber})",
                bookmark.Title, bookmark.PageNumber.Value);

            if (_navigateToPageAction != null)
            {
                await _navigateToPageAction(bookmark.PageNumber.Value);
                SelectedBookmark = bookmark;
            }
            else
            {
                _logger.LogWarning("Navigate to page action not set");
            }
        }
        else
        {
            _logger.LogDebug("Bookmark {Title} has no page destination", bookmark.Title);
        }
    }

    partial void OnPanelWidthChanged(double value)
    {
        var clampedWidth = Math.Clamp(value, 150, 600);
        if (Math.Abs(PanelWidth - clampedWidth) > 0.01)
        {
            PanelWidth = clampedWidth;
        }
    }
}
