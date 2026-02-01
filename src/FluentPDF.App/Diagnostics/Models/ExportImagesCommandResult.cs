// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result model for --test-export-images command.
/// </summary>
public sealed class ExportImagesCommandResult : CommandResult
{
    public InputInfo Input { get; set; } = new();
    public OutputInfo Output { get; set; } = new();
    public MetricsInfo Metrics { get; set; } = new();
    public ValidationInfo Validation { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "error")
        {
            if (Errors.Count > 0 && Errors[0].Contains("not found"))
                return 1;
            if (Errors.Count > 0 && Errors[0].Contains("export"))
                return 2;
        }

        if (!Validation.AllFilesExist)
            return 3;

        if (Output.FileCount != Metrics.PagesExported)
            return 4;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public sealed class OutputInfo
    {
        public string Directory { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
        public int FileCount { get; set; }
        public long TotalSizeBytes { get; set; }
    }

    public sealed class MetricsInfo
    {
        public string Format { get; set; } = string.Empty;
        public int Dpi { get; set; }
        public int JpegQuality { get; set; }
        public string PageRange { get; set; } = string.Empty;
        public int PagesExported { get; set; }
        public long AverageSizeBytes { get; set; }
        public double ProcessingTimeMs { get; set; }
    }

    public sealed class ValidationInfo
    {
        public bool AllFilesExist { get; set; }
    }
}
