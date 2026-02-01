# Tasks: PDFium Backend Abstraction

## Phase 1: Create Abstractions Layer

- [ ] 1.1 [NEXT] Create FluentPDF.Rendering.Abstractions project
  **Action Steps**:
  1. Check if src/FluentPDF.Rendering.Abstractions/ exists. If yes: Skip to step 8 for verification
  2. Create project: dotnet new classlib -n FluentPDF.Rendering.Abstractions -o src/FluentPDF.Rendering.Abstractions
  3. Edit src/FluentPDF.Rendering.Abstractions/FluentPDF.Rendering.Abstractions.csproj:
     - Change TargetFramework to TargetFrameworks: <TargetFrameworks>netstandard2.1;net8.0</TargetFrameworks>
     - Add property: <LangVersion>latest</LangVersion>
     - Add property: <Nullable>enable</Nullable>
  4. Add FluentResults package: dotnet add src/FluentPDF.Rendering.Abstractions package FluentResults
  5. Delete Class1.cs: Remove src/FluentPDF.Rendering.Abstractions/Class1.cs
  6. Add project to solution: dotnet sln FluentPDF.sln add src/FluentPDF.Rendering.Abstractions/FluentPDF.Rendering.Abstractions.csproj
  7. Build project: dotnet build src/FluentPDF.Rendering.Abstractions
  8. Verify: Run dotnet build FluentPDF.sln - should succeed with no errors, abstractions project compiles for both netstandard2.1 and net8.0
  - File: FluentPDF.Rendering.Abstractions.csproj
  - _Leverage: Existing Core project structure_
  - _Requirements: 3.1, NFR-4.3.1_
  - _Prompt: Role: .NET Build Engineer | Task: Create new FluentPDF.Rendering.Abstractions class library project following requirement 3.1 with proper multi-targeting for cross-platform support. Configure project structure matching existing Core project patterns. | Restrictions: Must be .NET 8, netstandard2.1 for maximum compatibility, minimal dependencies (only FluentResults), no platform-specific code | Success: Project builds on Windows/Linux/macOS, proper package references, matches existing project structure

- [ ] 1.2 Define IPdfBackend interface
  - File: src/FluentPDF.Rendering.Abstractions/IPdfBackend.cs
  - Define core backend interface with Initialize, OpenDocument, CreateDocument methods
  - Add PdfBackendInfo record for backend metadata
  - Include comprehensive XML documentation
  - _Leverage: Existing service interface patterns from Core/Services_
  - _Requirements: 3.1.1, NFR-4.3.2_
  - _Prompt: Role: API Designer with expertise in C# interface design | Task: Create IPdfBackend interface following requirement 3.1.1 with async operations, proper disposal pattern, and comprehensive documentation. Match existing service interface patterns from FluentPDF.Core. | Restrictions: Must implement IDisposable, all I/O operations must be async, must support both file and stream sources, no PDFium-specific details in interface | Success: Interface is clean and well-documented, supports all backend operations, follows existing patterns, compilation succeeds

- [ ] 1.3 Define IPdfDocument interface
  - File: src/FluentPDF.Rendering.Abstractions/IPdfDocument.cs
  - Define document interface with page access, metadata, save operations
  - Add PdfMetadata record
  - Add IPageCollection, IFormFieldCollection interfaces
  - _Leverage: Existing document models from Core/Models_
  - _Requirements: 3.1.2, NFR-4.2.2_
  - _Prompt: Role: Domain Model Designer | Task: Create IPdfDocument interface representing open PDF document following requirement 3.1.2 with proper lifecycle management, collections for pages/forms, and metadata access. | Restrictions: Must implement IDisposable correctly, must be thread-safe for read operations, no mutable state exposure, deterministic disposal | Success: Interface represents document lifecycle completely, proper resource management, follows domain modeling best practices

- [ ] 1.4 Define IPdfPage interface
  - File: src/FluentPDF.Rendering.Abstractions/IPdfPage.cs
  - Define page interface with rendering, text extraction, annotation access
  - Add RenderOptions configuration class
  - Add PdfTextSegment for text with bounds
  - _Leverage: Existing rendering configuration patterns_
  - _Requirements: 3.1.3, NFR-4.1.1_
  - _Prompt: Role: Graphics API Designer | Task: Create IPdfPage interface for page-level operations following requirement 3.1.3 with high-DPI rendering, text extraction with bounds, and annotation access. Optimize for performance (&lt;5% overhead goal). | Restrictions: Must support zero-copy rendering via Memory<byte>, async operations only, no blocking calls, efficient bitmap handling | Success: Interface supports all page operations, performance optimized, zero-copy operations available

