# Visual Regression Testing

## Overview

FluentPDF uses visual regression testing to ensure PDF rendering accuracy and detect unintended visual changes. The testing framework compares rendered PDF pages against baseline images using perceptual similarity metrics (SSIM - Structural Similarity Index).

## Key Features

- **Headless Rendering**: Uses Win2D for CI-compatible PDF to image conversion
- **Perceptual Comparison**: OpenCvSharp SSIM algorithm for human-like visual comparison
- **Automatic Baseline Management**: First run creates baselines automatically
- **Version-Controlled Baselines**: Baselines stored in git for team collaboration
- **Detailed Failure Reports**: Generates baseline, actual, and difference images
- **CI Integration**: GitHub Actions workflow for automated testing

## Getting Started

### Prerequisites

- Windows environment (required for Win2D)
- .NET 8 SDK
- Visual Studio 2022 or Rider

### Running Visual Tests Locally

Run all visual regression tests:

```bash
dotnet test --filter "Category=VisualRegression"
```

Run a specific test category:

```bash
dotnet test --filter "Category=VisualRegression&FullyQualifiedName~CoreRendering"
dotnet test --filter "Category=VisualRegression&FullyQualifiedName~Zoom"
```

Run a specific test:

```bash
dotnet test --filter "FullyQualifiedName~SimpleTextRendering_ShouldMatchBaseline"
```

### First Run Behavior

On the first run of a visual test (when no baseline exists):

1. The test renders the PDF page to an image
2. The rendered image is automatically saved as the baseline
3. The test passes (no comparison performed)
4. Baseline is saved to `tests/Baselines/{Category}/{TestName}/page_{number}.png`

**Important**: Review the generated baselines visually before committing them to version control!

## Baseline Management Workflow

### Creating New Baselines

When you add a new visual test:

1. Write the test using `VisualRegressionTestBase.AssertVisualMatchAsync()`
2. Run the test - it will create the baseline automatically
3. Review the baseline image in `tests/Baselines/{Category}/{TestName}/`
4. If the baseline looks correct, commit it to git
5. If incorrect, delete the baseline and fix the test/PDF, then re-run

Example:

```bash
# Run new test
dotnet test --filter "FullyQualifiedName~MyNewTest"

# Review baseline
open tests/Baselines/MyCategory/MyNewTest/page_1.png

# Commit if correct
git add tests/Baselines/MyCategory/MyNewTest/
git commit -m "Add visual baseline for MyNewTest"
```

### Reviewing Visual Changes

When a test fails, three images are generated in `tests/TestResults/{Category}/{TestName}/{timestamp}/`:

1. **baseline** (copied): The expected baseline image
2. **actual**: The newly rendered image
3. **difference**: Highlighted differences (red overlay)

Example failure output:

```
Visual regression test failed!
Category: CoreRendering
Test: SimpleTextRendering
Page: 1
SSIM Score: 0.847 (threshold: 0.950)

Images:
  Baseline: tests/Baselines/CoreRendering/SimpleTextRendering/page_1.png
  Actual:   tests/TestResults/CoreRendering/SimpleTextRendering/20260111_143022/actual_page1.png
  Diff:     tests/TestResults/CoreRendering/SimpleTextRendering/20260111_143022/difference_page1.png
```

### Updating Baselines (Accepting Visual Changes)

When intentional changes cause visual tests to fail:

1. Review the failure output and locate the images
2. Compare baseline vs actual images side-by-side
3. Verify the changes are intentional and correct
4. Copy the actual image to replace the baseline:

```bash
# Manual update
cp tests/TestResults/CoreRendering/SimpleTextRendering/20260111_143022/actual_page1.png \
   tests/Baselines/CoreRendering/SimpleTextRendering/page_1.png
```

Or use the BaselineManager programmatically:

```csharp
// In a test or utility
var baselineManager = new BaselineManager(logger);
await baselineManager.UpdateBaselineAsync(
    actualImagePath,
    "CoreRendering",
    "SimpleTextRendering",
    pageNumber: 0); // 0-based
```

4. Re-run the test to verify it passes
5. Commit the updated baseline:

```bash
git add tests/Baselines/CoreRendering/SimpleTextRendering/page_1.png
git commit -m "Update baseline for SimpleTextRendering after font rendering fix"
```

### Bulk Baseline Updates

When multiple tests fail due to intentional changes:

```bash
# Review all failures first
dotnet test --filter "Category=VisualRegression"

# For each failed test, copy actual to baseline
# Then commit all at once
git add tests/Baselines/
git commit -m "Update all baselines after rendering engine upgrade"
```

## Writing Visual Tests

### Basic Test Structure

