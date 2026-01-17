# FluentPDF CLI Testing Framework

## Overview

FluentPDF includes a comprehensive CLI testing framework for automated verification of application features. The framework supports test discovery, isolated execution, result verification, and detailed reporting. All tests implement the `ICliTest` interface and can be executed individually or as a complete test suite.

## Architecture

The CLI testing framework consists of several key components:

```
ICliTest (interface)
  ├── RenderCliTest (example implementation)
  └── [Your custom tests]

TestDiscovery
  └── Discovers all ICliTest implementations via reflection

TestExecutor
  └── Executes tests in isolated contexts

TestRunner (orchestrator)
  ├── Uses TestDiscovery to find tests
  ├── Uses TestExecutor to run tests
  ├── Uses ResultVerifier to validate results
  └── Returns TestSuiteResult

CliTestContext (execution environment)
  ├── Temporary working directory
  ├── Service provider (DI)
  ├── Logger instance
  └── Test data dictionary
```

### Component Responsibilities

1. **ICliTest**: Contract that all CLI tests must implement
   - `Name`: Unique identifier for the test
   - `Description`: Human-readable explanation of what the test verifies
   - `RunAsync()`: Execute the test operation
   - `VerifyAsync()`: Verify the test result meets expectations

2. **CliTestContext**: Provides isolated execution environment
   - Creates unique temporary working directory per test
   - Provides access to application services via DI
   - Captures structured logs for debugging
   - Automatically cleans up resources on disposal

3. **TestExecutor**: Orchestrates test execution
   - Creates isolated context for each test
   - Handles exceptions and timeouts (30s default)
   - Captures execution timing
   - Returns structured CliTestResult

4. **TestDiscovery**: Finds all available tests
   - Uses reflection to discover ICliTest implementations
   - Caches discovery results for performance
   - Logs discovery process for troubleshooting

5. **TestRunner**: High-level orchestration
   - Discovers all tests (or finds specific test by name)
   - Executes tests with proper isolation
   - Aggregates results into TestSuiteResult
   - Provides summary statistics

6. **Verification Rules**: Reusable verification logic
   - `FileExistsRule`: Verifies expected files were created
   - `ExitCodeRule`: Checks process exit codes
   - `LogContainsRule`: Validates log output contains expected text

## Quick Start

### Running Tests from CLI

```powershell
# List all available tests
FluentPDF.App.exe --list-tests

# Run a specific test
FluentPDF.App.exe --run-test render-pdf --verbose

# Run all tests
FluentPDF.App.exe --run-all-tests --verbose
```

Exit codes:
- `0`: All tests passed
- `1`: One or more tests failed

### Creating Your First Test

1. Create a new class implementing `ICliTest`:

```csharp
using FluentPDF.App.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// Test that verifies bookmark extraction functionality.
/// </summary>
public sealed class BookmarkExtractionTest : ICliTest
{
    public string Name => "extract-bookmarks";

    public string Description => "Extracts bookmarks from a PDF and verifies output structure";

    public async Task<CliTestResult> RunAsync(CliTestContext context)
    {
        var result = new CliTestResult
        {
            TestName = Name,
            Success = false
        };

        var startTime = DateTime.UtcNow;

        try
        {
            context.Logger.Information("Starting bookmark extraction test");

            // Get required services from DI
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();

            // Load test PDF
            var testPdfPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "tests", "Fixtures", "with-bookmarks.pdf"
            );
            testPdfPath = Path.GetFullPath(testPdfPath);

            if (!File.Exists(testPdfPath))
            {
                result.ErrorMessage = $"Test PDF not found: {testPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;

            // Extract bookmarks (implement your logic here)
            var bookmarks = ExtractBookmarks(document);

            // Save results to output file
            var outputPath = Path.Combine(context.WorkingDirectory, "bookmarks.json");
            await File.WriteAllTextAsync(outputPath,
                System.Text.Json.JsonSerializer.Serialize(bookmarks, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // Store outputs for verification
            result.Outputs["OutputFiles"] = new List<string> { outputPath };
            result.Outputs["BookmarkCount"] = bookmarks.Count;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information("Bookmark extraction completed. Found {Count} bookmarks", bookmarks.Count);
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Bookmark extraction test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    public Task<bool> VerifyAsync(CliTestResult result)
    {
        // Check basic success
        if (!result.Success)
        {
            return Task.FromResult(false);
        }

        // Verify output file exists
        if (!result.Outputs.TryGetValue("OutputFiles", out var outputFilesObj) ||
            outputFilesObj is not List<string> outputFiles ||
            outputFiles.Count == 0)
        {
            return Task.FromResult(false);
        }

        // Verify file actually exists on disk
        if (!File.Exists(outputFiles[0]))
        {
            return Task.FromResult(false);
        }

        // Verify bookmark count is reasonable
        if (result.Outputs.TryGetValue("BookmarkCount", out var countObj) &&
            countObj is int count)
        {
            if (count <= 0)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    private List<Bookmark> ExtractBookmarks(IPdfDocument document)
    {
        // Implement your bookmark extraction logic
        return new List<Bookmark>();
    }
}
```

2. The test will be automatically discovered by the framework through reflection.

