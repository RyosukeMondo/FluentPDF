# Requirements Document

## Introduction

The Visual Regression Testing feature implements automated visual quality assurance for FluentPDF's PDF rendering pipeline. This spec establishes a headless rendering system using Win2D CanvasRenderTarget for CI-compatible screenshot capture, SSIM (Structural Similarity Index) comparison via OpenCvSharp for perceptual difference detection, and comprehensive baseline management for tracking visual changes across releases.

The feature provides:
- **Headless Rendering**: Capture PDF renders without UI dependencies using Win2D
- **Perceptual Comparison**: Detect visual regressions using SSIM metrics
- **Baseline Management**: Store and version-control visual baselines
- **CI Integration**: Run visual tests in GitHub Actions with artifact generation
- **Regression Detection**: Automatically identify rendering changes and flag for review

## Alignment with Product Vision

This spec supports the product principle of "Verifiable Architecture" by providing continuous visual quality monitoring.

Supports product principles:
- **Quality Over Features**: Automated visual validation ensures rendering quality
- **Verifiable Architecture**: Measurable visual quality metrics with SSIM scores
- **AI-Assisted Development**: Automated regression detection with actionable reports
- **Observable System**: Visual test results integrated into CI/CD observability

Aligns with tech decisions:
- Win2D for headless rendering
- OpenCvSharp for SSIM calculation
- GitHub Actions for CI integration
- ArchUnitNET patterns for test organization

## Requirements

### Requirement 1: Win2D Headless Rendering Integration

**User Story:** As a developer, I want to capture PDF renders without UI, so that visual tests can run in CI environments.

#### Acceptance Criteria

1. WHEN Win2D NuGet package is added THEN it SHALL be compatible with .NET 8 and Windows
2. WHEN CanvasDevice is created THEN it SHALL initialize without requiring UI thread or window
3. WHEN CanvasRenderTarget is created THEN it SHALL support standard resolutions (1920x1080, 1280x720)
4. WHEN PDF page is rendered THEN it SHALL output to CanvasRenderTarget instead of WinUI control
5. WHEN rendering completes THEN it SHALL save CanvasRenderTarget to PNG file
6. IF Win2D initialization fails THEN it SHALL return Result.Fail with error code "WIN2D_INIT_FAILED"
7. WHEN tests run THEN they SHALL execute without requiring interactive desktop session

### Requirement 2: SSIM Comparison with OpenCvSharp

**User Story:** As a developer, I want to compare rendered PDFs using perceptual similarity metrics, so that minor rendering differences don't cause false positives.

#### Acceptance Criteria

1. WHEN OpenCvSharp NuGet package is added THEN it SHALL include native OpenCV binaries
2. WHEN two PNG images are provided THEN it SHALL load them as OpenCV Mat objects
3. WHEN images are loaded THEN it SHALL convert to grayscale for SSIM calculation
4. WHEN SSIM is calculated THEN it SHALL return score between 0.0 (completely different) and 1.0 (identical)
5. WHEN comparing identical images THEN SSIM score SHALL be 1.0
6. WHEN comparing slightly different images THEN SSIM score SHALL be > 0.99 (minor differences tolerated)
7. WHEN SSIM score < threshold THEN it SHALL generate difference image highlighting changes
8. IF SSIM calculation fails THEN it SHALL return Result.Fail with error code "SSIM_CALCULATION_FAILED"

### Requirement 3: Baseline Snapshot Management

**User Story:** As a developer, I want to manage visual baselines, so that I can track and approve visual changes over time.

#### Acceptance Criteria

1. WHEN a baseline is created THEN it SHALL be stored in `tests/Baselines/{test-name}/{page-number}.png`
2. WHEN a test runs THEN it SHALL compare against existing baseline
3. WHEN no baseline exists THEN it SHALL create initial baseline and pass test
4. WHEN baseline exists AND SSIM score >= threshold THEN test SHALL pass
5. WHEN SSIM score < threshold THEN test SHALL fail and save actual + diff images to `tests/TestResults/`
6. WHEN baselines change THEN they SHALL be committed to version control
7. WHEN reviewing changes THEN developer SHALL manually approve or reject new baselines
8. IF multiple baselines exist for a document THEN it SHALL test all pages independently

### Requirement 4: Visual Regression Test Framework

**User Story:** As a developer, I want a test framework for visual regression tests, so that I can easily add new visual tests.

#### Acceptance Criteria

