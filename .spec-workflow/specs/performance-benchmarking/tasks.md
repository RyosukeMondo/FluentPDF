# Tasks Document

## Implementation Tasks

- [x] 1. Create FluentPDF.Benchmarks project and configure BenchmarkDotNet
  - Files:
    - `tests/FluentPDF.Benchmarks/FluentPDF.Benchmarks.csproj` (create)
    - `tests/FluentPDF.Benchmarks/Program.cs` (create)
    - `tests/FluentPDF.Benchmarks/Config/BenchmarkConfig.cs` (create)
  - Create new .NET 8 console application for benchmarks
  - Add BenchmarkDotNet NuGet package (latest stable)
  - Add NativeMemoryProfiler NuGet package
  - Create shared BenchmarkConfig with MemoryDiagnoser, NativeMemoryProfiler, exporters
  - Configure Program.cs to use BenchmarkRunner with ManualConfig
  - Purpose: Establish benchmarking infrastructure with proper configuration
  - _Leverage: BenchmarkDotNet framework, existing project structure patterns_
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_
  - _Prompt: Role: Performance Engineer specializing in .NET benchmarking and BenchmarkDotNet | Task: Create FluentPDF.Benchmarks console project with BenchmarkDotNet configured for statistical analysis, memory profiling (managed + native), and multiple export formats (JSON, HTML, Markdown). Configure shared BenchmarkConfig class with MemoryDiagnoser, NativeMemoryProfiler, warmup/iteration counts, and Release-only execution. Set up Program.cs to run benchmarks using BenchmarkRunner.Run<T>(config). | Restrictions: Must enforce Release configuration, do not allow Debug mode benchmarks, include JitOptimizationsValidator.FailOnError, use Core 8.0 runtime and x64 platform. | Success: Project builds successfully, BenchmarkDotNet runs without errors, configuration includes all required diagnosers and exporters, Program.cs can execute benchmark suites via command-line arguments._

- [x] 2. Create sample PDF fixtures for benchmarking
  - Files:
    - `tests/FluentPDF.Benchmarks/Fixtures/text-heavy.pdf` (add)
    - `tests/FluentPDF.Benchmarks/Fixtures/image-heavy.pdf` (add)
    - `tests/FluentPDF.Benchmarks/Fixtures/vector-graphics.pdf` (add)
    - `tests/FluentPDF.Benchmarks/Fixtures/complex-layout.pdf` (add)
  - Create or source representative PDF samples for different workload types
  - Add 3-5 page PDFs for each category (text, images, vectors, complex)
  - Ensure files are small enough to commit (< 5MB each)
  - Document PDF characteristics in README in Fixtures folder
  - Purpose: Provide realistic test data for benchmarking different rendering scenarios
  - _Leverage: Existing test fixtures from tests/Fixtures/, PDFium test suite_
  - _Requirements: 2.2_
  - _Prompt: Role: QA Engineer specializing in test data creation | Task: Create or source sample PDF files representing different workload types for benchmarking: text-heavy (plain text document), image-heavy (photo gallery), vector-graphics (technical diagrams), complex-layout (magazine-style). Each PDF should be 3-5 pages and < 5MB. Add to tests/FluentPDF.Benchmarks/Fixtures/ and document characteristics in Fixtures/README.md. | Restrictions: PDFs must be redistributable (no copyright issues), files must be small enough to commit to repo, must represent realistic user documents. | Success: 4 sample PDF files added, README documents each file's characteristics, files are under 5MB each, PDFs load successfully in FluentPDF._

- [ ] 3. Implement RenderingBenchmarks suite
  - Files:
    - `tests/FluentPDF.Benchmarks/Suites/RenderingBenchmarks.cs` (create)
  - Create benchmark class with [Config(typeof(BenchmarkConfig))] attribute
  - Add GlobalSetup to initialize DI container and load sample PDFs
  - Add benchmark methods for rendering at different zoom levels (50%, 100%, 150%, 200%)
  - Add benchmarks for each PDF type (text-heavy, image-heavy, vector, complex)
  - Add GlobalCleanup to dispose documents and services
  - Verify P99 latency < 1 second for text-heavy pages at 100% zoom
  - Purpose: Measure PDF rendering performance across document types and zoom levels
  - _Leverage: IPdfDocumentService, IPdfRenderingService, BenchmarkConfig_
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9_
  - _Prompt: Role: Performance Engineer with expertise in rendering pipeline optimization | Task: Implement RenderingBenchmarks suite measuring PDF page rendering performance. Create benchmark methods for each PDF type (text-heavy, image-heavy, vector, complex) at multiple zoom levels (50%, 100%, 150%, 200%). Use [Arguments] attribute for parameterized benchmarks. Initialize services in GlobalSetup using DI container, load all sample PDFs. Implement GlobalCleanup to dispose resources. Track P50/P95/P99 latencies and memory allocations via MemoryDiagnoser. | Restrictions: Must use same service initialization as main app, do not mock services (measure real performance), ensure resources are properly disposed, keep benchmark methods focused (one operation per method). | Success: Benchmarks run successfully, P99 latency for text-heavy 100% zoom < 1 second, all PDF types render without errors, memory profiling shows allocations, results exported to JSON/HTML._

