using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// ViewModel for managing UI view state (panels, sidebars, view modes).
/// Manages visibility toggles and view mode switching.
/// </summary>
public partial class ViewStateViewModel : ViewModelBase
{
    private readonly ILogger<ViewStateViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewStateViewModel"/> class.
    /// </summary>
    public ViewStateViewModel(ILogger<ViewStateViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ViewStateViewModel initialized");
    }

    /// <summary>
    /// Gets or sets a value indicating whether the thumbnails sidebar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSidebarVisible = true;

    /// <summary>
    /// Gets or sets a value indicating whether the bookmarks panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isBookmarksPanelVisible = true;

    /// <summary>
    /// Gets or sets a value indicating whether the annotations list panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isAnnotationsPanelVisible;

    /// <summary>
    /// Gets or sets a value indicating whether the search panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSearchPanelVisible;

    /// <summary>
    /// Gets or sets the current page view mode.
    /// </summary>
    [ObservableProperty]
    private PageViewMode _viewMode = PageViewMode.SinglePage;

    /// <summary>Gets a value indicating whether the viewer is in single page mode.</summary>
    public bool IsSinglePageMode => ViewMode == PageViewMode.SinglePage;

    /// <summary>Gets a value indicating whether the viewer is in continuous scroll mode.</summary>
    public bool IsContinuousScrollMode => ViewMode == PageViewMode.ContinuousScroll;

    /// <summary>Gets a value indicating whether the viewer is in two-page mode.</summary>
    public bool IsTwoPageMode => ViewMode == PageViewMode.TwoPage;

    /// <summary>
    /// Toggles the visibility of the thumbnails sidebar.
    /// </summary>
    [RelayCommand]
    private void ToggleThumbnails()
    {
        _logger.LogInformation("ToggleThumbnails command invoked");
        IsSidebarVisible = !IsSidebarVisible;
        _logger.LogInformation("Thumbnails visibility toggled to: {IsVisible}", IsSidebarVisible);
        RaiseAccessibilityNotification(
            IsSidebarVisible ? "Thumbnails sidebar shown" : "Thumbnails sidebar hidden");
    }

    /// <summary>
    /// Toggles the visibility of the bookmarks panel.
    /// </summary>
    [RelayCommand]
    private void ToggleBookmarks()
    {
        _logger.LogInformation("ToggleBookmarks command invoked");
        IsBookmarksPanelVisible = !IsBookmarksPanelVisible;
        _logger.LogInformation("Bookmarks visibility toggled to: {IsVisible}", IsBookmarksPanelVisible);
        RaiseAccessibilityNotification(
            IsBookmarksPanelVisible ? "Bookmarks panel shown" : "Bookmarks panel hidden");
    }

    /// <summary>
    /// Toggles the visibility of the annotations list panel.
    /// </summary>
    [RelayCommand]
    private void ToggleAnnotations()
    {
        _logger.LogInformation("ToggleAnnotations command invoked");
        IsAnnotationsPanelVisible = !IsAnnotationsPanelVisible;
        _logger.LogInformation("Annotations visibility toggled to: {IsVisible}", IsAnnotationsPanelVisible);
        RaiseAccessibilityNotification(
            IsAnnotationsPanelVisible ? "Annotations panel shown" : "Annotations panel hidden");
    }

    /// <summary>
    /// Shows the search panel.
    /// </summary>
    [RelayCommand]
    private void ShowSearch()
    {
        _logger.LogInformation("ShowSearch command invoked");
        IsSearchPanelVisible = true;
    }

    /// <summary>
    /// Toggles the search panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleSearchPanel()
    {
        _logger.LogInformation("ToggleSearchPanel command invoked");
        IsSearchPanelVisible = !IsSearchPanelVisible;
    }

    /// <summary>
    /// Toggles the view mode between single page, continuous scroll, and two-page.
    /// </summary>
    [RelayCommand]
    private void ToggleViewMode()
    {
        ViewMode = ViewMode switch
        {
            PageViewMode.SinglePage => PageViewMode.ContinuousScroll,
            PageViewMode.ContinuousScroll => PageViewMode.TwoPage,
            PageViewMode.TwoPage => PageViewMode.SinglePage,
            _ => PageViewMode.SinglePage
        };

        OnPropertyChanged(nameof(IsSinglePageMode));
        OnPropertyChanged(nameof(IsContinuousScrollMode));
        OnPropertyChanged(nameof(IsTwoPageMode));

        _logger.LogInformation("View mode changed to {ViewMode}", ViewMode);
    }

    private void RaiseAccessibilityNotification(string message)
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new AccessibilityNotificationMessage(message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to raise accessibility notification: {Message}", message);
        }
    }
}
