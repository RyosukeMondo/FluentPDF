# Text Extraction and Search

FluentPDF provides comprehensive text extraction and search capabilities powered by PDFium, enabling users to extract text content, search within documents, highlight matches, and copy text to the clipboard.

## Features

### Text Extraction
- Extract text from individual PDF pages or entire documents
- Preserves paragraph structure and spacing
- Handles Unicode characters correctly (UTF-16)
- Supports multi-column text layouts with proper reading order
- Performance logging for slow extractions (> 2 seconds)

### In-Document Search
- Fast full-text search across all pages
- Case-sensitive and case-insensitive search options
- Whole word matching support
- Returns precise match locations with bounding boxes
- Visual highlighting of search matches on PDF pages
- Match navigation with keyboard shortcuts
- Debounced search (300ms delay) for responsive UI

### Text Selection and Copy
- Click-and-drag text selection
- Copy selected text to clipboard (Ctrl+C)
- Context menu with Copy option
- Preserves line breaks and spacing

## Usage

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Open search panel and focus search box |
| `F3` | Navigate to next match |
| `Shift+F3` | Navigate to previous match |
| `Escape` | Close search panel and clear highlights |
| `Ctrl+C` | Copy selected text to clipboard |

### Search Panel

The search panel appears at the top of the PDF viewer when activated with Ctrl+F:

1. **Search Input**: Type your search query
2. **Previous/Next Buttons**: Navigate between matches
3. **Match Counter**: Shows "X of Y matches"
4. **Case Sensitive Toggle**: Enable/disable case-sensitive matching
5. **Close Button**: Hide the search panel

### Search Options

- **Case Sensitive**: Match text with exact case (Aa checkbox)
- **Whole Word**: Match complete words only (future enhancement)

## Architecture

### Service Layer

#### ITextExtractionService
Interface for text extraction operations:

```csharp
public interface ITextExtractionService
{
    Task<Result<string>> ExtractTextAsync(PdfDocument document, int pageNumber);
    Task<Result<Dictionary<int, string>>> ExtractAllTextAsync(
        PdfDocument document,
        CancellationToken cancellationToken = default);
}
```

#### ITextSearchService
Interface for search operations:

```csharp
public interface ITextSearchService
{
    Task<Result<List<SearchMatch>>> SearchAsync(
        PdfDocument document,
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<List<SearchMatch>>> SearchPageAsync(
        PdfDocument document,
        int pageNumber,
        string query,
        SearchOptions? options = null);
}
```

### Data Models

#### SearchMatch
Represents a single search match with location information:

```csharp
public class SearchMatch
{
    public required int PageNumber { get; init; }
    public required int CharIndex { get; init; }
    public required int Length { get; init; }
    public required string Text { get; init; }
    public required Rect BoundingBox { get; init; } // PDF coordinates
}
```

#### SearchOptions
Configures search behavior:

```csharp
public class SearchOptions
{
    public bool CaseSensitive { get; init; } = false;
    public bool WholeWord { get; init; } = false;
}
```

### PDFium Integration

Text extraction and search use the following PDFium APIs via P/Invoke:

- `FPDFText_LoadPage` - Load text page object
- `FPDFText_ClosePage` - Release text page object
- `FPDFText_CountChars` - Get character count
- `FPDFText_GetText` - Extract text content
- `FPDFText_GetCharBox` - Get character bounding box
- `FPDFText_FindStart` - Initialize search
- `FPDFText_FindNext` - Find next match
- `FPDFText_GetSchResultIndex` - Get match position
- `FPDFText_GetSchCount` - Get match length
- `FPDFText_FindClose` - Release search handle

All text page handles use `SafePdfTextPageHandle` for automatic resource cleanup.

## Performance

### Benchmarks

- **Text Extraction**: < 500ms per page for typical documents
- **Full Document Search**: < 5 seconds for 100-page documents
- **Highlight Rendering**: Maintains 60 FPS (< 16ms per frame)
- **Memory Usage**: < 100MB for full document text storage

### Optimization Strategies

1. **Text Caching**: Extracted text is cached per page to avoid re-extraction
2. **Debounced Search**: 300ms delay after typing stops before initiating search
3. **Cancellation Support**: Long-running searches can be canceled
4. **Performance Logging**: Operations exceeding thresholds are logged for monitoring

## Error Handling

### Error Codes

All text operations return `Result<T>` with structured error codes:

| Error Code | Description | Recovery |
|------------|-------------|----------|
| `TEXT_PAGE_LOAD_FAILED` | Failed to load text page from PDFium | Disable text features for that page |
| `TEXT_EXTRACTION_FAILED` | Text extraction operation failed | Show error in status bar |
| `SEARCH_QUERY_EMPTY` | Empty search query provided | Clear previous highlights |
| `NO_TEXT_FOUND` | Page contains no extractable text | Return empty result (not an error) |

### Logging

Text operations are logged with structured data:

```csharp
// Text extraction
Log.Information("Text extracted from page {PageNumber} in {ElapsedMs}ms",
    pageNumber, elapsed);

// Search performance
Log.Information("Search completed: {MatchCount} matches found in {ElapsedMs}ms",
    matches.Count, elapsed);

// Performance warnings
Log.Warning("Slow text extraction on page {PageNumber}: {ElapsedMs}ms",
    pageNumber, elapsed);
```

## Testing

### Unit Tests

- `TextExtractionServiceTests` - Mock-based tests for text extraction logic
- `TextSearchServiceTests` - Mock-based tests for search logic
- `PdfViewerViewModelSearchTests` - Tests for search UI state management

### Integration Tests

- `TextExtractionIntegrationTests` - Real PDFium text extraction with sample PDFs
- Tests verify Unicode handling, multi-column layouts, and performance

### Architecture Tests

- `TextArchitectureTests` - ArchUnitNET rules enforcing:
  - Text services implement interfaces
  - Core layer doesn't depend on PDFium interop
  - SafeHandle pattern used for text page handles

## Dependencies

### Core Dependencies
- **PDFium**: Native library providing text extraction and search APIs
- **FluentResults**: Result pattern for operation outcomes
- **Serilog**: Structured logging for observability

### Platform Dependencies
- **Windows.ApplicationModel.DataTransfer**: Clipboard integration (Windows)
- **WinUI 3**: UI controls and rendering

## Future Enhancements

Potential improvements for future releases:

1. **Advanced Search**
   - Regular expression search
   - Fuzzy matching
   - Search within selection

2. **Text Analysis**
   - Word frequency analysis
   - Table of contents extraction
   - Metadata extraction

3. **Export Options**
   - Export text to plain text file
   - Export search results to CSV
   - Batch text extraction CLI

4. **Performance**
   - Lazy page-by-page search with progress indicator
   - Background text indexing for large documents
   - Incremental search result updates

## See Also

- [Architecture Documentation](ARCHITECTURE.md) - System architecture overview
- [Features Documentation](FEATURES.md) - Complete feature list
- [Performance Guide](PERFORMANCE.md) - Performance optimization details
- [Testing Guide](TESTING.md) - Testing strategy and guidelines
