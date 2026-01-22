namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Represents the result of testing a documented workaround.
/// </summary>
public record WorkaroundTestResult
{
    /// <summary>
    /// Gets the name of the workaround that was tested.
    /// </summary>
    public required string WorkaroundName { get; init; }

    /// <summary>
    /// Gets the documentation reference for this workaround (file path and line numbers).
    /// Format: "FilePath.cs:StartLine-EndLine" (e.g., "PdfiumInterop.cs:177-183").
    /// </summary>
    public required string DocumentationReference { get; init; }

    /// <summary>
    /// Gets the current status of the workaround.
    /// </summary>
    public required WorkaroundStatus Status { get; init; }

    /// <summary>
    /// Gets detailed information about the test results.
    /// </summary>
    public required string Details { get; init; }

    /// <summary>
    /// Gets the recommended action based on the test results.
    /// </summary>
    public required string RecommendedAction { get; init; }

    /// <summary>
    /// Gets the timestamp when this test was performed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets additional context information about the test (e.g., PDFium version, test parameters).
    /// </summary>
    public Dictionary<string, string>? Context { get; init; }
}

/// <summary>
/// Represents the status of a documented workaround.
/// </summary>
public enum WorkaroundStatus
{
    /// <summary>
    /// The workaround is still needed because the underlying issue still exists.
    /// </summary>
    StillNeeded,

    /// <summary>
    /// The workaround can be removed because the underlying issue has been fixed in PDFium.
    /// </summary>
    CanBeRemoved,

    /// <summary>
    /// The workaround is broken and no longer functions correctly, requiring investigation.
    /// </summary>
    Broken
}
