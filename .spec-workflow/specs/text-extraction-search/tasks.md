# Tasks Document

## Implementation Tasks

- [x] 1. Extend PdfiumInterop with text extraction P/Invoke declarations
  - Files:
    - `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs` (extend)
    - `src/FluentPDF.Rendering/Interop/SafePdfTextPageHandle.cs`
    - `tests/FluentPDF.Rendering.Tests/Interop/PdfiumTextInteropTests.cs`
  - Add P/Invoke declarations: FPDFText_LoadPage, FPDFText_ClosePage, FPDFText_CountChars, FPDFText_GetText, FPDFText_GetCharBox
  - Create SafePdfTextPageHandle with automatic disposal
  - Add helper methods for UTF-16 text extraction
  - Write unit tests verifying text extraction with sample PDF
  - Purpose: Provide safe P/Invoke layer for PDFium text APIs
  - _Leverage: Existing PdfiumInterop, SafeHandle pattern_
  - _Requirements: 1.1-1.7_
  - _Prompt: Role: Native Interop Developer | Task: Extend PdfiumInterop with text extraction P/Invoke using SafeHandle pattern | Restrictions: Must handle UTF-16 encoding, use SafeHandle, follow existing interop patterns | Success: Can extract text from PDF pages using PDFium text APIs_

- [x] 2. Implement TextExtractionService for page text extraction
  - Files:
    - `src/FluentPDF.Core/Services/ITextExtractionService.cs`
    - `src/FluentPDF.Rendering/Services/TextExtractionService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/TextExtractionServiceTests.cs`
  - Create ITextExtractionService interface
  - Implement ExtractTextAsync(document, pageNumber)
  - Implement ExtractAllTextAsync with cancellation support
  - Add text caching to avoid re-extraction
  - Add performance logging for slow extractions
  - Write unit tests with mocked PdfiumInterop
  - Purpose: Provide business logic for text extraction
  - _Leverage: PdfiumInterop text APIs, Result<T> pattern_
  - _Requirements: 2.1-2.8_
  - _Prompt: Role: Backend Service Developer | Task: Implement TextExtractionService using PDFium text APIs with caching and performance monitoring | Restrictions: Must handle Unicode, log slow operations, support cancellation | Success: Can extract text from pages with proper error handling and caching_

- [x] 3. Add search P/Invoke declarations to PdfiumInterop
  - Files:
    - `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs` (extend)
    - `tests/FluentPDF.Rendering.Tests/Interop/PdfiumSearchInteropTests.cs`
  - Add P/Invoke declarations: FPDFText_FindStart, FPDFText_FindNext, FPDFText_GetSchResultIndex, FPDFText_GetSchCount, FPDFText_FindClose
  - Add search flag constants: FPDF_MATCHCASE, FPDF_MATCHWHOLEWORD
  - Write unit tests verifying search with known text
  - Purpose: Provide P/Invoke layer for PDFium search APIs
  - _Leverage: Existing PdfiumInterop_
  - _Requirements: 3.1-3.8, 4.1-4.7_
  - _Prompt: Role: Native Interop Developer | Task: Add PDFium search P/Invoke declarations with proper flag handling | Restrictions: Must handle search handles, use correct flags, follow interop patterns | Success: Can search for text using PDFium search APIs_

- [x] 4. Create SearchMatch model and SearchOptions
  - Files:
    - `src/FluentPDF.Core/Models/SearchMatch.cs`
    - `src/FluentPDF.Core/Models/SearchOptions.cs`
    - `tests/FluentPDF.Core.Tests/Models/SearchMatchTests.cs`
  - Create SearchMatch with PageNumber, CharIndex, Length, Text, BoundingBox properties
  - Create SearchOptions with CaseSensitive and WholeWord flags
  - Add coordinate transformation helpers
  - Write unit tests for models
  - Purpose: Provide domain models for search functionality
  - _Leverage: Existing model patterns_
  - _Requirements: 3.4, 4.1-4.7_
  - _Prompt: Role: Domain Modeling Developer | Task: Create SearchMatch and SearchOptions models with proper validation | Restrictions: Keep models immutable, add coordinate helpers | Success: Models represent search results with location and bounding boxes_

