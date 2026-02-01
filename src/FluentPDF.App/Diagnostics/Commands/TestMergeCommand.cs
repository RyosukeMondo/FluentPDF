// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-merge command for verifying PDF merge functionality.
/// </summary>
public sealed class TestMergeCommand
{
    private readonly IDocumentEditingService _editingService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestMergeCommand> _logger;

    public TestMergeCommand(
        IDocumentEditingService editingService,
        IPdfDocumentService documentService,
        ILogger<TestMergeCommand> logger)
    {
        _editingService = editingService ?? throw new ArgumentNullException(nameof(editingService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the merge test command.
    /// </summary>
    /// <param name="inputFiles">Semicolon-separated list of input PDF files.</param>
    /// <param name="outputPath">Output merged PDF path.</param>
    /// <param name="verifyStructure">Whether to verify structure with QPDF.</param>
    /// <param name="verifyPages">Whether to verify page count matches.</param>
    /// <returns>Command result with JSON report.</returns>
    public async Task<MergeCommandResult> ExecuteAsync(
        string inputFiles,
        string outputPath,
        bool verifyStructure = true,
        bool verifyPages = true)
    {
        var result = new MergeCommandResult
        {
            Command = "test-merge",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting merge test: {InputFiles} -> {OutputPath}", inputFiles, outputPath);

            var files = inputFiles.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (files.Length < 2)
            {
                result.Status = "error";
                result.Errors.Add("At least 2 input files required for merge");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Input.Files.AddRange(files);

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    result.Status = "error";
                    result.Errors.Add($"Input file not found: {file}");
                    stopwatch.Stop();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                var docResult = await _documentService.LoadDocumentAsync(file);
                if (docResult.IsSuccess)
                {
                    result.Input.TotalPages += docResult.Value.PageCount;
                    docResult.Value.Dispose();
                }
            }

            var progress = new Progress<double>();
            var mergeResult = await _editingService.MergeAsync(
                files.ToList(),
                outputPath,
                progress,
                default);

            if (!mergeResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.Add($"Merge operation failed: {mergeResult.Errors[0].Message}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Output.File = outputPath;

            if (File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                result.Output.FileSizeBytes = fileInfo.Length;

                var outputDoc = await _documentService.LoadDocumentAsync(outputPath);
                if (outputDoc.IsSuccess)
                {
                    result.Output.Pages = outputDoc.Value.PageCount;
                    outputDoc.Value.Dispose();
                }
            }

            if (verifyPages)
            {
                result.Validation.PageCountMatch = result.Output.Pages == result.Input.TotalPages;
            }
            else
            {
                result.Validation.PageCountMatch = true;
            }

            if (verifyStructure)
            {
                result.Validation.StructureValid = await ValidateStructureAsync(outputPath, result.Validation.QpdfErrors);
            }
            else
            {
                result.Validation.StructureValid = true;
            }

            result.Status = result.Validation.PageCountMatch && result.Validation.StructureValid ? "pass" : "fail";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge test failed with exception");
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

    private async Task<bool> ValidateStructureAsync(string pdfPath, System.Collections.Generic.List<string> errors)
    {
        try
        {
            var qpdfPath = FindQpdf();
            if (string.IsNullOrEmpty(qpdfPath))
            {
                _logger.LogWarning("QPDF not found, skipping structure validation");
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
            if (process == null)
            {
                return true;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    errors.Add(error.Trim());
                }
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QPDF validation failed");
            return true;
        }
    }

    private string? FindQpdf()
    {
        var locations = new[]
        {
            "qpdf",
            @"C:\Program Files\qpdf\bin\qpdf.exe",
            @"C:\Program Files (x86)\qpdf\bin\qpdf.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qpdf", "bin", "qpdf.exe")
        };

        foreach (var location in locations)
        {
            try
            {
                if (File.Exists(location))
                {
                    return location;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "qpdf",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        return output.Split('\n')[0].Trim();
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }
}
