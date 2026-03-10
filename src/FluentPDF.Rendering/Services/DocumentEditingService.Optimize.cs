using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

public sealed partial class DocumentEditingService
{
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
}