3. Run your test:
```powershell
FluentPDF.App.exe --run-test extract-bookmarks --verbose
```

## Best Practices

### Test Design

1. **Single Responsibility**: Each test should verify one specific feature or scenario
2. **Isolated Execution**: Never rely on test execution order or shared state
3. **Clear Naming**: Use descriptive test names that explain what is being verified
4. **Comprehensive Verification**: Verify both success conditions and expected outputs

### Using CliTestContext

The test context provides everything your test needs:

```csharp
public async Task<CliTestResult> RunAsync(CliTestContext context)
{
    // 1. Log important steps
    context.Logger.Information("Starting critical operation");

    // 2. Use the working directory for temporary files
    var outputFile = Path.Combine(context.WorkingDirectory, "output.txt");
    await File.WriteAllTextAsync(outputFile, "test data");

    // 3. Access services via DI
    var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
    var renderingService = context.Services.GetRequiredService<IPdfRenderingService>();

    // 4. Store test-specific data for verification
    context.Data["StartTime"] = DateTime.UtcNow;

    // 5. Working directory is automatically cleaned up when context is disposed
}
```

### Storing Test Outputs

Use the `Outputs` dictionary to store verifiable artifacts:

```csharp
result.Outputs["OutputFiles"] = new List<string> { outputPath1, outputPath2 };
result.Outputs["PageCount"] = document.PageCount;
result.Outputs["ExitCode"] = 0;
result.Outputs["Metrics"] = new { RenderTime = elapsed, FileSize = fileInfo.Length };
```

Common output keys:
- `OutputFiles`: List of file paths created by the test
- `ExitCode`: Simulated exit code (0 = success, non-zero = failure)
- `Metrics`: Performance or diagnostic metrics
- Custom keys: Any test-specific data you want to verify

### Verification Patterns

#### Simple Verification (in VerifyAsync)

```csharp
public Task<bool> VerifyAsync(CliTestResult result)
{
    // Check basic success
    if (!result.Success)
        return Task.FromResult(false);

    // Verify specific outputs
    if (!result.Outputs.TryGetValue("PageCount", out var countObj) ||
        countObj is not int count ||
        count != 3)
    {
        return Task.FromResult(false);
    }

    return Task.FromResult(true);
}
```

#### Using Verification Rules

```csharp
using FluentPDF.App.Testing.Verification;

// In your test runner or verification code:
var rules = new List<IVerificationRule>
{
    new FileExistsRule("page_1.png", "page_2.png", "page_3.png"),
    new ExitCodeRule(expectedExitCode: 0),
    new LogContainsRule("Rendering completed successfully")
};

foreach (var rule in rules)
{
    var passed = await rule.VerifyAsync(result);
    if (!passed)
    {
        context.Logger.Warning("Verification failed: {Message}", rule.GetFailureMessage());
        return false;
    }
}
```

### Error Handling

Always handle exceptions gracefully:

```csharp
public async Task<CliTestResult> RunAsync(CliTestContext context)
{
    var result = new CliTestResult { TestName = Name, Success = false };
    var startTime = DateTime.UtcNow;

    try
    {
        // Your test logic here

        result.Success = true;
    }
    catch (OperationCanceledException)
    {
        result.ErrorMessage = "Test was cancelled";
        context.Logger.Warning("Test execution cancelled");
    }
    catch (Exception ex)
    {
        context.Logger.Error(ex, "Test failed with exception");
        result.ErrorMessage = $"Exception during test execution: {ex.Message}";
    }
    finally
    {
        result.Duration = DateTime.UtcNow - startTime;
    }

    return result;
}
```

## Advanced Usage

### Custom Verification Rules

Create reusable verification logic by implementing `IVerificationRule`:

```csharp
using FluentPDF.App.Testing;

namespace FluentPDF.App.Testing.Verification;

/// <summary>
/// Verification rule that checks if rendered images have minimum dimensions.
/// </summary>
public sealed class MinimumImageSizeRule : IVerificationRule
{
    private readonly int _minWidth;
    private readonly int _minHeight;
    private string _failureMessage = string.Empty;

    public string Name => "MinimumImageSize";

    public MinimumImageSizeRule(int minWidth, int minHeight)
    {
        _minWidth = minWidth;
        _minHeight = minHeight;
    }

    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (!result.Outputs.TryGetValue("ImageSizes", out var sizesObj) ||
            sizesObj is not List<(int Width, int Height)> sizes)
        {
            _failureMessage = "ImageSizes not found in test outputs";
            return Task.FromResult(false);
        }

        foreach (var (width, height) in sizes)
        {
            if (width < _minWidth || height < _minHeight)
            {
                _failureMessage = $"Image size {width}x{height} is below minimum {_minWidth}x{_minHeight}";
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public string GetFailureMessage() => _failureMessage;
}
```

### Test Fixtures

Place test PDFs in `tests/Fixtures/` directory:

```
tests/
  Fixtures/
    multi-page.pdf          # PDF with multiple pages for rendering tests
    with-bookmarks.pdf      # PDF containing bookmarks
    searchable-text.pdf     # PDF with searchable text
    password-protected.pdf  # PDF requiring password
    corrupted.pdf          # Intentionally malformed PDF for error handling tests
```

