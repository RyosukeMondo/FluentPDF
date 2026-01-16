namespace FluentPDF.Core.Models;

/// <summary>
/// Contains contextual information about a PDF rendering operation.
/// Used for logging, diagnostics, and strategy selection.
/// </summary>
/// <param name="DocumentPath">Full path to the PDF document being rendered.</param>
/// <param name="PageNumber">1-based page number being rendered.</param>
/// <param name="TotalPages">Total number of pages in the document.</param>
/// <param name="RenderDpi">DPI (dots per inch) used for rendering quality.</param>
/// <param name="RequestSource">Source of the render request (e.g., "MainViewer", "Thumbnail").</param>
/// <param name="RequestTime">Timestamp when the render was requested.</param>
/// <param name="OperationId">Unique identifier for tracking this operation through logs.</param>
public record RenderContext(
    string DocumentPath,
    int PageNumber,
    int TotalPages,
    double RenderDpi,
    string RequestSource,
    DateTime RequestTime,
    Guid OperationId
);

/// <summary>
/// Represents a snapshot of memory usage at a specific point in time.
/// Used for detecting memory leaks and abnormal memory growth during rendering.
/// </summary>
/// <param name="Label">Human-readable label describing when this snapshot was taken.</param>
/// <param name="WorkingSetBytes">Current working set (physical memory) in bytes.</param>
/// <param name="PrivateMemoryBytes">Current private memory size in bytes.</param>
/// <param name="ManagedMemoryBytes">Current managed heap size in bytes.</param>
/// <param name="HandleCount">Current number of OS handles held by process.</param>
/// <param name="Timestamp">When this snapshot was captured.</param>
public record MemorySnapshot(
    string Label,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long ManagedMemoryBytes,
    int HandleCount,
    DateTime Timestamp
);

/// <summary>
/// Represents the difference between two memory snapshots.
/// Used to detect abnormal memory growth that might indicate leaks.
/// </summary>
/// <param name="Before">The baseline memory snapshot.</param>
/// <param name="After">The comparison memory snapshot.</param>
/// <param name="WorkingSetDelta">Change in working set memory (bytes).</param>
/// <param name="PrivateMemoryDelta">Change in private memory (bytes).</param>
/// <param name="ManagedMemoryDelta">Change in managed heap memory (bytes).</param>
/// <param name="HandleCountDelta">Change in OS handle count.</param>
/// <param name="IsAbnormal">True if any delta exceeds abnormal thresholds (>100MB memory or >1000 handles for single page).</param>
public record MemoryDelta(
    MemorySnapshot Before,
    MemorySnapshot After,
    long WorkingSetDelta,
    long PrivateMemoryDelta,
    long ManagedMemoryDelta,
    int HandleCountDelta,
    bool IsAbnormal
);

/// <summary>
/// Represents a detected SafeHandle leak.
/// Tracks native resource handles that were allocated but not properly disposed.
/// </summary>
/// <param name="HandleType">Type name of the SafeHandle (e.g., "SafePdfDocumentHandle").</param>
/// <param name="HandleValue">Native handle value (pointer).</param>
/// <param name="CreatedAt">Timestamp when the handle was created (if available).</param>
/// <param name="AllocationStackTrace">Stack trace of where the handle was allocated (for debugging).</param>
/// <param name="IsDisposed">Whether the handle has been disposed.</param>
public record SafeHandleLeak(
    string HandleType,
    IntPtr HandleValue,
    DateTime CreatedAt,
    string AllocationStackTrace,
    bool IsDisposed
);

/// <summary>
/// Comprehensive diagnostic information about a completed rendering operation.
/// Used for logging, monitoring, and troubleshooting rendering issues.
/// </summary>
/// <param name="Context">The rendering context for this operation.</param>
/// <param name="StrategyUsed">Name of the rendering strategy that succeeded.</param>
/// <param name="StrategiesFailed">Names of strategies that failed before success (empty if first strategy worked).</param>
/// <param name="TotalDuration">Total time taken from request to completion.</param>
/// <param name="OutputSizeBytes">Size of the output image in bytes.</param>
/// <param name="MemoryImpact">Memory usage delta during this operation.</param>
/// <param name="UIBindingSucceeded">Whether UI binding verification passed.</param>
/// <param name="AdditionalMetrics">Additional metrics specific to the rendering strategy or operation.</param>
public record RenderingDiagnostics(
    RenderContext Context,
    string StrategyUsed,
    List<string> StrategiesFailed,
    TimeSpan TotalDuration,
    long OutputSizeBytes,
    MemoryDelta MemoryImpact,
    bool UIBindingSucceeded,
    Dictionary<string, object> AdditionalMetrics
);
