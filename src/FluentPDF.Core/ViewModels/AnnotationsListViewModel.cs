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

    public int AnnotationCount => Annotations.Count;

    public AnnotationsListViewModel(
        IAnnotationService annotationService,
        ILogger<AnnotationsListViewModel> logger)
    {
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("AnnotationsListViewModel initialized");
    }

    public void SetNavigateToPageAction(Func<int, Task> action)
    {
        _navigateToPageAction = action ?? throw new ArgumentNullException(nameof(action));
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
        _logger.LogInformation("Loading annotations from document: {FilePath}", document.FilePath);
        IsLoading = true;

        try
        {
            var allAnnotations = new List<AnnotationListItem>();

            for (int page = 0; page < document.PageCount; page++)
            {
                var result = await _annotationService.GetAnnotationsAsync(document, page);
                if (result.IsSuccess)
                {
                    for (int i = 0; i < result.Value.Count; i++)
                    {
                        var ann = result.Value[i];
                        allAnnotations.Add(new AnnotationListItem
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
            }

            Annotations = FilterType == null
                ? allAnnotations
                : allAnnotations.Where(a => a.Type == FilterType).ToList();

            OnPropertyChanged(nameof(HasAnnotations));
            OnPropertyChanged(nameof(AnnotationCount));

            _logger.LogInformation("Loaded {Count} annotations", Annotations.Count);

            if (!HasAnnotations)
            {
                _logger.LogInformation("No annotations found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading annotations");
            Annotations = new List<AnnotationListItem>();
            OnPropertyChanged(nameof(HasAnnotations));
            OnPropertyChanged(nameof(AnnotationCount));
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

        _logger.LogInformation("Navigating to annotation on page {Page}", item.PageNumber);
        SelectedAnnotation = item;
        await _navigateToPageAction(item.PageNumber);
    }

    [RelayCommand]
    private async Task DeleteAnnotationAsync(AnnotationListItem? item)
    {
        if (item == null || _document == null) return;

        _logger.LogInformation("Deleting annotation on page {Page}, index {Index}",
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
                _logger.LogWarning("Failed to delete annotation: {Errors}",
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
        _logger.LogInformation("Annotations panel toggled. Visible={Visible}", IsPanelVisible);
    }

    partial void OnFilterTypeChanged(string? value)
    {
        if (_document != null)
        {
            _ = LoadAnnotationsAsync(_document);
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
}
