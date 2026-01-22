# Requirements Document

## Introduction

FluentPDF integrates enterprise-grade native libraries (PDFium, QPDF) with modern WinUI 3 UI through P/Invoke marshaling. While the existing codebase has foundational marshaling verification infrastructure, several high-risk areas have been identified through operational experience:

1. **UTF-16LE string encoding mismatches** causing buffer overruns in bookmark extraction and text search
2. **Bitmap buffer marshaling** with potential stride calculation overflows for large images
3. **Threading model constraints** requiring `Task.Yield()` workarounds to prevent AccessViolation crashes
4. **Known API workarounds** (float dimension API returning garbage values) without systematic validation
5. **Annotation geometry marshaling** with manual struct unpacking prone to errors

This spec introduces a **comprehensive marshaling verification framework** that provides CLI-centric, ultra-speed iteration tools for detecting, preventing, and fixing marshaling issues before they reach production. The framework aligns with FluentPDF's "Verifiable Architecture" principle by making marshaling correctness testable, observable, and continuously validated.

## Alignment with Product Vision

This feature directly supports multiple core principles from `product.md`:

- **Quality Over Features**: Ensuring PDFium integration is rock-solid before expanding functionality prevents the "low-quality app" trap that plagues competitors
- **Verifiable Architecture**: Marshaling verification provides concrete proof of correctness, not just "it seems to work"
- **Open Source Foundation**: Leveraging PDFium effectively requires mastering its C API—this framework documents and validates that mastery
- **Standards Compliance**: ISO 32000 compliance requires correct marshaling of PDF data structures; errors here cause silent data corruption

**Success Metric Impact:**
- **Rendering Quality ≥ 95% ISO conformance**: Marshaling errors are the #1 cause of rendering failures (garbage values, crashes, memory corruption)
- **App Launch Time < 2 seconds**: Marshaling verification at development time (not runtime) ensures zero performance overhead
- **Memory Usage < 200MB**: Proper buffer marshaling prevents memory leaks and double-frees

## Requirements

### Requirement 1: High-Risk Area Marshaling Validators

**User Story:** As a **developer**, I want **automated validators for each high-risk marshaling area**, so that **I can detect marshaling errors immediately during development without manual testing**.

#### Acceptance Criteria

1. WHEN the CLI command `--validate-utf16-marshalling` is executed with a test PDF THEN the system SHALL validate UTF-16LE string marshaling for bookmark extraction, text search, and form fields with expected vs. actual comparisons
2. WHEN the CLI command `--validate-bitmap-marshalling` is executed THEN the system SHALL test bitmap buffer access with various image sizes (including edge cases: 1x1, 8192x8192, non-power-of-2 dimensions) and validate stride calculations, buffer sizes, and pixel data integrity
3. WHEN the CLI command `--validate-annotation-marshalling` is executed THEN the system SHALL verify annotation geometry marshaling (FS_QUADPOINTSF, FS_RECTF structs) with comprehensive test cases covering all 8 coordinates
4. WHEN the CLI command `--validate-threading-model` is executed THEN the system SHALL verify the Task.Yield() threading workaround by attempting concurrent PDFium operations and detecting AccessViolation scenarios
5. IF any validation fails THEN the system SHALL output structured JSON error reports containing: function name, expected behavior, actual behavior, stack trace, and suggested fix

### Requirement 2: API Signature Compliance Scanner

**User Story:** As a **developer**, I want **automated scanning of all P/Invoke signatures against PDFium's official API specification**, so that **I can detect signature mismatches before they cause runtime crashes**.

#### Acceptance Criteria

1. WHEN the CLI command `--scan-pdfium-signatures` is executed THEN the system SHALL parse all `[DllImport]` declarations in `PdfiumInterop.cs` and extract calling convention, return type, parameter types, and marshaling attributes
2. WHEN signature scanning is executed THEN the system SHALL compare extracted signatures against a reference specification (PDFium public API headers or curated JSON manifest) and flag mismatches in parameter count, type mismatch, or incorrect calling convention
3. WHEN a signature mismatch is detected THEN the system SHALL report: function name, expected signature, actual signature, severity (critical/warning), and example of potential failure scenario
4. WHEN the CLI command `--export-signature-report` is executed THEN the system SHALL generate a machine-readable JSON report and a human-readable HTML report with color-coded severity levels
5. IF signature scan detects critical issues THEN the exit code SHALL be non-zero to enable CI/CD blocking

### Requirement 3: Known Workaround Regression Detector

**User Story:** As a **developer**, I want **automated tests that validate all known marshaling workarounds**, so that **PDFium library updates don't silently break existing fixes**.

