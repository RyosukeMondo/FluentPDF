namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Contains performance metrics for a profiled P/Invoke function.
/// </summary>
public record PerformanceMetrics
{
    /// <summary>
    /// Gets the minimum execution time across all iterations (in milliseconds).
    /// </summary>
    public double MinLatencyMs { get; init; }

    /// <summary>
    /// Gets the maximum execution time across all iterations (in milliseconds).
    /// </summary>
    public double MaxLatencyMs { get; init; }

    /// <summary>
    /// Gets the median execution time (in milliseconds).
    /// This is the 50th percentile, representing the typical execution time.
    /// </summary>
    public double MedianLatencyMs { get; init; }

    /// <summary>
    /// Gets the 95th percentile execution time (in milliseconds).
    /// 95% of executions complete within this time or faster.
    /// </summary>
    public double P95LatencyMs { get; init; }

    /// <summary>
    /// Gets the 99th percentile execution time (in milliseconds).
    /// 99% of executions complete within this time or faster.
    /// </summary>
    public double P99LatencyMs { get; init; }

    /// <summary>
    /// Gets the mean (average) execution time (in milliseconds).
    /// </summary>
    public double MeanLatencyMs { get; init; }

    /// <summary>
    /// Gets the standard deviation of execution times (in milliseconds).
    /// Measures the variability/consistency of execution times.
    /// </summary>
    public double StandardDeviationMs { get; init; }

    /// <summary>
    /// Gets the total execution time across all iterations (in milliseconds).
    /// </summary>
    public double TotalExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the throughput in operations per second.
    /// Calculated as: iterations / (total execution time in seconds).
    /// </summary>
    public double ThroughputOpsPerSecond { get; init; }
}
