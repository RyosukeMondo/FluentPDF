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
    public Task<Result<string>> SplitAsync(
        string sourcePath,
        string pageRanges,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("SplitAsync will be implemented in task 6.");
    }

    /// <inheritdoc />
    public Task<Result<OptimizationResult>> OptimizeAsync(
        string sourcePath,
        string outputPath,
        OptimizationOptions options,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("OptimizeAsync will be implemented in task 7.");
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

    #endregion
}