#### Acceptance Criteria

1. WHEN the CLI command `--test-workarounds` is executed THEN the system SHALL run validation tests for all documented workarounds including: page dimension float API workaround, Task.Yield() threading workaround, and SoftwareBitmapSource WinUI workaround
2. WHEN testing the page dimension float API workaround THEN the system SHALL call both `FPDF_GetPageWidth` (integer) and `FPDF_GetPageWidthF` (float) APIs and verify that integer API returns valid values while documenting float API behavior
3. WHEN testing the threading model workaround THEN the system SHALL attempt to execute PDFium operations via `Task.Run()` and verify that the test framework correctly detects AccessViolation (without crashing the test process)
4. WHEN testing the SoftwareBitmapSource workaround THEN the system SHALL validate that bitmap conversion succeeds using the current workaround approach
5. IF any workaround test fails THEN the system SHALL generate a detailed report indicating which workaround broke and the likely root cause (e.g., "PDFium version update may have fixed float API—consider removing workaround")

### Requirement 4: Buffer Overflow & Memory Safety Validator

**User Story:** As a **developer**, I want **automated detection of buffer overflow risks in Marshal.Copy operations**, so that **large PDF documents don't cause memory corruption or crashes**.

#### Acceptance Criteria

1. WHEN the CLI command `--validate-buffer-safety` is executed THEN the system SHALL test all `Marshal.Copy` operations with edge-case buffer sizes including: zero-length buffers, 1-byte buffers, 2GB buffers (if physically possible), and stride calculations for 8192x8192 BGRA32 images
2. WHEN testing bitmap buffer marshaling THEN the system SHALL validate that `stride × height` calculations use checked arithmetic or explicit overflow detection before allocation
3. WHEN testing string buffer marshaling (bookmark titles, text extraction) THEN the system SHALL validate that two-phase allocation (get length, allocate buffer, fill buffer) correctly handles null terminators and UTF-16LE encoding
4. WHEN buffer overflow is detected in simulation THEN the system SHALL report: function name, calculated buffer size, actual buffer size, overflow amount, and suggested mitigation (e.g., "Use checked arithmetic")
5. IF any buffer safety test fails THEN the exit code SHALL be non-zero and structured logs SHALL include memory dump context for debugging

### Requirement 5: Marshaling Performance Profiler

**User Story:** As a **developer**, I want **performance profiling of all marshaling operations**, so that **I can identify and optimize performance bottlenecks in the P/Invoke layer**.

#### Acceptance Criteria

1. WHEN the CLI command `--profile-marshalling --output profiling-report.json` is executed THEN the system SHALL measure execution time for each PDFium P/Invoke function across 1000 invocations and calculate min, max, median, P95, and P99 latencies
2. WHEN marshaling profiling is executed THEN the system SHALL measure memory allocation overhead for each marshaling operation (managed buffer allocations, unmanaged memory pinning)
3. WHEN profiling bitmap rendering operations THEN the system SHALL measure: time to create bitmap, time to render page, time to copy buffer via Marshal.Copy, and time to destroy bitmap
4. WHEN the CLI command `--profile-marshalling --compare-baseline baseline.json` is executed THEN the system SHALL compare current performance against a saved baseline and flag regressions > 20% slowdown with severity: warning or critical
5. IF profiling detects performance regressions THEN the system SHALL output a detailed report highlighting: function name, baseline time, current time, regression percentage, and suggested investigation areas

### Requirement 6: Continuous Marshaling Validation in CI/CD

**User Story:** As a **DevOps engineer**, I want **marshaling validation integrated into CI/CD pipelines**, so that **PRs introducing marshaling bugs are automatically rejected before merge**.

#### Acceptance Criteria

1. WHEN the CLI command `--validate-all` is executed THEN the system SHALL run all validation suites (UTF-16 marshaling, bitmap marshaling, annotation marshaling, signature compliance, workaround tests, buffer safety) in parallel and aggregate results
2. WHEN `--validate-all` completes THEN the system SHALL output a unified JSON report containing: total tests run, passed count, failed count, critical failures, warnings, and execution time
3. WHEN any critical validation fails (signature mismatch, buffer overflow detected, threading model violation) THEN the exit code SHALL be 1 to fail CI builds
4. WHEN the CLI command `--validate-all --junit-output results.xml` is executed THEN the system SHALL export results in JUnit XML format for Azure DevOps / GitHub Actions integration
5. IF CI/CD integration is configured THEN validation failures SHALL produce annotated PR comments with: failed test name, error details, file path references, and suggested fixes

### Requirement 7: Marshaling Knowledge Base & Documentation