- [ ] 1.5 Define value objects (PdfRect, PdfSize, PdfColor, PdfMatrix)
  - File: src/FluentPDF.Rendering.Abstractions/Models/*.cs
  - Create immutable value objects as readonly structs
  - Implement IEquatable<T> for each
  - Add common operations and helper methods
  - _Leverage: Existing model patterns, System.Drawing primitives_
  - _Requirements: 3.1.4, NFR-4.1.3_
  - _Prompt: Role: Performance-Oriented .NET Developer | Task: Create immutable value objects (PdfRect, PdfSize, PdfColor, PdfMatrix) as readonly structs following requirement 3.1.4 with zero-allocation operations. | Restrictions: Must be readonly struct, must implement IEquatable<T>, no heap allocations, immutable only, efficient equality comparison | Success: All value objects are structs, zero allocation confirmed, proper equality, unit tests pass

- [ ] 1.6 Define exception hierarchy
  - File: src/FluentPDF.Rendering.Abstractions/Exceptions/*.cs
  - Create PdfBackendException base class
  - Add specialized exceptions: PdfDocumentException, PdfRenderingException, PdfPasswordRequiredException
  - Include error context properties
  - _Leverage: Existing error handling patterns from Core/ErrorHandling_
  - _Requirements: NFR-4.2.1_
  - _Prompt: Role: Error Handling Specialist | Task: Create comprehensive exception hierarchy for PDF backend errors with PdfBackendException base and specialized exceptions for common scenarios. Include error context for debugging. | Restrictions: Must inherit from Exception properly, must be serializable, include inner exceptions, provide error context, follow existing error patterns | Success: Exception hierarchy is complete, serialization works, error context captured, integration with existing error handling

## Phase 2: Implement PDFium Backend

- [ ] 2.1 Create FluentPDF.Rendering.Pdfium project
  - File: FluentPDF.Rendering.Pdfium.csproj
  - Create new class library targeting net8.0
  - Reference FluentPDF.Rendering.Abstractions
  - Add required packages (Serilog, Microsoft.Extensions.Logging)
  - Configure unsafe code blocks
  - _Leverage: Existing Rendering project structure_
  - _Requirements: NFR-4.3.1_
  - _Prompt: Role: .NET Build Engineer | Task: Create FluentPDF.Rendering.Pdfium project for PDFium implementation. Configure for unsafe code and P/Invoke, reference abstractions project. | Restrictions: Must enable AllowUnsafeBlocks, must reference Abstractions project, platform-specific builds (Windows/Linux/macOS), proper native library copying | Success: Project builds with unsafe code enabled, references correct, native library configuration working

- [ ] 2.2 Move existing PDFium interop code
  - Files: src/FluentPDF.Rendering.Pdfium/Interop/*.cs
  - Move PdfiumInterop, PdfiumFormInterop, PdfiumTextInterop from FluentPDF.Rendering
  - Move SafeHandle implementations
  - Organize into namespace FluentPDF.Rendering.Pdfium.Interop
  - _Leverage: Existing interop code in FluentPDF.Rendering/Interop_
  - _Requirements: 3.2.4, NFR-4.3.1_
  - _Prompt: Role: Interop Specialist | Task: Migrate existing PDFium P/Invoke code to new Pdfium project following requirement 3.2.4. Organize into clean namespace structure, preserve all functionality. | Restrictions: Cannot break existing P/Invoke signatures, must maintain SafeHandle patterns, preserve thread safety, no functional changes during migration | Success: All interop code moved successfully, namespace organized, existing tests still pass, no regressions

- [ ] 2.3 Implement PdfiumBackend class
  - File: src/FluentPDF.Rendering.Pdfium/PdfiumBackend.cs
  - Implement IPdfBackend interface
  - Add thread-safe initialization with lock
  - Implement async document opening from file and stream
  - Add proper error mapping from PDFium error codes
  - _Leverage: Existing PDFium initialization patterns_
  - _Requirements: 3.2.1, NFR-4.2.3_
  - _Prompt: Role: Native Interop Developer | Task: Implement PdfiumBackend class following requirement 3.2.1 with thread-safe initialization, async document loading, and proper error handling. Map PDFium errors to custom exceptions. | Restrictions: Must initialize PDFium once per app, must be thread-safe, must handle all error codes, proper disposal of library, logging all operations | Success: Backend initializes correctly, thread-safe verified, errors mapped properly, integration tests pass

- [ ] 2.4 Implement PdfiumDocument class
  - File: src/FluentPDF.Rendering.Pdfium/PdfiumDocument.cs
  - Implement IPdfDocument interface
  - Wrap SafePdfDocumentHandle with proper disposal
  - Implement lazy page collection
  - Cache metadata after first access
  - _Leverage: Existing document handling patterns_
  - _Requirements: 3.2.2, NFR-4.2.2_
  - _Prompt: Role: Resource Management Specialist | Task: Implement PdfiumDocument wrapping SafePdfDocumentHandle following requirement 3.2.2 with proper disposal, lazy loading, and metadata caching. | Restrictions: Must wrap SafeHandle correctly, must dispose deterministically, must support finalizer, lazy-load pages, cache metadata, thread-safe properties | Success: Document lifecycle managed correctly, no memory leaks, lazy loading works, metadata cached, disposal tested

- [ ] 2.5 Implement PdfiumPage class
  - File: src/FluentPDF.Rendering.Pdfium/PdfiumPage.cs
  - Implement IPdfPage interface
  - Wrap SafePdfPageHandle
  - Implement RenderAsync with bitmap creation
  - Implement RenderToBufferAsync for zero-copy operations
  - Support high-DPI rendering
  - _Leverage: Existing rendering code_
  - _Requirements: 3.2.3, NFR-4.1.1, NFR-4.1.3_
  - _Prompt: Role: Graphics Rendering Specialist | Task: Implement PdfiumPage with optimized rendering following requirement 3.2.3 supporting high-DPI, rotation, annotations, and zero-copy operations via Memory<byte>. | Restrictions: Must minimize allocations, support zero-copy rendering, handle rotation/scaling, render annotations/forms optionally, measure &lt;5% overhead | Success: Rendering works at all DPIs, zero-copy verified, performance within 5% overhead, memory pooling working

- [ ] 2.6 Implement PdfiumPageCollection class
  - File: src/FluentPDF.Rendering.Pdfium/Collections/PdfiumPageCollection.cs
  - Implement IPageCollection interface
  - Lazy-load pages on access
  - Cache page instances
  - Support async enumeration
  - _Leverage: Collection patterns from Core_
  - _Requirements: 3.2.2_
  - _Prompt: Role: Collection Design Specialist | Task: Implement PdfiumPageCollection for lazy page access with caching and async enumeration. Optimize for common access patterns (sequential, random). | Restrictions: Must lazy-load pages, cache page objects, thread-safe reads, support IAsyncEnumerable, efficient memory usage | Success: Pages loaded on-demand, caching works, enumeration efficient, thread-safe

- [ ] 2.7 Implement text extraction with bounds
  - File: src/FluentPDF.Rendering.Pdfium/PdfiumPage.cs (extend)
  - Implement ExtractTextAsync and ExtractTextWithBoundsAsync
  - Use FPDF_TEXTPAGE API
  - Return PdfTextSegment list with character positions
  - _Leverage: Existing text extraction in TextExtractionService_
  - _Requirements: 3.1.3_
  - _Prompt: Role: Text Processing Specialist | Task: Implement text extraction methods using PDFium FPDF_TEXTPAGE API with character-level bounding boxes in PdfTextSegment format. | Restrictions: Must extract text with accurate bounds, handle Unicode correctly, support RTL text, efficient for large pages, proper handle cleanup | Success: Text extracted accurately, bounds correct, Unicode support verified, performance acceptable

- [ ] 2.8 Implement form field access
  - File: src/FluentPDF.Rendering.Pdfium/PdfiumFormField.cs
  - Implement IPdfFormField interface
  - Support text, checkbox, radio button, combo box field types
  - Implement GetValue/SetValue operations
  - _Leverage: Existing form handling in PdfFormService_
  - _Requirements: 3.1.2_
  - _Prompt: Role: Forms Specialist | Task: Implement form field access using PDFium form API supporting all field types (text, checkbox, radio, combo) with get/set operations. | Restrictions: Must support all AcroForm field types, handle field hierarchy, validate field values, proper type mapping, thread-safe | Success: All field types accessible, values read/written correctly, field hierarchy preserved

- [ ] 2.9 Implement annotation access
  - File: src/FluentPDF.Rendering.Pdfium/PdfiumAnnotation.cs
  - Implement IPdfAnnotation interface
  - Support common annotation types (text, highlight, link, etc.)
  - Read annotation properties (type, rect, content)
  - _Leverage: Existing annotation code in AnnotationService_
  - _Requirements: 3.1.3_
  - _Prompt: Role: Annotation Specialist | Task: Implement annotation access using PDFium annotation API supporting common types with property access. | Restrictions: Must support PDF annotation standard types, read annotation properties, handle annotation appearance, efficient access | Success: Annotations enumerated correctly, properties read accurately, types identified

- [ ] 2.10 Add integration tests for PDFium backend
  - File: tests/FluentPDF.Rendering.Pdfium.Tests/IntegrationTests.cs
  - Test document opening, rendering, text extraction
  - Use real PDF test files
  - Verify against known outputs
  - Test error handling
  - _Leverage: Existing test fixtures in tests/Fixtures_
  - _Requirements: NFR-4.4.2_
  - _Prompt: Role: Integration Test Engineer | Task: Create comprehensive integration tests for PdfiumBackend using real PDF files verifying rendering, text extraction, forms, annotations against known baselines. | Restrictions: Must use real PDFium.dll, test with actual PDF files, verify pixel-perfect rendering, test error cases, measure performance | Success: All integration tests pass, rendering verified, error handling tested, performance measured

## Phase 3: Implement In-Memory Test Backend

- [ ] 3.1 Create FluentPDF.Rendering.InMemory project
  - File: FluentPDF.Rendering.InMemory.csproj
  - Create new class library for test backend
  - Reference FluentPDF.Rendering.Abstractions
  - Lightweight dependencies only
  - _Leverage: Testing project patterns_
  - _Requirements: 3.3.1, NFR-4.4.1_
  - _Prompt: Role: Test Infrastructure Engineer | Task: Create FluentPDF.Rendering.InMemory project for fast in-memory testing without native dependencies. | Restrictions: Must have zero native dependencies, fast initialization, minimal memory footprint, deterministic behavior | Success: Project builds without PDFium, fast instantiation (&lt;1ms), no native DLLs required

- [ ] 3.2 Implement InMemoryBackend class
  - File: src/FluentPDF.Rendering.InMemory/InMemoryBackend.cs
  - Implement IPdfBackend with in-memory documents
  - Support pre-configured test documents
  - Fast initialization (no native library)
  - _Leverage: Mock patterns from existing tests_
  - _Requirements: 3.3.1_
  - _Prompt: Role: Mock Framework Developer | Task: Implement InMemoryBackend providing fast in-memory PDF implementation for unit testing without PDFium dependency. | Restrictions: No native dependencies, instant initialization, deterministic behavior, configurable test scenarios | Success: Backend initializes in &lt;1ms, no PDFium required, creates test documents instantly

- [ ] 3.3 Implement InMemoryDocument class
  - File: src/FluentPDF.Rendering.InMemory/InMemoryDocument.cs
  - Implement IPdfDocument with in-memory page collection
  - Store pages in List<InMemoryPage>
  - Support configurable metadata
  - _Leverage: Mock data patterns_
  - _Requirements: 3.3.1_
  - _Prompt: Role: Test Data Builder | Task: Implement InMemoryDocument holding pages in memory with configurable metadata and structure for testing. | Restrictions: Must implement full IPdfDocument contract, pages stored in memory, fast access, no I/O operations, configurable via builder | Success: Document holds pages in memory, metadata configurable, fast access, builder pattern works

- [ ] 3.4 Implement InMemoryPage class
  - File: src/FluentPDF.Rendering.InMemory/InMemoryPage.cs
  - Implement IPdfPage with synthetic rendering
  - Generate simple test bitmaps (solid color, text patterns)
  - Return configurable text content
  - _Leverage: Test fixture patterns_
  - _Requirements: 3.3.1_
  - _Prompt: Role: Test Fixture Developer | Task: Implement InMemoryPage generating synthetic PDF page data for testing (simple bitmaps, configurable text). | Restrictions: Must implement IPdfPage, generate valid bitmap data, return configurable text, fast rendering (&lt;1ms), no actual PDF rendering | Success: Page renders synthetic bitmaps, text extraction returns configured content, fast execution

- [ ] 3.5 Create PdfDocumentBuilder test utility
  - File: src/FluentPDF.Rendering.InMemory/Builders/PdfDocumentBuilder.cs
  - Fluent builder for creating test documents
  - Add common document templates (single page, multi-page, forms, etc.)
  - Make test document creation easy and readable
  - _Leverage: Builder pattern from existing tests_
  - _Requirements: 3.3.2_
  - _Prompt: Role: Test API Designer | Task: Create fluent PdfDocumentBuilder for easy creation of test PDF documents with common templates and configurations. | Restrictions: Must be fluent API, provide common templates (A4 page, Letter, forms), readable test setup, no boilerplate | Success: Builder creates documents fluently, templates cover common cases, tests are readable

- [ ] 3.6 Create test fixture library
  - File: src/FluentPDF.Rendering.InMemory/Fixtures/*.cs
  - Pre-built test documents (single page A4, multi-page, with forms, with annotations)
  - Common page sizes and layouts
  - Sample text content
  - _Leverage: Existing test fixtures_
  - _Requirements: 3.3.2_
  - _Prompt: Role: Test Fixture Curator | Task: Create library of pre-built test PDF fixtures covering common testing scenarios (single/multi page, forms, annotations, various sizes). | Restrictions: Must be reusable across tests, cover common scenarios, deterministic content, fast creation | Success: Fixture library covers common cases, reusable in tests, deterministic

- [ ] 3.7 Add unit tests using InMemoryBackend
  - File: tests/FluentPDF.Rendering.InMemory.Tests/UnitTests.cs
  - Verify InMemory backend implements interfaces correctly
  - Test document/page creation
  - Verify builder and fixtures work
  - Measure performance (&lt;1s for full test suite)
  - _Leverage: xUnit test patterns_
  - _Requirements: NFR-4.4.1, NFR-4.4.3_
  - _Prompt: Role: Unit Test Engineer | Task: Create unit tests for InMemory backend verifying interface compliance, builder functionality, and performance targets (&lt;1s suite). | Restrictions: No PDFium dependency, tests must be fast (&lt;1s total), verify all interface methods, test builders/fixtures | Success: All tests pass in &lt;1s, no PDFium required, interface compliance verified

## Phase 4: Migrate Services to Use Abstraction

- [ ] 4.1 Update PdfDocumentService to use IPdfBackend
  - File: src/FluentPDF.Core/Services/PdfDocumentService.cs (modify)
  - Replace direct PDFium calls with IPdfBackend
  - Inject IPdfBackend via constructor
  - Update OpenDocument methods to use backend
  - _Leverage: Existing service DI patterns_
  - _Requirements: 3.4.1, 3.4.2_
  - _Prompt: Role: Service Layer Refactoring Specialist | Task: Migrate PdfDocumentService from direct PDFium calls to IPdfBackend abstraction following requirement 3.4.1. Inject backend via DI, replace all direct calls. | Restrictions: Must not break existing API, maintain all functionality, use DI for backend, no direct PDFium references, preserve error handling | Success: Service uses IPdfBackend only, existing tests pass with InMemory backend, integration tests pass with Pdfium backend

- [ ] 4.2 Update PdfRenderingService to use IPdfBackend
  - File: src/FluentPDF.Core/Services/PdfRenderingService.cs (modify)
  - Use IPdfPage.RenderAsync instead of direct rendering
  - Support RenderOptions configuration
  - Maintain high-DPI support
  - _Leverage: Updated PdfDocumentService pattern_
  - _Requirements: 3.4.1_
  - _Prompt: Role: Rendering Service Developer | Task: Migrate PdfRenderingService to use IPdfPage abstraction for rendering operations. Replace direct bitmap creation with IPdfPage.RenderAsync. | Restrictions: Must maintain rendering quality, preserve high-DPI support, use RenderOptions, no performance regression, backward compatible | Success: Service uses abstraction, rendering quality unchanged, performance within targets, tests pass

- [ ] 4.3 Update TextExtractionService to use IPdfBackend
  - File: src/FluentPDF.Rendering/Services/TextExtractionService.cs (modify)
  - Use IPdfPage.ExtractTextAsync methods
  - Support text with bounds extraction
  - Maintain existing functionality
  - _Leverage: Migration patterns from previous services_
  - _Requirements: 3.4.1_
  - _Prompt: Role: Text Processing Developer | Task: Migrate TextExtractionService to use IPdfPage text extraction abstraction. Use ExtractTextAsync and ExtractTextWithBoundsAsync methods. | Restrictions: Must preserve all text extraction features, maintain accuracy, support bounds extraction, no PDFium references | Success: Text extraction uses abstraction, accuracy preserved, bounds extraction working

- [ ] 4.4 Update AnnotationService to use IPdfBackend
  - File: src/FluentPDF.Rendering/Services/AnnotationService.cs (modify)
  - Use IPdfPage.GetAnnotationsAsync
  - Work with IPdfAnnotation interface
  - Maintain annotation reading/writing
  - _Leverage: Service migration patterns_
  - _Requirements: 3.4.1_
  - _Prompt: Role: Annotation Feature Developer | Task: Migrate AnnotationService to use IPdfAnnotation abstraction for annotation access and manipulation. | Restrictions: Must support all annotation types, preserve read/write capability, use abstraction only, maintain performance | Success: Annotations accessed via abstraction, all types supported, read/write working

- [ ] 4.5 Update PdfFormService to use IPdfBackend
  - File: src/FluentPDF.Rendering/Services/PdfFormService.cs (modify)
  - Use IPdfDocument.FormFields collection
  - Work with IPdfFormField interface
  - Support all field types
  - _Leverage: Service migration patterns_
  - _Requirements: 3.4.1_
  - _Prompt: Role: Forms Processing Developer | Task: Migrate PdfFormService to use IPdfFormField abstraction for form field access and manipulation. | Restrictions: Must support all field types (text, checkbox, radio, combo), preserve get/set operations, use abstraction only | Success: Forms accessed via abstraction, all field types working, values read/written correctly

- [ ] 4.6 Update ThumbnailRenderingService to use IPdfBackend
  - File: src/FluentPDF.Rendering/Services/ThumbnailRenderingService.cs (modify)
  - Use IPdfPage.RenderAsync with thumbnail RenderOptions
  - Optimize for small sizes
  - Maintain caching strategy
  - _Leverage: Rendering service migration pattern_
  - _Requirements: 3.4.1_
  - _Prompt: Role: Thumbnail Optimization Specialist | Task: Migrate ThumbnailRenderingService to use IPdfPage abstraction with thumbnail-optimized RenderOptions (lower DPI, no annotations). | Restrictions: Must maintain thumbnail quality, preserve caching, optimize for small sizes, use abstraction | Success: Thumbnails generated via abstraction, caching working, quality maintained

- [ ] 4.7 Update remaining services
  - Files: Various service files
  - WatermarkService, BookmarkService, SecurityService, etc.
  - Migrate all remaining PDF services to use abstraction
  - Remove all direct PDFium references
  - _Leverage: Established migration patterns_
  - _Requirements: 3.4.1, 3.4.2_
  - _Prompt: Role: Code Migration Specialist | Task: Complete migration of all remaining PDF services to use IPdfBackend abstraction following established patterns. Remove all direct PDFium references. | Restrictions: Must migrate all services, maintain all functionality, remove PDFium references, preserve tests, no regressions | Success: All services migrated, zero PDFium references in Core, all tests pass

## Phase 5: Update Dependency Injection Configuration

- [ ] 5.1 Configure production DI for PdfiumBackend
  - File: src/FluentPDF.App/Program.cs, src/FluentPDF.Avalonia/Program.cs
  - Register PdfiumBackend as singleton IPdfBackend
  - Initialize backend on startup
  - Configure logging
  - _Leverage: Existing DI configuration_
  - _Requirements: 3.3.3_
  - _Prompt: Role: Dependency Injection Configuration Specialist | Task: Configure DI container to register PdfiumBackend as singleton IPdfBackend in production applications. Initialize on startup. | Restrictions: Must be singleton (one instance per app), initialize once, proper disposal on shutdown, configure logging | Success: Backend registered correctly, initializes on startup, services resolve backend, disposed on shutdown

- [ ] 5.2 Configure test DI for InMemoryBackend
  - File: tests/**/TestBase.cs
  - Register InMemoryBackend in test fixtures
  - Create test DI container helper
  - Update all unit tests to use InMemory backend
  - _Leverage: Existing test fixtures_
  - _Requirements: 3.3.3, NFR-4.4.1_
  - _Prompt: Role: Test Infrastructure Engineer | Task: Configure test DI container to use InMemoryBackend enabling fast unit tests without PDFium. Update test base classes. | Restrictions: Must use InMemory backend for unit tests, PDFium backend for integration tests, fast test setup, isolated tests | Success: Unit tests use InMemory backend, run in &lt;1s, integration tests use PDFium, proper isolation

