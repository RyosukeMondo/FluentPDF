# Tasks Document

- [x] 1. Create QPDF P/Invoke declarations
  - File: src/FluentPDF.Rendering/Interop/QpdfNative.cs
  - Define P/Invoke methods for QPDF C API (qpdf_init, qpdf_read, qpdf_write, qpdf_merge_pages, qpdf_get_info)
  - Add error code constants and helper methods for error translation
  - Purpose: Enable C# code to call QPDF native library
  - _Leverage: src/FluentPDF.Rendering/Interop/PdfiumInterop.cs (P/Invoke pattern)_
  - _Requirements: All (foundation)_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Interop Developer specializing in P/Invoke and native library integration | Task: Create comprehensive P/Invoke declarations for QPDF C API following the patterns from PdfiumInterop.cs, including qpdf_init, qpdf_read, qpdf_write, qpdf_merge_pages, qpdf_get_info, and error handling | Restrictions: Do not expose unsafe pointers in public APIs, must use const string DllName pattern, follow CallingConvention.Cdecl for all QPDF functions, include XML documentation with QPDF API reference URLs | _Leverage: Examine PdfiumInterop.cs for P/Invoke patterns, DLL loading, error code translation patterns | _Requirements: Foundation for all QPDF operations (Req 1, 2, 3) | Success: All P/Invoke declarations compile without errors, DLL loads successfully at runtime, error codes translate to meaningful messages, follows existing interop patterns exactly | Instructions: First mark this task as in-progress in tasks.md by changing [ ] to [-]. After implementation, use log-implementation tool with detailed artifacts (functions created with signatures and locations). Then mark as complete [x] in tasks.md._

- [x] 2. Create SafeHandle wrapper for QPDF job handles
  - File: src/FluentPDF.Rendering/Interop/SafeQpdfJobHandle.cs
  - Implement SafeHandle-derived class for automatic memory cleanup
  - Override ReleaseHandle to call qpdf_cleanup
  - Purpose: Prevent memory leaks from QPDF job handles
  - _Leverage: src/FluentPDF.Rendering/Interop/SafePdfDocumentHandle.cs, SafePdfPageHandle.cs_
  - _Requirements: All (memory safety)_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer specializing in unmanaged memory management and SafeHandle patterns | Task: Create SafeQpdfJobHandle class extending SafeHandle for automatic QPDF job handle cleanup, following patterns from SafePdfDocumentHandle.cs and SafePdfPageHandle.cs | Restrictions: Must call qpdf_cleanup in ReleaseHandle, must handle invalid handles gracefully, do not expose IntPtr publicly, must be thread-safe | _Leverage: Study SafePdfDocumentHandle.cs implementation, error handling for invalid handles | _Requirements: Memory safety for all operations | Success: SafeHandle properly releases QPDF resources, no memory leaks under stress testing (100+ operations), handles invalid/null pointers without crashing, thread-safe | Instructions: Mark task in-progress [-] in tasks.md. After implementation, log with artifacts (class name, methods, location). Mark complete [x]._

- [x] 3. Create IDocumentEditingService interface
  - File: src/FluentPDF.Core/Services/IDocumentEditingService.cs
  - Define service contract with MergeAsync, SplitAsync, OptimizeAsync methods
  - Add OptimizationOptions and OptimizationResult record types
  - Purpose: Establish service layer contract for dependency injection
  - _Leverage: src/FluentPDF.Core/Services/IPdfDocumentService.cs, IPdfRenderingService.cs_
  - _Requirements: 1, 2, 3_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Software Architect specializing in service-oriented architecture and C# interfaces | Task: Design IDocumentEditingService interface with MergeAsync, SplitAsync, OptimizeAsync methods following requirements 1, 2, 3, extending patterns from IPdfDocumentService.cs and IPdfRenderingService.cs | Restrictions: Must return Result<T> for all operations, include IProgress<double> for progress reporting, support CancellationToken, do not expose QPDF types in interface signatures | _Leverage: Study IPdfDocumentService.cs method signatures, Result<T> usage patterns | _Requirements: Req 1 (merge), Req 2 (split), Req 3 (optimize), Req 4 (progress) | Success: Interface compiles with clean API surface, supports cancellation and progress, follows existing service patterns, all methods return Result<T> | Instructions: Mark in-progress [-]. Log with artifacts (interface name, methods with signatures). Mark complete [x]._

