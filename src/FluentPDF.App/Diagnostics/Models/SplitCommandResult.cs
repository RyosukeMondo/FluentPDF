// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-split command execution.
/// </summary>
public sealed class SplitCommandResult : CommandResult
{
    /// <summary>
    /// Gets or sets the input file details.
    /// </summary>
    public InputInfo Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the page ranges.
    /// </summary>
    public List<string> Ranges { get; set; } = new();

    /// <summary>
    /// Gets or sets the output files details.
    /// </summary>
    public List<OutputFileInfo> Output { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "error")
        {
            if (Errors.Count > 0 && Errors[0].Contains("not found"))
                return 1;
            if (Errors.Count > 0 && Errors[0].Contains("split"))
                return 2;
            if (Errors.Count > 0 && Errors[0].Contains("range"))
                return 3;
        }

        if (Output.Any(o => !o.Valid))
            return 4;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public int TotalPages { get; set; }
    }

    public sealed class OutputFileInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
        public bool Valid { get; set; }
    }
}
