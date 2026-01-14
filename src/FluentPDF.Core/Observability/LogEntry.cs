namespace FluentPDF.Core.Observability;

/// <summary>
/// Represents a structured log entry.
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log level (severity).
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the component (namespace) that generated the log.
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the additional context data associated with the log entry.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the exception message if an exception was logged.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets the stack trace if an exception was logged.
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Defines the log severity levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace-level logging (most verbose).
    /// </summary>
    Trace,

    /// <summary>
    /// Debug-level logging.
    /// </summary>
    Debug,

    /// <summary>
    /// Information-level logging.
    /// </summary>
    Information,

    /// <summary>
    /// Warning-level logging.
    /// </summary>
    Warning,

    /// <summary>
    /// Error-level logging.
    /// </summary>
    Error,

    /// <summary>
    /// Critical-level logging (least verbose).
    /// </summary>
    Critical
}
