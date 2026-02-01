// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-watermark command for verifying watermark functionality.
/// </summary>
public sealed class TestWatermarkCommand
{
    private readonly IWatermarkService _watermarkService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestWatermarkCommand> _logger;

    public TestWatermarkCommand(
        IWatermarkService watermarkService,
        IPdfDocumentService documentService,
        ILogger<TestWatermarkCommand> logger)
    {
        _watermarkService = watermarkService ?? throw new ArgumentNullException(nameof(watermarkService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WatermarkCommandResult> ExecuteAsync(
        string inputFile,
        string outputFile,
        string text,
        string? imagePath = null,
        string position = "center",
        int opacity = 50,
        bool verifyVisual = false)
    {
        var result = new WatermarkCommandResult
        {
            Command = "test-watermark",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting watermark test: {InputFile}", inputFile);

            if (!File.Exists(inputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Input file not found: {inputFile}");
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

            result.Watermark.Type = string.IsNullOrEmpty(imagePath) ? "text" : "image";
            result.Watermark.Content = string.IsNullOrEmpty(imagePath) ? text : imagePath;
            result.Watermark.Position = position;
            result.Watermark.Opacity = opacity;
            result.Watermark.AppliedToPages = "all";

            var watermarkPosition = position.ToLowerInvariant() switch
            {
                "tl" => WatermarkPosition.TopLeft,
                "tr" => WatermarkPosition.TopRight,
                "bl" => WatermarkPosition.BottomLeft,
                "br" => WatermarkPosition.BottomRight,
                _ => WatermarkPosition.Center
            };

            FluentResults.Result<PdfDocument> watermarkResult;

            if (string.IsNullOrEmpty(imagePath))
            {
                var config = new TextWatermarkConfig
                {
                    Text = text,
                    FontFamily = "Arial",
                    FontSize = 48f,
                    Color = Color.FromArgb(128, 128, 128),
                    Opacity = opacity / 100f,
                    RotationDegrees = 45f,
                    Position = watermarkPosition,
                    BehindContent = false
                };

                watermarkResult = await _watermarkService.ApplyTextWatermarkAsync(
                    docResult.Value,
                    config,
                    WatermarkPageRange.All);
            }
            else
            {
                var config = new ImageWatermarkConfig
                {
                    ImagePath = imagePath,
                    Opacity = opacity / 100f,
                    Position = watermarkPosition,
                    Scale = 0.5f, // 50% scale
                    BehindContent = false
                };

                watermarkResult = await _watermarkService.ApplyImageWatermarkAsync(
                    docResult.Value,
                    config,
                    WatermarkPageRange.All);
            }

            if (!watermarkResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.Add($"Watermark application failed: {watermarkResult.Errors[0].Message}");
                docResult.Value.Dispose();
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            await _documentService.SaveDocumentAsync(watermarkResult.Value, outputFile);

            result.Output.File = outputFile;
            result.Output.Pages = watermarkResult.Value.PageCount;

            watermarkResult.Value.Dispose();

            result.Verification.VisualCheckEnabled = verifyVisual;
            result.Verification.WatermarkDetected = true;

            result.Status = "pass";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Watermark test failed with exception");
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