Reference fixtures in your tests:

```csharp
var testPdfPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "..", "..", "..", "..", "..",
    "tests", "Fixtures", "multi-page.pdf"
);
testPdfPath = Path.GetFullPath(testPdfPath);
```

### Debugging Tests

Enable verbose logging to see detailed test execution:

```powershell
FluentPDF.App.exe --run-test render-pdf --verbose --console
```

The logs will show:
- Test discovery process
- Test context creation
- Service resolution
- Test execution steps
- Verification results
- Cleanup operations

### CI/CD Integration

#### GitHub Actions Example

```yaml
name: CLI Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build
        run: dotnet build -c Release

      - name: Run CLI Tests
        run: |
          $exe = ".\src\FluentPDF.App\bin\x64\Release\net8.0-windows10.0.19041.0\FluentPDF.App.exe"
          & $exe --run-all-tests --verbose --console

          if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed with exit code $LASTEXITCODE"
            exit 1
          }
```

#### Azure DevOps Example

```yaml
- task: PowerShell@2
  displayName: 'Run CLI Tests'
  inputs:
    targetType: 'inline'
    script: |
      $exe = "$(Build.SourcesDirectory)\src\FluentPDF.App\bin\x64\Release\net8.0-windows10.0.19041.0\FluentPDF.App.exe"
      & $exe --run-all-tests --verbose --console

      if ($LASTEXITCODE -ne 0) {
        Write-Error "##vso[task.logissue type=error]Tests failed"
        exit 1
      }
```

## Troubleshooting

### Test Not Discovered

**Problem**: Your test doesn't appear when running `--list-tests`

**Solutions**:
1. Ensure your class is `public` and implements `ICliTest`
2. Verify the assembly containing your test is loaded
3. Check that the class is not `abstract`
4. Run with `--verbose` to see discovery logs

### Test Times Out

**Problem**: Test execution exceeds 30-second timeout

**Solutions**:
1. Optimize test execution (use smaller test PDFs)
2. Break complex tests into smaller, focused tests
3. Check for deadlocks or infinite loops in test code
4. Use `--verbose` to identify which step is hanging

### Working Directory Cleanup Fails

**Problem**: Warning logs about failed cleanup

**Solutions**:
1. Ensure test closes all file handles before completion
2. Check that streams are properly disposed
3. Verify no background threads are still accessing files
4. Consider using `using` statements for automatic disposal

### Service Not Found

**Problem**: `GetRequiredService` throws exception

**Solutions**:
1. Verify the service is registered in DI container
2. Check service lifetime (singleton vs. scoped)
3. Ensure dependencies are properly configured
4. Review service registration in `App.xaml.cs`

### Test Passes Locally but Fails in CI

**Problem**: Test results differ between environments

**Solutions**:
1. Avoid hardcoded paths; use relative paths
2. Don't depend on specific screen resolutions or DPI
3. Ensure test fixtures are committed to repository
4. Check for timezone or culture-specific assumptions
5. Verify all dependencies are available in CI environment

## Reference Implementation

See `RenderCliTest` in `src/FluentPDF.App/Testing/Tests/RenderCliTest.cs` for a complete, production-quality test implementation. This test demonstrates:

- Proper use of CliTestContext
- Service resolution via DI
- Comprehensive error handling
- Output file generation and verification
- Structured logging throughout execution
- Clean resource disposal

## API Reference

### ICliTest Interface

```csharp
public interface ICliTest
{
    string Name { get; }
    string Description { get; }
    Task<CliTestResult> RunAsync(CliTestContext context);
    Task<bool> VerifyAsync(CliTestResult result);
}
```

### CliTestResult Class

```csharp
public sealed class CliTestResult
{
    public required string TestName { get; set; }
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Outputs { get; set; }
    public List<string> LogEntries { get; set; }
}
```

### TestSuiteResult Class

```csharp
public sealed class TestSuiteResult
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<CliTestResult> Results { get; set; }
    public bool AllTestsPassed { get; }
}
```

### CliTestContext Class

```csharp
public sealed class CliTestContext : IDisposable
{
    public required string WorkingDirectory { get; init; }
    public required IServiceProvider Services { get; init; }
    public required ILogger Logger { get; init; }
    public Dictionary<string, object> Data { get; init; }
    public void Dispose();
}
```

## Rendering Verification Tests

FluentPDF includes a suite of specialized CLI tests for verifying PDF rendering functionality. These tests cover single-page rendering, thumbnail generation, text extraction, form field rendering, and batch operations. All rendering tests are designed to work in CI/CD environments and return appropriate exit codes for automation.

### Available Rendering Tests

#### 1. PageRenderCliTest (`page-render`)

Verifies single page rendering with dimension and file size checks.

**What it tests:**
- Single page rendering to PNG format
- Image dimension verification (matches PDF page size)
- File size validation (>1KB requirement)
- Render time metrics

**Usage:**
```powershell
# Run page render test
FluentPDF.App.exe --run-test page-render --verbose

# With custom PDF and page number
FluentPDF.App.exe --run-test page-render --test-pdf "path/to/file.pdf" --page 1
```

