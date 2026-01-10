using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for PDF document structure manipulation operations using QPDF.
/// Provides merging, splitting, and optimization with progress reporting and cancellation support.
/// </summary>
public sealed class DocumentEditingService : IDocumentEditingService
{
    private readonly ILogger<DocumentEditingService> _logger;
    private const int ProgressReportIntervalMs = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentEditingService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public DocumentEditingService(ILogger<DocumentEditingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure QPDF is initialized
        if (!QpdfNative.Initialize())
        {
            throw new InvalidOperationException("Failed to initialize QPDF library. Ensure qpdf library is available.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> MergeAsync(
        IEnumerable<string> sourcePaths,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();
        var sourceList = sourcePaths?.ToList() ?? new List<string>();

        _logger.LogInformation(
            "Starting PDF merge operation. CorrelationId={CorrelationId}, SourceCount={SourceCount}, OutputPath={OutputPath}",
            correlationId, sourceList.Count, outputPath);

        // Validate inputs
        var validationResult = ValidateMergeInputs(sourceList, outputPath, correlationId);
        if (validationResult.IsFailed)
        {
            return validationResult;
        }

        return await Task.Run(async () =>
        {
            SafeQpdfJobHandle? targetJob = null;
            var sourceJobs = new List<SafeQpdfJobHandle>();

            try
            {
                // Report initial progress
                progress?.Report(0);

                // Create target job and load first document
                targetJob = QpdfNative.CreateJob();
                if (targetJob.IsInvalid)
                {
                    return CreateError(
                        "PDF_MERGE_FAILED",
                        "Failed to create QPDF job for merge operation.",
                        ErrorCategory.System,
                        correlationId);
                }

                // Load first document as base
                _logger.LogDebug(
                    "Loading base document. CorrelationId={CorrelationId}, FilePath={FilePath}",
                    correlationId, sourceList[0]);

                var readResult = QpdfNative.ReadDocument(targetJob, sourceList[0]);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, readResult, sourceList[0], correlationId, "read base document");
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                progress?.Report(10);

                // Load and merge remaining documents
                var progressPerDocument = 80.0 / sourceList.Count;
                for (int i = 1; i < sourceList.Count; i++)
                {
                    var sourcePath = sourceList[i];

                    _logger.LogDebug(
                        "Merging document {Index}/{Total}. CorrelationId={CorrelationId}, FilePath={FilePath}",
                        i + 1, sourceList.Count, correlationId, sourcePath);

                    // Create job for source document
                    var sourceJob = QpdfNative.CreateJob();
                    if (sourceJob.IsInvalid)
                    {
                        return CreateError(
                            "PDF_MERGE_FAILED",
                            $"Failed to create QPDF job for source document: {sourcePath}",
                            ErrorCategory.System,
                            correlationId,
                            ("SourcePath", sourcePath),
                            ("DocumentIndex", i));
                    }
                    sourceJobs.Add(sourceJob);

                    // Load source document
                    readResult = QpdfNative.ReadDocument(sourceJob, sourcePath);
                    if (readResult != QpdfNative.ErrorCodes.Success)
                    {
                        return HandleQpdfError(sourceJob, readResult, sourcePath, correlationId, "read source document");
                    }

                    // Add pages from source to target
                    var addResult = QpdfNative.AddPages(targetJob, sourceJob, null);
                    if (addResult != QpdfNative.ErrorCodes.Success)
                    {
                        return HandleQpdfError(targetJob, addResult, sourcePath, correlationId, "add pages");
                    }

                    // Check for cancellation
                    if (ct.IsCancellationRequested)
                    {
                        return CreateCancellationError(correlationId);
                    }

                    // Report progress with delay to avoid excessive updates
                    var currentProgress = 10 + (progressPerDocument * (i + 1));
                    progress?.Report(currentProgress);
                    await Task.Delay(Math.Min(ProgressReportIntervalMs, 50), ct);
                }

                // Write merged document
                _logger.LogInformation(
                    "Writing merged document. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);

                progress?.Report(90);

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var writeResult = QpdfNative.WriteDocument(targetJob, outputPath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, writeResult, outputPath, correlationId, "write merged document");
                }

                // Verify output file exists
                if (!File.Exists(outputPath))
                {
                    return CreateError(
                        "PDF_MERGE_FAILED",
                        "Merge operation completed but output file was not created.",
                        ErrorCategory.IO,
                        correlationId,
                        ("OutputPath", outputPath));
                }

                var outputSize = new FileInfo(outputPath).Length;

                progress?.Report(100);

                _logger.LogInformation(
                    "PDF merge completed successfully. CorrelationId={CorrelationId}, OutputPath={OutputPath}, OutputSize={OutputSize}",
                    correlationId, outputPath, outputSize);

                return Result.Ok(outputPath);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "PDF merge operation cancelled. CorrelationId={CorrelationId}",
                    correlationId);

                // Clean up output file if it was partially created
                if (File.Exists(outputPath))
                {
                    try
                    {
                        File.Delete(outputPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to clean up partial output file. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                            correlationId, outputPath);
                    }
                }

                return CreateCancellationError(correlationId);
            }
            catch (Exception ex)
            {
                return CreateError(
                    "PDF_MERGE_FAILED",
                    $"Unexpected error during merge operation: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name),
                    ("OutputPath", outputPath));
            }
            finally
            {
                // Clean up QPDF job handles
                targetJob?.Dispose();
                foreach (var sourceJob in sourceJobs)
                {
                    sourceJob?.Dispose();
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<string>> SplitAsync(
        string sourcePath,
        string pageRanges,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting PDF split operation. CorrelationId={CorrelationId}, SourcePath={SourcePath}, PageRanges={PageRanges}, OutputPath={OutputPath}",
            correlationId, sourcePath, pageRanges, outputPath);

        // Validate inputs
        var validationResult = ValidateSplitInputs(sourcePath, pageRanges, outputPath, correlationId);
        if (validationResult.IsFailed)
        {
            return validationResult;
        }

        // Parse page ranges
        var parseResult = Core.Utilities.PageRangeParser.Parse(pageRanges);
        if (parseResult.IsFailed)
        {
            _logger.LogError(
                "Failed to parse page ranges. CorrelationId={CorrelationId}, PageRanges={PageRanges}",
                correlationId, pageRanges);
            return Result.Fail(parseResult.Errors);
        }

        var ranges = parseResult.Value;

        return await Task.Run(() =>
        {
            SafeQpdfJobHandle? sourceJob = null;
            SafeQpdfJobHandle? targetJob = null;

            try
            {
                // Report initial progress
                progress?.Report(0);

                // Load source document
                sourceJob = QpdfNative.CreateJob();
                if (sourceJob.IsInvalid)
                {
                    return CreateError(
                        "PDF_SPLIT_FAILED",
                        "Failed to create QPDF job for split operation.",
                        ErrorCategory.System,
                        correlationId);
                }

                _logger.LogDebug(
                    "Loading source document. CorrelationId={CorrelationId}, FilePath={FilePath}",
                    correlationId, sourcePath);

                var readResult = QpdfNative.ReadDocument(sourceJob, sourcePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(sourceJob, readResult, sourcePath, correlationId, "read source document");
                }

                // Get total page count for validation
                var totalPages = QpdfNative.GetPageCount(sourceJob);
                _logger.LogDebug(
                    "Source document loaded. CorrelationId={CorrelationId}, TotalPages={TotalPages}",
                    correlationId, totalPages);

                // Validate page ranges against document
                var rangeValidationResult = ValidatePageRangesAgainstDocument(ranges, totalPages, correlationId);
                if (rangeValidationResult.IsFailed)
                {
                    return rangeValidationResult;
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                progress?.Report(30);

                // Build QPDF page range string (format: "1-5,10,15-20")
                var qpdfPageRange = BuildQpdfPageRangeString(ranges);

                _logger.LogDebug(
                    "Extracting pages. CorrelationId={CorrelationId}, QpdfPageRange={QpdfPageRange}",
                    correlationId, qpdfPageRange);

                // Create target document - we'll load source then use page selection on write
                // QPDF's C API approach: load document, then write with page selection
                targetJob = QpdfNative.CreateJob();
                if (targetJob.IsInvalid)
                {
                    return CreateError(
                        "PDF_SPLIT_FAILED",
                        "Failed to create QPDF job for target document.",
                        ErrorCategory.System,
                        correlationId);
                }

                // Load source into target job (we'll select pages during write)
                readResult = QpdfNative.ReadDocument(targetJob, sourcePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, readResult, sourcePath, correlationId, "load source for split");
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                progress?.Report(60);

                // Note: QPDF C API requires a workaround for page selection
                // We need to create a new job and add only selected pages
                var outputJob = QpdfNative.CreateJob();
                if (outputJob.IsInvalid)
                {
                    return CreateError(
                        "PDF_SPLIT_FAILED",
                        "Failed to create output job.",
                        ErrorCategory.System,
                        correlationId);
                }

                // Create minimal empty PDF (we'll add pages to it)
                // QPDF requires starting with a valid document
                readResult = QpdfNative.ReadDocument(outputJob, sourcePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    outputJob.Dispose();
                    return HandleQpdfError(outputJob, readResult, sourcePath, correlationId, "create output base");
                }

                // Use AddPages to copy only the selected pages
                // AddPages with page range specification
                var addResult = QpdfNative.AddPages(outputJob, targetJob, qpdfPageRange);
                if (addResult != QpdfNative.ErrorCodes.Success)
                {
                    outputJob.Dispose();
                    return HandleQpdfError(outputJob, addResult, sourcePath, correlationId, "add selected pages");
                }

                // Replace targetJob with outputJob for writing
                targetJob.Dispose();
                targetJob = outputJob;

                progress?.Report(75);

                // Write output document
                _logger.LogInformation(
                    "Writing split document. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var writeResult = QpdfNative.WriteDocument(targetJob, outputPath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, writeResult, outputPath, correlationId, "write split document");
                }

                // Verify output file exists
                if (!File.Exists(outputPath))
                {
                    return CreateError(
                        "PDF_SPLIT_FAILED",
                        "Split operation completed but output file was not created.",
                        ErrorCategory.IO,
                        correlationId,
                        ("OutputPath", outputPath));
                }

                var outputSize = new FileInfo(outputPath).Length;

                progress?.Report(100);

                _logger.LogInformation(
                    "PDF split completed successfully. CorrelationId={CorrelationId}, OutputPath={OutputPath}, OutputSize={OutputSize}, PagesExtracted={PagesExtracted}",
                    correlationId, outputPath, outputSize, ranges.Sum(r => r.PageCount));

                return Result.Ok(outputPath);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "PDF split operation cancelled. CorrelationId={CorrelationId}",
                    correlationId);

                // Clean up output file if it was partially created
                if (File.Exists(outputPath))
                {
                    try
                    {
                        File.Delete(outputPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to clean up partial output file. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                            correlationId, outputPath);
                    }
                }

                return CreateCancellationError(correlationId);
            }
            catch (Exception ex)
            {
                return CreateError(
                    "PDF_SPLIT_FAILED",
                    $"Unexpected error during split operation: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name),
                    ("OutputPath", outputPath));
            }
            finally
            {
                // Clean up QPDF job handles
                sourceJob?.Dispose();
                targetJob?.Dispose();
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<OptimizationResult>> OptimizeAsync(
        string sourcePath,
        string outputPath,
        OptimizationOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting PDF optimization operation. CorrelationId={CorrelationId}, SourcePath={SourcePath}, OutputPath={OutputPath}",
            correlationId, sourcePath, outputPath);

        // Validate inputs
        var validationResult = ValidateOptimizeInputs(sourcePath, outputPath, options, correlationId);
        if (validationResult.IsFailed)
        {
            return Result.Fail(validationResult.Errors);
        }

        // Get original file size
        long originalSize = new FileInfo(sourcePath).Length;

        return await Task.Run(() =>
        {
            SafeQpdfJobHandle? job = null;

            try
            {
                // Report initial progress
                progress?.Report(0);

                // Load source document
                job = QpdfNative.CreateJob();
                if (job.IsInvalid)
                {
                    return Result.Fail<OptimizationResult>(CreateError(
                        "PDF_OPTIMIZE_FAILED",
                        "Failed to create QPDF job for optimization operation.",
                        ErrorCategory.System,
                        correlationId).Errors);
                }

                _logger.LogDebug(
                    "Loading source document for optimization. CorrelationId={CorrelationId}, FilePath={FilePath}",
                    correlationId, sourcePath);

                var readResult = QpdfNative.ReadDocument(job, sourcePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return Result.Fail<OptimizationResult>(HandleQpdfError(job, readResult, sourcePath, correlationId, "read source document").Errors);
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return Result.Fail<OptimizationResult>(CreateCancellationError(correlationId).Errors);
                }

                progress?.Report(20);

                // Apply optimization settings
                _logger.LogDebug(
                    "Applying optimization settings. CorrelationId={CorrelationId}, CompressStreams={CompressStreams}, RemoveUnusedObjects={RemoveUnusedObjects}, DeduplicateResources={DeduplicateResources}, Linearize={Linearize}",
                    correlationId, options.CompressStreams, options.RemoveUnusedObjects, options.DeduplicateResources, options.Linearize);

                // Set stream compression
                if (options.CompressStreams)
                {
                    QpdfNative.SetCompressStreams(job, true);
                }

                // Set object removal (inverse of preserve)
                if (options.RemoveUnusedObjects)
                {
                    QpdfNative.SetPreserveUnreferencedObjects(job, false);
                }

                // Set object stream mode for deduplication
                if (options.DeduplicateResources)
                {
                    // Generate object streams to enable deduplication
                    QpdfNative.SetObjectStreamMode(job, QpdfNative.ObjectStreamMode.Generate);
                }

                // Set linearization
                if (options.Linearize)
                {
                    QpdfNative.SetLinearization(job, true);
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return Result.Fail<OptimizationResult>(CreateCancellationError(correlationId).Errors);
                }

                progress?.Report(50);

                // Write optimized document
                _logger.LogInformation(
                    "Writing optimized document. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var writeResult = QpdfNative.WriteDocument(job, outputPath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return Result.Fail<OptimizationResult>(HandleQpdfError(job, writeResult, outputPath, correlationId, "write optimized document").Errors);
                }

                // Verify output file exists
                if (!File.Exists(outputPath))
                {
                    return Result.Fail<OptimizationResult>(CreateError(
                        "PDF_OPTIMIZE_FAILED",
                        "Optimization operation completed but output file was not created.",
                        ErrorCategory.IO,
                        correlationId,
                        ("OutputPath", outputPath)).Errors);
                }

                progress?.Report(90);

                // Get optimized file size and calculate metrics
                var optimizedSize = new FileInfo(outputPath).Length;
                var processingTime = DateTime.UtcNow - startTime;

                var result = new OptimizationResult
                {
                    OutputPath = outputPath,
                    OriginalSize = originalSize,
                    OptimizedSize = optimizedSize,
                    WasLinearized = options.Linearize,
                    ProcessingTime = processingTime
                };

                // Warn if optimization increased file size
                if (result.ReductionPercentage < 0)
                {
                    _logger.LogWarning(
                        "PDF optimization increased file size. CorrelationId={CorrelationId}, OriginalSize={OriginalSize}, OptimizedSize={OptimizedSize}, Increase={Increase}%",
                        correlationId, originalSize, optimizedSize, Math.Abs(result.ReductionPercentage));
                }

                progress?.Report(100);

                _logger.LogInformation(
                    "PDF optimization completed successfully. CorrelationId={CorrelationId}, OutputPath={OutputPath}, OriginalSize={OriginalSize}, OptimizedSize={OptimizedSize}, Reduction={Reduction}%, ProcessingTime={ProcessingTime}ms",
                    correlationId, outputPath, originalSize, optimizedSize, result.ReductionPercentage, processingTime.TotalMilliseconds);

                return Result.Ok(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "PDF optimization operation cancelled. CorrelationId={CorrelationId}",
                    correlationId);

                // Clean up output file if it was partially created
                if (File.Exists(outputPath))
                {
                    try
                    {
                        File.Delete(outputPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to clean up partial output file. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                            correlationId, outputPath);
                    }
                }

                return Result.Fail<OptimizationResult>(CreateCancellationError(correlationId).Errors);
            }
            catch (Exception ex)
            {
                return Result.Fail<OptimizationResult>(CreateError(
                    "PDF_OPTIMIZE_FAILED",
                    $"Unexpected error during optimization operation: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name),
                    ("OutputPath", outputPath)).Errors);
            }
            finally
            {
                // Clean up QPDF job handle
                job?.Dispose();
            }
        }, ct);
    }

    #region Helper Methods

    private Result<string> ValidateMergeInputs(
        List<string> sourcePaths,
        string outputPath,
        Guid correlationId)
    {
        // Validate source paths count
        if (sourcePaths == null || sourcePaths.Count < 2)
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "At least 2 source PDF files are required for merge operation.",
                ErrorCategory.Validation,
                correlationId,
                ("SourceCount", sourcePaths?.Count ?? 0));
        }

        // Validate output path
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Output path cannot be null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        // Validate all source files exist
        for (int i = 0; i < sourcePaths.Count; i++)
        {
            var sourcePath = sourcePaths[i];

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return CreateError(
                    "PDF_VALIDATION_FAILED",
                    $"Source path at index {i} is null or empty.",
                    ErrorCategory.Validation,
                    correlationId,
                    ("SourceIndex", i));
            }

            if (!File.Exists(sourcePath))
            {
                return CreateError(
                    "PDF_FILE_NOT_FOUND",
                    $"Source PDF file not found: {sourcePath}",
                    ErrorCategory.IO,
                    correlationId,
                    ("FilePath", sourcePath),
                    ("SourceIndex", i));
            }
        }

        return Result.Ok(outputPath);
    }

    private Result<string> HandleQpdfError(
        SafeQpdfJobHandle job,
        int errorCode,
        string filePath,
        Guid correlationId,
        string operation)
    {
        var qpdfErrorMessage = QpdfNative.GetErrorMessage(job);
        var errorCodeDescription = QpdfNative.TranslateErrorCode(errorCode);

        var (code, category) = errorCode switch
        {
            QpdfNative.ErrorCodes.FileNotFound => ("PDF_FILE_NOT_FOUND", ErrorCategory.IO),
            QpdfNative.ErrorCodes.InvalidPassword => ("PDF_REQUIRES_PASSWORD", ErrorCategory.Security),
            QpdfNative.ErrorCodes.DamagedPdf => ("PDF_CORRUPTED", ErrorCategory.Validation),
            QpdfNative.ErrorCodes.OutOfMemory => ("PDF_OUT_OF_MEMORY", ErrorCategory.System),
            _ => ("PDF_MERGE_FAILED", ErrorCategory.System)
        };

        var message = string.IsNullOrEmpty(qpdfErrorMessage)
            ? $"Failed to {operation}: {errorCodeDescription}"
            : $"Failed to {operation}: {qpdfErrorMessage}";

        _logger.LogError(
            "QPDF operation failed. CorrelationId={CorrelationId}, Operation={Operation}, ErrorCode={ErrorCode}, QpdfError={QpdfError}",
            correlationId, operation, code, errorCode);

        return CreateError(
            code,
            message,
            category,
            correlationId,
            ("FilePath", filePath),
            ("QpdfErrorCode", errorCode),
            ("Operation", operation));
    }

    private Result<string> CreateError(
        string errorCode,
        string message,
        ErrorCategory category,
        Guid correlationId,
        params (string Key, object Value)[] contexts)
    {
        var error = new PdfError(errorCode, message, category, ErrorSeverity.Error)
            .WithContext("CorrelationId", correlationId);

        foreach (var (key, value) in contexts)
        {
            error.WithContext(key, value);
        }

        _logger.LogError(
            "PDF operation failed. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}, Message={Message}",
            correlationId, errorCode, message);

        return Result.Fail(error);
    }

    private Result<string> CreateCancellationError(Guid correlationId)
    {
        return CreateError(
            "PDF_OPERATION_CANCELLED",
            "PDF operation was cancelled by user request.",
            ErrorCategory.Validation,
            correlationId);
    }

    private Result<string> ValidateSplitInputs(
        string sourcePath,
        string pageRanges,
        string outputPath,
        Guid correlationId)
    {
        // Validate source path
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Source path cannot be null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        if (!File.Exists(sourcePath))
        {
            return CreateError(
                "PDF_FILE_NOT_FOUND",
                $"Source PDF file not found: {sourcePath}",
                ErrorCategory.IO,
                correlationId,
                ("FilePath", sourcePath));
        }

        // Validate page ranges
        if (string.IsNullOrWhiteSpace(pageRanges))
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Page ranges cannot be null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        // Validate output path
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Output path cannot be null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        return Result.Ok(outputPath);
    }

    private Result<string> ValidatePageRangesAgainstDocument(
        List<Core.Models.PageRange> ranges,
        int totalPages,
        Guid correlationId)
    {
        if (totalPages <= 0)
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Source document has no pages or page count could not be determined.",
                ErrorCategory.Validation,
                correlationId,
                ("TotalPages", totalPages));
        }

        foreach (var range in ranges)
        {
            if (range.StartPage > totalPages)
            {
                return CreateError(
                    "PDF_VALIDATION_PAGE_OUT_OF_RANGE",
                    $"Start page {range.StartPage} exceeds document page count ({totalPages}).",
                    ErrorCategory.Validation,
                    correlationId,
                    ("StartPage", range.StartPage),
                    ("TotalPages", totalPages));
            }

            if (range.EndPage > totalPages)
            {
                return CreateError(
                    "PDF_VALIDATION_PAGE_OUT_OF_RANGE",
                    $"End page {range.EndPage} exceeds document page count ({totalPages}).",
                    ErrorCategory.Validation,
                    correlationId,
                    ("EndPage", range.EndPage),
                    ("TotalPages", totalPages));
            }
        }

        return Result.Ok(string.Empty);
    }

    private string BuildQpdfPageRangeString(List<Core.Models.PageRange> ranges)
    {
        // QPDF expects page ranges in format: "1-5,10,15-20"
        var parts = ranges.Select(r =>
        {
            if (r.StartPage == r.EndPage)
            {
                return r.StartPage.ToString();
            }
            return $"{r.StartPage}-{r.EndPage}";
        });

        return string.Join(",", parts);
    }

    private Result<string> ValidateOptimizeInputs(
        string sourcePath,
        string outputPath,
        OptimizationOptions options,
        Guid correlationId)
    {
        // Validate source path
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Source path cannot be null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        if (!File.Exists(sourcePath))
        {
            return CreateError(
                "PDF_FILE_NOT_FOUND",
                $"Source PDF file not found: {sourcePath}",
                ErrorCategory.IO,
                correlationId,
                ("FilePath", sourcePath));
        }

        // Validate output path
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Output path cannot be null or empty.",
                ErrorCategory.Validation,
                correlationId);
        }

        // Validate options
        if (options == null)
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "Optimization options cannot be null.",
                ErrorCategory.Validation,
                correlationId);
        }

        return Result.Ok(outputPath);
    }

    #endregion
}
