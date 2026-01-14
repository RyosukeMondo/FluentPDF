namespace FluentPDF.QualityAgent.Models;

public class LogPatterns
{
    public ErrorRateAnalysis ErrorRate { get; init; } = new();
    public List<RepeatedExceptionPattern> RepeatedExceptions { get; init; } = new();
    public List<PerformanceWarning> PerformanceWarnings { get; init; } = new();
    public List<string> MissingCorrelationIds { get; init; } = new();
}

public class ErrorRateAnalysis
{
    public double ErrorsPerHour { get; init; }
    public double BaselineErrorsPerHour { get; init; }
    public bool IsSpike { get; init; }
    public double SpikeMultiplier { get; init; }
}

public class RepeatedExceptionPattern
{
    public string ExceptionType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string StackTraceHash { get; init; } = string.Empty;
    public int Occurrences { get; init; }
    public List<DateTime> Timestamps { get; init; } = new();
}

public class PerformanceWarning
{
    public DateTime Timestamp { get; init; }
    public string Operation { get; init; } = string.Empty;
    public double DurationMs { get; init; }
    public double ThresholdMs { get; init; }
}
