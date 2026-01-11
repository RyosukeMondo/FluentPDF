# Tasks Document

## Implementation Tasks

- [x] 1. Add Win2D and OpenCvSharp NuGet packages
  - Files:
    - `tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj` (modify)
    - `tests/FluentPDF.Validation.Tests/FluentPDF.Validation.Tests.csproj` (modify)
  - Add Microsoft.Graphics.Win2D package (latest stable)
  - Add OpenCvSharp4 and OpenCvSharp4.runtime.win packages
  - Verify packages restore and native dependencies are included
  - Test CanvasDevice initialization
  - Purpose: Add dependencies for headless rendering and image comparison
  - _Leverage: NuGet package manager_
  - _Requirements: 1.1-1.7, 2.1-2.8_
  - _Prompt: Role: .NET Package Manager | Task: Add Win2D and OpenCvSharp packages to test projects | Restrictions: Use latest stable versions, verify native dependencies | Success: Packages restore and libraries initialize successfully_

- [x] 2. Implement HeadlessRenderingService with Win2D
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Services/IHeadlessRenderingService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/HeadlessRenderingService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/HeadlessRenderingServiceTests.cs`
  - Create IHeadlessRenderingService interface
  - Implement RenderPageToFileAsync using CanvasRenderTarget
  - Initialize CanvasDevice.GetSharedDevice()
  - Integrate with PdfiumInterop for PDF rendering
  - Convert PDFium bitmap buffer to CanvasBitmap
  - Save CanvasRenderTarget to PNG
  - Write unit tests with sample PDF
  - Purpose: Provide headless PDF to image rendering
  - _Leverage: Win2D CanvasRenderTarget, PdfiumInterop, Result<T> pattern_
  - _Requirements: 1.1-1.7_
  - _Prompt: Role: Graphics Rendering Developer | Task: Implement headless rendering service using Win2D CanvasRenderTarget | Restrictions: Must work without UI, dispose resources properly, handle errors | Success: Can render PDF pages to PNG files without UI dependencies_

- [ ] 3. Create ComparisonResult model
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Models/ComparisonResult.cs`
    - `tests/FluentPDF.Rendering.Tests/Models/ComparisonResultTests.cs`
  - Create ComparisonResult with SsimScore, Passed, Threshold, paths properties
  - Add validation
  - Write unit tests
  - Purpose: Model for visual comparison results
  - _Leverage: Existing model patterns_
  - _Requirements: 2.4-2.8_
  - _Prompt: Role: Domain Modeling Developer | Task: Create ComparisonResult model for SSIM comparison results | Restrictions: Keep model immutable, include all necessary paths | Success: Model represents comparison results with SSIM score and image paths_

- [ ] 4. Implement VisualComparisonService with SSIM
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Services/IVisualComparisonService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/VisualComparisonService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/VisualComparisonServiceTests.cs`
  - Create IVisualComparisonService interface
  - Implement CompareImagesAsync using OpenCvSharp
  - Load images as OpenCV Mat objects
  - Convert to grayscale and calculate SSIM
  - Generate difference image with red highlighting
  - Write unit tests with known image pairs
  - Purpose: Provide perceptual image comparison
  - _Leverage: OpenCvSharp SSIM, Result<T> pattern_
  - _Requirements: 2.1-2.8, 7.1-7.7_
  - _Prompt: Role: Computer Vision Developer | Task: Implement visual comparison service using OpenCvSharp SSIM | Restrictions: Must generate diff images, handle size mismatches, use proper thresholds | Success: Can compare images and generate perceptual similarity scores with diff visualization_

- [ ] 5. Implement BaselineManager for baseline storage
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Services/IBaselineManager.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/BaselineManager.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/BaselineManagerTests.cs`
  - Create IBaselineManager interface
  - Implement GetBaselinePath with category/test/page structure
  - Implement BaselineExists check
  - Implement CreateBaselineAsync and UpdateBaselineAsync
  - Write unit tests with temporary baseline directory
  - Purpose: Manage visual test baselines
  - _Leverage: File system operations, Result<T> pattern_
  - _Requirements: 3.1-3.8_
  - _Prompt: Role: Test Infrastructure Developer | Task: Implement baseline manager for visual test baseline storage | Restrictions: Must organize by category, create directories, handle missing baselines | Success: Can create, check, and manage baseline images in organized structure_

- [ ] 6. Create VisualRegressionTestBase class
  - Files:
    - `tests/FluentPDF.Validation.Tests/VisualRegressionTestBase.cs`
    - `tests/FluentPDF.Validation.Tests/VisualRegressionTestBaseTests.cs`
  - Create abstract base class implementing IDisposable
  - Inject IHeadlessRenderingService, IVisualComparisonService, IBaselineManager
  - Implement AssertVisualMatch helper method
  - Add test results directory management
  - Handle first-run baseline creation
  - Throw descriptive VisualRegressionException on failure
  - Write unit tests for base class behavior
  - Purpose: Provide common functionality for visual tests
  - _Leverage: xUnit, FluentAssertions, all visual services_
  - _Requirements: 4.1-4.8_
  - _Prompt: Role: Test Framework Developer | Task: Create base class for visual regression tests with helper methods | Restrictions: Must handle first-run, create clear failure messages, dispose resources | Success: Provides reusable infrastructure for visual tests_

