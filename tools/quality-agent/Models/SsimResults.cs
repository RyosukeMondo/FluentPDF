namespace FluentPDF.QualityAgent.Models;

public class SsimResults
{
    public List<SsimTestResult> Tests { get; init; } = new();
    public int Total => Tests.Count;
    public int Passed => Tests.Count(t => t.Regression == RegressionSeverity.None);
    public int MinorRegressions => Tests.Count(t => t.Regression == RegressionSeverity.Minor);
    public int MajorRegressions => Tests.Count(t => t.Regression == RegressionSeverity.Major);
    public int CriticalRegressions => Tests.Count(t => t.Regression == RegressionSeverity.Critical);
}

public class SsimTestResult
{
    public required string TestName { get; init; }
    public double SsimScore { get; init; }
    public string? BaselineImagePath { get; init; }
    public string? CurrentImagePath { get; init; }
    public RegressionSeverity Regression { get; init; }
}

public enum RegressionSeverity
{
    None,
    Minor,    // < 0.99
    Major,    // < 0.97
    Critical  // < 0.95
}
