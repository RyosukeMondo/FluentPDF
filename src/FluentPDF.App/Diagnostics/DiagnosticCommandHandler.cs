// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Commands;
using FluentPDF.App.Diagnostics.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics;

/// <summary>
/// Entry point and router for all diagnostic CLI commands.
/// Routes commands to appropriate handlers and manages JSON report output.
/// </summary>
public sealed class DiagnosticCommandRouter
{
    private readonly ILogger<DiagnosticCommandRouter> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DiagnosticCommandRouter(
        ILogger<DiagnosticCommandRouter> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Routes to the appropriate test command and returns exit code.
    /// </summary>
    public async Task<int> RouteCommandAsync(CommandLineOptions options)
    {
        try
        {
            CommandResult? result = null;
            var outputFormat = DetermineOutputFormat(options);

            if (!string.IsNullOrEmpty(options.TestMerge))
            {
                var command = CreateCommand<TestMergeCommand>();
                var mergeOptions = ParseMergeOptions(options);
                result = await command.ExecuteAsync(
                    mergeOptions.InputFiles,
                    mergeOptions.OutputPath,
                    mergeOptions.VerifyStructure,
                    mergeOptions.VerifyPages);
            }
            else if (!string.IsNullOrEmpty(options.TestSplit))
            {
                var command = CreateCommand<TestSplitCommand>();
                var splitOptions = ParseSplitOptions(options);
                result = await command.ExecuteAsync(
                    splitOptions.InputFile,
                    splitOptions.Ranges,
                    splitOptions.OutputDir,
                    splitOptions.VerifyStructure);
            }
            else if (!string.IsNullOrEmpty(options.TestOptimize))
            {
                var command = CreateCommand<TestOptimizeCommand>();
                var optimizeOptions = ParseOptimizeOptions(options);
                result = await command.ExecuteAsync(
                    optimizeOptions.InputFile,
                    optimizeOptions.OutputFile,
                    optimizeOptions.MinReduction,
                    optimizeOptions.VerifyVisual);
            }
            else if (!string.IsNullOrEmpty(options.TestExportImages))
            {
                var command = CreateCommand<TestExportImagesCommand>();
                var exportOptions = ParseExportImagesOptions(options);
                result = await command.ExecuteAsync(
                    exportOptions.InputFile,
                    exportOptions.OutputDirectory,
                    exportOptions.Format,
                    exportOptions.Dpi,
                    exportOptions.Quality,
                    exportOptions.PageRange);
            }
            else if (!string.IsNullOrEmpty(options.TestWatermark))
            {
                var command = CreateCommand<TestWatermarkCommand>();
                var watermarkOptions = ParseWatermarkOptions(options);
                result = await command.ExecuteAsync(
                    watermarkOptions.InputFile,
                    watermarkOptions.OutputFile,
                    watermarkOptions.Text,
                    watermarkOptions.ImagePath,
                    watermarkOptions.Position,
                    watermarkOptions.Opacity,
                    watermarkOptions.VerifyVisual);
            }
            else if (!string.IsNullOrEmpty(options.TestEncrypt))
            {
                var command = CreateCommand<TestEncryptCommand>();
                var encryptOptions = ParseEncryptOptions(options);
                result = await command.ExecuteAsync(
                    encryptOptions.InputFile,
                    encryptOptions.OutputFile,
                    encryptOptions.UserPassword,
                    encryptOptions.OwnerPassword,
                    encryptOptions.Strength,
                    encryptOptions.AllowPrint,
                    encryptOptions.AllowCopy,
                    encryptOptions.AllowModify,
                    encryptOptions.AllowAnnotate);
            }
            else if (!string.IsNullOrEmpty(options.TestAnnotationsCmd))
            {
                var command = CreateCommand<TestAnnotationsCommand>();
                var annotationsOptions = ParseAnnotationsOptions(options);
                result = await command.ExecuteAsync(
                    annotationsOptions.InputFile,
                    annotationsOptions.AnnotationsJson,
                    annotationsOptions.OutputFile,
                    annotationsOptions.VerifyPersistence);
            }
            else if (!string.IsNullOrEmpty(options.TestFormsCmd))
            {
                var command = CreateCommand<TestFormsCommand>();
                var formsOptions = ParseFormsOptions(options);
                result = await command.ExecuteAsync(
                    formsOptions.InputFile,
                    formsOptions.DataJson,
                    formsOptions.OutputFile,
                    formsOptions.VerifyValidation,
                    formsOptions.VerifyPersistence);
            }
            else if (!string.IsNullOrEmpty(options.TestStamp))
            {
                var command = CreateCommand<TestStampCommand>();
                var stampOptions = ParseStampOptions(options);
                result = await command.ExecuteAsync(
                    stampOptions.InputFile,
                    stampOptions.StampType,
                    stampOptions.OutputFile,
                    stampOptions.PageNumber,
                    stampOptions.PositionX,
                    stampOptions.PositionY);
            }
            else if (!string.IsNullOrEmpty(options.TestConversion))
            {
                var command = CreateCommand<TestConversionCommand>();
                var conversionOptions = ParseConversionOptions(options);
                result = await command.ExecuteAsync(
                    conversionOptions.InputFile,
                    conversionOptions.OutputFile,
                    conversionOptions.VerifyStructure);
            }

            if (result != null)
            {
                await WriteReportAsync(result, options.OutputDirectory ?? ".", outputFormat);
                return result.GetExitCode();
            }

            _logger.LogWarning("No matching diagnostic command found");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command routing failed");
            Console.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    private T CreateCommand<T>() where T : class
    {
        var command = _serviceProvider.GetService(typeof(T)) as T;
        if (command == null)
        {
            throw new InvalidOperationException($"Command {typeof(T).Name} not registered in DI container");
        }
        return command;
    }

    private async Task WriteReportAsync(CommandResult result, string outputDir, string format)
    {
        try
        {
            Directory.CreateDirectory(outputDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{result.Command}_{timestamp}.{format}";
            var filePath = Path.Combine(outputDir, fileName);

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"Report written to: {filePath}");

            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine($"  Command: {result.Command}");
            Console.WriteLine($"  Status: {result.Status}");
            Console.WriteLine($"  Duration: {result.DurationMs}ms");
            if (result.Errors.Count > 0)
            {
                Console.WriteLine($"  Errors: {result.Errors.Count}");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"    - {error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write report");
        }
    }

    private string DetermineOutputFormat(CommandLineOptions options)
    {
        if (!string.IsNullOrEmpty(options.JunitOutput)) return "xml";
        return "json";
    }

    private MergeOptions ParseMergeOptions(CommandLineOptions options)
    {
        return new MergeOptions
        {
            InputFiles = options.TestMerge ?? string.Empty,
            OutputPath = options.OutputPath ?? "merged.pdf",
            VerifyStructure = options.VerifyStructure,
            VerifyPages = options.VerifyPages
        };
    }

    private SplitOptions ParseSplitOptions(CommandLineOptions options)
    {
        return new SplitOptions
        {
            InputFile = options.TestSplit ?? string.Empty,
            Ranges = options.SplitRanges ?? "1-5",
            OutputDir = options.OutputDirectory ?? ".",
            VerifyStructure = options.VerifyStructure
        };
    }

    private OptimizeOptions ParseOptimizeOptions(CommandLineOptions options)
    {
        return new OptimizeOptions
        {
            InputFile = options.TestOptimize ?? string.Empty,
            OutputFile = options.OutputPath ?? "optimized.pdf",
            MinReduction = options.MinReduction,
            VerifyVisual = options.VerifyVisual
        };
    }

    private WatermarkOptions ParseWatermarkOptions(CommandLineOptions options)
    {
        return new WatermarkOptions
        {
            InputFile = options.TestWatermark ?? string.Empty,
            OutputFile = options.OutputPath ?? "watermarked.pdf",
            Text = options.WatermarkText ?? "CONFIDENTIAL",
            ImagePath = options.WatermarkImage,
            Position = options.WatermarkPosition ?? "center",
            Opacity = (int)(options.WatermarkOpacity * 100),
            VerifyVisual = options.VerifyVisual
        };
    }

    private EncryptOptions ParseEncryptOptions(CommandLineOptions options)
    {
        return new EncryptOptions
        {
            InputFile = options.TestEncrypt ?? string.Empty,
            OutputFile = options.OutputPath ?? "encrypted.pdf",
            UserPassword = options.EncryptUserPassword,
            OwnerPassword = options.EncryptOwnerPassword ?? "owner",
            Strength = options.EncryptionStrength,
            AllowPrint = options.AllowPrint,
            AllowCopy = options.AllowCopy,
            AllowModify = options.AllowModify,
            AllowAnnotate = options.AllowAnnotate
        };
    }

    private AnnotationsOptions ParseAnnotationsOptions(CommandLineOptions options)
    {
        return new AnnotationsOptions
        {
            InputFile = options.TestAnnotationsCmd ?? string.Empty,
            AnnotationsJson = options.AnnotationsJsonPath ?? "annotations.json",
            OutputFile = options.OutputPath ?? "annotated.pdf",
            VerifyPersistence = options.VerifyPersistence
        };
    }

    private FormsOptions ParseFormsOptions(CommandLineOptions options)
    {
        return new FormsOptions
        {
            InputFile = options.TestFormsCmd ?? string.Empty,
            DataJson = options.FormDataJsonPath ?? "formdata.json",
            OutputFile = options.OutputPath ?? "filled.pdf",
            VerifyValidation = options.VerifyValidation,
            VerifyPersistence = options.VerifyPersistence
        };
    }

    private StampOptions ParseStampOptions(CommandLineOptions options)
    {
        return new StampOptions
        {
            InputFile = options.TestStamp ?? string.Empty,
            StampType = options.StampType ?? "Approved",
            OutputFile = options.OutputPath ?? "stamped.pdf",
            PageNumber = 0,
            PositionX = 100f,
            PositionY = 100f
        };
    }

    private ConversionOptions ParseConversionOptions(CommandLineOptions options)
    {
        return new ConversionOptions
        {
            InputFile = options.TestConversion ?? string.Empty,
            OutputFile = options.OutputPath ?? "converted.pdf",
            VerifyStructure = options.VerifyStructure
        };
    }

    private ExportImagesOptions ParseExportImagesOptions(CommandLineOptions options)
    {
        return new ExportImagesOptions
        {
            InputFile = options.TestExportImages ?? string.Empty,
            OutputDirectory = options.OutputDirectory ?? "exported_images",
            Format = options.ExportImageFormat ?? "png",
            Dpi = options.ExportImageDpi,
            Quality = options.ExportImageQuality,
            PageRange = options.ExportImagePageRange ?? "all"
        };
    }

    private record MergeOptions
    {
        public string InputFiles { get; init; } = string.Empty;
        public string OutputPath { get; init; } = string.Empty;
        public bool VerifyStructure { get; init; } = true;
        public bool VerifyPages { get; init; } = true;
    }

    private record SplitOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string Ranges { get; init; } = string.Empty;
        public string OutputDir { get; init; } = string.Empty;
        public bool VerifyStructure { get; init; } = true;
    }

    private record OptimizeOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public double MinReduction { get; init; } = 10.0;
        public bool VerifyVisual { get; init; }
    }

    private record WatermarkOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public string? ImagePath { get; init; }
        public string Position { get; init; } = "center";
        public int Opacity { get; init; } = 50;
        public bool VerifyVisual { get; init; }
    }

