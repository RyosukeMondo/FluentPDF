# Design Document

## Overview

Annotation Tools implements PDF annotation using PDFium's annotation API. Design follows MVVM with AnnotationService for PDFium operations, AnnotationViewModel for tool state, and AnnotationLayer canvas overlay for rendering annotations on top of PDF pages.

## Steering Document Alignment

### Technical Standards (tech.md)
- **WinUI 3 + MVVM**: AnnotationLayer canvas with AnnotationViewModel
- **FluentResults**: Annotation operations return Result<T>
- **PDFium P/Invoke**: Annotation-specific functions
- **SafeHandle**: SafeAnnotationHandle for memory safety

### Project Structure (structure.md)
- `src/FluentPDF.Rendering/Interop/PdfiumAnnotationInterop.cs`
- `src/FluentPDF.Rendering/Services/AnnotationService.cs`
- `src/FluentPDF.Core/Services/IAnnotationService.cs`
- `src/FluentPDF.Core/Models/Annotation.cs`
- `src/FluentPDF.App/ViewModels/AnnotationViewModel.cs`
- `src/FluentPDF.App/Controls/AnnotationLayer.xaml`

## Components

### Annotation Model

```csharp
public class Annotation
{
    public required int AnnotationIndex { get; init; }
    public required AnnotationType Type { get; init; }
    public required RectF Bounds { get; init; }
    public required Color Color { get; init; }
    public string? Contents { get; init; }  // For text annotations
    public float StrokeWidth { get; init; } = 1.0f;
    public List<PointF>? InkPoints { get; init; }  // For freehand
}

public enum AnnotationType
{
    Highlight, Underline, Strikethrough,
    Text, Square, Circle, Ink
}
```

### PDFium Annotation P/Invoke

```csharp
[DllImport("pdfium.dll")]
internal static extern IntPtr FPDFPage_CreateAnnot(SafePdfPageHandle page, int subtype);

[DllImport("pdfium.dll")]
internal static extern int FPDFPage_GetAnnotCount(SafePdfPageHandle page);

[DllImport("pdfium.dll")]
internal static extern IntPtr FPDFPage_GetAnnot(SafePdfPageHandle page, int index);

[DllImport("pdfium.dll")]
internal static extern bool FPDFAnnot_SetColor(IntPtr annot, int type, uint R, uint G, uint B, uint A);

[DllImport("pdfium.dll")]
internal static extern bool FPDFAnnot_SetRect(IntPtr annot, ref RectF rect);

[DllImport("pdfium.dll")]
internal static extern bool FPDFPage_RemoveAnnot(SafePdfPageHandle page, int index);
```

### IAnnotationService

```csharp
public interface IAnnotationService
{
    Task<Result<List<Annotation>>> GetAnnotationsAsync(PdfDocument document, int pageNumber);
    Task<Result<Annotation>> CreateAnnotationAsync(PdfDocument document, int pageNumber, AnnotationType type, RectF bounds, Color color);
    Task<Result> UpdateAnnotationAsync(PdfDocument document, int pageNumber, Annotation annotation);
    Task<Result> DeleteAnnotationAsync(PdfDocument document, int pageNumber, int annotationIndex);
    Task<Result> SaveAnnotationsAsync(PdfDocument document, string outputPath);
}
```

### AnnotationViewModel

```csharp
public partial class AnnotationViewModel : ObservableObject
{
    [ObservableProperty] private AnnotationTool _activeTool = AnnotationTool.None;
    [ObservableProperty] private Color _selectedColor = Colors.Yellow;
    [ObservableProperty] private List<Annotation> _annotations = new();
    [ObservableProperty] private Annotation? _selectedAnnotation;

    [RelayCommand]
    private void SelectTool(AnnotationTool tool)
    {
        ActiveTool = tool;
        _logger.LogInformation("Selected annotation tool: {Tool}", tool);
    }

    [RelayCommand]
    private async Task CreateAnnotationAsync(RectF bounds)
    {
        var result = await _annotationService.CreateAnnotationAsync(
            _currentDocument, _currentPage, ActiveTool.ToAnnotationType(), bounds, SelectedColor);
        if (result.IsSuccess)
        {
            Annotations.Add(result.Value);
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAnnotationAsync()
    {
        if (SelectedAnnotation != null)
        {
            await _annotationService.DeleteAnnotationAsync(_currentDocument, _currentPage, SelectedAnnotation.AnnotationIndex);
            Annotations.Remove(SelectedAnnotation);
            SelectedAnnotation = null;
        }
    }
}
```

### AnnotationLayer Canvas Overlay

- **Purpose**: Transparent canvas overlay on PDF viewer for drawing annotations
- **Rendering**: Uses Win2D for hardware-accelerated annotation rendering
- **Interaction**: Captures pointer events for drawing, selection, dragging

## Testing Strategy

- **AnnotationServiceTests**: Create, update, delete, save annotations (mock PDFium)
- **AnnotationViewModelTests**: Tool selection, annotation creation
- **Integration**: Create annotation, save PDF, reload, verify persistence

## Future Enhancements

- Annotation review workflow
- Flatten annotations (burn into page)
- Annotation export (XML, JSON)
- Collaborative annotations