- [x] 5. Implement TextSearchService with PDFium search APIs
  - Files:
    - `src/FluentPDF.Core/Services/ITextSearchService.cs`
    - `src/FluentPDF.Rendering/Services/TextSearchService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/TextSearchServiceTests.cs`
  - Create ITextSearchService interface
  - Implement SearchAsync(document, query, options) searching all pages
  - Implement SearchPageAsync for single page search
  - Calculate bounding boxes by combining character boxes
  - Add cancellation support for long searches
  - Add performance logging
  - Write comprehensive unit tests
  - Purpose: Provide in-document search functionality
  - _Leverage: ITextExtractionService, PdfiumInterop search APIs_
  - _Requirements: 3.1-3.8, 4.1-4.7_
  - _Prompt: Role: Search Algorithm Developer | Task: Implement TextSearchService using PDFium search APIs with bounding box calculation | Restrictions: Must support case sensitivity, handle multi-line matches, support cancellation | Success: Can search documents and return matches with accurate bounding boxes_

- [x] 6. Extend PdfViewerViewModel with search commands and state
  - Files:
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (extend)
    - `tests/FluentPDF.App.Tests/ViewModels/PdfViewerViewModelSearchTests.cs`
  - Add observable properties: IsSearchPanelVisible, SearchQuery, SearchMatches, CurrentMatchIndex, IsSearching, CaseSensitive
  - Add commands: ToggleSearchPanel, Search, GoToNextMatch, GoToPreviousMatch
  - Implement debounced search (300ms delay)
  - Implement match navigation with page scrolling
  - Add search progress indicator
  - Write unit tests for search workflow
  - Purpose: Provide search presentation logic
  - _Leverage: Existing PdfViewerViewModel, ITextSearchService_
  - _Requirements: 5.1-5.12_
  - _Prompt: Role: WinUI MVVM Developer | Task: Extend PdfViewerViewModel with search state and commands including debounced search and match navigation | Restrictions: Must follow MVVM, be UI-agnostic, handle async properly | Success: ViewModel provides complete search workflow with navigation_

- [x] 7. Create search panel UI in PdfViewerPage
  - Files:
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml` (extend)
  - Add search panel with TextBox, Previous/Next buttons, match counter, case-sensitive checkbox
  - Add keyboard accelerators: Ctrl+F, F3, Shift+F3, Escape
  - Bind all controls to ViewModel properties and commands
  - Add visibility binding for search panel
  - Implement focus management (focus search box on Ctrl+F)
  - Purpose: Provide search UI in PDF viewer
  - _Leverage: Existing PdfViewerPage, WinUI 3 controls_
  - _Requirements: 5.1-5.12_
  - _Prompt: Role: WinUI Frontend Developer | Task: Add search panel UI to PdfViewerPage with keyboard shortcuts and data binding | Restrictions: Must use data binding, follow Fluent Design, ensure accessibility | Success: Search panel integrates seamlessly with viewer UI_

- [x] 8. Implement highlight overlay for search matches
  - Files:
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml` (add Canvas overlay)
    - `src/FluentPDF.App/Helpers/CoordinateTransformHelper.cs`
  - Add Canvas overlay above PDF image
  - Implement coordinate transformation from PDF to screen coordinates
  - Render semi-transparent rectangles for search matches
  - Highlight current match with distinct color
  - Update highlights when zoom/scroll changes
  - Handle multi-line matches with multiple rectangles
  - Purpose: Visually show search match locations
  - _Leverage: WinUI Canvas, SearchMatch bounding boxes_
  - _Requirements: 4.1-4.7, 5.12_
  - _Prompt: Role: Graphics UI Developer | Task: Implement highlight overlay rendering search matches with coordinate transformation | Restrictions: Must transform coordinates correctly, handle zoom/rotation, use distinct colors | Success: Search matches visually highlighted on PDF pages_

