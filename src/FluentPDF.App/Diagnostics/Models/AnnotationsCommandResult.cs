// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-annotations command execution.
/// </summary>
public sealed class AnnotationsCommandResult : CommandResult
{
    /// <summary>
    /// Gets or sets the input file details.
    /// </summary>
    public InputInfo Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the annotations details.
    /// </summary>
    public AnnotationsInfo Annotations { get; set; } = new();

    /// <summary>
    /// Gets or sets the output file details.
    /// </summary>
    public OutputInfo Output { get; set; } = new();

    /// <summary>
    /// Gets or sets the persistence verification results.
    /// </summary>
    public PersistenceInfo Persistence { get; set; } = new();

    public override int GetExitCode()
    {
        if (Status == "error")
        {
            if (Errors.Count > 0 && Errors[0].Contains("not found"))
                return 1;
            if (Errors.Count > 0 && Errors[0].Contains("annotation"))
                return 2;
        }

        if (!Persistence.Verified)
            return 3;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public int Pages { get; set; }
    }

    public sealed class AnnotationsInfo
    {
        public int Count { get; set; }
        public List<string> Types { get; set; } = new();
    }

    public sealed class OutputInfo
    {
        public string File { get; set; } = string.Empty;
    }

    public sealed class PersistenceInfo
    {
        public bool Verified { get; set; }
        public int AnnotationsRecovered { get; set; }
    }
}
