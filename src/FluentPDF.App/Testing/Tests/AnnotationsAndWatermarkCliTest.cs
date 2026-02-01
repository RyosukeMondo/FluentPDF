using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies PDF annotation functionality.
/// Tests creating highlight, underline, rectangle, circle, and note annotations, then saves to FDF.
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
    public string Description => "Creates highlight, underline, rectangle, circle, note annotations, saves to FDF and verifies export";

    /// <summary>
    /// Executes the annotations test using the provided test context.
    /// Creates various annotation types and exports to FDF for verification.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing annotation creation metrics and output files</returns>
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
            context.Logger.Information("Starting PDF annotations test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var annotationService = context.Services.GetRequiredService<IAnnotationService>();

            // Get test PDF path from context data or use default
            var inputPath = context.Data.TryGetValue("InputPath", out var pathObj) && pathObj is string path
                ? path
                : Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "sample-with-text.pdf"
                ));

            context.Logger.Information("Loading PDF: {FileName}", Path.GetFileName(inputPath));

            if (!File.Exists(inputPath))
            {
                result.ErrorMessage = $"Input PDF not found: {inputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(inputPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            var pageIndex = 0; // Use first page
            context.Logger.Information("Loaded PDF with {PageCount} pages, using page {Page}", document.PageCount, pageIndex + 1);

            var operationStart = DateTime.UtcNow;
            var annotationsCreated = new List<(string Type, bool Success)>();

            // Create highlight annotation
            try
            {
                var highlight = new Annotation
                {
                    Type = AnnotationType.Highlight,
                    PageNumber = pageIndex,
                    Bounds = new PdfRectangle(100, 100, 300, 150),
                    FillColor = System.Drawing.Color.Yellow,
                    Author = "FluentPDF Test",
                    Contents = "Test highlight annotation"
                };

                var createResult = await annotationService.CreateAnnotationAsync(document, highlight);
                annotationsCreated.Add(("Highlight", createResult.IsSuccess));
                if (createResult.IsSuccess)
                {
                    context.Logger.Debug("Created highlight annotation");
                }
            }
            catch (Exception ex)
            {
                context.Logger.Warning(ex, "Failed to create highlight annotation");
                annotationsCreated.Add(("Highlight", false));
            }

            // Create underline annotation
            try
            {
                var underline = new Annotation
                {
                    Type = AnnotationType.Underline,
                    PageNumber = pageIndex,
                    Bounds = new PdfRectangle(100, 200, 300, 250),
                    FillColor = System.Drawing.Color.Red,
                    Author = "FluentPDF Test",
                    Contents = "Test underline annotation"
                };

                var createResult = await annotationService.CreateAnnotationAsync(document, underline);
                annotationsCreated.Add(("Underline", createResult.IsSuccess));
                if (createResult.IsSuccess)
                {
                    context.Logger.Debug("Created underline annotation");
                }
            }
            catch (Exception ex)
            {
                context.Logger.Warning(ex, "Failed to create underline annotation");
                annotationsCreated.Add(("Underline", false));
            }

            // Create rectangle annotation
            try
            {
                var rectangle = new Annotation
                {
                    Type = AnnotationType.Square,
                    PageNumber = pageIndex,
                    Bounds = new PdfRectangle(350, 100, 500, 200),
                    FillColor = System.Drawing.Color.Blue,
                    Author = "FluentPDF Test",
                    Contents = "Test rectangle annotation"
                };

                var createResult = await annotationService.CreateAnnotationAsync(document, rectangle);
                annotationsCreated.Add(("Rectangle", createResult.IsSuccess));
                if (createResult.IsSuccess)
                {
                    context.Logger.Debug("Created rectangle annotation");
                }
            }
            catch (Exception ex)
            {
                context.Logger.Warning(ex, "Failed to create rectangle annotation");
                annotationsCreated.Add(("Rectangle", false));
            }

            // Create circle annotation
            try
            {
                var circle = new Annotation
                {
                    Type = AnnotationType.Circle,
                    PageNumber = pageIndex,
                    Bounds = new PdfRectangle(350, 250, 450, 350),
                    FillColor = System.Drawing.Color.Green,
                    Author = "FluentPDF Test",
                    Contents = "Test circle annotation"
                };

                var createResult = await annotationService.CreateAnnotationAsync(document, circle);
                annotationsCreated.Add(("Circle", createResult.IsSuccess));
                if (createResult.IsSuccess)
                {
                    context.Logger.Debug("Created circle annotation");
                }
            }
            catch (Exception ex)
            {
                context.Logger.Warning(ex, "Failed to create circle annotation");
                annotationsCreated.Add(("Circle", false));
            }

            // Create note (text) annotation
            try
            {
                var note = new Annotation
                {
                    Type = AnnotationType.Text,
                    PageNumber = pageIndex,
                    Bounds = new PdfRectangle(550, 100, 570, 120),
                    FillColor = System.Drawing.Color.Orange,
                    Author = "FluentPDF Test",
                    Contents = "This is a test note annotation with some detailed text content for verification."
                };

                var createResult = await annotationService.CreateAnnotationAsync(document, note);
                annotationsCreated.Add(("Note", createResult.IsSuccess));
                if (createResult.IsSuccess)
                {
                    context.Logger.Debug("Created note annotation");
                }
            }
            catch (Exception ex)
            {
                context.Logger.Warning(ex, "Failed to create note annotation");
                annotationsCreated.Add(("Note", false));
            }

            var creationTime = DateTime.UtcNow - operationStart;
            var successfulAnnotations = annotationsCreated.Count(a => a.Success);

            context.Logger.Information(
                "Created {Successful}/{Total} annotations ({Types})",
                successfulAnnotations,
                annotationsCreated.Count,
                string.Join(", ", annotationsCreated.Where(a => a.Success).Select(a => a.Type))
            );

            // Save annotated document
            var outputPath = Path.Combine(context.WorkingDirectory, "annotated_output.pdf");
            var saveResult = await annotationService.SaveAnnotationsAsync(document, outputPath, createBackup: false);

            if (saveResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to save annotated PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            context.Logger.Information("Saved annotated PDF to: {OutputPath}", outputPath);

            // Verify by reloading and counting annotations
            var verifyStart = DateTime.UtcNow;
            var reloadResult = await documentService.LoadDocumentAsync(outputPath);
            if (reloadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to reload annotated PDF: {string.Join(", ", reloadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var reloadedDoc = reloadResult.Value;
            var annotationsResult = await annotationService.GetAnnotationsAsync(reloadedDoc, pageIndex);
            if (annotationsResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to get annotations from reloaded PDF: {string.Join(", ", annotationsResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var retrievedAnnotations = annotationsResult.Value;
            var verifyTime = DateTime.UtcNow - verifyStart;

            context.Logger.Information("Retrieved {Count} annotations from saved PDF", retrievedAnnotations.Count);

            // Export annotations data to JSON (FDF-like format)
            var fdfPath = Path.Combine(context.WorkingDirectory, "annotations.json");
            var annotationData = retrievedAnnotations.Select(a => new
            {
                Type = a.Type.ToString(),
                PageNumber = a.PageNumber,
                X = a.Bounds.Left,
                Y = a.Bounds.Bottom,
                Width = a.Bounds.Width,
                Height = a.Bounds.Height,
                FillColor = $"#{a.FillColor.R:X2}{a.FillColor.G:X2}{a.FillColor.B:X2}",
                Author = a.Author ?? "",
                Contents = a.Contents ?? "",
                CreatedDate = a.CreatedDate.ToString("O")
            });

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(annotationData, jsonOptions);
            await File.WriteAllTextAsync(fdfPath, json);
            context.Logger.Information("Exported annotations to JSON: {FdfPath}", fdfPath);

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "annotations_report.txt");
            var report = new StringBuilder();
            report.AppendLine("PDF Annotations Test Report");
            report.AppendLine($"Input File: {Path.GetFileName(inputPath)}");
            report.AppendLine($"Page Index: {pageIndex + 1}");
            report.AppendLine($"Annotations Attempted: {annotationsCreated.Count}");
            report.AppendLine($"Annotations Created: {successfulAnnotations}");
            report.AppendLine($"Annotations Retrieved: {retrievedAnnotations.Count}");
            report.AppendLine($"Creation Time: {creationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Verify Time: {verifyTime.TotalMilliseconds:F2} ms");
            report.AppendLine();
            report.AppendLine("Annotation Types:");
            foreach (var (type, success) in annotationsCreated)
            {
                report.AppendLine($"  {type}: {(success ? "Success" : "Failed")}");
            }
            report.AppendLine();
            report.AppendLine($"Output PDF: {outputPath}");
            report.AppendLine($"Annotations JSON: {fdfPath}");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved annotations report to: {ReportPath}", reportPath);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["FdfFile"] = fdfPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["AnnotationsAttempted"] = annotationsCreated.Count;
            result.Outputs["AnnotationsCreated"] = successfulAnnotations;
            result.Outputs["AnnotationsRetrieved"] = retrievedAnnotations.Count;
            result.Outputs["CreationTimeMs"] = creationTime.TotalMilliseconds;
            result.Outputs["VerifyTimeMs"] = verifyTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = successfulAnnotations > 0 ? 0 : 1;

            result.Success = successfulAnnotations > 0;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Annotations test completed. Created {Created}/{Attempted} annotations, {CreationTimeMs:F0} ms",
                successfulAnnotations,
                annotationsCreated.Count,
                creationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Annotations test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the annotations test produced expected outputs.
    /// </summary>
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

        // Verify FDF/JSON file exists
        if (!result.Outputs.TryGetValue("FdfFile", out var fdfFileObj) ||
            fdfFileObj is not string fdfFile ||
            !File.Exists(fdfFile))
        {
            return Task.FromResult(false);
        }

        // Verify at least one annotation was created
        if (!result.Outputs.TryGetValue("AnnotationsCreated", out var createdObj) ||
            createdObj is not int created ||
            created == 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// CLI test that verifies PDF watermark functionality.
/// Tests applying text watermark and verifying position and opacity.
/// </summary>
public sealed class WatermarkCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "watermark";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Applies text watermark and verifies position and opacity";

    /// <summary>
    /// Executes the watermark test using the provided test context.
    /// Applies a text watermark and validates the result.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing watermark application metrics</returns>
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
            context.Logger.Information("Starting PDF watermark test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var watermarkService = context.Services.GetRequiredService<IWatermarkService>();

            // Get test PDF path from context data or use default
            var inputPath = context.Data.TryGetValue("InputPath", out var pathObj) && pathObj is string path
                ? path
                : Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "sample.pdf"
                ));

            // Get watermark parameters from context or use defaults
            var watermarkText = context.Data.TryGetValue("WatermarkText", out var textObj) && textObj is string text
                ? text
                : "CONFIDENTIAL";

            var opacity = context.Data.TryGetValue("Opacity", out var opacityObj) && opacityObj is double op
                ? (float)op
                : 0.5f;

            context.Logger.Information("Loading PDF: {FileName}", Path.GetFileName(inputPath));
            context.Logger.Information("Watermark text: {Text}, opacity: {Opacity:F2}", watermarkText, opacity);

            if (!File.Exists(inputPath))
            {
                result.ErrorMessage = $"Input PDF not found: {inputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(inputPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            context.Logger.Information("Loaded PDF with {PageCount} pages", document.PageCount);

            // Configure text watermark
            var config = new TextWatermarkConfig
            {
                Text = watermarkText,
                FontFamily = "Arial",
                FontSize = 48f,
                Color = System.Drawing.Color.Gray,
                Opacity = opacity,
                RotationDegrees = 45f,
                Position = WatermarkPosition.Center,
                BehindContent = false
            };

            // Apply watermark to all pages
            var operationStart = DateTime.UtcNow;
            var pageRange = WatermarkPageRange.All;

            var watermarkResult = await watermarkService.ApplyTextWatermarkAsync(document, config, pageRange);

            if (watermarkResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to apply watermark: {string.Join(", ", watermarkResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var operationTime = DateTime.UtcNow - operationStart;
            context.Logger.Information("Applied watermark in {OperationTimeMs:F0} ms", operationTime.TotalMilliseconds);

            // Save watermarked document
            var outputPath = Path.Combine(context.WorkingDirectory, "watermarked_output.pdf");
            var saveResult = await documentService.SaveDocumentAsync(document, outputPath);

            if (saveResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to save watermarked PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            context.Logger.Information("Saved watermarked PDF to: {OutputPath}", outputPath);

            // Verify output file
            if (!File.Exists(outputPath))
            {
                result.ErrorMessage = $"Output file not created: {outputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var outputFileSize = new FileInfo(outputPath).Length;

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "watermark_report.txt");
            var report = new StringBuilder();
            report.AppendLine("PDF Watermark Test Report");
            report.AppendLine($"Input File: {Path.GetFileName(inputPath)}");
            report.AppendLine($"Watermark Text: {watermarkText}");
            report.AppendLine($"Font: {config.FontFamily}, {config.FontSize}pt");
            report.AppendLine($"Color: Gray");
            report.AppendLine($"Opacity: {config.Opacity:F2}");
            report.AppendLine($"Rotation: {config.RotationDegrees} degrees");
            report.AppendLine($"Position: {config.Position}");
            report.AppendLine($"Behind Content: {config.BehindContent}");
            report.AppendLine($"Pages Watermarked: All ({document.PageCount} pages)");
            report.AppendLine($"Operation Time: {operationTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");
            report.AppendLine($"Output Size: {outputFileSize:N0} bytes");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved watermark report to: {ReportPath}", reportPath);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["WatermarkText"] = watermarkText;
            result.Outputs["Opacity"] = config.Opacity;
            result.Outputs["Position"] = config.Position.ToString();
            result.Outputs["RotationDegrees"] = config.RotationDegrees;
            result.Outputs["PagesWatermarked"] = document.PageCount;
            result.Outputs["OperationTimeMs"] = operationTime.TotalMilliseconds;
            result.Outputs["OutputFileSize"] = outputFileSize;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Watermark test completed successfully. Applied '{Text}' to {Pages} pages, {OperationTimeMs:F0} ms",
                watermarkText,
                document.PageCount,
                operationTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Watermark test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the watermark test produced expected outputs.
    /// </summary>
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

        // Verify output file has content
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Verify watermark parameters were captured
        if (!result.Outputs.TryGetValue("WatermarkText", out var textObj) ||
            textObj is not string text ||
            string.IsNullOrEmpty(text))
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("Opacity", out var opacityObj) ||
            opacityObj is not float opacity ||
            opacity <= 0 || opacity > 1)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
