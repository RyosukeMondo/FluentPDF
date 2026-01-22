using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification.Profilers;
using FluentPDF.Rendering.Interop.Verification.Reports;
using System.Text.Json;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Unit tests for MarshalingPerformanceProfiler class.
/// Validates profiling accuracy, baseline comparison, and statistical calculations.
/// </summary>
public class MarshalingPerformanceProfilerTests : IDisposable
{
    private readonly MarshalingPerformanceProfiler _profiler;
    private readonly string _testOutputDirectory;

    public MarshalingPerformanceProfilerTests()
    {
        _profiler = new MarshalingPerformanceProfiler(typeof(PdfiumInterop));
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF_ProfilerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testOutputDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ProfileFunctionAsync_WithValidFunction_ReturnsSuccessResult()
    {
        // Arrange
        const string functionName = "FPDF_InitLibrary";
        const int iterations = 100;

        // Act
        var result = await _profiler.ProfileFunctionAsync(functionName, iterations);

        // Assert
        Assert.True(result.Success, $"Profiling should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(functionName, result.FunctionName);
        Assert.Equal(iterations, result.Iterations);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Performance);
        Assert.NotNull(result.Memory);
    }

    [Fact]
    public async Task ProfileFunctionAsync_WithInvalidFunction_ReturnsFailureResult()
    {
        // Arrange
        const string functionName = "NonExistentFunction";
        const int iterations = 100;

        // Act
        var result = await _profiler.ProfileFunctionAsync(functionName, iterations);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(functionName, result.FunctionName);
        Assert.Equal(iterations, result.Iterations);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileFunctionAsync_CalculatesPerformanceMetricsCorrectly()
    {
        // Arrange
        const string functionName = "FPDF_InitLibrary";
        const int iterations = 1000;

        // Act
        var result = await _profiler.ProfileFunctionAsync(functionName, iterations);

        // Assert
        Assert.True(result.Success);
        var perf = result.Performance;

        // Validate latency metrics
        Assert.True(perf.MinLatencyMs >= 0, "Min latency should be non-negative");
        Assert.True(perf.MaxLatencyMs >= perf.MinLatencyMs, "Max should be >= Min");
        Assert.True(perf.MedianLatencyMs >= perf.MinLatencyMs && perf.MedianLatencyMs <= perf.MaxLatencyMs,
            "Median should be between Min and Max");
        Assert.True(perf.P95LatencyMs >= perf.MedianLatencyMs, "P95 should be >= Median");
        Assert.True(perf.P99LatencyMs >= perf.P95LatencyMs, "P99 should be >= P95");
        Assert.True(perf.MeanLatencyMs >= perf.MinLatencyMs, "Mean should be >= Min");

        // Validate standard deviation
        Assert.True(perf.StandardDeviationMs >= 0, "Standard deviation should be non-negative");

        // Validate total execution time
        Assert.True(perf.TotalExecutionTimeMs > 0, "Total execution time should be positive");

        // Validate throughput
        Assert.True(perf.ThroughputOpsPerSecond > 0, "Throughput should be positive");
    }

    [Fact]
    public async Task ProfileFunctionAsync_TracksMemoryMetricsCorrectly()
    {
        // Arrange
        const string functionName = "FPDF_InitLibrary";
        const int iterations = 1000;

        // Act
        var result = await _profiler.ProfileFunctionAsync(functionName, iterations);

        // Assert
        Assert.True(result.Success);
        var mem = result.Memory;

        // Validate memory metrics
        Assert.True(mem.ManagedMemoryBytes >= 0, "Managed memory should be non-negative");
        Assert.True(mem.UnmanagedMemoryBytes >= 0, "Unmanaged memory should be non-negative");
        Assert.True(mem.Gen0Collections >= 0, "Gen0 collections should be non-negative");
        Assert.True(mem.Gen1Collections >= 0, "Gen1 collections should be non-negative");
        Assert.True(mem.Gen2Collections >= 0, "Gen2 collections should be non-negative");
        Assert.True(mem.TotalCollections >= 0, "Total collections should be non-negative");
        Assert.Equal(mem.Gen0Collections + mem.Gen1Collections + mem.Gen2Collections, mem.TotalCollections);
        Assert.True(mem.AverageManagedMemoryPerIteration >= 0, "Average memory per iteration should be non-negative");
    }

    [Fact]
    public async Task ProfileFunctionAsync_WithZeroIterations_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string functionName = "FPDF_InitLibrary";
        const int iterations = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _profiler.ProfileFunctionAsync(functionName, iterations));
    }