**Output verification:**
- `OutputFile`: Path to rendered PNG file
- `ImageWidth`: Width of rendered image in pixels
- `ImageHeight`: Height of rendered image in pixels
- `FileSizeKB`: File size in kilobytes
- `RenderTimeMs`: Rendering time in milliseconds
- `PageNumber`: Page number that was rendered
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Page render test completed successfully. Page 1, 800x600, 45.23 KB, 123 ms
```

#### 2. ThumbnailAllPagesCliTest (`thumbnail-all-pages`)

Generates thumbnails for all pages in a PDF document.

**What it tests:**
- Thumbnail generation for every page
- Thumbnail count matches page count
- All thumbnails have consistent dimensions
- Performance metrics for batch thumbnail generation

**Usage:**
```powershell
# Generate thumbnails for all pages
FluentPDF.App.exe --run-test thumbnail-all-pages --verbose

# With custom PDF
FluentPDF.App.exe --run-test thumbnail-all-pages --test-pdf "path/to/file.pdf"
```

**Output verification:**
- `PageCount`: Total number of pages
- `SuccessCount`: Number of successfully generated thumbnails
- `FailCount`: Number of failed thumbnail generations
- `ThumbnailPaths`: List of all generated thumbnail file paths
- `TotalTimeMs`: Total time to generate all thumbnails
- `AverageTimeMs`: Average time per thumbnail
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Thumbnail generation complete: 25/25 successful, 0 failed
Total time: 2.5s, Average: 100ms per page
```

#### 3. TextExtractionCliTest (`text-extraction`)

Extracts text content from PDF pages and saves to a text file.

**What it tests:**
- Text extraction from PDF document
- Character count validation (minimum 10 characters)
- Encoding correctness
- Text saved to output file for manual inspection

**Usage:**
```powershell
# Extract text from PDF
FluentPDF.App.exe --run-test text-extraction --verbose

# With custom PDF
FluentPDF.App.exe --run-test text-extraction --test-pdf "path/to/file.pdf"
```

**Output verification:**
- `OutputFile`: Path to extracted text file
- `CharacterCount`: Total number of characters extracted
- `PageCount`: Number of pages processed
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Text extraction completed. 1,523 characters extracted from 3 pages
Output saved to: extracted_text.txt
```

#### 4. FormFieldRenderCliTest (`form-field-render`)

Detects and renders PDF forms with form field visualization.

**What it tests:**
- Form field detection (HasForms check)
- Form field counting and type identification
- Rendering of PDFs with interactive forms
- Graceful handling of non-form PDFs

**Usage:**
```powershell
# Test form field rendering
FluentPDF.App.exe --run-test form-field-render --verbose

# With form PDF
FluentPDF.App.exe --run-test form-field-render --test-pdf "path/to/form.pdf"
```

**Output verification:**
- `HasForms`: Boolean indicating if PDF contains forms
- `FormFieldCount`: Number of form fields detected
- `FormFieldTypes`: Dictionary of field types and counts
- `OutputFile`: Path to rendered PDF with forms (if HasForms is true)
- `ExitCode`: 0 for success, non-zero for failure

**Example output with forms:**
```
Form PDF detected: 5 form fields
Field types: TextBox=3, CheckBox=1, RadioButton=1
Rendered with forms to: form_rendered.png
```

**Example output without forms:**
```
Non-form PDF: 0 form fields detected
Skipped form rendering
```

#### 5. BatchRenderCliTest (`batch-render`)

Renders all pages in a PDF document to separate image files with performance reporting.

**What it tests:**
- Sequential rendering of all pages
- Progress reporting during batch operation
- Performance metrics (total time, average time, min/max)
- Partial failure handling (continues after failed pages)
- Success rate calculation

**Usage:**
```powershell
# Batch render all pages
FluentPDF.App.exe --run-test batch-render --verbose

# With custom PDF and DPI
FluentPDF.App.exe --run-test batch-render --test-pdf "path/to/file.pdf" --dpi 150
```

**Output verification:**
- `PageCount`: Total number of pages
- `SuccessCount`: Number of successfully rendered pages
- `FailCount`: Number of failed renders
- `FailedPages`: List of page numbers that failed
- `SuccessRate`: Percentage of successful renders (0-100)
- `TotalBytes`: Total size of all rendered files
- `TotalMB`: Total size in megabytes
- `TotalTimeMs`: Total rendering time
- `TotalSeconds`: Total time in seconds
- `AverageTimeMs`: Average time per page
- `MinTimeMs`: Fastest page render time
- `MaxTimeMs`: Slowest page render time
- `RenderedPaths`: List of all rendered file paths
- `ExitCode`: 0 for full success, 1 for partial failure

**Example output:**
```
Batch rendering complete: 100/100 successful, 0 failed
Success rate: 100.0%
Total size: 45.2 MB
Total time: 12.3s, Average: 123ms per page
Min: 87ms, Max: 245ms
```

### Running All Rendering Tests

Execute all rendering tests in sequence:

```powershell
FluentPDF.App.exe --run-all-tests --verbose
```

This will run all tests including the rendering verification tests and provide a summary report:

```
Test Suite Results
==================
Total Tests: 5
Passed: 5
Failed: 0
Duration: 15.2 seconds

