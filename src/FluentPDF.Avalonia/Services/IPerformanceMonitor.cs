using System;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Represents real-time performance metrics for the application.
/// </summary>
public sealed class PerformanceMetrics
{
    /// <summary>
    /// Gets the current frames per second (FPS).
    /// </summary>
    public double CurrentFps { get; init; }

    /// <summary>
    /// Gets the managed memory usage in megabytes.
    /// </summary>
    public double ManagedMemoryMb { get; init; }

    /// <summary>
    /// Gets the native memory usage in megabytes.
    /// </summary>
    public double NativeMemoryMb { get; init; }

    /// <summary>
    /// Gets the total memory usage in megabytes (managed + native).
    /// </summary>
    public double TotalMemoryMb => ManagedMemoryMb + NativeMemoryMb;

    /// <summary>
    /// Gets the average frame time in milliseconds.
    /// </summary>
    public double AverageFrameTimeMs => CurrentFps > 0 ? 1000.0 / CurrentFps : 0;

    /// <summary>
    /// Gets the timestamp when the metrics were captured.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets a value indicating whether the performance is considered good (greater than 60 FPS).
    /// </summary>
    public bool IsPerformanceGood => CurrentFps >= 60;

    /// <summary>
    /// Gets a value indicating whether the performance is degraded (30-60 FPS).
    /// </summary>
    public bool IsPerformanceDegraded => CurrentFps >= 30 && CurrentFps < 60;

    /// <summary>
    /// Gets a value indicating whether the performance is poor (less than 30 FPS).
    /// </summary>
    public bool IsPerformancePoor => CurrentFps < 30;
}

/// <summary>
/// Monitors real-time application performance including FPS and memory usage.
/// Provides an observable stream of performance metrics for reactive monitoring.
/// </summary>
public interface IPerformanceMonitor : IDisposable
{
    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    double CurrentFps { get; }

    /// <summary>
    /// Gets the current memory usage in megabytes (managed + native).
    /// </summary>
    double MemoryUsageMb { get; }

    /// <summary>
    /// Gets the latest performance metrics.
    /// </summary>
    PerformanceMetrics CurrentMetrics { get; }

    /// <summary>
    /// Gets an observable stream of performance metrics.
    /// Emits metrics every 5 seconds for monitoring and logging.
    /// </summary>
    IObservable<PerformanceMetrics> MetricsStream { get; }

    /// <summary>
    /// Starts performance monitoring.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops performance monitoring.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets a value indicating whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }
}
