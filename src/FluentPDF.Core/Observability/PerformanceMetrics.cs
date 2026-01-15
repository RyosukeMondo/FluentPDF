namespace FluentPDF.Core.Observability;

/// <summary>
/// Represents runtime performance metrics for the application.
/// </summary>
public sealed class PerformanceMetrics
{
    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    public required double CurrentFPS { get; init; }

    /// <summary>
    /// Gets the managed memory usage in megabytes.
    /// </summary>
    public required long ManagedMemoryMB { get; init; }

    /// <summary>
    /// Gets the native memory usage in megabytes.
    /// </summary>
    public required long NativeMemoryMB { get; init; }

    /// <summary>
    /// Gets the total memory usage (managed + native) in megabytes.
    /// </summary>
    public long TotalMemoryMB => ManagedMemoryMB + NativeMemoryMB;

    /// <summary>
    /// Gets the last render time in milliseconds.
    /// </summary>
    public required double LastRenderTimeMs { get; init; }

    /// <summary>
    /// Gets the current page number being rendered.
    /// </summary>
    public required int CurrentPageNumber { get; init; }

    /// <summary>
    /// Gets the timestamp when these metrics were captured.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the performance level based on FPS and memory usage.
    /// </summary>
    public required PerformanceLevel Level { get; init; }

    /// <summary>
    /// Calculates the performance level based on FPS and total memory usage.
    /// </summary>
    /// <param name="fps">The frames per second.</param>
    /// <param name="totalMemoryMB">The total memory usage in megabytes.</param>
    /// <returns>The calculated performance level.</returns>
    public static PerformanceLevel CalculateLevel(double fps, long totalMemoryMB)
    {
        if (fps < 15 || totalMemoryMB > 1000)
        {
            return PerformanceLevel.Critical;
        }

        if (fps < 30 || totalMemoryMB >= 500)
        {
            return PerformanceLevel.Warning;
        }

        return PerformanceLevel.Good;
    }
}

/// <summary>
/// Defines the performance levels based on FPS and memory thresholds.
/// </summary>
public enum PerformanceLevel
{
    /// <summary>
    /// Good performance: FPS greater than or equal to 30, Memory less than 500MB.
    /// </summary>
    Good,

    /// <summary>
    /// Warning performance: FPS 15-30, Memory 500-1000MB.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical performance: FPS less than 15, Memory greater than 1000MB.
    /// </summary>
    Critical
}
