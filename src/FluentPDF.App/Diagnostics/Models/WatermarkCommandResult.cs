// Copyright (c) 2025 FluentPDF. All rights reserved.

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-watermark command execution.
/// </summary>
public sealed class WatermarkCommandResult : CommandResult
{
    /// <summary>
    /// Gets or sets the input file details.
    /// </summary>
    public InputInfo Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the watermark details.
    /// </summary>
    public WatermarkInfo Watermark { get; set; } = new();

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
            if (Errors.Count > 0 && Errors[0].Contains("watermark"))
                return 2;
        }

        if (Verification.VisualCheckEnabled && !Verification.WatermarkDetected)
            return 3;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
    }

    public sealed class WatermarkInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int Opacity { get; set; }
        public string AppliedToPages { get; set; } = string.Empty;
    }

    public sealed class OutputInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
    }

    public sealed class VerificationInfo
    {
        public bool VisualCheckEnabled { get; set; }
        public bool WatermarkDetected { get; set; }
    }
}
