# Requirements Document

## Introduction

The Save Document feature exposes the existing annotation and form save functionality through the user interface. Currently, FluentPDF has fully implemented save capabilities in the service layer (`AnnotationService.SaveAnnotationsAsync`, `PdfFormService.SaveFormDataAsync`) but lacks UI elements to trigger these operations. This spec adds File menu save options, keyboard shortcuts, unsaved changes tracking, and close confirmation dialogs.

This feature enables:
- **Save Menu Items**: File > Save (Ctrl+S) and File > Save As (Ctrl+Shift+S)
- **Unsaved Changes Tracking**: Track modifications to annotations and form data
- **Visual Indicators**: Tab title shows "*" prefix for unsaved documents
- **Close Confirmation**: Prompt user before closing tabs with unsaved changes

## Alignment with Product Vision

This spec completes the annotation and form editing workflow, transforming FluentPDF from a viewer into a full editor.

Supports product principles:
- **Quality Over Features**: Leverages existing, tested save infrastructure (no new service logic)
- **Transparency Above All**: Clear visual indicators show document state
- **Local-First Processing**: All save operations happen locally with backup creation
- **Respect User Resources**: Prevents accidental data loss through confirmation dialogs

## Requirements

### Requirement 1: Save Menu Items

**User Story:** As a user, I want Save and Save As menu items, so that I can persist my annotations and form data.

#### Acceptance Criteria

1. WHEN user opens File menu THEN "Save" menu item SHALL be visible with Ctrl+S accelerator
2. WHEN user opens File menu THEN "Save As..." menu item SHALL be visible with Ctrl+Shift+S accelerator
3. WHEN user clicks "Save" with unsaved changes THEN document SHALL save to current file path
4. WHEN user clicks "Save" with no unsaved changes THEN nothing SHALL happen (command disabled)
5. WHEN user clicks "Save As..." THEN FileSavePicker SHALL appear with .pdf filter
6. WHEN user completes "Save As..." THEN document SHALL save to selected path
7. WHEN save succeeds THEN status message SHALL confirm save location
8. IF save fails THEN error dialog SHALL display with reason

### Requirement 2: Unsaved Changes Tracking

**User Story:** As a user, I want to see when documents have unsaved changes, so that I don't accidentally lose work.

#### Acceptance Criteria

1. WHEN annotation is created THEN HasUnsavedChanges SHALL become true
2. WHEN annotation is modified THEN HasUnsavedChanges SHALL become true
3. WHEN annotation is deleted THEN HasUnsavedChanges SHALL become true
4. WHEN form field value changes THEN HasUnsavedChanges SHALL become true
5. WHEN document is saved successfully THEN HasUnsavedChanges SHALL become false
6. WHEN document is first opened (no changes) THEN HasUnsavedChanges SHALL be false

### Requirement 3: Tab Title Indicator

**User Story:** As a user, I want to see which tabs have unsaved changes, so that I can identify modified documents at a glance.

#### Acceptance Criteria

1. WHEN HasUnsavedChanges is true THEN tab title SHALL display "*" prefix (e.g., "*Document.pdf")
2. WHEN HasUnsavedChanges is false THEN tab title SHALL display filename only (e.g., "Document.pdf")
3. WHEN HasUnsavedChanges changes THEN tab title SHALL update immediately

### Requirement 4: Close Confirmation Dialog

**User Story:** As a user, I want a confirmation when closing tabs with unsaved changes, so that I don't accidentally lose work.

#### Acceptance Criteria

1. WHEN user closes tab with unsaved changes THEN confirmation dialog SHALL appear
2. WHEN confirmation dialog appears THEN it SHALL display "Save", "Don't Save", "Cancel" buttons
3. WHEN user clicks "Save" THEN document SHALL save and tab SHALL close
4. WHEN user clicks "Don't Save" THEN tab SHALL close without saving
5. WHEN user clicks "Cancel" THEN dialog SHALL close and tab SHALL remain open
6. WHEN user closes tab without unsaved changes THEN no dialog SHALL appear

### Requirement 5: Keyboard Shortcuts

**User Story:** As a user, I want keyboard shortcuts for save operations, so that I can work efficiently.

#### Acceptance Criteria

1. WHEN user presses Ctrl+S THEN Save command SHALL execute for active tab
2. WHEN user presses Ctrl+Shift+S THEN Save As command SHALL execute for active tab
3. WHEN no tab is active THEN shortcuts SHALL do nothing
4. WHEN Save command cannot execute THEN shortcut SHALL do nothing (no error)

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: Unsaved changes tracking in dedicated service/property
- **Modular Design**: Save commands reuse existing IAnnotationService and IPdfFormService
- **Dependency Management**: ViewModels receive services via DI
- **Clear Interfaces**: Commands exposed via ICommand pattern

### Performance
- **Save Operation**: Complete in < 2 seconds for typical documents
- **Dirty Tracking**: Zero overhead on render operations
- **UI Updates**: Tab title updates in < 16ms (60 FPS)

### Security
- **Backup Creation**: Original file backed up before save (existing behavior)
- **Backup Restoration**: Failed saves restore from backup (existing behavior)

### Usability
- **Familiar Patterns**: Standard Ctrl+S/Ctrl+Shift+S shortcuts
- **Clear Feedback**: Status messages confirm save success/failure
- **Non-Blocking**: Save As dialog uses async picker pattern
