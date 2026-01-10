# Requirements Document

## Introduction

The Recent Files Management implements MRU (Most Recently Used) file tracking, Windows Jump List integration, and multi-tab document support. This spec enables users to quickly reopen recently viewed PDFs, manage multiple documents simultaneously, and leverage Windows taskbar integration for fast access to common files.

The recent files system enables:
- **Recent Files List**: Track and display recently opened PDF files
- **Jump List Integration**: Windows taskbar right-click shows recent files
- **Multi-Tab Support**: Open multiple PDFs in separate tabs
- **Persistence**: Recent files list survives app restarts
- **Privacy Control**: Clear recent files option

## Alignment with Product Vision

This spec enhances productivity by providing quick access to frequently used documents, a standard feature in professional applications.

Supports product principles:
- **Respect User Resources**: Efficient tab management, memory-conscious document handling
- **Transparency Above All**: Clear recent files management, easy to clear history
- **Privacy-First**: Optional recent files tracking, clear button to erase history
- **Standards Compliance**: Windows Jump List integration follows platform guidelines

Aligns with tech decisions:
- WinUI 3 TabView for multi-document UI
- ApplicationData.LocalSettings for recent files persistence
- Windows.UI.StartScreen.JumpList API for taskbar integration
- Dependency injection for all services

## Requirements

### Requirement 1: Recent Files Tracking and Storage

**User Story:** As a user, I want the app to remember recently opened files, so that I can quickly reopen them later.

#### Acceptance Criteria

1. WHEN a PDF file is successfully opened THEN it SHALL be added to the recent files list
2. WHEN a file is opened multiple times THEN it SHALL move to the top of the list (MRU ordering)
3. WHEN the recent files list exceeds 10 items THEN the oldest item SHALL be removed
4. WHEN the app closes THEN the recent files list SHALL persist to ApplicationData.LocalSettings
5. WHEN the app starts THEN the recent files list SHALL load from storage
6. WHEN a file path no longer exists THEN it SHALL be removed from the list on next access attempt
7. WHEN user clears recent files THEN all entries SHALL be removed from storage
8. IF storage is corrupted THEN the service SHALL log error and reset to empty list

### Requirement 2: Recent Files UI

**User Story:** As a user, I want to see recently opened files in the File menu, so that I can quickly reopen them.

#### Acceptance Criteria

1. WHEN user clicks "File" menu THEN a "Recent Files" section SHALL display
2. WHEN recent files exist THEN they SHALL display as clickable menu items (file name only, with tooltip showing full path)
3. WHEN user clicks a recent file THEN it SHALL open in a new tab
4. WHEN recent files list is empty THEN the menu SHALL show "No recent files"
5. WHEN user hovers over recent file THEN tooltip SHALL show full file path
6. WHEN a file fails to open THEN it SHALL be removed from the recent list
7. WHEN user clicks "Clear Recent Files" THEN a confirmation dialog SHALL appear
8. WHEN user confirms clear THEN all recent files SHALL be removed

### Requirement 3: Windows Jump List Integration

**User Story:** As a user, I want to right-click the taskbar icon to see recent files, so that I can open them without launching the app first.

#### Acceptance Criteria

1. WHEN a file is added to recent files THEN it SHALL be added to Windows Jump List
2. WHEN Jump List updates THEN it SHALL show up to 10 recent items
3. WHEN user clicks a Jump List item THEN the app SHALL launch and open that file
4. WHEN the app is already running THEN clicking Jump List item SHALL open file in new tab
5. WHEN recent files are cleared THEN Jump List SHALL be cleared
6. IF Jump List update fails THEN the error SHALL be logged but app SHALL continue
7. WHEN app is pinned to taskbar THEN Jump List SHALL be available

### Requirement 4: Multi-Tab Document Support

**User Story:** As a user, I want to open multiple PDFs in tabs, so that I can switch between documents easily.

#### Acceptance Criteria

