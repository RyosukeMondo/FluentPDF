# FluentPDF Benchmarks

Comprehensive performance benchmarking suite for FluentPDF using BenchmarkDotNet.

## Overview

This benchmark suite provides statistical performance analysis across multiple dimensions:
- **Rendering Performance**: PDF page rendering across document types and zoom levels
- **Memory Profiling**: Managed and native memory allocations, GC behavior
- **Startup Performance**: Application initialization and cold start metrics
- **Navigation Performance**: Page transitions and zoom operations

## Running Benchmarks Locally

### Prerequisites

- .NET 8.0 SDK
- Release build configuration (benchmarks will not run in Debug mode)
- PDFium native libraries (automatically copied during build)
- At least 4GB RAM recommended
- 10-15 minutes for full benchmark suite execution

### Quick Start

Run all benchmarks:
```bash
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --all
```

Run specific benchmark suite:
```bash
# Rendering benchmarks
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --rendering

# Memory benchmarks
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --memory

# Startup benchmarks
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --startup

# Navigation benchmarks
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --navigation
```

### Output

Benchmark results are saved to `BenchmarkDotNet.Artifacts/results/`:
- **JSON**: Machine-readable results for analysis and regression detection
- **HTML**: Human-readable report with tables and charts
- **Markdown**: Summary report for documentation
- **CSV**: Raw data for custom analysis

## Benchmark Suites

### RenderingBenchmarks

Measures PDF page rendering performance across document types and zoom levels.

**Document Types**:
- `text-heavy.pdf`: Plain text document (simulates documentation)
- `image-heavy.pdf`: Photo gallery (simulates image-rich PDFs)
- `vector-graphics.pdf`: Technical diagrams (simulates CAD/engineering drawings)
- `complex-layout.pdf`: Magazine-style layout (simulates marketing materials)

**Zoom Levels**: 50%, 100%, 150%, 200%

**Key Metrics**:
- P50/P95/P99 latency (median, 95th, 99th percentile)
- Mean execution time
- Standard deviation
- **Requirement**: P99 < 1 second for text-heavy at 100% zoom

**Example Results**:
```
| Method             | ZoomLevel | Mean     | P99      | Allocated |
|------------------- |---------- |---------:|---------:|----------:|
| RenderTextHeavy    | 0.5       | 125.3 ms | 142.1 ms |   5.21 MB |
| RenderTextHeavy    | 1.0       | 287.4 ms | 325.6 ms |  12.43 MB |
| RenderImageHeavy   | 1.0       | 456.2 ms | 512.8 ms |  28.91 MB |
| RenderVectorGraphics | 1.0     | 342.1 ms | 389.4 ms |  15.67 MB |
```

### MemoryBenchmarks

Profiles memory allocations and detects memory leaks.

**Tests**:
- `LoadAndDispose_Document`: Document lifecycle memory management
- `RenderAndDispose_Page`: Page rendering memory cleanup
- `LoadRender_100Pages`: Stress test for sustained operation

**Key Metrics**:
- Gen0/Gen1/Gen2 GC collections
- Total allocated bytes (managed memory)
- Native memory allocations (via NativeMemoryProfiler)
- SafeHandle disposal verification

**Leak Detection**: Benchmarks verify that SafeHandles are properly disposed and no native memory leaks occur.

**Example Results**:
```
| Method              | Gen0  | Gen1  | Allocated | Native Mem |
|-------------------- |------:|------:|----------:|-----------:|
| LoadAndDispose      | 1.23  | 0.45  |   4.21 MB |     2.1 MB |
| RenderAndDispose    | 3.45  | 1.12  |  12.34 MB |     8.4 MB |
| LoadRender_100Pages | 45.67 | 12.34 | 523.45 MB |   421.2 MB |
```

### StartupBenchmarks

Measures application initialization performance.

**Tests**:
- `Initialize_PDFium`: PDFium library initialization time
- `Initialize_DIContainer`: Dependency injection container setup
- `ColdStart_FullApplication`: Complete application cold start

**Requirements**:
- PDFium initialization: < 100ms
- DI container setup: < 50ms
- Full cold start P99: < 2 seconds

**Example Results**:
```
| Method                    | Mean    | P95     | P99     |
|-------------------------- |--------:|--------:|--------:|
| Initialize_PDFium         |  45.2 ms|  52.1 ms|  58.3 ms|
| Initialize_DIContainer    |  23.4 ms|  28.9 ms|  32.1 ms|
| ColdStart_FullApplication | 1234 ms | 1456 ms | 1623 ms |
```

### NavigationBenchmarks

Measures performance of common user interactions.

**Tests**:
- `Navigate_NextPage`: Forward page navigation
- `Navigate_PreviousPage`: Backward page navigation
- `ZoomChange_100To150`: Zoom operation with re-render
- `JumpToPage_Random`: Random page access

**Requirements**:
- Page navigation P99: < 1 second
- Zoom change P99: < 2 seconds

## Interpreting Results

### Statistical Significance

BenchmarkDotNet performs statistical analysis to ensure reliable results:
- Multiple warmup iterations eliminate JIT compilation overhead
- Multiple measurement iterations provide statistical confidence
- Standard deviation indicates result stability
- Outliers are detected and reported

