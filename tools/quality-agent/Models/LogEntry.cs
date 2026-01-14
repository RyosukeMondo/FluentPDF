namespace FluentPDF.QualityAgent.Models;

public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public Dictionary<string, object>? Properties { get; init; }
    public ExceptionInfo? Exception { get; init; }
}

public class ExceptionInfo
{
    public string Type { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
}

public class LogResults
{
    public List<LogEntry> Entries { get; init; } = new();
    public Dictionary<string, List<LogEntry>> EntriesByCorrelationId { get; init; } = new();
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int InfoCount { get; init; }
}
