# Text Selection and Annotation Tools - Design

## Architecture Overview

```
┌─────────────────────┐
│   PdfViewerPage     │
│   (View)            │
│  - Pointer Events   │
│  - SelectionCanvas  │
└──────────┬──────────┘
           │
           ↓
┌─────────────────────┐
│ PdfViewerViewModel  │
│  - TextSelection    │
│  - Coordinates      │
└──────────┬──────────┘
           │
           ├───────────────────┐
           ↓                   ↓
┌──────────────────┐  ┌──────────────────┐
│ AnnotationVM     │  │ TextExtraction   │
│ - ActiveTool     │  │ Service          │
│ - CreateAnnot    │  │ - GetTextInBounds│
└──────────────────┘  └──────────────────┘
           │                   │
           └─────────┬─────────┘
                     ↓
           ┌──────────────────┐
           │ AnnotationService│
           │ (PDFium API)     │
           └──────────────────┘
```

## Component Design

### 1. Text Selection Enhancement

#### CoordinateMapper Service
```csharp
public interface ICoordinateMapper
{
    /// <summary>
    /// Converts screen coordinates to PDF page coordinates.
    /// </summary>
    PointF ScreenToPdf(Point screenPoint, int pageIndex, double zoomLevel);

    /// <summary>
    /// Converts PDF coordinates to screen coordinates.
    /// </summary>
    Point PdfToScreen(PointF pdfPoint, int pageIndex, double zoomLevel);
}
```

#### TextExtractionService Enhancement
```csharp
public interface ITextExtractionService
{
    // Existing method
    Task<Result<string>> ExtractTextAsync(PdfDocument document, int pageNumber);

    // NEW: Extract text within bounds
    Task<Result<TextSelection>> ExtractTextInBoundsAsync(
        PdfDocument document,
        int pageNumber,
        RectangleF bounds);
}

public class TextSelection
{
    public string Text { get; set; }
    public List<RectangleF> CharacterBounds { get; set; }
    public RectangleF SelectionBounds { get; set; }
}
```

### 2. Annotation Creation Flow

#### State Machine for Annotation Tools
```
[No Tool] ──click tool──> [Tool Active]
    ↑                          │
    │                          │ (cursor changes)
    │                          │
    │                          ↓
    │                    [Text Selected]
    │                          │
    │                          │ (release mouse)
    │                          ↓
    │                    [Create Annotation]
    │                          │
    └──────annotation created──┘
```

#### AnnotationViewModel Enhancement
```csharp
public partial class AnnotationViewModel
{
    // Track if tool should stay active
    [ObservableProperty]
    private bool _toolStaysActive = true;

    // Create annotation from text selection
    public async Task<Result> CreateAnnotationFromSelectionAsync(
        TextSelection selection,
        AnnotationTool tool)
    {
        return tool switch
        {
            AnnotationTool.Highlight =>
                await CreateHighlightAsync(selection),
            AnnotationTool.Underline =>
                await CreateUnderlineAsync(selection),
            AnnotationTool.Strikethrough =>
                await CreateStrikethroughAsync(selection),
            _ => Result.Fail("Invalid tool")
        };
    }
}
```

### 3. PDFium Integration

#### Text Page API Usage
```csharp
// In TextExtractionService.cs
private async Task<Result<TextSelection>> ExtractTextInBoundsAsync(
    IntPtr pageHandle,
    RectangleF bounds)
{
    // 1. Load text page
    var textPageHandle = PdfiumInterop.FPDFText_LoadPage(pageHandle);
    if (textPageHandle == IntPtr.Zero)
        return Result.Fail("Failed to load text page");

    try
    {
        // 2. Get character count
        int charCount = PdfiumInterop.FPDFText_CountChars(textPageHandle);

        // 3. Find characters within bounds
        var charsInBounds = new List<char>();
        var charBounds = new List<RectangleF>();

        for (int i = 0; i < charCount; i++)
        {
            // Get character bounds
            if (PdfiumInterop.FPDFText_GetCharBox(
                textPageHandle, i,
                out double left, out double right,
                out double bottom, out double top))
            {
                var charRect = new RectangleF(
                    (float)left, (float)bottom,
                    (float)(right - left), (float)(top - bottom));

                // Check if character is within selection bounds
                if (bounds.IntersectsWith(charRect))
                {
                    ushort charCode = PdfiumInterop.FPDFText_GetUnicode(textPageHandle, i);
                    charsInBounds.Add((char)charCode);
                    charBounds.Add(charRect);
                }
            }
        }

        return Result.Ok(new TextSelection
        {
            Text = new string(charsInBounds.ToArray()),
            CharacterBounds = charBounds,
            SelectionBounds = bounds
        });
    }
    finally
    {
        PdfiumInterop.FPDFText_ClosePage(textPageHandle);
    }
}
```

### 4. Continuous Scroll Mode Integration

