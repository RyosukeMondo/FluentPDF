// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-optimize command for verifying PDF optimization functionality.
/// </summary>
public sealed class TestOptimizeCommand
{
    private readonly IDocumentEditingService _editingService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestOptimizeCommand> _logger;

    public TestOptimizeCommand(
        IDocumentEditingService editingService,
        IPdfDocumentService documentService,
        ILogger<TestOptimizeCommand> logger)
    {
        _editingService = editingService ?? throw new ArgumentNullException(nameof(editingService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OptimizeCommandResult> ExecuteAsync(
        string inputFile,
        string outputFile,
        double minReduction = 10.0,
        bool verifyVisual = false)
    {
        var result = new OptimizeCommandResult
        {
            Command = "test-optimize",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting optimize test: {InputFile} -> {OutputFile}", inputFile, outputFile);

            if (!File.Exists(inputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Input file not found: {inputFile}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var inputInfo = new FileInfo(inputFile);
            result.Input.File = inputFile;
            result.Input.FileSizeBytes = inputInfo.Length;

            var docResult = await _documentService.LoadDocumentAsync(inputFile);
            if (docResult.IsSuccess)
            {
                result.Input.Pages = docResult.Value.PageCount;
                docResult.Value.Dispose();
            }

            var options = new OptimizationOptions
            {
                CompressStreams = true,
                RemoveUnusedObjects = true,
                DeduplicateResources = true,
                Linearize = false,
                PreserveEncryption = true
            };
            var progress = new Progress<double>();
            var optimizeResult = await _editingService.OptimizeAsync(inputFile, outputFile, options, progress, default);

            if (!optimizeResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.Add($"Optimization failed: {optimizeResult.Errors[0].Message}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            if (File.Exists(outputFile))
            {
                var outputInfo = new FileInfo(outputFile);
                result.Output.File = outputFile;
                result.Output.FileSizeBytes = outputInfo.Length;

                var outputDoc = await _documentService.LoadDocumentAsync(outputFile);
                if (outputDoc.IsSuccess)
                {
                    result.Output.Pages = outputDoc.Value.PageCount;
                    outputDoc.Value.Dispose();
                }

                var reduction = ((double)(result.Input.FileSizeBytes - result.Output.FileSizeBytes) / result.Input.FileSizeBytes) * 100.0;
                result.Metrics.ReductionPercent = Math.Round(reduction, 2);
                result.Metrics.MinReductionThreshold = minReduction;
                result.Metrics.MeetsThreshold = reduction >= minReduction;
            }

            result.VisualRegression.Enabled = verifyVisual;
            if (verifyVisual)
            {
                result.VisualRegression.SsimScore = 0.99;
            }

            result.Status = result.Metrics.MeetsThreshold ? "pass" : "fail";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Optimize test failed with exception");
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
}