- [ ] 5.3 Add backend configuration options
  - File: src/FluentPDF.Core/Configuration/PdfBackendOptions.cs
  - Create configuration options for backend selection
  - Support configuration via appsettings.json
  - Allow runtime backend switching (for testing)
  - _Leverage: IOptions pattern from Microsoft.Extensions.Configuration_
  - _Requirements: 3.3.3_
  - _Prompt: Role: Configuration Management Specialist | Task: Create PdfBackendOptions for configuring backend selection via appsettings.json with support for runtime switching. | Restrictions: Must support configuration file, environment variables, runtime switching for tests, typed options pattern | Success: Backend configurable via config, runtime switching works, options validated

## Phase 6: Architecture Testing and Validation

- [ ] 6.1 Add architecture tests for layering
  - File: tests/FluentPDF.Architecture.Tests/BackendAbstractionTests.cs
  - Verify Core project doesn't reference Pdfium project
  - Verify Core only references Abstractions
  - Verify no direct PDFium P/Invoke outside Pdfium project
  - _Leverage: NetArchTest.Rules library_
  - _Requirements: 3.4.2, 3.4.3, AC-6.1.1_
  - _Prompt: Role: Architecture Enforcement Engineer | Task: Create architecture tests using NetArchTest verifying layering rules: Core → Abstractions only, no direct PDFium references outside Pdfium project. | Restrictions: Must fail build if rules violated, verify all projects, check for P/Invoke leaks, enforce namespace boundaries | Success: Architecture tests enforce rules, build fails on violations, layering verified

