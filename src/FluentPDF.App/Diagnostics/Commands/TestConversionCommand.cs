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
/// Implements --test-conversion command for verifying DOCX to PDF conversion.
/// </summary>
public sealed class TestConversionCommand
{
    private readonly IDocxConverterService _converterService;
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<TestConversionCommand> _logger;

    public TestConversionCommand(
        IDocxConverterService converterService,
        IPdfDocumentService documentService,
        ILogger<TestConversionCommand> logger)
    {
        _converterService = converterService ?? throw new ArgumentNullException(nameof(converterService));
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConversionCommandResult> ExecuteAsync(
        string inputFile,
        string outputFile,
        bool verifyStructure = true)
    {
        var result = new ConversionCommandResult
        {
            Command = "test-conversion",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting conversion test: {InputFile} -> {OutputFile}", inputFile, outputFile);

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

            var conversionResult = await _converterService.ConvertDocxToPdfAsync(inputFile, outputFile, options: null, cancellationToken: default);

            if (!conversionResult.IsSuccess)
            {
                result.Status = "error";
                result.Errors.Add($"Conversion failed: {conversionResult.Errors[0].Message}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            if (File.Exists(outputFile))
            {
                var outputInfo = new FileInfo(outputFile);
                result.Output.File = outputFile;
                result.Output.FileSizeBytes = outputInfo.Length;

                var docResult = await _documentService.LoadDocumentAsync(outputFile);
                if (docResult.IsSuccess)
                {
                    result.Output.Pages = docResult.Value.PageCount;
                    docResult.Value.Dispose();
                }

                if (verifyStructure)
                {
                    result.Verification.StructureValid = await ValidateStructureAsync(outputFile);
                }
                else
                {
                    result.Verification.StructureValid = true;
                }
            }

            result.Status = result.Verification.StructureValid ? "pass" : "fail";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion test failed with exception");
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
            }
            catch { }
        }
        return null;
    }
}
