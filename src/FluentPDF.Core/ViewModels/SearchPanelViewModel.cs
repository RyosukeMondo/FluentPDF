using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// View model for the search panel.
/// UI-framework agnostic implementation that manages search functionality.
/// </summary>
public partial class SearchPanelViewModel : ViewModelBase
{
    private readonly ITextSearchService _searchService;
    private readonly ILogger<SearchPanelViewModel> _logger;
    private CancellationTokenSource? _searchCts;
    private System.Threading.Timer? _searchDebounceTimer;
    private PdfDocument? _currentDocument;
    private Func<int, Task>? _navigateToPageAction;

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets the list of search matches.
    /// </summary>
    [ObservableProperty]
    private List<SearchMatch> _searchMatches = new();

    /// <summary>
    /// Gets or sets the current match index (0-based).
    /// </summary>
    [ObservableProperty]
    private int _currentMatchIndex = -1;

    /// <summary>
    /// Gets or sets a value indicating whether a search is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isSearching;

    /// <summary>
    /// Gets or sets a value indicating whether the search is case-sensitive.
    /// </summary>
    [ObservableProperty]
    private bool _caseSensitive;

    /// <summary>
    /// Gets or sets a value indicating whether to search for whole words only.
    /// </summary>
    [ObservableProperty]
    private bool _wholeWordOnly;

    /// <summary>
    /// Gets or sets a value indicating whether the search panel is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Gets the search result summary text.
    /// </summary>
    public string SearchResultSummary => SearchMatches.Count > 0
        ? $"{CurrentMatchIndex + 1} of {SearchMatches.Count}"
        : "No results";

    /// <summary>
    /// Gets a value indicating whether there are any search results.
    /// </summary>
    public bool HasResults => SearchMatches.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPanelViewModel"/> class.
    /// </summary>
    public SearchPanelViewModel(
        ITextSearchService searchService,
        ILogger<SearchPanelViewModel> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("SearchPanelViewModel initialized");
    }

    /// <summary>
    /// Sets the current document for searching.
    /// </summary>
    public void SetDocument(PdfDocument? document)
    {
        _currentDocument = document;
        ClearSearch();
    }

    /// <summary>
    /// Sets the navigation action callback.
    /// </summary>
    public void SetNavigateToPageAction(Func<int, Task> navigateToPageAction)
    {
        _navigateToPageAction = navigateToPageAction ?? throw new ArgumentNullException(nameof(navigateToPageAction));
    }

    /// <summary>
    /// Initiates a debounced search.
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        _logger.LogInformation("Search command invoked. Query={Query}", SearchQuery);

        _searchDebounceTimer?.Dispose();
        _searchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(SearchQuery) || _currentDocument == null)
        {
            ClearSearch();
            return;
        }

        _searchDebounceTimer = new System.Threading.Timer(
            async _ => await ExecuteSearchAsync(),
            null,
            TimeSpan.FromMilliseconds(300),
            Timeout.InfiniteTimeSpan);
    }

    private async Task ExecuteSearchAsync()
    {
        if (_currentDocument == null || string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        try
        {
            IsSearching = true;

            var options = new SearchOptions
            {
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWordOnly
            };

            var result = await _searchService.SearchAsync(
                _currentDocument,
                SearchQuery,
                options,
                _searchCts.Token);

            if (result.IsSuccess)
            {
                SearchMatches = result.Value;
                CurrentMatchIndex = SearchMatches.Count > 0 ? 0 : -1;

                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(SearchResultSummary));

                _logger.LogInformation("Search completed. Matches={MatchCount}", SearchMatches.Count);

                if (CurrentMatchIndex >= 0)
                {
                    await NavigateToCurrentMatchAsync();
                }
            }
            else
            {
                _logger.LogError("Search failed: {Errors}", result.Errors);
                ClearSearch();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during search");
            ClearSearch();
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Navigates to the next search match.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateMatches))]
    private async Task NextMatchAsync()
    {
        if (SearchMatches.Count == 0) return;

        CurrentMatchIndex = (CurrentMatchIndex + 1) % SearchMatches.Count;
        OnPropertyChanged(nameof(SearchResultSummary));
        await NavigateToCurrentMatchAsync();
    }

    /// <summary>
    /// Navigates to the previous search match.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigateMatches))]
    private async Task PreviousMatchAsync()
    {
        if (SearchMatches.Count == 0) return;

        CurrentMatchIndex = (CurrentMatchIndex - 1 + SearchMatches.Count) % SearchMatches.Count;
        OnPropertyChanged(nameof(SearchResultSummary));
        await NavigateToCurrentMatchAsync();
    }

    private bool CanNavigateMatches() => SearchMatches.Count > 0 && !IsSearching;

    private async Task NavigateToCurrentMatchAsync()
    {
        if (CurrentMatchIndex < 0 || CurrentMatchIndex >= SearchMatches.Count)
        {
            return;
        }

        var match = SearchMatches[CurrentMatchIndex];
        var targetPage = match.PageNumber + 1; // Convert 0-based to 1-based

        if (_navigateToPageAction != null)
        {
            await _navigateToPageAction(targetPage);
        }
    }

    /// <summary>
    /// Clears the current search.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchMatches.Clear();
        CurrentMatchIndex = -1;
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(SearchResultSummary));
    }

    /// <summary>
    /// Closes the search panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
        ClearSearch();
        SearchQuery = string.Empty;
        _searchCts?.Cancel();
    }

    partial void OnSearchQueryChanged(string value)
    {
        Search();
    }

    partial void OnCaseSensitiveChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            Search();
        }
    }

    partial void OnWholeWordOnlyChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            Search();
        }
    }
}
