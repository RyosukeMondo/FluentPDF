# Tasks Document

## Implementation Tasks

- [x] 1. Extend PdfiumInterop with annotation P/Invoke declarations
  - Files: `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs`, `src/FluentPDF.Rendering/Interop/SafeAnnotationHandle.cs`
  - Requirements: 1.1-1.7
  - Instructions: Add P/Invoke for FPDFPage_CreateAnnot, FPDFAnnot_SetColor, FPDFAnnot_SetRect, FPDFPage_RemoveAnnot, etc. Create SafeAnnotationHandle.

- [x] 2. Create Annotation domain model
  - Files: `src/FluentPDF.Core/Models/Annotation.cs`
  - Requirements: All annotation types
  - Instructions: Define Annotation model with Type, Bounds, Color, Contents, InkPoints. Add AnnotationType enum.

- [x] 3. Implement IAnnotationService and AnnotationService
  - Files: `src/FluentPDF.Core/Services/IAnnotationService.cs`, `src/FluentPDF.Rendering/Services/AnnotationService.cs`, `tests/FluentPDF.Rendering.Tests/Services/AnnotationServiceTests.cs`
  - Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.6, 4.1-4.6, 5.1-5.6
  - Instructions: Implement service with Get, Create, Update, Delete, Save methods. Use PDFium annotation API. Test with sample PDFs.

- [x] 4. Create AnnotationViewModel with tool management
  - Files: `src/FluentPDF.App/ViewModels/AnnotationViewModel.cs`, `tests/FluentPDF.App.Tests/ViewModels/AnnotationViewModelTests.cs`
  - Requirements: 2.1-2.7, 6.1-6.6
  - Instructions: ViewModel with ActiveTool, SelectedColor, Annotations list. Commands for creating, deleting, selecting annotations.

- [x] 5. Create AnnotationLayer canvas overlay control
  - Files: `src/FluentPDF.App/Controls/AnnotationLayer.xaml`, `src/FluentPDF.App/Controls/AnnotationLayer.xaml.cs`
  - Requirements: 2.1-2.7, 4.1-4.6
  - Instructions: Transparent canvas overlay using Win2D. Render annotations on top of PDF. Handle pointer events for drawing, selection.

- [x] 6. Add annotation toolbar to PdfViewerPage
  - Files: `src/FluentPDF.App/Views/PdfViewerPage.xaml`
  - Requirements: 2.1, 3.1, 4.1
  - Instructions: Add annotation toolbar with buttons: Highlight, Underline, Strikethrough, Comment, Rectangle, Circle, Freehand. Add color picker.

- [x] 7. Integrate AnnotationLayer with PDF viewer
  - Files: `src/FluentPDF.App/Views/PdfViewerPage.xaml`, `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
  - Requirements: 2.1-2.7, 6.1-6.6
  - Instructions: Overlay AnnotationLayer on PDF viewer. Pass AnnotationViewModel to layer. Load annotations when page changes.

- [x] 8. Implement annotation save with backup
  - Files: `src/FluentPDF.Rendering/Services/AnnotationService.cs`
  - Requirements: 5.1-5.6
  - Instructions: Save annotations using FPDF_SaveAsCopy. Create .bak backup before overwriting. Restore backup on failure.

- [x] 9. Register AnnotationService in DI
  - Files: `src/FluentPDF.App/App.xaml.cs`
  - Instructions: Register IAnnotationService, AnnotationViewModel in container.

- [x] 10. Integration testing with real PDFium annotations
  - Files: `tests/FluentPDF.Rendering.Tests/Integration/AnnotationIntegrationTests.cs`
  - Requirements: All
  - Instructions: Create annotation, save PDF, reload, verify persistence. Test all annotation types. Test lossless save.

- [x] 11. Final testing and documentation
  - Files: `docs/ARCHITECTURE.md`, `README.md`
  - Requirements: All
  - Instructions: E2E test annotation workflow. Update docs with annotation features.

## Summary

Implements comprehensive annotation tools:
- PDFium annotation API integration with SafeHandle
- Text markup (highlight, underline, strikethrough)
- Text comments and sticky notes
- Drawing tools (rectangle, circle, freehand)
- Annotation save to PDF with lossless quality
- Annotation editing and deletion
- Win2D canvas overlay for rendering
- Comprehensive testing (unit, integration)
