namespace FluentPDF.QualityAgent.Models;

/// <summary>
/// Complete quality report for a build.
/// </summary>
public class QualityReport
{
    public required ReportSummary Summary { get; init; }
    public double OverallScore { get; init; }
    public QualityStatus Status { get; init; }
    public required BuildInfo BuildInfo { get; init; }
    public required AnalysisResults Analysis { get; init; }
    public List<RootCauseHypothesisReport> RootCauseHypotheses { get; init; } = new();
    public List<Recommendation> Recommendations { get; init; } = new();
}

public class ReportSummary
{
    public DateTime Timestamp { get; init; }
    public required string BuildId { get; init; }
    public int TotalIssues { get; init; }
    public int CriticalIssues { get; init; }
}

public enum QualityStatus
{
    Pass,   // ≥ 80
    Warn,   // 60-79
    Fail    // < 60
}

public class BuildInfo
{
    public required string BuildId { get; init; }
    public DateTime Timestamp { get; init; }
    public required string Branch { get; init; }
    public string? Commit { get; init; }
    public string? Author { get; init; }
}

public class AnalysisResults
{
    public required TestAnalysisReport TestAnalysis { get; init; }
    public required LogAnalysisReport LogAnalysis { get; init; }
    public required VisualAnalysisReport VisualAnalysis { get; init; }
    public ValidationAnalysisReport? ValidationAnalysis { get; init; }
}

public class TestAnalysisReport
{
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public double PassRate { get; init; }
    public double Score { get; init; }
    public List<TestFailure> Failures { get; init; } = new();
}

public class LogAnalysisReport
{
    public LogHealthStatus ErrorRateHealth { get; init; }
    public int TotalPatterns { get; init; }
    public double Score { get; init; }
    public ErrorRateAnalysis? ErrorRate { get; init; }
    public List<RepeatedExceptionSummary> RepeatedExceptions { get; init; } = new();
    public List<PerformanceWarningSummary> PerformanceWarnings { get; init; } = new();
}

public enum LogHealthStatus
{
    Healthy,
    Warning,
    Critical
}

public class RepeatedExceptionSummary
{
    public required string ExceptionType { get; init; }
    public required string Message { get; init; }
    public int Occurrences { get; init; }
}

public class PerformanceWarningSummary
{
    public required string Operation { get; init; }
    public double DurationMs { get; init; }
    public double ThresholdMs { get; init; }
}

public class VisualAnalysisReport
{
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int MinorRegressions { get; init; }
    public int MajorRegressions { get; init; }
    public int CriticalRegressions { get; init; }
    public int DegradingTests { get; init; }
    public double Score { get; init; }
    public List<VisualRegression> Regressions { get; init; } = new();
}

public class ValidationAnalysisReport
{
    public int TotalValidations { get; init; }
    public int PassedValidations { get; init; }
    public double Score { get; init; }
}

public class RootCauseHypothesisReport
{
    public string? TestName { get; init; }
    public required string Issue { get; init; }
    public required string Hypothesis { get; init; }
    public double Confidence { get; init; }
    public required string Severity { get; init; }
    public List<string> RecommendedActions { get; init; } = new();
    public List<string> RelatedContext { get; init; } = new();
    public bool UsedFallback { get; init; }
}

public class Recommendation
{
    public RecommendationPriority Priority { get; init; }
    public RecommendationCategory Category { get; init; }
    public required string Description { get; init; }
    public List<string> RelatedIssues { get; init; } = new();
}

public enum RecommendationPriority
{
    Critical,
    High,
    Medium,
    Low
}

public enum RecommendationCategory
{
    Testing,
    Logging,
    Visual,
    Validation,
    Performance
}