- [x] 4. Create PageRange parser utility
  - File: src/FluentPDF.Core/Utilities/PageRangeParser.cs
  - Implement static Parse method to convert strings like "1-5, 10, 15-20" to List<PageRange>
  - Add validation for invalid ranges (negative, zero, overlapping)
  - Purpose: Support split operation page range specification
  - _Leverage: Existing validation patterns in FluentPDF.Core_
  - _Requirements: 2_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer specializing in parsing and validation logic | Task: Create PageRangeParser utility with Parse method to convert page range strings to List<PageRange> following requirement 2, with comprehensive validation | Restrictions: Must handle edge cases (empty strings, invalid formats, negative numbers, zero pages), return Result<List<PageRange>> not exceptions, validate page numbers are positive integers | _Leverage: Examine existing validation utilities in FluentPDF.Core for error handling patterns | _Requirements: Req 2 (split with page ranges) | Success: Parses valid range strings correctly ("1-5, 10, 15-20"), returns validation errors for invalid input, handles edge cases (overlapping ranges, reverse ranges, single pages), comprehensive unit tests pass | Instructions: Mark in-progress [-]. Log with artifacts (function Parse signature and location). Mark complete [x]._

- [x] 5. Implement DocumentEditingService with MergeAsync
  - File: src/FluentPDF.Rendering/Services/DocumentEditingService.cs
  - Create class implementing IDocumentEditingService
  - Implement MergeAsync using QPDF qpdf_merge_pages
  - Add progress reporting and cancellation support
  - Purpose: Provide PDF merge functionality
  - _Leverage: src/FluentPDF.Rendering/Services/PdfDocumentService.cs, PdfRenderingService.cs_
  - _Requirements: 1, 4_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Backend Developer with expertise in document processing and async operations | Task: Implement DocumentEditingService.MergeAsync method following requirements 1 and 4, using QPDF qpdf_merge_pages with progress reporting and cancellation support, extending patterns from PdfDocumentService.cs | Restrictions: Must validate all input files exist before processing, use SafeQpdfJobHandle for memory safety, report progress every 500ms minimum, support CancellationToken, return Result<string> with output path | _Leverage: Study PdfDocumentService.cs constructor injection, error handling with Result<T>, ITelemetryService logging | _Requirements: Req 1 (merge), Req 4 (progress reporting) | Success: Merges 2-10 PDFs correctly preserving page order, reports progress accurately, supports cancellation mid-operation, validates output with QPDF checks, no memory leaks | Instructions: Mark in-progress [-]. Log with artifacts (class DocumentEditingService, method MergeAsync, integration with QPDF). Mark complete [x]._

- [x] 6. Implement SplitAsync method
  - File: src/FluentPDF.Rendering/Services/DocumentEditingService.cs (continue from task 5)
  - Implement SplitAsync using QPDF page extraction
  - Parse page ranges using PageRangeParser
  - Add validation for page numbers against document page count
  - Purpose: Provide PDF split functionality
  - _Leverage: PageRangeParser from task 4, existing QPDF interop_
  - _Requirements: 2, 4_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Backend Developer with expertise in PDF manipulation and validation | Task: Implement SplitAsync method in DocumentEditingService following requirements 2 and 4, using PageRangeParser and QPDF page extraction with proper validation | Restrictions: Must validate page ranges before processing, check page numbers against document page count, preserve encryption if requested, support progress reporting and cancellation | _Leverage: Use PageRangeParser.Parse from task 4, existing SafeQpdfJobHandle pattern, QPDF page extraction API | _Requirements: Req 2 (split), Req 4 (progress) | Success: Splits PDFs by valid page ranges correctly, validates page numbers before extraction, preserves page quality losslessly, reports progress and supports cancellation, handles encrypted PDFs | Instructions: Mark in-progress [-]. Log with artifacts (method SplitAsync signature and implementation details). Mark complete [x]._

