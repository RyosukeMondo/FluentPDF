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

## See Also

- [CLI Automation Guide](CLI-AUTOMATION.md) - General CLI usage and automation
- [Testing Guide](TESTING.md) - Unit testing and integration testing
- [Architecture Documentation](ARCHITECTURE.md) - Application architecture overview