#### PageViewerPage.xaml.cs
```csharp
private async void OnViewModeChanged(object sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(ViewModel.ViewMode))
    {
        switch (ViewModel.ViewMode)
        {
            case PageViewMode.ContinuousScroll:
                // Load document into continuous scroll viewer
                if (ViewModel.CurrentDocument != null)
                {
                    await ContinuousScrollViewerControl.LoadDocumentAsync(
                        ViewModel.CurrentDocument,
                        ViewModel.ZoomLevel);

                    // Subscribe to page change events
                    ContinuousScrollViewerControl.CurrentPageChanged +=
                        OnContinuousScrollPageChanged;
                }
                break;

            case PageViewMode.TwoPage:
                // Similar for two-page mode
                break;

            case PageViewMode.SinglePage:
                // Unsubscribe from continuous scroll events
                ContinuousScrollViewerControl.CurrentPageChanged -=
                    OnContinuousScrollPageChanged;
                break;
        }
    }
}
```

### 5. Visual Feedback

#### Cursor Management
```csharp
// In PdfViewerPage.xaml.cs
private void UpdateCursorForActiveTool()
{
    var tool = ViewModel.AnnotationViewModel?.ActiveTool ?? AnnotationTool.None;

    PdfPageImage.Cursor = tool switch
    {
        AnnotationTool.Highlight => CoreCursorType.Cross,
        AnnotationTool.Underline => CoreCursorType.Cross,
        AnnotationTool.Strikethrough => CoreCursorType.Cross,
        AnnotationTool.Freehand => CoreCursorType.Pen,
        _ => CoreCursorType.Arrow
    };

    // Update status message
    ViewModel.StatusMessage = tool switch
    {
        AnnotationTool.Highlight => "Select text to highlight",
        AnnotationTool.Underline => "Select text to underline",
        AnnotationTool.Strikethrough => "Select text to strikethrough",
        AnnotationTool.Freehand => "Click and drag to draw",
        _ => string.Empty
    };
}
```

## Data Flow

### Annotation Creation Flow
1. User clicks highlight tool button
2. `AnnotationViewModel.SelectToolCommand` sets `ActiveTool = Highlight`
3. `PropertyChanged` event triggers cursor update
4. User clicks and drags on PDF page
5. `OnImagePointerPressed` → `BeginTextSelectionCommand`
6. `OnImagePointerMoved` → `UpdateTextSelectionCommand` (visual rectangle)
7. `OnImagePointerReleased` → `EndTextSelectionCommand`
8. `EndTextSelectionCommand`:
   - Converts screen coords to PDF coords
   - Calls `TextExtractionService.ExtractTextInBoundsAsync`
   - If tool is active, calls `AnnotationViewModel.CreateAnnotationFromSelectionAsync`
9. `CreateAnnotationFromSelectionAsync`:
   - Calls `AnnotationService.CreateHighlightAsync`
   - Adds annotation to `Annotations` collection
   - Marks `HasUnsavedChanges = true`
10. UI updates with new annotation on canvas

## File Changes Required

### New Files
- `src/FluentPDF.Core/Services/ICoordinateMapper.cs`
- `src/FluentPDF.App/Services/CoordinateMapper.cs`
- `src/FluentPDF.Core/Models/TextSelection.cs`

### Modified Files
- `src/FluentPDF.Core/Services/ITextExtractionService.cs` - Add `ExtractTextInBoundsAsync`
- `src/FluentPDF.Rendering/Services/TextExtractionService.cs` - Implement bounds-based extraction
- `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` - Update text selection commands
- `src/FluentPDF.App/ViewModels/AnnotationViewModel.cs` - Add creation methods
- `src/FluentPDF.App/Views/PdfViewerPage.xaml.cs` - Wire up annotation creation
- `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs` - Add text page APIs

### PDFium APIs Needed
```csharp
// Text Page APIs
[DllImport("pdfium.dll")]
public static extern IntPtr FPDFText_LoadPage(IntPtr page);

[DllImport("pdfium.dll")]
public static extern void FPDFText_ClosePage(IntPtr text_page);

[DllImport("pdfium.dll")]
public static extern int FPDFText_CountChars(IntPtr text_page);

[DllImport("pdfium.dll")]
public static extern ushort FPDFText_GetUnicode(IntPtr text_page, int index);

[DllImport("pdfium.dll")]
public static extern bool FPDFText_GetCharBox(
    IntPtr text_page, int index,
    out double left, out double right,
    out double bottom, out double top);
```

## Testing Strategy

### Unit Tests
- `CoordinateMapperTests.cs` - Screen/PDF coordinate conversion
- `TextExtractionServiceTests.cs` - Bounds-based text extraction
- `AnnotationViewModelTests.cs` - Tool selection and annotation creation

### Integration Tests
- Text selection → annotation creation flow
- Continuous scroll mode with annotations
- Multiple annotations on same page

### Manual Testing
1. Open PDF with text
2. Click highlight tool
3. Select text region
4. Verify yellow highlight appears
5. Repeat with underline and strikethrough
6. Switch to continuous scroll mode
7. Verify annotations persist
8. Save and reload document
9. Verify annotations reappear

## Performance Considerations

### Optimization Points
1. **Text extraction caching**: Cache text page handle for current page
2. **Coordinate conversion**: Pre-calculate transformation matrix
3. **Virtual scrolling**: Only render visible pages in continuous mode
4. **Debounce**: Debounce pointer moved events to reduce overhead
5. **Async operations**: All PDFium calls on background thread

### Memory Management
- Dispose text page handles promptly
- Limit cached page renders (max 5 pages)
- Clear selection rectangle after annotation creation
