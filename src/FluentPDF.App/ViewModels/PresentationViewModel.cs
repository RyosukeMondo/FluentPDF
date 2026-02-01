using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Windows.Input;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for the presentation mode window.
/// Provides full-screen viewing with minimal UI, auto-hiding controls, and keyboard navigation.
/// </summary>
public partial class PresentationViewModel : ObservableObject
{
    private readonly PdfViewerViewModel _parentViewModel;
    private readonly ILogger<PresentationViewModel> _logger;
    private System.Threading.Timer? _controlsHideTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresentationViewModel"/> class.
    /// </summary>
    /// <param name="parentViewModel">The parent PDF viewer view model providing document data.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public PresentationViewModel(
        PdfViewerViewModel parentViewModel,
        ILogger<PresentationViewModel> logger)
    {
        _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to parent property changes
        _parentViewModel.PropertyChanged += OnParentPropertyChanged;

        // Initialize from parent state
        UpdateFromParent();

        _logger.LogInformation("PresentationViewModel initialized for page {CurrentPageNumber}/{TotalPages}",
            CurrentPageNumber, TotalPages);
    }

    /// <summary>
    /// Gets or sets the current page image displayed in presentation mode.
    /// </summary>
    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.ImageSource? _currentPageImage;

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentPageNumber = 1;

    /// <summary>
    /// Gets or sets the total number of pages in the document.
    /// </summary>
    [ObservableProperty]
    private int _totalPages;

    /// <summary>
    /// Gets or sets a value indicating whether the presentation controls overlay is visible.
    /// Controls auto-hide after 3 seconds of inactivity.
    /// </summary>
    [ObservableProperty]
    private bool _areControlsVisible = true;

    /// <summary>
    /// Gets the page indicator text (e.g., "5 / 42").
    /// </summary>
    public string PageIndicatorText => $"{CurrentPageNumber} / {TotalPages}";

    /// <summary>
    /// Gets a value indicating whether the previous page button should be enabled.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether the next page button should be enabled.
    /// </summary>
    public bool CanGoToNextPage => CurrentPageNumber < TotalPages;

    /// <summary>
    /// Navigates to the previous page in the document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        _logger.LogInformation("Presentation mode: navigating to previous page from {CurrentPage}", CurrentPageNumber);

        await _parentViewModel.GoToPreviousPageCommand.ExecuteAsync(null);

        // Show controls briefly when navigating
        ShowControlsTemporarily();
    }

    /// <summary>
    /// Navigates to the next page in the document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task GoToNextPageAsync()
    {
        _logger.LogInformation("Presentation mode: navigating to next page from {CurrentPage}", CurrentPageNumber);

        await _parentViewModel.GoToNextPageCommand.ExecuteAsync(null);

        // Show controls briefly when navigating
        ShowControlsTemporarily();
    }

    /// <summary>
    /// Exits presentation mode and closes the presentation window.
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        _logger.LogInformation("Exiting presentation mode");

        // The window will handle the actual closure
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event raised when the user requests to exit presentation mode.
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Shows the controls overlay temporarily and starts the auto-hide timer.
    /// </summary>
    public void ShowControlsTemporarily()
    {
        AreControlsVisible = true;

        // Cancel existing timer
        _controlsHideTimer?.Dispose();

        // Start new timer to hide controls after 3 seconds
        _controlsHideTimer = new System.Threading.Timer(
            _ =>
            {
                // Must dispatch to UI thread
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    AreControlsVisible = false;
                });
            },
            null,
            TimeSpan.FromSeconds(3),
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Handles mouse movement to show controls.
    /// </summary>
    public void OnMouseMove()
    {
        if (!AreControlsVisible)
        {
            ShowControlsTemporarily();
        }
    }

    /// <summary>
    /// Updates the presentation view model from the parent view model state.
    /// </summary>
    private void UpdateFromParent()
    {
        CurrentPageImage = _parentViewModel.CurrentPageImage;
        CurrentPageNumber = _parentViewModel.CurrentPageNumber;
        TotalPages = _parentViewModel.TotalPages;

        OnPropertyChanged(nameof(PageIndicatorText));
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Handles property changes from the parent view model.
    /// </summary>
    private void OnParentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageImage))
        {
            CurrentPageImage = _parentViewModel.CurrentPageImage;
        }
        else if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageNumber))
        {
            CurrentPageNumber = _parentViewModel.CurrentPageNumber;
            OnPropertyChanged(nameof(PageIndicatorText));
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
            GoToNextPageCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(PdfViewerViewModel.TotalPages))
        {
            TotalPages = _parentViewModel.TotalPages;
            OnPropertyChanged(nameof(PageIndicatorText));
            GoToNextPageCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Disposing PresentationViewModel");

        // Unsubscribe from parent
        _parentViewModel.PropertyChanged -= OnParentPropertyChanged;

        // Dispose timer
        _controlsHideTimer?.Dispose();
        _controlsHideTimer = null;
    }
}
