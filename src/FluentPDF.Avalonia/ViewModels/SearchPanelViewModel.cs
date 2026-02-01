using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

public partial class SearchPanelViewModel : ObservableObject
{
    private readonly ITextSearchService? _searchService;
    private readonly ITextReplacementService? _replacementService;
    private readonly ILogger<SearchPanelViewModel>? _logger;
    private PdfDocument? _currentDocument;
    private List<SearchMatch> _currentMatches = new();
    private readonly List<TextReplacement> _recentReplacements = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private string _searchResultsText = string.Empty;

    [ObservableProperty]
    private bool _hasMatches;

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _wholeWord;

    [ObservableProperty]
    private bool _previewMode;

    [ObservableProperty]
    private int _currentMatchIndex;

    [ObservableProperty]
    private int _totalMatches;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPanelViewModel"/> class.
    /// </summary>
    /// <param name="searchService">Service for searching text in PDFs.</param>
    /// <param name="replacementService">Service for replacing text in PDFs.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SearchPanelViewModel(
        ITextSearchService? searchService = null,
        ITextReplacementService? replacementService = null,
        ILogger<SearchPanelViewModel>? logger = null)
    {
        _searchService = searchService;
        _replacementService = replacementService;
        _logger = logger;
    }

    /// <summary>
    /// Sets the current document for search operations.
    /// </summary>
    public void SetDocument(PdfDocument? document)
    {
        _currentDocument = document;
        ClearSearch();
    }

    [RelayCommand(CanExecute = nameof(HasMatches))]
    private void PreviousMatch()
    {
        if (CurrentMatchIndex > 0)
        {
            CurrentMatchIndex--;
            UpdateSearchResultsText();
        }
    }

    [RelayCommand(CanExecute = nameof(HasMatches))]
    private void NextMatch()
    {
        if (CurrentMatchIndex < TotalMatches - 1)
        {
            CurrentMatchIndex++;
            UpdateSearchResultsText();
        }
    }

    [RelayCommand]
    private void CloseSearch()
    {
        // TODO: Signal to parent to hide search panel
    }

    partial void OnSearchTextChanged(string value)
    {
        // TODO: Trigger search
        PerformSearch();
    }

    partial void OnCaseSensitiveChanged(bool value)
    {
        // Re-run search with new settings
        PerformSearch();
    }

    partial void OnWholeWordChanged(bool value)
    {
        // Re-run search with new settings
        PerformSearch();
    }

    private async void PerformSearch()
    {
        if (_currentDocument == null || _searchService == null)
        {
            ClearSearch();
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ClearSearch();
            return;
        }

        try
        {
            var options = new SearchOptions
            {
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWord
            };

            var result = await _searchService.SearchAsync(_currentDocument, SearchText, options);

            if (result.IsSuccess)
            {
                _currentMatches = result.Value;
                TotalMatches = _currentMatches.Count;
                CurrentMatchIndex = _currentMatches.Count > 0 ? 0 : -1;

                _logger?.LogInformation(
                    "Search completed. Query={Query}, MatchCount={MatchCount}",
                    SearchText, TotalMatches);
            }
            else
            {
                _logger?.LogWarning(
                    "Search failed. Query={Query}, Error={Error}",
                    SearchText, result.Errors.FirstOrDefault()?.Message);

                ClearSearch();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception during search. Query={Query}", SearchText);
            ClearSearch();
        }

        PreviousMatchCommand.NotifyCanExecuteChanged();
        NextMatchCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        ReplaceAllCommand.NotifyCanExecuteChanged();
    }

    private void ClearSearch()
    {
        _currentMatches.Clear();
        HasMatches = false;
        TotalMatches = 0;
        CurrentMatchIndex = -1;
        SearchResultsText = string.Empty;
    }

    private void UpdateSearchResultsText()
    {
        if (TotalMatches > 0)
        {
            SearchResultsText = $"{CurrentMatchIndex + 1} of {TotalMatches}";
        }
        else if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResultsText = "No matches found";
        }
        else
        {
            SearchResultsText = string.Empty;
        }
    }

    partial void OnCurrentMatchIndexChanged(int value)
    {
        UpdateSearchResultsText();
    }

    partial void OnTotalMatchesChanged(int value)
    {
        HasMatches = value > 0;
        UpdateSearchResultsText();
        PreviousMatchCommand.NotifyCanExecuteChanged();
        NextMatchCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        ReplaceAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanReplace))]
    private async Task ReplaceAsync()
    {
        if (_currentDocument == null || _replacementService == null || !CanReplace())
        {
            return;
        }

        try
        {
            var currentMatch = _currentMatches[CurrentMatchIndex];

            _logger?.LogInformation(
                "Replacing text. PageNumber={PageNumber}, OriginalText={OriginalText}, ReplacementText={ReplacementText}, Preview={Preview}",
                currentMatch.PageNumber, currentMatch.Text, ReplaceText, PreviewMode);

            var result = await _replacementService.ReplaceAsync(
                _currentDocument,
                currentMatch,
                ReplaceText,
                PreviewMode);

            if (result.IsSuccess)
            {
                if (!PreviewMode)
                {
                    _recentReplacements.Add(result.Value);

                    // Remove the replaced match from the list and adjust index
                    _currentMatches.RemoveAt(CurrentMatchIndex);
                    TotalMatches = _currentMatches.Count;

                    if (CurrentMatchIndex >= TotalMatches && TotalMatches > 0)
                    {
                        CurrentMatchIndex = TotalMatches - 1;
                    }
                    else if (TotalMatches == 0)
                    {
                        CurrentMatchIndex = -1;
                    }

                    _logger?.LogInformation("Text replaced successfully.");
                }
                else
                {
                    _logger?.LogInformation("Preview mode - no changes applied.");
                }
            }
            else
            {
                _logger?.LogWarning(
                    "Replace failed. Error={Error}",
                    result.Errors.FirstOrDefault()?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception during replace.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanReplace))]
    private async Task ReplaceAllAsync()
    {
        if (_currentDocument == null || _replacementService == null || !CanReplace())
        {
            return;
        }

        try
        {
            _logger?.LogInformation(
                "Replacing all text. FindText={FindText}, ReplaceText={ReplaceText}, MatchCount={MatchCount}, Preview={Preview}",
                SearchText, ReplaceText, TotalMatches, PreviewMode);

            var options = new SearchOptions
            {
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWord
            };

            var result = await _replacementService.ReplaceAllAsync(
                _currentDocument,
                SearchText,
                ReplaceText,
                options,
                PreviewMode);

            if (result.IsSuccess)
            {
                if (!PreviewMode)
                {
                    _recentReplacements.AddRange(result.Value);

                    _logger?.LogInformation(
                        "Replace all completed. ReplacementCount={ReplacementCount}",
                        result.Value.Count);

                    // Clear the search results since all matches were replaced
                    ClearSearch();
                }
                else
                {
                    _logger?.LogInformation(
                        "Preview mode - {PreviewCount} replacements previewed.",
                        result.Value.Count);
                }
            }
            else
            {
                _logger?.LogWarning(
                    "Replace all failed. Error={Error}",
                    result.Errors.FirstOrDefault()?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception during replace all.");
        }
    }

    private bool CanReplace()
    {
        return HasMatches && !string.IsNullOrWhiteSpace(ReplaceText);
    }

    /// <summary>
    /// Gets the current match for navigation purposes.
    /// </summary>
    public SearchMatch? GetCurrentMatch()
    {
        if (CurrentMatchIndex >= 0 && CurrentMatchIndex < _currentMatches.Count)
        {
            return _currentMatches[CurrentMatchIndex];
        }

        return null;
    }

    /// <summary>
    /// Gets all recent replacements for undo support.
    /// </summary>
    public List<TextReplacement> GetRecentReplacements()
    {
        return new List<TextReplacement>(_recentReplacements);
    }

    /// <summary>
    /// Clears recent replacement history.
    /// </summary>
    public void ClearReplacementHistory()
    {
        _recentReplacements.Clear();
    }
}
