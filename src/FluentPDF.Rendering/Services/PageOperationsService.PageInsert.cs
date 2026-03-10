using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

public sealed partial class PageOperationsService
{
    /// <inheritdoc />
    public async Task<Result> ReorderPagesAsync(
        PdfDocument document,
        int[] pageIndices,
        int targetIndex,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting page reorder. CorrelationId={CorrelationId}, PageCount={PageCount}, TargetIndex={TargetIndex}",
            correlationId, pageIndices?.Length ?? 0, targetIndex);

        // Validate inputs
        var validationResult = ValidateDocument(document, correlationId);
        if (validationResult.IsFailed)
        {
            return validationResult;
        }

        if (pageIndices == null || pageIndices.Length == 0)
        {
            return CreateError(
                "PDF_VALIDATION_FAILED",
                "No pages specified for reordering.",
                ErrorCategory.Validation,
                correlationId);
        }

        return await Task.Run(() =>
        {
            SafeQpdfJobHandle? sourceJob = null;
            SafeQpdfJobHandle? targetJob = null;

            try
            {
                // Load source document
                sourceJob = QpdfNative.CreateJob();
                if (sourceJob.IsInvalid)
                {
                    return CreateError(
                        "PDF_REORDER_FAILED",
                        "Failed to create QPDF job for reorder operation.",
                        ErrorCategory.System,
                        correlationId);
                }

                var readResult = QpdfNative.ReadDocument(sourceJob, document.FilePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(sourceJob, readResult, document.FilePath, correlationId, "read document for reorder");
                }

                var totalPages = QpdfNative.GetPageCount(sourceJob);

                // Validate page indices and target
                if (targetIndex < 0 || targetIndex > totalPages)
                {
                    return CreateError(
                        "PDF_PAGE_INVALID",
                        $"Target index {targetIndex} is out of range. Document has {totalPages} pages.",
                        ErrorCategory.Validation,
                        correlationId,
                        ("TargetIndex", targetIndex),
                        ("TotalPages", totalPages));
                }

                foreach (var pageIndex in pageIndices)
                {
                    if (pageIndex < 0 || pageIndex >= totalPages)
                    {
                        return CreateError(
                            "PDF_PAGE_INVALID",
                            $"Page index {pageIndex} is out of range. Document has {totalPages} pages.",
                            ErrorCategory.Validation,
                            correlationId,
                            ("PageIndex", pageIndex),
                            ("TotalPages", totalPages));
                    }
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                // Build new page order
                var newOrder = BuildNewPageOrder(totalPages, pageIndices, targetIndex);
                var pageRangeString = string.Join(",", newOrder.Select(p => p.ToString()));

                _logger.LogDebug(
                    "Reordering with new order: {PageOrder}. CorrelationId={CorrelationId}",
                    pageRangeString, correlationId);

                // Create target job and add pages in new order
                targetJob = QpdfNative.CreateJob();
                if (targetJob.IsInvalid)
                {
                    return CreateError(
                        "PDF_REORDER_FAILED",
                        "Failed to create target QPDF job.",
                        ErrorCategory.System,
                        correlationId);
                }

                // Read into target job
                readResult = QpdfNative.ReadDocument(targetJob, document.FilePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, readResult, document.FilePath, correlationId, "read for target");
                }

                // Use AddPages with page range to reorder
                var addResult = QpdfNative.AddPages(targetJob, sourceJob, pageRangeString);
                if (addResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, addResult, document.FilePath, correlationId, "reorder pages");
                }

                // Write back to the same file
                var writeResult = QpdfNative.WriteDocument(targetJob, document.FilePath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(targetJob, writeResult, document.FilePath, correlationId, "write reordered document");
                }

                _logger.LogInformation(
                    "Page reorder completed successfully. CorrelationId={CorrelationId}",
                    correlationId);

                return Result.Ok();
            }
            catch (OperationCanceledException)
            {
                return CreateCancellationError(correlationId);
            }
            catch (Exception ex)
            {
                return CreateError(
                    "PDF_REORDER_FAILED",
                    $"Unexpected error during reorder: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name));
            }
            finally
            {
                sourceJob?.Dispose();
                targetJob?.Dispose();
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result> InsertBlankPageAsync(
        PdfDocument document,
        int insertAtIndex,
        PageSize pageSize,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting blank page insertion. CorrelationId={CorrelationId}, InsertAt={InsertAt}, PageSize={PageSize}",
            correlationId, insertAtIndex, pageSize);

        // Validate inputs
        var validationResult = ValidateDocument(document, correlationId);
        if (validationResult.IsFailed)
        {
            return validationResult;
        }

        return await Task.Run(() =>
        {
            SafeQpdfJobHandle? job = null;

            try
            {
                // Load document
                job = QpdfNative.CreateJob();
                if (job.IsInvalid)
                {
                    return CreateError(
                        "PDF_INSERT_FAILED",
                        "Failed to create QPDF job for insert operation.",
                        ErrorCategory.System,
                        correlationId);
                }

                var readResult = QpdfNative.ReadDocument(job, document.FilePath);
                if (readResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, readResult, document.FilePath, correlationId, "read document for insert");
                }

                var totalPages = QpdfNative.GetPageCount(job);

                // Validate insert index
                if (insertAtIndex < 0 || insertAtIndex > totalPages)
                {
                    return CreateError(
                        "PDF_PAGE_INVALID",
                        $"Insert index {insertAtIndex} is out of range. Document has {totalPages} pages.",
                        ErrorCategory.Validation,
                        correlationId,
                        ("InsertIndex", insertAtIndex),
                        ("TotalPages", totalPages));
                }

                // Check for cancellation
                if (ct.IsCancellationRequested)
                {
                    return CreateCancellationError(correlationId);
                }

                // Determine page dimensions
                double[] mediaBox;
                if (pageSize == PageSize.SameAsCurrent)
                {
                    // Use reference page (or first page if inserting at start)
                    var refPageNum = insertAtIndex > 0 ? insertAtIndex : 1;
                    var refPageHandle = QpdfNative.GetPageHandle(job, refPageNum);
                    var refMediaBox = QpdfNative.GetPageMediaBox(job, refPageHandle);

                    if (refMediaBox == null)
                    {
                        // Default to Letter if we can't get reference page size
                        mediaBox = new double[] { 0, 0, 612, 792 };
                    }
                    else
                    {
                        mediaBox = refMediaBox;
                    }
                }
                else
                {
                    mediaBox = GetMediaBoxForPageSize(pageSize);
                }

                _logger.LogDebug(
                    "Creating blank page with dimensions: [{LLX}, {LLY}, {URX}, {URY}]. CorrelationId={CorrelationId}",
                    mediaBox[0], mediaBox[1], mediaBox[2], mediaBox[3], correlationId);

                // Insert blank page (position is 1-based, 0 means append)
                var position = insertAtIndex + 1; // Convert to 1-based
                var newPageHandle = QpdfNative.AddBlankPage(job, mediaBox, position);

                if (newPageHandle == 0)
                {
                    return CreateError(
                        "PDF_INSERT_FAILED",
                        "Failed to insert blank page.",
                        ErrorCategory.System,
                        correlationId);
                }

                // Write back to the same file
                var writeResult = QpdfNative.WriteDocument(job, document.FilePath);
                if (writeResult != QpdfNative.ErrorCodes.Success)
                {
                    return HandleQpdfError(job, writeResult, document.FilePath, correlationId, "write document after insert");
                }

                var newPageCount = totalPages + 1;

                _logger.LogInformation(
                    "Blank page insertion completed successfully. CorrelationId={CorrelationId}, NewPageCount={NewPageCount}",
                    correlationId, newPageCount);

                return Result.Ok();
            }
            catch (OperationCanceledException)
            {
                return CreateCancellationError(correlationId);
            }
            catch (Exception ex)
            {
                return CreateError(
                    "PDF_INSERT_FAILED",
                    $"Unexpected error during insertion: {ex.Message}",
                    ErrorCategory.System,
                    correlationId,
                    ("ExceptionType", ex.GetType().Name));
            }
            finally
            {
                job?.Dispose();
            }
        }, ct);
    }

    private static int[] BuildNewPageOrder(int totalPages, int[] pagesToMove, int targetIndex)
    {
        // Build new page order (1-based page numbers)
        var movingPages = new HashSet<int>(pagesToMove.Select(i => i + 1));
        var remaining = Enumerable.Range(1, totalPages).Where(p => !movingPages.Contains(p)).ToList();

        // Insert moving pages at target position
        var adjustedTarget = Math.Min(targetIndex, remaining.Count);
        remaining.InsertRange(adjustedTarget, pagesToMove.OrderBy(i => i).Select(i => i + 1));

        return remaining.ToArray();
    }

    private static double[] GetMediaBoxForPageSize(PageSize pageSize)
    {
        return pageSize switch
        {
            PageSize.Letter => new double[] { 0, 0, 612, 792 },   // 8.5 x 11 inches
            PageSize.A4 => new double[] { 0, 0, 595, 842 },       // 210 x 297 mm
            PageSize.Legal => new double[] { 0, 0, 612, 1008 },   // 8.5 x 14 inches
            _ => new double[] { 0, 0, 612, 792 }                   // Default to Letter
        };
    }
}