- [ ] 4. Implement MemoryBenchmarks suite
  - Files:
    - `tests/FluentPDF.Benchmarks/Suites/MemoryBenchmarks.cs` (create)
  - Create benchmark class with [MemoryDiagnoser] and [NativeMemoryProfiler] attributes
  - Add benchmark: LoadAndDispose_Document (measure document load/dispose cycle)
  - Add benchmark: RenderAndDispose_Page (measure page render and bitmap cleanup)
  - Add benchmark: LoadRender_100Pages (stress test memory management)
  - Track Gen0/1/2 collections, total allocated bytes, native memory allocations
  - Verify SafeHandles are disposed and no memory leaks occur
  - Purpose: Profile memory allocations and detect memory leaks in PDF operations
  - _Leverage: MemoryDiagnoser, NativeMemoryProfiler, IPdfDocumentService, IPdfRenderingService_
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_
  - _Prompt: Role: Memory Profiling Specialist with expertise in managed and native memory management | Task: Implement MemoryBenchmarks suite measuring memory allocations and detecting leaks. Create benchmarks for document load/dispose, page render/dispose, and 100-page stress test. Use [MemoryDiagnoser] and [NativeMemoryProfiler] attributes to track managed and native allocations. Verify SafeHandles are disposed by checking handle counts before/after operations. Track Gen0/1/2 GC collections and flag operations allocating > 1MB on LOH or > 100MB for single page. | Restrictions: Must properly dispose all resources, measure realistic workloads, do not artificially suppress GC (measure natural behavior), verify no handle leaks. | Success: Benchmarks report Gen0/1/2 collections, total allocated bytes (managed + native), operations properly dispose resources with no leaks detected, stress test completes without OOM errors._

- [ ] 5. Implement StartupBenchmarks suite
  - Files:
    - `tests/FluentPDF.Benchmarks/Suites/StartupBenchmarks.cs` (create)
  - Create benchmark class measuring initialization performance
  - Add benchmark: Initialize_PDFium (measure PDFium init time)
  - Add benchmark: Initialize_DIContainer (measure service registration and build)
  - Add benchmark: ColdStart_FullApplication (simulate full app startup)
  - Verify PDFium initializes in < 100ms
  - Verify DI container setup in < 50ms
  - Verify full cold start in < 2 seconds (P99)
  - Purpose: Measure application initialization performance and meet cold start requirement
  - _Leverage: PdfiumInterop, DI container setup from App.xaml.cs_
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_
  - _Prompt: Role: Performance Engineer specializing in application startup optimization | Task: Implement StartupBenchmarks suite measuring initialization performance. Create benchmarks for PDFium initialization (FPDF_InitLibrary), DI container setup (service registration + BuildServiceProvider), and full application cold start (PDFium + DI + first window render simulation). Run each benchmark 10 times and report median, P95, P99. Verify PDFium < 100ms, DI < 50ms, full cold start P99 < 2 seconds. | Restrictions: Must measure realistic initialization (no mocks), include all required services in DI setup, properly dispose resources between iterations, fail benchmark if P99 exceeds thresholds. | Success: PDFium initializes in < 100ms, DI container setup < 50ms, full cold start P99 < 2 seconds, benchmarks report statistical significance, results show consistent timing across iterations._

- [ ] 6. Implement NavigationBenchmarks suite
  - Files:
    - `tests/FluentPDF.Benchmarks/Suites/NavigationBenchmarks.cs` (create)
  - Create benchmark class measuring navigation and zoom operations
  - Add benchmark: Navigate_NextPage (measure page transition forward)
  - Add benchmark: Navigate_PreviousPage (measure page transition backward)
  - Add benchmark: ZoomChange_100To150 (measure zoom operation with re-render)
  - Add benchmark: JumpToPage_Random (measure random page access)
  - Purpose: Measure performance of common user interactions
  - _Leverage: IPdfRenderingService, loaded document fixture_
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  - _Prompt: Role: UX Performance Engineer specializing in interaction responsiveness | Task: Implement NavigationBenchmarks suite measuring page navigation and zoom performance. Create benchmarks for next page, previous page, zoom change (100% to 150%), and random page jump. Load document in GlobalSetup, measure render time for each navigation operation. Track P50/P95/P99 latencies to ensure smooth user experience (< 1 second for navigation, < 2 seconds for zoom). | Restrictions: Must measure full render cycle (not just page load), use realistic navigation patterns, ensure document is kept loaded between operations, properly clean up after benchmarks. | Success: Navigation benchmarks run successfully, P99 latencies < 1 second for page nav, < 2 seconds for zoom, results show consistent performance, memory allocations are tracked._