```csharp
using FluentPDF.Rendering.Tests.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Validation.Tests;

[Trait("Category", "VisualRegression")]
public class MyVisualTests : VisualRegressionTestBase
{
    private const string TestCategory = "MyFeature";

    public MyVisualTests()
        : base(
            new HeadlessRenderingService(CreateLogger<HeadlessRenderingService>()),
            new VisualComparisonService(CreateLogger<VisualComparisonService>()),
            new BaselineManager(CreateLogger<BaselineManager>()))
    {
    }

    [Fact]
    public async Task MyFeature_ShouldMatchBaseline()
    {
        // Arrange
        var pdfPath = Path.Combine("tests", "Fixtures", "my-test.pdf");

        // Act & Assert
        await AssertVisualMatchAsync(
            pdfPath,
            TestCategory,
            "MyFeature",
            pageNumber: 1,
            threshold: 0.95);
    }

    private static ILogger<T> CreateLogger<T>()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        return loggerFactory.CreateLogger<T>();
    }
}
```

### Test Parameters

The `AssertVisualMatchAsync` method accepts:

- **pdfPath**: Path to the PDF file (relative to project root)
- **category**: Organizational category (e.g., "CoreRendering", "Zoom", "Filters")
- **testName**: Unique test name (used for baseline path)
- **pageNumber**: 1-based page number to test (default: 1)
- **threshold**: SSIM threshold for passing (0.0 to 1.0, default: 0.95)
- **dpi**: Rendering resolution (default: 96)
- **cancellationToken**: Cancellation support

### Choosing SSIM Thresholds

SSIM scores range from 0.0 (completely different) to 1.0 (identical):

- **0.95-1.0**: Recommended for most tests (strict)
- **0.93-0.95**: For tests with compression artifacts or anti-aliasing variations
- **0.90-0.93**: For complex graphics with platform-specific rendering differences
- **<0.90**: Too permissive, may miss regressions

Examples:

```csharp
// Strict threshold for text rendering
await AssertVisualMatchAsync(pdfPath, category, "Text", threshold: 0.95);

// Relaxed threshold for JPEG images with compression
await AssertVisualMatchAsync(pdfPath, category, "Images", threshold: 0.93);

// Very strict for simple vector graphics
await AssertVisualMatchAsync(pdfPath, category, "Shapes", threshold: 0.98);
```

### Testing Multiple Pages

```csharp
[Fact]
public async Task MultiPageDocument_AllPages_ShouldMatchBaseline()
{
    var pdfPath = Path.Combine("tests", "Fixtures", "multi-page.pdf");

    // Test each page separately
    await AssertVisualMatchAsync(pdfPath, TestCategory, "MultiPage_Page1", pageNumber: 1);
    await AssertVisualMatchAsync(pdfPath, TestCategory, "MultiPage_Page2", pageNumber: 2);
    await AssertVisualMatchAsync(pdfPath, TestCategory, "MultiPage_Page3", pageNumber: 3);
}
```

### Testing Different DPI

```csharp
[Theory]
[InlineData(72)]
[InlineData(96)]
[InlineData(150)]
[InlineData(300)]
public async Task Rendering_AtVariousDPI_ShouldMatchBaseline(int dpi)
{
    var pdfPath = Path.Combine("tests", "Fixtures", "test.pdf");

    await AssertVisualMatchAsync(
        pdfPath,
        TestCategory,
        $"DPI_{dpi}",
        pageNumber: 1,
        dpi: dpi);
}
```

## Directory Structure

```
FluentPDF/
├── tests/
│   ├── Baselines/                    # VERSION CONTROLLED
│   │   ├── .gitkeep
│   │   ├── README.md                 # Baseline management guide
│   │   ├── CoreRendering/
│   │   │   ├── SimpleTextRendering/
│   │   │   │   └── page_1.png       # Baseline image
│   │   │   ├── ComplexLayoutRendering/
│   │   │   │   └── page_1.png
│   │   │   └── FontRendering/
│   │   │       └── page_1.png
│   │   └── Zoom/
│   │       ├── ZoomLevel_50/
│   │       │   └── page_1.png
│   │       └── ZoomLevel_200/
│   │           └── page_1.png
│   │
│   └── TestResults/                  # IGNORED BY GIT
│       ├── .gitignore                # Excludes all test results
│       ├── CoreRendering/
│       │   └── SimpleTextRendering/
│       │       └── 20260111_143022/  # Timestamped test run
│       │           ├── actual_page1.png
│       │           └── difference_page1.png
│       └── Zoom/
│           └── ZoomLevel_50/
│               └── 20260111_143115/
│                   ├── actual_page1.png
│                   └── difference_page1.png
```

### Baseline Path Pattern

Baselines follow this structure:

```
tests/Baselines/{Category}/{TestName}/page_{pageNumber}.png
```

Where:
- **Category**: Test category (sanitized, filesystem-safe)
- **TestName**: Test name (sanitized, filesystem-safe)
- **pageNumber**: 0-based page index (page 1 → `page_0.png`)

## CI Integration

### GitHub Actions Workflow

Visual regression tests run automatically in CI via `.github/workflows/visual-regression.yml`:

