# PDF Rendering Reliability Guide

## Overview

FluentPDF implements a comprehensive rendering reliability system to handle WinUI rendering bugs, memory leaks, and UI binding failures. This guide explains the architecture, troubleshooting steps, and diagnostic tools.

## Architecture

### 1. Strategy Pattern for Rendering

FluentPDF uses multiple rendering strategies with automatic fallback:

```
PdfRenderingService (PDFium) → PNG Stream
                                    ↓
                        RenderingCoordinator
                                    ↓
                    ┌───────────────┴───────────────┐
                    ↓                               ↓
        WriteableBitmapStrategy          FileBasedStrategy
        (Primary - Priority 0)           (Fallback - Priority 10)
                    ↓                               ↓
              WinUI ImageSource                WinUI ImageSource
```

**WriteableBitmapRenderingStrategy** (Primary)
- Uses ImageSharp to decode PNG
- Creates WriteableBitmap with pixel buffer manipulation
- Fast in-memory approach
- Priority: 0 (tried first)

**FileBasedRenderingStrategy** (Fallback)
- Saves PNG to temp file
- Loads via BitmapImage with file URI
- Avoids WinUI InMemoryRandomAccessStream bug
- Priority: 10 (fallback)
- Automatic temp file cleanup

### 2. Observability Pipeline

**RenderingObservabilityService** provides structured logging with:
- Operation timing (via IDisposable scope)
- Memory metrics before/after rendering
- Fallback strategy usage tracking
- UI binding failure detection

**MemoryMonitor** captures:
- Working set and private memory
- Managed heap size (GC.GetTotalMemory)
- Handle count (Process.HandleCount)
- Abnormal growth detection (>100MB or >1000 handles)

### 3. UI Binding Verification

**UIBindingVerifier** detects when rendered images don't appear in UI:
- Monitors PropertyChanged events with timeout
- Checks Image.Source property on UI thread
- Triggers forced refresh on verification failure

## Common Issues and Solutions

### Issue 1: Blank Pages Despite Successful Rendering

**Symptoms:**
- PDF loads successfully
- Logs show render succeeded
- Page appears blank in UI

**Root Cause:**
WinUI InMemoryRandomAccessStream bug causes binding failures.

**Solution:**
Rendering coordinator automatically falls back to FileBasedStrategy. Check logs:
```
[Warning] Primary rendering strategy 'WriteableBitmap + ImageSharp' failed,
         fell back to 'FileBased'.
```

**Manual Fix:**
If automatic fallback doesn't work, force file-based strategy:
```csharp
// In RenderingCoordinator, skip WriteableBitmap strategy
// (Requires code modification - contact developers)
```

### Issue 2: Memory Leaks During PDF Navigation

**Symptoms:**
- Memory grows continuously when navigating pages
- Application slows down over time
- High handle count

**Diagnostic Steps:**

1. Enable verbose logging:
   ```bash
   FluentPDF.App.exe --verbose "file.pdf"
   ```

2. Check logs for abnormal memory growth:
   ```
   [Warning] Abnormal memory growth detected during rendering.
             WorkingSet: 150.5MB, Handles: 1200
   ```

3. Run memory diagnostics:
   ```bash
   FluentPDF.App.exe --diagnostics
   ```

**Solutions:**

- **SafeHandle leaks**: PDFium document/page handles not disposed
  - Check all `using` statements in rendering code
  - Verify SafeHandles are disposed in finally blocks

- **Cached images not released**: ViewModel holding image references
  - Clear `CurrentPageImage` before setting new image
  - Dispose `DisposableBitmapImage` properly

- **Event handler leaks**: PropertyChanged subscribers not unsubscribed
  - Use weak event patterns
  - Unsubscribe in Dispose/Cleanup methods

### Issue 3: UI Freezes During Rendering

**Symptoms:**
- UI becomes unresponsive when opening PDFs
- Loading indicator doesn't animate
- Application appears hung

**Root Cause:**
Rendering on UI thread or blocking UI thread with synchronous calls.

**Solutions:**

1. Verify async/await usage:
   ```csharp
   // CORRECT: Async all the way
   await RenderingCoordinator.RenderWithFallbackAsync(pageNum, context);

   // WRONG: Blocking call on UI thread
   RenderingCoordinator.RenderWithFallbackAsync(...).Wait();
   ```

2. Check for synchronous file I/O:
   ```csharp
   // WRONG: Synchronous file access
   var bytes = File.ReadAllBytes(path);

   // CORRECT: Async file access
   var bytes = await File.ReadAllBytesAsync(path);
   ```

3. Profile with dotnet-trace:
   ```bash
   dotnet-trace collect --name FluentPDF.App --providers Microsoft-Windows-DotNETRuntime
   ```

### Issue 4: Test Render Failures in CI/CD

**Symptoms:**
- `--test-render` returns non-zero exit code
- Rendering works locally but fails in CI

**Exit Codes:**
- 0: Success
- 1: Document load failed
- 2: Rendering failed
- 3: UI binding failed

**Diagnostic Steps:**

1. Run test-render with verbose logging:
   ```bash
   FluentPDF.App.exe --test-render "file.pdf" --verbose
   ```

2. Check for missing dependencies:
   - pdfium.dll in output directory
   - Visual C++ Runtime installed
   - Windows 10 SDK components

3. Validate PDF file:
   ```bash
   # Try rendering all pages to PNGs
   FluentPDF.App.exe --render-test "file.pdf" --output "test-output"
   ```

**Solutions:**

