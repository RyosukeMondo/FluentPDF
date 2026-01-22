using FluentPDF.Rendering.Interop.Verification.Reports;

namespace FluentPDF.Rendering.Interop.Verification.Profilers;

/// <summary>
/// Defines the contract for profiling marshaling performance of P/Invoke functions.
/// </summary>
public interface IMarshalingProfiler
{
    /// <summary>
    /// Profiles a single P/Invoke function across multiple iterations to measure performance.
    /// </summary>
    /// <param name="functionName">The name of the function to profile.</param>
    /// <param name="iterations">The number of iterations to run (default: 1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A profiling result containing performance and memory metrics.</returns>
    Task<ProfilingResult> ProfileFunctionAsync(
        string functionName,
        int iterations = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Profiles all P/Invoke functions in the target assembly, measuring their performance characteristics.
    /// </summary>
    /// <param name="iterations">The number of iterations per function (default: 1000).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A comprehensive profiling report for all functions.</returns>
    Task<ProfilingReport> ProfileAllFunctionsAsync(
        int iterations = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares current profiling results against a baseline to detect performance regressions.
    /// </summary>
    /// <param name="currentReport">The current profiling report to analyze.</param>
    /// <param name="baselinePath">Path to the baseline JSON file containing historical performance data.</param>
    /// <param name="regressionThreshold">Percentage threshold for regression detection (default: 20% slowdown).</param>
    /// <returns>A profiling report with regression analysis and warnings for functions that degraded.</returns>
    Task<ProfilingReport> CompareAgainstBaselineAsync(
        ProfilingReport currentReport,
        string baselinePath,
        double regressionThreshold = 0.20);
}