- [x] 7. Implement OptimizeAsync method
  - File: src/FluentPDF.Rendering/Services/DocumentEditingService.cs (continue from task 6)
  - Implement OptimizeAsync with QPDF optimization features
  - Support stream compression, object removal, deduplication, linearization
  - Calculate and return optimization metrics (size reduction, processing time)
  - Purpose: Provide PDF optimization functionality
  - _Leverage: Existing QPDF interop, OptimizationOptions record_
  - _Requirements: 3, 4_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Performance Engineer with expertise in PDF optimization and compression | Task: Implement OptimizeAsync method following requirements 3 and 4, using QPDF optimization APIs to compress streams, remove unused objects, deduplicate resources, and optionally linearize | Restrictions: Must NOT recompress images or fonts (lossless only), warn user if optimization increases file size, measure and report size reduction percentage, support progress and cancellation | _Leverage: Use OptimizationOptions for configuration, QPDF qpdf_optimize APIs, calculate OptimizationResult metrics | _Requirements: Req 3 (optimize), Req 4 (progress) | Success: Reduces PDF file size through lossless optimization, linearization works for fast web viewing, reports accurate metrics, warns on size increase, no quality degradation | Instructions: Mark in-progress [-]. Log with artifacts (method OptimizeAsync, QPDF optimization integration). Mark complete [x]._

- [x] 8. Add DI registration for DocumentEditingService
  - File: src/FluentPDF.App/App.xaml.cs (modify existing)
  - Register IDocumentEditingService as singleton in DI container
  - Configure service dependencies (IPdfDocumentService, ITelemetryService)
  - Purpose: Enable service injection throughout application
  - _Leverage: Existing DI configuration in App.xaml.cs_
  - _Requirements: All (infrastructure)_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer with expertise in dependency injection and service configuration | Task: Register IDocumentEditingService in DI container in App.xaml.cs following existing registration patterns, configuring singleton lifetime and dependencies | Restrictions: Must register as singleton for thread-safety, ensure IPdfDocumentService and ITelemetryService are resolved, do not create circular dependencies | _Leverage: Study existing service registrations in App.xaml.cs ConfigureServices method | _Requirements: Foundation for all features | Success: Service is properly registered and resolvable from DI container, dependencies inject correctly, no registration errors at startup | Instructions: Mark in-progress [-]. Log with artifacts (DI registration code location). Mark complete [x]._

- [-] 9. Add merge/split/optimize commands to PdfViewerViewModel
  - File: src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs (modify existing)
  - Add RelayCommand properties for MergeCommand, SplitCommand, OptimizeCommand
  - Inject IDocumentEditingService via constructor
  - Implement command handlers with progress reporting
  - Purpose: Expose operations to UI layer
  - _Leverage: Existing RelayCommand pattern in PdfViewerViewModel_
  - _Requirements: All_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Frontend Developer specializing in MVVM and WinUI 3 | Task: Add MergeCommand, SplitCommand, OptimizeCommand to PdfViewerViewModel following existing RelayCommand patterns, injecting IDocumentEditingService and implementing handlers with progress UI | Restrictions: Must use CommunityToolkit.Mvvm [RelayCommand] attribute, implement IProgress<double> for progress bars, handle cancellation via CancellationTokenSource, show user-friendly error messages | _Leverage: Study existing LoadDocumentCommand pattern, error handling with ErrorDialog, INavigationService usage | _Requirements: All requirements exposed via UI commands | Success: Commands are properly data-bound, progress updates UI smoothly, cancellation works, errors show user-friendly dialogs | Instructions: Mark in-progress [-]. Log with artifacts (commands added to ViewModel). Mark complete [x]._

- [ ] 10. Create merge/split/optimize UI in PdfViewerPage
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml (modify existing)
  - Add toolbar buttons for merge, split, optimize operations
  - Add progress bar for long operations
  - Add cancellation button during operations
  - Purpose: Provide UI for document operations
  - _Leverage: Existing toolbar and button styles in PdfViewerPage.xaml_
  - _Requirements: All_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: UI/UX Designer with expertise in WinUI 3 and XAML | Task: Add toolbar buttons for merge, split, optimize operations to PdfViewerPage.xaml following existing toolbar patterns, with progress bar and cancellation support | Restrictions: Must follow Fluent Design System, use existing icon fonts, bind to ViewModel commands via x:Bind, show progress bar only during operations, match existing toolbar styling | _Leverage: Study existing toolbar CommandBar, button styles, ProgressBar usage patterns | _Requirements: Req 4 (progress UI), all operations accessible | Success: UI is intuitive and follows design system, buttons are properly enabled/disabled based on document state, progress bar updates smoothly, cancellation button works | Instructions: Mark in-progress [-]. Log with artifacts (UI components added to XAML). Mark complete [x]._