- [ ] 7. Implement BaselineManager for result storage and comparison
  - Files:
    - `tests/FluentPDF.Benchmarks/Utils/BaselineManager.cs` (create)
    - `tests/FluentPDF.Benchmarks/Baselines/` (create directory)
    - `tests/FluentPDF.Benchmarks.Tests/BaselineManagerTests.cs` (create)
  - Create BaselineManager class with SaveBaseline, LoadBaseline, Compare methods
  - Implement JSON serialization for BenchmarkRunInfo with commit SHA, date, hardware info
  - Implement baseline comparison logic calculating percent change
  - Implement regression detection with configurable thresholds (10%, 20%)
  - Store baselines as `baseline-YYYY-MM-DD-{SHA}.json`
  - Write unit tests for baseline operations
  - Purpose: Manage benchmark baselines and detect performance regressions
  - _Leverage: System.Text.Json, Result<T> pattern, FluentResults_
  - _Requirements: 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_
  - _Prompt: Role: Software Engineer specializing in data persistence and comparison algorithms | Task: Implement BaselineManager utility for storing, loading, and comparing benchmark results. Create methods: SaveBaseline (serialize BenchmarkRunInfo to JSON with commit SHA, date, hardware specs), LoadBaseline (deserialize from file), Compare (calculate percent change between current and baseline), HasRegression (detect regressions at 10%/20% thresholds). Store baselines in tests/FluentPDF.Benchmarks/Baselines/ with naming convention baseline-YYYY-MM-DD-{SHA}.json. Write comprehensive unit tests verifying JSON serialization, comparison logic, and threshold detection. | Restrictions: Use Result<T> for file I/O operations, validate baseline format before loading, handle missing baselines gracefully, do not fail if baseline doesn't exist (treat as new baseline), keep BaselineManager stateless. | Success: BaselineManager saves baselines correctly, loads and deserializes without errors, calculates percent change accurately, detects regressions at thresholds, unit tests cover all methods with edge cases (missing file, corrupted JSON, invalid data)._

- [ ] 8. Create CI benchmark workflow with regression detection
  - Files:
    - `.github/workflows/benchmark.yml` (create)
  - Create GitHub Actions workflow running on Windows Server 2022
  - Add steps: checkout, setup .NET, build native libs, run benchmarks
  - Integrate BaselineManager: load baseline from main, compare results
  - Add logic: fail build if regression > 20%, warn if 10-20%
  - Upload benchmark artifacts (HTML report, JSON results)
  - Post PR comment with benchmark summary and regression details
  - Purpose: Enable continuous performance monitoring and regression detection in CI
  - _Leverage: GitHub Actions, BaselineManager, benchmark artifacts_
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8_
  - _Prompt: Role: DevOps Engineer specializing in CI/CD and performance testing automation | Task: Create .github/workflows/benchmark.yml workflow for automated benchmark execution and regression detection. Configure workflow to run on Windows Server 2022 on push to PR and main branch. Steps: checkout code, setup .NET 8, build native libraries (PDFium), run benchmarks (dotnet run -c Release --project tests/FluentPDF.Benchmarks), load baseline from main branch using BaselineManager, compare results and detect regressions (> 20% fail, 10-20% warn), upload HTML report and JSON results as artifacts, post PR comment with benchmark summary table showing regressions. | Restrictions: Must use consistent hardware for reliable results, ensure PDFium DLLs are available, run benchmarks in Release mode only, handle missing baseline gracefully, do not fail workflow if benchmarks timeout (mark as inconclusive). | Success: Workflow runs on PR and main pushes, benchmarks execute successfully, baseline comparison works, regressions are detected and reported, HTML report is uploaded as artifact, PR comment shows clear summary with regression details, build fails if critical regression (>20%)._

- [ ] 9. Implement performance report generation
  - Files:
    - `tests/FluentPDF.Benchmarks/Reporting/ReportGenerator.cs` (create)
    - `tests/FluentPDF.Benchmarks/Reporting/Templates/report.html` (create)
  - Create ReportGenerator class to generate HTML performance reports
  - Implement report template with summary table, memory profile, charts
  - Add comparison section highlighting regressions in red, improvements in green
  - Include P50/P95/P99 latency distribution charts
  - Include memory allocation over time charts
  - Generate report from BenchmarkDotNet results and baseline comparison
  - Purpose: Provide human-readable performance reports for analysis
  - _Leverage: BenchmarkDotNet exporters, HTML/CSS for report template_
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
  - _Prompt: Role: Frontend Developer with expertise in data visualization and reporting | Task: Implement ReportGenerator to create HTML performance reports from benchmark results. Design HTML template with sections: Summary (benchmark name, P50/P95/P99, mean, stddev in table format), Memory Profile (Gen0/1/2 collections, allocated bytes, native memory), Comparison (baseline vs current with % change, highlight regressions in red and improvements in green), Charts (latency distribution histogram using Chart.js or similar). Read benchmark results from BenchmarkDotNet JSON export and baseline data, populate template, write to output file. | Restrictions: Report must be standalone HTML (no external dependencies except CDN for charting library), must be accessible without specialized tools, charts must be responsive, use semantic HTML and WCAG-compliant colors. | Success: Report generates successfully from benchmark results, HTML is well-formatted and readable, comparison section correctly highlights regressions and improvements, charts render correctly, report opens in any browser without errors._