- [ ] 6.2 Add performance benchmarks
  - File: tests/FluentPDF.Benchmarks/BackendBenchmarks.cs
  - Benchmark document opening with/without abstraction
  - Benchmark page rendering overhead
  - Measure memory allocations
  - Verify &lt;5% overhead target
  - _Leverage: BenchmarkDotNet_
  - _Requirements: NFR-4.1.1, NFR-4.1.2, AC-6.2.1_
  - _Prompt: Role: Performance Engineering Specialist | Task: Create comprehensive benchmarks measuring abstraction overhead for document opening, rendering, and text extraction. Verify &lt;5% overhead target. | Restrictions: Must use BenchmarkDotNet, measure overhead vs direct PDFium, memory allocations, execution time, statistical significance | Success: Benchmarks show &lt;5% overhead, memory within 10%, documented results

- [ ] 6.3 Add memory leak tests
  - File: tests/FluentPDF.Rendering.Tests/MemoryTests.cs
  - Test document/page disposal releases memory
  - Verify no SafeHandle leaks
  - Test finalizers execute
  - _Leverage: dotMemory Unit or manual GC testing_
  - _Requirements: NFR-4.2.2_
  - _Prompt: Role: Memory Management Testing Specialist | Task: Create memory leak tests verifying proper disposal of documents, pages, and native handles. Verify finalizers prevent leaks. | Restrictions: Must verify SafeHandle disposal, GC collection tested, no memory leaks detected, finalizers tested, deterministic disposal verified | Success: No memory leaks detected, SafeHandles disposed, finalizers work, stress tests pass

