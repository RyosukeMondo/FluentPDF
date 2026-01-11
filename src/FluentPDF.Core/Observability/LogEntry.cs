namespace FluentPDF.Core.Observability;

/// <summary>
/// Represents a structured log entry.
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// Gets the timestamp when the log entry was created.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the log level (severity).
    /// </summary>
    public required LogLevel Level { get; init; }

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the component (namespace) that generated the log.
    /// </summary>
    public required string Component { get; init; }

    /// <summary>
    /// Gets the additional context data associated with the log entry.
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();

    /// <summary>
    /// Gets the exception message if an exception was logged.
    /// </summary>
    public string? Exception { get; init; }

    /// <summary>
    /// Gets the stack trace if an exception was logged.
    /// </summary>
    public string? StackTrace { get; init; }
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
