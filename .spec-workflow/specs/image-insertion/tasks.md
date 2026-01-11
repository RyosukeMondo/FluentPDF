# Tasks Document

## Implementation Tasks

- [x] 1. Extend PdfiumInterop with image object P/Invoke
  - File: `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs`
  - Add P/Invoke for FPDFPageObj_NewImageObj, FPDFImageObj_LoadJpegFile, FPDFImageObj_SetBitmap
  - Add FPDFPageObj_Transform, FPDFPageObj_SetMatrix, FPDFPage_InsertObject
  - Purpose: Enable PDFium image object manipulation
  - _Leverage: Existing PdfiumInterop.cs patterns_
  - _Requirements: 1.1-1.7, 6.1-6.5_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# P/Invoke Developer with PDFium expertise | Task: Add P/Invoke declarations for image object APIs: FPDFPageObj_NewImageObj, FPDFImageObj_LoadJpegFile, FPDFImageObj_LoadJpegFileInline, FPDFImageObj_SetBitmap, FPDFPageObj_Transform, FPDFPageObj_SetMatrix, FPDFPage_InsertObject, FPDFPage_RemoveObject, FPDFPageObj_GetBounds. Follow existing CallingConvention.Cdecl pattern. | Restrictions: Only add P/Invoke declarations, do not implement wrapper logic. Follow existing naming conventions. | _Leverage: Existing PdfiumInterop.cs | _Requirements: 1.1-1.7, 6.1-6.5 | Success: All P/Invoke declarations compile, follow existing patterns. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 2. Create IImageInsertionService interface
  - File: `src/FluentPDF.Core/Services/IImageInsertionService.cs`
  - Define InsertImageAsync, MoveImageAsync, ScaleImageAsync, RotateImageAsync, DeleteImageAsync
  - Add ImageObject model class
  - Purpose: Establish service contract for image operations
  - _Leverage: `src/FluentPDF.Core/Services/IAnnotationService.cs` pattern_
  - _Requirements: 1.1-1.7, 2.1-2.6, 3.1-3.6, 4.1-4.5, 5.1-5.5_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Software Architect | Task: Create IImageInsertionService with methods: InsertImageAsync(PdfDocument, int pageIndex, string imagePath, PointF position) returning Result<ImageObject>, MoveImageAsync(ImageObject, PointF), ScaleImageAsync(ImageObject, SizeF), RotateImageAsync(ImageObject, float degrees), DeleteImageAsync(ImageObject). Create ImageObject class with PageIndex, Position, Size, RotationDegrees, SourcePath properties. | Restrictions: Interface only, no implementation. Follow FluentResults pattern. | _Leverage: IAnnotationService pattern | _Requirements: 1.1-1.7, 2.1-2.6, 3.1-3.6, 4.1-4.5, 5.1-5.5 | Success: Interface and model compile, proper Result pattern usage. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 3. Implement ImageInsertionService
  - File: `src/FluentPDF.Rendering/Services/ImageInsertionService.cs`
  - Implement all IImageInsertionService methods using PDFium
  - Handle PNG, JPEG, BMP, GIF formats
  - Purpose: Provide image embedding functionality
  - _Leverage: PdfiumInterop image APIs, `src/FluentPDF.Rendering/Services/AnnotationService.cs` patterns_
  - _Requirements: 1.1-1.7, 2.1-2.6, 3.1-3.6, 4.1-4.5, 5.1-5.5, 6.1-6.5_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer with PDFium and image processing expertise | Task: Implement ImageInsertionService. InsertImageAsync loads image, creates FPDFPageObj_NewImageObj, sets bitmap, positions with FPDFPageObj_Transform, adds to page with FPDFPage_InsertObject. MoveImageAsync updates transform matrix. ScaleImageAsync updates size in matrix. RotateImageAsync applies rotation. DeleteImageAsync calls FPDFPage_RemoveObject. Handle PNG alpha, JPEG quality, BMP conversion. | Restrictions: Use existing PdfiumInterop, handle all supported formats, proper error handling. | _Leverage: PdfiumInterop, AnnotationService patterns | _Requirements: 1.1-1.7, 2.1-2.6, 3.1-3.6, 4.1-4.5, 5.1-5.5, 6.1-6.5 | Success: All operations work, formats supported, proper error handling. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 4. Create ImageInsertionViewModel
  - File: `src/FluentPDF.App/ViewModels/ImageInsertionViewModel.cs`
  - Add InsertImageCommand with file picker
  - Add DeleteSelectedImageCommand
  - Add SelectedImage and InsertedImages properties
  - Purpose: Manage image insertion state and UI interaction
  - _Leverage: `src/FluentPDF.App/ViewModels/AnnotationViewModel.cs` pattern_
  - _Requirements: 1.1-1.7, 5.1-5.5_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# WinUI 3 MVVM Developer | Task: Create ImageInsertionViewModel with: InsertImageCommand that shows FileOpenPicker with image filters then calls service, DeleteSelectedImageCommand, SelectedImage property with OnSelectedImageChanged, InsertedImages ObservableCollection. Inject IImageInsertionService. Notify PdfViewerViewModel on changes. | Restrictions: Follow AnnotationViewModel patterns. Use proper async patterns. | _Leverage: AnnotationViewModel pattern | _Requirements: 1.1-1.7, 5.1-5.5 | Success: Commands work, state properly managed, file picker functional. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 5. Create ImageManipulationOverlay control
  - Files: `src/FluentPDF.App/Controls/ImageManipulationOverlay.xaml`, `src/FluentPDF.App/Controls/ImageManipulationOverlay.xaml.cs`
  - Render selection handles for selected image
  - Handle pointer events for move, scale, rotate
  - Purpose: Provide visual image manipulation interface
  - _Leverage: `src/FluentPDF.App/Controls/AnnotationLayer.xaml` pattern_
  - _Requirements: 2.1-2.6, 3.1-3.6, 4.1-4.5_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Control Developer | Task: Create Canvas-based ImageManipulationOverlay. Draw selection rectangle and 8 resize handles when image selected. Draw rotation handle above selection. Handle PointerPressed/Moved/Released for drag (move), corner drag (scale), rotation handle drag (rotate). Transform coordinates from screen to PDF points. Bind to ImageInsertionViewModel. | Restrictions: Follow AnnotationLayer patterns. Use Canvas for rendering. | _Leverage: AnnotationLayer pattern | _Requirements: 2.1-2.6, 3.1-3.6, 4.1-4.5 | Success: Selection handles visible, drag/scale/rotate work, smooth real-time feedback. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 6. Integrate ImageManipulationOverlay with PdfViewerPage
  - File: `src/FluentPDF.App/Views/PdfViewerPage.xaml`
  - Add ImageManipulationOverlay to page layout
  - Add "Insert Image" button to toolbar
  - Purpose: Enable image insertion from viewer
  - _Leverage: Existing PdfViewerPage.xaml toolbar structure_
  - _Requirements: 1.1-1.2_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Add ImageManipulationOverlay to PdfViewerPage Grid overlaying the PDF viewer. Add AppBarButton "Insert Image" with Picture symbol to toolbar CommandBar. Bind to ImageInsertionViewModel.InsertImageCommand. Position overlay to match PDF viewer bounds. | Restrictions: Follow existing toolbar patterns. Do not modify unrelated layout. | _Leverage: Existing PdfViewerPage.xaml | _Requirements: 1.1-1.2 | Success: Insert Image button visible, overlay renders over PDF, commands work. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 7. Add context menu for inserted images
  - File: `src/FluentPDF.App/Controls/ImageManipulationOverlay.xaml`
  - Add right-click context menu with Delete, Rotate options
  - Add Bring to Front, Send to Back options
  - Purpose: Provide quick access to image operations
  - _Leverage: WinUI MenuFlyout, page-operations context menu pattern_
  - _Requirements: 4.3, 5.2_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Add MenuFlyout to ImageManipulationOverlay that shows on right-click of selected image. Add items: Delete, Rotate Right 90°, Rotate Left 90°, Rotate 180°, separator, Bring to Front, Send to Back. Bind to ViewModel commands. | Restrictions: Only show when image selected. Follow existing menu patterns. | _Leverage: WinUI MenuFlyout | _Requirements: 4.3, 5.2 | Success: Context menu appears on right-click, all items functional. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 8. Add keyboard shortcuts for image operations
  - File: `src/FluentPDF.App/Controls/ImageManipulationOverlay.xaml.cs`
  - Handle Delete key to remove selected image
  - Handle arrow keys for nudging
  - Purpose: Enable keyboard-based image manipulation
  - _Leverage: WinUI keyboard handling_
  - _Requirements: 2.5-2.6, 5.1_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer | Task: Handle KeyDown in ImageManipulationOverlay. Delete key deletes selected image. Arrow keys nudge by 1 point. Shift+Arrow nudges by 10 points. Only handle when image is selected. | Restrictions: Only when overlay has focus and image selected. | _Leverage: WinUI keyboard handling | _Requirements: 2.5-2.6, 5.1 | Success: Keyboard shortcuts work correctly for selected images. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 9. Register ImageInsertionService and ViewModel in DI
  - File: `src/FluentPDF.App/App.xaml.cs`
  - Register IImageInsertionService and ImageInsertionViewModel
  - Purpose: Enable dependency injection
  - _Leverage: Existing DI registration in App.xaml.cs_
  - _Requirements: All_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer | Task: Add services.AddSingleton<IImageInsertionService, ImageInsertionService>() and services.AddTransient<ImageInsertionViewModel>() to ConfigureServices in App.xaml.cs. | Restrictions: Only add registrations, follow existing patterns. | _Leverage: Existing App.xaml.cs DI setup | _Requirements: All | Success: Services registered and resolvable. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 10. Integrate with HasUnsavedChanges
  - File: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
  - Track image modifications in HasUnsavedChanges
  - Purpose: Include image changes in save workflow
  - _Leverage: save-document HasUnsavedChanges pattern_
  - _Requirements: 5.5_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# MVVM Developer | Task: Add HasImageModifications property to PdfViewerViewModel. Include in HasUnsavedChanges calculation. Subscribe to ImageInsertionViewModel changes. Set HasImageModifications=true when images inserted/modified/deleted. Reset on save. | Restrictions: Follow existing HasUnsavedChanges pattern. | _Leverage: save-document HasUnsavedChanges | _Requirements: 5.5 | Success: Image operations set HasUnsavedChanges correctly. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 11. Add unit tests
  - File: `tests/FluentPDF.Rendering.Tests/Services/ImageInsertionServiceTests.cs`
  - Test insert, move, scale, rotate, delete operations
  - Test format support
  - Purpose: Ensure service reliability
  - _Leverage: Existing service test patterns_
  - _Requirements: All_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer | Task: Create ImageInsertionServiceTests with tests for each operation: InsertImage_ValidPath_Succeeds, InsertImage_InvalidPath_Fails, MoveImage_ValidPosition_Succeeds, ScaleImage_ValidSize_Succeeds, RotateImage_90Degrees_Succeeds, DeleteImage_Succeeds. Test PNG, JPEG, BMP format handling. Mock PDFium interop. | Restrictions: Use existing test patterns. | _Leverage: Existing test patterns | _Requirements: All | Success: All tests pass, good coverage. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 12. Add integration tests
  - File: `tests/FluentPDF.App.Tests/Integration/ImageInsertionTests.cs`
  - Test insert image workflow
  - Test manipulation handles
  - Purpose: End-to-end testing
  - _Leverage: FlaUI, existing integration patterns_
  - _Requirements: All_
  - _Prompt: Implement the task for spec image-insertion, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create ImageInsertionTests with FlaUI tests: InsertImageButton_Click_OpensFilePicker, ImageSelected_ShowsHandles, DeleteImage_RemovesFromPage. Use Page Object Pattern. | Restrictions: Skip if no UI. Follow existing patterns. | _Leverage: FlaUI, existing tests | _Requirements: All | Success: Integration tests verify workflow. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

## Summary

Implements image insertion into PDF pages:
- Insert images (PNG, JPEG, BMP, GIF) via file picker
- Visual manipulation overlay with selection handles
- Move, scale, rotate images with mouse and keyboard
- Context menu for image operations
- Integration with HasUnsavedChanges for save workflow
- Unit and integration tests

Uses PDFium FPDFPageObj APIs for image embedding.