All tests passed ✓
```

### Exit Codes

All rendering tests follow these exit code conventions:

- `0`: Test completed successfully, all verifications passed
- `1`: Test failed (e.g., partial batch render failure, missing PDF)
- `2`: Render operation failed
- `3`: Verification failed

Exit codes are stored in the test result `Outputs["ExitCode"]` and can be used in automation scripts:

```powershell
FluentPDF.App.exe --run-test page-render --verbose
if ($LASTEXITCODE -ne 0) {
    Write-Error "Page render test failed with exit code $LASTEXITCODE"
    exit 1
}
```

### Test Fixtures for Rendering Tests

The rendering tests use the following test PDFs from `tests/Fixtures/`:

- `multi-page.pdf`: Multi-page document for batch and thumbnail tests
- `sample-with-text.pdf`: PDF with searchable text content
- `sample-form.pdf`: Interactive PDF form for form field tests
- `complex-layout.pdf`: Complex layout for stress testing

### Troubleshooting Rendering Tests

#### Page Render Failures

**Problem**: PageRenderCliTest fails with "Failed to render page"

**Solutions:**
1. Verify PDF file is not corrupted
2. Check that page number is valid (1-indexed)
3. Ensure sufficient memory for rendering
4. Review PDFium initialization logs with `--verbose`

#### Thumbnail Generation Slow

**Problem**: ThumbnailAllPagesCliTest takes too long

**Solutions:**
1. Use a smaller test PDF during development
2. Thumbnail generation is intentionally sequential for reliability
3. Performance metrics help identify bottlenecks
4. Consider adjusting thumbnail size if needed

#### Text Extraction Returns Empty

**Problem**: TextExtractionCliTest extracts 0 characters

**Solutions:**
1. Verify PDF contains actual text (not scanned images)
2. Check PDF isn't password protected
3. Some PDFs have text in non-standard encodings
4. Review extracted text file for invisible characters

#### Form Fields Not Detected

**Problem**: FormFieldRenderCliTest reports HasForms=false for form PDF

**Solutions:**
1. Verify PDF actually contains interactive form fields
2. Some "forms" are just visual (not interactive AcroForms)
3. Check PDF version compatibility
4. Review with Adobe Acrobat to confirm form fields exist

#### Batch Render Partial Failures

**Problem**: BatchRenderCliTest shows some pages failed

**Solutions:**
1. Check `FailedPages` output to identify specific pages
2. Review logs for per-page error messages
3. Some pages may have rendering issues (corrupted content)
4. Test continues after failures as designed (requirement 5.4)
5. ExitCode will be 1 for partial failure, test still returns Success=true if at least one page rendered

### Example: Automated Rendering Verification

Complete PowerShell script for CI/CD rendering verification:

```powershell
# Test all rendering features
$tests = @(
    "page-render",
    "thumbnail-all-pages",
    "text-extraction",
    "form-field-render",
    "batch-render"
)

$failed = @()
$passed = @()

foreach ($test in $tests) {
    Write-Host "Running $test..." -ForegroundColor Cyan

    FluentPDF.App.exe --run-test $test --verbose

    if ($LASTEXITCODE -eq 0) {
        $passed += $test
        Write-Host "✓ $test PASSED" -ForegroundColor Green
    } else {
        $failed += $test
        Write-Host "✗ $test FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
}

Write-Host "`nSummary:" -ForegroundColor Yellow
Write-Host "Passed: $($passed.Count)/$($tests.Count)"
Write-Host "Failed: $($failed.Count)/$($tests.Count)"

if ($failed.Count -gt 0) {
    Write-Host "`nFailed tests:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $_" }
    exit 1
}

Write-Host "`nAll rendering tests passed!" -ForegroundColor Green
exit 0
```

## Document Operations Tests

FluentPDF includes specialized CLI tests for verifying document operations such as bookmark extraction, text search, page manipulation (rotate, delete, reorder), annotation detection, and metadata extraction. These tests ensure reliable document processing functionality and are designed for automated verification in CI/CD pipelines.

### Available Document Operations Tests

#### 1. BookmarksCliTest (`bookmarks`)

Extracts and verifies bookmark structure from PDF documents.

**What it tests:**
- Bookmark extraction with hierarchy preservation
- Page destination verification (valid page numbers)
- Root and total bookmark counting
- JSON output generation for bookmark structure
- Handling of PDFs without bookmarks

**Usage:**
```powershell
# Extract bookmarks from PDF
FluentPDF.App.exe --run-test bookmarks --verbose

