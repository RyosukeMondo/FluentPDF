namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Contains memory allocation and garbage collection metrics for a profiled P/Invoke function.
/// </summary>
public record MemoryMetrics
{
    /// <summary>
    /// Gets the total managed memory allocated during profiling (in bytes).
    /// Measured using GC.GetTotalMemory before and after execution.
    /// </summary>
    public long ManagedMemoryBytes { get; init; }

    /// <summary>
    /// Gets the estimated unmanaged memory allocated (in bytes).
    /// This is approximate and based on known allocation patterns for PDFium operations.
    /// </summary>
    public long UnmanagedMemoryBytes { get; init; }

    /// <summary>
    /// Gets the number of generation 0 garbage collections triggered during profiling.
    /// </summary>
    public int Gen0Collections { get; init; }

    /// <summary>
    /// Gets the number of generation 1 garbage collections triggered during profiling.
    /// </summary>
    public int Gen1Collections { get; init; }

    /// <summary>
    /// Gets the number of generation 2 garbage collections triggered during profiling.
    /// </summary>
    public int Gen2Collections { get; init; }

    /// <summary>
    /// Gets the total number of garbage collections across all generations.
    /// </summary>
    public int TotalCollections => Gen0Collections + Gen1Collections + Gen2Collections;

    /// <summary>
    /// Gets the average managed memory allocated per iteration (in bytes).
    /// </summary>
    public long AverageManagedMemoryPerIteration { get; init; }

    /// <summary>
    /// Gets a value indicating whether this function caused significant garbage collection pressure.
    /// True if Gen2 collections occurred, indicating large or long-lived allocations.
    /// </summary>
    public bool HasGarbageCollectionPressure => Gen2Collections > 0;
}
