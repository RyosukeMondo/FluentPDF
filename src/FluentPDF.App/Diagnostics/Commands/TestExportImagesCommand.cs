// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-export-images command for verifying image export functionality.
/// </summary>
public sealed class TestExportImagesCommand
{
    private readonly IImageExportService _exportService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestExportImagesCommand> _logger;

    public TestExportImagesCommand(
        IImageExportService exportService,
        IPdfDocumentService documentService,
        ILogger<TestExportImagesCommand> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExportImagesCommandResult> ExecuteAsync(
        string inputFile,
        string outputDirectory,
        string format = "png",
        int dpi = 150,
        int quality = 90,
        string pageRange = "all")
    {
        var result = new ExportImagesCommandResult
        {
            Command = "test-export-images",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting export images test: {InputFile} -> {OutputDirectory} (format={Format}, dpi={Dpi}, quality={Quality}, pages={PageRange})",
                inputFile, outputDirectory, format, dpi, quality, pageRange);

            // Validate input file
            if (!File.Exists(inputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Input file not found: {inputFile}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Input.File = inputFile;
            var inputInfo = new FileInfo(inputFile);
            result.Input.FileSizeBytes = inputInfo.Length;

            // Load document
            var docResult = await _documentService.LoadDocumentAsync(inputFile);
            if (docResult.IsFailed)
            {
                result.Status = "error";
                result.Errors.Add($"Failed to load document: {docResult.Errors[0].Message}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            var document = docResult.Value;
            result.Input.Pages = document.PageCount;

            try
            {
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                result.Output.Directory = outputDirectory;

                // Configure export options
                var imageFormat = format.ToLowerInvariant() == "jpg" || format.ToLowerInvariant() == "jpeg"
                    ? ImageFormat.Jpg
                    : ImageFormat.Png;

                var options = new ImageExportOptions
                {
                    Format = imageFormat,
                    Dpi = dpi,
                    JpegQuality = quality,
                    PageRange = pageRange,
                    OverwriteExisting = true
                };

                // Export images
                var exportResult = await _exportService.ExportAsync(
                    document,
                    outputDirectory,
                    options,
                    progress: null,
                    cancellationToken: default);

                if (exportResult.IsFailed)
                {
                    result.Status = "error";
                    result.Errors.Add($"Export failed: {exportResult.Errors[0].Message}");
                    stopwatch.Stop();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                var exportData = exportResult.Value;

                // Populate output information
                result.Output.Files = exportData.ExportedFiles.ToList();
                result.Output.FileCount = exportData.PageCount;
                result.Output.TotalSizeBytes = exportData.TotalSize;

                // Populate metrics
                result.Metrics.Format = format;
                result.Metrics.Dpi = dpi;
                result.Metrics.JpegQuality = imageFormat == ImageFormat.Jpg ? quality : 0;
                result.Metrics.PageRange = pageRange;
                result.Metrics.PagesExported = exportData.PageCount;
                result.Metrics.AverageSizeBytes = exportData.TotalSize / exportData.PageCount;
                result.Metrics.ProcessingTimeMs = exportData.ProcessingTime.TotalMilliseconds;

                // Verify all files exist
                var allFilesExist = exportData.ExportedFiles.All(File.Exists);
                result.Validation.AllFilesExist = allFilesExist;

                if (!allFilesExist)
                {
                    result.Status = "fail";
                    result.Errors.Add("Not all exported files exist on disk");
                }
                else
                {
                    result.Status = "pass";
                }

                _logger.LogInformation(
                    "Export images test completed: exported {Count} pages, total size {Size} bytes",
                    exportData.PageCount, exportData.TotalSize);
            }
            finally
            {
                document.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export images test failed with exception");
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