- [ ] 7. Create sample visual regression tests
  - Files:
    - `tests/FluentPDF.Validation.Tests/CoreRenderingVisualTests.cs`
    - `tests/FluentPDF.Validation.Tests/ZoomVisualTests.cs`
    - `tests/Fixtures/sample.pdf` (add sample PDFs)
  - Create CoreRenderingVisualTests with simple PDF tests
  - Create ZoomVisualTests with zoom level tests
  - Add [Trait("Category", "VisualRegression")] to all tests
  - Add sample PDF files to fixtures
  - Run tests to generate initial baselines
  - Purpose: Demonstrate visual regression testing
  - _Leverage: VisualRegressionTestBase_
  - _Requirements: 4.1-4.8, 6.1-6.6_
  - _Prompt: Role: QA Test Developer | Task: Create sample visual regression tests using test base class | Restrictions: Must use trait categories, test various scenarios, include sample PDFs | Success: Visual tests run and generate baselines for comparison_

- [ ] 8. Set up test results directory and .gitignore
  - Files:
    - `tests/TestResults/.gitignore`
    - `.gitignore` (update)
  - Create tests/TestResults directory
  - Add .gitignore excluding all TestResults contents
  - Ensure tests/Baselines are committed to version control
  - Purpose: Organize test outputs and version control
  - _Requirements: 3.1-3.8, 8.1-8.7_
  - _Prompt: Role: DevOps Engineer | Task: Set up test results directory structure with proper .gitignore | Restrictions: Must exclude test outputs, include baselines | Success: TestResults ignored, Baselines committed_

- [ ] 9. Add GitHub Actions workflow for visual tests
  - Files:
    - `.github/workflows/visual-regression.yml` (create)
  - Create workflow running on windows-latest
  - Add steps: checkout, setup .NET, restore, build, test
  - Filter tests: --filter "Category=VisualRegression"
  - Upload test results artifacts on failure
  - Upload baselines on baseline changes
  - Purpose: Run visual tests in CI pipeline
  - _Leverage: Existing GitHub Actions workflows_
  - _Requirements: 5.1-5.7_
  - _Prompt: Role: CI/CD Engineer | Task: Create GitHub Actions workflow for visual regression tests | Restrictions: Must run on Windows, upload artifacts on failure, use proper filters | Success: Visual tests run in CI and upload comparison images on failures_

- [ ] 10. Add performance benchmarks for visual tests
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Performance/VisualTestPerformanceBenchmarks.cs`
  - Create BenchmarkDotNet benchmarks for rendering and SSIM
  - Measure time for headless rendering
  - Measure time for SSIM calculation
  - Document baseline performance metrics
  - Purpose: Measure visual test performance
  - _Leverage: BenchmarkDotNet_
  - _Requirements: 8.1-8.7_
  - _Prompt: Role: Performance Engineer | Task: Create performance benchmarks for visual regression tests | Restrictions: Must use BenchmarkDotNet, measure key operations | Success: Benchmarks document rendering and comparison performance_

- [ ] 11. Document baseline update workflow
  - Files:
    - `docs/VISUAL-TESTING.md` (create)
    - `tests/Baselines/README.md` (create)
  - Document how to run visual tests locally
  - Explain baseline creation on first run
  - Document how to review and approve visual changes
  - Provide examples of updating baselines
  - Explain test failure output (baseline, actual, diff)
  - Purpose: Guide developers on visual testing workflow
  - _Requirements: 3.1-3.8_
  - _Prompt: Role: Technical Writer | Task: Document visual regression testing workflow and baseline management | Restrictions: Must be clear and actionable, include examples | Success: Developers can understand and use visual testing_

- [ ] 12. Final testing and documentation
  - Files:
    - `docs/ARCHITECTURE.md` (update)
    - `docs/TESTING.md` (update)
    - `README.md` (update)
  - Run full visual test suite and verify all pass
  - Test on different Windows versions and configurations
  - Update architecture documentation with visual testing components
  - Update testing documentation with visual regression info
  - Update README with visual testing feature
  - Verify CI workflow runs successfully
  - Verify all requirements met
  - Purpose: Ensure feature is complete and documented
  - _Leverage: All previous tasks_
  - _Requirements: All requirements_
  - _Prompt: Role: Technical Writer and QA Lead | Task: Complete final validation and documentation for visual regression testing | Restrictions: Must verify all requirements, test CI integration | Success: Feature is production-ready with comprehensive documentation_

## Summary

This spec implements visual regression testing:
- Win2D headless rendering for CI-compatible screenshots
- OpenCvSharp SSIM comparison for perceptual similarity
- Baseline management with version control
- Test base class for easy test creation
- GitHub Actions CI integration with artifacts
- Comprehensive documentation and examples
- Performance benchmarks for monitoring