# With custom PDF
FluentPDF.App.exe --run-test bookmarks --test-pdf "path/to/file.pdf"
```

**Output verification:**
- `OutputFile`: Path to JSON file containing bookmark tree
- `RootBookmarkCount`: Number of root-level bookmarks
- `TotalBookmarkCount`: Total bookmarks including all levels
- `BookmarksWithDestinations`: Count of bookmarks with page destinations
- `InvalidPageNumbers`: Count of bookmarks with invalid page references
- `PageCount`: Total pages in document
- `ExtractionTimeMs`: Extraction time in milliseconds
- `ExitCode`: 0 for success, 1 if invalid page numbers detected

**Example output:**
```
Bookmark extraction completed successfully. 12 bookmarks, 10 with destinations, 45 ms
Output: bookmarks.json
```

**JSON output structure:**
```json
[
  {
    "title": "Chapter 1",
    "pageNumber": 1,
    "hasDestination": true,
    "isValidPage": true,
    "x": 72.0,
    "y": 720.0,
    "childCount": 2,
    "children": [
      {
        "title": "Section 1.1",
        "pageNumber": 2,
        "hasDestination": true,
        "isValidPage": true,
        "childCount": 0
      }
    ]
  }
]
```

#### 2. SearchCliTest (`search`)

Searches for text in PDF documents with match reporting and position tracking.

**What it tests:**
- Text search across all pages
- Match counting and page distribution
- Bounding box position verification
- Case-sensitive and whole-word search options
- Graceful handling of zero results

**Usage:**
```powershell
# Search for text in PDF
FluentPDF.App.exe --run-test search --verbose

# With custom search term and options
FluentPDF.App.exe --run-test search --search-term "important" --case-sensitive --whole-word
```

**Output verification:**
- `OutputFile`: Path to text report of search results
- `JsonOutputFile`: Path to JSON file with detailed match data
- `SearchTerm`: The search term used
- `TotalMatches`: Total number of matches found
- `PagesWithMatches`: Number of pages containing matches
- `PageCount`: Total pages in document
- `SearchTimeMs`: Search time in milliseconds
- `MatchesByPage`: Dictionary mapping page numbers to match counts
- `ExitCode`: 0 for success (even with zero results)

**Example output:**
```
Search completed successfully. 47 matches for 'example' found, 125 ms
Pages with matches: 8/25
Output: search_results.txt, search_results.json
```

**JSON output structure:**
```json
{
  "searchTerm": "example",
  "caseSensitive": false,
  "wholeWord": false,
  "totalMatches": 47,
  "pagesWithMatches": 8,
  "pageCount": 25,
  "matchesByPage": [
    { "pageNumber": 1, "matchCount": 5 },
    { "pageNumber": 3, "matchCount": 12 }
  ],
  "matches": [
    {
      "pageNumber": 1,
      "charIndex": 234,
      "length": 7,
      "text": "example",
      "boundingBox": {
        "left": 72.0,
        "top": 650.0,
        "right": 120.0,
        "bottom": 665.0
      }
    }
  ],
  "searchTimeMs": 125.3
}
```

#### 3. PageRotateCliTest (`page-rotate`)

Rotates specified pages and verifies the operation.

**What it tests:**
- Single and multiple page rotation
- Rotation angle validation (90, 180, 270 degrees)
- Page index validation
- Output PDF generation
- Operation time metrics

**Usage:**
```powershell
# Rotate page 1 by 90 degrees
FluentPDF.App.exe --run-test page-rotate --verbose

# Rotate multiple pages
FluentPDF.App.exe --run-test page-rotate --pages "0,2,4" --angle 90
```

**Output verification:**
- `OutputFile`: Path to rotated PDF
- `ReportFile`: Path to rotation report
- `PageIndices`: Array of rotated page indices (0-based)
- `RotationAngle`: Rotation angle in degrees
- `PageCount`: Total pages in document
- `OperationTimeMs`: Operation time in milliseconds
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Page rotation completed successfully. Rotated 3 pages, 85 ms
Output: rotated_output.pdf
```

#### 4. PageDeleteCliTest (`page-delete`)

Deletes specified pages and verifies page count changes.

**What it tests:**
- Single and multiple page deletion
- Page count verification (before/after)
- Prevention of deleting all pages
- Page index validation
- Output PDF generation

**Usage:**
```powershell
# Delete page 2
FluentPDF.App.exe --run-test page-delete --verbose

# Delete multiple pages
FluentPDF.App.exe --run-test page-delete --pages "1,3,5"
```

**Output verification:**
- `OutputFile`: Path to modified PDF
- `ReportFile`: Path to deletion report
- `PageIndices`: Array of deleted page indices
- `OriginalPageCount`: Page count before deletion
- `NewPageCount`: Page count after deletion
- `PagesDeleted`: Number of pages removed
- `OperationTimeMs`: Operation time in milliseconds
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Page deletion completed successfully. Deleted 2 pages (10 -> 8), 95 ms
Output: deleted_output.pdf
```

#### 5. PageReorderCliTest (`page-reorder`)

Reorders pages by moving them to a new position.

**What it tests:**
- Page movement to specified target position
- Page count preservation (no pages added/removed)
- Index validation (source and target)
- Output PDF generation
- Operation time metrics

**Usage:**
```powershell
# Move page 3 to position 1
FluentPDF.App.exe --run-test page-reorder --verbose

# Move multiple pages to beginning
FluentPDF.App.exe --run-test page-reorder --pages "2,3,4" --target 0
```

**Output verification:**
- `OutputFile`: Path to reordered PDF
- `ReportFile`: Path to reorder report
- `PageIndices`: Array of moved page indices
- `TargetIndex`: Destination position
- `PageCount`: Total pages (unchanged)
- `OperationTimeMs`: Operation time in milliseconds
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Page reordering completed successfully. Moved 1 page to position 1, 78 ms
Output: reordered_output.pdf
```

#### 6. AnnotationsCliTest (`annotations`)

