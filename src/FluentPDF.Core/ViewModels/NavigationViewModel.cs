using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// View model for PDF page navigation.
/// Manages page navigation commands and state.
/// </summary>
public partial class NavigationViewModel : ViewModelBase
{
    private readonly IAnimationService? _animationService;
    private readonly ILogger<NavigationViewModel> _logger;
    private CancellationTokenSource? _navigationAnimationCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewModel"/> class.
    /// </summary>
    public NavigationViewModel(
        ILogger<NavigationViewModel> logger,
        IAnimationService? animationService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _animationService = animationService;

        _logger.LogInformation("NavigationViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentPageNumber = 1;

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [ObservableProperty]
    private int _totalPages;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether a document is loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasDocument;

    /// <summary>
    /// Callback invoked when page navigation occurs.
    /// </summary>
    public Func<int, Task>? OnPageChanged { get; set; }

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        _logger.LogInformation("GoToPreviousPage command invoked. CurrentPage={CurrentPage}", CurrentPageNumber);

        _navigationAnimationCts?.Cancel();
        _navigationAnimationCts?.Dispose();
        _navigationAnimationCts = new CancellationTokenSource();

        CurrentPageNumber--;
        await AnimatePageTransitionAsync(PageTransitionDirection.Backward, _navigationAnimationCts.Token);
        await NotifyPageChangedAsync();
    }

    private bool CanGoToPreviousPage() => CurrentPageNumber > 1 && !IsLoading && HasDocument;

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task GoToNextPageAsync()
    {
        _logger.LogInformation("GoToNextPage command invoked. CurrentPage={CurrentPage}", CurrentPageNumber);

        _navigationAnimationCts?.Cancel();
        _navigationAnimationCts?.Dispose();
        _navigationAnimationCts = new CancellationTokenSource();

        CurrentPageNumber++;
        await AnimatePageTransitionAsync(PageTransitionDirection.Forward, _navigationAnimationCts.Token);
        await NotifyPageChangedAsync();
    }

    private bool CanGoToNextPage() => CurrentPageNumber < TotalPages && !IsLoading && HasDocument;

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    [RelayCommand]
    private async Task GoToPageAsync(int pageNumber)
    {
        _logger.LogInformation("GoToPage command invoked. PageNumber={PageNumber}", pageNumber);

        if (pageNumber >= 1 && pageNumber <= TotalPages && !IsLoading && HasDocument)
        {
            _navigationAnimationCts?.Cancel();
            _navigationAnimationCts?.Dispose();
            _navigationAnimationCts = new CancellationTokenSource();

            CurrentPageNumber = pageNumber;
            await AnimatePageTransitionAsync(PageTransitionDirection.Jump, _navigationAnimationCts.Token);
            await NotifyPageChangedAsync();
        }
    }

    /// <summary>
    /// Navigates to the first page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
    private async Task FirstPageAsync()
    {
        _logger.LogInformation("FirstPage command invoked");
        CurrentPageNumber = 1;
        await NotifyPageChangedAsync();
    }

    private bool CanGoToFirstPage() => CurrentPageNumber > 1 && !IsLoading && HasDocument;

    /// <summary>
    /// Navigates to the last page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
    private async Task LastPageAsync()
    {
        _logger.LogInformation("LastPage command invoked");
        CurrentPageNumber = TotalPages;
        await NotifyPageChangedAsync();
    }

    private bool CanGoToLastPage() => CurrentPageNumber < TotalPages && !IsLoading && HasDocument;

    private async Task AnimatePageTransitionAsync(
        PageTransitionDirection direction,
        CancellationToken cancellationToken)
    {
        if (_animationService == null)
        {
            return;
        }

        try
        {
            await _animationService.AnimatePageTransitionAsync(null, direction, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Page transition animation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Page transition animation failed");
        }
    }

    private async Task NotifyPageChangedAsync()
    {
        if (OnPageChanged != null)
        {
            await OnPageChanged(CurrentPageNumber);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsLoading) ||
            e.PropertyName == nameof(CurrentPageNumber) ||
            e.PropertyName == nameof(TotalPages) ||
            e.PropertyName == nameof(HasDocument))
        {
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
            GoToNextPageCommand.NotifyCanExecuteChanged();
            FirstPageCommand.NotifyCanExecuteChanged();
            LastPageCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        _navigationAnimationCts?.Cancel();
        _navigationAnimationCts?.Dispose();
        _navigationAnimationCts = null;
    }
}
