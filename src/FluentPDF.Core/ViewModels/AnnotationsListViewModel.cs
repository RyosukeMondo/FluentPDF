using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// ViewModel for the annotations list sidebar panel.
/// Displays all annotations across all pages with navigation and filtering.
/// </summary>
public partial class AnnotationsListViewModel : ViewModelBase
{
    private readonly IAnnotationService _annotationService;
    private readonly ILogger<AnnotationsListViewModel> _logger;
    private PdfDocument? _document;
    private Func<int, Task>? _navigateToPageAction;
    private List<AnnotationListItem> _allAnnotations = new();

    [ObservableProperty]
    private List<AnnotationListItem> _annotations = new();

    [ObservableProperty]
    private bool _isPanelVisible;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _emptyMessage = "No annotations in this document";

    [ObservableProperty]
    private AnnotationListItem? _selectedAnnotation;

    [ObservableProperty]
    private string? _filterType;

    public bool HasAnnotations => Annotations.Count > 0;

    /// <summary>
    /// True when the document has any annotations (ignoring filter).
    /// Used to keep the filter row visible even when a filter yields 0 results.
    /// </summary>
    public bool HasAnyAnnotations => _allAnnotations.Count > 0;

    public int AnnotationCount => Annotations.Count;

    public int TotalAnnotationCount => _allAnnotations.Count;

    public AnnotationsListViewModel(
        IAnnotationService annotationService,
        ILogger<AnnotationsListViewModel> logger)
    {
        _annotationService = annotationService
            ?? throw new ArgumentNullException(nameof(annotationService));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("AnnotationsListViewModel initialized");
    }

    public void SetNavigateToPageAction(Func<int, Task> action)
    {
        _navigateToPageAction = action
            ?? throw new ArgumentNullException(nameof(action));
    }

    [RelayCommand]
    private async Task LoadAnnotationsAsync(PdfDocument document)
    {
        if (document == null)
        {
            _logger.LogWarning("LoadAnnotationsAsync called with null document");
            return;
        }

        _document = document;
        _logger.LogInformation(
            "Loading annotations from document: {FilePath}", document.FilePath);
        IsLoading = true;

        try
        {
            var items = new List<AnnotationListItem>();

            for (int page = 0; page < document.PageCount; page++)
            {
                var result = await _annotationService
                    .GetAnnotationsAsync(document, page);
                if (!result.IsSuccess) continue;

                for (int i = 0; i < result.Value.Count; i++)
                {
                    var ann = result.Value[i];
                    items.Add(new AnnotationListItem
                    {
                        PageNumber = page + 1,
                        Type = ann.Type.ToString(),
                        Contents = ann.Contents,
                        Author = ann.Author,
                        CreatedDate = ann.CreatedDate,
                        AnnotationIndexOnPage = i,
                        Bounds = ann.Bounds
                    });
                }
            }

            _allAnnotations = items;
            ApplyFilter();

            _logger.LogInformation(
                "Loaded {Total} annotations, showing {Visible}",
                _allAnnotations.Count, Annotations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading annotations");
            _allAnnotations = new List<AnnotationListItem>();
            Annotations = new List<AnnotationListItem>();
            NotifyCountProperties();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToAnnotationAsync(AnnotationListItem? item)
    {
        if (item == null || _navigateToPageAction == null) return;

        _logger.LogInformation(
            "Navigating to annotation on page {Page}", item.PageNumber);
        SelectedAnnotation = item;
        await _navigateToPageAction(item.PageNumber);
    }

    [RelayCommand]
    private async Task DeleteAnnotationAsync(AnnotationListItem? item)
    {
        if (item == null || _document == null) return;

        _logger.LogInformation(
            "Deleting annotation on page {Page}, index {Index}",
            item.PageNumber, item.AnnotationIndexOnPage);

        try
        {
            var result = await _annotationService.DeleteAnnotationAsync(
                _document, item.PageNumber - 1, item.AnnotationIndexOnPage);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Annotation deleted, reloading list");
                await LoadAnnotationsAsync(_document);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to delete annotation: {Errors}",
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting annotation");
        }
    }

    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelVisible = !IsPanelVisible;
        _logger.LogInformation(
            "Annotations panel toggled. Visible={Visible}", IsPanelVisible);
    }

    partial void OnFilterTypeChanged(string? value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Annotations = string.IsNullOrEmpty(FilterType)
            ? _allAnnotations.ToList()
            : _allAnnotations.Where(a => a.Type == FilterType).ToList();

        NotifyCountProperties();
        UpdateEmptyMessage();
    }

    private void NotifyCountProperties()
    {
        OnPropertyChanged(nameof(HasAnnotations));
        OnPropertyChanged(nameof(HasAnyAnnotations));
        OnPropertyChanged(nameof(AnnotationCount));
        OnPropertyChanged(nameof(TotalAnnotationCount));
    }

    private void UpdateEmptyMessage()
    {
        if (_allAnnotations.Count == 0)
        {
            EmptyMessage = "No annotations in this document";
        }
        else if (!HasAnnotations && !string.IsNullOrEmpty(FilterType))
        {
            EmptyMessage = $"No {FilterType} annotations found";
        }
    }
}

/// <summary>
/// Represents a single annotation item in the annotations list sidebar.
/// </summary>
public class AnnotationListItem
{
    public int PageNumber { get; set; }
    public string Type { get; set; } = "";
    public string Contents { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public int AnnotationIndexOnPage { get; set; }
    public PdfRectangle Bounds { get; set; }

    public string DisplayText => string.IsNullOrEmpty(Contents)
        ? $"[{Type}]"
        : Contents.Length > 80 ? Contents[..80] + "..." : Contents;

    public string PageLabel => $"Page {PageNumber}";

    public string DateLabel => CreatedDate == default
        ? ""
        : CreatedDate.ToString("yyyy-MM-dd HH:mm");

    /// <summary>
    /// Returns a short glyph string representing the annotation type.
    /// Used as a quick visual indicator in the sidebar list.
    /// </summary>
    public string TypeIcon => Type switch
    {
        "Highlight" => "H",
        "Underline" => "U",
        "StrikeOut" => "S",
        "Text" => "T",
        "FreeText" => "Aa",
        "Ink" => "~",
        "Square" => "\u25a1",   // white square
        "Circle" => "\u25cb",   // white circle
        "Line" => "/",
        "Stamp" => "\u2605",    // star
        "Link" => "\u2197",     // north-east arrow
        "Polygon" => "\u2b21",  // hexagon
        "PolyLine" => "\u2f00", // kangxi radical one (line-like)
        "Caret" => "^",
        "Popup" => "\u25ad",    // rect
        "Redact" => "\u2588",   // full block
        _ => "?"
    };
}
