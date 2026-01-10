# Requirements Document

## Introduction

The Text Extraction and Search feature enables FluentPDF to extract text content from PDF documents and provide powerful in-document search capabilities with match highlighting. This spec integrates PDFium's text extraction APIs via P/Invoke to provide fast, accurate text extraction while maintaining the architectural patterns established in the pdf-viewer-core spec.

The feature provides:
- **Accurate Text Extraction**: Extract text from PDF pages preserving structure and formatting
- **Fast Search**: Find text within documents with case-sensitive and case-insensitive options
- **Match Highlighting**: Visually highlight search matches with bounding rectangles
- **Clipboard Integration**: Copy selected text to clipboard
- **Observable Operations**: Structured logging for text extraction and search performance

## Alignment with Product Vision

This spec supports the product goal of providing essential PDF viewing features with professional quality.

Supports product principles:
- **Quality Over Features**: Uses PDFium's proven text extraction APIs for accurate results
- **Verifiable Architecture**: Extends existing PDFium P/Invoke layer with SafeHandle patterns
- **Observable System**: Performance logging for text extraction and search operations
- **Standards Compliance**: Correctly handles Unicode, RTL text, and PDF text encoding

Aligns with tech decisions:
- PDFium P/Invoke for text extraction
- FluentResults for operation errors
- WinUI 3 for search UI
- Dependency injection for services

## Requirements

### Requirement 1: PDFium Text Extraction P/Invoke

**User Story:** As a developer, I want PDFium text extraction functions integrated, so that I can extract text from PDF pages programmatically.

#### Acceptance Criteria

1. WHEN PdfiumInterop is extended THEN it SHALL add P/Invoke declarations for: FPDFText_LoadPage, FPDFText_CountChars, FPDFText_GetText, FPDFText_ClosePage
2. WHEN text extraction functions are declared THEN they SHALL use SafeHandle for text page handles
3. WHEN FPDFText_LoadPage is called THEN it SHALL return SafePdfTextPageHandle
4. WHEN text is extracted THEN it SHALL correctly handle Unicode characters (UTF-16)
5. WHEN FPDFText_GetText is called THEN it SHALL return complete text without truncation
6. WHEN text page is closed THEN SafeHandle SHALL automatically dispose via FPDFText_ClosePage
7. IF text extraction fails THEN it SHALL return Result.Fail with error code "TEXT_EXTRACTION_FAILED"

### Requirement 2: Text Extraction Service

**User Story:** As a user, I want to extract text from PDF pages, so that I can copy, search, or process document content.

#### Acceptance Criteria

1. WHEN a page is provided THEN the service SHALL extract all text content from that page
2. WHEN text is extracted THEN it SHALL preserve paragraph structure and spacing
3. WHEN text contains multiple columns THEN it SHALL extract in reading order (left-to-right, top-to-bottom)
4. WHEN extraction completes THEN it SHALL return Result.Ok with extracted text string
5. WHEN extraction fails THEN it SHALL return Result.Fail with appropriate error code
6. IF a page has no text THEN it SHALL return empty string (not error)
7. WHEN extraction takes > 2 seconds THEN it SHALL log performance warning
8. WHEN extracting from multiple pages THEN it SHALL support async batch extraction

### Requirement 3: Text Search Service

**User Story:** As a user, I want to search for text within a PDF, so that I can quickly find relevant information.

#### Acceptance Criteria

1. WHEN a search query is provided THEN the service SHALL find all matches in the document
2. WHEN search is case-insensitive THEN it SHALL match regardless of case (default behavior)
3. WHEN search is case-sensitive THEN it SHALL match exact case only
4. WHEN matches are found THEN it SHALL return list of SearchMatch objects with: PageNumber, CharIndex, Text, BoundingBox
5. WHEN no matches are found THEN it SHALL return empty list (not error)
6. WHEN search query is empty THEN it SHALL return Result.Fail with validation error
7. WHEN searching large documents THEN it SHALL support cancellation via CancellationToken
8. WHEN search completes THEN it SHALL log search time and match count

### Requirement 4: Search Match Bounding Boxes

**User Story:** As a user, I want search matches to be highlighted visually, so that I can see where matches occur on the page.

#### Acceptance Criteria

