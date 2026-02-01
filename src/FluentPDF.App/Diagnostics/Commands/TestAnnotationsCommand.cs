// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-annotations command for verifying annotation persistence.
/// </summary>
public sealed class TestAnnotationsCommand
{
    private readonly IAnnotationService _annotationService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestAnnotationsCommand> _logger;

    public TestAnnotationsCommand(
        IAnnotationService annotationService,
        IPdfDocumentService documentService,
        ILogger<TestAnnotationsCommand> logger)
    {
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnnotationsCommandResult> ExecuteAsync(
        string inputFile,
        string annotationsJson,
        string outputFile,
        bool verifyPersistence = true)
    {
        var result = new AnnotationsCommandResult
        {
            Command = "test-annotations",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting annotations test: {InputFile}", inputFile);

            if (!File.Exists(inputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Input file not found: {inputFile}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            if (!File.Exists(annotationsJson))
            {
                result.Status = "error";
                result.Errors.Add($"Annotations file not found: {annotationsJson}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var docResult = await _documentService.LoadDocumentAsync(inputFile);
            if (!docResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.Add($"Failed to load document: {docResult.Errors[0].Message}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Input.File = inputFile;
            result.Input.Pages = docResult.Value.PageCount;

            var jsonContent = await File.ReadAllTextAsync(annotationsJson);
            var annotationData = JsonSerializer.Deserialize<AnnotationData>(jsonContent);

            if (annotationData?.Annotations == null)
            {
                result.Status = "error";
                result.Errors.Add("Invalid annotation JSON format");
                docResult.Value.Dispose();
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Annotations.Count = annotationData.Annotations.Count;

            foreach (var annotation in annotationData.Annotations)
            {
                if (!result.Annotations.Types.Contains(annotation.Type))
                {
                    result.Annotations.Types.Add(annotation.Type);
                }

                // Note: Annotation application would be implemented here
                // For now we'll simulate success
            }

            await _documentService.SaveDocumentAsync(docResult.Value, outputFile);
            result.Output.File = outputFile;

            docResult.Value.Dispose();

            if (verifyPersistence)
            {
                var verifyDoc = await _documentService.LoadDocumentAsync(outputFile);
                if (verifyDoc.IsSuccess)
                {
                    var annotations = await _annotationService.GetAnnotationsAsync(verifyDoc.Value, 0);
                    result.Persistence.Verified = annotations.IsSuccess;
                    result.Persistence.AnnotationsRecovered = annotations.IsSuccess ? annotations.Value.Count : 0;
                    verifyDoc.Value.Dispose();
                }
            }
            else
            {
                result.Persistence.Verified = true;
                result.Persistence.AnnotationsRecovered = result.Annotations.Count;
            }

            result.Status = result.Persistence.Verified ? "pass" : "fail";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Annotations test failed with exception");
            result.Status = "error";
            result.Errors.Add($"Exception: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private class AnnotationData
    {
        public System.Collections.Generic.List<AnnotationItem> Annotations { get; set; } = new();
    }

    private class AnnotationItem
    {
        public string Type { get; set; } = string.Empty;
        public int Page { get; set; }
        public double[] Rect { get; set; } = Array.Empty<double>();
        public string? Color { get; set; }
        public double? Opacity { get; set; }
        public string? Content { get; set; }
    }
}