**User Story:** As a **developer**, I want **comprehensive documentation of all marshaling patterns and pitfalls**, so that **I can write new P/Invoke code correctly the first time**.

#### Acceptance Criteria

1. WHEN the CLI command `--generate-marshalling-docs --output docs/` is executed THEN the system SHALL generate markdown documentation covering: safe marshaling patterns, known pitfalls, UTF-16LE encoding rules, buffer safety guidelines, and threading model constraints
2. WHEN documentation is generated THEN it SHALL include code examples for each high-risk area with side-by-side "incorrect vs. correct" implementations
3. WHEN documentation is generated THEN it SHALL include a decision tree flowchart: "How to marshal this data type" with branches for strings, arrays, structs, callbacks, and raw buffers
4. WHEN the CLI command `--explain-function <function_name>` is executed THEN the system SHALL output detailed marshaling documentation for that specific function including: parameter marshaling requirements, return value handling, known issues, and example usage
5. IF a developer adds a new P/Invoke function without following documented patterns THEN static analysis (via Roslyn analyzer) SHALL emit warnings flagging potential marshaling issues

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility Principle**: Validators for each high-risk area (UTF-16, bitmaps, annotations, etc.) must be isolated classes with single, testable purposes
- **Modular Design**: Marshaling validation framework must be a standalone library (`FluentPDF.Rendering.Interop.Validation`) that can be consumed by CLI, unit tests, and future tooling
- **Dependency Management**: Validation framework must depend only on PDFium interop layer and standard .NET libraries—no WinUI 3 dependencies to enable headless execution in CI
- **Clear Interfaces**: Define `IMarshallingValidator` interface that all validators implement with standardized `ValidationResult` return type

### Performance

- **Validation Execution Time**: Full validation suite (`--validate-all`) must complete in < 30 seconds on CI hardware (GitHub Actions Standard runner)
- **Profiling Overhead**: Marshaling profiler must introduce < 5% overhead compared to normal execution (enable conditional compilation for release builds)
- **Memory Footprint**: Validation framework must use < 50MB additional memory for test data and result aggregation

### Security

- **No Production Runtime Cost**: Marshaling validation must execute only during development/testing—zero code paths in production builds
- **Test PDF Safety**: Validation framework must use synthetic or known-safe PDF test files—no user-uploaded PDFs to prevent information disclosure
- **Crash Isolation**: Tests validating AccessViolation scenarios must run in isolated processes to prevent test runner crashes

### Reliability

- **CI/CD Stability**: Validation tests must have 100% deterministic results (no flaky tests due to timing, threading, or environment differences)
- **Cross-Version Compatibility**: Validation framework must detect PDFium version mismatches and warn if test expectations are based on different library versions
- **Graceful Degradation**: If PDFium library is missing or incompatible, validation must emit clear error messages (not cryptic DllNotFoundException)

### Usability

- **Developer Ergonomics**: CLI commands must use consistent naming (`--validate-*`, `--profile-*`, `--test-*`) with `--help` documentation for every flag
- **Error Message Quality**: Validation failures must provide actionable error messages: "Expected UTF-16LE with null terminator, got UTF-8" (not "marshaling failed")
- **IDE Integration**: Validation results must include file paths and line numbers that IDE parsers recognize (for clickable error navigation)
- **Report Formats**: Support multiple output formats: console (human-readable), JSON (machine-readable), JUnit XML (CI integration), HTML (management reports)

## Future Enhancements

### Phase 1 (This Spec): Core Validation Framework
- High-risk area validators (UTF-16, bitmaps, annotations, threading, buffer safety)
- API signature compliance scanning
- Known workaround regression tests
- CLI commands for all validators
- CI/CD integration with JUnit XML output

### Phase 2: Advanced Validation & Monitoring
- **Fuzzing Infrastructure**: Integrate with SharpFuzz or libFuzzer to generate malformed PDFs and detect marshaling crashes
- **Runtime Marshaling Telemetry**: Optional instrumentation for production builds that logs marshaling exceptions to Serilog for field diagnostics
- **Visual Regression for Marshaling**: Extend visual regression tests to detect rendering differences caused by marshaling errors

### Phase 3: Proactive Marshaling Assistance
- **Roslyn Analyzer**: Real-time IDE warnings for unsafe marshaling patterns (e.g., "Missing null terminator in UTF-16 string")
- **Code Generation**: Auto-generate safe P/Invoke wrappers from PDFium API specifications (similar to ClangSharp approach)
- **Benchmarking Dashboard**: Continuous performance tracking with historical trend charts for marshaling overhead