    private record EncryptOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public string? UserPassword { get; init; }
        public string OwnerPassword { get; init; } = string.Empty;
        public int Strength { get; init; } = 256;
        public bool AllowPrint { get; init; } = true;
        public bool AllowCopy { get; init; } = true;
        public bool AllowModify { get; init; } = true;
        public bool AllowAnnotate { get; init; } = true;
    }

    private record AnnotationsOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string AnnotationsJson { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public bool VerifyPersistence { get; init; } = true;
    }

    private record FormsOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string DataJson { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public bool VerifyValidation { get; init; } = true;
        public bool VerifyPersistence { get; init; } = true;
    }

    private record StampOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string StampType { get; init; } = "Approved";
        public string OutputFile { get; init; } = string.Empty;
        public int PageNumber { get; init; } = 0;
        public float PositionX { get; init; } = 100f;
        public float PositionY { get; init; } = 100f;
    }

    private record ConversionOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string OutputFile { get; init; } = string.Empty;
        public bool VerifyStructure { get; init; } = true;
    }

    private record ExportImagesOptions
    {
        public string InputFile { get; init; } = string.Empty;
        public string OutputDirectory { get; init; } = string.Empty;
        public string Format { get; init; } = "png";
        public int Dpi { get; init; } = 150;
        public int Quality { get; init; } = 90;
        public string PageRange { get; init; } = "all";
    }
}
