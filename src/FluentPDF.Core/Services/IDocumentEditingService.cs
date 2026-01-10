using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF document structure manipulation operations.
/// Provides methods for merging, splitting, and optimizing PDF documents using lossless operations.
/// All operations return Result&lt;T&gt; for consistent error handling and support progress reporting and cancellation.
/// </summary>
public interface IDocumentEditingService
{
    /// <summary>
    /// Merges multiple PDF documents into a single output file.
    /// All source documents are combined in the order provided, preserving page dimensions and quality.
    /// </summary>
    /// <param name="sourcePaths">Paths to PDF files to merge (minimum 2 files required).</param>
    /// <param name="outputPath">Full path where the merged PDF will be saved.</param>
    /// <param name="progress">Optional progress reporter for operations (reports percentage 0-100).</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result containing the output file path if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_FILE_NOT_FOUND, PDF_INVALID_FORMAT, PDF_CORRUPTED, PDF_REQUIRES_PASSWORD,
    /// PDF_MERGE_FAILED, PDF_INSUFFICIENT_DISK_SPACE.
    /// </returns>
    Task<Result<string>> MergeAsync(
        IEnumerable<string> sourcePaths,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Splits a PDF document by extracting specified page ranges into a new document.
    /// Page ranges are specified using comma-separated notation (e.g., "1-5, 10, 15-20").
    /// </summary>
    /// <param name="sourcePath">Path to the PDF file to split.</param>
    /// <param name="pageRanges">Page range specification (e.g., "1-5, 10, 15-20"). Pages are 1-based.</param>
    /// <param name="outputPath">Full path where the split PDF will be saved.</param>
    /// <param name="progress">Optional progress reporter for operations (reports percentage 0-100).</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result containing the output file path if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_FILE_NOT_FOUND, PDF_INVALID_FORMAT, PDF_PAGE_INVALID, PDF_SPLIT_FAILED,
    /// PDF_VALIDATION_FAILED (for invalid page range syntax).
    /// </returns>
    Task<Result<string>> SplitAsync(
        string sourcePath,
        string pageRanges,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes a PDF document to reduce file size through lossless compression and structure optimization.
    /// Does NOT recompress images or fonts. Optionally linearizes for fast web viewing.
    /// </summary>
    /// <param name="sourcePath">Path to the PDF file to optimize.</param>
    /// <param name="outputPath">Full path where the optimized PDF will be saved.</param>
    /// <param name="options">Optimization settings (compression, linearization, etc.).</param>
    /// <param name="progress">Optional progress reporter for operations (reports percentage 0-100).</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A Result containing OptimizationResult with metrics if successful, or a PdfError if the operation failed.
    /// Error codes: PDF_FILE_NOT_FOUND, PDF_INVALID_FORMAT, PDF_OPTIMIZE_FAILED,
    /// PDF_SIZE_INCREASED (warning if optimization increased file size).
    /// </returns>
    Task<Result<OptimizationResult>> OptimizeAsync(
        string sourcePath,
        string outputPath,
        OptimizationOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Configuration options for PDF optimization operations.
/// </summary>
public record OptimizationOptions
{
    /// <summary>
    /// Compress content streams using lossless compression. Default: true.
    /// </summary>
    public bool CompressStreams { get; init; } = true;

    /// <summary>
    /// Remove unused objects from the PDF structure. Default: true.
    /// </summary>
    public bool RemoveUnusedObjects { get; init; } = true;

    /// <summary>
    /// Deduplicate duplicate resources (fonts, images, etc.). Default: true.
    /// </summary>
    public bool DeduplicateResources { get; init; } = true;

    /// <summary>
    /// Linearize the PDF for fast web viewing (byte-serving). Default: false.
    /// Linearization reorganizes the PDF structure so the first page can be displayed before the entire file downloads.
    /// </summary>
    public bool Linearize { get; init; } = false;

    /// <summary>
    /// Preserve encryption settings from the source document. Default: true.
    /// If false, encryption will be removed from the output.
    /// </summary>
    public bool PreserveEncryption { get; init; } = true;
}

/// <summary>
/// Results and metrics from a PDF optimization operation.
/// </summary>
public record OptimizationResult
{
    /// <summary>
    /// Path to the optimized output file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Original file size in bytes.
    /// </summary>
    public required long OriginalSize { get; init; }

    /// <summary>
    /// Optimized file size in bytes.
    /// </summary>
    public required long OptimizedSize { get; init; }

    /// <summary>
    /// Percentage reduction in file size. Negative if file size increased.
    /// Calculated as: 100.0 * (1.0 - OptimizedSize / OriginalSize)
    /// </summary>
    public double ReductionPercentage => 100.0 * (1.0 - (double)OptimizedSize / OriginalSize);

    /// <summary>
    /// Indicates whether the output was linearized for fast web viewing.
    /// </summary>
    public required bool WasLinearized { get; init; }

    /// <summary>
    /// Total time taken to perform the optimization.
    /// </summary>
    public required TimeSpan ProcessingTime { get; init; }
}
