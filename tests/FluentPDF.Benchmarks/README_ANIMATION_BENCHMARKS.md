# Animation Benchmarks for Liquid Glass UI

## Overview

This document describes the animation performance benchmarks created for Task 4.2 of the liquid-glass-ui specification.

**Specification**: `.spec-workflow/specs/liquid-glass-ui/tasks.md` Task 4.2
**Requirement**: 1.6.2 - Ensure 60 FPS maintained during all animations

## Benchmark Suite

The `AnimationBenchmarks` class in `Suites/AnimationBenchmarks.cs` measures animation performance for the liquid glass UI, including:

### Performance Targets

- **Frame Time**: < 16ms average (60 FPS)
- **Minimum FPS**: >= 30 FPS sustained
- **Acrylic Blur CPU Overhead**: < 5% CPU
- **Memory Allocation**: < 10MB additional

### Benchmarks

#### 1. Page Slide Transition Frame
**Target**: < 16ms per frame
**Description**: Measures single frame rendering time for page slide transitions (350ms total animation)

#### 2. Page Fade Transition Frame
**Target**: < 16ms per frame
**Description**: Measures single frame rendering time for page fade transitions (300ms total animation)

#### 3. Page Zoom Transition Frame
**Target**: < 16ms per frame
**Description**: Measures single frame rendering time for page zoom transitions (400ms total animation) with transform overhead

#### 4. Page Slide Full Animation
**Target**: >= 60 FPS sustained
**Description**: Renders all frames for a complete 350ms slide animation and validates FPS

#### 5. Panel Slide Frame
**Target**: < 16ms per frame
**Description**: Measures panel slide animation frame time (250ms total animation)

#### 6. Acrylic Blur Single Frame
**Target**: < 16ms per frame with blur effect
**Description**: Measures rendering with acrylic blur effect applied

#### 7. Acrylic Blur CPU Overhead
**Target**: < 5% CPU overhead
**Description**: Compares CPU usage with and without blur to calculate overhead percentage
**Validation**: Fails if overhead exceeds 5%

#### 8. Animation Memory Allocation
**Target**: < 10MB memory increase
**Description**: Measures memory allocation during 1 second of animation (60 frames)
**Validation**: Fails if memory increase exceeds 10MB

#### 9. Sustained 60 FPS Test
**Target**: >= 30 FPS minimum
**Description**: Validates sustained animation performance over 1 second
**Validation**: Fails if FPS drops below 30

## Running the Benchmarks

### Prerequisites

- .NET 8 SDK
- Windows x64 platform
- Release configuration (benchmarks fail in Debug mode)

### Command Line

```bash
# Run animation benchmarks only
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --animation

# Run all benchmark suites (including animations)
dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --all
```

### Output Formats

Benchmarks generate reports in multiple formats:
- **HTML**: `BenchmarkDotNet.Artifacts/results/*.html`
- **Markdown**: `BenchmarkDotNet.Artifacts/results/*.md`
- **JSON**: `BenchmarkDotNet.Artifacts/results/*.json`

## Benchmark Configuration

The benchmarks use the following configuration (from `Config/BenchmarkConfig.cs`):

- **Memory Diagnostics**: Enabled (MemoryDiagnoser)
- **Native Memory Profiling**: Enabled on Windows (NativeMemoryProfiler)
- **Statistics**: Mean, StdDev, Median, P95, Min, Max
- **Iterations**: 3 warmup, 10 measurement
- **Platform**: x64
- **Validation**: JIT optimizations enforced (Release mode only)

## Understanding Results

### Frame Time Analysis

Frame time results show how long each frame takes to render. For 60 FPS:
- **Target**: 16.67ms per frame
- **Good**: < 10ms (headroom for other operations)
- **Acceptable**: 10-16ms
- **Degraded**: 16-33ms (30-60 FPS)
- **Poor**: > 33ms (< 30 FPS)

### FPS Calculation

```
FPS = 1000ms / frame_time_ms
```

Example:
- 10ms frame time = 100 FPS
- 16.67ms frame time = 60 FPS
- 33.33ms frame time = 30 FPS

### CPU Overhead

Acrylic blur CPU overhead is calculated as:

```
overhead_% = ((blur_cpu - baseline_cpu) / baseline_cpu) * 100
```

**Target**: < 5%

Example:
- Baseline CPU: 10%
- With Blur CPU: 10.4%
- Overhead: 4% ✓ (passes)

### Memory Allocation

