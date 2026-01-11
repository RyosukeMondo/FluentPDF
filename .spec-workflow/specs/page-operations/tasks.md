# Tasks Document

## Implementation Tasks

- [x] 1. Create IPageOperationsService interface
  - File: `src/FluentPDF.Core/Services/IPageOperationsService.cs`
  - Define interface with RotatePagesAsync, DeletePagesAsync, ReorderPagesAsync, InsertBlankPageAsync methods
  - Add RotationAngle and PageSize enums
  - Purpose: Establish service contract for page-level operations
  - _Leverage: `src/FluentPDF.Core/Services/IDocumentEditingService.cs` pattern_
  - _Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.7, 4.1-4.6_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Software Architect specializing in service interfaces | Task: Create IPageOperationsService interface with methods: RotatePagesAsync(PdfDocument, int[], RotationAngle), DeletePagesAsync(PdfDocument, int[]), ReorderPagesAsync(PdfDocument, int[], int targetIndex), InsertBlankPageAsync(PdfDocument, int, PageSize). Add RotationAngle enum (Rotate90, Rotate180, Rotate270) and PageSize enum (SameAsCurrent, Letter, A4, Legal). Follow FluentResults pattern. | Restrictions: Do not implement service, only interface. Follow existing service interface patterns. | _Leverage: IDocumentEditingService pattern | _Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.7, 4.1-4.6 | Success: Interface compiles, follows existing patterns, all methods defined with proper signatures. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 2. Implement PageOperationsService with QPDF
  - File: `src/FluentPDF.Rendering/Services/PageOperationsService.cs`
  - Implement IPageOperationsService using QPDF P/Invoke
  - Handle page rotation, deletion, reordering, and blank page insertion
  - Purpose: Provide lossless page manipulation via QPDF
  - _Leverage: `src/FluentPDF.Rendering/Interop/QpdfInterop.cs`, `src/FluentPDF.Rendering/Services/DocumentEditingService.cs`_
  - _Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.7, 4.1-4.6_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer with QPDF expertise | Task: Implement PageOperationsService using QPDF for all operations. RotatePagesAsync should use qpdf_init_write_page and rotation flags. DeletePagesAsync removes pages by index. ReorderPagesAsync reorders via qpdf_add_page. InsertBlankPageAsync creates blank page with specified dimensions. All operations return Result pattern. | Restrictions: Use existing QPDF interop, do not duplicate P/Invoke. Create backup before destructive operations. | _Leverage: QpdfInterop, DocumentEditingService patterns | _Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.7, 4.1-4.6 | Success: All operations work correctly with QPDF, proper error handling, backups created. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 3. Register PageOperationsService in DI container
  - File: `src/FluentPDF.App/App.xaml.cs`
  - Register IPageOperationsService with PageOperationsService implementation
  - Purpose: Enable service injection throughout application
  - _Leverage: Existing DI registration patterns in App.xaml.cs_
  - _Requirements: All_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer | Task: Add services.AddSingleton<IPageOperationsService, PageOperationsService>() to ConfigureServices in App.xaml.cs. Follow existing service registration patterns. | Restrictions: Only add registration, do not modify other code. | _Leverage: Existing DI setup in App.xaml.cs | _Requirements: All | Success: Service is registered and resolvable via DI. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 4. Add page operation commands to ThumbnailsViewModel
  - File: `src/FluentPDF.App/ViewModels/ThumbnailsViewModel.cs`
  - Add RotateRightCommand, RotateLeftCommand, Rotate180Command
  - Add DeletePagesCommand with confirmation
  - Add InsertBlankPageCommand
  - Add MovePagesTo method for drag-drop
  - Purpose: Expose page operations as commands for UI binding
  - _Leverage: `src/FluentPDF.App/ViewModels/ThumbnailsViewModel.cs`, IPageOperationsService_
  - _Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.7, 4.1-4.6, 5.1-5.4_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# WinUI 3 Developer with MVVM expertise | Task: Add [RelayCommand] methods: RotateRightAsync, RotateLeftAsync, Rotate180Async, DeletePagesAsync, InsertBlankPageAsync. Add CanExecute that checks SelectedThumbnails.Any(). Inject IPageOperationsService. Add MovePagesTo(int[] indices, int targetIndex) for drag-drop. After each operation, refresh thumbnails and notify PdfViewerViewModel. | Restrictions: Use existing SelectedThumbnails property. Do not modify thumbnail loading logic. | _Leverage: Existing ThumbnailsViewModel, IPageOperationsService | _Requirements: 1.1-1.7, 2.1-2.7, 3.1-3.7, 4.1-4.6, 5.1-5.4 | Success: All commands work, thumbnails refresh after operations, CanExecute logic correct. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 5. Add context menu to ThumbnailsPanel
  - File: `src/FluentPDF.App/Controls/ThumbnailsPanel.xaml`
  - Add MenuFlyout with Rotate submenu (Right 90°, Left 90°, 180°)
  - Add Delete menu item
  - Add Insert Blank Page menu item with size submenu
  - Purpose: Provide right-click access to page operations
  - _Leverage: WinUI 3 MenuFlyout, existing ThumbnailsPanel.xaml_
  - _Requirements: 1.1-1.2, 2.1-2.2, 4.1-4.2_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Add MenuFlyout to ListView in ThumbnailsPanel.xaml. Add MenuFlyoutSubItem "Rotate" with items: "Rotate Right 90°", "Rotate Left 90°", "Rotate 180°". Add MenuFlyoutItem "Delete". Add MenuFlyoutSubItem "Insert Blank Page" with items: "Same Size", "Letter", "A4", "Legal". Bind Click to ViewModel commands. | Restrictions: Follow existing XAML patterns. Use proper keyboard accelerators. | _Leverage: Existing ThumbnailsPanel.xaml, WinUI MenuFlyout | _Requirements: 1.1-1.2, 2.1-2.2, 4.1-4.2 | Success: Context menu appears on right-click, all items work correctly. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 6. Implement drag-drop reordering in ThumbnailsPanel
  - Files: `src/FluentPDF.App/Controls/ThumbnailsPanel.xaml`, `src/FluentPDF.App/Controls/ThumbnailsPanel.xaml.cs`
  - Enable CanDragItems and CanReorderItems on ListView
  - Handle DragItemsStarting, DragOver, Drop events
  - Show visual insertion indicator during drag
  - Purpose: Enable intuitive page reordering via drag-drop
  - _Leverage: WinUI 3 ListView drag-drop, ThumbnailsViewModel.MovePagesTo_
  - _Requirements: 3.1-3.7_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer with drag-drop expertise | Task: Set CanDragItems="True" and CanReorderItems="True" on ListView. Handle DragItemsStarting to set drag data. Handle DragOver to show insertion indicator. Handle Drop to call ViewModel.MovePagesTo with source indices and target index. Support multi-item drag. | Restrictions: Use WinUI standard drag-drop, no custom adorners. | _Leverage: WinUI ListView drag-drop, ThumbnailsViewModel | _Requirements: 3.1-3.7 | Success: Pages can be dragged and dropped to reorder, visual feedback during drag, multi-select works. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 7. Add keyboard shortcuts for page operations
  - File: `src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml.cs`
  - Handle Delete key for page deletion
  - Handle Ctrl+R for rotate right, Ctrl+Shift+R for rotate left
  - Handle Ctrl+A for select all
  - Purpose: Enable keyboard-based page operations
  - _Leverage: WinUI KeyboardAccelerator, ThumbnailsViewModel commands_
  - _Requirements: 5.1-5.4, 6.1-6.3_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer | Task: Add KeyboardAccelerators to ThumbnailsPanel: Delete key invokes DeletePagesCommand, Ctrl+R invokes RotateRightCommand, Ctrl+Shift+R invokes RotateLeftCommand, Ctrl+A selects all thumbnails. Check CanExecute before invoking. | Restrictions: Only add accelerators when thumbnails panel has focus. | _Leverage: WinUI KeyboardAccelerator, existing shortcuts pattern | _Requirements: 5.1-5.4, 6.1-6.3 | Success: All shortcuts work correctly, respects selection state. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 8. Integrate page operations with HasUnsavedChanges
  - File: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
  - Track page modifications in HasUnsavedChanges
  - Add PageModified event subscription
  - Purpose: Track unsaved page changes for save workflow
  - _Leverage: save-document spec HasUnsavedChanges pattern_
  - _Requirements: 1.7, 2.7, 3.6, 4.6_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# MVVM Developer | Task: Add HasPageModifications property to PdfViewerViewModel. Update HasUnsavedChanges to include HasPageModifications. Subscribe to ThumbnailsViewModel page operation events to set HasPageModifications=true. Reset on save. | Restrictions: Follow existing HasUnsavedChanges pattern from save-document spec. | _Leverage: save-document HasUnsavedChanges pattern | _Requirements: 1.7, 2.7, 3.6, 4.6 | Success: Page operations set HasUnsavedChanges, tab shows asterisk after operations. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 9. Add delete confirmation dialog
  - Files: `src/FluentPDF.App/Views/Dialogs/DeletePagesDialog.xaml`, `src/FluentPDF.App/Views/Dialogs/DeletePagesDialog.xaml.cs`
  - Show confirmation with page count
  - Prevent deleting all pages
  - Purpose: Prevent accidental page deletion
  - _Leverage: WinUI 3 ContentDialog, SaveConfirmationDialog pattern_
  - _Requirements: 2.2-2.4_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Create DeletePagesDialog with ContentDialog showing "Delete {count} page(s)?" message. Add Delete and Cancel buttons. Add static ShowAsync(XamlRoot, int pageCount) method. Return bool for confirmed. If pageCount equals total pages, show error instead. | Restrictions: Follow SaveConfirmationDialog pattern. Simple dialog only. | _Leverage: SaveConfirmationDialog pattern, ContentDialog | _Requirements: 2.2-2.4 | Success: Dialog shows page count, prevents full deletion, returns correct result. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 10. Add unit tests for PageOperationsService
  - File: `tests/FluentPDF.Rendering.Tests/Services/PageOperationsServiceTests.cs`
  - Test each operation with mock QPDF
  - Test error scenarios
  - Purpose: Ensure service reliability
  - _Leverage: Existing service test patterns, Moq, FluentAssertions_
  - _Requirements: All_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer with xUnit expertise | Task: Create PageOperationsServiceTests with tests: RotatePages_ValidIndices_Succeeds, DeletePages_ValidIndices_Succeeds, DeletePages_AllPages_Fails, ReorderPages_ValidIndices_Succeeds, InsertBlankPage_ValidIndex_Succeeds. Mock QPDF interop. Test error handling. | Restrictions: Use existing test patterns. Mock external dependencies. | _Leverage: Existing service tests, Moq, FluentAssertions | _Requirements: All | Success: All tests pass, good coverage of operations and error cases. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [ ] 11. Add integration tests for page operations
  - File: `tests/FluentPDF.App.Tests/Integration/PageOperationsTests.cs`
  - Test context menu operations
  - Test drag-drop reorder
  - Test keyboard shortcuts
  - Purpose: End-to-end testing of page operations
  - _Leverage: FlaUI, existing integration test patterns_
  - _Requirements: All_
  - _Prompt: Implement the task for spec page-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer with FlaUI expertise | Task: Create PageOperationsTests with tests: ContextMenu_RotateRight_RotatesPage, ContextMenu_Delete_ShowsConfirmation, DragDrop_ReorderPage_UpdatesOrder, Keyboard_Delete_DeletesPage. Use FlaUI Page Object Pattern. | Restrictions: Skip if no UI available. Follow existing integration patterns. | _Leverage: Existing FlaUI tests, Page Object Pattern | _Requirements: All | Success: Integration tests verify page operations work end-to-end. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

## Summary

Implements page-level operations:
- Rotate pages (90°, 180°, 270°) via context menu and shortcuts
- Delete pages with confirmation dialog
- Reorder pages via drag-drop in thumbnails
- Insert blank pages at specified positions
- Multi-page selection support
- Keyboard shortcuts (Delete, Ctrl+R, Ctrl+Shift+R, Ctrl+A)
- Integration with HasUnsavedChanges for save workflow

Uses existing QPDF library for lossless operations.
