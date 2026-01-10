# Requirements Document

## Introduction

The Bookmarks Panel implements PDF outline/table of contents extraction and navigation. This spec delivers structured document navigation by extracting PDF bookmarks (also known as outlines or document structure) using PDFium and presenting them in a hierarchical tree view. Users can quickly jump to sections of interest by clicking on bookmark entries, significantly improving navigation for large, structured documents.

The bookmarks panel enables:
- **Bookmark Extraction**: Read PDF outline/bookmarks structure using PDFium
- **Hierarchical Display**: Show bookmarks in a collapsible tree view
- **Jump Navigation**: Click bookmark to jump to destination page and position
- **Nested Bookmarks**: Support multi-level bookmark hierarchies
- **Empty State Handling**: Clear indication when PDF has no bookmarks

## Alignment with Product Vision

This spec enhances the core viewing experience by providing structured navigation, a standard feature in professional PDF viewers.

Supports product principles:
- **Quality Over Features**: Uses PDFium's proven bookmark API with comprehensive error handling
- **Verifiable Architecture**: All components testable, Result pattern for failures, ArchUnitNET rules extended
- **Observable System**: Structured logging for bookmark extraction operations
- **Respect User Resources**: Efficient tree structure, minimal memory footprint

Aligns with tech decisions:
- WinUI 3 TreeView control for hierarchical display
- FluentResults for bookmark extraction errors
- Serilog for performance monitoring
- Dependency injection for all services

## Requirements

### Requirement 1: PDFium Bookmark Extraction

**User Story:** As a developer, I want to extract PDF bookmarks using PDFium API, so that I can display document structure to users.

#### Acceptance Criteria

1. WHEN a PDF document is loaded THEN the service SHALL extract bookmarks using FPDF_GetFirstChild
2. WHEN extracting bookmarks THEN the service SHALL retrieve title, page number, and destination
3. WHEN a bookmark has children THEN the service SHALL recursively extract all nested levels
4. WHEN bookmark extraction completes THEN it SHALL return Result.Ok with BookmarkNode tree
5. WHEN a PDF has no bookmarks THEN the service SHALL return Result.Ok with empty list
6. WHEN extraction fails THEN the service SHALL return Result.Fail with PdfError(ErrorCategory.Rendering)
7. WHEN extracting bookmarks THEN it SHALL log operation with correlation ID and bookmark count
8. WHEN a bookmark title is empty THEN it SHALL use placeholder "(Untitled)"

### Requirement 2: Bookmark Data Model

**User Story:** As a developer, I want a hierarchical bookmark model, so that I can represent nested document structure.

#### Acceptance Criteria

1. WHEN a bookmark is created THEN it SHALL have Title, PageNumber, and Children properties
2. WHEN a bookmark has no destination THEN PageNumber SHALL be null
3. WHEN a bookmark has children THEN Children SHALL be a list of BookmarkNode
4. WHEN accessing bookmark depth THEN it SHALL calculate level from root (0-based)
5. WHEN serializing bookmarks THEN the model SHALL support JSON conversion for diagnostics
6. IF a bookmark title exceeds 500 characters THEN it SHALL be truncated with ellipsis

### Requirement 3: Bookmarks Panel UI

**User Story:** As a user, I want to see PDF bookmarks in a sidebar panel, so that I can understand document structure at a glance.

#### Acceptance Criteria

1. WHEN a PDF is loaded THEN the bookmarks panel SHALL appear on the left side
2. WHEN bookmarks exist THEN they SHALL display in a TreeView control
3. WHEN bookmarks are hierarchical THEN nested levels SHALL be indented
4. WHEN a bookmark node is collapsed THEN its children SHALL be hidden
5. WHEN a bookmark node is expanded THEN its children SHALL be visible
6. WHEN a PDF has no bookmarks THEN the panel SHALL show "No bookmarks in this document"
7. WHEN bookmarks are loading THEN the panel SHALL show a progress indicator
8. WHEN the panel is narrow THEN bookmark titles SHALL truncate with tooltip showing full text
9. WHEN user clicks "Toggle Bookmarks" button THEN the panel SHALL show/hide
10. IF panel is hidden THEN the button SHALL show "Show Bookmarks" icon

### Requirement 4: Bookmark Navigation

