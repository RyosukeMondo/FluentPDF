# Tasks Document

- [x] 1. Create PdfiumServiceBase class with threading documentation
  - File: src/FluentPDF.Rendering/Services/Base/PdfiumServiceBase.cs
  - Create abstract base class with ExecutePdfiumOperationAsync helper
  - Add comprehensive XML documentation about threading constraints
  - Purpose: Provide architectural safeguard against future Task.Run usage
  - _Leverage: Existing service patterns_
  - _Requirements: 1.1, 2.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Architect specializing in cross-platform native interop and WinUI 3 | Task: Create PdfiumServiceBase abstract class in src/FluentPDF.Rendering/Services/Base/ with ExecutePdfiumOperationAsync<T> helper method and comprehensive XML documentation explaining that PDFium cannot be called from Task.Run threads in .NET 9.0 WinUI 3 self-contained deployments due to AccessViolation crashes | Restrictions: Do not modify existing service interfaces, maintain async/await patterns, only provide helper methods and documentation | Success: Base class compiles successfully, documentation is clear and prominent, provides reusable pattern for services to use | Instructions: After starting, edit tasks.md to mark this task as in-progress [-], then implement the code. When complete, use log-implementation tool to record: file created (PdfiumServiceBase.cs), artifacts (classes: PdfiumServiceBase with ExecutePdfiumOperationAsync method), and mark task as complete [x] in tasks.md

- [x] 2. Fix BookmarkService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/BookmarkService.cs
  - Replace Task.Run with Task.Yield in LoadBookmarksAsync
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix bookmark loading crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with expertise in async/await patterns and service architecture | Task: Remove Task.Run wrapper from BookmarkService.LoadBookmarksAsync method (around line 43), replace with await Task.Yield(), and make the service inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Do not change public method signatures, preserve all error handling and logging, maintain FluentResults return types | Success: BookmarkService compiles, inherits PdfiumServiceBase, uses Task.Yield instead of Task.Run, bookmark loading works without crashes | Instructions: Mark task in-progress in tasks.md, implement changes, test bookmark loading, use log-implementation to record modifications to BookmarkService.cs including method signature changes, then mark complete

- [x] 3. Fix TextSearchService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/TextSearchService.cs
  - Find and replace all Task.Run calls with Task.Yield
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix text search crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with expertise in text processing and async patterns | Task: Search TextSearchService for all Task.Run usages, replace with await Task.Yield(), make service inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve existing search algorithm logic, do not change search result types, maintain error handling patterns | Success: TextSearchService compiles and inherits base class, no Task.Run calls remain, text search functionality works without crashes | Instructions: Mark in-progress, implement changes, verify search works, log implementation details including all modified methods, mark complete

- [x] 4. Fix ThumbnailRenderingService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/ThumbnailRenderingService.cs
  - Replace Task.Run with Task.Yield in thumbnail rendering
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix thumbnail generation crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in image processing and rendering | Task: Remove Task.Run wrappers from ThumbnailRenderingService rendering methods, replace with await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Do not change thumbnail sizing logic, preserve image quality settings, maintain caching behavior | Success: Service compiles with base class inheritance, thumbnails render without crashes, thumbnail quality unchanged | Instructions: Mark in-progress, fix threading, test thumbnail generation, log implementation with method details, mark complete

- [x] 5. Fix WatermarkService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/WatermarkService.cs
  - Replace Task.Run with Task.Yield in watermark operations
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix watermark application crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with PDF manipulation and graphics experience | Task: Remove Task.Run from WatermarkService AddWatermarkAsync and related methods, use await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve watermark positioning and transparency logic, maintain text and image watermark support, do not change watermark rendering quality | Success: WatermarkService inherits base class, watermarks apply without crashes, visual quality maintained | Instructions: Mark in-progress, implement threading fix, test watermark application, log implementation details, mark complete

