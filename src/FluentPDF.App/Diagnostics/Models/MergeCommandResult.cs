// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-merge command execution.
/// </summary>
public sealed class MergeCommandResult : CommandResult
{
    /// <summary>
    /// Gets or sets the input file details.
    /// </summary>
    public InputInfo Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the output file details.
    /// </summary>
    public OutputInfo Output { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation results.
    /// </summary>
    public ValidationInfo Validation { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "error")
        {
            if (Errors.Count > 0 && Errors[0].Contains("not found"))
                return 1;
            if (Errors.Count > 0 && Errors[0].Contains("merge"))
                return 2;
        }

        if (!Validation.StructureValid)
            return 4;

        if (!Validation.PageCountMatch)
            return 3;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public List<string> Files { get; set; } = new();
        public int TotalPages { get; set; }
    }

    public sealed class OutputInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public sealed class ValidationInfo
    {
        public bool StructureValid { get; set; }
        public bool PageCountMatch { get; set; }
        public List<string> QpdfErrors { get; set; } = new();
    }
}
