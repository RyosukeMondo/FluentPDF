using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentPDF.Core.Models;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Observable properties and property change handlers for PdfViewerViewModel.
/// </summary>
public partial class PdfViewerViewModel
{
    #region Observable Properties

    [ObservableProperty]
    private object? _currentPageImage;

    /// <summary>
    /// Gets or sets the current page number (1-based). Delegates to NavigationViewModel.
    /// </summary>
    public int CurrentPageNumber
    {
        get => Navigation.CurrentPageNumber;
        set => Navigation.CurrentPageNumber = value;
    }

    /// <summary>Gets the current page index (0-based).</summary>
    public int CurrentPageIndex
    {
        get => CurrentPageNumber - 1;
        set => CurrentPageNumber = value + 1;
    }

    /// <summary>Gets or sets the total number of pages. Delegates to NavigationViewModel.</summary>
    public int TotalPages
    {
        get => Navigation.TotalPages;
        set => Navigation.TotalPages = value;
    }

    /// <summary>Gets the page count.</summary>
    public int PageCount => TotalPages;

    /// <summary>Gets or sets the current zoom level. Delegates to ZoomViewModel.</summary>
    public double ZoomLevel
    {
        get => Zoom.ZoomLevel;
        set => Zoom.ZoomLevel = value;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Open a PDF file to get started";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private double _operationProgress = -1.0;

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private string _operationDescription = string.Empty;

    /// <summary>Gets or sets the current page view mode. Delegates to ViewStateViewModel.</summary>
    public PageViewMode ViewMode
    {
        get => ViewState.ViewMode;
        set => ViewState.ViewMode = value;
    }

    public bool IsSinglePageMode => ViewState.IsSinglePageMode;
    public bool IsContinuousScrollMode => ViewState.IsContinuousScrollMode;
    public bool IsTwoPageMode => ViewState.IsTwoPageMode;

    /// <summary>Gets or sets whether the search panel is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsSearchPanelVisible
    {
        get => ViewState.IsSearchPanelVisible;
        set => ViewState.IsSearchPanelVisible = value;
    }

    [ObservableProperty]
    private double _currentPageWidth;

    [ObservableProperty]
    private double _currentPageHeight;

    [ObservableProperty]
    private string _selectedText = string.Empty;

    [ObservableProperty]
    private bool _hasSelectedText;

    /// <summary>
    /// Stores the last text selection with character bounds for annotation placement.
    /// </summary>
    public TextSelection? LastTextSelection { get; set; }

    [ObservableProperty]
    private DisplayInfo? _currentDisplayInfo;

    [ObservableProperty]
    private RenderingQuality _currentRenderingQuality = RenderingQuality.Auto;

    [ObservableProperty]
    private bool _isAdjustingQuality;

    /// <summary>Gets or sets whether the sidebar is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsSidebarVisible
    {
        get => ViewState.IsSidebarVisible;
        set => ViewState.IsSidebarVisible = value;
    }

    /// <summary>Gets or sets whether the bookmarks panel is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsBookmarksPanelVisible
    {
        get => ViewState.IsBookmarksPanelVisible;
        set => ViewState.IsBookmarksPanelVisible = value;
    }

    [ObservableProperty]
    private bool _hasPageModifications;

    [ObservableProperty]
    private bool _isDrawingToolbarVisible;

    [ObservableProperty]
    private DrawingTool _activeDrawingTool = DrawingTool.None;

    [ObservableProperty]
    private string _drawingStrokeColor = "#000000";

    [ObservableProperty]
    private string _drawingFillColor = "#00000000";

    [ObservableProperty]
    private float _drawingStrokeWidth = 2f;

    [ObservableProperty]
    private bool _isOriginalObjectsLocked = true;

    [ObservableProperty]
    private string? _pageSummary;

    [ObservableProperty]
    private bool _isPageSummaryExpanded;

    /// <summary>Whether a drawing tool is currently active.</summary>
    public bool IsDrawingToolActive => ActiveDrawingTool != DrawingTool.None;

    public bool HasUnsavedChanges => HasPageModifications;

    /// <summary>Gets or sets whether the annotations list panel is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsAnnotationsPanelVisible
    {
        get => ViewState.IsAnnotationsPanelVisible;
        set => ViewState.IsAnnotationsPanelVisible = value;
    }

    /// <summary>Gets or sets whether the metadata panel is visible. Delegates to ViewStateViewModel.</summary>
    public bool IsMetadataPanelVisible
    {
        get => ViewState.IsMetadataPanelVisible;
        set => ViewState.IsMetadataPanelVisible = value;
    }

    #endregion

    #region Property Change Handlers

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NavigationViewModel.CurrentPageNumber))
        {
            OnPropertyChanged(nameof(CurrentPageNumber));
            OnPropertyChanged(nameof(CurrentPageIndex));
            UpdateCurrentPageBookmarkState();
        }
        else if (e.PropertyName == nameof(NavigationViewModel.TotalPages))
        {
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageCount));
        }
    }

    private void OnZoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ZoomViewModel.ZoomLevel))
        {
            OnPropertyChanged(nameof(ZoomLevel));
        }
    }

    private void OnViewStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewStateViewModel.IsSidebarVisible):
                OnPropertyChanged(nameof(IsSidebarVisible));
                break;
            case nameof(ViewStateViewModel.IsBookmarksPanelVisible):
                OnPropertyChanged(nameof(IsBookmarksPanelVisible));
                break;
            case nameof(ViewStateViewModel.IsAnnotationsPanelVisible):
                OnPropertyChanged(nameof(IsAnnotationsPanelVisible));
                break;
            case nameof(ViewStateViewModel.IsMetadataPanelVisible):
                OnPropertyChanged(nameof(IsMetadataPanelVisible));
                break;
            case nameof(ViewStateViewModel.IsSearchPanelVisible):
                OnPropertyChanged(nameof(IsSearchPanelVisible));
                break;
            case nameof(ViewStateViewModel.ViewMode):
                OnPropertyChanged(nameof(ViewMode));
                OnPropertyChanged(nameof(IsSinglePageMode));
                OnPropertyChanged(nameof(IsContinuousScrollMode));
                OnPropertyChanged(nameof(IsTwoPageMode));
                break;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsLoading))
        {
            Navigation.IsLoading = IsLoading;
            Zoom.IsLoading = IsLoading;
            NotifyPageCommandsCanExecuteChanged();
        }

        if (e.PropertyName == nameof(HasPageModifications))
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }

        if (e.PropertyName == nameof(ActiveDrawingTool))
        {
            OnPropertyChanged(nameof(IsDrawingToolActive));
        }
    }

    #endregion
}