Detects and reports annotations in PDF documents.

**What it tests:**
- Annotation detection across all pages
- Annotation type identification (highlight, text, etc.)
- Bounding box position verification
- Metadata extraction (author, dates, opacity)
- Graceful handling of PDFs without annotations

**Usage:**
```powershell
# Detect annotations in PDF
FluentPDF.App.exe --run-test annotations --verbose

# With custom PDF
FluentPDF.App.exe --run-test annotations --test-pdf "path/to/annotated.pdf"
```

**Output verification:**
- `ReportFile`: Path to text report of annotations
- `JsonOutputFile`: Path to JSON file with annotation details
- `TotalAnnotations`: Total number of annotations found
- `PagesWithAnnotations`: Number of pages containing annotations
- `PageCount`: Total pages in document
- `AnnotationsByType`: Dictionary mapping types to counts
- `DetectionTimeMs`: Detection time in milliseconds
- `ExitCode`: 0 for success (even with zero annotations)

**Example output:**
```
Annotation detection completed successfully. 23 annotations found, 156 ms
Types: Highlight=15, Text=5, Underline=3
Output: annotations_report.txt, annotations.json
```

**JSON output structure:**
```json
{
  "totalAnnotations": 23,
  "pagesWithAnnotations": 8,
  "pageCount": 25,
  "annotationsByType": {
    "Highlight": 15,
    "Text": 5,
    "Underline": 3
  },
  "annotationsByPage": [
    {
      "pageNumber": 1,
      "annotationCount": 3,
      "annotations": [
        {
          "id": "annot-1",
          "type": "Highlight",
          "bounds": {
            "left": 72.0,
            "top": 650.0,
            "right": 200.0,
            "bottom": 665.0
          },
          "contents": "Important section",
          "author": "John Doe",
          "createdDate": "2024-01-15T10:30:00Z",
          "opacity": 0.5
        }
      ]
    }
  ],
  "detectionTimeMs": 156.2
}
```

#### 7. MetadataCliTest (`metadata`)

Extracts document metadata and file properties.

**What it tests:**
- File information (name, size, path, dates)
- Document properties (page count, loaded time)
- PDF metadata fields (title, author, subject, etc.)
- Field availability reporting
- Graceful handling of missing metadata

**Usage:**
```powershell
# Extract metadata from PDF
FluentPDF.App.exe --run-test metadata --verbose

# With custom PDF
FluentPDF.App.exe --run-test metadata --test-pdf "path/to/file.pdf"
```

**Output verification:**
- `ReportFile`: Path to text report of metadata
- `JsonOutputFile`: Path to JSON file with metadata
- `Metadata`: Dictionary of all metadata fields
- `SetFields`: Count of fields with values
- `TotalFields`: Total metadata fields checked
- `NotImplementedFields`: Count of fields not yet available
- `ExtractionTimeMs`: Extraction time in milliseconds
- `ExitCode`: 0 for success, non-zero for failure

**Example output:**
```
Metadata extraction completed successfully. 8/16 fields available, 45 ms
Output: metadata_report.txt, metadata.json
Note: 8 metadata fields not yet implemented (require FPDF_GetMetaText support)
```

**JSON output structure:**
```json
{
  "fileInfo": {
    "fileName": "document.pdf",
    "filePath": "C:\\path\\to\\document.pdf",
    "fileSize": "2.5 MB",
    "fileSizeBytes": 2621440,
    "fileCreatedDate": "2024-01-10T08:30:00Z",
    "fileModifiedDate": "2024-01-15T14:22:00Z"
  },
  "documentProperties": {
    "pageCount": 25,
    "loadedAt": "2024-01-17T12:00:00Z"
  },
  "pdfMetadata": {
    "title": "(not implemented)",
    "author": "(not implemented)",
    "subject": "(not implemented)",
    "keywords": "(not implemented)",
    "creator": "(not implemented)",
    "producer": "(not implemented)",
    "creationDate": "(not implemented)",
    "modificationDate": "(not implemented)"
  },
  "extractionTimeMs": 45.2,
  "note": "PDF metadata fields (Title, Author, etc.) require FPDF_GetMetaText implementation"
}
```

**Note:** Some PDF metadata fields are not yet implemented and require FPDF_GetMetaText API support in the rendering layer. File system metadata and document properties are fully available.

### Running All Document Operations Tests

Execute all document operations tests:

```powershell
# Run all tests
FluentPDF.App.exe --run-all-tests --verbose
```

Or run only document operations tests using a script:

```powershell
$docOpTests = @(
    "bookmarks",
    "search",
    "page-rotate",
    "page-delete",
    "page-reorder",
    "annotations",
    "metadata"
)

foreach ($test in $docOpTests) {
    Write-Host "Running $test..." -ForegroundColor Cyan
    FluentPDF.App.exe --run-test $test --verbose

    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ $test FAILED" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ $test PASSED" -ForegroundColor Green
}
```

### Exit Codes

Document operations tests follow these exit code conventions:

- `0`: Test completed successfully, all verifications passed
- `1`: Test failed or partial failure (e.g., invalid bookmark page numbers)
- Non-zero: Operation or verification failed

### Test Fixtures for Document Operations Tests

