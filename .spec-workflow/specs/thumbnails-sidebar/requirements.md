# Requirements Document

## Introduction

The Thumbnails Sidebar spec implements a visual navigation interface for PDF documents, displaying low-resolution page previews in a vertical sidebar. This feature significantly improves document navigation UX by providing visual context and enabling quick jumps to any page through thumbnail clicks.

The thumbnails sidebar provides:
- **Low-Resolution Rendering**: Generate thumbnail previews at reduced resolution (150x200px typical) for fast rendering
- **Sidebar Navigation UI**: Vertical scrollable list of thumbnails in WinUI 3 sidebar
- **Jump to Page**: Click thumbnail to navigate directly to that page
- **LRU Cache**: Memory-efficient caching of thumbnail bitmaps (max 100 thumbnails in memory)

This directly supports the product principle **"Respect User Resources"** through efficient thumbnail caching and the goal of delivering "high-quality rendering" by maintaining visual fidelity even at reduced resolutions.

## Alignment with Product Vision

Aligns with product objectives:
- **Quality Over Features**: Thumbnails maintain visual quality while being memory-efficient
- **Respect User Resources**: LRU cache prevents memory bloat with large documents
- **Responsive UI**: Thumbnails load asynchronously without blocking main UI

Supports success metrics:
- **Performance**: Thumbnail rendering < 200ms per page (at low resolution)
- **Memory usage**: Thumbnail cache < 50MB for typical 100-page document
- **User experience**: Visual navigation improves document browsing efficiency

## Requirements

### Requirement 1: Thumbnail Rendering Service

**User Story:** As a developer, I want a thumbnail rendering service, so that I can generate low-resolution page previews efficiently.

#### Acceptance Criteria

1. WHEN rendering thumbnail THEN it SHALL use PDFium with reduced DPI (96 → 48 DPI)
2. WHEN rendering thumbnail THEN output dimensions SHALL be approximately 150x200 pixels
3. WHEN rendering thumbnail THEN it SHALL maintain aspect ratio of original page
4. WHEN thumbnail rendering completes THEN it SHALL return BitmapImage for WinUI display
5. IF thumbnail rendering fails THEN it SHALL return placeholder image (gray rectangle with page number)
6. WHEN rendering multiple thumbnails THEN they SHALL render asynchronously in background
7. WHEN thumbnail is rendered THEN it SHALL be added to LRU cache
8. WHEN cache is full (100 items) THEN it SHALL evict least recently used thumbnail

### Requirement 2: LRU Thumbnail Cache

**User Story:** As a developer, I want an LRU cache for thumbnails, so that memory usage remains bounded with large documents.

#### Acceptance Criteria

1. WHEN cache is initialized THEN it SHALL have configurable max capacity (default 100)
2. WHEN adding thumbnail to cache THEN it SHALL use page number as key
3. WHEN retrieving thumbnail THEN it SHALL mark as recently used (move to front)
4. WHEN cache reaches capacity THEN it SHALL evict least recently used item
5. WHEN thumbnail is evicted THEN it SHALL dispose BitmapImage to free memory
6. WHEN document is closed THEN cache SHALL be cleared and all bitmaps disposed
7. WHEN cache contains thumbnail THEN retrieval SHALL be O(1) via dictionary lookup

### Requirement 3: Thumbnails Sidebar UI

**User Story:** As a user, I want a sidebar showing page thumbnails, so that I can visualize document structure and navigate quickly.

#### Acceptance Criteria

1. WHEN viewer opens document THEN sidebar SHALL display on left side of content area
2. WHEN sidebar is visible THEN it SHALL show vertical scrollable list of thumbnails
3. WHEN thumbnail is displayed THEN it SHALL show:
   - Thumbnail image (150x200px)
   - Page number (centered below image)
   - Border highlight if page is currently selected
4. WHEN user scrolls sidebar THEN visible thumbnails SHALL load asynchronously
5. WHEN thumbnails are loading THEN placeholders SHALL show (gray boxes with page numbers)
6. WHEN sidebar width changes THEN thumbnails SHALL maintain aspect ratio
7. WHEN document has > 100 pages THEN sidebar SHALL use virtualization for performance

### Requirement 4: Thumbnail Click Navigation

**User Story:** As a user, I want to click a thumbnail to jump to that page, so that I can navigate quickly.

#### Acceptance Criteria

1. WHEN user clicks thumbnail THEN viewer SHALL navigate to that page
2. WHEN navigation occurs THEN main viewer SHALL render full-resolution page
3. WHEN current page changes THEN corresponding thumbnail SHALL be highlighted
4. WHEN navigation is in progress THEN thumbnail click SHALL be disabled (prevent double-click issues)
5. WHEN clicked thumbnail is not visible in sidebar THEN sidebar SHALL scroll to show it
6. WHEN page loads THEN highlight animation SHALL briefly flash to indicate selection