- **Missing native dependencies**: Copy pdfium.dll to CI agent
- **Insufficient permissions**: Run with appropriate file access rights
- **Corrupt PDF**: Test with known-good sample.pdf from Fixtures
- **Graphics drivers**: CI agents may lack proper GPU drivers (use software rendering)

## Diagnostic Commands

### System Diagnostics
```bash
FluentPDF.App.exe --diagnostics
```

Output includes:
- OS version and architecture
- .NET runtime version
- PDFium library version
- Available memory
- Display DPI configuration

### Test Render
```bash
FluentPDF.App.exe --test-render "path/to/file.pdf"
echo $LASTEXITCODE  # Windows
echo $?             # Linux/macOS
```

Creates diagnostic output file with:
- Render timing
- Memory usage before/after
- Strategy used (WriteableBitmap or FileBased)
- Any errors or warnings

### Batch Render Test
```bash
FluentPDF.App.exe --render-test "input.pdf" --output "output-dir"
```

Renders all pages to PNG files:
- `output-dir/page_1.png`
- `output-dir/page_2.png`
- etc.

Useful for:
- Visual regression testing
- Comparing rendering across versions
- Validating PDFium integration

### Crash Dump Capture
```bash
FluentPDF.App.exe --test-render "file.pdf" --capture-crash-dump
```

Enables Windows Error Reporting crash dumps for debugging native crashes in PDFium.

## Performance Benchmarks

Run BenchmarkDotNet tests to measure overhead:

```bash
dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj -- --filter *RenderingReliabilityBenchmarks*
```

**Performance Targets:**
- Memory snapshot capture: <10ms
- Memory delta calculation: <1ms
- Observability overhead: <50ms per render
- UI binding verification: <500ms (with timeout)

**Interpreting Results:**

```
| Method                          | Mean      | StdDev  |
|-------------------------------- |----------:|--------:|
| Benchmark_MemoryMonitorSnapshot | 2.5 ms    | 0.1 ms  | ✓ Target: <10ms
| Benchmark_RenderWithObservability| 245.2 ms | 12.3 ms | ✓ Target: <50ms overhead
| Benchmark_FileBasedStrategyIO   | 18.7 ms   | 1.2 ms  | ✓ Acceptable
```

If benchmarks show regression:
1. Profile with dotnet-trace or Visual Studio Profiler
2. Check for blocking I/O on hot path
3. Review memory allocation patterns
4. Optimize logging (use Serilog levels appropriately)

## Architecture Decision Records

### Why Strategy Pattern for Rendering?

**Problem:** WinUI InMemoryRandomAccessStream has intermittent bugs causing blank pages.

**Solution:** Multiple strategies with automatic fallback.

**Benefits:**
- Graceful degradation when primary approach fails
- Easy to add new strategies (e.g., GPU-accelerated rendering)
- Testable in isolation

**Tradeoffs:**
- Increased complexity
- Slight performance overhead (strategy selection)

### Why File-Based Fallback?

**Problem:** In-memory BitmapImage.SetSource sometimes fails silently.

**Solution:** Save PNG to temp file, load via file URI.

**Benefits:**
- Reliable (file I/O is well-tested)
- Works around WinUI bugs

**Tradeoffs:**
- Slower (disk I/O)
- Temp file management required
- Not suitable for sandboxed environments

### Why BeginRenderOperation Scope Pattern?

**Problem:** Manual logging is error-prone (easy to forget, inconsistent).

**Solution:** IDisposable scope that automatically logs start/end with timing.

**Benefits:**
- Guaranteed cleanup (even on exceptions)
- Consistent structured logging
- Memory tracking integrated automatically

**Tradeoffs:**
- Allocates scope object (minor GC pressure)
- Slightly more complex than manual logging

## Troubleshooting Checklist

When rendering fails, check:

- [ ] PDFium initialized successfully (check startup logs)
- [ ] PDF file is valid (test with Adobe Reader)
- [ ] pdfium.dll present in application directory
- [ ] Sufficient memory available (>500MB free)
- [ ] No file permission issues (read access to PDF)
- [ ] Visual C++ Runtime installed
- [ ] Windows version supported (>= Windows 10 1809)
- [ ] .NET 9 runtime installed
- [ ] Temp directory has write permissions (for FileBasedStrategy)
- [ ] Logs show which strategy was used
- [ ] Memory growth is within normal bounds (<100MB per page)

## Logging Configuration

FluentPDF uses Serilog for structured logging. Configure in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "FluentPDF.App.Services.RenderingObservabilityService": "Debug",
        "FluentPDF.App.Services.RenderingCoordinator": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/fluentpdf-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

**Recommended Log Levels:**

- **Production**: Information (shows render success/failure, fallback usage)
- **Development**: Debug (shows operation timing, memory metrics)
- **Troubleshooting**: Verbose (shows all strategy attempts, memory snapshots)

## Contributing

When modifying rendering reliability features:

1. Run all integration tests:
   ```bash
   dotnet test tests/FluentPDF.App.Tests --filter "Category=RenderingReliability"
   ```

2. Run performance benchmarks:
   ```bash
   dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj -- --benchmark
   ```

3. Test with problematic PDFs (keep samples in `tests/Fixtures/problematic/`)

4. Update this guide if adding new features or strategies

## References

- [WinUI InMemoryRandomAccessStream Bug](https://github.com/microsoft/microsoft-ui-xaml/issues/xxxx)
- [PDFium Documentation](https://pdfium.googlesource.com/pdfium/)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Getting-Started)
