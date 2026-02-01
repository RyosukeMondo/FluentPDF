// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Base result for diagnostic command execution with JSON report generation.
/// </summary>
public abstract class CommandResult
{
    /// <summary>
    /// Gets or sets the command name.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Gets or sets the errors encountered during execution.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Determines the exit code based on the result.
    /// </summary>
    /// <returns>Exit code: 0=success, 1-4=specific errors.</returns>
    public abstract int GetExitCode();
}
