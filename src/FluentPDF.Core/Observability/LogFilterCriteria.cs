namespace FluentPDF.Core.Observability;

/// <summary>
/// Represents filtering criteria for log entries.
/// </summary>
public sealed class LogFilterCriteria
{
    /// <summary>
    /// Gets the minimum log level to include (inclusive).
    /// </summary>
    public LogLevel? MinimumLevel { get; init; }

    /// <summary>
    /// Gets the correlation ID to filter by (exact match).
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the component filter (StartsWith match).
    /// </summary>
    public string? ComponentFilter { get; init; }

    /// <summary>
    /// Gets the start time for time range filtering (inclusive).
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// Gets the end time for time range filtering (inclusive).
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    /// Gets the search text to filter by (case-insensitive Contains on Message).
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Determines whether a log entry matches these filter criteria.
    /// </summary>
    /// <param name="entry">The log entry to check.</param>
    /// <returns>True if the entry matches all criteria; otherwise, false.</returns>
    public bool Matches(LogEntry entry)
    {
        if (MinimumLevel.HasValue && entry.Level < MinimumLevel.Value)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(CorrelationId) && entry.CorrelationId != CorrelationId)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(ComponentFilter) && !entry.Component.StartsWith(ComponentFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (StartTime.HasValue && entry.Timestamp < StartTime.Value)
        {
            return false;
        }

        if (EndTime.HasValue && entry.Timestamp > EndTime.Value)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(SearchText) && !entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
