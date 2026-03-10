using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

public sealed partial class DocumentEditingService
{
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

    private static string BuildQpdfPageRangeString(List<Core.Models.PageRange> ranges)
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
}
