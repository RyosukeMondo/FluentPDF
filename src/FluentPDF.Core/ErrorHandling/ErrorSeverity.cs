namespace FluentPDF.Core.ErrorHandling;

/// <summary>
/// Indicates the severity level of an error for prioritization and handling decisions.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Critical errors that prevent the application from functioning (e.g., corrupted state, fatal crashes).
    /// Requires immediate attention and typically results in application termination.
    /// </summary>
    Critical,

    /// <summary>
    /// Error conditions that prevent a specific operation from completing but don't crash the app.
    /// User action may be blocked, but the application remains functional.
    /// </summary>
    Error,

    /// <summary>
    /// Warning conditions that indicate potential issues but allow operations to continue.
    /// May result in degraded functionality or unexpected behavior.
    /// </summary>
    Warning,

    /// <summary>
    /// Informational messages about non-critical issues or expected failure conditions.
    /// Used for audit trails and debugging purposes.
    /// </summary>
    Info
}