- [x] 6. Fix TextExtractionService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/TextExtractionService.cs
  - Replace Task.Run with Task.Yield in text extraction
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix text extraction crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in text processing and encoding | Task: Remove Task.Run wrappers from TextExtractionService ExtractTextAsync method, replace with await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve text encoding handling, maintain formatting preservation logic, do not change extraction accuracy | Success: Service inherits base class, text extraction works without crashes, extracted text quality unchanged | Instructions: Mark in-progress, fix threading, verify text extraction, log implementation, mark complete

- [x] 7. Fix PdfFormService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/PdfFormService.cs
  - Replace Task.Run with Task.Yield in form field operations
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix form field interaction crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with forms and UI controls expertise | Task: Remove Task.Run from PdfFormService GetFormFieldsAsync, UpdateFormFieldAsync, and other form methods, use await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve form field validation logic, maintain field type handling, do not change form field data structures | Success: Form service inherits base class, form operations work without crashes, form functionality intact | Instructions: Mark in-progress, implement threading fixes for all form methods, test form interactions, log implementation, mark complete

- [x] 8. Fix PageOperationsService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/PageOperationsService.cs
  - **SKIPPED**: This service uses QPDF (QpdfNative), not PDFium (PdfiumInterop)
  - QPDF is a different native library and may have different threading requirements
  - Per requirements, only PDFium services need fixes
  - Task.Run usage in this service is NOT causing the crashes addressed by this spec
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in document manipulation | Task: Remove Task.Run from PageOperationsService methods (DeletePageAsync, RotatePageAsync, ExtractPageAsync, etc.), replace with await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve page manipulation logic, maintain document integrity checks, do not change operation success criteria | Success: Service inherits base class, page operations work without crashes, document integrity maintained | Instructions: Mark in-progress, fix all page operation methods, test operations, log implementation with all modified methods, mark complete

- [x] 9. Fix ImageInsertionService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/ImageInsertionService.cs
  - Replace Task.Run with Task.Yield in image insertion methods
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix image insertion crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with image processing and PDF manipulation experience | Task: Remove Task.Run from ImageInsertionService InsertImageAsync method, use await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve image scaling and positioning logic, maintain image quality, do not change supported image formats | Success: Service inherits base class, image insertion works without crashes, image quality maintained | Instructions: Mark in-progress, implement threading fix, test image insertion, log implementation, mark complete

- [ ] 10. Fix DocumentEditingService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/DocumentEditingService.cs
  - Replace Task.Run with Task.Yield in document editing operations
  - Make service inherit from PdfiumServiceBase
  - Purpose: Fix document editing crashes
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in document editing and manipulation | Task: Search DocumentEditingService for all Task.Run usages in editing methods, replace with await Task.Yield(), inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: Preserve all editing logic and validation, maintain undo/redo functionality if present, do not change editing capabilities | Success: Service inherits base class, document editing works without crashes, all editing features functional | Instructions: Mark in-progress, fix all editing methods, test editing operations, log implementation details, mark complete

- [ ] 11. Fix FormValidationService - Remove Task.Run
  - File: src/FluentPDF.Rendering/Services/FormValidationService.cs
  - Check for Task.Run usage and replace with Task.Yield if found
  - Make service inherit from PdfiumServiceBase if it uses PDFium
  - Purpose: Ensure form validation doesn't crash
  - _Leverage: PdfiumServiceBase, PdfiumInterop_
  - _Requirements: 1.1, 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with validation and forms expertise | Task: Examine FormValidationService for Task.Run usage and PDFium interop calls, if found replace Task.Run with await Task.Yield() and inherit from PdfiumServiceBase following requirements 1.1, 1.3, and 3.1 | Restrictions: If service doesn't call PDFium, skip inheritance but document the finding, preserve validation rules and logic | Success: Service is thread-safe if it uses PDFium, validation logic unchanged, no crashes during validation | Instructions: Mark in-progress, analyze service, apply fixes if needed, log findings and any changes made, mark complete

