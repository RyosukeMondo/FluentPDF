using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Represents a single item in the search results list panel.
/// </summary>
public record SearchResultItem(int PageNumber, string Snippet, int MatchIndex, string MatchText);

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

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>Alias for SearchQuery used by the SearchPanel XAML.</summary>
    public string SearchText
    {
        get => SearchQuery;
        set => SearchQuery = value;
    }

    [ObservableProperty]
    private List<SearchMatch> _searchMatches = new();

    [ObservableProperty]
    private int _currentMatchIndex = -1;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _wholeWordOnly;

    /// <summary>Alias for WholeWordOnly used by the SearchPanel XAML.</summary>
    public bool WholeWord
    {
        get => WholeWordOnly;
        set => WholeWordOnly = value;
    }

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    /// <summary>Search results formatted for display in the results list.</summary>
    public ObservableCollection<SearchResultItem> SearchResultItems { get; } = new();

    /// <summary>Summary text like "5 matches across 3 pages".</summary>
    public string SearchResultsSummary
    {
        get
        {
            if (SearchMatches.Count == 0) return string.Empty;
            var pageCount = SearchMatches.Select(m => m.PageNumber).Distinct().Count();
            return $"{SearchMatches.Count} match{(SearchMatches.Count == 1 ? "" : "es")} across {pageCount} page{(pageCount == 1 ? "" : "s")}";
        }
    }

    /// <summary>Gets the search result summary text (e.g. "1 of 5").</summary>
    public string SearchResultSummary => SearchMatches.Count > 0
        ? $"{CurrentMatchIndex + 1} of {SearchMatches.Count}"
        : "No results";

    /// <summary>Alias for SearchResultSummary used by the SearchPanel XAML.</summary>
    public string SearchResultsText => SearchResultSummary;

    public bool HasResults => SearchMatches.Count > 0;

    /// <summary>Alias for HasResults used by the SearchPanel XAML.</summary>
    public bool HasMatches => HasResults;

    public SearchPanelViewModel(
        ITextSearchService searchService,
        ILogger<SearchPanelViewModel> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void SetDocument(PdfDocument? document)
    {
        _currentDocument = document;
        ClearSearch();
    }

    public void SetNavigateToPageAction(Func<int, Task> navigateToPageAction)
    {
        _navigateToPageAction = navigateToPageAction ?? throw new ArgumentNullException(nameof(navigateToPageAction));
    }

    [RelayCommand]
    private void Search()
    {
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

                NotifyAllProperties();
                BuildSearchResultItems();

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

    private void BuildSearchResultItems()
    {
        SearchResultItems.Clear();
        for (int i = 0; i < SearchMatches.Count; i++)
        {
            var match = SearchMatches[i];
            var snippet = BuildSnippet(match.Text, SearchQuery);
            SearchResultItems.Add(new SearchResultItem(
                PageNumber: match.PageNumber + 1,
                Snippet: snippet,
                MatchIndex: i,
                MatchText: match.Text));
        }
    }

    private static string BuildSnippet(string matchText, string query)
    {
        // The match text from PDFium is typically just the matched text itself.
        // Show it with ellipsis context markers.
        const int contextChars = 30;
        if (matchText.Length <= contextChars * 2)
            return matchText;
        return matchText[..contextChars] + "..." + matchText[^contextChars..];
    }

    [RelayCommand(CanExecute = nameof(CanNavigateMatches))]
    private async Task NextMatchAsync()
    {
        if (SearchMatches.Count == 0) return;

        CurrentMatchIndex = (CurrentMatchIndex + 1) % SearchMatches.Count;
        NotifyAllProperties();
        await NavigateToCurrentMatchAsync();
    }

    [RelayCommand(CanExecute = nameof(CanNavigateMatches))]
    private async Task PreviousMatchAsync()
    {
        if (SearchMatches.Count == 0) return;

        CurrentMatchIndex = (CurrentMatchIndex - 1 + SearchMatches.Count) % SearchMatches.Count;
        NotifyAllProperties();
        await NavigateToCurrentMatchAsync();
    }

    /// <summary>
    /// Navigates to a specific match by index.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToMatchAsync(int matchIndex)
    {
        if (matchIndex < 0 || matchIndex >= SearchMatches.Count) return;

        CurrentMatchIndex = matchIndex;
        NotifyAllProperties();
        await NavigateToCurrentMatchAsync();
    }

    [RelayCommand]
    private void Replace()
    {
        // Placeholder — replace functionality not yet implemented
        _logger.LogInformation("Replace requested (not implemented)");
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        // Placeholder — replace all functionality not yet implemented
        _logger.LogInformation("ReplaceAll requested (not implemented)");
    }

    [RelayCommand]
    private void CloseSearch()
    {
        IsVisible = false;
        ClearSearch();
        SearchQuery = string.Empty;
        _searchCts?.Cancel();
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

    [RelayCommand]
    private void ClearSearch()
    {
        SearchMatches = new List<SearchMatch>();
        CurrentMatchIndex = -1;
        SearchResultItems.Clear();
        NotifyAllProperties();
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasMatches));
        OnPropertyChanged(nameof(SearchResultSummary));
        OnPropertyChanged(nameof(SearchResultsText));
        OnPropertyChanged(nameof(SearchResultsSummary));
    }

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(SearchText));
        Search();
    }

    partial void OnCaseSensitiveChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
            Search();
    }

    partial void OnWholeWordOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(WholeWord));
        if (!string.IsNullOrWhiteSpace(SearchQuery))
            Search();
    }
}