- [ ] 11. Add unit tests for PageRangeParser
  - File: tests/FluentPDF.Core.Tests/Utilities/PageRangeParserTests.cs
  - Write tests for valid range parsing ("1-5", "1-5, 10", "1-5, 10, 15-20")
  - Test edge cases (empty, null, invalid formats, negative, zero, overlapping)
  - Purpose: Ensure parser reliability and edge case handling
  - _Leverage: tests/FluentPDF.Core.Tests patterns, FluentAssertions_
  - _Requirements: 2_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer with expertise in unit testing and edge case validation | Task: Create comprehensive unit tests for PageRangeParser covering all valid formats and edge cases from requirement 2, using FluentAssertions for readable assertions | Restrictions: Must test both success and failure scenarios, use Theory/InlineData for parameterized tests, test edge cases thoroughly, maintain test isolation | _Leverage: Examine existing test patterns in FluentPDF.Core.Tests, FluentAssertions usage | _Requirements: Req 2 validation | Success: All edge cases covered (100+ test cases), tests are fast and isolated, failures provide clear diagnostic messages | Instructions: Mark in-progress [-]. Log with artifacts (test class and test methods). Mark complete [x]._

- [ ] 12. Add integration tests for DocumentEditingService
  - File: tests/FluentPDF.Rendering.Tests/Services/DocumentEditingServiceTests.cs
  - Write tests for merge (2-10 PDFs), split (various ranges), optimize (size reduction)
  - Test with sample PDFs from Fixtures folder
  - Test error scenarios (corrupted PDF, encrypted PDF, insufficient disk space simulation)
  - Purpose: Ensure service reliability with real QPDF operations
  - _Leverage: tests/FluentPDF.Rendering.Tests/Fixtures sample PDFs, existing test patterns_
  - _Requirements: 1, 2, 3_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Integration Test Engineer with expertise in PDF testing and QPDF validation | Task: Create integration tests for DocumentEditingService covering merge, split, optimize operations with real sample PDFs, testing both success and error scenarios from requirements 1, 2, 3 | Restrictions: Must use sample PDFs from Fixtures folder, validate output using QPDF structural checks, test error paths (corrupted PDF, encrypted), clean up output files in test teardown | _Leverage: Use existing sample PDFs in tests/FluentPDF.Rendering.Tests/Fixtures, QPDF validation utilities | _Requirements: Req 1, 2, 3 (all operations) | Success: All operations tested with real PDFs, error scenarios covered, tests are reliable and clean up properly, QPDF validation confirms output quality | Instructions: Mark in-progress [-]. Log with artifacts (test class, integration with QPDF validation). Mark complete [x]._

- [ ] 13. Add architecture tests for DocumentEditing layer
  - File: tests/FluentPDF.Architecture.Tests/DocumentEditingLayerTests.cs
  - Validate IDocumentEditingService is in FluentPDF.Core
  - Validate DocumentEditingService is in FluentPDF.Rendering
  - Ensure QpdfNative is internal and not exposed publicly
  - Purpose: Prevent architectural violations
  - _Leverage: tests/FluentPDF.Architecture.Tests existing ArchUnitNET patterns_
  - _Requirements: Non-functional (architecture)_
  - _Prompt: Implement the task for spec document-structure-operations, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Software Architect with expertise in ArchUnitNET and architectural testing | Task: Create architecture tests validating DocumentEditing layer follows project structure rules, using ArchUnitNET patterns from existing tests | Restrictions: Must enforce that interfaces are in Core, implementations in Rendering, interop types are internal, no circular dependencies | _Leverage: Study existing ArchUnitNET tests in FluentPDF.Architecture.Tests for layer validation patterns | _Requirements: Code architecture NFR | Success: Tests enforce layer boundaries, detect violations, run in CI pipeline, prevent architectural erosion | Instructions: Mark in-progress [-]. Log with artifacts (architecture test rules). Mark complete [x]._