- [ ] 12. Audit remaining services for Task.Run usage
  - Files: All services in src/FluentPDF.Rendering/Services not yet covered
  - Systematically check: DocxParserService, DpiDetectionService, DocxConverterService, HtmlToPdfService, LogExportService, MetricsCollectionService, LibreOfficeValidator
  - Purpose: Ensure no PDFium services were missed
  - _Leverage: Grep/search tools_
  - _Requirements: 1.1, 2.2_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Code Auditor with systematic review expertise | Task: Use grep or search tools to find all Task.Run calls in remaining services (DocxParserService, DpiDetectionService, DocxConverterService, HtmlToPdfService, LogExportService, MetricsCollectionService, LibreOfficeValidator), determine which call PDFium, fix any found issues following requirements 1.1 and 2.2 | Restrictions: Only modify services that actually call PdfiumInterop, document services that are safe, do not change non-PDFium code unnecessarily | Success: All services audited, any PDFium-calling services fixed, comprehensive list of safe vs fixed services documented | Instructions: Mark in-progress, use grep to search for Task.Run in remaining services, fix any issues found, log detailed audit results including which services were modified and which were safe, mark complete

- [ ] 13. Re-enable and test PDF loading in PdfViewerViewModel
  - File: src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs
  - Uncomment bookmarks, thumbnails, rendering, form fields, annotations loading
  - Remove debug logging statements
  - Purpose: Restore full PDF loading functionality
  - _Leverage: Fixed services from previous tasks_
  - _Requirements: 1.3, 3.1_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with WinUI 3 and MVVM expertise | Task: In PdfViewerViewModel.LoadDocumentFromPathAsync method, uncomment the calls to BookmarksViewModel.LoadBookmarksCommand, ThumbnailsViewModel.LoadThumbnailsAsync, RenderCurrentPageAsync, FormFieldViewModel.LoadFormFieldsCommand, and AnnotationViewModel.LoadAnnotationsCommand, remove all debug System.IO.File.AppendAllText logging statements following requirements 1.3 and 3.1 | Restrictions: Do not change the order of operations, maintain error handling, preserve existing viewmodel patterns | Success: Full PDF loading flow restored, debug logging removed, application loads PDFs with all features without crashing | Instructions: Mark in-progress, uncomment code sections, remove debug logs, verify changes compile, log implementation details, mark complete

- [ ] 14. Integration testing - Verify all PDF operations
  - Test: Load PDFs, navigate pages, search text, view bookmarks, fill forms, add annotations
  - Run through complete user workflow for 5+ minutes
  - Purpose: Validate fix works across all operations
  - _Leverage: Test PDFs in C:\Users\ryosu\Downloads_
  - _Requirements: All_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer with comprehensive testing expertise | Task: Perform thorough integration testing of all PDF operations including loading PDFs, navigating pages, searching text, viewing bookmarks, filling forms, and adding annotations, run application continuously for at least 5 minutes performing various operations covering all requirements | Restrictions: Test with real PDF files, verify application stability, do not skip any operations, test error scenarios too | Success: Application loads PDFs successfully, remains stable during all operations, no crashes observed, all features work as expected | Instructions: Mark in-progress, launch application, systematically test all PDF operations, if crashes occur document them and return to debugging, once stable for 5+ minutes with all operations working, log testing results detailing operations tested and stability observed, mark complete

- [ ] 15. Code cleanup and documentation
  - Add XML documentation to PdfiumServiceBase
  - Add code comments in services explaining threading requirements
  - Update any relevant README or architecture documentation
  - Purpose: Prevent future introduction of threading bugs
  - _Leverage: Existing documentation patterns_
  - _Requirements: 2.1, 2.2_
  - _Prompt: Implement the task for spec pdfium-threading-fix, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Technical Writer and Senior Developer | Task: Add comprehensive XML documentation to PdfiumServiceBase class, add code comments in key service methods explaining PDFium threading constraints, update README.md or architecture documentation to document the threading requirements and patterns following requirements 2.1 and 2.2 | Restrictions: Do not change code logic, maintain existing documentation structure, make documentation clear for future developers | Success: PdfiumServiceBase has complete XML docs, key service methods have explanatory comments, documentation updated with threading patterns and constraints | Instructions: Mark in-progress, write documentation and comments, verify documentation builds correctly, log documentation additions and updates, mark complete
