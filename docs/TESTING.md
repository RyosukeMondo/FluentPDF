# FluentPDF Testing Strategy

This document describes the comprehensive testing strategy for FluentPDF, including test types, coverage requirements, and how to write and run tests.

## Table of Contents

- [Testing Philosophy](#testing-philosophy)
- [Testing Pyramid](#testing-pyramid)
- [Test Project Structure](#test-project-structure)
- [Running Tests](#running-tests)
- [Writing Tests](#writing-tests)
- [Architecture Tests](#architecture-tests)
- [Unit Tests](#unit-tests)
- [UI Tests](#ui-tests)
- [Coverage Requirements](#coverage-requirements)
- [CI/CD Integration](#cicd-integration)

## Testing Philosophy

FluentPDF follows these core testing principles:

1. **Tests are First-Class Code**: Tests receive the same care and attention as production code
2. **Fast Feedback**: Unit tests should complete in seconds, not minutes
3. **Headless Where Possible**: Core logic tests run without UI runtime (Linux/CI compatible)
4. **Architecture as Code**: ArchUnitNET enforces architectural rules automatically
5. **Comprehensive Coverage**: 80% minimum, 90% for critical paths

## Testing Pyramid

```
                    /\
                   /  \
                  / UI \           FlaUI automation (slow, few)
                 /------\
                /        \
               / Integr.  \        Cross-layer tests (moderate, some)
              /------------\
             /              \
            /   Unit Tests   \    Isolated tests (fast, many)
           /------------------\
          /  Architecture Tests \ ArchUnitNET rules (instant, comprehensive)
         /______________________\
```

### Test Distribution

- **Architecture Tests**: ~10 tests, run on every build (< 1 second)
- **Unit Tests**: ~500+ tests, run on every build (< 10 seconds)
- **Integration Tests**: ~50 tests, run on PR (< 30 seconds)
- **UI Tests**: ~20 tests, run on release candidate (< 2 minutes)

## Test Project Structure

```
tests/
├── FluentPDF.Architecture.Tests/    # ArchUnitNET architecture validation
│   ├── LayerTests.cs                # Dependency rules
│   ├── NamingTests.cs               # Naming conventions
│   ├── InterfaceTests.cs            # Interface patterns
│   └── ArchitectureTestBase.cs     # Shared architecture setup
│
├── FluentPDF.Core.Tests/            # Unit tests (headless)
│   ├── ErrorHandling/
│   │   └── PdfErrorTests.cs         # Error type tests
│   ├── Logging/
│   │   └── SerilogConfigurationTests.cs
│   ├── ViewModels/
│   │   └── MainViewModelTests.cs   # ViewModel tests (no UI runtime)
│   └── TestBase.cs                  # Shared test utilities
│
└── FluentPDF.App.Tests/             # UI automation tests
    ├── Views/
    │   └── MainWindowTests.cs       # FlaUI automation
    └── UITestBase.cs                # FlaUI initialization helpers
```

## Running Tests

### Run All Tests

```bash
dotnet test FluentPDF.sln
```

### Run Specific Test Projects

```bash
# Architecture tests (fastest, no dependencies)
dotnet test tests/FluentPDF.Architecture.Tests

# Core unit tests (headless, Linux-compatible)
dotnet test tests/FluentPDF.Core.Tests

# UI tests (requires Windows runtime)
dotnet test tests/FluentPDF.App.Tests
```

### Run Tests with Coverage

```bash
# Install coverage tool (first time only)
dotnet tool install --global dotnet-coverage

# Run with coverage
dotnet coverage collect "dotnet test FluentPDF.sln" -f xml -o coverage.xml

# View coverage report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

### Run Specific Tests

```bash
# By test name pattern
dotnet test --filter "FullyQualifiedName~PdfError"

# By test category (if using [Trait])
dotnet test --filter "Category=Unit"

# By class name
dotnet test --filter "ClassName~MainViewModelTests"
```

### Visual Studio Test Explorer

1. Open Test Explorer: **View** → **Test Explorer** (Ctrl+E, T)
2. Click **Run All** or right-click specific tests
3. View results, stack traces, and output
4. Debug tests by right-clicking → **Debug Selected Tests**

## Writing Tests

### General Test Structure

Follow the **Arrange-Act-Assert** pattern:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange: Set up test data and dependencies
    var sut = new SystemUnderTest();

    // Act: Execute the method being tested
    var result = sut.DoSomething();

    // Assert: Verify expected behavior
    result.Should().Be(expectedValue);
}
```

### Test Naming Conventions

Use the pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `LoadDocument_FileNotFound_ReturnsFailureResult`
- `SaveCommand_WhenLoading_CannotExecute`
- `PdfError_WithContext_IncludesContextInMetadata`

### Test Attributes

```csharp
[Fact]                                  // Single test case
public void SimpleTest() { }

[Theory]                                // Parameterized test
[InlineData(1, 2, 3)]
[InlineData(5, 5, 10)]
public void AddNumbers_ReturnsSum(int a, int b, int expected)
{
    var result = a + b;
    result.Should().Be(expected);
}

[Trait("Category", "Unit")]            // Test categorization
[Trait("Priority", "High")]
public void CriticalTest() { }
```

## Architecture Tests

### Purpose

ArchUnitNET tests enforce architectural rules at compile-time:
- Layer dependencies
- Naming conventions
- Interface patterns
- Code organization

### Example: Layer Dependency Rule

```csharp
[Fact]
public void CoreLayer_ShouldNot_DependOn_AppLayer()
{
    // Arrange
    var rule = Classes()
        .That().ResideInNamespace("FluentPDF.Core")
        .Should().NotDependOnAny(Classes()
            .That().ResideInNamespace("FluentPDF.App"))
        .Because("Core must be UI-agnostic for testability");

    // Act & Assert
    rule.Check(Architecture);
}
```

### Example: Naming Convention Rule

```csharp
[Fact]
public void ViewModels_Should_EndWith_ViewModel()
{
    var rule = Classes()
        .That().AreAssignableTo(typeof(ObservableObject))
        .And().DoNotHaveFullName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject")
        .Should().HaveNameEndingWith("ViewModel")
        .Because("ViewModels must follow naming conventions");

    rule.Check(Architecture);
}
```

### Example: Interface Pattern Rule

```csharp
[Fact]
public void Services_Should_ImplementInterfaces()
{
    var rule = Classes()
        .That().HaveNameEndingWith("Service")
        .And().AreNotInterfaces()
        .Should().ImplementInterface(typeof(object))  // Placeholder - actual logic more complex
        .Because("Services must be abstracted for DI and testing");

    rule.Check(Architecture);
}
```

### Adding New Architecture Rules

1. Identify the architectural constraint
2. Create a new test in the appropriate file:
   - `LayerTests.cs`: Dependency rules
   - `NamingTests.cs`: Naming conventions
   - `InterfaceTests.cs`: Interface patterns
3. Write the rule using ArchUnitNET fluent API
4. Add `.Because()` clause explaining the rule
5. Run tests to verify rule enforcement

## Unit Tests

### Core Unit Test Example

```csharp
public class PdfErrorTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var errorCode = "PDF_ERROR_001";
        var category = ErrorCategory.IO;
        var severity = ErrorSeverity.Error;

        // Act
        var error = new PdfError(errorCode, category, severity);

        // Assert
        error.ErrorCode.Should().Be(errorCode);
        error.Category.Should().Be(category);
        error.Severity.Should().Be(severity);
        error.Metadata.Should().ContainKey("ErrorCode");
        error.Metadata.Should().ContainKey("Category");
        error.Metadata.Should().ContainKey("Severity");
    }

    [Fact]
    public void WithContext_AddsContextMetadata()
    {
        // Arrange
        var error = new PdfError("TEST", ErrorCategory.IO, ErrorSeverity.Error);

        // Act
        var result = error.WithContext("FilePath", "/path/to/file.pdf");

        // Assert
        result.Context.Should().ContainKey("FilePath");
        result.Context["FilePath"].Should().Be("/path/to/file.pdf");
    }
}
```

### ViewModel Unit Test Example

```csharp
public class MainViewModelTests
{
    [Fact]
    public void Title_InitializesToDefaultValue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainViewModel>>();

        // Act
        var vm = new MainViewModel(logger);

        // Assert
        vm.Title.Should().Be("FluentPDF");
    }

    [Fact]
    public void LoadDocumentCommand_SetsIsLoadingDuringExecution()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainViewModel>>();
        var vm = new MainViewModel(logger);

        // Act & Assert
        vm.IsLoading.Should().BeFalse();

        // Note: Testing async command requires more complex setup
        // This is a simplified example
    }

    [Fact]
    public void SaveCommand_WhenLoading_CannotExecute()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainViewModel>>();
        var vm = new MainViewModel(logger);

        // Act
        vm.IsLoading = true;

        // Assert
        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }
}
```

### Using FluentAssertions

FluentAssertions provides readable test assertions:

```csharp
// Basic assertions
result.Should().Be(expected);
result.Should().NotBeNull();
result.Should().BeOfType<PdfDocument>();

// Collection assertions
list.Should().HaveCount(5);
list.Should().Contain(item);
list.Should().BeEmpty();

// String assertions
str.Should().StartWith("Pdf");
str.Should().Contain("Error");
str.Should().BeNullOrWhiteSpace();

// Exception assertions
Action act = () => method();
act.Should().Throw<InvalidOperationException>()
   .WithMessage("*not found*");

// Result pattern assertions
result.IsSuccess.Should().BeTrue();
result.IsFailed.Should().BeFalse();
result.Value.Should().NotBeNull();
```

### Using Mocks (NSubstitute)

```csharp
// Create mock
var mockLogger = Substitute.For<ILogger<MainViewModel>>();

// Setup behavior
mockLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

// Verify calls
mockLogger.Received(1).Log(
    LogLevel.Information,
    Arg.Any<EventId>(),
    Arg.Any<object>(),
    Arg.Any<Exception>(),
    Arg.Any<Func<object, Exception, string>>());
```

### Using AutoFixture for Test Data

```csharp
// Create fixture
var fixture = new Fixture();

// Generate test data
var randomString = fixture.Create<string>();
var randomInt = fixture.Create<int>();
var documentList = fixture.CreateMany<PdfDocument>(10);

// Customize generation
fixture.Customize<PdfDocument>(c => c
    .With(d => d.PageCount, 100)
    .Without(d => d.InternalHandle));
```

## UI Tests

### FlaUI Automation Example

```csharp
public class MainWindowTests : UITestBase
{
    [Fact]
    public void MainWindow_OpensSuccessfully()
    {
        // Arrange & Act
        using var app = LaunchApp();
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation);

        // Assert
        window.Should().NotBeNull();
        window.Title.Should().Contain("FluentPDF");
    }

    [Fact]
    public void LoadButton_Click_OpensFileDialog()
    {
        // Arrange
        using var app = LaunchApp();
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation);

        // Act
        var loadButton = window.FindFirstDescendant(cf => cf.ByAutomationId("LoadButton"));
        loadButton.Click();

        // Assert
        var fileDialog = window.ModalWindows.FirstOrDefault();
        fileDialog.Should().NotBeNull();
    }
}
```

### Visual Regression Testing

FluentPDF implements comprehensive visual regression testing using Win2D headless rendering and OpenCvSharp SSIM comparison. These tests detect rendering regressions by comparing rendered PDF pages against baseline images stored in version control.

**Test Location**: `tests/FluentPDF.Validation.Tests/`

**Trait Filter**: `Category=VisualRegression`

**Architecture**: See [ARCHITECTURE.md - Visual Regression Testing Architecture](./ARCHITECTURE.md#visual-regression-testing-architecture) for detailed architectural overview.

#### Running Visual Tests

Visual regression tests require Windows (Win2D dependency):

```bash
# Run all visual regression tests
dotnet test --filter "Category=VisualRegression"

# Run specific visual test category
dotnet test --filter "Category=VisualRegression&FullyQualifiedName~CoreRendering"

# Run on Windows only (skip on Linux/macOS)
dotnet test --filter "Category=VisualRegression" || echo "Skipped (Windows required)"
```

**Important**: Visual tests **must** run on Windows. Win2D's CanvasDevice is not available on Linux/macOS.

#### Test Base Class

All visual regression tests inherit from `VisualRegressionTestBase`:

```csharp
[Trait("Category", "VisualRegression")]
public class CoreRenderingVisualTests : VisualRegressionTestBase
{
    [Fact]
    public async Task SimpleDocument_Page1_MatchesBaseline()
    {
        // Arrange
        var pdfPath = GetTestPdfPath("sample.pdf");

        // Act & Assert
        await AssertVisualMatch(
            pdfPath,
            pageNumber: 1,
            category: "CoreRendering",
            testName: "SimpleDocument",
            threshold: 0.95);
    }
}
```

**Helper Methods**:
- `AssertVisualMatch`: Renders page, compares to baseline, asserts SSIM >= threshold
- `GetTestPdfPath`: Locates test PDF from `tests/Fixtures/`
- `CleanupTestResults`: Removes old test result files

#### First-Run Baseline Creation

When a test runs for the first time (no baseline exists):
1. Test renders the PDF page
2. Saves rendered image as baseline in `tests/Baselines/{category}/{testName}_Page{N}.png`
3. Test **passes automatically** (no comparison on first run)
4. Developer **commits baseline** to version control

**Example First Run**:
```bash
$ dotnet test --filter "FullyQualifiedName~SimpleDocument_Page1_MatchesBaseline"

# Output:
# ✓ SimpleDocument_Page1_MatchesBaseline (1.2s)
#   Baseline created: tests/Baselines/CoreRendering/SimpleDocument_Page1.png
#   IMPORTANT: Review and commit this baseline to Git!

$ git add tests/Baselines/CoreRendering/SimpleDocument_Page1.png
$ git commit -m "Add visual regression baseline for SimpleDocument"
```

#### Subsequent Runs (Comparison Mode)

When a baseline exists, tests perform visual comparison:

1. **Render Actual Image**: Render PDF page to `tests/TestResults/VisualRegression/{testName}_Page{N}_Actual.png`
2. **Load Baseline**: Load baseline from `tests/Baselines/{category}/{testName}_Page{N}.png`
3. **Calculate SSIM**: Compare images using Structural Similarity Index
4. **Generate Diff**: Create difference image highlighting changes in red
5. **Assert Pass/Fail**: Pass if SSIM >= threshold (default 0.95)

**Pass Example**:
```bash
$ dotnet test --filter "FullyQualifiedName~SimpleDocument_Page1_MatchesBaseline"

# Output:
# ✓ SimpleDocument_Page1_MatchesBaseline (0.8s)
#   SSIM: 0.9876 (threshold: 0.95) - PASS
```

**Fail Example**:
```bash
$ dotnet test --filter "FullyQualifiedName~SimpleDocument_Page1_MatchesBaseline"

# Output:
# ✗ SimpleDocument_Page1_MatchesBaseline (0.9s)
#   VisualRegressionException: Visual regression detected!
#   SSIM: 0.8742 (threshold: 0.95)
#   Baseline: tests/Baselines/CoreRendering/SimpleDocument_Page1.png
#   Actual: tests/TestResults/VisualRegression/SimpleDocument_Page1_Actual.png
#   Diff: tests/TestResults/VisualRegression/SimpleDocument_Page1_Diff.png
```

#### Reviewing Visual Failures

When a visual test fails:

1. **Download Artifacts**: If running in CI, download test result artifacts from GitHub Actions
2. **Review Diff Image**: Open `*_Diff.png` to see highlighted differences
3. **Compare Side-by-Side**: View baseline vs. actual images
4. **Determine Validity**:
   - **Valid Change**: Intentional improvement → update baseline
   - **Invalid Change**: Rendering bug → fix code

**Reviewing Diff Images**:
- Changed pixels highlighted in **red**
- Alpha blending shows change intensity
- PNG format for lossless comparison

#### Updating Baselines

When visual changes are **valid and intentional** (e.g., after PDFium upgrade, rendering improvements):

```bash
# 1. Run tests locally to regenerate baselines
dotnet test --filter "Category=VisualRegression"

# 2. Review changes in Git
git diff tests/Baselines/

# 3. Verify diff images show expected changes
# (Review tests/TestResults/VisualRegression/*_Diff.png)

# 4. Commit updated baselines
git add tests/Baselines/
git commit -m "Update visual regression baselines for PDFium 5.x upgrade"
git push
```

**Baseline Update Checklist**:
- [ ] Reviewed all diff images
- [ ] Verified changes are intentional
- [ ] Updated `tests/Baselines/README.md` with change rationale
- [ ] Committed baselines with descriptive message
- [ ] PR includes visual comparison screenshots

#### Test Categories

Visual regression tests are organized by category:

**CoreRendering** (`tests/FluentPDF.Validation.Tests/CoreRenderingVisualTests.cs`):
- Basic document rendering
- Multi-page documents
- Complex layouts

**ZoomLevels** (`tests/FluentPDF.Validation.Tests/ZoomVisualTests.cs`):
- 50%, 75%, 100%, 125%, 150%, 200% zoom
- Zoom level accuracy
- DPI scaling correctness

**Annotations** (future):
- Annotation rendering accuracy
- Annotation persistence

**Forms** (future):
- Form field rendering
- Form field overlays

#### SSIM Threshold Guidelines

| SSIM Range | Status | Interpretation | Action |
|------------|--------|----------------|--------|
| 0.95-1.00 | ✓ Pass | Excellent match | Continue |
| 0.90-0.95 | ⚠ Warning | Minor differences | Investigate, may pass |
| 0.80-0.90 | ⚠ Warning | Moderate differences | Likely regression, investigate |
| < 0.80 | ✗ Fail | Significant regression | Fix code or update baseline |

**Threshold Tuning**:
- **Strict tests** (critical UI): threshold = 0.98
- **Standard tests** (general rendering): threshold = 0.95 (default)
- **Lenient tests** (minor variations OK): threshold = 0.90

**Example with Custom Threshold**:
```csharp
[Fact]
public async Task ComplexDocument_LooseComparison()
{
    // Use lower threshold for document with fonts that vary across systems
    await AssertVisualMatch(pdfPath, 1, "CoreRendering", "ComplexDocument", threshold: 0.90);
}
```

#### CI/CD Integration

Visual regression tests run automatically in GitHub Actions on every push and PR.

**Workflow**: `.github/workflows/visual-regression.yml`

**CI Behavior**:
- **Runs on**: `windows-latest` (Win2D requirement)
- **Triggers**: Push to main, pull requests
- **Artifacts**: Uploads test results on failure (actual/diff images)

**CI Pass Example**:
```yaml
✓ Run Visual Regression Tests (45s)
  All visual tests passed
```

**CI Fail Example**:
```yaml
✗ Run Visual Regression Tests (52s)
  3 visual tests failed

Artifacts uploaded:
  - visual-test-results/SimpleDocument_Page1_Actual.png
  - visual-test-results/SimpleDocument_Page1_Diff.png
  - visual-test-results/ComplexDocument_Page2_Actual.png
  - visual-test-results/ComplexDocument_Page2_Diff.png
```

**Downloading Artifacts**:
1. Go to GitHub Actions run
2. Scroll to **Artifacts** section
3. Download `visual-test-results.zip`
4. Review diff images locally

#### Performance Characteristics

**Per-Test Performance** (Intel i7-1165G7, 16GB RAM):
- Headless rendering (96 DPI): ~245ms
- SSIM comparison: ~120ms
- Diff generation: ~85ms
- **Total**: ~450ms per test

**Test Suite Scalability**:
- 10 tests: ~4.5 seconds
- 50 tests: ~22 seconds
- 100 tests: ~45 seconds

**Memory Usage**:
- Baseline memory: ~35MB per test
- CanvasDevice (shared): ~50MB (one-time)
- Peak memory: ~200MB for full suite

#### Baseline Storage and Version Control

**Baseline Directory Structure**:
```
tests/Baselines/
├── CoreRendering/
│   ├── SimpleDocument_Page1.png
│   ├── SimpleDocument_Page2.png
│   └── ComplexDocument_Page1.png
├── ZoomLevels/
│   ├── Zoom50Percent_Page1.png
│   ├── Zoom100Percent_Page1.png
│   └── Zoom200Percent_Page1.png
└── README.md                    # Baseline update history
```

**Git LFS Recommendation** (for large baseline sets):
```bash
# Track PNG baselines with Git LFS (optional, for repos with many baselines)
git lfs track "tests/Baselines/**/*.png"
git add .gitattributes
git commit -m "Configure Git LFS for visual baselines"
```

**Baseline Size Guidelines**:
- Individual baseline: ~50-500KB (PNG)
- Total baseline set: < 50MB (without LFS)
- Use Git LFS if baselines exceed 100MB

#### Test Results (.gitignore)

Test results are excluded from version control:

```
tests/TestResults/
├── VisualRegression/
│   ├── *.png                    # Actual and diff images
│   └── .gitignore               # Ignore all contents
```

**`.gitignore`**:
```gitignore
# Exclude test results
tests/TestResults/**

# Include baselines
!tests/Baselines/**
```

#### Troubleshooting

**Issue**: `InvalidOperationException: CanvasDevice.GetSharedDevice() failed`

**Cause**: Running on Linux/macOS (Win2D requires Windows)

**Solution**: Run visual tests only on Windows:
```bash
# Skip on non-Windows
if [ "$OS" = "Windows_NT" ]; then
  dotnet test --filter "Category=VisualRegression"
fi
```

**Issue**: SSIM scores consistently below threshold after PDFium upgrade

**Cause**: Expected rendering differences from library upgrade

**Solution**: Review changes, update baselines if valid:
```bash
dotnet test --filter "Category=VisualRegression"
git diff tests/Baselines/  # Review changes
git add tests/Baselines/    # Commit if valid
```

**Issue**: Memory leaks during visual test execution

**Cause**: CanvasBitmap or OpenCV Mat objects not disposed

**Solution**: Ensure all disposable objects use `using` statements:
```csharp
using var bitmap = await _renderingService.RenderPageToFileAsync(...);
using var mat = new Mat(imagePath);
```

#### Best Practices

✅ **DO**:
- Commit baselines to version control immediately after creation
- Review diff images before updating baselines
- Document baseline updates in `tests/Baselines/README.md`
- Use descriptive test names (e.g., `ComplexLayout_WithImages_MatchesBaseline`)
- Run visual tests locally before pushing changes
- Use appropriate SSIM thresholds for test sensitivity

❌ **DON'T**:
- Commit test results (`tests/TestResults/`) to Git
- Update baselines without reviewing diff images
- Use visual tests for unit test coverage (too slow)
- Run visual tests on Linux/macOS (will fail)
- Ignore visual test failures without investigation

#### References

- [Visual Regression Testing Architecture](./ARCHITECTURE.md#visual-regression-testing-architecture)
- [Visual Testing Documentation](../docs/VISUAL-TESTING.md)
- [Baseline Update Workflow](../tests/Baselines/README.md)
- [GitHub Actions Workflow](../.github/workflows/visual-regression.yml)
- [Performance Benchmarks](../tests/FluentPDF.Rendering.Tests/Performance/VisualTestPerformanceBenchmarks.cs)

## Coverage Requirements

### Minimum Coverage Thresholds

- **Overall**: 80% line coverage minimum
- **Critical Paths**: 90% line coverage (error handling, rendering, file operations)
- **ViewModels**: 85% line coverage
- **Services**: 80% line coverage
- **Error Handling**: 95% line coverage

### Measuring Coverage

```bash
# Generate coverage report
dotnet coverage collect "dotnet test FluentPDF.sln" -f xml -o coverage.xml

# Generate HTML report
reportgenerator \
  -reports:coverage.xml \
  -targetdir:coverage-report \
  -reporttypes:Html

# Open report
start coverage-report/index.html  # Windows
```

### Coverage in CI/CD

GitHub Actions automatically:
1. Collects coverage during test run
2. Uploads coverage to Codecov (future)
3. Comments coverage % on PRs
4. Fails build if coverage drops below threshold

## PDF Rendering Tests

The PDF rendering subsystem has comprehensive test coverage across all layers, from low-level P/Invoke to high-level ViewModels.

### Test Structure

```
tests/
├── FluentPDF.Architecture.Tests/
│   └── PdfRenderingArchitectureTests.cs    # Architecture rules for PDF layer
│
├── FluentPDF.Core.Tests/
│   └── Models/
│       ├── PdfDocumentTests.cs             # PdfDocument model tests
│       └── PdfPageTests.cs                 # PdfPage model tests
│
├── FluentPDF.Rendering.Tests/
│   ├── Interop/
│   │   └── PdfiumInteropTests.cs           # P/Invoke layer tests (Windows only)
│   ├── Services/
│   │   ├── PdfDocumentServiceTests.cs      # Document service unit tests
│   │   └── PdfRenderingServiceTests.cs     # Rendering service unit tests
│   └── Integration/
│       └── PdfViewerIntegrationTests.cs    # End-to-end integration tests
│
└── FluentPDF.App.Tests/
    └── ViewModels/
        └── PdfViewerViewModelTests.cs      # ViewModel tests (headless)
```

### Architecture Tests (PdfRenderingArchitectureTests.cs)

**Purpose**: Enforce architectural boundaries and design patterns in the PDF rendering layer.

**Test Cases**:

```csharp
[Fact]
public void PInvoke_ShouldOnly_ExistIn_RenderingNamespace()
{
    // Ensures all DllImport attributes are in FluentPDF.Rendering.Interop
    // Prevents P/Invoke proliferation across the codebase
}

[Fact]
public void PInvoke_Should_UseSafeHandle_ForPointers()
{
    // Verifies SafePdfDocumentHandle and SafePdfPageHandle usage
    // Prevents raw IntPtr usage for native handles
}

[Fact]
public void ViewModels_ShouldNot_Reference_Pdfium()
{
    // Ensures ViewModels depend on service interfaces only
    // Prevents tight coupling to native layer
}

[Fact]
public void RenderingServices_Should_ImplementInterfaces()
{
    // Verifies all services implement I*Service interfaces
    // Enables dependency injection and mocking
}

[Fact]
public void CoreLayer_ShouldNot_Reference_PdfiumInterop()
{
    // Ensures Core layer remains independent of Rendering infrastructure
    // Maintains clean architecture boundaries
}
```

**Run**: `dotnet test tests/FluentPDF.Architecture.Tests --filter PdfRenderingArchitectureTests`

### Unit Tests

#### PdfiumInteropTests.cs (Windows Only)

**Purpose**: Test PDFium P/Invoke layer with real pdfium.dll.

**Important**: These tests require pdfium.dll in test output directory. Skip on Linux/macOS.

**Test Cases**:

```csharp
[Fact]
public void Initialize_ShouldSucceed()
{
    // Verifies FPDF_InitLibrary succeeds
    var interop = new PdfiumInterop();
    var result = interop.Initialize();
    result.Should().BeTrue();
}

[Fact]
public void LoadDocument_WithValidPdf_ShouldReturnValidHandle()
{
    // Uses sample.pdf from test fixtures
    var handle = interop.LoadDocument("sample.pdf", null);
    handle.Should().NotBeNull();
    handle.IsInvalid.Should().BeFalse();
}

[Fact]
public void GetPageCount_WithValidDocument_ShouldReturnCorrectCount()
{
    var handle = interop.LoadDocument("sample.pdf", null);
    var count = interop.GetPageCount(handle);
    count.Should().BeGreaterThan(0);
}

[Fact]
public void SafeHandles_ShouldDispose_WithoutLeaks()
{
    // Verifies SafePdfDocumentHandle and SafePdfPageHandle clean up
    // Uses reflection to check handle closure
}
```

**Fixtures**: Sample PDFs in `tests/Fixtures/`:
- `sample.pdf`: Valid 3-page PDF for testing
- `corrupted.pdf`: Invalid PDF for error testing
- `password-protected.pdf`: Password-protected PDF (if available)

**Run**: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfiumInteropTests`

#### PdfDocumentServiceTests.cs

**Purpose**: Test document loading service logic with mocked PdfiumInterop.

**Test Cases**:

```csharp
[Fact]
public async Task LoadDocumentAsync_WithValidFile_ReturnsSuccess()
{
    // Mock: PdfiumInterop.LoadDocument returns valid handle
    // Mock: GetPageCount returns 3
    // Assert: Result.IsSuccess == true
    // Assert: PdfDocument.PageCount == 3
}

[Fact]
public async Task LoadDocumentAsync_WithNonExistentFile_ReturnsFileNotFoundError()
{
    // Assert: Result.IsFailed == true
    // Assert: Error.Code == "PDF_FILE_NOT_FOUND"
    // Assert: Error.Context["FilePath"] == file path
}

[Fact]
public async Task LoadDocumentAsync_WithCorruptedFile_ReturnsCorruptedError()
{
    // Mock: PdfiumInterop.LoadDocument returns invalid handle
    // Assert: Result.IsFailed == true
    // Assert: Error.Code == "PDF_CORRUPTED" or "PDF_INVALID_FORMAT"
}

[Fact]
public async Task GetPageInfoAsync_WithValidPage_ReturnsPageInfo()
{
    // Mock: LoadPage, GetPageWidth, GetPageHeight
    // Assert: PdfPage.Width > 0, PdfPage.Height > 0
    // Assert: PdfPage.AspectRatio calculated correctly
}

[Fact]
public async Task GetPageInfoAsync_WithInvalidPage_ReturnsError()
{
    // Test page number < 1 and > PageCount
    // Assert: Result.IsFailed == true
}
```

**Run**: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfDocumentServiceTests`

#### PdfRenderingServiceTests.cs

**Purpose**: Test page rendering service logic with mocked PdfiumInterop.

**Test Cases**:

```csharp
[Fact]
public async Task RenderPageAsync_WithValidInputs_ReturnsBitmapImage()
{
    // Mock: LoadPage, GetPageWidth/Height, CreateBitmap, RenderPageBitmap
    // Assert: Result.IsSuccess == true
    // Assert: BitmapImage != null
}

[Fact]
public async Task RenderPageAsync_WithInvalidPage_ReturnsError()
{
    // Assert: Result.IsFailed == true
    // Assert: Error.Code == "PDF_PAGE_INVALID"
}

[Fact]
public async Task RenderPageAsync_AtDifferentZoomLevels_CalculatesCorrectSize()
{
    // Test zoom: 0.5, 1.0, 1.5, 2.0
    // Assert: Output dimensions = (page dimensions * zoom * dpi/72)
}

[Fact]
public async Task RenderPageAsync_SlowRender_LogsPerformanceWarning()
{
    // Mock: Delay rendering > 2 seconds
    // Assert: Logger.LogWarning called with performance message
}

[Fact]
public async Task RenderPageAsync_Always_DisposesBitmapAfterConversion()
{
    // Verify bitmap handle is disposed even if conversion fails
}
```

**Run**: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfRenderingServiceTests`

#### PdfViewerViewModelTests.cs

**Purpose**: Test ViewModel logic with mocked services (headless, no WinUI runtime).

**Test Cases**:

```csharp
[Fact]
public async Task OpenDocumentCommand_WithValidFile_LoadsAndRendersFirstPage()
{
    // Mock: IPdfDocumentService.LoadDocumentAsync returns success
    // Mock: IPdfRenderingService.RenderPageAsync returns BitmapImage
    // Execute: OpenDocumentCommand.Execute()
    // Assert: TotalPages > 0
    // Assert: CurrentPageNumber == 1
    // Assert: CurrentPageImage != null
}

[Fact]
public async Task GoToNextPageCommand_AdvancesPage()
{
    // Setup: Load document with 3 pages
    // Execute: GoToNextPageCommand.Execute()
    // Assert: CurrentPageNumber == 2
}

[Fact]
public void GoToNextPageCommand_OnLastPage_IsDisabled()
{
    // Setup: CurrentPageNumber == TotalPages
    // Assert: GoToNextPageCommand.CanExecute() == false
}

[Fact]
public async Task ZoomInCommand_IncreasesZoom()
{
    // Setup: ZoomLevel == 1.0
    // Execute: ZoomInCommand.Execute()
    // Assert: ZoomLevel == 1.25
    // Assert: RenderPageAsync called with new zoom
}

[Fact]
public void PropertyChanged_Fires_WhenPropertiesChange()
{
    // Verify INotifyPropertyChanged fires for all observable properties
}
```

**Run**: `dotnet test tests/FluentPDF.App.Tests --filter PdfViewerViewModelTests`

### Integration Tests (PdfViewerIntegrationTests.cs)

**Purpose**: Test complete workflow with real PDFium and sample PDFs.

**Important**: Requires Windows and pdfium.dll. Mark with `[Trait("Category", "Integration")]`.

**Test Cases**:

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task LoadDocument_WithValidPdf_Succeeds()
{
    // Use real PdfDocumentService (not mocked)
    // Load tests/Fixtures/sample.pdf
    // Assert: Result.IsSuccess
    // Assert: PageCount > 0
    // Assert: Handle is valid
    // Dispose document
}

[Fact]
[Trait("Category", "Integration")]
public async Task RenderPage_WithValidDocument_ReturnsImage()
{
    // Load sample.pdf
    // Render page 1 at 100% zoom
    // Assert: BitmapImage != null
    // Assert: Image dimensions match expected size (page size * dpi/72)
}

[Fact]
[Trait("Category", "Integration")]
public async Task RenderAllPages_Succeeds()
{
    // Load sample.pdf
    // Render all pages sequentially
    // Assert: All renders succeed
    // Assert: No errors logged
}

[Fact]
[Trait("Category", "Integration")]
public async Task ZoomLevels_RenderCorrectly()
{
    // Load sample.pdf
    // Render page 1 at 50%, 100%, 150%, 200%
    // Assert: Image sizes scale proportionally
}

[Fact]
[Trait("Category", "Integration")]
public async Task LoadCorruptedPdf_ReturnsError()
{
    // Load tests/Fixtures/corrupted.pdf
    // Assert: Result.IsFailed
    // Assert: Error.Code == "PDF_CORRUPTED" or "PDF_INVALID_FORMAT"
}

[Fact]
[Trait("Category", "Integration")]
public async Task MemoryCleanup_NoLeaks()
{
    // Load and dispose multiple documents
    // Assert: SafeHandles are disposed
    // Use reflection to check handle closure
}
```

**Run**: `dotnet test tests/FluentPDF.Rendering.Tests --filter "Category=Integration"`

### Test Fixtures

**Location**: `tests/Fixtures/`

**Files**:
- `sample.pdf`: Valid 3-page PDF (Letter size, 8.5x11 inches)
  - Page 1: Title page with text
  - Page 2: Content page with images
  - Page 3: Summary page
- `corrupted.pdf`: Invalid PDF (binary file renamed to .pdf)
- `password-protected.pdf`: PDF with user password "test123" (optional)

**Usage**:
```csharp
var samplePdfPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "sample.pdf");
var result = await documentService.LoadDocumentAsync(samplePdfPath);
```

### CI/CD Integration for PDF Tests

**build.yml modifications**:
```yaml
- name: Copy PDFium DLLs to test output
  run: |
    Copy-Item "libs/x64/bin/pdfium.dll" "tests/FluentPDF.Rendering.Tests/bin/Release/net8.0/" -Force
```

**test.yml modifications**:
```yaml
- name: Run Unit Tests (excluding Integration)
  run: dotnet test --no-build --filter "Category!=Integration"

- name: Run Integration Tests (Windows only)
  run: dotnet test --no-build --filter "Category=Integration"
  if: runner.os == 'Windows'
```

### Coverage Targets

**PDF Rendering Layer**:
- PdfiumInterop: 70% (low because many native calls, hard to test all error paths)
- PdfDocumentService: 90% (critical path, comprehensive error handling)
- PdfRenderingService: 90% (critical path, performance monitoring)
- PdfViewerViewModel: 85% (all commands, properties, error scenarios)

**Overall Target**: 80% minimum across all PDF rendering components.

## CI/CD Integration

### Test Workflow (.github/workflows/test.yml)

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-2022
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Run Architecture Tests
        run: dotnet test tests/FluentPDF.Architecture.Tests --logger "trx;LogFileName=architecture.trx"

      - name: Run Unit Tests
        run: dotnet test tests/FluentPDF.Core.Tests --logger "trx;LogFileName=core.trx"

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: "**/*.trx"

      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: "**/*.trx"
          reporter: dotnet-trx
```

### Quality Analysis (Future)

AI-powered test failure analysis:
- Parse TRX files for failure patterns
- Correlate failures with recent code changes
- Generate natural language failure summaries
- Suggest fixes based on error messages

## Best Practices

### DO

✅ Write tests before or alongside code (TDD/BDD)
✅ Keep tests simple and focused (one assertion per test when possible)
✅ Use descriptive test names that explain the scenario
✅ Mock external dependencies (file system, network, etc.)
✅ Test both success and failure paths
✅ Use FluentAssertions for readable assertions
✅ Keep test setup minimal (use TestBase classes)
✅ Run tests frequently during development

### DON'T

❌ Test framework internals (e.g., WinUI framework behavior)
❌ Write tests that depend on external services (use mocks)
❌ Use Thread.Sleep or arbitrary delays (use async properly)
❌ Ignore flaky tests (fix them or remove them)
❌ Write overly complex test setups (extract to helper methods)
❌ Test private methods directly (test through public API)
❌ Commit code without running tests locally

## Troubleshooting Tests

### Tests Fail on CI but Pass Locally

**Cause**: Environment differences (paths, Windows version, timing)

**Solutions**:
- Use `Environment.GetFolderPath()` instead of hardcoded paths
- Avoid `Thread.Sleep()`, use proper async/await
- Check for Windows version-specific APIs
- Use `[Fact(Skip = "Requires Windows 11")]` for version-specific tests

### Flaky Tests

**Cause**: Race conditions, timing issues, shared state

**Solutions**:
- Use `await Task.Delay()` with reasonable timeouts
- Ensure test isolation (no shared static state)
- Use `Task.WaitAll()` instead of sequential awaits
- Add retry logic for integration tests (use Polly)

### Tests Run Slowly

**Cause**: Too many integration/UI tests, heavy setup

**Solutions**:
- Move tests down the pyramid (more unit, fewer UI)
- Use `[Collection]` to run slow tests in parallel
- Extract expensive setup to `IClassFixture<T>`
- Use in-memory databases/mocks instead of real services

### Coverage Too Low

**Cause**: Missing tests for edge cases, error paths

**Solutions**:
- Review coverage report to find untested code
- Add tests for error handling paths
- Test boundary conditions (null, empty, max values)
- Use mutation testing (Stryker.NET) to find weak tests

## References

- [Architecture Documentation](./ARCHITECTURE.md)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [NSubstitute](https://nsubstitute.github.io/)
- [ArchUnitNET](https://archunitnet.readthedocs.io/)
- [FlaUI](https://github.com/FlaUI/FlaUI)
- [Verify.Xunit](https://github.com/VerifyTests/Verify)