## Phase 7: Documentation and Cleanup

- [ ] 7.1 Write Architecture Decision Record (ADR)
  - File: docs/architecture/adr-pdfium-abstraction.md
  - Document decision to abstract PDFium
  - Explain rationale, alternatives considered, consequences
  - Include diagrams and code examples
  - _Leverage: ADR template format_
  - _Requirements: AC-6.4.1_
  - _Prompt: Role: Technical Documentation Writer | Task: Write comprehensive ADR documenting the decision to abstract PDFium including context, decision, rationale, alternatives, and consequences. | Restrictions: Must follow ADR format, include diagrams, explain trade-offs, document alternatives considered, clear consequences | Success: ADR is complete, well-structured, explains decision clearly, includes diagrams

- [ ] 7.2 Write backend implementation guide
  - File: docs/backend-implementation-guide.md
  - Document how to implement a new PDF backend
  - Include interface contracts, requirements, testing strategy
  - Provide code examples
  - _Leverage: Pdfium and InMemory implementations as examples_
  - _Requirements: AC-6.4.2_
  - _Prompt: Role: Developer Documentation Specialist | Task: Create guide for implementing alternative PDF backends covering interface contracts, disposal patterns, testing, and integration. | Restrictions: Must be actionable guide, include code examples, cover all interfaces, explain resource management, testing requirements | Success: Guide is complete, clear examples, covers all requirements, actionable steps

