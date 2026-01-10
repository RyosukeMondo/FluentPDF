# Technology Stack

## Project Type

**Desktop Application**: Modern Windows native application using WinUI 3 (Windows App SDK) with C# frontend and C++ native library integration. Distributed via Microsoft Store as MSIX package.

## Core Technologies

### Primary Language(s)
- **C# 11+** (.NET 7/8): UI layer, application logic, interop orchestration
- **C++ (MSVC)**: Native library integration layer (PDFium, QPDF wrappers)
- **Runtime**: .NET Runtime + Windows App SDK
- **Package Manager**: NuGet (C# dependencies), vcpkg (C++ libraries)

### Key Dependencies/Libraries

#### PDF Rendering & Processing
- **PDFium** (BSD-3-Clause): Core rendering engine
  - Same engine used by Chrome/Edge
  - World-class performance and ISO 32000 compliance
  - Version: Latest stable from vcpkg
  - Purpose: Page rendering, text extraction, document parsing

- **QPDF** (Apache 2.0): Structure manipulation engine
  - Lossless merge/split/optimization
  - Linearization (web optimization)
  - Encryption/decryption support
  - Version: Latest stable from vcpkg
  - Purpose: Document editing, page reorganization, file size optimization

#### Office Document Conversion
- **Mammoth.NET** (BSD-2-Clause): .docx to HTML converter
  - Lightweight semantic conversion
  - Clean HTML output suitable for PDF rendering
  - NuGet package

- **WebView2** (Microsoft, proprietary): HTML to PDF rendering
  - Chromium-based rendering engine
  - `CoreWebView2.PrintToPdfAsync` for high-quality PDF generation
  - Built-in to Windows 11, redistributable for Windows 10

#### UI Framework
- **WinUI 3** (Windows App SDK): Native Windows UI
  - DirectX-based rendering
  - Fluent Design System
  - Modern XAML controls
  - Version: Windows App SDK 1.5+

- **CommunityToolkit.Mvvm**: Modern MVVM implementation
  - Source generators for boilerplate reduction
  - ObservableProperty and RelayCommand attributes
  - WeakReferenceMessenger for decoupled communication
  - INavigationService abstraction for testable navigation

#### Error Handling & Quality Infrastructure
- **FluentResults** (MIT): Result pattern implementation
  - Type-safe error handling without exceptions
  - Rich error context (ErrorCode, Category, Severity, Context)
  - Error chaining and metadata support
  - Superior to LanguageExt for C# developers (lower learning curve)

- **PDF Validation Tools**:
  - **QPDF** (CLI): PDF structural validation and integrity checks
  - **VeraPDF** (CLI, Java-based): PDF/A compliance validation with JSON reports
  - **JHOVE** (CLI): Format identification and characterization

#### Observability & Telemetry Stack
- **Serilog**: Structured logging framework
  - JSON formatter for machine-readable logs
  - Enrichers for context (CorrelationId, Environment, Version)
  - File sink with MSIX-compatible paths (ApplicationData.LocalFolder)
  - Async writing to minimize I/O impact

- **OpenTelemetry .NET**: Telemetry collection standard
  - OTLP exporter for logs, traces, and metrics
  - Integration with Serilog via OpenTelemetry sink
  - Distributed tracing for multi-layer operations
  - Vendor-neutral observability

- **.NET Aspire Dashboard**: Development observability
  - Container-based dashboard (OTLP endpoint)
  - Real-time log search and filtering
  - Trace visualization (waterfall charts)
  - Metrics time-series graphs

#### Quality Assurance & Testing Tools
- **ArchUnitNET**: Architecture testing
  - Automated enforcement of layering rules
  - Dependency direction validation
  - Naming convention checks
  - Prevents architectural erosion

- **Verify.Xaml**: Snapshot testing for WinUI 3
  - Golden master testing for UI components
  - Image and XAML tree comparison
  - Manual approval workflow for changes
  - Regression detection

- **Win2D**: Off-screen rendering for visual tests
  - CanvasRenderTarget for headless rendering
  - Direct2D wrapper for CI environments
  - GPU-accelerated bitmap generation

- **OpenCvSharp**: Image comparison algorithms
  - SSIM (Structural Similarity Index) for perceptual comparison
  - Perceptual hashing for layout change detection
  - Tolerance-based visual regression testing

- **FlaUI**: UI automation framework
  - Native UI Automation API wrapper (faster than WinAppDriver)
  - Page Object Pattern support
  - Retry strategies for flaky test mitigation
  - AutomationId-based element discovery

- **BenchmarkDotNet**: Performance benchmarking
  - Microbenchmarking for critical paths
  - Memory diagnostics (heap allocation tracking)
  - NativeMemoryProfiler for P/Invoke memory leak detection
  - Statistical analysis of performance variations

#### AI Quality Assessment Infrastructure
- **OpenAI API / Azure OpenAI**: LLM integration for quality analysis
  - Structured outputs (JSON Schema enforcement)
  - TRX file analysis for test failure root cause identification
  - Log aggregation and anomaly detection
  - Quality trend analysis and reporting

### Application Architecture

**Layered Architecture with Native Interop:**

```
┌─────────────────────────────────────┐
│   Presentation Layer (C# + XAML)   │  ← WinUI 3 UI, MVVM pattern
├─────────────────────────────────────┤
│   Application Layer (C#)           │  ← Business logic, view models
├─────────────────────────────────────┤
│   Interop Layer (P/Invoke + C#)    │  ← Unsafe code, memory marshaling
├─────────────────────────────────────┤
│   Native Libraries (C++)           │  ← PDFium, QPDF (built via vcpkg)
└─────────────────────────────────────┘
```

**Key Architectural Patterns:**
- **MVVM (Model-View-ViewModel)**: Separation of UI from business logic
  - ViewModels are UI-agnostic POCOs (testable without WinUI runtime)
  - CommunityToolkit.Mvvm source generators eliminate boilerplate
  - INavigationService abstraction for testable navigation
- **Result Pattern**: Functional error handling over exceptions
  - FluentResults for type-safe error propagation
  - Rich error context (ErrorCode, Category, Severity, InnerError)
  - Domain failures are data, not control flow
- **P/Invoke**: Direct C# to C++ interop for performance-critical paths
- **Memory Management**: Unmanaged memory pools for zero-copy rendering
- **Dependency Injection**: Constructor injection via Microsoft.Extensions.DependencyInjection
  - Interface-based service contracts
  - Easy mocking for unit tests
  - "Bus Project" pattern: Core library with no UI dependencies
- **Threading Model**:
  - UI thread for XAML/WinUI
  - Dedicated render threads for PDFium (CPU-bound work)
  - Task-based async/await for I/O operations
- **Observability by Design**:
  - Correlation IDs for distributed tracing
  - Structured logging at architectural boundaries
  - OpenTelemetry instrumentation for all critical paths

### Data Storage

- **Primary Storage**: Local file system (user documents)
- **Configuration**: JSON files in `ApplicationData.LocalFolder`
- **Cache**: Rendered page tiles stored in memory (LRU cache) and disk cache for large documents
- **Data Formats**:
  - PDF (ISO 32000)
  - DOCX (Office Open XML)
  - HTML (for Mammoth pipeline)
  - BGRA pixel buffers (for rendering)

### External Integrations

- **File System**: Windows.Storage API (UWP) with `broadFileSystemAccess` capability
- **Printing**: Windows.Graphics.Printing API
- **Sharing**: Windows.ApplicationModel.DataTransfer (Share contract)
- **Authentication**: N/A for MVP (future: Microsoft Account for cloud sync)

### Monitoring & Dashboard Technologies

- **Diagnostics Mode**: In-app developer panel for performance metrics
  - Real-time FPS counter
  - Memory profiler (managed + native)
  - Render pipeline timing visualization
- **Telemetry**: OpenTelemetry-compliant structured telemetry
  - Opt-in, anonymized crash reports
  - Performance metrics (P95/P99 latencies)
  - Feature usage analytics
- **Logging Infrastructure**:
  - **Serilog**: Primary logging framework with JSON formatter
  - **OpenTelemetry Sink**: OTLP export for centralized analysis
  - **File Sink**: Local persistence in MSIX-safe ApplicationData.LocalFolder
  - **Correlation IDs**: End-to-end request tracing across layers
- **Development Dashboard**: .NET Aspire Dashboard (containerized)
  - Log aggregation with full-text search
  - Distributed trace visualization
  - Metrics dashboards (Grafana-style)
- **AI Quality Agent**: Automated quality analysis
  - TRX file parsing for test failure analysis
  - Log pattern recognition and anomaly detection
  - Visual regression report generation (SSIM analysis)
  - Root cause hypothesis generation

## Development Environment

### Build & Development Tools
- **Build System**: MSBuild (C# projects), CMake (for vcpkg libraries)
- **IDE**: Visual Studio 2022 with "Desktop development with C++" and ".NET desktop development" workloads
- **Package Management**:
  - **NuGet**: C# dependencies (Mammoth.NET, CommunityToolkit, etc.)
  - **vcpkg**: C++ libraries (PDFium, QPDF, dependencies like zlib, libjpeg-turbo, freetype)
- **Hot Reload**: WinUI 3 XAML Hot Reload for rapid UI iteration

### Code Quality Tools
- **Static Analysis**:
  - Roslyn analyzers for C#
  - EditorConfig for consistent style
  - SonarAnalyzer (optional)
- **Formatting**:
  - C#: .editorconfig with enforced rules
  - C++: clang-format
- **Testing Framework**:
  - **Unit Tests**: xUnit for C# business logic
    - FluentAssertions for readable assertions
    - Moq for mocking dependencies
    - AutoFixture for test data generation
  - **Architecture Tests**: ArchUnitNET
    - Layer dependency validation
    - Naming convention enforcement
    - Interface usage rules
  - **Integration Tests**: FlaUI for UI automation (replacing deprecated WinAppDriver)
    - Page Object Pattern for maintainability
    - Retry strategies for stability
  - **Visual Regression Tests**: Verify.Xaml + Win2D
    - Snapshot testing for UI components
    - SSIM-based image comparison (OpenCvSharp)
    - Headless rendering via CanvasRenderTarget
  - **Performance Tests**: BenchmarkDotNet
    - Microbenchmarking critical paths
    - Memory diagnostics (MemoryDiagnoser attribute)
    - Native memory profiling for P/Invoke
  - **PDF Validation Tests**: QPDF, VeraPDF, JHOVE integration
    - Structural integrity checks
    - PDF/A compliance validation
    - Format characterization
- **Documentation**: XML doc comments, DocFX for API documentation

### Version Control & Collaboration
- **VCS**: Git
- **Branching Strategy**: GitHub Flow (feature branches, PR to main)
- **Code Review Process**: Required PR approvals, automated CI checks
- **CI/CD**: GitHub Actions
  - Build validation on Windows Server 2022
  - vcpkg binary caching for faster builds
  - MSIX packaging and signing
  - Automated test execution:
    - Unit tests (headless)
    - Architecture tests (ArchUnitNET)
    - Visual regression tests (Win2D headless rendering)
    - Performance benchmarks (BenchmarkDotNet)
  - AI Quality Agent:
    - TRX file analysis for failure root cause
    - Log aggregation and anomaly detection
    - Quality report generation (JSON Schema-validated)
    - Slack/Teams notifications for critical regressions

### vcpkg Build Pipeline

**Critical for Native Library Integration:**

1. **Triplet Selection**: `x64-windows` (dynamic linking) for x64, `arm64-windows` for ARM64 (Surface Pro X)
2. **Build Command**:
   ```bash
   vcpkg install pdfium:x64-windows qpdf:x64-windows
   ```
3. **Output Location**: `installed/x64-windows/bin/*.dll`, `installed/x64-windows/include/`
4. **Integration**: DLLs copied to WinUI 3 project as `Content` items, deployed with MSIX package

## Deployment & Distribution

- **Target Platform**: Windows 10 (version 1809+), Windows 11
- **Distribution Method**: Microsoft Store (MSIX package)
- **Installation Requirements**:
  - Windows 10 build 17763+ or Windows 11
  - x64 or ARM64 architecture
  - .NET Runtime (included in self-contained deployment)
  - WebView2 Runtime (auto-install if missing)
- **Update Mechanism**: Microsoft Store automatic updates
- **Packaging**:
  - MSIX format with digital signature
  - App Container sandbox with `broadFileSystemAccess` capability
  - VC Runtime dependencies via `Microsoft.VCLibs` framework package

## Technical Requirements & Constraints

### Performance Requirements
- **Startup Time**: < 2 seconds cold start (measured from launch to first UI render)
- **Rendering Speed**: 60 FPS scrolling for typical PDFs (text-heavy documents)
- **Memory Usage**:
  - Base: < 50MB idle
  - Typical: < 200MB with 100-page document open
  - Large docs: Tile-based rendering to avoid OOM
- **HiDPI Rendering**: Must respect `RasterizationScale` for sharp rendering on 4K displays

### Compatibility Requirements
- **Platform Support**: Windows 10 (1809+), Windows 11, x64 and ARM64
- **PDF Compatibility**: ISO 32000-1:2008 (PDF 1.7) and PDF 2.0 features (via PDFium)
- **Office Formats**: .docx (Office Open XML) via Mammoth
- **Display Scaling**: 100%-300% DPI scaling support

### Security & Compliance
- **Sandboxing**: Full AppContainer isolation (Microsoft Store requirement)
- **File Access**: `broadFileSystemAccess` capability with user consent and Store justification
- **Code Signing**: All DLLs and executables signed with EV certificate to avoid SmartScreen warnings
- **Privacy**:
  - Zero telemetry by default
  - Optional crash reporting with anonymization
  - All document processing local (no cloud uploads)
- **Store Compliance**: Must pass Windows App Certification Kit (WACK) tests

### Scalability & Reliability
- **Document Size**: Support PDFs up to 10,000 pages via virtualization and tiling
- **Concurrency**: Handle multiple documents in separate tabs/windows
- **Error Recovery**: Graceful degradation on corrupted PDFs, detailed error messages

## Technical Decisions & Rationale

### Decision Log

1. **PDFium over MuPDF/Poppler**:
   - **Rationale**: BSD license avoids GPL/AGPL complications for commercial Store app. Chrome-level quality and maintenance ensure long-term viability.
   - **Trade-offs**: MuPDF has slightly better anti-aliasing, but licensing risk too high without commercial license purchase.

2. **QPDF for Structure Editing**:
   - **Rationale**: Apache 2.0 license, lossless operations (no recompression), industry-standard tool.
   - **Alternatives Considered**: PyPDF2 (Python, requires bundling runtime), pdftk (GPL, similar licensing issues).

3. **Mammoth + WebView2 vs LibreOffice**:
   - **Rationale**: Mammoth + WebView2 stack is ~5MB vs ~300MB for LibreOffice. Acceptable quality for semantic documents.
   - **Trade-offs**: LibreOffice has superior fidelity for complex formatting, but bloat and startup time unacceptable for MVP.

4. **P/Invoke over C++/WinRT Component**:
   - **Rationale**: Faster development, existing .NET ecosystem knowledge, easier debugging. PDFium's C API maps naturally to P/Invoke.
   - **Trade-offs**: Type safety weaker than WinRT, requires `unsafe` code blocks. Accepted for performance gains.

5. **WinUI 3 over WPF/Avalonia**:
   - **Rationale**: Native Windows 11 integration, DirectX acceleration, mandatory for Store's "modern app" requirements.
   - **Trade-offs**: WPF has more mature ecosystem, but WinUI 3 is Microsoft's strategic direction.

6. **Dynamic Linking (DLLs) over Static**:
   - **Rationale**: Easier license compliance (LGPL dependencies clearly separated), smaller binary size, easier to update libraries.
   - **Trade-offs**: Must bundle DLLs in MSIX, slightly larger package vs. single .exe.

7. **FluentResults over LanguageExt**:
   - **Rationale**: More intuitive for C# developers, object-oriented approach, easier to add metadata (ErrorCode, Context). Lower learning curve than functional programming paradigm.
   - **Trade-offs**: LanguageExt offers more functional purity and immutability guarantees, but at cost of team productivity.

8. **Serilog + OpenTelemetry over ILogger directly**:
   - **Rationale**: Serilog provides superior structured logging ergonomics. OpenTelemetry ensures vendor neutrality and future-proofing for observability backends.
   - **Trade-offs**: Additional dependencies, but observability is non-negotiable for quality assurance.

9. **ArchUnitNET for Architecture Enforcement**:
   - **Rationale**: Prevents architectural erosion through automated testing. Critical for AI-generated code and multi-developer teams where manual reviews can't catch all violations.
   - **Trade-offs**: Requires initial setup investment, but saves exponentially more time in long-term maintenance.

10. **FlaUI over WinAppDriver**:
    - **Rationale**: WinAppDriver is deprecated/unmaintained. FlaUI directly wraps UI Automation API, faster and more reliable.
    - **Trade-offs**: Smaller community than Selenium ecosystem, but superior Windows-specific support.

11. **Win2D for Headless Visual Testing**:
    - **Rationale**: RenderTargetBitmap requires on-screen rendering, fails in CI. Win2D's CanvasRenderTarget works offline.
    - **Trade-offs**: Slightly lower-level API, but essential for CI/CD visual regression testing.

12. **SSIM over Pixel-Perfect Comparison**:
    - **Rationale**: Pixel-perfect comparison is too brittle (GPU differences, anti-aliasing). SSIM detects perceptual changes humans care about.
    - **Trade-offs**: Requires OpenCvSharp dependency, but eliminates flaky visual tests.

## Known Limitations

- **Initial Build Complexity**: vcpkg bootstrapping and PDFium build takes ~30 minutes first time. Mitigated by binary caching in CI.
- **ARM64 Testing**: Limited access to ARM64 hardware for testing. Rely on emulation and community feedback.
- **WebView2 Dependency**: Requires WebView2 runtime installation on Windows 10 (auto-handled, but adds ~100MB download for first-time users).
- **Store Review Time**: Microsoft Store certification can take 1-3 days per submission. Plan release cycles accordingly.
- **Broad File Access Justification**: `broadFileSystemAccess` requires clear justification in Store submission. Rejection risk if explanation insufficient—must emphasize "productivity tool" nature.
