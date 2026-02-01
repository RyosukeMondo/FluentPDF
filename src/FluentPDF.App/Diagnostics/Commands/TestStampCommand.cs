using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-stamp command for verifying stamp annotations.
/// </summary>
public sealed class TestStampCommand
{
    private readonly IPdfDocumentService _documentService;
    private readonly IAnnotationService _annotationService;
    private readonly IStampService _stampService;
    private readonly ILogger<TestStampCommand> _logger;

    public TestStampCommand(
        IPdfDocumentService documentService,
        IAnnotationService annotationService,
        IStampService stampService,
        ILogger<TestStampCommand> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _stampService = stampService ?? throw new ArgumentNullException(nameof(stampService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StampCommandResult> ExecuteAsync(
        string inputFile,
        string stampType,
        string outputFile,
        int pageNumber = 0,
        float positionX = 100f,
        float positionY = 100f)
    {
        var result = new StampCommandResult
        {
            Command = "test-stamp",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting stamp test: Input={InputFile}, StampType={StampType}, Output={OutputFile}",
                inputFile, stampType, outputFile);

            if (!File.Exists(inputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Input file not found: {inputFile}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Load document
            var docResult = await _documentService.LoadDocumentAsync(inputFile);
            if (!docResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.Add($"Failed to load document: {docResult.Errors[0].Message}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var document = docResult.Value;
            result.Input.File = inputFile;
            result.Input.Pages = document.PageCount;
            result.Input.StampType = stampType;

            // Validate page number
            if (pageNumber < 0 || pageNumber >= document.PageCount)
            {
                result.Status = "error";
                result.Errors.Add($"Invalid page number: {pageNumber}. Document has {document.PageCount} pages.");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Parse stamp type
            if (!Enum.TryParse<StampType>(stampType, ignoreCase: true, out var parsedStampType))
            {
                result.Status = "error";
                result.Errors.Add($"Unknown stamp type: {stampType}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Create stamp
            var stamp = _stampService.CreateStamp(parsedStampType);
            stamp.Author = "TestStamp";

            // Apply dynamic replacements
            _stampService.ApplyDynamicReplacements(stamp);

            // Apply stamp to document
            var stampResult = await _stampService.ApplyStampAsync(
                document,
                stamp,
                pageNumber,
                new System.Drawing.PointF(positionX, positionY));

            if (!stampResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.AddRange(stampResult.Errors.Select(e => e.Message));
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var annotation = stampResult.Value;

            // Add the annotation to the document
            var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
            if (!createResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.AddRange(createResult.Errors.Select(e => e.Message));
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Save the document
            var saveResult = await _annotationService.SaveAnnotationsAsync(document, outputFile, createBackup: false);
            if (!saveResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.AddRange(saveResult.Errors.Select(e => e.Message));
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            stopwatch.Stop();

            result.Status = "success";
            result.Output.File = outputFile;
            result.Output.StampId = annotation.Id;
            result.Output.StampAppliedToPage = pageNumber;
            result.Output.StampPosition = $"({positionX}, {positionY})";
            result.Output.StampOpacity = stamp.Opacity;
            result.Output.StampRotation = stamp.RotationAngle;
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "Stamp test completed successfully. StampId={StampId}, Page={Page}, Duration={DurationMs}ms",
                annotation.Id, pageNumber, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Status = "error";
            result.Errors.Add($"Unexpected error: {ex.Message}");
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Stamp test failed");
            return result;
        }
    }
}

/// <summary>
/// Result model for stamp command execution.
/// </summary>
public class StampCommandResult : CommandResult
{
    public StampInput Input { get; set; } = new();
    public StampOutput Output { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "pass")
        {
            return 0;
        }

        if (Errors.Exists(e => e.Contains("not found")))
        {
            return 1; // File not found
        }

        return 2; // Stamp failed
    }
}

public class StampInput
{
    public string File { get; set; } = string.Empty;
    public int Pages { get; set; }
    public string StampType { get; set; } = string.Empty;
}

public class StampOutput
{
    public string File { get; set; } = string.Empty;
    public string StampId { get; set; } = string.Empty;
    public int StampAppliedToPage { get; set; }
    public string StampPosition { get; set; } = string.Empty;
    public double StampOpacity { get; set; }
    public float StampRotation { get; set; }
}
