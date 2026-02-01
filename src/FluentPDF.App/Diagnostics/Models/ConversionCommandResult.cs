// Copyright (c) 2025 FluentPDF. All rights reserved.

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-conversion command execution.
/// </summary>
public sealed class ConversionCommandResult : CommandResult
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
    /// Gets or sets the verification results.
    /// </summary>
    public VerificationInfo Verification { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "error")
        {
            if (Errors.Count > 0 && Errors[0].Contains("not found"))
                return 1;
            if (Errors.Count > 0 && Errors[0].Contains("conversion"))
                return 2;
        }

        if (!Verification.StructureValid)
            return 3;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    public sealed class OutputInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public sealed class VerificationInfo
    {
        public bool StructureValid { get; set; }
    }
}
