namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Represents a comprehensive profiling report for marshaling performance analysis.
/// </summary>
public record ProfilingReport
{
    /// <summary>
    /// Gets the timestamp when this profiling report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total number of functions profiled in this report.
    /// </summary>
    public int TotalFunctions { get; init; }

    /// <summary>
    /// Gets the number of iterations used per function for profiling.
    /// </summary>
    public int IterationsPerFunction { get; init; }

    /// <summary>
    /// Gets the total execution time across all profiled functions (in milliseconds).
    /// </summary>
    public double TotalExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the total managed memory allocated during profiling (in bytes).
    /// </summary>
    public long TotalManagedMemoryBytes { get; init; }

    /// <summary>
    /// Gets the total number of garbage collections triggered during profiling.
    /// </summary>
    public int TotalGarbageCollections { get; init; }

    /// <summary>
    /// Gets profiling results organized by function name.
    /// </summary>
    public required Dictionary<string, ProfilingResult> ResultsByFunction { get; init; }

    /// <summary>
    /// Gets the list of functions that showed performance regressions compared to baseline.
    /// Null if baseline comparison was not performed.
    /// </summary>
    public List<string>? RegressedFunctions { get; init; }

    /// <summary>
    /// Gets the baseline file path used for regression detection, if applicable.
    /// </summary>
    public string? BaselinePath { get; init; }

    /// <summary>
    /// Gets the regression threshold used for detecting slowdowns (e.g., 0.20 = 20%).
    /// Null if baseline comparison was not performed.
    /// </summary>
    public double? RegressionThreshold { get; init; }

    /// <summary>
    /// Gets summary statistics for overall profiling performance.
    /// </summary>
    public ProfilingSummary Summary { get; init; } = new ProfilingSummary();
}

/// <summary>
/// Contains summary statistics for profiling performance.
/// </summary>
public record ProfilingSummary
{
    /// <summary>
    /// Gets the median execution time across all functions (in milliseconds).
    /// </summary>
    public double MedianExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the 95th percentile execution time (in milliseconds).
    /// </summary>
    public double P95ExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the 99th percentile execution time (in milliseconds).
    /// </summary>
    public double P99ExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the average memory allocation per function (in bytes).
    /// </summary>
    public long AverageMemoryBytesPerFunction { get; init; }

    /// <summary>
    /// Gets the number of functions that triggered garbage collection.
    /// </summary>
    public int FunctionsWithGarbageCollections { get; init; }
}
