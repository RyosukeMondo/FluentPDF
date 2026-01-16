using System.Diagnostics;
using FluentPDF.Core.Models;

namespace FluentPDF.App.Services;

/// <summary>
/// Monitors memory usage and detects resource leaks during PDF rendering operations.
/// Provides lightweight memory diagnostics without significant performance overhead.
/// </summary>
public sealed class MemoryMonitor
{
    private const long AbnormalMemoryDeltaBytes = 100 * 1024 * 1024; // 100 MB
    private const int AbnormalHandleCountDelta = 1000;

    /// <summary>
    /// Captures a snapshot of current memory usage and resource handles.
    /// </summary>
    /// <param name="label">Human-readable label for this snapshot (e.g., "BeforeRender", "AfterRender").</param>
    /// <returns>A MemorySnapshot containing current memory metrics.</returns>
    /// <remarks>
    /// This method is designed to be lightweight (completes in under 10ms).
    /// It captures working set, private memory, managed heap size, and handle count.
    /// </remarks>
    public MemorySnapshot CaptureSnapshot(string label)
    {
        try
        {
            using var process = Process.GetCurrentProcess();

            return new MemorySnapshot(
                Label: label,
                WorkingSetBytes: process.WorkingSet64,
                PrivateMemoryBytes: process.PrivateMemorySize64,
                ManagedMemoryBytes: GC.GetTotalMemory(forceFullCollection: false),
                HandleCount: process.HandleCount,
                Timestamp: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            // If we can't capture metrics, return a zero snapshot rather than failing
            // Log the exception if logger is available
            return new MemorySnapshot(
                Label: $"{label} (Error: {ex.Message})",
                WorkingSetBytes: 0,
                PrivateMemoryBytes: 0,
                ManagedMemoryBytes: 0,
                HandleCount: 0,
                Timestamp: DateTime.UtcNow
            );
        }
    }

    /// <summary>
    /// Calculates the difference between two memory snapshots and determines if growth is abnormal.
    /// </summary>
    /// <param name="before">The baseline snapshot taken before an operation.</param>
    /// <param name="after">The comparison snapshot taken after an operation.</param>
    /// <returns>A MemoryDelta containing the differences and abnormality flag.</returns>
    /// <remarks>
    /// Abnormal thresholds:
    /// - Memory delta > 100MB for a single operation
    /// - Handle count delta > 1000 handles for a single operation
    /// </remarks>
    public MemoryDelta CalculateDelta(MemorySnapshot before, MemorySnapshot after)
    {
        var workingSetDelta = after.WorkingSetBytes - before.WorkingSetBytes;
        var privateMemoryDelta = after.PrivateMemoryBytes - before.PrivateMemoryBytes;
        var managedMemoryDelta = after.ManagedMemoryBytes - before.ManagedMemoryBytes;
        var handleCountDelta = after.HandleCount - before.HandleCount;

        // Check if any metric exceeds abnormal thresholds
        var isAbnormal = Math.Abs(workingSetDelta) > AbnormalMemoryDeltaBytes ||
                        Math.Abs(privateMemoryDelta) > AbnormalMemoryDeltaBytes ||
                        Math.Abs(managedMemoryDelta) > AbnormalMemoryDeltaBytes ||
                        Math.Abs(handleCountDelta) > AbnormalHandleCountDelta;

        return new MemoryDelta(
            Before: before,
            After: after,
            WorkingSetDelta: workingSetDelta,
            PrivateMemoryDelta: privateMemoryDelta,
            ManagedMemoryDelta: managedMemoryDelta,
            HandleCountDelta: handleCountDelta,
            IsAbnormal: isAbnormal
        );
    }

    /// <summary>
    /// Detects potential SafeHandle leaks by examining current handle count and memory state.
    /// This is a basic implementation that logs current metrics rather than tracking individual handles.
    /// </summary>
    /// <returns>A list of detected leaks (currently returns basic diagnostic info).</returns>
    /// <remarks>
    /// Full SafeHandle leak detection requires invasive instrumentation or profiling tools.
    /// This basic implementation captures current state for diagnostic purposes.
    /// For detailed leak analysis, use tools like dotMemory or PerfView.
    /// </remarks>
    public Task<List<SafeHandleLeak>> DetectSafeHandleLeaksAsync()
    {
        // Note: Comprehensive SafeHandle leak detection requires invasive tracking
        // or profiling tools. This basic implementation just captures current metrics.
        // In a production system, you would:
        // 1. Track SafeHandle allocations with WeakReference
        // 2. Monitor finalization queue
        // 3. Use ETW events for handle tracking

        var leaks = new List<SafeHandleLeak>();

        try
        {
            using var process = Process.GetCurrentProcess();
            var handleCount = process.HandleCount;

            // If handle count is suspiciously high, log a diagnostic entry
            // This is a heuristic - adjust threshold based on your application
            if (handleCount > 10000)
            {
                leaks.Add(new SafeHandleLeak(
                    HandleType: "Unknown (high handle count detected)",
                    HandleValue: IntPtr.Zero,
                    CreatedAt: DateTime.UtcNow,
                    AllocationStackTrace: $"Current handle count: {handleCount}. Consider using profiling tools for detailed analysis.",
                    IsDisposed: false
                ));
            }
        }
        catch
        {
            // If we can't detect leaks, return empty list
        }

        return Task.FromResult(leaks);
    }

    /// <summary>
    /// Forces garbage collection and waits for finalizers.
    /// Use sparingly as this can impact performance.
    /// </summary>
    /// <remarks>
    /// This method is useful when detecting abnormal memory growth to determine
    /// if the growth is due to pending garbage collection or actual leaks.
    /// </remarks>
    public void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