**User Story:** As a user, I want to click bookmarks to jump to pages, so that I can quickly navigate to sections of interest.

#### Acceptance Criteria

1. WHEN user clicks a bookmark THEN the viewer SHALL navigate to the target page
2. WHEN a bookmark has destination coordinates THEN the viewer SHALL scroll to that position
3. WHEN navigation occurs THEN the current page indicator SHALL update
4. WHEN navigating via bookmark THEN the bookmark SHALL be highlighted in the tree
5. WHEN a bookmark has no valid destination THEN clicking it SHALL do nothing
6. WHEN navigation completes THEN it SHALL log the bookmark title and target page
7. IF navigation fails THEN it SHALL show error toast notification

### Requirement 5: Bookmark Tree Rendering and Interaction

**User Story:** As a user, I want to expand/collapse bookmark sections, so that I can focus on relevant parts of the document structure.

#### Acceptance Criteria

1. WHEN the tree loads THEN top-level bookmarks SHALL be expanded by default
2. WHEN user clicks expand icon THEN the node SHALL expand showing children
3. WHEN user clicks collapse icon THEN the node SHALL collapse hiding children
4. WHEN a node has no children THEN it SHALL show no expand/collapse icon
5. WHEN user double-clicks a bookmark THEN it SHALL navigate and expand/collapse the node
6. WHEN keyboard focus is on tree THEN arrow keys SHALL navigate (Up/Down move selection, Right expands, Left collapses)
7. WHEN Enter key is pressed THEN the focused bookmark SHALL trigger navigation

### Requirement 6: Bookmarks Panel State Management

**User Story:** As a user, I want bookmark panel state to persist, so that I don't have to reconfigure it every time.

#### Acceptance Criteria

1. WHEN user resizes bookmark panel THEN the width SHALL persist across app sessions
2. WHEN user toggles panel visibility THEN the state SHALL persist
3. WHEN panel state is saved THEN it SHALL use ApplicationData.LocalSettings
4. WHEN loading panel state THEN it SHALL validate values (minimum width 150px, maximum 600px)
5. WHEN no saved state exists THEN it SHALL use defaults (panel visible, width 250px)
6. WHEN switching documents THEN panel state (visible/hidden, width) SHALL remain unchanged

### Requirement 7: Performance and Resource Management

**User Story:** As a user, I want bookmark extraction to be fast, so that documents load quickly.

#### Acceptance Criteria

1. WHEN extracting bookmarks THEN it SHALL complete in < 200ms for documents with < 100 bookmarks
2. WHEN extracting deep hierarchies (> 5 levels) THEN it SHALL handle gracefully without stack overflow
3. WHEN rendering bookmark tree THEN it SHALL virtualize for > 1000 total nodes
4. WHEN a document has no bookmarks THEN extraction SHALL complete in < 10ms
5. WHEN disposing document THEN bookmark data SHALL be cleared from memory
6. IF extraction exceeds 1 second THEN it SHALL log performance warning

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate concerns: bookmark extraction (service), bookmark model (domain), bookmark UI (view)
- **Modular Design**: IBookmarkService, BookmarkNode model, BookmarksPanel control are independently testable
- **Dependency Management**: Services depend on IPdfiumInterop abstraction
- **Clear Interfaces**: All services expose I*Service interfaces for DI and testing

### Performance
- **Extraction Speed**: < 200ms for typical documents
- **UI Rendering**: Virtualized tree view for large bookmark lists
- **Memory Efficiency**: Dispose bookmark tree when document closes
- **Async Operations**: All bookmark extraction async to prevent UI blocking

### Security
- **Input Validation**: Validate all bookmark data before display (sanitize titles)
- **Memory Safety**: Use SafeHandle for PDFium bookmark handles
- **Error Handling**: Never expose raw PDFium errors to users; always wrap in PdfError

### Reliability
- **Error Recovery**: If bookmark extraction fails, document should still be viewable
- **Resource Cleanup**: Ensure bookmark handles are disposed
- **Logging**: All operations logged with correlation IDs for debugging

### Usability
- **Responsive UI**: Bookmark extraction doesn't block document rendering
- **Keyboard Shortcuts**: Toggle bookmarks panel with Ctrl+B
- **Clear Feedback**: Show meaningful messages for empty bookmark state
- **Accessibility**: TreeView supports screen readers and keyboard navigation
