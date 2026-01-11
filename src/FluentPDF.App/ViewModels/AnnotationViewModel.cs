using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.UI;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for PDF annotation tools.
/// Manages annotation tool selection, active annotations, and annotation operations.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class AnnotationViewModel : ObservableObject
{
    private readonly IAnnotationService _annotationService;
    private readonly ILogger<AnnotationViewModel> _logger;
    private PdfDocument? _currentDocument;
    private int _currentPageNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnotationViewModel"/> class.
    /// </summary>
    /// <param name="annotationService">Service for annotation operations.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public AnnotationViewModel(
        IAnnotationService annotationService,
        ILogger<AnnotationViewModel> logger)
    {
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("AnnotationViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the currently active annotation tool.
    /// </summary>
    [ObservableProperty]
    private AnnotationTool _activeTool = AnnotationTool.None;

    /// <summary>
    /// Gets or sets the selected color for new annotations.
    /// </summary>
    [ObservableProperty]
    private Color _selectedColor = Colors.Yellow;

    /// <summary>
    /// Gets or sets the stroke width for drawing annotations.
    /// </summary>
    [ObservableProperty]
    private double _strokeWidth = 2.0;

    /// <summary>
    /// Gets or sets the opacity for new annotations (0.0 to 1.0).
    /// </summary>
    [ObservableProperty]
    private double _opacity = 0.5;

    /// <summary>
    /// Gets the collection of annotations on the current page.
    /// </summary>
    public ObservableCollection<Annotation> Annotations { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected annotation.
    /// </summary>
    [ObservableProperty]
    private Annotation? _selectedAnnotation;

    /// <summary>
    /// Gets or sets a value indicating whether an annotation operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether the annotation toolbar is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isToolbarVisible;

    /// <summary>
    /// Selects an annotation tool for creating new annotations.
    /// </summary>
    /// <param name="tool">The tool to activate.</param>
    [RelayCommand]
    private void SelectTool(AnnotationTool tool)
    {
        _logger.LogInformation("Selected annotation tool: {Tool}", tool);
        ActiveTool = tool;

        // Deselect current annotation when switching tools
        if (SelectedAnnotation != null)
        {
            SelectedAnnotation.IsSelected = false;
            SelectedAnnotation = null;
        }
    }

    /// <summary>
    /// Creates a new annotation at the specified bounds.
    /// </summary>
    /// <param name="bounds">The bounding rectangle for the annotation in PDF coordinates.</param>
    [RelayCommand(CanExecute = nameof(CanCreateAnnotation))]
    private async Task CreateAnnotationAsync(PdfRectangle bounds)
    {
        if (_currentDocument == null || ActiveTool == AnnotationTool.None)
        {
            _logger.LogWarning("Cannot create annotation: document={HasDocument}, tool={Tool}",
                _currentDocument != null, ActiveTool);
            return;
        }

        _logger.LogInformation(
            "Creating annotation. Type={Type}, Page={Page}, Bounds=({Left},{Top},{Right},{Bottom})",
            ActiveTool, _currentPageNumber, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

        try
        {
            IsLoading = true;

            var annotation = new Annotation
            {
                Type = ConvertToolToAnnotationType(ActiveTool),
                PageNumber = _currentPageNumber,
                Bounds = bounds,
                FillColor = System.Drawing.Color.FromArgb(
                    (int)(Opacity * 255),
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B),
                StrokeColor = System.Drawing.Color.FromArgb(
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B),
                Opacity = Opacity,
                StrokeWidth = StrokeWidth
            };

            var result = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);

            if (result.IsSuccess)
            {
                Annotations.Add(result.Value);
                _logger.LogInformation(
                    "Annotation created successfully. Id={Id}, Type={Type}",
                    result.Value.Id, result.Value.Type);

                // Reset to selection tool after creating annotation
                ActiveTool = AnnotationTool.None;
            }
            else
            {
                _logger.LogError("Failed to create annotation: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating annotation");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCreateAnnotation() => _currentDocument != null && ActiveTool != AnnotationTool.None && !IsLoading;

    /// <summary>
    /// Creates a freehand ink annotation with the specified points.
    /// </summary>
    /// <param name="inkPoints">The collection of points defining the freehand path.</param>
    [RelayCommand(CanExecute = nameof(CanCreateInkAnnotation))]
    private async Task CreateInkAnnotationAsync(List<System.Drawing.PointF> inkPoints)
    {
        if (_currentDocument == null || inkPoints == null || inkPoints.Count == 0)
        {
            _logger.LogWarning("Cannot create ink annotation: invalid parameters");
            return;
        }

        _logger.LogInformation(
            "Creating ink annotation. Page={Page}, PointCount={PointCount}",
            _currentPageNumber, inkPoints.Count);

        try
        {
            IsLoading = true;

            // Calculate bounding box from ink points
            var minX = inkPoints.Min(p => p.X);
            var minY = inkPoints.Min(p => p.Y);
            var maxX = inkPoints.Max(p => p.X);
            var maxY = inkPoints.Max(p => p.Y);

            var annotation = new Annotation
            {
                Type = AnnotationType.Ink,
                PageNumber = _currentPageNumber,
                Bounds = new PdfRectangle
                {
                    Left = minX,
                    Top = minY,
                    Right = maxX,
                    Bottom = maxY
                },
                InkPoints = inkPoints,
                StrokeColor = System.Drawing.Color.FromArgb(
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B),
                Opacity = Opacity,
                StrokeWidth = StrokeWidth
            };

            var result = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);

            if (result.IsSuccess)
            {
                Annotations.Add(result.Value);
                _logger.LogInformation(
                    "Ink annotation created successfully. Id={Id}, PointCount={PointCount}",
                    result.Value.Id, result.Value.InkPoints.Count);

                // Reset to selection tool after creating annotation
                ActiveTool = AnnotationTool.None;
            }
            else
            {
                _logger.LogError("Failed to create ink annotation: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating ink annotation");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCreateInkAnnotation() => _currentDocument != null && !IsLoading;

    /// <summary>
    /// Deletes the currently selected annotation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteAnnotation))]
    private async Task DeleteAnnotationAsync()
    {
        if (SelectedAnnotation == null || _currentDocument == null)
        {
            _logger.LogWarning("Cannot delete annotation: no annotation selected");
            return;
        }

        _logger.LogInformation(
            "Deleting annotation. Id={Id}, Type={Type}, Page={Page}",
            SelectedAnnotation.Id, SelectedAnnotation.Type, SelectedAnnotation.PageNumber);

        try
        {
            IsLoading = true;

            // Find the annotation index in the current page's annotations
            var annotationIndex = Annotations.IndexOf(SelectedAnnotation);
            if (annotationIndex < 0)
            {
                _logger.LogError("Cannot find annotation index for deletion");
                return;
            }

            var result = await _annotationService.DeleteAnnotationAsync(
                _currentDocument,
                SelectedAnnotation.PageNumber,
                annotationIndex);

            if (result.IsSuccess)
            {
                Annotations.Remove(SelectedAnnotation);
                _logger.LogInformation("Annotation deleted successfully. Id={Id}", SelectedAnnotation.Id);
                SelectedAnnotation = null;
            }
            else
            {
                _logger.LogError("Failed to delete annotation: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting annotation");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeleteAnnotation() => SelectedAnnotation != null && !IsLoading;

    /// <summary>
    /// Loads annotations for the specified document and page.
    /// </summary>
    /// <param name="parameters">Tuple containing the document and page number.</param>
    [RelayCommand]
    private async Task LoadAnnotationsAsync((PdfDocument document, int pageNumber) parameters)
    {
        _currentDocument = parameters.document;
        _currentPageNumber = parameters.pageNumber;

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot load annotations: no document provided");
            Annotations.Clear();
            return;
        }

        _logger.LogInformation(
            "Loading annotations. Document={FilePath}, Page={Page}",
            _currentDocument.FilePath, _currentPageNumber);

        try
        {
            IsLoading = true;
            Annotations.Clear();

            var result = await _annotationService.GetAnnotationsAsync(_currentDocument, _currentPageNumber);

            if (result.IsSuccess)
            {
                foreach (var annotation in result.Value)
                {
                    Annotations.Add(annotation);
                }

                _logger.LogInformation(
                    "Annotations loaded successfully. Count={Count}, Page={Page}",
                    Annotations.Count, _currentPageNumber);
            }
            else
            {
                _logger.LogError("Failed to load annotations: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading annotations");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Saves all annotations in the current document.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveAnnotations))]
    private async Task SaveAnnotationsAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot save annotations: no document loaded");
            return;
        }

        _logger.LogInformation(
            "Saving annotations. Document={FilePath}, AnnotationCount={Count}",
            _currentDocument.FilePath, Annotations.Count);

        try
        {
            IsLoading = true;

            var result = await _annotationService.SaveAnnotationsAsync(
                _currentDocument,
                _currentDocument.FilePath,
                createBackup: true);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Annotations saved successfully to {FilePath}", _currentDocument.FilePath);
            }
            else
            {
                _logger.LogError("Failed to save annotations: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving annotations");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSaveAnnotations() => _currentDocument != null && !IsLoading;

    /// <summary>
    /// Toggles the visibility of the annotation toolbar.
    /// </summary>
    [RelayCommand]
    private void ToggleToolbar()
    {
        _logger.LogInformation("Toggling annotation toolbar. Current={Current}", IsToolbarVisible);
        IsToolbarVisible = !IsToolbarVisible;

        // Clear tool selection when hiding toolbar
        if (!IsToolbarVisible)
        {
            ActiveTool = AnnotationTool.None;
        }
    }

    /// <summary>
    /// Selects an annotation for editing or deletion.
    /// </summary>
    /// <param name="annotation">The annotation to select.</param>
    [RelayCommand]
    private void SelectAnnotation(Annotation? annotation)
    {
        _logger.LogInformation(
            "Selecting annotation. Id={Id}, Type={Type}",
            annotation?.Id, annotation?.Type);

        // Deselect previous annotation
        if (SelectedAnnotation != null)
        {
            SelectedAnnotation.IsSelected = false;
        }

        SelectedAnnotation = annotation;

        // Mark new annotation as selected
        if (SelectedAnnotation != null)
        {
            SelectedAnnotation.IsSelected = true;
        }

        // Clear active tool when selecting an annotation
        if (SelectedAnnotation != null)
        {
            ActiveTool = AnnotationTool.None;
        }
    }

    /// <summary>
    /// Converts an AnnotationTool to an AnnotationType.
    /// </summary>
    /// <param name="tool">The annotation tool.</param>
    /// <returns>The corresponding annotation type.</returns>
    private static AnnotationType ConvertToolToAnnotationType(AnnotationTool tool)
    {
        return tool switch
        {
            AnnotationTool.Highlight => AnnotationType.Highlight,
            AnnotationTool.Underline => AnnotationType.Underline,
            AnnotationTool.Strikethrough => AnnotationType.StrikeOut,
            AnnotationTool.Comment => AnnotationType.Text,
            AnnotationTool.Rectangle => AnnotationType.Square,
            AnnotationTool.Circle => AnnotationType.Circle,
            AnnotationTool.Freehand => AnnotationType.Ink,
            _ => AnnotationType.Unknown
        };
    }
}

/// <summary>
/// Represents the available annotation tools in the UI.
/// </summary>
public enum AnnotationTool
{
    /// <summary>
    /// No tool selected (selection mode).
    /// </summary>
    None,

    /// <summary>
    /// Highlight text tool.
    /// </summary>
    Highlight,

    /// <summary>
    /// Underline text tool.
    /// </summary>
    Underline,

    /// <summary>
    /// Strikethrough text tool.
    /// </summary>
    Strikethrough,

    /// <summary>
    /// Comment (sticky note) tool.
    /// </summary>
    Comment,

    /// <summary>
    /// Rectangle drawing tool.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Circle drawing tool.
    /// </summary>
    Circle,

    /// <summary>
    /// Freehand drawing tool.
    /// </summary>
    Freehand
}
