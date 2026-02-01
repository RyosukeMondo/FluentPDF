using CommunityToolkit.Mvvm.ComponentModel;
using FluentPDF.Core.ViewModels;

namespace FluentPDF.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _showAnnotationToolbar;

    [ObservableProperty]
    private bool _showSearchPanel;

    [ObservableProperty]
    private bool _showThumbnails = true;

    [ObservableProperty]
    private bool _showBookmarks;

    [ObservableProperty]
    private string _documentTitle = "FluentPDF";

    [ObservableProperty]
    private string _currentTheme = "Default"; // TODO: Replace with Avalonia theme enum

    // Child ViewModels
    [ObservableProperty]
    private MainToolbarViewModel? _toolbarViewModel;

    [ObservableProperty]
    private AnnotationToolbarViewModel? _annotationToolbarViewModel;

    [ObservableProperty]
    private SearchPanelViewModel? _searchPanelViewModel;

    [ObservableProperty]
    private ThumbnailsViewModel? _thumbnailsViewModel;

    [ObservableProperty]
    private BookmarksViewModel? _bookmarksViewModel;

    [ObservableProperty]
    private PdfViewerViewModel? _pdfContentViewModel;

    public MainWindowViewModel()
    {
        // Initialize child ViewModels
        ToolbarViewModel = new MainToolbarViewModel();
        AnnotationToolbarViewModel = new AnnotationToolbarViewModel();
        SearchPanelViewModel = new SearchPanelViewModel();
        ThumbnailsViewModel = App.GetService<ThumbnailsViewModel>();
        BookmarksViewModel = App.GetService<BookmarksViewModel>();
        PdfContentViewModel = App.GetService<PdfViewerViewModel>();

        // Wire up visibility bindings from toolbar
        ToolbarViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainToolbarViewModel.ShowThumbnails))
                ShowThumbnails = ToolbarViewModel.ShowThumbnails;
            else if (e.PropertyName == nameof(MainToolbarViewModel.ShowBookmarks))
                ShowBookmarks = ToolbarViewModel.ShowBookmarks;
            else if (e.PropertyName == nameof(MainToolbarViewModel.ShowSearch))
                ShowSearchPanel = ToolbarViewModel.ShowSearch;
        };
    }
}