1. WHEN a visual test is created THEN it SHALL inherit from VisualRegressionTestBase
2. WHEN test runs THEN it SHALL render PDF page using PdfRenderingService
3. WHEN rendering completes THEN it SHALL capture to CanvasRenderTarget
4. WHEN image is captured THEN it SHALL save to TestResults directory
5. WHEN comparing THEN it SHALL use configurable SSIM threshold (default 0.99)
6. WHEN test fails THEN it SHALL output baseline path, actual path, diff path, and SSIM score
7. WHEN test passes THEN it SHALL log SSIM score for monitoring
8. IF rendering takes > 10 seconds THEN test SHALL timeout and fail

### Requirement 5: CI Integration with GitHub Actions

**User Story:** As a developer, I want visual tests to run in CI, so that regressions are caught before merge.

#### Acceptance Criteria

1. WHEN CI workflow runs THEN it SHALL install Win2D and OpenCvSharp dependencies
2. WHEN visual tests run THEN they SHALL execute on windows-latest runner
3. WHEN tests complete THEN it SHALL upload test results as artifacts
4. WHEN tests fail THEN it SHALL upload actual images, diff images, and baselines
5. WHEN viewing artifacts THEN developer SHALL see side-by-side comparison images
6. IF Win2D is unavailable in CI THEN it SHALL skip visual tests with warning
7. WHEN tests pass THEN it SHALL log SSIM scores for trend analysis

### Requirement 6: Test Organization and Categories

**User Story:** As a developer, I want visual tests organized by category, so that I can run specific test subsets.

#### Acceptance Criteria

1. WHEN tests are created THEN they SHALL use [Trait("Category", "VisualRegression")]
2. WHEN running visual tests THEN it SHALL filter using: `--filter "Category=VisualRegression"`
3. WHEN tests are organized THEN they SHALL group by: Core Rendering, Text Extraction, Zoom Levels, Rotation
4. WHEN baseline paths are structured THEN they SHALL use: `tests/Baselines/{category}/{test-name}/{page}.png`
5. WHEN viewing test results THEN it SHALL be clear which category failed
6. IF only text rendering changed THEN only text-related tests SHALL fail

### Requirement 7: Difference Image Generation

**User Story:** As a developer, I want to see visual differences highlighted, so that I can quickly understand what changed.

#### Acceptance Criteria

1. WHEN SSIM score < threshold THEN it SHALL generate difference image
2. WHEN difference image is created THEN it SHALL highlight changed regions in red
3. WHEN highlighting changes THEN it SHALL overlay on grayscale baseline
4. WHEN viewing difference image THEN changed pixels SHALL be clearly visible
5. WHEN difference is subtle THEN it SHALL amplify changes for visibility
6. WHEN saving diff image THEN it SHALL save to: `tests/TestResults/{test-name}-diff.png`
7. WHEN test fails THEN output SHALL include paths to: baseline, actual, and diff images

### Requirement 8: Performance and Resource Management

**User Story:** As a developer, I want visual tests to run efficiently, so that CI pipeline remains fast.

#### Acceptance Criteria

1. WHEN rendering for visual tests THEN it SHALL use standard resolution (1920x1080) by default
2. WHEN running all visual tests THEN they SHALL complete in < 5 minutes
3. WHEN tests run THEN memory usage SHALL not exceed 2GB
4. WHEN CanvasRenderTarget is disposed THEN it SHALL release GPU resources immediately
5. IF rendering fails THEN it SHALL clean up partial outputs
6. WHEN tests complete THEN all temporary files SHALL be deleted
7. WHEN running in parallel THEN tests SHALL not interfere with each other

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate rendering, comparison, and baseline management
- **Modular Design**: IVisualComparisonService, IBaselineManager independently testable
- **Test Base Classes**: VisualRegressionTestBase provides common functionality
- **Clear Interfaces**: All services expose interfaces for easy mocking

### Performance
- **Test Execution**: < 5 minutes for full visual test suite
- **Single Test**: < 10 seconds per test
- **SSIM Calculation**: < 1 second per comparison
- **Memory Usage**: < 2GB for entire test suite

### Security
- **Baseline Integrity**: Baselines checked into version control prevent tampering
- **CI Isolation**: Tests run in isolated CI environment
- **Artifact Security**: Test artifacts only accessible to authorized users

### Reliability
- **Deterministic Results**: Same PDF renders identically across runs
- **Threshold Tuning**: SSIM threshold allows minor GPU/driver differences
- **Error Recovery**: Failed tests don't block other tests from running
- **Cleanup**: All resources released even on test failure

### Usability
- **Clear Failure Messages**: Output shows baseline, actual, diff, and SSIM score
- **Easy Baseline Updates**: Simple process to approve visual changes
- **CI Artifacts**: Easy access to comparison images in GitHub Actions
- **Test Naming**: Descriptive test names clearly identify what's being tested
