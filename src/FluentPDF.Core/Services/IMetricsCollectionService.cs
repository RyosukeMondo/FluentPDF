using FluentPDF.Core.Observability;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Contract for performance metrics collection and reporting.
/// </summary>
public interface IMetricsCollectionService
{
    /// <summary>
    /// Records the current frames per second.
    /// </summary>
    /// <param name="fps">The frames per second value.</param>
    void RecordFPS(double fps);

    /// <summary>
    /// Records the render time for a specific page.
    /// </summary>
    /// <param name="pageNumber">The page number that was rendered.</param>
    /// <param name="milliseconds">The render time in milliseconds.</param>
    void RecordRenderTime(int pageNumber, double milliseconds);

    /// <summary>
    /// Records the current memory usage.
    /// </summary>
    /// <param name="managedMB">Managed memory usage in megabytes.</param>
    /// <param name="nativeMB">Native memory usage in megabytes.</param>
    void RecordMemoryUsage(long managedMB, long nativeMB);

    /// <summary>
    /// Gets the current performance metrics snapshot.
    /// </summary>
    /// <returns>The current performance metrics.</returns>
    PerformanceMetrics GetCurrentMetrics();

    /// <summary>
    /// Gets the metrics history within the specified time duration.
    /// </summary>
    /// <param name="duration">The time duration to look back.</param>
    /// <returns>A read-only list of performance metrics within the time window.</returns>
    IReadOnlyList<PerformanceMetrics> GetMetricsHistory(TimeSpan duration);

    /// <summary>
    /// Exports metrics to a file in the specified format.
    /// </summary>
    /// <param name="filePath">The file path to export to.</param>
    /// <param name="format">The export format (JSON or CSV).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExportMetricsAsync(string filePath, ExportFormat format);
}
