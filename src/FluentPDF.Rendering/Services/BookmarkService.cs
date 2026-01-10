using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for extracting hierarchical bookmark structures from PDF documents using PDFium.
/// Implements iterative tree traversal to avoid stack overflow with deeply nested bookmarks.
/// </summary>
public sealed class BookmarkService : IBookmarkService
{
    private readonly ILogger<BookmarkService> _logger;
    private const int MaxDepth = 20; // Prevent infinite loops from malformed PDFs

    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarkService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public BookmarkService(ILogger<BookmarkService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<List<BookmarkNode>>> ExtractBookmarksAsync(PdfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Extracting bookmarks. CorrelationId={CorrelationId}, FilePath={FilePath}",
            correlationId, document.FilePath);

        var startTime = DateTime.UtcNow;

        return await Task.Run(() =>
        {
            try
            {
                var documentHandle = (SafePdfDocumentHandle)document.Handle;

                if (documentHandle.IsInvalid)
                {
                    var error = new PdfError(
                        "BOOKMARK_INVALID_HANDLE",
                        "Invalid document handle for bookmark extraction.",
                        ErrorCategory.Validation,
                        ErrorSeverity.Error)
                        .WithContext("FilePath", document.FilePath)
                        .WithContext("CorrelationId", correlationId);

                    _logger.LogError(
                        "Invalid document handle. CorrelationId={CorrelationId}",
                        correlationId);

                    return Result.Fail<List<BookmarkNode>>(error);
                }

                var rootBookmarks = new List<BookmarkNode>();
                var firstBookmark = PdfiumInterop.GetFirstChildBookmark(documentHandle, IntPtr.Zero);

                if (firstBookmark == IntPtr.Zero)
                {
                    // No bookmarks in document - this is not an error
                    var elapsed = DateTime.UtcNow - startTime;
                    _logger.LogInformation(
                        "No bookmarks found. CorrelationId={CorrelationId}, ElapsedMs={ElapsedMs}",
                        correlationId, elapsed.TotalMilliseconds);

                    return Result.Ok(rootBookmarks);
                }

                // Extract bookmarks using iterative algorithm
                ExtractBookmarksIterative(documentHandle, firstBookmark, rootBookmarks, correlationId);

                var totalCount = rootBookmarks.Sum(b => b.GetTotalNodeCount());
                var elapsedTime = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "Bookmarks extracted successfully. CorrelationId={CorrelationId}, " +
                    "RootCount={RootCount}, TotalCount={TotalCount}, ElapsedMs={ElapsedMs}",
                    correlationId, rootBookmarks.Count, totalCount, elapsedTime.TotalMilliseconds);

                return Result.Ok(rootBookmarks);
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "BOOKMARK_EXTRACTION_FAILED",
                    $"Failed to extract bookmarks: {ex.Message}",
                    ErrorCategory.Rendering,
                    ErrorSeverity.Error)
                    .WithContext("FilePath", document.FilePath)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("Exception", ex.ToString());

                _logger.LogError(
                    ex,
                    "Bookmark extraction failed. CorrelationId={CorrelationId}, FilePath={FilePath}",
                    correlationId, document.FilePath);

                return Result.Fail<List<BookmarkNode>>(error);
            }
        });
    }

    /// <summary>
    /// Extracts bookmarks using an iterative depth-first traversal algorithm.
    /// Uses a stack to avoid recursion and prevent stack overflow with deeply nested bookmarks.
    /// </summary>
    private void ExtractBookmarksIterative(
        SafePdfDocumentHandle documentHandle,
        IntPtr firstBookmark,
        List<BookmarkNode> rootBookmarks,
        Guid correlationId)
    {
        // Stack item: (bookmark handle, parent's children list, depth)
        var stack = new Stack<(IntPtr handle, List<BookmarkNode> parentList, int depth)>();

        // Start with first root bookmark
        stack.Push((firstBookmark, rootBookmarks, 0));

        while (stack.Count > 0)
        {
            var (currentHandle, parentList, depth) = stack.Pop();

            if (currentHandle == IntPtr.Zero)
            {
                continue;
            }

            // Prevent infinite loops from malformed PDFs
            if (depth >= MaxDepth)
            {
                _logger.LogWarning(
                    "Maximum bookmark depth reached. CorrelationId={CorrelationId}, Depth={Depth}",
                    correlationId, depth);
                continue;
            }

            // Extract bookmark data
            var title = PdfiumInterop.GetBookmarkTitle(currentHandle);
            var dest = PdfiumInterop.GetBookmarkDest(documentHandle, currentHandle);

            int? pageNumber = null;
            float? x = null;
            float? y = null;

            if (dest != IntPtr.Zero)
            {
                var pageIndex = PdfiumInterop.GetDestPageIndex(documentHandle, dest);
                if (pageIndex >= 0)
                {
                    // Convert 0-based to 1-based page number
                    pageNumber = pageIndex + 1;

                    // Try to get coordinates (optional)
                    if (PdfiumInterop.GetDestLocationInPage(dest, out var hasX, out var hasY, out _, out var xCoord, out var yCoord, out _))
                    {
                        if (hasX)
                        {
                            x = xCoord;
                        }
                        if (hasY)
                        {
                            y = yCoord;
                        }
                    }
                }
            }

            // Create bookmark node
            var bookmarkNode = new BookmarkNode
            {
                Title = title,
                PageNumber = pageNumber,
                X = x,
                Y = y
            };

            parentList.Add(bookmarkNode);

            // Push next sibling (processed after children due to stack LIFO)
            var nextSibling = PdfiumInterop.GetNextSiblingBookmark(documentHandle, currentHandle);
            if (nextSibling != IntPtr.Zero)
            {
                stack.Push((nextSibling, parentList, depth));
            }

            // Push first child (processed before siblings)
            var firstChild = PdfiumInterop.GetFirstChildBookmark(documentHandle, currentHandle);
            if (firstChild != IntPtr.Zero)
            {
                stack.Push((firstChild, bookmarkNode.Children, depth + 1));
            }
        }
    }
}
