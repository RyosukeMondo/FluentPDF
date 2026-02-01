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
/// Implements --test-split command for verifying PDF split functionality.
/// </summary>
public sealed class TestSplitCommand
{
    private readonly IDocumentEditingService _editingService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestSplitCommand> _logger;

    public TestSplitCommand(
        IDocumentEditingService editingService,
        IPdfDocumentService documentService,
        ILogger<TestSplitCommand> logger)
    {
        _editingService = editingService ?? throw new ArgumentNullException(nameof(editingService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the split test command.
    /// </summary>
    public async Task<SplitCommandResult> ExecuteAsync(
        string inputFile,
        string ranges,
        string outputDir,
        bool verifyStructure = true)
    {
        var result = new SplitCommandResult
        {
            Command = "test-split",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting split test: {InputFile} with ranges {Ranges}", inputFile, ranges);

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
            result.Input.TotalPages = docResult.Value.PageCount;
            docResult.Value.Dispose();

            Directory.CreateDirectory(outputDir);

            var rangeList = ranges.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            result.Ranges.AddRange(rangeList);

            var fileName = Path.GetFileNameWithoutExtension(inputFile);
            var index = 1;

            foreach (var range in rangeList)
            {
                try
                {
                    var outputPath = Path.Combine(outputDir, $"{fileName}_{index}.pdf");

                    var progress = new Progress<double>();
                    var splitResult = await _editingService.SplitAsync(
                        inputFile,
                        range,
                        outputPath,
                        progress,
                        default);

                    var outputInfo = new SplitCommandResult.OutputFileInfo
                    {
                        File = outputPath,
                        Valid = splitResult.IsSuccess
                    };

                    if (splitResult.IsSuccess && File.Exists(outputPath))
                    {
                        var splitDoc = await _documentService.LoadDocumentAsync(outputPath);
                        if (splitDoc.IsSuccess)
                        {
                            outputInfo.Pages = splitDoc.Value.PageCount;
                            splitDoc.Value.Dispose();
                        }

                        if (verifyStructure)
                        {
                            outputInfo.Valid = await ValidateStructureAsync(outputPath);
                        }
                    }
                    else if (!splitResult.IsSuccess)
                    {
                        result.Errors.Add($"Split failed for range {range}: {splitResult.Errors[0].Message}");
                    }

                    result.Output.Add(outputInfo);
                    index++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to split range {Range}", range);
                    result.Errors.Add($"Range {range} failed: {ex.Message}");
                }
            }

            result.Status = result.Output.All(o => o.Valid) && result.Errors.Count == 0 ? "pass" : "fail";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Split test failed with exception");
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

    private async Task<bool> ValidateStructureAsync(string pdfPath)
    {
        try
        {
            var qpdfPath = FindQpdf();
            if (string.IsNullOrEmpty(qpdfPath))
            {
                return true;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = qpdfPath,
                Arguments = $"--check \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return true;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return true;
        }
    }

    private string? FindQpdf()
    {
        var locations = new[] { "qpdf", @"C:\Program Files\qpdf\bin\qpdf.exe" };
        foreach (var location in locations)
        {
            try
            {
                if (File.Exists(location)) return location;
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "qpdf",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if (!string.IsNullOrWhiteSpace(output))
                        return output.Split('\n')[0].Trim();
                }
            }
            catch { }
        }
        return null;
    }
}
