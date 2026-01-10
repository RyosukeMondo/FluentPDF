# FluentPDF Performance Characteristics

This document describes FluentPDF's performance requirements, benchmarking methodology, and optimization strategies.

## Performance Requirements

### Application Launch

**Requirement**: Cold start P99 < 2 seconds

**Components**:
- PDFium initialization: < 100ms
- DI container setup: < 50ms
- First window render: < 1.85s

**Measured via**: `StartupBenchmarks` suite

### PDF Rendering

**Requirement**: Page render P99 < 1 second (text-heavy document at 100% zoom)

**Breakdown by Document Type** (at 100% zoom):
- Text-heavy: P99 < 1000ms (baseline requirement)
- Image-heavy: P99 < 2000ms
- Vector graphics: P99 < 1500ms
- Complex layout: P99 < 2000ms

**Zoom Level Impact**:
- 50%: ~40% faster than 100% (smaller bitmap)
- 150%: ~70% slower than 100% (larger bitmap)
- 200%: ~120% slower than 100% (4x bitmap size)

**Measured via**: `RenderingBenchmarks` suite

### Navigation

**Requirements**:
- Page navigation (next/previous): P99 < 1 second
- Zoom change: P99 < 2 seconds
- Random page jump: P99 < 1.5 seconds

**Measured via**: `NavigationBenchmarks` suite

### Memory

**Requirements**:
- Application baseline: < 50MB (no documents loaded)
- Single document loaded: < 200MB
- Sustained operation (100 pages): No memory leaks
- SafeHandles properly disposed after operations

**Limits**:
- Single page render: < 100MB allocated
- Large object heap (LOH): < 1MB per operation
- Gen2 collections: Minimize (< 1 per 100 operations)

**Measured via**: `MemoryBenchmarks` suite

## Benchmarking Methodology

### Framework