- [ ] 9. Implement text selection and clipboard copy
  - Files:
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (extend)
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml` (add selection handling)
  - Add mouse event handling for text selection (click-drag)
  - Extract selected text using TextExtractionService
  - Implement Ctrl+C to copy to clipboard
  - Add right-click context menu with Copy option
  - Clear selection on click elsewhere
  - Purpose: Enable text copying from PDFs
  - _Leverage: ITextExtractionService, Windows.ApplicationModel.DataTransfer_
  - _Requirements: 6.1-6.8_
  - _Prompt: Role: UI Interaction Developer | Task: Implement text selection with mouse drag and clipboard copy functionality | Restrictions: Must preserve line breaks, use proper clipboard API, handle selection clearing | Success: Can select and copy text from PDF pages_

- [ ] 10. Register text services in DI container
  - Files:
    - `src/FluentPDF.App/App.xaml.cs` (modify)
  - Register ITextExtractionService and implementation
  - Register ITextSearchService and implementation
  - Verify service dependencies resolve correctly
  - Purpose: Wire up text extraction and search services
  - _Leverage: Existing IHost DI container_
  - _Requirements: All integration_
  - _Prompt: Role: Application Integration Engineer | Task: Register text extraction and search services in DI container | Restrictions: Follow existing DI patterns, use appropriate service lifetimes | Success: All text services registered and resolvable_

- [ ] 11. Add integration tests for text extraction and search
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Integration/TextExtractionIntegrationTests.cs`
    - `tests/Fixtures/sample-with-text.pdf` (add PDF with known text)
  - Create integration tests using real PDFium
  - Test text extraction from pages with known content
  - Test search finds all expected matches
  - Verify bounding boxes are correct
  - Test Unicode and special characters
  - Test performance with large documents
  - Purpose: Verify text functionality with real PDFium
  - _Leverage: Real PDFium, sample PDFs_
  - _Requirements: All functional requirements_
  - _Prompt: Role: QA Integration Engineer | Task: Create integration tests for text extraction and search using real PDFium | Restrictions: Must use real dependencies, verify text accuracy, check performance | Success: Integration tests verify text functionality end-to-end_

- [ ] 12. Add ArchUnitNET rules for text services
  - Files:
    - `tests/FluentPDF.Architecture.Tests/TextArchitectureTests.cs`
  - Add rule: Text services must implement interfaces
  - Add rule: Core must not depend on PdfiumInterop
  - Add rule: Text services must use SafeHandle for text page handles
  - Purpose: Enforce architectural rules for text components
  - _Leverage: Existing ArchUnitNET tests_
  - _Requirements: Architecture integrity_
  - _Prompt: Role: Software Architect | Task: Add ArchUnitNET tests enforcing text service architecture rules | Restrictions: Must catch violations, verify SafeHandle usage | Success: Architecture tests enforce clean boundaries for text components_

- [ ] 13. Final testing and documentation
  - Files:
    - `docs/ARCHITECTURE.md` (update)
    - `docs/TEXT-SEARCH.md` (new)
    - `README.md` (update)
  - Perform end-to-end testing of text extraction and search
  - Test with various PDFs (text-heavy, multi-column, Unicode)
  - Verify search performance with large documents
  - Update architecture documentation
  - Create TEXT-SEARCH.md documenting usage
  - Update README with search feature description
  - Verify all requirements met
  - Purpose: Ensure feature is complete and documented
  - _Leverage: All previous tasks_
  - _Requirements: All requirements_
  - _Prompt: Role: Technical Writer and QA Lead | Task: Complete final validation and documentation for text extraction and search | Restrictions: Must verify all requirements, ensure documentation accuracy | Success: Feature is production-ready with complete documentation_

## Summary

This spec implements text extraction and search:
- PDFium text extraction P/Invoke with SafeHandle
- Text extraction service with caching
- In-document search with match highlighting
- Search UI with keyboard shortcuts
- Text selection and clipboard copy
- Comprehensive testing with real PDFium
- Architecture enforcement with ArchUnitNET