    [Fact]
    public async Task ProfileFunctionAsync_WithNegativeIterations_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string functionName = "FPDF_InitLibrary";
        const int iterations = -1;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _profiler.ProfileFunctionAsync(functionName, iterations));
    }

    [Fact]
    public async Task ProfileFunctionAsync_WithNullFunctionName_ThrowsArgumentException()
    {
        // Arrange
        string? functionName = null;
        const int iterations = 100;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _profiler.ProfileFunctionAsync(functionName!, iterations));
    }

    [Fact]
    public async Task ProfileFunctionAsync_WithEmptyFunctionName_ThrowsArgumentException()
    {
        // Arrange
        const string functionName = "";
        const int iterations = 100;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _profiler.ProfileFunctionAsync(functionName, iterations));
    }

    [Fact]
    public async Task ProfileAllFunctionsAsync_ProfilesAllDllImportMethods()
    {
        // Arrange
        const int iterations = 100;

        // Act
        var report = await _profiler.ProfileAllFunctionsAsync(iterations);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.TotalFunctions > 0, "Should find at least one DllImport method");
        Assert.Equal(iterations, report.IterationsPerFunction);
        Assert.NotNull(report.ResultsByFunction);
        Assert.Equal(report.TotalFunctions, report.ResultsByFunction.Count);
        Assert.True(report.TotalExecutionTimeMs >= 0, "Total execution time should be non-negative");
        Assert.True(report.GeneratedAt <= DateTime.UtcNow, "Generated timestamp should be in the past or now");
    }

    [Fact]
    public async Task ProfileAllFunctionsAsync_CalculatesSummaryStatistics()
    {
        // Arrange
        const int iterations = 100;

        // Act
        var report = await _profiler.ProfileAllFunctionsAsync(iterations);

        // Assert
        Assert.NotNull(report.Summary);
        Assert.True(report.Summary.MedianExecutionTimeMs >= 0, "Median execution time should be non-negative");
        Assert.True(report.Summary.P95ExecutionTimeMs >= report.Summary.MedianExecutionTimeMs,
            "P95 should be >= Median");
        Assert.True(report.Summary.P99ExecutionTimeMs >= report.Summary.P95ExecutionTimeMs,
            "P99 should be >= P95");
        Assert.True(report.Summary.AverageMemoryBytesPerFunction >= 0,
            "Average memory per function should be non-negative");
        Assert.True(report.Summary.FunctionsWithGarbageCollections >= 0,
            "Functions with GC should be non-negative");
    }

    [Fact]
    public async Task CompareAgainstBaselineAsync_DetectsRegressions()
    {
        // Arrange
        const int iterations = 100;
        const double regressionThreshold = 0.20; // 20%

        // Create baseline report with slower times
        var baselineReport = new ProfilingReport
        {
            GeneratedAt = DateTime.UtcNow.AddDays(-1),
            TotalFunctions = 2,
            IterationsPerFunction = iterations,
            TotalExecutionTimeMs = 100.0,
            TotalManagedMemoryBytes = 1000,
            TotalGarbageCollections = 0,
            ResultsByFunction = new Dictionary<string, ProfilingResult>
            {
                ["FPDF_InitLibrary"] = new ProfilingResult
                {
                    FunctionName = "FPDF_InitLibrary",
                    Iterations = iterations,
                    Success = true,
                    Performance = new PerformanceMetrics
                    {
                        MedianLatencyMs = 1.0, // Baseline: 1ms
                        MinLatencyMs = 0.5,
                        MaxLatencyMs = 2.0,
                        P95LatencyMs = 1.8,
                        P99LatencyMs = 1.9,
                        MeanLatencyMs = 1.0,
                        StandardDeviationMs = 0.3,
                        TotalExecutionTimeMs = 100.0,
                        ThroughputOpsPerSecond = 1000.0
                    },
                    Memory = new MemoryMetrics
                    {
                        ManagedMemoryBytes = 500,
                        UnmanagedMemoryBytes = 0,
                        Gen0Collections = 0,
                        Gen1Collections = 0,
                        Gen2Collections = 0,
                        AverageManagedMemoryPerIteration = 5
                    }
                }
            },
            Summary = new ProfilingSummary()
        };

        // Create current report with much slower times (30% regression)
        var currentReport = new ProfilingReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalFunctions = 2,
            IterationsPerFunction = iterations,
            TotalExecutionTimeMs = 130.0,
            TotalManagedMemoryBytes = 1000,
            TotalGarbageCollections = 0,
            ResultsByFunction = new Dictionary<string, ProfilingResult>
            {
                ["FPDF_InitLibrary"] = new ProfilingResult
                {
                    FunctionName = "FPDF_InitLibrary",
                    Iterations = iterations,
                    Success = true,
                    Performance = new PerformanceMetrics
                    {
                        MedianLatencyMs = 1.3, // Current: 1.3ms (30% slower)
                        MinLatencyMs = 0.6,
                        MaxLatencyMs = 2.5,
                        P95LatencyMs = 2.3,
                        P99LatencyMs = 2.4,
                        MeanLatencyMs = 1.3,
                        StandardDeviationMs = 0.4,
                        TotalExecutionTimeMs = 130.0,
                        ThroughputOpsPerSecond = 769.2
                    },
                    Memory = new MemoryMetrics
                    {
                        ManagedMemoryBytes = 500,
                        UnmanagedMemoryBytes = 0,
                        Gen0Collections = 0,
                        Gen1Collections = 0,
                        Gen2Collections = 0,
                        AverageManagedMemoryPerIteration = 5
                    }
                }
            },
            Summary = new ProfilingSummary()
        };

        // Save baseline to file
        var baselinePath = Path.Combine(_testOutputDirectory, "baseline.json");
        var baselineJson = JsonSerializer.Serialize(baselineReport, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(baselinePath, baselineJson);

        // Act
        var comparisonReport = await _profiler.CompareAgainstBaselineAsync(
            currentReport,
            baselinePath,
            regressionThreshold);

        // Assert
        Assert.NotNull(comparisonReport);
        Assert.NotNull(comparisonReport.RegressedFunctions);
        Assert.Contains("FPDF_InitLibrary", comparisonReport.RegressedFunctions);
        Assert.Equal(baselinePath, comparisonReport.BaselinePath);
        Assert.Equal(regressionThreshold, comparisonReport.RegressionThreshold);

        // Verify regression details
        var result = comparisonReport.ResultsByFunction["FPDF_InitLibrary"];
        Assert.True(result.HasRegression);
        Assert.NotNull(result.RegressionPercentage);
        Assert.True(result.RegressionPercentage > regressionThreshold,
            $"Expected regression > {regressionThreshold}, got {result.RegressionPercentage}");
        Assert.Equal(1.0, result.BaselineMedianMs);
    }

    [Fact]
    public async Task CompareAgainstBaselineAsync_WithNoRegressions_ReturnsEmptyList()
    {
        // Arrange
        const int iterations = 100;
        const double regressionThreshold = 0.20; // 20%

        // Create baseline and current reports with identical times
        var baselineReport = new ProfilingReport
        {
            GeneratedAt = DateTime.UtcNow.AddDays(-1),
            TotalFunctions = 1,
            IterationsPerFunction = iterations,
            TotalExecutionTimeMs = 100.0,
            TotalManagedMemoryBytes = 1000,
            TotalGarbageCollections = 0,
            ResultsByFunction = new Dictionary<string, ProfilingResult>
            {
                ["FPDF_InitLibrary"] = new ProfilingResult
                {
                    FunctionName = "FPDF_InitLibrary",
                    Iterations = iterations,
                    Success = true,
                    Performance = new PerformanceMetrics { MedianLatencyMs = 1.0 },
                    Memory = new MemoryMetrics()
                }
            },
            Summary = new ProfilingSummary()
        };

        var currentReport = baselineReport with { GeneratedAt = DateTime.UtcNow };

        // Save baseline to file
        var baselinePath = Path.Combine(_testOutputDirectory, "baseline-no-regression.json");
        var baselineJson = JsonSerializer.Serialize(baselineReport, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(baselinePath, baselineJson);

        // Act
        var comparisonReport = await _profiler.CompareAgainstBaselineAsync(
            currentReport,
            baselinePath,
            regressionThreshold);

        // Assert
        Assert.NotNull(comparisonReport);
        Assert.NotNull(comparisonReport.RegressedFunctions);
        Assert.Empty(comparisonReport.RegressedFunctions);

        var result = comparisonReport.ResultsByFunction["FPDF_InitLibrary"];
        Assert.False(result.HasRegression);
    }

    [Fact]
    public async Task CompareAgainstBaselineAsync_WithNonExistentBaseline_ThrowsFileNotFoundException()
    {
        // Arrange
        var currentReport = new ProfilingReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalFunctions = 0,
            IterationsPerFunction = 100,
            TotalExecutionTimeMs = 0,
            TotalManagedMemoryBytes = 0,
            TotalGarbageCollections = 0,
            ResultsByFunction = new Dictionary<string, ProfilingResult>(),
            Summary = new ProfilingSummary()
        };
        var baselinePath = Path.Combine(_testOutputDirectory, "nonexistent.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _profiler.CompareAgainstBaselineAsync(currentReport, baselinePath));
    }

    [Fact]
    public async Task CompareAgainstBaselineAsync_WithNullReport_ThrowsArgumentNullException()
    {
        // Arrange
        ProfilingReport? currentReport = null;
        var baselinePath = Path.Combine(_testOutputDirectory, "baseline.json");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _profiler.CompareAgainstBaselineAsync(currentReport!, baselinePath));
    }

    [Fact]
    public async Task CompareAgainstBaselineAsync_WithNullBaselinePath_ThrowsArgumentException()
    {
        // Arrange
        var currentReport = new ProfilingReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalFunctions = 0,
            IterationsPerFunction = 100,
            TotalExecutionTimeMs = 0,
            TotalManagedMemoryBytes = 0,
            TotalGarbageCollections = 0,
            ResultsByFunction = new Dictionary<string, ProfilingResult>(),
            Summary = new ProfilingSummary()
        };
        string? baselinePath = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _profiler.CompareAgainstBaselineAsync(currentReport, baselinePath!));
    }

    [Fact]
    public async Task ProfileAllFunctionsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        const int iterations = 10000; // Large iteration count to allow time for cancellation
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _profiler.ProfileAllFunctionsAsync(iterations, cts.Token));
    }
}
