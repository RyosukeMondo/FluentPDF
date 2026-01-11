# Tasks Document

## Implementation Tasks

- [x] 1. Add HasUnsavedChanges property to PdfViewerViewModel
  - File: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
  - Add computed property that aggregates unsaved state from AnnotationViewModel and FormFieldViewModel
  - Subscribe to PropertyChanged events from child ViewModels
  - Purpose: Track combined unsaved changes across annotations and forms
  - _Leverage: `src/FluentPDF.App/ViewModels/AnnotationViewModel.cs`, `src/FluentPDF.App/ViewModels/FormFieldViewModel.cs`_
  - _Requirements: 2.1-2.6_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# WinUI 3 Developer specializing in MVVM and CommunityToolkit.Mvvm | Task: Add HasUnsavedChanges computed property to PdfViewerViewModel that returns true if AnnotationViewModel has unsaved annotations OR FormFieldViewModel.IsModified is true. Subscribe to property changes from child ViewModels to trigger PropertyChanged. | Restrictions: Do not modify AnnotationViewModel or FormFieldViewModel logic, only observe their state. Do not add new dependencies to services. | _Leverage: Existing AnnotationViewModel, FormFieldViewModel IsModified property | _Requirements: 2.1-2.6 | Success: HasUnsavedChanges correctly reflects combined dirty state, updates when annotations/forms change, unit tests pass. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 2. Extend TabViewModel with HasUnsavedChanges and DisplayName
  - File: `src/FluentPDF.App/ViewModels/TabViewModel.cs`
  - Add HasUnsavedChanges property delegating to ViewerViewModel
  - Add DisplayName property that prefixes "*" when unsaved
  - Purpose: Expose unsaved state and modified tab title for UI binding
  - _Leverage: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`_
  - _Requirements: 3.1-3.3_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# WinUI 3 Developer specializing in MVVM data binding | Task: Add HasUnsavedChanges property that delegates to ViewerViewModel.HasUnsavedChanges. Add DisplayName property that returns "*" + FileName when HasUnsavedChanges is true, otherwise just FileName. Subscribe to ViewerViewModel.PropertyChanged to forward HasUnsavedChanges changes. | Restrictions: Do not modify PdfViewerViewModel, only observe it. Keep single responsibility. | _Leverage: Existing TabViewModel, PdfViewerViewModel | _Requirements: 3.1-3.3 | Success: DisplayName shows asterisk prefix when unsaved, tab header updates immediately on change, no memory leaks from event subscriptions. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 3. Update TabView binding to use DisplayName
  - File: `src/FluentPDF.App/Views/MainWindow.xaml`
  - Change TabViewItem Header binding from FileName to DisplayName
  - Purpose: Show unsaved indicator in tab headers
  - _Leverage: Existing MainWindow.xaml TabItemTemplate_
  - _Requirements: 3.1-3.3_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: In MainWindow.xaml TabView.TabItemTemplate, change TabViewItem Header binding from FileName to DisplayName. Ensure Mode=OneWay for property change updates. | Restrictions: Do not modify other parts of MainWindow.xaml. Do not change TabViewItem structure beyond header binding. | _Leverage: Existing MainWindow.xaml TabItemTemplate | _Requirements: 3.1-3.3 | Success: Tab headers display "*filename.pdf" when unsaved, update immediately on change. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 4. Add SaveCommand and SaveAsCommand to PdfViewerViewModel
  - File: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
  - Add RelayCommand for Save that calls annotation and form save services
  - Add RelayCommand for SaveAs with FileSavePicker
  - Reset HasUnsavedChanges on successful save
  - Purpose: Expose save operations as commands for menu binding
  - _Leverage: `src/FluentPDF.Core/Services/IAnnotationService.cs`, `src/FluentPDF.Core/Services/IPdfFormService.cs`, existing FileSavePicker usage in merge/split commands_
  - _Requirements: 1.1-1.8_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# WinUI 3 Developer specializing in MVVM commands and async operations | Task: Add [RelayCommand] SaveAsync that calls AnnotationService.SaveAnnotationsAsync and FormService.SaveFormDataAsync to current FilePath. Add CanSave that returns HasUnsavedChanges. Add [RelayCommand] SaveAsAsync that shows FileSavePicker then saves to selected path. Handle errors with Result pattern and show appropriate messages. Set HasUnsavedChanges=false on success. | Restrictions: Reuse existing service methods, do not implement new save logic. Use FileSavePicker pattern from existing MergeDocumentsCommand. | _Leverage: IAnnotationService.SaveAnnotationsAsync, IPdfFormService.SaveFormDataAsync | _Requirements: 1.1-1.8 | Success: SaveCommand saves annotations and forms to current file, SaveAsCommand prompts for location, both reset HasUnsavedChanges on success. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 5. Add Save menu items to MainWindow File menu
  - Files: `src/FluentPDF.App/Views/MainWindow.xaml`, `src/FluentPDF.App/Views/MainWindow.xaml.cs`
  - Add Save MenuFlyoutItem with Ctrl+S accelerator after Open
  - Add Save As MenuFlyoutItem with Ctrl+Shift+S accelerator
  - Add Click handlers to invoke commands on active tab
  - Purpose: Provide menu access to save functionality
  - _Leverage: Existing MainWindow.xaml File menu structure, OnOpenFileClick pattern_
  - _Requirements: 1.1-1.2, 5.1-5.4_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer | Task: Add MenuFlyoutItem "Save" with Ctrl+S KeyboardAccelerator after "Open..." item. Add MenuFlyoutItem "Save As..." with Ctrl+Shift+S KeyboardAccelerator after Save. Add Click handlers in code-behind that invoke ActiveTab.ViewerViewModel.SaveCommand and SaveAsCommand. Disable items when no tab active or CanSave is false. | Restrictions: Follow existing menu item patterns. Use consistent naming and styling. | _Leverage: Existing File menu, OnOpenFileClick pattern | _Requirements: 1.1-1.2, 5.1-5.4 | Success: Save and Save As menu items visible with correct shortcuts, invoke correct commands, disabled when no unsaved changes. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 6. Create SaveConfirmationDialog
  - Files: `src/FluentPDF.App/Views/Dialogs/SaveConfirmationDialog.xaml`, `src/FluentPDF.App/Views/Dialogs/SaveConfirmationDialog.xaml.cs`
  - Create ContentDialog with Save, Don't Save, Cancel buttons
  - Add static ShowAsync method returning SaveConfirmationResult enum
  - Purpose: Prompt user before closing tabs with unsaved changes
  - _Leverage: WinUI 3 ContentDialog pattern_
  - _Requirements: 4.1-4.5_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 XAML Developer specializing in dialogs | Task: Create ContentDialog-based SaveConfirmationDialog with Title "Unsaved Changes", message "Do you want to save changes to {filename}?", and three buttons: Save (Primary), Don't Save (Secondary), Cancel (Close). Create static ShowAsync method that returns SaveConfirmationResult enum. Use XamlRoot parameter for proper dialog hosting. | Restrictions: Follow WinUI 3 ContentDialog best practices. Keep dialog simple and focused. | _Leverage: WinUI 3 ContentDialog documentation | _Requirements: 4.1-4.5 | Success: Dialog displays correctly, returns appropriate result for each button click, handles Cancel/Escape properly. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 7. Integrate SaveConfirmationDialog with tab close
  - File: `src/FluentPDF.App/Views/MainWindow.xaml.cs`
  - Modify OnTabCloseRequested to check HasUnsavedChanges
  - Show dialog and handle Save/DontSave/Cancel results
  - Purpose: Prevent accidental data loss on tab close
  - _Leverage: Existing OnTabCloseRequested handler, SaveConfirmationDialog.ShowAsync_
  - _Requirements: 4.1-4.6_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 C# Developer | Task: Modify OnTabCloseRequested to check TabViewModel.HasUnsavedChanges. If true, show SaveConfirmationDialog. Handle Save result by calling SaveCommand then closing. Handle DontSave by closing without save. Handle Cancel by setting args.Cancel=true. If no unsaved changes, close immediately. | Restrictions: Do not modify SaveConfirmationDialog. Keep existing tab close logic for non-dirty tabs. | _Leverage: Existing OnTabCloseRequested handler, SaveConfirmationDialog.ShowAsync | _Requirements: 4.1-4.6 | Success: Tab close shows dialog only when unsaved, Save/DontSave/Cancel behave correctly, no regression for clean tabs. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 8. Add unit tests for save functionality
  - Files: `tests/FluentPDF.App.Tests/ViewModels/PdfViewerViewModelSaveTests.cs`, `tests/FluentPDF.App.Tests/ViewModels/TabViewModelTests.cs`
  - Test SaveCommand execution and CanExecute logic
  - Test HasUnsavedChanges aggregation
  - Test DisplayName with/without unsaved changes
  - Purpose: Ensure save commands and tracking work correctly
  - _Leverage: Existing ViewModel test patterns, Moq, FluentAssertions_
  - _Requirements: All_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer specializing in xUnit and Moq | Task: Create PdfViewerViewModelSaveTests with tests: SaveCommand_WhenHasUnsavedChanges_SavesSuccessfully, SaveCommand_WhenNoChanges_CannotExecute, SaveAsCommand_ShowsPicker_SavesSuccessfully, HasUnsavedChanges_WhenAnnotationModified_ReturnsTrue. Create TabViewModelTests for DisplayName_WhenUnsaved_ShowsAsterisk. Mock IAnnotationService and IPdfFormService. | Restrictions: Use existing test patterns. Focus on ViewModel behavior. | _Leverage: Existing ViewModel test patterns, Moq, FluentAssertions | _Requirements: All | Success: All tests pass, good coverage of save scenarios, tests are isolated and reliable. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 9. Add integration tests for save workflow
  - File: `tests/FluentPDF.App.Tests/Integration/SaveWorkflowTests.cs`
  - Test Ctrl+S save behavior
  - Test Save As file picker flow
  - Test close confirmation dialog
  - Purpose: End-to-end testing of save workflow
  - _Leverage: Existing FlaUI integration tests, Page Object Pattern_
  - _Requirements: All_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer with FlaUI expertise | Task: Create SaveWorkflowTests with tests: CtrlS_WhenDocumentModified_SavesDocument, SaveAs_ShowsFilePicker, CloseTab_WhenUnsaved_ShowsConfirmation, CloseTab_WhenClean_ClosesImmediately. Use FlaUI Page Object Pattern. Handle async dialogs with FlaUI retry strategies. | Restrictions: Skip tests if running in CI without UI. Follow existing integration test patterns. | _Leverage: Existing FlaUI tests, Page Object Pattern | _Requirements: All | Success: Integration tests verify end-to-end save workflow, tests are stable and CI-compatible. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

- [x] 10. Update documentation
  - Files: `docs/ARCHITECTURE.md`, `README.md`
  - Add save command flow to architecture docs
  - Update features section with save functionality
  - Add keyboard shortcut reference
  - Purpose: Document save functionality for users and developers
  - _Leverage: Existing documentation structure_
  - _Requirements: All_
  - _Prompt: Implement the task for spec save-document, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Technical Writer | Task: Update ARCHITECTURE.md with save command flow diagram. Update README.md features section to include save functionality. Add keyboard shortcut reference (Ctrl+S, Ctrl+Shift+S). Document backup behavior on save. | Restrictions: Follow existing documentation style. Keep updates concise. | _Leverage: Existing docs/ARCHITECTURE.md, README.md | _Requirements: All | Success: Documentation accurately describes save features, shortcuts listed, architecture updated. Mark task in-progress in tasks.md before starting, log implementation with log-implementation tool after completion, then mark as complete._

## Summary

Implements complete save document UI:
- Save (Ctrl+S) and Save As (Ctrl+Shift+S) menu items
- HasUnsavedChanges tracking aggregating annotations and forms
- Tab title asterisk indicator for unsaved documents
- Confirmation dialog before closing unsaved tabs
- Unit and integration tests
- Documentation updates

All save logic reuses existing IAnnotationService and IPdfFormService - no new service-layer code required.
