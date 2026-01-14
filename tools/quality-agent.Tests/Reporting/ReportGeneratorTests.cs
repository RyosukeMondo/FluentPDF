using FluentPDF.QualityAgent.Models;
using FluentPDF.QualityAgent.Reporting;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Reporting;

public class ReportGeneratorTests
{
    private readonly string _schemaPath;

    public ReportGeneratorTests()
    {
        // Schema is in the root schemas directory
        var projectRoot = GetProjectRoot();
        _schemaPath = Path.Combine(projectRoot, "schemas", "quality-report.schema.json");
    }

    [Fact]
    public void GenerateReport_WithPassingScenario_ReturnsPassStatus()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 100, passed: 100, failed: 0);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.Equal(QualityStatus.Pass, report.Status);
        Assert.True(report.OverallScore >= 80.0);
    }

    [Fact]
    public void GenerateReport_WithFailingTests_ReturnsLowerScore()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 100, passed: 50, failed: 50);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        // 50% pass rate * 0.4 + 100 * 0.6 = 20 + 60 = 80 (exactly at threshold)
        Assert.Equal(80.0, report.OverallScore, 1);
        Assert.Equal(50.0, report.Analysis.TestAnalysis.PassRate);
        Assert.Equal(QualityStatus.Pass, report.Status); // 80 is Pass
    }

    [Fact]
    public void GenerateReport_WithErrorSpike_PenalizesLogScore()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 100, passed: 100, failed: 0);
        var logPatterns = new LogPatterns
        {
            ErrorRate = new ErrorRateAnalysis
            {
                ErrorsPerHour = 100.0,
                BaselineErrorsPerHour = 10.0,
                IsSpike = true,
                SpikeMultiplier = 10.0 // 10x spike
            }
        };
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.True(report.Analysis.LogAnalysis.Score < 100.0);
        Assert.Equal(LogHealthStatus.Critical, report.Analysis.LogAnalysis.ErrorRateHealth);
    }

    [Fact]
    public void GenerateReport_WithCriticalVisualRegression_PenalizesScore()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 100, passed: 100, failed: 0);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = new VisualAnalysis
        {
            TotalTests = 10,
            PassedTests = 8,
            CriticalRegressions = 2,
            MajorRegressions = 0,
            MinorRegressions = 0,
            DegradingTests = 0,
            Regressions = new List<VisualRegression>
            {
                new VisualRegression
                {
                    TestName = "Test1",
                    SsimScore = 0.93,
                    Severity = RegressionSeverity.Critical
                },
                new VisualRegression
                {
                    TestName = "Test2",
                    SsimScore = 0.94,
                    Severity = RegressionSeverity.Critical
                }
            }
        };
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.Equal(2, report.Analysis.VisualAnalysis.CriticalRegressions);
        Assert.True(report.Analysis.VisualAnalysis.Score < 100.0);
        Assert.Equal(2, report.Summary.CriticalIssues);
    }

    [Fact]
    public void GenerateReport_CalculatesWeightedScore_Correctly()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);

        // Test score: 50% (weight 0.4) = 20 points
        var testResults = CreateTestResults(total: 100, passed: 50, failed: 50);

        // Log score: 100% (weight 0.3) = 30 points
        var logPatterns = CreateHealthyLogPatterns();

        // Visual score: 100% (weight 0.2) = 20 points
        var visualAnalysis = CreateHealthyVisualAnalysis();

        // Validation score: 100% (weight 0.1) = 10 points
        // Total expected: 20 + 30 + 20 + 10 = 80 points

        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.Equal(80.0, report.OverallScore, 1); // Allow 1 decimal tolerance
        Assert.Equal(QualityStatus.Pass, report.Status); // Exactly at pass threshold
    }

    [Fact]
    public void GenerateReport_StatusThresholds_WorkCorrectly()
    {
        // Test Pass status (≥80)
        var passReport = CreateReportWithScore(85.0);
        Assert.Equal(QualityStatus.Pass, passReport.Status);

        // Test Warn status (60-79)
        var warnReport = CreateReportWithScore(70.0);
        Assert.Equal(QualityStatus.Warn, warnReport.Status);

        // Test Fail status (<60) - need to make logs/visual bad too
        var failReport = CreateFailReport();
        Assert.Equal(QualityStatus.Fail, failReport.Status);
        Assert.True(failReport.OverallScore < 60.0);

        // Test boundary: exactly 80 should be Pass
        var boundaryPassReport = CreateReportWithScore(80.0);
        Assert.Equal(QualityStatus.Pass, boundaryPassReport.Status);

        // Test boundary: exactly 60 should be Warn
        var boundaryWarnReport = CreateReportWithScore(60.0);
        Assert.Equal(QualityStatus.Warn, boundaryWarnReport.Status);
    }

    [Fact]
    public void GenerateReport_WithRootCauseHypotheses_IncludesInReport()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 10, passed: 8, failed: 2);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var buildInfo = CreateBuildInfo();

        var testFailureAnalyses = new List<TestFailureAnalysis>
        {
            new TestFailureAnalysis
            {
                TestName = "FailingTest1",
                AiHypothesis = new RootCauseHypothesis
                {
                    Issue = "NullReferenceException",
                    Hypothesis = "Document object is null when rendering",
                    Confidence = 0.85,
                    Severity = "High",
                    RecommendedActions = new List<string> { "Add null check", "Initialize document" }
                },
                UsedFallback = false
            }
        };

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.Single(report.RootCauseHypotheses);
        var hypothesis = report.RootCauseHypotheses[0];
        Assert.Equal("FailingTest1", hypothesis.TestName);
        Assert.Equal("NullReferenceException", hypothesis.Issue);
        Assert.Equal(0.85, hypothesis.Confidence);
        Assert.False(hypothesis.UsedFallback);
    }

    [Fact]
    public void GenerateReport_GeneratesRecommendations_BasedOnIssues()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 100, passed: 70, failed: 30);
        var logPatterns = new LogPatterns
        {
            ErrorRate = new ErrorRateAnalysis
            {
                ErrorsPerHour = 50.0,
                BaselineErrorsPerHour = 10.0,
                IsSpike = true,
                SpikeMultiplier = 5.0
            },
            RepeatedExceptions = new List<RepeatedExceptionPattern>
            {
                new RepeatedExceptionPattern
                {
                    ExceptionType = "NullReferenceException",
                    Message = "Object reference not set",
                    Occurrences = 10
                }
            }
        };
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.NotEmpty(report.Recommendations);

        // Should have recommendations for failing tests and error spike
        var criticalRecs = report.Recommendations.Where(r => r.Priority == RecommendationPriority.Critical).ToList();
        Assert.NotEmpty(criticalRecs);

        // Check for test failure recommendation
        var testRec = report.Recommendations.FirstOrDefault(r => r.Category == RecommendationCategory.Testing);
        Assert.NotNull(testRec);

        // Check for error spike recommendation
        var logRec = report.Recommendations.FirstOrDefault(r =>
            r.Category == RecommendationCategory.Logging &&
            r.Description.Contains("spike"));
        Assert.NotNull(logRec);
    }

    [Fact]
    public void GenerateReport_ValidatesAgainstJsonSchema_Successfully()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 100, passed: 95, failed: 5);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        // Act
        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Assert - validation is done internally, so success means valid schema
        Assert.True(result.IsSuccess);

        // Also test serialization
        var json = generator.SerializeReport(result.Value);
        Assert.NotEmpty(json);
        Assert.Contains("\"overallScore\"", json);
        Assert.Contains("\"status\"", json);
    }

    [Fact]
    public void SerializeReport_ProducesFormattedJson()
    {
        // Arrange
        var generator = new QualityReportGenerator(_schemaPath);
        var testResults = CreateTestResults(total: 10, passed: 10, failed: 0);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        // Act
        var json = generator.SerializeReport(result.Value);

        // Assert
        Assert.Contains("\n", json); // Should be indented
        Assert.Contains("\"Pass\"", json); // Enums should be strings
    }

    // Helper methods

    private TestResults CreateTestResults(int total, int passed, int failed, int skipped = 0)
    {
        var failures = new List<TestFailure>();
        for (int i = 0; i < failed; i++)
        {
            failures.Add(new TestFailure
            {
                TestName = $"FailingTest{i + 1}",
                ErrorMessage = "Test failed",
                StackTrace = "at TestMethod()",
                CorrelationId = $"corr-{i}"
            });
        }

        return new TestResults
        {
            Total = total,
            Passed = passed,
            Failed = failed,
            Skipped = skipped,
            Failures = failures
        };
    }

    private LogPatterns CreateHealthyLogPatterns()
    {
        return new LogPatterns
        {
            ErrorRate = new ErrorRateAnalysis
            {
                ErrorsPerHour = 5.0,
                BaselineErrorsPerHour = 10.0,
                IsSpike = false,
                SpikeMultiplier = 0.5
            },
            RepeatedExceptions = new List<RepeatedExceptionPattern>(),
            PerformanceWarnings = new List<PerformanceWarning>(),
            MissingCorrelationIds = new List<string>()
        };
    }

    private VisualAnalysis CreateHealthyVisualAnalysis()
    {
        return new VisualAnalysis
        {
            TotalTests = 10,
            PassedTests = 10,
            CriticalRegressions = 0,
            MajorRegressions = 0,
            MinorRegressions = 0,
            DegradingTests = 0,
            Regressions = new List<VisualRegression>(),
            Trends = new List<VisualTrend>()
        };
    }

    private BuildInfo CreateBuildInfo()
    {
        return new BuildInfo
        {
            BuildId = "build-123",
            Timestamp = DateTime.UtcNow,
            Branch = "main",
            Commit = "abc123",
            Author = "developer@example.com"
        };
    }

    private QualityReport CreateReportWithScore(double targetScore)
    {
        var generator = new QualityReportGenerator(_schemaPath);

        // Calculate test pass rate needed to achieve target score
        // overallScore = testScore * 0.4 + 100 * 0.3 + 100 * 0.2 + 100 * 0.1
        // overallScore = testScore * 0.4 + 60
        // testScore = (overallScore - 60) / 0.4

        var testScore = (targetScore - 60.0) / 0.4;
        var passRate = Math.Max(0, Math.Min(100, testScore));
        var passed = (int)(passRate * 100 / 100);
        var failed = 100 - passed;

        var testResults = CreateTestResults(total: 100, passed: passed, failed: failed);
        var logPatterns = CreateHealthyLogPatterns();
        var visualAnalysis = CreateHealthyVisualAnalysis();
        var testFailureAnalyses = new List<TestFailureAnalysis>();
        var buildInfo = CreateBuildInfo();

        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            testFailureAnalyses,
            buildInfo
        );

        return result.Value;
    }

    private QualityReport CreateFailReport()
    {
        var generator = new QualityReportGenerator(_schemaPath);

        // Create a failing scenario: all tests fail, critical error spike, critical visual regressions
        var testResults = CreateTestResults(total: 100, passed: 0, failed: 100);

        var logPatterns = new LogPatterns
        {
            ErrorRate = new ErrorRateAnalysis
            {
                ErrorsPerHour = 100.0,
                BaselineErrorsPerHour = 10.0,
                IsSpike = true,
                SpikeMultiplier = 10.0 // Critical spike
            },
            RepeatedExceptions = new List<RepeatedExceptionPattern>
            {
                new RepeatedExceptionPattern { ExceptionType = "Error1", Message = "Test", Occurrences = 10 },
                new RepeatedExceptionPattern { ExceptionType = "Error2", Message = "Test", Occurrences = 10 }
            }
        };

        var visualAnalysis = new VisualAnalysis
        {
            TotalTests = 10,
            PassedTests = 0,
            CriticalRegressions = 10,
            Regressions = Enumerable.Range(1, 10).Select(i => new VisualRegression
            {
                TestName = $"Test{i}",
                SsimScore = 0.9,
                Severity = RegressionSeverity.Critical
            }).ToList()
        };

        var result = generator.GenerateReport(
            testResults,
            logPatterns,
            visualAnalysis,
            new List<TestFailureAnalysis>(),
            CreateBuildInfo()
        );

        return result.Value;
    }

    private string GetProjectRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null && !File.Exists(Path.Combine(directory, "FluentPDF.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        if (directory == null)
        {
            throw new InvalidOperationException("Could not find project root (FluentPDF.sln not found)");
        }

        return directory;
    }
}