1. WHEN user opens a second file THEN it SHALL open in a new tab
2. WHEN multiple tabs exist THEN each tab SHALL show the file name
3. WHEN user clicks a tab THEN that document SHALL become active
4. WHEN user closes a tab THEN that document SHALL be disposed
5. WHEN the last tab is closed THEN the app SHALL show empty welcome screen
6. WHEN tabs exceed available width THEN they SHALL scroll horizontally
7. WHEN user presses Ctrl+Tab THEN it SHALL switch to next tab
8. WHEN user presses Ctrl+Shift+Tab THEN it SHALL switch to previous tab
9. WHEN user presses Ctrl+W THEN it SHALL close the active tab
10. IF a tab has unsaved changes (future: annotations) THEN closing SHALL prompt for confirmation

### Requirement 5: Tab State Management

**User Story:** As a user, I want each tab to remember its viewing state, so that switching tabs preserves my position.

#### Acceptance Criteria

1. WHEN user switches tabs THEN each tab SHALL preserve page number and zoom level
2. WHEN returning to a tab THEN it SHALL restore the previous page and zoom
3. WHEN a tab is closed THEN its state SHALL be discarded
4. WHEN memory usage is high THEN inactive tabs SHALL unload rendered pages (keep metadata only)
5. WHEN returning to unloaded tab THEN it SHALL re-render the current page
6. IF tab state restoration fails THEN it SHALL default to page 1, 100% zoom

### Requirement 6: Tab Management UI

**User Story:** As a user, I want clear tab controls, so that I can manage multiple documents efficiently.

#### Acceptance Criteria

1. WHEN hovering over a tab THEN a close button SHALL appear
2. WHEN clicking tab close button THEN that tab SHALL close
3. WHEN middle-clicking a tab THEN that tab SHALL close
4. WHEN a tab is active THEN it SHALL be visually highlighted
5. WHEN dragging a tab THEN it SHALL reorder within the tab strip
6. WHEN a file name is long THEN tab title SHALL truncate with tooltip showing full name
7. WHEN tabs exceed screen width THEN scroll buttons SHALL appear

### Requirement 7: Performance and Resource Management

**User Story:** As a user, I want multiple tabs to be responsive, so that the app doesn't slow down with many documents open.

#### Acceptance Criteria

1. WHEN opening a new tab THEN it SHALL load in < 1 second
2. WHEN switching tabs THEN the switch SHALL be instant (< 100ms)
3. WHEN 5 tabs are open THEN memory usage SHALL not exceed 1GB
4. WHEN inactive tabs exist THEN they SHALL release rendered page bitmaps
5. WHEN closing a tab THEN all resources SHALL be freed immediately
6. IF memory usage exceeds threshold THEN inactive tabs SHALL aggressively unload

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate concerns: recent files tracking (service), Jump List management (service), tab management (ViewModel)
- **Modular Design**: IRecentFilesService, IJumpListService, TabViewModel are independently testable
- **Dependency Management**: Services depend on abstractions, not concrete implementations
- **Clear Interfaces**: All services expose I*Service interfaces for DI and testing

### Performance
- **Tab Switching**: < 100ms for tab activation
- **Recent Files Loading**: < 50ms on app startup
- **Memory Efficiency**: Inactive tabs release bitmaps, keep only document metadata
- **Jump List Updates**: Async to prevent blocking UI

### Security
- **Input Validation**: Validate all file paths before opening
- **Path Sanitization**: Prevent directory traversal attacks
- **Privacy**: Recent files can be cleared, no telemetry of file names
- **Sandboxing**: All file operations respect MSIX boundaries

### Reliability
- **Error Recovery**: If one tab fails, others remain functional
- **Corrupt State Handling**: Reset recent files if storage corrupted
- **Resource Cleanup**: Dispose documents when tabs close
- **Logging**: All operations logged with correlation IDs

### Usability
- **Keyboard Shortcuts**: Standard tab navigation (Ctrl+Tab, Ctrl+W)
- **Clear Feedback**: Tab titles show file names, tooltips show full paths
- **Intuitive UI**: Tab behavior matches browser conventions
- **Accessibility**: TabView supports screen readers and keyboard navigation
