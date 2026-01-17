using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies text extraction functionality from PDF documents.
/// Tests text extraction with character count verification and output file generation.
/// </summary>
public sealed class TextExtractionCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "text-extraction";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Extracts text from all pages in a PDF and verifies character count";

    /// <summary>
    /// Executes the text extraction test using the provided test context.
    /// Loads a test PDF, extracts text from all pages, and captures extraction metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing character counts, extracted text file, and metrics</returns>
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
            context.Logger.Information("Starting text extraction test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var textExtractionService = context.Services.GetRequiredService<ITextExtractionService>();

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

            // Extract text from all pages
            context.Logger.Information("Extracting text from all {PageCount} pages", pageCount);
            var extractionResult = await textExtractionService.ExtractAllTextAsync(document);

            if (extractionResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to extract text: {string.Join(", ", extractionResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var extractionTime = DateTime.UtcNow - extractionStart;
            var extractedTextByPage = extractionResult.Value;

            // Calculate statistics
            var totalCharacters = 0;
            var totalLines = 0;
            var totalWords = 0;
            var pageCharCounts = new Dictionary<int, int>();
            var textBuilder = new StringBuilder();

            foreach (var kvp in extractedTextByPage.OrderBy(x => x.Key))
            {
                var pageNum = kvp.Key;
                var pageText = kvp.Value;

                var charCount = pageText.Length;
                var lineCount = pageText.Split('\n').Length;
                var wordCount = pageText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                pageCharCounts[pageNum] = charCount;
                totalCharacters += charCount;
                totalLines += lineCount;
                totalWords += wordCount;

                // Append to combined text output with page separator
                textBuilder.AppendLine($"===== Page {pageNum} =====");
                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine();

                context.Logger.Information(
                    "Page {PageNumber}: {CharCount} characters, {WordCount} words, {LineCount} lines",
                    pageNum,
                    charCount,
                    wordCount,
                    lineCount
                );
            }

            context.Logger.Information(
                "Total: {TotalCharacters} characters, {TotalWords} words, {TotalLines} lines across {PageCount} pages",
                totalCharacters,
                totalWords,
                totalLines,
                pageCount
            );

            // Save extracted text to file
            var outputPath = Path.Combine(context.WorkingDirectory, "extracted_text.txt");
            await File.WriteAllTextAsync(outputPath, textBuilder.ToString(), Encoding.UTF8);
            context.Logger.Information("Saved extracted text to: {OutputPath}", outputPath);

            // Check for encoding issues (basic detection)
            var hasReplacementChars = textBuilder.ToString().Contains('\uFFFD');
            var hasNullChars = textBuilder.ToString().Contains('\0');
            var hasEncodingIssues = hasReplacementChars || hasNullChars;

            if (hasEncodingIssues)
            {
                context.Logger.Warning(
                    "Detected potential encoding issues (replacement chars: {HasReplacement}, null chars: {HasNull})",
                    hasReplacementChars,
                    hasNullChars
                );
            }

            context.Logger.Information("Extraction time: {ExtractionTimeMs} ms", extractionTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["TotalCharacters"] = totalCharacters;
            result.Outputs["TotalWords"] = totalWords;
            result.Outputs["TotalLines"] = totalLines;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["PageCharCounts"] = pageCharCounts;
            result.Outputs["ExtractionTimeMs"] = extractionTime.TotalMilliseconds;
            result.Outputs["AverageTimePerPageMs"] = pageCount > 0 ? extractionTime.TotalMilliseconds / pageCount : 0;
            result.Outputs["HasEncodingIssues"] = hasEncodingIssues;
            result.Outputs["ExitCode"] = hasEncodingIssues ? 1 : 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Text extraction test completed successfully. {TotalCharacters} chars, {TotalWords} words, {PageCount} pages, {ExtractionTimeMs:F0} ms",
                totalCharacters,
                totalWords,
                pageCount,
                extractionTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Text extraction test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the text extraction test produced expected outputs.
    /// Checks that text was extracted, has minimum character count, and output file exists.
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

        // Verify minimum character count (requirement 3.2: minimum 10 characters)
        if (!result.Outputs.TryGetValue("TotalCharacters", out var totalCharsObj) ||
            totalCharsObj is not int totalCharacters ||
            totalCharacters < 10)
        {
            return Task.FromResult(false);
        }

        // Verify page count is valid
        if (!result.Outputs.TryGetValue("PageCount", out var pageCountObj) ||
            pageCountObj is not int pageCount ||
            pageCount <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify page character counts exist and match page count
        if (!result.Outputs.TryGetValue("PageCharCounts", out var pageCharCountsObj) ||
            pageCharCountsObj is not Dictionary<int, int> pageCharCounts ||
            pageCharCounts.Count != pageCount)
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

        // Verify total words count is reasonable (should be > 0 if we have characters)
        if (!result.Outputs.TryGetValue("TotalWords", out var totalWordsObj) ||
            totalWordsObj is not int totalWords ||
            (totalCharacters >= 10 && totalWords <= 0))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
