using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies PDF find and replace functionality.
/// Tests text replacement with preview mode, undo support, and metrics reporting.
/// </summary>
public sealed class FindReplaceCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "find-replace";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Finds and replaces text in a PDF with preview and verification";

    /// <summary>
    /// Executes the find and replace test using the provided test context.
    /// Loads a test PDF, searches for text, replaces occurrences, and saves the modified PDF.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing replacement counts, modified PDF path, and metrics</returns>
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
            context.Logger.Information("Starting find and replace test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var searchService = context.Services.GetRequiredService<ITextSearchService>();
            var replacementService = context.Services.GetRequiredService<ITextReplacementService>();
            var annotationService = context.Services.GetRequiredService<IAnnotationService>();

            // Get test PDF path from context data or use default
            var inputPdfPath = context.Data.TryGetValue("InputFile", out var inputObj) && inputObj is string input
                ? input
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "sample-with-text.pdf"
                );
            inputPdfPath = Path.GetFullPath(inputPdfPath);

            // Get find/replace terms from context data or use defaults
            var findText = context.Data.TryGetValue("FindText", out var findObj) && findObj is string find
                ? find
                : "the"; // Common word for testing

            var replaceText = context.Data.TryGetValue("ReplaceText", out var replaceObj) && replaceObj is string replace
                ? replace
                : "THE"; // Replace with uppercase

            // Get output path
            var outputPdfPath = context.Data.TryGetValue("OutputFile", out var outputObj) && outputObj is string output
                ? output
                : Path.Combine(context.WorkingDirectory, "replaced.pdf");
            outputPdfPath = Path.GetFullPath(outputPdfPath);

            // Get search options
            var caseSensitive = context.Data.TryGetValue("CaseSensitive", out var csObj) && csObj is bool cs && cs;
            var wholeWord = context.Data.TryGetValue("WholeWord", out var wwObj) && wwObj is bool ww && ww;
            var preview = context.Data.TryGetValue("Preview", out var prevObj) && prevObj is bool prev && prev;

            var searchOptions = new SearchOptions
            {
                CaseSensitive = caseSensitive,
                WholeWord = wholeWord
            };

            context.Logger.Information("Input PDF: {InputPath}", inputPdfPath);
            context.Logger.Information("Output PDF: {OutputPath}", outputPdfPath);
            context.Logger.Information("Find: '{FindText}', Replace: '{ReplaceText}'", findText, replaceText);
            context.Logger.Information("Options: CaseSensitive={CaseSensitive}, WholeWord={WholeWord}, Preview={Preview}",
                caseSensitive, wholeWord, preview);

            if (!File.Exists(inputPdfPath))
            {
                result.ErrorMessage = $"Input PDF not found: {inputPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(inputPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            var pageCount = document.PageCount;
            context.Logger.Information("Loaded PDF with {PageCount} pages", pageCount);

            // First, search to find matches
            var searchStart = DateTime.UtcNow;
            context.Logger.Information("Searching for '{FindText}' across all pages", findText);
            var searchResult = await searchService.SearchAsync(document, findText, searchOptions);

            if (searchResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to search PDF: {string.Join(", ", searchResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var searchTime = DateTime.UtcNow - searchStart;
            var matches = searchResult.Value;
            var totalMatches = matches.Count;

            context.Logger.Information("Found {TotalMatches} matches in {SearchTimeMs:F0} ms",
                totalMatches, searchTime.TotalMilliseconds);

            if (totalMatches == 0)
            {
                result.ErrorMessage = "No matches found to replace";
                result.Outputs["ExitCode"] = 1; // No matches = failure for replace
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Perform replacement
            var replaceStart = DateTime.UtcNow;
            context.Logger.Information("Replacing all {TotalMatches} matches (Preview={Preview})", totalMatches, preview);

            var replaceResult = await replacementService.ReplaceAllAsync(
                document,
                findText,
                replaceText,
                searchOptions,
                preview);

            if (replaceResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to replace text: {string.Join(", ", replaceResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var replaceTime = DateTime.UtcNow - replaceStart;
            var replacements = replaceResult.Value;
            var totalReplacements = replacements.Count;

            context.Logger.Information("Replaced {ReplacementCount}/{TotalMatches} matches in {ReplaceTimeMs:F0} ms",
                totalReplacements, totalMatches, replaceTime.TotalMilliseconds);

            // Calculate statistics
            var pagesAffected = replacements.Select(r => r.PageNumber).Distinct().Count();
            var replacementsByPage = replacements.GroupBy(r => r.PageNumber)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            // Build detailed results
            var resultsText = new StringBuilder();
            resultsText.AppendLine($"Find and Replace Results");
            resultsText.AppendLine($"Find: '{findText}', Replace: '{replaceText}'");
            resultsText.AppendLine($"Case Sensitive: {caseSensitive}, Whole Word: {wholeWord}");
            resultsText.AppendLine($"Preview Mode: {preview}");
            resultsText.AppendLine($"Total Matches: {totalMatches}");
            resultsText.AppendLine($"Replacements Made: {totalReplacements}");
            resultsText.AppendLine($"Pages Affected: {pagesAffected}/{pageCount}");
            resultsText.AppendLine();

            foreach (var kvp in replacementsByPage)
            {
                var pageNum = kvp.Key;
                var count = kvp.Value;
                resultsText.AppendLine($"Page {pageNum + 1}: {count} replacements");

                var pageReplacements = replacements.Where(r => r.PageNumber == pageNum).Take(5).ToList();
                foreach (var replacement in pageReplacements)
                {
                    resultsText.AppendLine($"  - '{replacement.OriginalText}' -> '{replacement.ReplacementText}' at ({replacement.BoundingBox.Left:F2}, {replacement.BoundingBox.Top:F2})");
                }
                if (kvp.Value > 5)
                {
                    resultsText.AppendLine($"  ... and {kvp.Value - 5} more replacements");
                }
            }

            // Save results to file
            var resultsPath = Path.Combine(context.WorkingDirectory, "replace_results.txt");
            await File.WriteAllTextAsync(resultsPath, resultsText.ToString(), Encoding.UTF8);
            context.Logger.Information("Saved replacement results to: {ResultsPath}", resultsPath);

            // Save detailed JSON results
            var jsonOutputPath = Path.Combine(context.WorkingDirectory, "replace_results.json");
            var jsonData = new
            {
                findText,
                replaceText,
                caseSensitive,
                wholeWord,
                preview,
                totalMatches,
                totalReplacements,
                pagesAffected,
                pageCount,
                replacementsByPage = replacementsByPage.Select(kvp => new
                {
                    pageNumber = kvp.Key + 1, // Convert to 1-based
                    replacementCount = kvp.Value
                }).ToList(),
                replacements = replacements.Select(r => new
                {
                    pageNumber = r.PageNumber + 1, // Convert to 1-based
                    originalText = r.OriginalText,
                    replacementText = r.ReplacementText,
                    annotationIndex = r.AnnotationIndex,
                    timestamp = r.Timestamp,
                    boundingBox = new
                    {
                        left = r.BoundingBox.Left,
                        top = r.BoundingBox.Top,
                        right = r.BoundingBox.Right,
                        bottom = r.BoundingBox.Bottom
                    }
                }).ToList(),
                searchTimeMs = searchTime.TotalMilliseconds,
                replaceTimeMs = replaceTime.TotalMilliseconds
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jsonData, jsonOptions);
            await File.WriteAllTextAsync(jsonOutputPath, json);
            context.Logger.Information("Saved JSON replacement results to: {JsonOutputPath}", jsonOutputPath);

            // Save the modified PDF (if not in preview mode)
            if (!preview)
            {
                var saveStart = DateTime.UtcNow;
                context.Logger.Information("Saving modified PDF to: {OutputPath}", outputPdfPath);

                var saveResult = await annotationService.SaveAnnotationsAsync(document, outputPdfPath, createBackup: false);

                if (saveResult.IsFailed)
                {
                    result.ErrorMessage = $"Failed to save PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                    result.Duration = DateTime.UtcNow - startTime;
                    return result;
                }

                var saveTime = DateTime.UtcNow - saveStart;
                context.Logger.Information("Saved modified PDF in {SaveTimeMs:F0} ms", saveTime.TotalMilliseconds);

                // Verify output file exists
                if (!File.Exists(outputPdfPath))
                {
                    result.ErrorMessage = "Output PDF was not created";
                    result.Duration = DateTime.UtcNow - startTime;
                    return result;
                }

                var outputFileInfo = new FileInfo(outputPdfPath);
                context.Logger.Information("Output PDF size: {FileSizeBytes} bytes", outputFileInfo.Length);

                result.Outputs["OutputFile"] = outputPdfPath;
                result.Outputs["OutputFileSizeBytes"] = outputFileInfo.Length;
                result.Outputs["SaveTimeMs"] = saveTime.TotalMilliseconds;
            }
            else
            {
                context.Logger.Information("Preview mode - PDF not saved");
            }

            // Store outputs for verification
            result.Outputs["ResultsFile"] = resultsPath;
            result.Outputs["JsonOutputFile"] = jsonOutputPath;
            result.Outputs["FindText"] = findText;
            result.Outputs["ReplaceText"] = replaceText;
            result.Outputs["TotalMatches"] = totalMatches;
            result.Outputs["TotalReplacements"] = totalReplacements;
            result.Outputs["PagesAffected"] = pagesAffected;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["Preview"] = preview;
            result.Outputs["SearchTimeMs"] = searchTime.TotalMilliseconds;
            result.Outputs["ReplaceTimeMs"] = replaceTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0; // Success

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Find and replace test completed successfully. {TotalReplacements}/{TotalMatches} replacements made, {TotalTimeMs:F0} ms total",
                totalReplacements,
                totalMatches,
                result.Duration.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Find and replace test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the find and replace test produced expected outputs.
    /// Checks that replacement executed successfully and output files exist.
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

        // Verify results file exists
        if (!result.Outputs.TryGetValue("ResultsFile", out var resultsFileObj) ||
            resultsFileObj is not string resultsFile ||
            !File.Exists(resultsFile))
        {
            return Task.FromResult(false);
        }

        // Verify JSON file exists
        if (!result.Outputs.TryGetValue("JsonOutputFile", out var jsonFileObj) ||
            jsonFileObj is not string jsonFile ||
            !File.Exists(jsonFile))
        {
            return Task.FromResult(false);
        }

        // Verify find text was captured
        if (!result.Outputs.TryGetValue("FindText", out var findTextObj) ||
            findTextObj is not string findText ||
            string.IsNullOrWhiteSpace(findText))
        {
            return Task.FromResult(false);
        }

        // Verify replace text was captured
        if (!result.Outputs.TryGetValue("ReplaceText", out var replaceTextObj) ||
            replaceTextObj is not string replaceText ||
            string.IsNullOrWhiteSpace(replaceText))
        {
            return Task.FromResult(false);
        }

        // Verify total matches count is valid (must be > 0 for successful replace)
        if (!result.Outputs.TryGetValue("TotalMatches", out var totalMatchesObj) ||
            totalMatchesObj is not int totalMatches ||
            totalMatches <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify total replacements count is valid
        if (!result.Outputs.TryGetValue("TotalReplacements", out var totalReplacementsObj) ||
            totalReplacementsObj is not int totalReplacements ||
            totalReplacements < 0 ||
            totalReplacements > totalMatches)
        {
            return Task.FromResult(false);
        }

        // Verify preview mode flag
        if (!result.Outputs.TryGetValue("Preview", out var previewObj) ||
            previewObj is not bool preview)
        {
            return Task.FromResult(false);
        }

        // If not preview mode, verify output PDF exists
        if (!preview)
        {
            if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
                outputFileObj is not string outputFile ||
                !File.Exists(outputFile))
            {
                return Task.FromResult(false);
            }

            // Verify output file size is reasonable
            if (!result.Outputs.TryGetValue("OutputFileSizeBytes", out var fileSizeObj) ||
                fileSizeObj is not long fileSize ||
                fileSize <= 0)
            {
                return Task.FromResult(false);
            }
        }

        // Verify timing metrics
        if (!result.Outputs.TryGetValue("SearchTimeMs", out var searchTimeObj) ||
            searchTimeObj is not double searchTimeMs ||
            searchTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("ReplaceTimeMs", out var replaceTimeObj) ||
            replaceTimeObj is not double replaceTimeMs ||
            replaceTimeMs < 0)
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

        return Task.FromResult(true);
    }
}
