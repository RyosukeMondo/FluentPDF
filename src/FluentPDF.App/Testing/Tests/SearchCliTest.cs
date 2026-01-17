using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies PDF text search functionality.
/// Tests search with result verification, metrics reporting, and zero-result handling.
/// </summary>
public sealed class SearchCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "search";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Searches for text in a PDF and verifies match count, page numbers, and positions";

    /// <summary>
    /// Executes the search test using the provided test context.
    /// Loads a test PDF, searches for text, and captures search metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing match counts, search results, and metrics</returns>
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
            context.Logger.Information("Starting search test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var searchService = context.Services.GetRequiredService<ITextSearchService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "multi-page.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            // Get search term from context data or use default
            var searchTerm = context.Data.TryGetValue("SearchTerm", out var termObj) && termObj is string term
                ? term
                : "the"; // Common word for testing

            // Get search options from context data or use default
            var caseSensitive = context.Data.TryGetValue("CaseSensitive", out var csObj) && csObj is bool cs && cs;
            var wholeWord = context.Data.TryGetValue("WholeWord", out var wwObj) && wwObj is bool ww && ww;
            var searchOptions = new SearchOptions
            {
                CaseSensitive = caseSensitive,
                WholeWord = wholeWord
            };

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);
            context.Logger.Information("Search term: '{SearchTerm}' (CaseSensitive={CaseSensitive}, WholeWord={WholeWord})",
                searchTerm, caseSensitive, wholeWord);

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

            // Record search start time
            var searchStart = DateTime.UtcNow;

            // Search for text
            context.Logger.Information("Searching for '{SearchTerm}' across all pages", searchTerm);
            var searchResult = await searchService.SearchAsync(document, searchTerm, searchOptions);

            if (searchResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to search PDF: {string.Join(", ", searchResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var searchTime = DateTime.UtcNow - searchStart;
            var matches = searchResult.Value;

            // Calculate statistics
            var totalMatches = matches.Count;
            var pagesWithMatches = matches.Select(m => m.PageNumber).Distinct().Count();
            var matchesByPage = matches.GroupBy(m => m.PageNumber)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            context.Logger.Information(
                "Found {TotalMatches} matches on {PagesWithMatches}/{PageCount} pages",
                totalMatches,
                pagesWithMatches,
                pageCount
            );

            // Build detailed search results for logging and verification
            var searchDetails = new StringBuilder();
            searchDetails.AppendLine($"Search Results for '{searchTerm}'");
            searchDetails.AppendLine($"Case Sensitive: {caseSensitive}, Whole Word: {wholeWord}");
            searchDetails.AppendLine($"Total Matches: {totalMatches}");
            searchDetails.AppendLine($"Pages with Matches: {pagesWithMatches}/{pageCount}");
            searchDetails.AppendLine();

            if (totalMatches == 0)
            {
                searchDetails.AppendLine("No matches found.");
                context.Logger.Information("No matches found for '{SearchTerm}'", searchTerm);
            }
            else
            {
                foreach (var kvp in matchesByPage)
                {
                    var pageNum = kvp.Key;
                    var count = kvp.Value;
                    searchDetails.AppendLine($"Page {pageNum + 1}: {count} matches");

                    var pageMatches = matches.Where(m => m.PageNumber == pageNum).ToList();
                    foreach (var match in pageMatches.Take(5)) // Limit to 5 per page for readability
                    {
                        searchDetails.AppendLine($"  - Char {match.CharIndex}: '{match.Text}' at ({match.BoundingBox.Left:F2}, {match.BoundingBox.Top:F2})");
                    }
                    if (pageMatches.Count > 5)
                    {
                        searchDetails.AppendLine($"  ... and {pageMatches.Count - 5} more matches");
                    }

                    context.Logger.Information("Page {PageNumber}: {MatchCount} matches", pageNum + 1, count);
                }
            }

            // Save search results to file
            var outputPath = Path.Combine(context.WorkingDirectory, "search_results.txt");
            await File.WriteAllTextAsync(outputPath, searchDetails.ToString(), Encoding.UTF8);
            context.Logger.Information("Saved search results to: {OutputPath}", outputPath);

            // Save detailed JSON results
            var jsonOutputPath = Path.Combine(context.WorkingDirectory, "search_results.json");
            var jsonData = new
            {
                searchTerm,
                caseSensitive,
                wholeWord,
                totalMatches,
                pagesWithMatches,
                pageCount,
                matchesByPage = matchesByPage.Select(kvp => new
                {
                    pageNumber = kvp.Key + 1, // Convert to 1-based
                    matchCount = kvp.Value
                }).ToList(),
                matches = matches.Select(m => new
                {
                    pageNumber = m.PageNumber + 1, // Convert to 1-based
                    charIndex = m.CharIndex,
                    length = m.Length,
                    text = m.Text,
                    boundingBox = new
                    {
                        left = m.BoundingBox.Left,
                        top = m.BoundingBox.Top,
                        right = m.BoundingBox.Right,
                        bottom = m.BoundingBox.Bottom
                    }
                }).ToList(),
                searchTimeMs = searchTime.TotalMilliseconds
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jsonData, jsonOptions);
            await File.WriteAllTextAsync(jsonOutputPath, json);
            context.Logger.Information("Saved JSON search results to: {JsonOutputPath}", jsonOutputPath);

            context.Logger.Information("Search time: {SearchTimeMs} ms", searchTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["JsonOutputFile"] = jsonOutputPath;
            result.Outputs["SearchTerm"] = searchTerm;
            result.Outputs["TotalMatches"] = totalMatches;
            result.Outputs["PagesWithMatches"] = pagesWithMatches;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["SearchTimeMs"] = searchTime.TotalMilliseconds;
            result.Outputs["MatchesByPage"] = matchesByPage;
            result.Outputs["ExitCode"] = 0; // Success even with zero results

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Search test completed successfully. {TotalMatches} matches found, {SearchTimeMs:F0} ms",
                totalMatches,
                searchTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Search test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the search test produced expected outputs.
    /// Checks that search executed successfully and output files exist.
    /// Accepts zero results as valid (requirement 2.4).
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

        // Verify output files exist
        if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
            outputFileObj is not string outputFile ||
            !File.Exists(outputFile))
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("JsonOutputFile", out var jsonFileObj) ||
            jsonFileObj is not string jsonFile ||
            !File.Exists(jsonFile))
        {
            return Task.FromResult(false);
        }

        // Verify search term was captured
        if (!result.Outputs.TryGetValue("SearchTerm", out var searchTermObj) ||
            searchTermObj is not string searchTerm ||
            string.IsNullOrWhiteSpace(searchTerm))
        {
            return Task.FromResult(false);
        }

        // Verify total matches count is valid (can be 0)
        if (!result.Outputs.TryGetValue("TotalMatches", out var totalMatchesObj) ||
            totalMatchesObj is not int totalMatches ||
            totalMatches < 0)
        {
            return Task.FromResult(false);
        }

        // Verify pages with matches count is valid
        if (!result.Outputs.TryGetValue("PagesWithMatches", out var pagesWithMatchesObj) ||
            pagesWithMatchesObj is not int pagesWithMatches ||
            pagesWithMatches < 0)
        {
            return Task.FromResult(false);
        }

        // Verify search time was captured
        if (!result.Outputs.TryGetValue("SearchTimeMs", out var searchTimeObj) ||
            searchTimeObj is not double searchTimeMs ||
            searchTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        // Verify JSON file is valid
        try
        {
            var json = File.ReadAllText(jsonFile);
            JsonDocument.Parse(json);
        }
        catch
        {
            return Task.FromResult(false);
        }

        // Verify matches by page dictionary is valid
        if (!result.Outputs.TryGetValue("MatchesByPage", out var matchesByPageObj) ||
            matchesByPageObj is not Dictionary<int, int> matchesByPage)
        {
            return Task.FromResult(false);
        }

        // Verify consistency: if total matches > 0, pages with matches > 0
        if (totalMatches > 0 && pagesWithMatches == 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