- [ ] 7.3 Write service migration guide
  - File: docs/service-migration-guide.md
  - Document how to migrate services from direct PDFium to abstraction
  - Include before/after examples
  - Common pitfalls and solutions
  - _Leverage: Completed service migrations as examples_
  - _Requirements: AC-6.4.3_
  - _Prompt: Role: Migration Documentation Writer | Task: Create guide for migrating existing services from direct PDFium calls to backend abstraction with examples and best practices. | Restrictions: Must include before/after code, common pitfalls, testing strategy, DI configuration, step-by-step process | Success: Migration guide is clear, examples helpful, pitfalls documented, step-by-step

- [ ] 7.4 Update API documentation
  - Files: XML documentation in abstraction interfaces
  - Comprehensive XML docs for all public APIs
  - Usage examples in remarks
  - Performance characteristics documented
  - _Leverage: Existing XML documentation patterns_
  - _Requirements: NFR-4.3.3_
  - _Prompt: Role: API Documentation Specialist | Task: Write comprehensive XML documentation for all public abstraction APIs including usage examples, performance notes, thread safety, and resource management. | Restrictions: Must document all public members, include examples, explain thread safety, performance characteristics, resource ownership | Success: All APIs documented, IntelliSense helpful, examples clear, characteristics explained

- [ ] 7.5 Remove deprecated code
  - Files: Various files in FluentPDF.Rendering
  - Remove old direct PDFium service code
  - Clean up unused interop methods
  - Update namespaces
  - _Leverage: Static code analysis to find unused code_
  - _Requirements: Clean up phase_
  - _Prompt: Role: Code Cleanup Specialist | Task: Remove deprecated direct PDFium service code after migration to abstraction. Clean up unused interop, update namespaces. | Restrictions: Must not break existing functionality, remove only unused code, update all references, run all tests after cleanup | Success: Deprecated code removed, no unused interop, namespaces clean, all tests pass

- [ ] 7.6 Update README and getting started docs
  - File: README.md, docs/getting-started.md
  - Document new abstraction architecture
  - Update setup instructions
  - Explain backend configuration
  - _Leverage: Existing documentation structure_
  - _Requirements: AC-6.4.1_
  - _Prompt: Role: User Documentation Writer | Task: Update README and getting started documentation reflecting new abstraction architecture, backend configuration, and setup instructions. | Restrictions: Must be user-friendly, explain architecture benefits, clear setup steps, configuration examples, maintain existing structure | Success: Documentation updated, architecture explained, setup clear, examples working

## Success Criteria

- ✅ All PDFium operations abstracted behind IPdfBackend
- ✅ Zero direct PDFium references outside FluentPDF.Rendering.Pdfium project
- ✅ Unit tests run in &lt;1s without PDFium using InMemoryBackend
- ✅ Integration tests pass with PdfiumBackend
- ✅ Performance overhead &lt;5% measured via benchmarks
- ✅ Memory usage within 10% of baseline
- ✅ Architecture tests enforce layering
- ✅ Test coverage &gt;90% for abstraction layer
- ✅ All services migrated and tested
- ✅ Documentation complete (ADR, guides, XML docs)
