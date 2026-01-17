using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies annotation detection functionality from PDF documents.
/// Tests annotation detection with type verification, metadata reporting, and zero-annotation handling.
/// </summary>
public sealed class AnnotationsCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "annotations";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Detects annotations in a PDF and verifies count, types, and positions";

    /// <summary>
    /// Executes the annotation detection test using the provided test context.
    /// Loads a test PDF, detects annotations, and captures detection metrics.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing annotation counts, types, positions, and metrics</returns>
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
            context.Logger.Information("Starting annotation detection test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var annotationService = context.Services.GetRequiredService<IAnnotationService>();

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

            // Record detection start time
            var detectionStart = DateTime.UtcNow;

            // Detect annotations on all pages
            var allAnnotations = new List<Annotation>();
            var annotationsByPage = new Dictionary<int, List<Annotation>>();

            context.Logger.Information("Detecting annotations across all {PageCount} pages", pageCount);

            for (int pageNum = 0; pageNum < pageCount; pageNum++)
            {
                var annotationsResult = await annotationService.GetAnnotationsAsync(document, pageNum);

                if (annotationsResult.IsFailed)
                {
                    context.Logger.Warning("Failed to get annotations for page {PageNum}: {Error}",
                        pageNum + 1,
                        string.Join(", ", annotationsResult.Errors.Select(e => e.Message)));
                    continue;
                }

                var pageAnnotations = annotationsResult.Value;
                if (pageAnnotations.Count > 0)
                {
                    annotationsByPage[pageNum] = pageAnnotations;
                    allAnnotations.AddRange(pageAnnotations);
                    context.Logger.Information("Page {PageNum}: Found {Count} annotations",
                        pageNum + 1, pageAnnotations.Count);
                }
            }

            var detectionTime = DateTime.UtcNow - detectionStart;

            // Calculate statistics
            var totalAnnotations = allAnnotations.Count;
            var pagesWithAnnotations = annotationsByPage.Count;
            var annotationsByType = allAnnotations
                .GroupBy(a => a.Type)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            context.Logger.Information(
                "Found {TotalAnnotations} annotations on {PagesWithAnnotations}/{PageCount} pages",
                totalAnnotations,
                pagesWithAnnotations,
                pageCount
            );

            // Build detailed annotation report
            var report = new StringBuilder();
            report.AppendLine("Annotation Detection Test Report");
            report.AppendLine($"PDF: {Path.GetFileName(testPdfPath)}");
            report.AppendLine($"Total Annotations: {totalAnnotations}");
            report.AppendLine($"Pages with Annotations: {pagesWithAnnotations}/{pageCount}");
            report.AppendLine();

            if (totalAnnotations == 0)
            {
                report.AppendLine("No annotations found.");
                context.Logger.Information("No annotations found in PDF");
            }
            else
            {
                // Annotations by type summary
                report.AppendLine("Annotations by Type:");
                foreach (var kvp in annotationsByType)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
                report.AppendLine();

                // Annotations by page
                report.AppendLine("Annotations by Page:");
                foreach (var kvp in annotationsByPage.OrderBy(x => x.Key))
                {
                    var pageNum = kvp.Key;
                    var annotations = kvp.Value;
                    report.AppendLine($"Page {pageNum + 1}: {annotations.Count} annotations");

                    foreach (var annotation in annotations.Take(5)) // Limit to 5 per page for readability
                    {
                        report.AppendLine($"  - Type: {annotation.Type}, Bounds: ({annotation.Bounds.Left:F2}, {annotation.Bounds.Top:F2}, {annotation.Bounds.Right:F2}, {annotation.Bounds.Bottom:F2})");
                        if (!string.IsNullOrWhiteSpace(annotation.Contents))
                        {
                            var preview = annotation.Contents.Length > 50
                                ? annotation.Contents.Substring(0, 50) + "..."
                                : annotation.Contents;
                            report.AppendLine($"    Contents: {preview}");
                        }
                    }

                    if (annotations.Count > 5)
                    {
                        report.AppendLine($"  ... and {annotations.Count - 5} more annotations");
                    }
                }
            }

            report.AppendLine();
            report.AppendLine($"Detection Time: {detectionTime.TotalMilliseconds:F2} ms");

            // Save report to file
            var reportPath = Path.Combine(context.WorkingDirectory, "annotations_report.txt");
            await File.WriteAllTextAsync(reportPath, report.ToString(), Encoding.UTF8);
            context.Logger.Information("Saved annotation report to: {ReportPath}", reportPath);

            // Save detailed JSON results
            var jsonOutputPath = Path.Combine(context.WorkingDirectory, "annotations.json");
            var jsonData = new
            {
                totalAnnotations,
                pagesWithAnnotations,
                pageCount,
                annotationsByType,
                annotationsByPage = annotationsByPage.Select(kvp => new
                {
                    pageNumber = kvp.Key + 1, // Convert to 1-based
                    annotationCount = kvp.Value.Count,
                    annotations = kvp.Value.Select(a => new
                    {
                        id = a.Id,
                        type = a.Type.ToString(),
                        bounds = new
                        {
                            left = a.Bounds.Left,
                            top = a.Bounds.Top,
                            right = a.Bounds.Right,
                            bottom = a.Bounds.Bottom,
                            width = a.Bounds.Width,
                            height = a.Bounds.Height
                        },
                        contents = a.Contents,
                        author = a.Author,
                        createdDate = a.CreatedDate,
                        modifiedDate = a.ModifiedDate,
                        opacity = a.Opacity
                    }).ToList()
                }).ToList(),
                detectionTimeMs = detectionTime.TotalMilliseconds
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(jsonData, jsonOptions);
            await File.WriteAllTextAsync(jsonOutputPath, json);
            context.Logger.Information("Saved JSON annotation data to: {JsonOutputPath}", jsonOutputPath);

            context.Logger.Information("Detection time: {DetectionTimeMs} ms", detectionTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["JsonOutputFile"] = jsonOutputPath;
            result.Outputs["TotalAnnotations"] = totalAnnotations;
            result.Outputs["PagesWithAnnotations"] = pagesWithAnnotations;
            result.Outputs["PageCount"] = pageCount;
            result.Outputs["AnnotationsByType"] = annotationsByType;
            result.Outputs["DetectionTimeMs"] = detectionTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = 0; // Success even with zero annotations

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            if (totalAnnotations == 0)
            {
                context.Logger.Information(
                    "Annotation detection test completed successfully. No annotations found, {DetectionTimeMs:F0} ms",
                    detectionTime.TotalMilliseconds
                );
            }
            else
            {
                context.Logger.Information(
                    "Annotation detection test completed successfully. {TotalAnnotations} annotations found, {DetectionTimeMs:F0} ms",
                    totalAnnotations,
                    detectionTime.TotalMilliseconds
                );
            }
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Annotation detection test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the annotation detection test produced expected outputs.
    /// Checks that detection executed successfully and output files exist.
    /// Accepts zero annotations as valid (requirement 4.4).
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

        // Verify total annotations count is valid (can be 0)
        if (!result.Outputs.TryGetValue("TotalAnnotations", out var totalAnnotationsObj) ||
            totalAnnotationsObj is not int totalAnnotations ||
            totalAnnotations < 0)
        {
            return Task.FromResult(false);
        }

        // Verify pages with annotations count is valid
        if (!result.Outputs.TryGetValue("PagesWithAnnotations", out var pagesWithAnnotationsObj) ||
            pagesWithAnnotationsObj is not int pagesWithAnnotations ||
            pagesWithAnnotations < 0)
        {
            return Task.FromResult(false);
        }

        // Verify detection time was captured
        if (!result.Outputs.TryGetValue("DetectionTimeMs", out var detectionTimeObj) ||
            detectionTimeObj is not double detectionTimeMs ||
            detectionTimeMs < 0)
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

        // Verify annotations by type dictionary is valid
        if (!result.Outputs.TryGetValue("AnnotationsByType", out var annotationsByTypeObj) ||
            annotationsByTypeObj is not Dictionary<string, int> annotationsByType)
        {
            return Task.FromResult(false);
        }

        // Verify consistency: if total annotations > 0, pages with annotations > 0
        if (totalAnnotations > 0 && pagesWithAnnotations == 0)
        {
            return Task.FromResult(false);
        }

        // Verify consistency: sum of annotations by type equals total
        if (annotationsByType.Values.Sum() != totalAnnotations)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
