using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies metadata extraction functionality from PDF documents.
/// Tests metadata extraction with field validation and output reporting.
/// </summary>
/// <remarks>
/// NOTE: This test currently extracts basic document properties (page count, file size, file path).
/// Full PDF metadata extraction (title, author, subject, keywords, creator, producer, creation date, mod date)
/// requires implementing FPDF_GetMetaText API support in the rendering layer.
/// TODO: Enhance this test when metadata service is implemented.
/// </remarks>
public sealed class MetadataCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "metadata";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Extracts document metadata and verifies standard fields";

    /// <summary>
    /// Executes the metadata extraction test using the provided test context.
    /// Loads a test PDF, extracts available metadata, and captures extraction metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing metadata fields and metrics</returns>
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
            context.Logger.Information("Starting metadata extraction test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();

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

            // Record extraction start time
            var extractionStart = DateTime.UtcNow;

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;

            // Extract available metadata
            var metadata = new Dictionary<string, object>
            {
                // Basic document properties (currently available)
                ["FilePath"] = document.FilePath,
                ["FileName"] = Path.GetFileName(document.FilePath),
                ["FileSize"] = FormatFileSize(document.FileSizeBytes),
                ["FileSizeBytes"] = document.FileSizeBytes,
                ["PageCount"] = document.PageCount,
                ["LoadedAt"] = document.LoadedAt,

                // File system metadata
                ["FileCreatedDate"] = File.GetCreationTime(testPdfPath),
                ["FileModifiedDate"] = File.GetLastWriteTime(testPdfPath),
                ["FileAccessedDate"] = File.GetLastAccessTime(testPdfPath),

                // PDF metadata fields (to be implemented via FPDF_GetMetaText)
                // TODO: Implement these when metadata service is available
                ["Title"] = "(not implemented)",
                ["Author"] = "(not implemented)",
                ["Subject"] = "(not implemented)",
                ["Keywords"] = "(not implemented)",
                ["Creator"] = "(not implemented)",
                ["Producer"] = "(not implemented)",
                ["CreationDate"] = "(not implemented)",
                ["ModDate"] = "(not implemented)"
            };

            var extractionTime = DateTime.UtcNow - extractionStart;

            context.Logger.Information("Extracted metadata from PDF");
            context.Logger.Information("  File: {FileName}", metadata["FileName"]);
            context.Logger.Information("  Size: {FileSize}", metadata["FileSize"]);
            context.Logger.Information("  Pages: {PageCount}", metadata["PageCount"]);

            // Build detailed metadata report
            var report = new StringBuilder();
            report.AppendLine("Document Metadata Test Report");
            report.AppendLine("=" + new string('=', 79));
            report.AppendLine();
            report.AppendLine("File Information:");
            report.AppendLine($"  File Name:       {metadata["FileName"]}");
            report.AppendLine($"  File Path:       {metadata["FilePath"]}");
            report.AppendLine($"  File Size:       {metadata["FileSize"]} ({metadata["FileSizeBytes"]:N0} bytes)");
            report.AppendLine($"  Created:         {metadata["FileCreatedDate"]}");
            report.AppendLine($"  Modified:        {metadata["FileModifiedDate"]}");
            report.AppendLine($"  Accessed:        {metadata["FileAccessedDate"]}");
            report.AppendLine();
            report.AppendLine("PDF Document Properties:");
            report.AppendLine($"  Page Count:      {metadata["PageCount"]}");
            report.AppendLine($"  Loaded At:       {metadata["LoadedAt"]}");
            report.AppendLine();
            report.AppendLine("PDF Metadata (from document info dictionary):");
            report.AppendLine($"  Title:           {metadata["Title"]}");
            report.AppendLine($"  Author:          {metadata["Author"]}");
            report.AppendLine($"  Subject:         {metadata["Subject"]}");
            report.AppendLine($"  Keywords:        {metadata["Keywords"]}");
            report.AppendLine($"  Creator:         {metadata["Creator"]}");
            report.AppendLine($"  Producer:        {metadata["Producer"]}");
            report.AppendLine($"  Creation Date:   {metadata["CreationDate"]}");
            report.AppendLine($"  Modification Date: {metadata["ModDate"]}");
            report.AppendLine();
            report.AppendLine($"Extraction Time: {extractionTime.TotalMilliseconds:F2} ms");
            report.AppendLine();
            report.AppendLine("NOTE: PDF metadata fields (Title, Author, Subject, etc.) are not yet implemented.");
            report.AppendLine("      These require FPDF_GetMetaText API support in the rendering layer.");

            // Save report to file
            var reportPath = Path.Combine(context.WorkingDirectory, "metadata_report.txt");
            await File.WriteAllTextAsync(reportPath, report.ToString(), Encoding.UTF8);
            context.Logger.Information("Saved metadata report to: {ReportPath}", reportPath);

            // Save JSON output
            var jsonOutputPath = Path.Combine(context.WorkingDirectory, "metadata.json");
            var jsonData = new
            {
                fileInfo = new
                {
                    fileName = metadata["FileName"],
                    filePath = metadata["FilePath"],
                    fileSize = metadata["FileSize"],
                    fileSizeBytes = metadata["FileSizeBytes"],
                    fileCreatedDate = metadata["FileCreatedDate"],
                    fileModifiedDate = metadata["FileModifiedDate"],
                    fileAccessedDate = metadata["FileAccessedDate"]
                },
                documentProperties = new
                {
                    pageCount = metadata["PageCount"],
                    loadedAt = metadata["LoadedAt"]
                },
                pdfMetadata = new
                {
                    title = metadata["Title"],
                    author = metadata["Author"],
                    subject = metadata["Subject"],
                    keywords = metadata["Keywords"],
                    creator = metadata["Creator"],
                    producer = metadata["Producer"],
                    creationDate = metadata["CreationDate"],
                    modificationDate = metadata["ModDate"]
                },
                extractionTimeMs = extractionTime.TotalMilliseconds,
                note = "PDF metadata fields (Title, Author, etc.) require FPDF_GetMetaText implementation"
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(jsonData, jsonOptions);
            await File.WriteAllTextAsync(jsonOutputPath, json);
            context.Logger.Information("Saved JSON metadata to: {JsonOutputPath}", jsonOutputPath);

            context.Logger.Information("Extraction time: {ExtractionTimeMs} ms", extractionTime.TotalMilliseconds);

            // Count fields that are set vs not set
            var setFields = metadata.Values.Count(v => v is string s && !s.Contains("not implemented") && !s.Contains("(not set)"));
            var totalFields = metadata.Count;
            var notImplementedFields = metadata.Values.Count(v => v is string s && s.Contains("not implemented"));

            // Store outputs for verification
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["JsonOutputFile"] = jsonOutputPath;
            result.Outputs["Metadata"] = metadata;
            result.Outputs["SetFields"] = setFields;
            result.Outputs["TotalFields"] = totalFields;
            result.Outputs["NotImplementedFields"] = notImplementedFields;
            result.Outputs["ExtractionTimeMs"] = extractionTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Metadata extraction test completed successfully. {SetFields}/{TotalFields} fields available, {ExtractionTimeMs:F0} ms",
                setFields,
                totalFields,
                extractionTime.TotalMilliseconds
            );

            if (notImplementedFields > 0)
            {
                context.Logger.Warning(
                    "{NotImplementedFields} metadata fields not yet implemented (require FPDF_GetMetaText support)",
                    notImplementedFields
                );
            }
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Metadata extraction test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the metadata extraction test produced expected outputs.
    /// Checks that extraction executed successfully and output files exist.
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

        // Verify report file exists
        if (!result.Outputs.TryGetValue("ReportFile", out var reportFileObj) ||
            reportFileObj is not string reportFile ||
            !File.Exists(reportFile))
        {
            return Task.FromResult(false);
        }

        // Verify JSON output file exists
        if (!result.Outputs.TryGetValue("JsonOutputFile", out var jsonFileObj) ||
            jsonFileObj is not string jsonFile ||
            !File.Exists(jsonFile))
        {
            return Task.FromResult(false);
        }

        // Verify metadata dictionary exists
        if (!result.Outputs.TryGetValue("Metadata", out var metadataObj) ||
            metadataObj is not Dictionary<string, object> metadata)
        {
            return Task.FromResult(false);
        }

        // Verify essential fields are present
        var requiredFields = new[] { "FilePath", "FileName", "FileSize", "PageCount" };
        if (!requiredFields.All(field => metadata.ContainsKey(field)))
        {
            return Task.FromResult(false);
        }

        // Verify field counts are valid
        if (!result.Outputs.TryGetValue("SetFields", out var setFieldsObj) ||
            setFieldsObj is not int setFields ||
            setFields < 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("TotalFields", out var totalFieldsObj) ||
            totalFieldsObj is not int totalFields ||
            totalFields <= 0 ||
            setFields > totalFields)
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
            var json = File.ReadAllText(jsonFile);
            JsonDocument.Parse(json);
        }
        catch
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
