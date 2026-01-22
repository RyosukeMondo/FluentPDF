namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Represents the profiling result for a single P/Invoke function.
/// </summary>
public record ProfilingResult
{
    /// <summary>
    /// Gets the name of the profiled function.
    /// </summary>
    public required string FunctionName { get; init; }

    /// <summary>
    /// Gets the number of iterations executed for this function.
    /// </summary>
    public int Iterations { get; init; }

    /// <summary>
    /// Gets a value indicating whether the profiling completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if profiling failed, or null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets performance metrics for this function.
    /// </summary>
    public PerformanceMetrics Performance { get; init; } = new PerformanceMetrics();

    /// <summary>
    /// Gets memory metrics for this function.
    /// </summary>
    public MemoryMetrics Memory { get; init; } = new MemoryMetrics();

    /// <summary>
    /// Gets a value indicating whether this function showed a performance regression compared to baseline.
    /// Null if baseline comparison was not performed.
    /// </summary>
    public bool? HasRegression { get; init; }

    /// <summary>
    /// Gets the percentage of performance degradation compared to baseline (e.g., 0.25 = 25% slower).
    /// Null if baseline comparison was not performed.
    /// </summary>
    public double? RegressionPercentage { get; init; }

    /// <summary>
    /// Gets the baseline median execution time (in milliseconds) used for comparison.
    /// Null if baseline comparison was not performed.
    /// </summary>
    public double? BaselineMedianMs { get; init; }
}
