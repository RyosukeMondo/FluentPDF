// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result for test-forms command execution.
/// </summary>
public sealed class FormsCommandResult : CommandResult
{
    /// <summary>
    /// Gets or sets the input file details.
    /// </summary>
    public InputInfo Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the form data details.
    /// </summary>
    public DataInfo Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation results.
    /// </summary>
    public ValidationInfo Validation { get; set; } = new();

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
            if (Errors.Count > 0 && Errors[0].Contains("fill"))
                return 2;
        }

        if (Validation.Enabled && !Validation.Passed)
            return 3;

        if (!Persistence.Verified)
            return 4;

        return Status == "pass" ? 0 : 1;
    }

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public int FieldCount { get; set; }
    }

    public sealed class DataInfo
    {
        public int FieldsProvided { get; set; }
        public int FieldsFilled { get; set; }
    }

    public sealed class ValidationInfo
    {
        public bool Enabled { get; set; }
        public bool Passed { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public sealed class PersistenceInfo
    {
        public bool Verified { get; set; }
        public int FieldsRecovered { get; set; }
        public bool ValuesMatch { get; set; }
    }
}
