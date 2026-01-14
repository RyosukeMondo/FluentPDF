# Requirements Document

## Introduction

The Page Operations feature enables users to manipulate individual pages within a PDF document. This includes rotating pages, deleting pages, reordering pages via drag-and-drop, and inserting blank pages. These operations use the existing QPDF library for lossless manipulation.

The feature integrates with the existing thumbnails sidebar to provide intuitive visual page management:
- **Rotate Pages**: Rotate selected pages by 90°, 180°, or 270°
- **Delete Pages**: Remove selected pages from the document
- **Reorder Pages**: Drag-and-drop pages in the thumbnails sidebar
- **Insert Blank Pages**: Add new blank pages at specified positions

## Alignment with Product Vision

This spec extends FluentPDF's document editing capabilities beyond merge/split to granular page-level control.

Supports product principles:
- **Quality Over Features**: Uses QPDF for lossless page manipulation
- **Local-First Processing**: All operations happen locally
- **Respect User Resources**: Efficient single-page operations without full document rewrite
- **Open Source Foundation**: Leverages QPDF (Apache 2.0)

## Requirements

### Requirement 1: Rotate Pages

**User Story:** As a user, I want to rotate pages, so that I can correct scanned documents or adjust page orientation.

#### Acceptance Criteria

1. WHEN user selects page(s) in thumbnails THEN context menu SHALL show "Rotate" submenu
2. WHEN user clicks "Rotate Right 90°" THEN selected pages SHALL rotate clockwise
3. WHEN user clicks "Rotate Left 90°" THEN selected pages SHALL rotate counter-clockwise
4. WHEN user clicks "Rotate 180°" THEN selected pages SHALL rotate upside-down
5. WHEN rotation completes THEN thumbnails SHALL update to show rotated preview
6. WHEN rotation completes THEN main viewer SHALL show rotated page
7. WHEN rotation is applied THEN HasUnsavedChanges SHALL become true

### Requirement 2: Delete Pages

**User Story:** As a user, I want to delete pages, so that I can remove unwanted content from documents.

#### Acceptance Criteria

1. WHEN user selects page(s) in thumbnails THEN context menu SHALL show "Delete" option
2. WHEN user clicks "Delete" THEN confirmation dialog SHALL appear with page count
3. WHEN user confirms deletion THEN selected pages SHALL be removed
4. WHEN last page would be deleted THEN error SHALL display "Cannot delete all pages"
5. WHEN deletion completes THEN thumbnails SHALL update showing remaining pages
6. WHEN deletion completes THEN page numbers SHALL renumber correctly
7. WHEN deletion is applied THEN HasUnsavedChanges SHALL become true

### Requirement 3: Reorder Pages

**User Story:** As a user, I want to reorder pages by dragging, so that I can organize document content.

#### Acceptance Criteria

1. WHEN user drags thumbnail THEN visual indicator SHALL show drag operation
2. WHEN user drags over drop position THEN insertion line SHALL show target location
3. WHEN user drops thumbnail THEN page SHALL move to new position
4. WHEN multiple thumbnails selected THEN all selected pages SHALL move together
5. WHEN reorder completes THEN page numbers SHALL update immediately
6. WHEN reorder completes THEN HasUnsavedChanges SHALL become true
7. WHEN user presses Escape during drag THEN operation SHALL cancel

### Requirement 4: Insert Blank Pages

**User Story:** As a user, I want to insert blank pages, so that I can add space for annotations or separate sections.

#### Acceptance Criteria

1. WHEN user right-clicks between thumbnails THEN "Insert Blank Page" SHALL appear
2. WHEN user selects page size THEN options SHALL include: Same as current, Letter, A4, Legal
3. WHEN user confirms THEN blank page SHALL insert at specified position
4. WHEN insertion completes THEN thumbnails SHALL update with new page
5. WHEN insertion completes THEN page numbers SHALL update
6. WHEN insertion is applied THEN HasUnsavedChanges SHALL become true

### Requirement 5: Keyboard Shortcuts

**User Story:** As a user, I want keyboard shortcuts for page operations, so that I can work efficiently.

#### Acceptance Criteria

1. WHEN user presses Delete with page selected THEN delete confirmation SHALL appear
2. WHEN user presses Ctrl+R with page selected THEN page SHALL rotate right 90°
3. WHEN user presses Ctrl+Shift+R with page selected THEN page SHALL rotate left 90°
4. WHEN no page selected THEN shortcuts SHALL do nothing

### Requirement 6: Multi-Page Selection

**User Story:** As a user, I want to select multiple pages, so that I can perform batch operations.

#### Acceptance Criteria

1. WHEN user Ctrl+clicks thumbnails THEN multiple individual pages SHALL select
2. WHEN user Shift+clicks thumbnails THEN range of pages SHALL select
3. WHEN user presses Ctrl+A THEN all pages SHALL select
4. WHEN multiple selected THEN operations SHALL apply to all selected pages

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: Separate service for page operations (IPageOperationsService)
- **Modular Design**: Service reuses existing QPDF P/Invoke bindings
- **Clear Interfaces**: Commands exposed via ViewModel for UI binding

### Performance
- **Rotate**: < 100ms per page
- **Delete**: < 200ms for up to 10 pages
- **Reorder**: Instant visual feedback, < 500ms to persist
- **Thumbnails**: Update within 100ms after operation

### Security
- **Backup Creation**: Create backup before destructive operations
- **Undo Support**: Operations should be reversible until saved

### Usability
- **Visual Feedback**: Clear selection highlighting in thumbnails
- **Drag Indicators**: Show insertion point during drag
- **Confirmation**: Prompt before deleting pages
