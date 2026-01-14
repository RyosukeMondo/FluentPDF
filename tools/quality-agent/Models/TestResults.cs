namespace FluentPDF.QualityAgent.Models;

public class TestResults
{
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public List<TestFailure> Failures { get; init; } = new();

    public double PassRate => Total > 0 ? (double)Passed / Total * 100 : 0;
}

public class TestFailure
{
    public required string TestName { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
    public string? CorrelationId { get; init; }
}