We use [BenchmarkDotNet](https://benchmarkdotnet.org/) for statistical performance analysis:
- Industry-standard .NET benchmarking framework
- Automatic warmup to eliminate JIT overhead
- Multiple iterations for statistical confidence
- Built-in outlier detection
- Memory profiling (managed + native)

### Configuration

```csharp
[Config(typeof(BenchmarkConfig))]
public class RenderingBenchmarks
{
    // Benchmark configuration:
    // - 5 warmup iterations
    // - 10 measurement iterations
    // - MemoryDiagnoser enabled
    // - NativeMemoryProfiler enabled
    // - Release mode enforced
}
```

### Statistical Analysis

**Metrics Reported**:
- **Mean**: Average execution time
- **StdDev**: Standard deviation (measures consistency)
- **P50**: Median (50th percentile)
- **P95**: 95th percentile (5% of operations are slower)
- **P99**: 99th percentile (1% of operations are slower, worst-case UX)

**Why P99?** The P99 metric represents worst-case user experience. If P99 < 1 second, users will rarely experience delays > 1 second.

### Hardware Consistency

**CI Environment**:
- Windows Server 2022
- 2-core CPU (GitHub Actions standard runner)
- 7GB RAM
- .NET 8.0 runtime

**Local Development**:
- Results vary by hardware
- Relative comparisons (% change) more important than absolute values
- Baseline comparison detects regressions

## Baseline Management

### Creating Baselines

Baselines are automatically created on `main` branch commits:
```json
{
  "commitSha": "abc123def456",
  "timestamp": "2026-01-11T00:00:00Z",
  "hardwareInfo": {
    "cpu": "AMD Ryzen 9 5950X",
    "ramGb": 32,
    "operatingSystem": "Windows 10",
    "runtimeVersion": "8.0.0"
  },
  "results": [
    {
      "benchmarkName": "RenderTextHeavy_100Percent",
      "meanNanoseconds": 287400000,
      "p99Nanoseconds": 325600000,
      "allocatedBytes": 13025280,
      "gen0Collections": 3,
      "gen1Collections": 1,
      "gen2Collections": 0
    }
  ]
}
```

### Regression Detection

**Thresholds**:
- **10-20% slower**: Warning (investigate, may be acceptable)
- **> 20% slower**: Critical regression (fail CI build)
- **> 10% faster**: Improvement (celebrate!)

**Calculation**:
```
percentChange = ((current - baseline) / baseline) * 100
```

**Example**:
- Baseline: 287ms mean
- Current: 315ms mean
- Change: ((315 - 287) / 287) * 100 = +9.75% (warning threshold)

### Historical Trends

Baseline files are stored in `tests/FluentPDF.Benchmarks/Baselines/`:
```
baseline-2026-01-10-abc123de.json
baseline-2026-01-09-def456ab.json
baseline-2026-01-08-789012cd.json
```

Track performance over time by comparing multiple baselines.

## Performance Optimization Strategies

### Rendering Optimization

**Current Approach**:
- PDFium native rendering (fast, battle-tested)
- Bitmap caching for repeated renders
- Zoom-specific bitmap generation

**Optimization Opportunities**:
1. **Tile-based rendering**: Render only visible viewport
2. **Progressive rendering**: Show low-res preview while rendering high-res
3. **Background pre-rendering**: Pre-render next/previous pages
4. **GPU acceleration**: Investigate Direct2D for bitmap operations

### Memory Optimization

**Current Approach**:
- SafeHandle for native resource management
- `IAsyncDisposable` for async cleanup
- Gen2 avoidance (keep allocations < 85KB)

**Best Practices**:
1. **Dispose bitmaps promptly**: Don't hold references longer than needed
2. **Use pooling**: ArrayPool for temporary buffers
3. **Avoid LOH**: Keep allocations < 85KB to avoid Large Object Heap
4. **Monitor Gen2**: High Gen2 collections indicate memory pressure

**Memory Leak Detection**:
```csharp
// Before operation
var handlesBefore = Process.GetCurrentProcess().HandleCount;

// Perform operation
await RenderPageAsync(doc, pageIndex);

// After operation
var handlesAfter = Process.GetCurrentProcess().HandleCount;

// Verify no leak
Assert.Equal(handlesBefore, handlesAfter);
```

### Startup Optimization

**Current Approach**:
- Lazy PDFium initialization (init on first use)
- Minimal DI container (only required services)
- Fast service registration (no reflection-heavy operations)

**Optimization Opportunities**:
1. **Parallel initialization**: Init PDFium and DI concurrently
2. **Ahead-of-time compilation**: Use NativeAOT for faster startup
3. **Splash screen**: Show UI while initializing

### Navigation Optimization

**Current Approach**:
- Synchronous page switching (simple, predictable)
- On-demand page rendering

**Optimization Opportunities**:
1. **Background pre-rendering**: Render next page while user views current
2. **Render queue**: Prioritize visible page, queue adjacent pages
3. **Cancellation**: Cancel background renders when user navigates away

## CI Performance Monitoring

### Automated Regression Detection

**Workflow**: `.github/workflows/benchmark.yml`

**Trigger**: Push to `main`, pull request

**Process**:
1. Run full benchmark suite
2. Load baseline from `main` branch
3. Compare results
4. Detect regressions (10%, 20% thresholds)
5. Post PR comment with summary
6. Fail build if critical regression (> 20%)

**Example PR Comment**:
```markdown
## Benchmark Results

| Benchmark | Baseline | Current | Change | Status |
|-----------|----------|---------|--------|--------|
| RenderTextHeavy_100 | 287ms | 315ms | +9.8% | ⚠️ Warning |
| RenderImageHeavy_100 | 456ms | 512ms | +12.3% | ⚠️ Warning |
| Initialize_PDFium | 45ms | 42ms | -6.7% | ✅ Improvement |

**Summary**: 2 warnings, 1 improvement, no critical regressions
```

### Performance Reports

**HTML Report**: Generated after each benchmark run
- Summary table with all benchmarks
- Memory profile (Gen0/1/2, allocations)
- Comparison vs baseline (red = regression, green = improvement)
- Latency distribution charts

**Accessing Reports**:
1. Go to GitHub Actions workflow run
2. Download "benchmark-results" artifact
3. Open `report.html` in browser

## Performance Testing Guide

### Running Benchmarks Locally

```bash
# Full suite (15-20 minutes)
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --all

# Quick check (2-3 minutes)
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --rendering
```

### Profiling with Visual Studio

1. Open FluentPDF.sln
2. Set FluentPDF.App as startup project
3. Build in Release mode
4. Debug > Performance Profiler
5. Select: CPU Usage, Memory Usage, .NET Object Allocation
6. Start profiling
7. Perform operations (load PDF, render pages, zoom, navigate)
8. Stop profiling and analyze

### Profiling with dotnet-trace

```bash
# Install tool
dotnet tool install --global dotnet-trace

# Start app
dotnet run -c Release --project src/FluentPDF.App

# Capture trace (in another terminal)
dotnet trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime

# Analyze trace in PerfView or Visual Studio
```

### Memory Leak Detection

```bash
# Install dotnet-dump
dotnet tool install --global dotnet-dump

# Run app and perform operations
dotnet run -c Release --project src/FluentPDF.App

# Capture memory dump
dotnet dump collect --process-id <PID>

# Analyze dump
dotnet dump analyze <dump-file>
> dumpheap -stat
> gcroot <address>
```

## Continuous Improvement

### Performance Review Process

1. **Monitor baselines**: Review benchmark results on main branch
2. **Investigate regressions**: Understand why performance degraded
3. **Optimize critical paths**: Focus on P99 latency and memory allocations
4. **Update baselines**: Accept new baseline if performance improves

### Adding New Benchmarks

When adding features, add corresponding benchmarks:

```csharp
[Benchmark]
public async Task NewFeature_Scenario()
{
    // Setup
    var service = _serviceProvider.GetRequiredService<INewService>();

    // Benchmark operation
    await service.PerformOperationAsync();

    // Cleanup (not measured)
}
```

### Performance Budget

**Render Budget**: 16.67ms per frame (60 FPS)
- PDFium render: ~10ms
- Bitmap copy: ~2ms
- UI update: ~2ms
- Buffer: ~2.67ms

**Memory Budget**: 200MB total
- Application baseline: 50MB
- Loaded document: 100MB
- Rendered bitmaps (3 pages): 50MB

**Startup Budget**: 2 seconds
- PDFium init: 100ms
- DI setup: 50ms
- Window creation: 200ms
- First render: 1650ms

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/core/performance/)
- [Memory Management in .NET](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/)
- [FluentPDF Benchmark Suite](../tests/FluentPDF.Benchmarks/README.md)

## Performance SLOs (Service Level Objectives)

### Tier 1 (Critical)
- App launch P99 < 2 seconds
- Text rendering P99 < 1 second at 100% zoom
- No memory leaks in sustained operation

### Tier 2 (Important)
- Image/vector rendering P99 < 2 seconds
- Navigation P99 < 1 second
- Memory < 200MB for single document

### Tier 3 (Nice to Have)
- All benchmarks within 10% of baseline
- Gen2 collections < 1 per 100 operations
- Startup consistency (low standard deviation)

**SLO Monitoring**: Automated via CI benchmarks, reviewed weekly.
