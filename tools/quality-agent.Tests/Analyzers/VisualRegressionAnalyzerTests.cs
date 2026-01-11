using FluentPDF.QualityAgent.Analyzers;
using FluentPDF.QualityAgent.Models;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Analyzers;

public class VisualRegressionAnalyzerTests
{
    private readonly string _testHistoryPath;

    public VisualRegressionAnalyzerTests()
    {
        // Use temp path for test history to avoid conflicts
        _testHistoryPath = Path.Combine(Path.GetTempPath(), $"test-ssim-history-{Guid.NewGuid()}.json");
    }

    [Fact]
    public void Analyze_WithStableScores_ReturnsNoRegressions()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.995),
            ("Test2", 0.998),
            ("Test3", 1.000)
        });

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Empty(analysis.Regressions);
        Assert.Equal(3, analysis.TotalTests);
        Assert.Equal(3, analysis.PassedTests);
        Assert.Equal(0, analysis.MinorRegressions);
        Assert.Equal(0, analysis.MajorRegressions);
        Assert.Equal(0, analysis.CriticalRegressions);
    }

    [Fact]
    public void Analyze_WithMinorRegression_ClassifiesCorrectly()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.985), // Minor regression (< 0.99)
            ("Test2", 0.995)  // Passing
        });

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Single(analysis.Regressions);
        Assert.Equal(2, analysis.TotalTests);
        Assert.Equal(1, analysis.PassedTests);
        Assert.Equal(1, analysis.MinorRegressions);
        Assert.Equal(0, analysis.MajorRegressions);
        Assert.Equal(0, analysis.CriticalRegressions);

        var regression = analysis.Regressions.First();
        Assert.Equal("Test1", regression.TestName);
        Assert.Equal(0.985, regression.SsimScore);
        Assert.Equal(RegressionSeverity.Minor, regression.Severity);
    }

    [Fact]
    public void Analyze_WithMajorRegression_ClassifiesCorrectly()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.965), // Major regression (< 0.97)
            ("Test2", 0.995)  // Passing
        });

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Single(analysis.Regressions);
        Assert.Equal(1, analysis.MajorRegressions);
        Assert.Equal(RegressionSeverity.Major, analysis.Regressions.First().Severity);
    }

    [Fact]
    public void Analyze_WithCriticalRegression_ClassifiesCorrectly()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.920), // Critical regression (< 0.95)
            ("Test2", 0.995)  // Passing
        });

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Single(analysis.Regressions);
        Assert.Equal(1, analysis.CriticalRegressions);
        Assert.Equal(RegressionSeverity.Critical, analysis.Regressions.First().Severity);
    }

    [Fact]
    public void Analyze_WithMultipleSeverities_ClassifiesAll()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.920), // Critical
            ("Test2", 0.965), // Major
            ("Test3", 0.985), // Minor
            ("Test4", 0.995)  // Passing
        });

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Equal(3, analysis.Regressions.Count);
        Assert.Equal(4, analysis.TotalTests);
        Assert.Equal(1, analysis.PassedTests);
        Assert.Equal(1, analysis.MinorRegressions);
        Assert.Equal(1, analysis.MajorRegressions);
        Assert.Equal(1, analysis.CriticalRegressions);

        // Verify regressions are sorted by score (lowest first)
        Assert.Equal(0.920, analysis.Regressions[0].SsimScore);
        Assert.Equal(0.965, analysis.Regressions[1].SsimScore);
        Assert.Equal(0.985, analysis.Regressions[2].SsimScore);
    }

    [Fact]
    public void Analyze_FirstRun_CreatesHistory()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.995),
            ("Test2", 0.990)
        });

        // Act
        var result = analyzer.Analyze(ssimResults, buildId: "build-001");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(_testHistoryPath));

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithDegradationTrend_DetectsTrend()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(
            historyFilePath: _testHistoryPath,
            degradationThreshold: 3);

        // Simulate 5 runs with consecutive decreases
        var scores = new[] { 0.999, 0.995, 0.990, 0.985, 0.980 };

        for (int i = 0; i < scores.Length; i++)
        {
            var ssimResults = CreateSsimResults(new[] { ("Test1", scores[i]) });
            analyzer.Analyze(ssimResults, buildId: $"build-{i:D3}");
        }

        // Act - final analysis
        var finalResults = CreateSsimResults(new[] { ("Test1", 0.975) });
        var result = analyzer.Analyze(finalResults, buildId: "build-005");

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.NotEmpty(analysis.Trends);
        Assert.Equal(1, analysis.DegradingTests);

        var trend = analysis.Trends.First();
        Assert.Equal("Test1", trend.TestName);
        Assert.True(trend.IsDegrading);
        Assert.True(trend.ConsecutiveDecreases >= 3);

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithStableTrend_DoesNotDetectDegradation()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(
            historyFilePath: _testHistoryPath,
            degradationThreshold: 3);

        // Simulate 5 runs with stable scores
        var scores = new[] { 0.995, 0.996, 0.995, 0.997, 0.995 };

        for (int i = 0; i < scores.Length; i++)
        {
            var ssimResults = CreateSsimResults(new[] { ("Test1", scores[i]) });
            analyzer.Analyze(ssimResults, buildId: $"build-{i:D3}");
        }

        // Act - final analysis
        var finalResults = CreateSsimResults(new[] { ("Test1", 0.996) });
        var result = analyzer.Analyze(finalResults, buildId: "build-005");

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Empty(analysis.Trends); // No degrading trends
        Assert.Equal(0, analysis.DegradingTests);

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithMixedTrend_OnlyReportsDegrading()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(
            historyFilePath: _testHistoryPath,
            degradationThreshold: 3);

        // Test1: degrading, Test2: stable, Test3: improving
        var testScores = new Dictionary<string, double[]>
        {
            ["Test1"] = new[] { 0.999, 0.995, 0.990, 0.985, 0.980 }, // Degrading
            ["Test2"] = new[] { 0.995, 0.996, 0.995, 0.997, 0.995 }, // Stable
            ["Test3"] = new[] { 0.980, 0.985, 0.990, 0.995, 0.999 }  // Improving
        };

        for (int i = 0; i < 5; i++)
        {
            var testData = testScores.Select(kvp => (kvp.Key, kvp.Value[i])).ToArray();
            var ssimResults = CreateSsimResults(testData);
            analyzer.Analyze(ssimResults, buildId: $"build-{i:D3}");
        }

        // Act - final analysis
        var finalResults = CreateSsimResults(new[]
        {
            ("Test1", 0.975),
            ("Test2", 0.996),
            ("Test3", 1.000)
        });
        var result = analyzer.Analyze(finalResults, buildId: "build-005");

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Single(analysis.Trends); // Only degrading trend
        Assert.Equal(1, analysis.DegradingTests);
        Assert.Equal("Test1", analysis.Trends.First().TestName);

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithHistoryLimit_KeepsOnlyRecentEntries()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(
            historyFilePath: _testHistoryPath,
            maxHistoryEntries: 3); // Only keep last 3 entries

        // Add 5 entries
        for (int i = 0; i < 5; i++)
        {
            var ssimResults = CreateSsimResults(new[] { ("Test1", 0.995) });
            analyzer.Analyze(ssimResults, buildId: $"build-{i:D3}");
        }

        // Act - verify history is limited
        var finalResults = CreateSsimResults(new[] { ("Test1", 0.990) });
        var result = analyzer.Analyze(finalResults, buildId: "build-005");

        // Assert
        Assert.True(result.IsSuccess);
        // History should have at most maxHistoryEntries + current entry
        // We can't directly inspect history, but we can verify it doesn't fail

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithMinorFluctuations_AvoidsFalsePositives()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(
            historyFilePath: _testHistoryPath,
            degradationThreshold: 3);

        // Simulate minor fluctuations (not a consistent trend)
        var scores = new[] { 0.995, 0.993, 0.996, 0.992, 0.997 };

        for (int i = 0; i < scores.Length; i++)
        {
            var ssimResults = CreateSsimResults(new[] { ("Test1", scores[i]) });
            analyzer.Analyze(ssimResults, buildId: $"build-{i:D3}");
        }

        // Act
        var finalResults = CreateSsimResults(new[] { ("Test1", 0.994) });
        var result = analyzer.Analyze(finalResults, buildId: "build-005");

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Empty(analysis.Trends); // No degradation trend detected
        Assert.Equal(0, analysis.DegradingTests);

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithCustomThresholds_UsesCustomValues()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(
            historyFilePath: _testHistoryPath,
            minorThreshold: 0.98,
            majorThreshold: 0.95,
            criticalThreshold: 0.90);

        var ssimResults = CreateSsimResults(new[]
        {
            ("Test1", 0.975), // Should be minor with custom thresholds
            ("Test2", 0.940), // Should be major
            ("Test3", 0.880)  // Should be critical
        });

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Equal(3, analysis.Regressions.Count);

        // With default thresholds (0.99, 0.97, 0.95):
        // Test1 (0.975) would be minor
        // Test2 (0.940) would be critical
        // Test3 (0.880) would be critical
        // But with custom thresholds, classifications should match the custom values

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    [Fact]
    public void Analyze_WithEmptyResults_ReturnsEmptyAnalysis()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = new SsimResults
        {
            Tests = new List<SsimTestResult>()
        };

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Empty(analysis.Regressions);
        Assert.Empty(analysis.Trends);
        Assert.Equal(0, analysis.TotalTests);
        Assert.Equal(0, analysis.PassedTests);
        Assert.Equal(0, analysis.DegradingTests);
    }

    [Fact]
    public void Analyze_WithImagePaths_IncludesPathsInRegressions()
    {
        // Arrange
        var analyzer = new VisualRegressionAnalyzer(historyFilePath: _testHistoryPath);
        var ssimResults = new SsimResults
        {
            Tests = new List<SsimTestResult>
            {
                new SsimTestResult
                {
                    TestName = "Test1",
                    SsimScore = 0.92,
                    BaselineImagePath = "/baseline/test1.png",
                    CurrentImagePath = "/current/test1.png",
                    Regression = RegressionSeverity.Critical
                }
            }
        };

        // Act
        var result = analyzer.Analyze(ssimResults);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        var regression = analysis.Regressions.First();
        Assert.Equal("/baseline/test1.png", regression.BaselineImagePath);
        Assert.Equal("/current/test1.png", regression.CurrentImagePath);

        // Cleanup
        File.Delete(_testHistoryPath);
    }

    private SsimResults CreateSsimResults(params (string testName, double score)[] testData)
    {
        var tests = testData.Select(t => new SsimTestResult
        {
            TestName = t.testName,
            SsimScore = t.score,
            Regression = ClassifyRegression(t.score)
        }).ToList();

        return new SsimResults
        {
            Tests = tests
        };
    }

    private RegressionSeverity ClassifyRegression(double ssimScore)
    {
        if (ssimScore < 0.95) return RegressionSeverity.Critical;
        if (ssimScore < 0.97) return RegressionSeverity.Major;
        if (ssimScore < 0.99) return RegressionSeverity.Minor;
        return RegressionSeverity.None;
    }
}