### Requirement 5: Sidebar Toggle and Visibility

**User Story:** As a user, I want to show/hide the thumbnails sidebar, so that I can maximize content area when needed.

#### Acceptance Criteria

1. WHEN viewer opens THEN sidebar SHALL be visible by default
2. WHEN user clicks "Toggle Thumbnails" button THEN sidebar SHALL show/hide
3. WHEN sidebar is hidden THEN content area SHALL expand to full width
4. WHEN sidebar is shown THEN content area SHALL shrink to accommodate sidebar
5. WHEN toggling sidebar THEN animation SHALL smoothly resize content area
6. WHEN sidebar is hidden THEN thumbnail rendering SHALL pause
7. WHEN sidebar is shown again THEN thumbnail rendering SHALL resume

### Requirement 6: Thumbnail ViewModel and State Management

**User Story:** As a developer, I want a ViewModel managing thumbnail state, so that UI and business logic are properly separated.

#### Acceptance Criteria

1. WHEN ViewModel is initialized THEN it SHALL create thumbnail cache
2. WHEN document loads THEN ViewModel SHALL trigger thumbnail generation for visible pages
3. WHEN user scrolls sidebar THEN ViewModel SHALL load thumbnails for newly visible pages
4. WHEN current page changes THEN ViewModel SHALL update selected thumbnail property
5. WHEN ViewModel is disposed THEN it SHALL clear cache and dispose all bitmaps
6. WHEN thumbnail rendering fails THEN ViewModel SHALL log error and show placeholder
7. WHEN thumbnail completes THEN ViewModel SHALL notify UI via property change

### Requirement 7: Performance and Memory Management

**User Story:** As a developer, I want efficient thumbnail rendering and memory management, so that the sidebar remains responsive with large documents.

#### Acceptance Criteria

1. WHEN rendering thumbnails THEN it SHALL use background threads (Task.Run)
2. WHEN rendering multiple thumbnails THEN it SHALL limit concurrent renders (max 4 parallel)
3. WHEN thumbnails render THEN P99 latency SHALL be < 200ms per thumbnail
4. WHEN cache holds 100 thumbnails THEN memory usage SHALL be < 50MB
5. WHEN document has 1000+ pages THEN sidebar SHALL use UI virtualization
6. WHEN thumbnails are disposed THEN memory SHALL be reclaimed (verified via MemoryDiagnoser)
7. WHEN thumbnail rendering is slow THEN it SHALL not block main UI thread

### Requirement 8: Sidebar Accessibility

**User Story:** As a user, I want the thumbnails sidebar to be accessible, so that all users can navigate efficiently.

#### Acceptance Criteria

1. WHEN sidebar is visible THEN thumbnails SHALL be keyboard navigable (Tab, Arrow keys)
2. WHEN thumbnail has focus THEN it SHALL show visual focus indicator
3. WHEN user presses Enter on focused thumbnail THEN it SHALL navigate to that page
4. WHEN thumbnails load THEN they SHALL have accessible names ("Page 1 thumbnail", etc.)
5. WHEN sidebar is hidden THEN screen readers SHALL announce "Thumbnails hidden"
6. WHEN sidebar is shown THEN screen readers SHALL announce "Thumbnails visible"

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate thumbnail rendering (service), caching (LRU cache), UI (sidebar control)
- **Modular Design**: ThumbnailRenderingService, LruThumbnailCache, ThumbnailsSidebar are independently testable
- **Dependency Management**: Sidebar depends on IThumbnailRenderingService via DI
- **Clear Interfaces**: IThumbnailRenderingService provides abstraction for rendering

### Performance
- **Thumbnail Rendering**: P99 latency < 200ms per thumbnail (at 48 DPI)
- **Cache Access**: O(1) retrieval time via dictionary lookup
- **Memory Efficiency**: Cache size < 50MB for 100 thumbnails
- **UI Responsiveness**: Sidebar scrolling at 60 FPS without frame drops

### Security
- **Memory Safety**: All BitmapImage objects properly disposed
- **Resource Limits**: Cache capacity prevents unbounded memory growth

### Reliability
- **Error Recovery**: Placeholder images shown if thumbnail rendering fails
- **Memory Management**: LRU cache prevents memory leaks through automatic eviction
- **Disposal Pattern**: All resources cleaned up when document is closed

### Usability
- **Visual Feedback**: Loading placeholders and smooth animations
- **Keyboard Navigation**: Full keyboard support for accessibility
- **Responsive Design**: Sidebar adapts to window size changes