Memory allocation is measured over 60 frames (1 second at 60 FPS):

```
memory_increase_MB = (final_memory - initial_memory) / (1024 * 1024)
```

**Target**: < 10MB

## Simulation Approach

Since these benchmarks run without actual Avalonia UI rendering, they simulate frame rendering using:

1. **Frame Buffer**: 800x600 RGBA buffer (simulates render target)
2. **Blur Simulation**: Sample-based box blur approximation
3. **Transform Simulation**: Matrix calculation overhead
4. **CPU Measurement**: Process.TotalProcessorTime tracking

This approach provides:
- **Repeatable results** (no GPU variance)
- **CI/CD compatibility** (no display required)
- **Lower bound estimates** (actual GPU rendering may be faster)

## Validation

Benchmarks include runtime validation that fails if:
- FPS drops below 60 for full animations
- Sustained FPS drops below 30
- Acrylic blur overhead exceeds 5%
- Memory allocation exceeds 10MB

Failed validations throw `InvalidOperationException` with details.

## Integration with Spec Workflow

### Logging Implementation Results

After running benchmarks, log results using the spec workflow tool:

```bash
# Log implementation for task 4.2
# This should be done after benchmark execution completes
```

### Implementation Log Structure

```json
{
  "taskId": "4.2",
  "summary": "Created BenchmarkDotNet animation benchmarks measuring page transition frame times and acrylic blur overhead",
  "artifacts": {
    "classes": [{
      "name": "AnimationBenchmarks",
      "purpose": "Performance benchmarks for liquid glass UI animations",
      "location": "tests/FluentPDF.Benchmarks/Suites/AnimationBenchmarks.cs",
      "methods": [
        "PageSlideTransitionFrame",
        "PageFadeTransitionFrame",
        "PageZoomTransitionFrame",
        "PageSlideFullAnimation",
        "PanelSlideFrame",
        "AcrylicBlurFrame",
        "AcrylicBlurCpuOverhead",
        "AnimationMemoryAllocation",
        "SustainedAnimationPerformance"
      ],
      "isExported": true
    }]
  },
  "filesCreated": [
    "tests/FluentPDF.Benchmarks/Suites/AnimationBenchmarks.cs",
    "tests/FluentPDF.Benchmarks/README_ANIMATION_BENCHMARKS.md"
  ],
  "filesModified": [
    "tests/FluentPDF.Benchmarks/Program.cs",
    "tests/FluentPDF.Benchmarks/FluentPDF.Benchmarks.csproj",
    "tests/FluentPDF.Benchmarks/Suites/ThumbnailBenchmarks.cs"
  ],
  "statistics": {
    "linesAdded": 450,
    "linesRemoved": 10
  }
}
```

## Performance Baselines

Once initial benchmarks are run, establish baselines for regression testing:

1. Run benchmarks on reference hardware
2. Record Mean, P95, and Max values
3. Set alert thresholds (e.g., 10% regression)
4. Integrate into CI/CD pipeline

## Continuous Monitoring

Recommended CI/CD integration:

```yaml
- name: Run Animation Benchmarks
  run: dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --animation

- name: Upload Benchmark Results
  uses: actions/upload-artifact@v3
  with:
    name: benchmark-results
    path: BenchmarkDotNet.Artifacts/results/
```

## Troubleshooting

### Debug Mode Error

```
ERROR: Benchmarks must be run in Release mode.
Use: dotnet run -c Release --project tests/FluentPDF.Benchmarks
```

**Solution**: Always use `-c Release` configuration.

### No Results Generated

If benchmarks complete but no results appear, check:
1. `BenchmarkDotNet.Artifacts/` directory exists
2. Write permissions to output directory
3. BenchmarkDotNet logs for errors

### Validation Failures

If benchmarks throw validation exceptions:
1. **FPS < 60**: Hardware too slow or simulation too heavy
2. **Blur overhead > 5%**: Blur simulation needs optimization
3. **Memory > 10MB**: Check for memory leaks in simulation code

## References

- **Specification**: `.spec-workflow/specs/liquid-glass-ui/requirements.md`
- **Task Definition**: `.spec-workflow/specs/liquid-glass-ui/tasks.md` (Task 4.2)
- **BenchmarkDotNet Docs**: https://benchmarkdotnet.org/
- **Requirement 1.6.2**: 60 FPS target for all animations
- **Requirement 1.1**: < 5% CPU overhead for acrylic blur

## License

Part of FluentPDF project. See main LICENSE file.