### Percentiles

- **P50 (Median)**: Middle value, represents typical performance
- **P95**: 95% of operations complete within this time
- **P99**: 99% of operations complete within this time (important for worst-case UX)

### Memory Metrics

- **Gen0**: Frequent, fast collections (short-lived objects)
- **Gen1**: Medium-lived objects
- **Gen2**: Long-lived objects (expensive collections)
- **Allocated**: Total managed memory allocated per operation
- **Native Mem**: Unmanaged memory (PDFium, bitmaps)

## Baseline Management

### Creating a Baseline

Baselines are automatically created when benchmarks run. To manually save a baseline:

```csharp
var manager = new BaselineManager("path/to/baselines");
var runInfo = new BenchmarkRunInfo
{
    CommitSha = "abc123",
    Timestamp = DateTime.UtcNow,
    HardwareInfo = new HardwareInfo { /* ... */ },
    Results = benchmarkResults
};
manager.SaveBaseline(runInfo);
```

Baselines are saved as: `baseline-YYYY-MM-DD-{SHA}.json`

### Comparing Against Baseline

```csharp
var baseline = manager.LoadLatestBaseline().Value;
var current = /* current benchmark results */;
var comparison = manager.Compare(current, baseline).Value;

// Detect regressions
var regressions = manager.HasRegression(comparison, thresholdPercent: 10.0).Value;
if (regressions.HasRegressions)
{
    foreach (var regression in regressions.Regressions)
    {
        Console.WriteLine($"{regression.BenchmarkName}: {regression.PercentChange:F1}% slower");
    }
}
```

### Regression Thresholds

- **10%**: Warning threshold (investigate)
- **20%**: Critical threshold (fail CI build)

## CI Integration

Benchmarks run automatically in CI via `.github/workflows/benchmark.yml`:
- Runs on Windows Server 2022 (consistent hardware)
- Executes on push to `main` and on pull requests
- Compares results against baseline from `main` branch
- Fails build if regression > 20%
- Posts PR comment with benchmark summary

### CI Workflow Steps

1. Checkout code
2. Setup .NET 8
3. Build native libraries (PDFium)
4. Run benchmarks in Release mode
5. Load baseline from main branch
6. Compare results and detect regressions
7. Upload HTML report as artifact
8. Post PR comment with summary

### Viewing CI Results

- **Artifacts**: Download HTML report from GitHub Actions artifacts
- **PR Comments**: View summary table with regressions highlighted
- **Build Status**: Green = no critical regressions, Red = > 20% regression

## Fixtures

Sample PDF files in `Fixtures/` directory:

| File | Pages | Size | Description |
|------|-------|------|-------------|
| text-heavy.pdf | 3 | 1.2 MB | Plain text document |
| image-heavy.pdf | 5 | 4.8 MB | Photo gallery |
| vector-graphics.pdf | 4 | 2.1 MB | Technical diagrams |
| complex-layout.pdf | 3 | 3.4 MB | Magazine-style layout |

See `Fixtures/README.md` for detailed characteristics of each PDF.

## Troubleshooting

### Benchmarks Fail to Run in Debug Mode

**Error**: "Benchmarks must be run in Release mode"

**Solution**: Always use `-c Release` flag:
```bash
dotnet run -c Release --project tests/FluentPDF.Benchmarks
```

### PDFium Initialization Failure

**Error**: "Failed to initialize PDFium library"

**Solution**: Ensure PDFium native libraries are in output directory:
```bash
dotnet build -c Release src/FluentPDF.Rendering
```

### Out of Memory During Benchmarks

**Error**: OutOfMemoryException during stress tests

**Solution**:
- Close other applications
- Ensure at least 4GB free RAM
- Consider running individual suites instead of `--all`

### High Variance in Results

**Symptom**: Large standard deviation, inconsistent results

**Solution**:
- Close background applications
- Disable antivirus temporarily
- Run benchmarks on dedicated hardware
- Increase warmup/iteration counts in BenchmarkConfig

## Advanced Configuration

### Custom BenchmarkConfig

Modify `Config/BenchmarkConfig.cs` to adjust:
- Warmup iteration count
- Measurement iteration count
- Exporters (JSON, HTML, Markdown, CSV)
- Diagnosers (MemoryDiagnoser, NativeMemoryProfiler)

### Running Specific Methods

Use BenchmarkDotNet filters:
```bash
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --filter *RenderTextHeavy*
```

### Custom Analysis

Parse JSON results for custom analysis:
```csharp
var json = File.ReadAllText("BenchmarkDotNet.Artifacts/results/results.json");
var results = JsonSerializer.Deserialize<BenchmarkResults>(json);
// Perform custom analysis
```

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Best Practices](../../docs/PERFORMANCE.md)
- [FluentPDF Architecture](../../docs/ARCHITECTURE.md)

## Contributing

When adding new benchmarks:
1. Follow naming convention: `{Operation}_{Variant}`
2. Use `[Benchmark]` attribute
3. Add to appropriate suite or create new suite
4. Update this README with benchmark description
5. Verify benchmarks run in CI
6. Document expected performance characteristics
