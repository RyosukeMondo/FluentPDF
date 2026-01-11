# Tasks Document

## Implementation Tasks

- [x] 1. Create watermark configuration models
  - File: `src/FluentPDF.Core/Models/WatermarkConfig.cs`
  - Define TextWatermarkConfig, ImageWatermarkConfig, WatermarkPosition, PageRange classes
  - Purpose: Establish data models for watermark configuration
  - _Leverage: Existing model patterns in `src/FluentPDF.Core/Models/`_
  - _Requirements: 1.1-1.7, 2.1-2.5, 3.1-3.5, 4.1-4.7, 5.1-5.7_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer | Task: Create WatermarkConfig.cs with classes: TextWatermarkConfig (Text, FontFamily, FontSize, Color, Opacity, RotationDegrees, Position, BehindContent), ImageWatermarkConfig (ImagePath, Scale, Opacity, RotationDegrees, Position, BehindContent), WatermarkPosition enum (Center, TopLeft, TopRight, BottomLeft, BottomRight, Custom), PageRange class with Type, SpecificPages, and static Parse method for "1-5, 10" format. | Restrictions: Models only, no logic. Follow existing model patterns. | _Leverage: Existing Core models | _Requirements: 1.1-1.7, 2.1-2.5, 3.1-3.5, 4.1-4.7, 5.1-5.7 | Success: All models compile, proper properties defined. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 2. Create IWatermarkService interface
  - File: `src/FluentPDF.Core/Services/IWatermarkService.cs`
  - Define ApplyTextWatermarkAsync, ApplyImageWatermarkAsync, RemoveWatermarksAsync, GeneratePreviewAsync
  - Purpose: Establish service contract for watermark operations
  - _Leverage: `src/FluentPDF.Core/Services/IAnnotationService.cs` pattern_
  - _Requirements: 1.1-1.7, 2.1-2.5, 6.1-6.5, 7.1-7.4_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Software Architect | Task: Create IWatermarkService with: ApplyTextWatermarkAsync(PdfDocument, TextWatermarkConfig, PageRange), ApplyImageWatermarkAsync(PdfDocument, ImageWatermarkConfig, PageRange), RemoveWatermarksAsync(PdfDocument, PageRange), GeneratePreviewAsync(PdfDocument, int pageIndex, WatermarkConfig) returning byte[] for preview image. Use Result pattern. | Restrictions: Interface only. Follow existing service patterns. | _Leverage: IAnnotationService pattern | _Requirements: 1.1-1.7, 2.1-2.5, 6.1-6.5, 7.1-7.4 | Success: Interface compiles, proper signatures. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 3. Implement WatermarkService
  - File: `src/FluentPDF.Rendering/Services/WatermarkService.cs`
  - Implement text watermarks using PDFium text objects
  - Implement image watermarks using PDFium image objects
  - Support positioning, opacity, rotation
  - Purpose: Provide watermark embedding functionality
  - _Leverage: `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs`, ImageInsertionService patterns_
  - _Requirements: 1.1-1.7, 2.1-2.5, 3.1-3.5, 4.1-4.7, 5.1-5.7, 6.1-6.5, 7.1-7.4_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer with PDFium expertise | Task: Implement WatermarkService. ApplyTextWatermarkAsync creates text objects with FPDFPageObj_CreateTextObj, sets font, color, applies transform for position/rotation/opacity. ApplyImageWatermarkAsync uses image object APIs like ImageInsertionService. RemoveWatermarksAsync removes watermark objects by tag. GeneratePreviewAsync renders page with watermark to bitmap. Iterate PageRange for batch application. | Restrictions: Use existing PdfiumInterop. Tag watermarks for later removal. | _Leverage: PdfiumInterop, ImageInsertionService | _Requirements: 1.1-1.7, 2.1-2.5, 3.1-3.5, 4.1-4.7, 5.1-5.7, 6.1-6.5, 7.1-7.4 | Success: All watermark operations work, preview generates correctly. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 4. Create WatermarkViewModel
  - File: `src/FluentPDF.App/ViewModels/WatermarkViewModel.cs`
  - Add TextConfig, ImageConfig properties
  - Add SelectedType, TargetPages properties
  - Add ApplyCommand, PreviewCommand, RemoveCommand
  - Purpose: Manage watermark dialog state
  - _Leverage: Existing ViewModel patterns_
  - _Requirements: 1.1-1.7, 2.1-2.5, 5.1-5.7, 6.1-6.5, 7.1-7.4_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# WinUI 3 MVVM Developer | Task: Create WatermarkViewModel with: TextConfig and ImageConfig observable properties, SelectedType (Text/Image) enum, TargetPages PageRange, ApplyCommand that calls service and closes dialog, PreviewCommand that generates and displays preview, RemoveCommand for removing watermarks, PreviewImage byte[] for display. Inject IWatermarkService. | Restrictions: Follow existing ViewModel patterns. Use proper change notification. | _Leverage: Existing ViewModels | _Requirements: 1.1-1.7, 2.1-2.5, 5.1-5.7, 6.1-6.5, 7.1-7.4 | Success: ViewModel manages state correctly, commands work. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 5. Create WatermarkDialog
  - Files: `src/FluentPDF.App/Views/WatermarkDialog.xaml`, `src/FluentPDF.App/Views/WatermarkDialog.xaml.cs`
  - Add tabs for Text and Image watermark types
  - Add configuration controls (text input, font, color, opacity, rotation, position)
  - Add page range selector
  - Add preview panel
  - Purpose: UI for watermark configuration
  - _Leverage: WinUI 3 ContentDialog, TabView_
  - _Requirements: 1.1-1.7, 2.1-2.5, 3.1-3.5, 4.1-4.7, 5.1-5.7, 6.1-6.5_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Create ContentDialog-based WatermarkDialog with: TabView for Text/Image selection, TextBox for watermark text, ComboBox for font, Slider for font size (12-144), ColorPicker for color, Slider for opacity (0-100%), NumberBox for rotation (-180 to 180), ComboBox for position presets, RadioButtons for page range (All/Current/Custom/Odd/Even), TextBox for custom range, Image control for preview. Bind to WatermarkViewModel. Apply/Cancel buttons. | Restrictions: Follow existing dialog patterns. Use proper MVVM binding. | _Leverage: WinUI ContentDialog, existing dialogs | _Requirements: 1.1-1.7, 2.1-2.5, 3.1-3.5, 4.1-4.7, 5.1-5.7, 6.1-6.5 | Success: Dialog displays all controls, bindings work, preview updates. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 6. Add live preview to WatermarkDialog
  - File: `src/FluentPDF.App/Views/WatermarkDialog.xaml.cs`
  - Update preview on any configuration change
  - Debounce preview generation
  - Purpose: Real-time visual feedback
  - _Leverage: Debounce pattern, IWatermarkService.GeneratePreviewAsync_
  - _Requirements: 6.1-6.5_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer | Task: Subscribe to WatermarkViewModel property changes. On any change, debounce 100ms then call GeneratePreviewAsync. Display result in preview Image control. Show loading indicator during generation. Cancel previous preview if new change occurs. | Restrictions: Use debouncing to avoid excessive updates. Handle cancellation properly. | _Leverage: Debounce pattern | _Requirements: 6.1-6.5 | Success: Preview updates within 100ms of change, smooth experience. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 7. Add watermark presets
  - File: `src/FluentPDF.App/ViewModels/WatermarkViewModel.cs`
  - Add preset buttons: CONFIDENTIAL, DRAFT, COPY, APPROVED
  - Add diagonal rotation preset
  - Purpose: Quick access to common watermarks
  - _Leverage: WatermarkViewModel_
  - _Requirements: 1.2-1.6, 4.5_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer | Task: Add ApplyPresetCommand(string presetName) to WatermarkViewModel. Presets: "CONFIDENTIAL" (red, 72pt, diagonal), "DRAFT" (gray, 96pt, diagonal), "COPY" (blue, 72pt, diagonal), "APPROVED" (green, 72pt, center). Diagonal preset sets rotation to 45°. Each preset updates TextConfig properties. | Restrictions: Presets only modify TextConfig, don't apply. | _Leverage: WatermarkViewModel | _Requirements: 1.2-1.6, 4.5 | Success: Preset buttons configure watermark correctly. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 8. Add watermark button to toolbar
  - File: `src/FluentPDF.App/Views/PdfViewerPage.xaml`
  - Add "Watermark" AppBarButton
  - Wire to show WatermarkDialog
  - Purpose: Enable watermark access from viewer
  - _Leverage: Existing toolbar structure_
  - _Requirements: 1.1_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Add AppBarButton "Watermark" with appropriate symbol to toolbar CommandBar in PdfViewerPage.xaml. Add Click handler to show WatermarkDialog. Create WatermarkViewModel and pass current document. | Restrictions: Follow existing toolbar patterns. | _Leverage: Existing toolbar | _Requirements: 1.1 | Success: Watermark button visible, opens dialog. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 9. Register WatermarkService in DI
  - File: `src/FluentPDF.App/App.xaml.cs`
  - Register IWatermarkService and WatermarkViewModel
  - Purpose: Enable dependency injection
  - _Leverage: Existing DI setup_
  - _Requirements: All_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer | Task: Add services.AddSingleton<IWatermarkService, WatermarkService>() and services.AddTransient<WatermarkViewModel>() to ConfigureServices. | Restrictions: Only add registrations. | _Leverage: Existing App.xaml.cs | _Requirements: All | Success: Services registered and resolvable. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 10. Integrate with HasUnsavedChanges
  - File: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
  - Track watermark modifications
  - Purpose: Include watermarks in save workflow
  - _Leverage: save-document HasUnsavedChanges pattern_
  - _Requirements: 7.4_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# MVVM Developer | Task: Add HasWatermarkModifications property. Include in HasUnsavedChanges. Set true when watermarks applied or removed. Reset on save. | Restrictions: Follow existing pattern. | _Leverage: save-document pattern | _Requirements: 7.4 | Success: Watermark changes set HasUnsavedChanges. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 11. Add unit tests
  - File: `tests/FluentPDF.Rendering.Tests/Services/WatermarkServiceTests.cs`
  - Test text and image watermark application
  - Test page range parsing
  - Test remove watermarks
  - Purpose: Ensure service reliability
  - _Leverage: Existing test patterns_
  - _Requirements: All_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer | Task: Create WatermarkServiceTests with: ApplyTextWatermark_ValidConfig_Succeeds, ApplyImageWatermark_ValidConfig_Succeeds, ApplyWatermark_AllPages_AppliesCorrectly, ApplyWatermark_PageRange_AppliesCorrectly, RemoveWatermarks_Succeeds, PageRange_Parse_ValidFormat_Succeeds, PageRange_Parse_InvalidFormat_Fails. Mock PDFium. | Restrictions: Use existing patterns. | _Leverage: Existing tests | _Requirements: All | Success: All tests pass. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 12. Add integration tests
  - File: `tests/FluentPDF.App.Tests/Integration/WatermarkTests.cs`
  - Test dialog workflow
  - Test preview updates
  - Test apply watermark
  - Purpose: End-to-end testing
  - _Leverage: FlaUI, existing patterns_
  - _Requirements: All_
  - _Prompt: Implement the task for spec watermarks, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create WatermarkTests with FlaUI: WatermarkButton_Click_OpensDialog, TextWatermark_Configure_PreviewUpdates, ApplyWatermark_AddsToPages. Use Page Object Pattern. | Restrictions: Skip if no UI. | _Leverage: FlaUI | _Requirements: All | Success: Integration tests pass. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

## Summary

Implements watermark functionality:
- Text watermarks with customizable font, size, color
- Image watermarks with scale and positioning
- Position presets (center, corners) and custom coordinates
- Opacity, rotation, and layer control (above/behind content)
- Page range selection (all, current, custom, odd, even)
- Live preview with debouncing
- Quick presets (CONFIDENTIAL, DRAFT, COPY, APPROVED)
- Remove watermarks option
- Integration with HasUnsavedChanges

Uses PDFium page object APIs for watermark embedding.