- [ ] 10. Integration testing of full benchmark suite
  - Files:
    - `tests/FluentPDF.Benchmarks.Tests/Integration/BenchmarkSuiteTests.cs` (create)
  - Create integration tests verifying benchmark suite execution
  - Test: Run RenderingBenchmarks and verify results are generated
  - Test: Run MemoryBenchmarks and verify memory metrics are tracked
  - Test: Run StartupBenchmarks and verify timing meets requirements
  - Test: Verify BaselineManager saves and loads baselines correctly
  - Test: Verify regression detection logic with mock data
  - Purpose: Ensure benchmark infrastructure works end-to-end
  - _Leverage: BenchmarkRunner, BaselineManager, sample PDFs_
  - _Requirements: All requirements_
  - _Prompt: Role: QA Integration Engineer specializing in testing framework validation | Task: Create integration tests for benchmark suite in FluentPDF.Benchmarks.Tests. Write tests: Run RenderingBenchmarks and verify JSON/HTML output is generated, run MemoryBenchmarks and verify MemoryDiagnoser data is present, run StartupBenchmarks and verify timing results, test BaselineManager end-to-end (save, load, compare), test regression detection with mock baseline showing 5%, 15%, 25% regressions. Use BenchmarkRunner programmatically in tests to execute small benchmark subsets. | Restrictions: Keep integration tests fast (< 30 seconds total), use subset of benchmarks (not full suite), verify output files exist and are valid JSON/HTML, do not commit test output files, use [Trait("Category", "Integration")] attribute. | Success: Integration tests run successfully, benchmarks execute and generate output, BaselineManager operations verified, regression detection logic tested with mock data, tests complete in < 30 seconds, all assertions pass._

- [ ] 11. Documentation and CI integration verification
  - Files:
    - `tests/FluentPDF.Benchmarks/README.md` (create)
    - `docs/PERFORMANCE.md` (create)
    - `README.md` (update - add performance section)
  - Document how to run benchmarks locally
  - Document baseline management process
  - Document CI integration and regression detection
  - Add performance characteristics to main README
  - Verify CI workflow runs successfully on test PR
  - Purpose: Ensure benchmarking system is documented and operational
  - _Leverage: Existing documentation structure_
  - _Requirements: All requirements_
  - _Prompt: Role: Technical Writer and DevOps Engineer performing final documentation and validation | Task: Create comprehensive documentation for benchmarking system. Write tests/FluentPDF.Benchmarks/README.md explaining: how to run benchmarks locally (dotnet run -c Release), how to interpret results, benchmark suite descriptions, fixture documentation. Create docs/PERFORMANCE.md documenting: performance requirements, benchmark results, optimization strategies, historical trends. Update main README.md with performance characteristics section: app launch < 2s, page render P99 < 1s, memory < 200MB. Verify CI benchmark workflow by creating test PR with intentional regression (add Thread.Sleep), confirm CI detects and reports regression, verify HTML report artifact is uploaded. | Restrictions: Documentation must be clear for new contributors, include examples of benchmark output, link to BenchmarkDotNet documentation for advanced usage, ensure CI verification completes successfully. | Success: README.md documents local benchmark execution, PERFORMANCE.md provides comprehensive performance overview, main README updated with performance metrics, CI workflow verified with test PR showing regression detection, HTML report artifact downloadable and readable._

## Summary

This spec implements comprehensive performance benchmarking:
- BenchmarkDotNet integration with statistical analysis
- Rendering benchmarks (P50/P95/P99 latencies across document types and zoom levels)
- Memory profiling (managed + native allocations, leak detection)
- Startup benchmarks (PDFium init, DI container, cold start)
- Navigation benchmarks (page transitions, zoom operations)
- Baseline management (storage, comparison, regression detection)
- CI integration (automated benchmarks, regression alerts, PR comments)
- Performance reporting (HTML reports with charts and comparisons)

**Next steps after completion:**
- Integrate benchmarks into regular development workflow
- Use benchmark data to guide optimization efforts
- Establish performance SLOs based on benchmark baseline
- Monitor performance trends over time through baseline history
