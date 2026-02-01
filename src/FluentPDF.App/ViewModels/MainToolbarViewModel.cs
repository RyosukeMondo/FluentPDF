using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace FluentPDF.App.ViewModels;

public partial class MainToolbarViewModel : ObservableObject
{
    // Navigation properties
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 0;

    [ObservableProperty]
    private bool _canGoPreviousPage;

    [ObservableProperty]
    private bool _canGoNextPage;

    // View toggle properties
    [ObservableProperty]
    private bool _showThumbnails = true;

    [ObservableProperty]
    private bool _showBookmarks;

    [ObservableProperty]
    private bool _showSearch;

    // Zoom properties
    [ObservableProperty]
    private object? _zoomLevel;

    [ObservableProperty]
    private object? _viewMode;

    // Commands
    [RelayCommand]
    private void OpenFile()
    {
        // TODO: Implement file open dialog
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdateNavigationState();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdateNavigationState();
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        // TODO: Implement zoom in
    }

    [RelayCommand]
    private void ZoomOut()
    {
        // TODO: Implement zoom out
    }

    [RelayCommand]
    private void ResetZoom()
    {
        // TODO: Reset zoom to 100%
    }

    [RelayCommand]
    private void ShowMergeDialog()
    {
        // TODO: Show merge dialog
    }

    [RelayCommand]
    private void ShowSplitDialog()
    {
        // TODO: Show split dialog
    }

    [RelayCommand]
    private void ShowOptimizeDialog()
    {
        // TODO: Show optimize dialog
    }

    [RelayCommand]
    private void InsertImage()
    {
        // TODO: Insert image
    }

    [RelayCommand]
    private void ShowWatermarkDialog()
    {
        // TODO: Show watermark dialog
    }

    [RelayCommand]
    private void ShowConversionPage()
    {
        // TODO: Show conversion page
    }

    private void UpdateNavigationState()
    {
        CanGoPreviousPage = CurrentPage > 1;
        CanGoNextPage = CurrentPage < TotalPages;
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentPageChanged(int value)
    {
        UpdateNavigationState();
    }

    partial void OnTotalPagesChanged(int value)
    {
        UpdateNavigationState();
    }
}
