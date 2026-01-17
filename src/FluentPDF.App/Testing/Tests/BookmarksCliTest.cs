using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies bookmark extraction functionality from PDF documents.
/// Tests bookmark extraction with hierarchy verification and JSON output generation.
/// </summary>
public sealed class BookmarksCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "bookmarks";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Extracts bookmarks from a PDF and verifies structure, page destinations, and hierarchy";

    /// <summary>
    /// Executes the bookmark extraction test using the provided test context.
    /// Loads a test PDF, extracts bookmarks, and captures extraction metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing bookmark counts, JSON output file, and metrics</returns>
    public async Task<CliTestResult> RunAsync(CliTestContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var result = new CliTestResult
        {
            TestName = Name,
            Success = false
        };

        var startTime = DateTime.UtcNow;

        try
        {
            context.Logger.Information("Starting bookmark extraction test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var bookmarkService = context.Services.GetRequiredService<IBookmarkService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);

            if (!File.Exists(testPdfPath))
            {
                result.ErrorMessage = $"Test PDF not found: {testPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            var pageCount = document.PageCount;
            context.Logger.Information("Loaded PDF with {PageCount} pages", pageCount);

            // Record extraction start time
            var extractionStart = DateTime.UtcNow;

            // Extract bookmarks
            context.Logger.Information("Extracting bookmarks");
            var bookmarkResult = await bookmarkService.ExtractBookmarksAsync(document);

            if (bookmarkResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to extract bookmarks: {string.Join(", ", bookmarkResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var extractionTime = DateTime.UtcNow - extractionStart;
            var bookmarks = bookmarkResult.Value;

            // Calculate statistics
            var rootBookmarkCount = bookmarks.Count;
            var totalBookmarkCount = bookmarks.Sum(b => b.GetTotalNodeCount());
            var bookmarksWithDestinations = CountBookmarksWithDestinations(bookmarks);
            var invalidPageNumbers = CountInvalidPageNumbers(bookmarks, pageCount);

            context.Logger.Information(
                "Extracted {RootCount} root bookmarks, {TotalCount} total bookmarks",
                rootBookmarkCount,
                totalBookmarkCount
            );

            context.Logger.Information(
                "Bookmarks with destinations: {WithDestinations}/{Total}, Invalid page numbers: {Invalid}",
                bookmarksWithDestinations,
                totalBookmarkCount,
                invalidPageNumbers
            );

            // Build detailed bookmark tree for logging and verification
            var bookmarkDetails = BuildBookmarkDetails(bookmarks, pageCount);

            // Save bookmarks to JSON
            var outputPath = Path.Combine(context.WorkingDirectory, "bookmarks.json");
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(bookmarkDetails, jsonOptions);
            await File.WriteAllTextAsync(outputPath, json);
            context.Logger.Information("Saved bookmark tree to: {OutputPath}", outputPath);

            context.Logger.Information("Extraction time: {ExtractionTimeMs} ms", extractionTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["RootBookmarkCount"] = rootBookmarkCount;
            result.Outputs["TotalBookmarkCount"] = totalBookmarkCount;
            result.Outputs["BookmarksWithDestinations"] = bookmarksWithDestinations;
            result.Outputs["InvalidPageNumbers"] = invalidPageNumbers;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["ExtractionTimeMs"] = extractionTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = invalidPageNumbers > 0 ? 1 : 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Bookmark extraction test completed successfully. {TotalCount} bookmarks, {WithDestinations} with destinations, {ExtractionTimeMs:F0} ms",
                totalBookmarkCount,
                bookmarksWithDestinations,
                extractionTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Bookmark extraction test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the bookmark extraction test produced expected outputs.
    /// Checks that bookmarks were extracted, structure is valid, and output file exists.
    /// </summary>
    /// <param name="result">The test result produced by RunAsync</param>
    /// <returns>True if all verification checks pass, false otherwise</returns>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Check basic success
        if (!result.Success)
        {
            return Task.FromResult(false);
        }

        // Verify output file exists
        if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
            outputFileObj is not string outputFile ||
            !File.Exists(outputFile))
        {
            return Task.FromResult(false);
        }

        // Verify bookmark counts are valid
        if (!result.Outputs.TryGetValue("TotalBookmarkCount", out var totalCountObj) ||
            totalCountObj is not int totalCount ||
            totalCount < 0)
        {
            return Task.FromResult(false);
        }

        // Verify root bookmark count is valid
        if (!result.Outputs.TryGetValue("RootBookmarkCount", out var rootCountObj) ||
            rootCountObj is not int rootCount ||
            rootCount < 0 ||
            rootCount > totalCount)
        {
            return Task.FromResult(false);
        }

        // Verify no invalid page numbers
        if (!result.Outputs.TryGetValue("InvalidPageNumbers", out var invalidPageObj) ||
            invalidPageObj is not int invalidPages ||
            invalidPages != 0)
        {
            return Task.FromResult(false);
        }

        // Verify extraction time was captured
        if (!result.Outputs.TryGetValue("ExtractionTimeMs", out var extractionTimeObj) ||
            extractionTimeObj is not double extractionTimeMs ||
            extractionTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        // Verify JSON file is valid
        try
        {
            var json = File.ReadAllText(outputFile);
            JsonDocument.Parse(json);
        }
        catch
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Counts the number of bookmarks that have page destinations.
    /// </summary>
    private static int CountBookmarksWithDestinations(List<BookmarkNode> bookmarks)
    {
        int count = 0;
        foreach (var bookmark in bookmarks)
        {
            if (bookmark.PageNumber.HasValue)
            {
                count++;
            }
            count += CountBookmarksWithDestinations(bookmark.Children);
        }
        return count;
    }

    /// <summary>
    /// Counts the number of bookmarks with invalid page numbers (out of range).
    /// </summary>
    private static int CountInvalidPageNumbers(List<BookmarkNode> bookmarks, int pageCount)
    {
        int count = 0;
        foreach (var bookmark in bookmarks)
        {
            if (bookmark.PageNumber.HasValue &&
                (bookmark.PageNumber.Value < 1 || bookmark.PageNumber.Value > pageCount))
            {
                count++;
            }
            count += CountInvalidPageNumbers(bookmark.Children, pageCount);
        }
        return count;
    }

    /// <summary>
    /// Builds a detailed bookmark tree structure for JSON serialization.
    /// </summary>
    private static List<object> BuildBookmarkDetails(List<BookmarkNode> bookmarks, int pageCount)
    {
        var details = new List<object>();
        foreach (var bookmark in bookmarks)
        {
            var item = new Dictionary<string, object>
            {
                ["title"] = bookmark.Title,
                ["pageNumber"] = bookmark.PageNumber ?? -1,
                ["hasDestination"] = bookmark.PageNumber.HasValue,
                ["isValidPage"] = bookmark.PageNumber.HasValue &&
                                  bookmark.PageNumber.Value >= 1 &&
                                  bookmark.PageNumber.Value <= pageCount,
                ["x"] = bookmark.X ?? -1,
                ["y"] = bookmark.Y ?? -1,
                ["childCount"] = bookmark.Children.Count
            };

            if (bookmark.Children.Count > 0)
            {
                item["children"] = BuildBookmarkDetails(bookmark.Children, pageCount);
            }

            details.Add(item);
        }
        return details;
    }
}
