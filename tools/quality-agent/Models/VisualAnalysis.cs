namespace FluentPDF.QualityAgent.Models;

public class VisualAnalysis
{
    public List<VisualRegression> Regressions { get; init; } = new();
    public List<VisualTrend> Trends { get; init; } = new();
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int MinorRegressions { get; init; }
    public int MajorRegressions { get; init; }
    public int CriticalRegressions { get; init; }
    public int DegradingTests { get; init; }
}

public class VisualRegression
{
    public required string TestName { get; init; }
    public double SsimScore { get; init; }
    public RegressionSeverity Severity { get; init; }
    public string? BaselineImagePath { get; init; }
    public string? CurrentImagePath { get; init; }
}

public class VisualTrend
{
    public required string TestName { get; init; }
    public List<SsimHistoryEntry> History { get; init; } = new();
    public bool IsDegrading { get; init; }
    public int ConsecutiveDecreases { get; init; }
    public double AverageScore { get; init; }
}

public class SsimHistoryEntry
{
    public DateTime Timestamp { get; init; }
    public double SsimScore { get; init; }
    public string? BuildId { get; init; }
}