- Triggered on: push to main, pull requests
- Runs on: `windows-latest` (required for Win2D)
- Filters tests: `Category=VisualRegression`
- On failure: uploads baseline, actual, and diff images as artifacts

### Viewing CI Failures

When visual tests fail in CI:

1. Go to the GitHub Actions run
2. Download the `visual-test-results` artifact
3. Extract and review the images
4. If changes are intentional, update baselines locally and push

Example CI workflow excerpt:

```yaml
- name: Run Visual Regression Tests
  run: dotnet test --filter "Category=VisualRegression" --logger "trx;LogFileName=visual-tests.trx"

- name: Upload Test Results on Failure
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: visual-test-results
    path: tests/TestResults/
```

## Best Practices

### DO

- ✅ Review all baselines visually before committing
- ✅ Use descriptive test and category names
- ✅ Choose appropriate SSIM thresholds
- ✅ Commit baselines with clear commit messages
- ✅ Run visual tests before pushing changes
- ✅ Keep test PDFs small and focused
- ✅ Test edge cases (empty pages, complex graphics, various fonts)

### DON'T

- ❌ Commit auto-generated baselines without review
- ❌ Use overly permissive thresholds (<0.90)
- ❌ Test with large multi-page PDFs (split into separate tests)
- ❌ Ignore visual test failures without investigation
- ❌ Update baselines without understanding why they changed
- ❌ Commit test results (they're auto-ignored)

## Troubleshooting

### Test Fails with "CanvasDevice initialization failed"

**Cause**: Win2D requires Windows and GPU/display access.

**Solution**:
- Ensure running on Windows
- In CI, use `windows-latest` runner
- For headless servers, ensure virtual display or Windows graphics drivers

### Test Fails with "Baseline not found" but baseline exists

**Cause**: Path case sensitivity or sanitization issue.

**Solution**:
- Check that category/testName don't contain special characters
- Verify baseline path matches expected pattern
- Use `BaselineManager.GetBaselinePath()` to see expected path

### SSIM Score Fluctuates Between Runs

**Cause**: Anti-aliasing, font rendering, or compression differences.

**Solution**:
- Lower threshold slightly (0.93-0.95)
- Ensure consistent DPI settings
- Check for platform-specific rendering differences

### Baseline Directory Not Created

**Cause**: BaselineManager failed to create directory.

**Solution**:
- Check file system permissions
- Verify `tests/Baselines/` exists
- Check logs for detailed error messages

### Tests Pass Locally but Fail in CI

**Cause**: Environment differences (fonts, drivers, OS version).

**Solution**:
- Compare baseline vs actual images from CI artifacts
- Consider separate baseline sets for different environments
- Use stricter thresholds for platform-specific tests

## Performance Considerations

Typical performance (see `VisualTestPerformanceBenchmarks.cs`):

- **Rendering**: ~50-200ms per page (depends on complexity)
- **SSIM Comparison**: ~10-50ms per comparison
- **Total per test**: ~100-300ms

For large test suites:

- Use `[Trait]` to organize tests by speed
- Run fast tests in PR builds, comprehensive tests nightly
- Consider parallel test execution (xUnit default)

## Advanced Usage

### Custom Rendering Service

```csharp
public class CustomVisualTests : VisualRegressionTestBase
{
    public CustomVisualTests()
        : base(
            new CustomRenderingService(),  // Your implementation
            new VisualComparisonService(logger),
            new BaselineManager(logger))
    {
    }
}
```

### Custom Comparison Algorithm

Implement `IVisualComparisonService` with alternative algorithms:

- Mean Squared Error (MSE)
- Peak Signal-to-Noise Ratio (PSNR)
- Perceptual hashing (pHash)

### Programmatic Baseline Management

```csharp
var manager = new BaselineManager(logger);

// Check if baseline exists
bool exists = manager.BaselineExists("MyCategory", "MyTest", pageNumber: 0);

// Get baseline path
string path = manager.GetBaselinePath("MyCategory", "MyTest", pageNumber: 0);

// Create new baseline
var result = await manager.CreateBaselineAsync(
    sourcePath: "path/to/rendered.png",
    category: "MyCategory",
    testName: "MyTest",
    pageNumber: 0);

// Update existing baseline
var updateResult = await manager.UpdateBaselineAsync(
    sourcePath: "path/to/new-baseline.png",
    category: "MyCategory",
    testName: "MyTest",
    pageNumber: 0);
```

## Related Documentation

- [Testing Guide](TESTING.md) - General testing practices
- [Architecture](ARCHITECTURE.md) - System architecture overview
- [Baselines README](../tests/Baselines/README.md) - Baseline management quick reference

## References

- [Win2D Documentation](https://microsoft.github.io/Win2D/)
- [OpenCvSharp](https://github.com/shimat/opencvsharp)
- [SSIM: Image Quality Assessment](https://en.wikipedia.org/wiki/Structural_similarity)
- [xUnit Documentation](https://xunit.net/)
