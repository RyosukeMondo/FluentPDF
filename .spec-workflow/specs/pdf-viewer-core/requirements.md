# Requirements Document

## Introduction

The PDF Viewer Core implements the fundamental viewing capabilities for FluentPDF. This spec delivers the core value proposition: the ability to open, render, and navigate PDF documents using enterprise-grade PDFium rendering engine. It provides a solid foundation for all future PDF viewing features while maintaining the quality standards established in the project foundation.

The viewer enables:
- **Fast PDF Loading**: Open PDF documents with error handling and validation
- **High-Quality Rendering**: Render PDF pages using PDFium with configurable DPI
- **Smooth Navigation**: Navigate between pages with keyboard shortcuts and UI controls
- **Flexible Viewing**: Zoom in/out with multiple preset zoom levels
- **Observable Operations**: All operations logged with correlation IDs for debugging

## Alignment with Product Vision

This spec directly delivers the core product promise - viewing PDF files on Windows with enterprise quality.

Supports product principles:
- **Quality Over Features**: Uses PDFium (Google's proven PDF engine) with comprehensive error handling
- **Verifiable Architecture**: All components testable, Result pattern for failures, ArchUnitNET rules extended
- **Observable System**: Structured logging for all rendering operations with performance metrics
- **Standards Compliance**: PDF 1.7 and PDF 2.0 support via PDFium

Aligns with tech decisions:
- WinUI 3 with MVVM for UI layer
- FluentResults for PDF loading errors
- Serilog for performance monitoring
- Dependency injection for all services

## Requirements

### Requirement 1: PDFium Native Library Integration

**User Story:** As a developer, I want PDFium native library properly integrated via P/Invoke, so that I can reliably call PDF rendering functions from managed code.

#### Acceptance Criteria

1. WHEN the build-libs.ps1 script is run THEN it SHALL download and compile PDFium via vcpkg
2. WHEN PDFium DLLs are placed in libs/x64/bin/ THEN they SHALL be copied to the app's output directory during build
3. WHEN the app starts THEN it SHALL successfully load pdfium.dll from the application directory
4. WHEN P/Invoke declares PDFium functions THEN they SHALL use SafeHandle for pointer safety
5. WHEN PDFium functions are called THEN error codes SHALL be checked and converted to PdfError
6. IF PDFium initialization fails THEN the app SHALL log detailed error and show user-friendly message
7. WHEN the app closes THEN it SHALL call FPDF_DestroyLibrary() to clean up PDFium resources

### Requirement 2: PDF Document Loading Service

**User Story:** As a user, I want to open PDF files from disk, so that I can view their contents.

#### Acceptance Criteria

1. WHEN a user selects "Open File" THEN a file picker SHALL show filtering for *.pdf files
2. WHEN a PDF file is selected THEN the service SHALL validate it is a valid PDF file
3. WHEN loading a valid PDF THEN the service SHALL return Result.Ok with PdfDocument model
4. WHEN loading an invalid file THEN the service SHALL return Result.Fail with PdfError(ErrorCategory.Validation)
5. WHEN loading a password-protected PDF THEN the service SHALL return Result.Fail with error code "PDF_REQUIRES_PASSWORD"
6. WHEN loading a corrupted PDF THEN the service SHALL return Result.Fail with error code "PDF_CORRUPTED"
7. WHEN loading fails THEN the error SHALL include file path in Context dictionary
8. IF a document is already loaded THEN opening a new document SHALL dispose the previous document first
9. WHEN a document is disposed THEN all PDFium resources (pages, bitmaps) SHALL be freed

### Requirement 3: PDF Page Rendering Service

**User Story:** As a user, I want to see PDF pages rendered at high quality, so that I can read documents clearly.

#### Acceptance Criteria

1. WHEN a page is requested for rendering THEN it SHALL render at the current zoom level (default 100%)
2. WHEN rendering a page THEN it SHALL use the specified DPI (default 96 DPI for Windows)
3. WHEN rendering completes THEN it SHALL return a BitmapImage for display in WinUI
4. WHEN rendering a page THEN it SHALL use PDFium's FPDF_RenderPageBitmap with antialiasing enabled
5. WHEN a page number is invalid THEN it SHALL return Result.Fail with PdfError(ErrorCategory.Validation)
6. WHEN rendering fails THEN it SHALL log error with correlation ID and page number
7. IF rendering takes > 2 seconds THEN it SHALL log performance warning
8. WHEN rendering completes THEN it SHALL dispose intermediate bitmaps to prevent memory leaks
9. WHEN zoom level changes THEN the current page SHALL be re-rendered at the new zoom level

### Requirement 4: PDF Viewer ViewModel and UI

**User Story:** As a user, I want a clean UI to view PDF pages, so that I can focus on the document content.

#### Acceptance Criteria

1. WHEN the viewer opens THEN it SHALL show a toolbar with: Open, Previous Page, Next Page, Zoom controls
2. WHEN no document is loaded THEN the viewer SHALL show "Open a PDF file to get started" message
3. WHEN a document is loaded THEN it SHALL display page 1 by default
4. WHEN the current page is displayed THEN the UI SHALL show "Page X of Y" indicator
5. WHEN user clicks "Next Page" THEN it SHALL advance to page+1 (if not at last page)
6. WHEN user clicks "Previous Page" THEN it SHALL go to page-1 (if not at first page)
7. WHEN user presses Right Arrow key THEN it SHALL advance to next page
8. WHEN user presses Left Arrow key THEN it SHALL go to previous page
9. IF the user is on the last page THEN "Next Page" button SHALL be disabled
10. IF the user is on the first page THEN "Previous Page" button SHALL be disabled
11. WHEN a page is loading THEN it SHALL show a loading spinner over the page area
12. WHEN an error occurs THEN it SHALL show error dialog with correlation ID

### Requirement 5: Zoom Functionality

**User Story:** As a user, I want to zoom in and out of PDF pages, so that I can read small text or see the overall layout.

#### Acceptance Criteria

1. WHEN the viewer loads THEN the default zoom SHALL be 100% (actual size)
2. WHEN user clicks "Zoom In" THEN zoom SHALL increase by one level (100% → 125% → 150% → 175% → 200%)
3. WHEN user clicks "Zoom Out" THEN zoom SHALL decrease by one level (200% → 175% → 150% → 125% → 100% → 75% → 50%)
4. WHEN zoom level changes THEN the current page SHALL re-render immediately
5. WHEN zoom is at maximum (200%) THEN "Zoom In" button SHALL be disabled
6. WHEN zoom is at minimum (50%) THEN "Zoom Out" button SHALL be disabled
7. WHEN user presses Ctrl+Plus THEN it SHALL zoom in
8. WHEN user presses Ctrl+Minus THEN it SHALL zoom out
9. WHEN user presses Ctrl+0 THEN zoom SHALL reset to 100%
10. WHEN zoom changes THEN the UI SHALL display current zoom percentage (e.g., "150%")

### Requirement 6: Navigation and Document State

**User Story:** As a user, I want to navigate to specific pages, so that I can quickly jump to sections of interest.

#### Acceptance Criteria

1. WHEN the UI shows a page number input THEN user SHALL be able to type a page number
2. WHEN user enters a valid page number and presses Enter THEN it SHALL navigate to that page
3. WHEN user enters an invalid page number (< 1 or > page count) THEN it SHALL show validation error
4. WHEN a document is loaded THEN the ViewModel SHALL expose: CurrentPageNumber, TotalPages, ZoomLevel properties
5. WHEN any property changes THEN INotifyPropertyChanged SHALL fire for data binding
6. WHEN switching between documents THEN all state (page number, zoom) SHALL reset to defaults
7. IF navigation is in progress THEN additional navigation requests SHALL be queued

### Requirement 7: Performance and Resource Management

**User Story:** As a user, I want PDF rendering to be fast and responsive, so that I can navigate smoothly through documents.

#### Acceptance Criteria

1. WHEN opening a PDF THEN it SHALL load metadata in < 500ms for files < 10MB
2. WHEN rendering a page THEN it SHALL complete in < 1 second for standard pages (8.5x11 at 96 DPI)
3. WHEN navigating between pages THEN it SHALL render the new page in < 1 second
4. WHEN the app is idle THEN memory usage SHALL not exceed 200MB for a single 50-page document
5. WHEN disposing a document THEN all unmanaged resources SHALL be freed immediately
6. WHEN rendering fails THEN it SHALL not leak PDFium handles or bitmaps
7. IF a rendering operation exceeds 5 seconds THEN it SHALL log performance warning with page details

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate concerns: P/Invoke (low-level), Service (business logic), ViewModel (presentation)
- **Modular Design**: PdfiumInterop, PdfDocumentService, PdfRenderingService, PdfViewerViewModel are independently testable
- **Dependency Management**: Services depend on IPdfiumInterop abstraction, ViewModels depend on service interfaces
- **Clear Interfaces**: All services expose I*Service interfaces for DI and testing

### Performance
- **Startup Time**: PDFium initialization < 100ms
- **Page Rendering**: < 1 second for standard pages at 100% zoom
- **Memory Efficiency**: Use double-buffering and dispose bitmaps immediately after rendering
- **Async Operations**: All file I/O and rendering async to prevent UI blocking

### Security
- **Input Validation**: Validate all file paths and page numbers before passing to PDFium
- **Memory Safety**: Use SafeHandle for all native pointers
- **Error Handling**: Never expose raw PDFium errors to users; always wrap in PdfError with sanitized messages
- **Sandboxing**: All file operations respect MSIX ApplicationData boundaries

### Reliability
- **Error Recovery**: If one page fails to render, other pages should still work
- **Resource Cleanup**: Ensure Dispose pattern is implemented for all PDFium resources
- **Logging**: All operations logged with correlation IDs for debugging
- **Crash Prevention**: Global exception handlers catch unhandled errors in rendering pipeline

### Usability
- **Responsive UI**: All long operations (load, render) show progress indicators
- **Keyboard Shortcuts**: Support standard shortcuts (Ctrl+O, Ctrl+Plus, Ctrl+Minus, Arrow keys)
- **Clear Feedback**: Show meaningful error messages when operations fail
- **Accessibility**: UI controls labeled for screen readers, keyboard navigation supported