1. WHEN text is found THEN PDFium SHALL return character bounding boxes via FPDFText_GetCharBox
2. WHEN multiple characters form a match THEN bounding boxes SHALL be combined into single rectangle
3. WHEN a match spans multiple lines THEN it SHALL create multiple bounding rectangles
4. WHEN bounding boxes are calculated THEN they SHALL be in PDF page coordinates (points)
5. WHEN displaying highlights THEN bounding boxes SHALL be transformed to screen coordinates
6. WHEN zoom level changes THEN highlights SHALL scale correctly
7. WHEN page is rotated THEN highlights SHALL transform correctly

### Requirement 5: Search UI Integration

**User Story:** As a user, I want a search toolbar in the PDF viewer, so that I can search within documents easily.

#### Acceptance Criteria

1. WHEN viewer opens THEN it SHALL show a search icon/button in toolbar
2. WHEN user clicks search button THEN it SHALL show search panel (TextBox + Find Next/Previous buttons)
3. WHEN user types in search box THEN it SHALL initiate search after short delay (debounce 300ms)
4. WHEN matches are found THEN it SHALL show "X of Y matches" indicator
5. WHEN user clicks "Find Next" THEN it SHALL navigate to next match
6. WHEN user clicks "Find Previous" THEN it SHALL navigate to previous match
7. WHEN user presses Ctrl+F THEN it SHALL show search panel and focus search box
8. WHEN user presses F3 THEN it SHALL find next match
9. WHEN user presses Shift+F3 THEN it SHALL find previous match
10. WHEN user presses Escape THEN it SHALL hide search panel and clear highlights
11. WHEN search panel is open THEN it SHALL show case-sensitive toggle checkbox
12. WHEN navigating to a match THEN it SHALL scroll page to ensure match is visible

### Requirement 6: Text Selection and Copy

**User Story:** As a user, I want to select text with my mouse and copy it to clipboard, so that I can use PDF content in other applications.

#### Acceptance Criteria

1. WHEN user clicks and drags on PDF page THEN it SHALL highlight selected text
2. WHEN text is selected THEN it SHALL show selection highlight (semi-transparent background)
3. WHEN user presses Ctrl+C THEN it SHALL copy selected text to clipboard
4. WHEN user right-clicks selected text THEN it SHALL show context menu with "Copy" option
5. WHEN text is copied THEN it SHALL preserve spaces and line breaks
6. WHEN selection spans multiple lines THEN it SHALL include line breaks in copied text
7. WHEN user clicks elsewhere THEN it SHALL clear text selection
8. WHEN text extraction is in progress THEN selection SHALL be disabled with "Loading..." indicator

### Requirement 7: Performance Requirements

**User Story:** As a user, I want text extraction and search to be fast, so that I can work efficiently with large documents.

#### Acceptance Criteria

1. WHEN extracting text from a page THEN it SHALL complete in < 500ms for typical pages
2. WHEN searching a 100-page document THEN it SHALL complete in < 5 seconds
3. WHEN highlighting matches THEN rendering SHALL not degrade below 30 FPS
4. WHEN extracting text from entire document THEN memory usage SHALL not exceed 100MB for text storage
5. IF search takes > 10 seconds THEN it SHALL show progress indicator
6. WHEN search is canceled THEN it SHALL abort within 1 second

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate text extraction, search logic, and UI concerns
- **Modular Design**: ITextExtractionService, ITextSearchService independently testable
- **Dependency Management**: Services depend on abstractions, not concrete PDFium implementations
- **Clear Interfaces**: All services expose I*Service interfaces for DI and mocking

### Performance
- **Text Extraction**: < 500ms per page for typical documents
- **Search Speed**: < 5 seconds for 100-page documents
- **Highlight Rendering**: No FPS degradation (< 16ms per frame)
- **Memory Efficiency**: < 100MB for full document text storage

### Security
- **Input Validation**: Sanitize search queries to prevent regex injection (if using regex)
- **Memory Safety**: Use SafeHandle for text page handles
- **Clipboard Security**: Only copy text, never executable content

### Reliability
- **Error Recovery**: If text extraction fails on one page, continue with other pages
- **Resource Cleanup**: Ensure text page handles disposed even on errors
- **Cancellation Support**: Properly handle CancellationToken in search operations

### Usability
- **Search Feedback**: Show match count and current match index
- **Keyboard Shortcuts**: Ctrl+F, F3, Shift+F3, Ctrl+C
- **Visual Feedback**: Clear highlight colors for selection and search matches
- **Accessibility**: Screen reader support for search results and navigation
