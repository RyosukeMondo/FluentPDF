using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace FluentPDF.Avalonia.ViewModels;

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