Document operations tests use the following test PDFs from `tests/Fixtures/`:

- `multi-page.pdf`: Multi-page document for page operations
- `with-bookmarks.pdf`: PDF containing bookmark hierarchy
- `searchable-text.pdf`: PDF with searchable text content
- `annotated.pdf`: PDF with various annotation types
- `with-metadata.pdf`: PDF with complete metadata fields

### Troubleshooting Document Operations Tests

#### Bookmark Extraction Returns Empty

**Problem**: BookmarksCliTest reports 0 bookmarks for PDF with bookmarks

**Solutions:**
1. Verify PDF actually contains bookmarks (open in Adobe Reader)
2. Check PDF is not password protected
3. Some PDFs have outline-style bookmarks vs. traditional bookmarks
4. Review extracted JSON file for structure

#### Search Finds Unexpected Results

**Problem**: SearchCliTest returns too many or too few matches

**Solutions:**
1. Verify case-sensitive and whole-word options match expectations
2. Check if PDF text is searchable (not scanned images)
3. Review JSON output to see actual match positions
4. Some PDFs have hidden or metadata text that matches

#### Page Operations Invalid Index

**Problem**: Page rotate/delete/reorder fails with "Invalid page indices"

**Solutions:**
1. Remember page indices are 0-based (first page is 0)
2. Verify page indices are within valid range (0 to PageCount-1)
3. Check that page numbers haven't changed after previous operations
4. Review error message for specific invalid indices

#### Annotation Detection Misses Annotations

**Problem**: AnnotationsCliTest reports fewer annotations than expected

**Solutions:**
1. Not all PDF "markup" is stored as annotations
2. Some visual markup is part of page content, not annotations
3. Check PDF with Adobe Acrobat to confirm annotation types
4. Review annotation types in JSON output

#### Metadata Fields Not Implemented

**Problem**: MetadataCliTest shows "(not implemented)" for many fields

**Solutions:**
1. This is expected - PDF metadata extraction requires FPDF_GetMetaText
2. File system metadata (size, dates, path) is fully available
3. Document properties (page count) are available
4. Future enhancement will add full PDF metadata support
5. Use file info and document properties for now

### Example: Complete Document Operations Verification

Comprehensive PowerShell script for CI/CD document operations testing:

```powershell
# Test all document operations
$tests = @(
    @{ Name = "bookmarks"; Pdf = "with-bookmarks.pdf"; ExpectedOutput = "OutputFile" },
    @{ Name = "search"; Pdf = "searchable-text.pdf"; ExpectedOutput = "JsonOutputFile" },
    @{ Name = "page-rotate"; Pdf = "multi-page.pdf"; ExpectedOutput = "OutputFile" },
    @{ Name = "page-delete"; Pdf = "multi-page.pdf"; ExpectedOutput = "OutputFile" },
    @{ Name = "page-reorder"; Pdf = "multi-page.pdf"; ExpectedOutput = "OutputFile" },
    @{ Name = "annotations"; Pdf = "annotated.pdf"; ExpectedOutput = "JsonOutputFile" },
    @{ Name = "metadata"; Pdf = "multi-page.pdf"; ExpectedOutput = "JsonOutputFile" }
)

$results = @{
    Passed = @()
    Failed = @()
    Skipped = @()
}

foreach ($test in $tests) {
    $testName = $test.Name
    $testPdf = "tests/Fixtures/$($test.Pdf)"

    Write-Host "`nRunning $testName..." -ForegroundColor Cyan

    # Check if test PDF exists
    if (-not (Test-Path $testPdf)) {
        Write-Host "⚠ Test PDF not found: $testPdf - SKIPPED" -ForegroundColor Yellow
        $results.Skipped += $testName
        continue
    }

    # Run test
    FluentPDF.App.exe --run-test $testName --test-pdf $testPdf --verbose

    if ($LASTEXITCODE -eq 0) {
        $results.Passed += $testName
        Write-Host "✓ $testName PASSED" -ForegroundColor Green
    } else {
        $results.Failed += $testName
        Write-Host "✗ $testName FAILED (exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
}

# Print summary
Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "Document Operations Test Summary" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Passed:  $($results.Passed.Count)/$($tests.Count)" -ForegroundColor Green
Write-Host "Failed:  $($results.Failed.Count)/$($tests.Count)" -ForegroundColor $(if ($results.Failed.Count -gt 0) { "Red" } else { "Gray" })
Write-Host "Skipped: $($results.Skipped.Count)/$($tests.Count)" -ForegroundColor $(if ($results.Skipped.Count -gt 0) { "Yellow" } else { "Gray" })

if ($results.Failed.Count -gt 0) {
    Write-Host "`nFailed tests:" -ForegroundColor Red
    $results.Failed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

if ($results.Skipped.Count -gt 0) {
    Write-Host "`nSkipped tests:" -ForegroundColor Yellow
    $results.Skipped | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

Write-Host "`n✓ All document operations tests passed!" -ForegroundColor Green
exit 0
```

## See Also

- [CLI Automation Guide](CLI-AUTOMATION.md) - General CLI usage and automation
- [Testing Guide](TESTING.md) - Unit testing and integration testing
- [Architecture Documentation](ARCHITECTURE.md) - Application architecture overview
