using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// View model for the bookmarks panel.
/// Manages bookmark loading, panel visibility, navigation, and state persistence.
/// </summary>
public partial class BookmarksViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarkService;
    private readonly PdfViewerViewModel _pdfViewerViewModel;
    private readonly ILogger<BookmarksViewModel> _logger;

    /// <summary>
    /// Gets or sets the list of root-level bookmarks for the current document.
    /// Null when no document is loaded.
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
    /// Constrained to 150-600px range.
    /// </summary>
    [ObservableProperty]
    private double _panelWidth = 250;

    /// <summary>
    /// Gets or sets a value indicating whether bookmarks are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the message displayed when the document has no bookmarks.
    /// </summary>
    [ObservableProperty]
    private string _emptyMessage = "No bookmarks in this document";

    /// <summary>
    /// Gets or sets the currently selected bookmark.
    /// Null when no bookmark is selected.
    /// </summary>
    [ObservableProperty]
    private BookmarkNode? _selectedBookmark;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarksViewModel"/> class.
    /// </summary>
    /// <param name="bookmarkService">Service for extracting bookmarks from PDF documents.</param>
    /// <param name="pdfViewerViewModel">The PDF viewer view model for navigation.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public BookmarksViewModel(
        IBookmarkService bookmarkService,
        PdfViewerViewModel pdfViewerViewModel,
        ILogger<BookmarksViewModel> logger)
    {
        _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
        _pdfViewerViewModel = pdfViewerViewModel ?? throw new ArgumentNullException(nameof(pdfViewerViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadPanelState();
        _logger.LogInformation("BookmarksViewModel initialized. PanelVisible={PanelVisible}, PanelWidth={PanelWidth}",
            IsPanelVisible, PanelWidth);
    }

    /// <summary>
    /// Loads bookmarks from a PDF document asynchronously.
    /// </summary>
    /// <param name="document">The PDF document to extract bookmarks from.</param>
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
                var totalCount = Bookmarks.Sum(b => b.GetTotalNodeCount());
                _logger.LogInformation("Loaded {RootCount} root bookmarks ({TotalCount} total) from {FilePath}",
                    Bookmarks.Count,
                    totalCount,
                    document.FilePath);
            }
            else
            {
                _logger.LogWarning("Failed to load bookmarks from {FilePath}: {Errors}",
                    document.FilePath,
                    string.Join(", ", result.Errors));
                Bookmarks = new List<BookmarkNode>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while loading bookmarks from {FilePath}", document.FilePath);
            Bookmarks = new List<BookmarkNode>();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the visibility of the bookmarks panel and saves the state.
    /// </summary>
    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelVisible = !IsPanelVisible;
        _logger.LogInformation("Bookmarks panel toggled. Visible={Visible}", IsPanelVisible);
        SavePanelState();
    }

    /// <summary>
    /// Navigates to the page specified by a bookmark.
    /// </summary>
    /// <param name="bookmark">The bookmark to navigate to.</param>
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
                bookmark.Title,
                bookmark.PageNumber.Value);

            await _pdfViewerViewModel.GoToPageCommand.ExecuteAsync(bookmark.PageNumber.Value);
            SelectedBookmark = bookmark;
        }
        else
        {
            _logger.LogDebug("Bookmark {Title} has no page destination", bookmark.Title);
        }
    }

    /// <summary>
    /// Saves the current panel state (visibility and width) to application settings.
    /// </summary>
    private void SavePanelState()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["BookmarksPanelVisible"] = IsPanelVisible;
            settings.Values["BookmarksPanelWidth"] = PanelWidth;
            _logger.LogDebug("Panel state saved. Visible={Visible}, Width={Width}", IsPanelVisible, PanelWidth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save panel state");
        }
    }

    /// <summary>
    /// Loads the panel state (visibility and width) from application settings.
    /// </summary>
    private void LoadPanelState()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;

            if (settings.Values.TryGetValue("BookmarksPanelVisible", out var visible))
            {
                IsPanelVisible = (bool)visible;
            }

            if (settings.Values.TryGetValue("BookmarksPanelWidth", out var width))
            {
                var w = Convert.ToDouble(width);
                PanelWidth = Math.Clamp(w, 150, 600);
            }

            _logger.LogDebug("Panel state loaded. Visible={Visible}, Width={Width}", IsPanelVisible, PanelWidth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load panel state, using defaults");
        }
    }

    /// <summary>
    /// Called when a property value changes.
    /// Saves panel state when width changes.
    /// </summary>
    /// <param name="e">The property changed event arguments.</param>
    partial void OnPanelWidthChanged(double value)
    {
        // Validate and clamp the width
        var clampedWidth = Math.Clamp(value, 150, 600);
        if (Math.Abs(PanelWidth - clampedWidth) > 0.01)
        {
            PanelWidth = clampedWidth;
        }
        SavePanelState();
    }
}
