// Copyright (c) 2025 FluentPDF. All rights reserved.

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-optimize command execution.
/// </summary>
public sealed class OptimizeCommandResult : CommandResult
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
    /// Gets or sets the optimization metrics.
    /// </summary>
    public MetricsInfo Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the visual regression test results.
    /// </summary>
    public VisualRegressionInfo VisualRegression { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "error")
        {
            if (Errors.Count > 0 && Errors[0].Contains("not found"))
                return 1;
            if (Errors.Count > 0 && Errors[0].Contains("optimi"))
                return 2;
        }

        if (VisualRegression.Enabled && VisualRegression.SsimScore < 0.95)
            return 4;

        if (!Metrics.MeetsThreshold)
            return 3;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int Pages { get; set; }
    }

    public sealed class OutputInfo
    {
        public string File { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int Pages { get; set; }
    }

    public sealed class MetricsInfo
    {
        public double ReductionPercent { get; set; }
        public double MinReductionThreshold { get; set; }
        public bool MeetsThreshold { get; set; }
    }

    public sealed class VisualRegressionInfo
    {
        public bool Enabled { get; set; }
        public double? SsimScore { get; set; }
    }
}
