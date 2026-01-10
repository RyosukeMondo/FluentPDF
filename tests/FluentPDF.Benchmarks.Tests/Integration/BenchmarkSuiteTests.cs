using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using FluentAssertions;
using FluentPDF.Benchmarks.Config;
using FluentPDF.Benchmarks.Suites;
using FluentPDF.Benchmarks.Utils;
using System.Text.Json;

namespace FluentPDF.Benchmarks.Tests.Integration;

/// <summary>
/// Integration tests verifying benchmark suite execution and infrastructure.
/// These tests run actual benchmarks with minimal iterations to validate end-to-end functionality.
/// </summary>
[Trait("Category", "Integration")]
public class BenchmarkSuiteTests : IDisposable
{
    private readonly string _tempBaselinesDir;

    public BenchmarkSuiteTests()
    {
        // Create temporary directory for baseline tests
        _tempBaselinesDir = Path.Combine(Path.GetTempPath(), $"benchmark-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempBaselinesDir);
    }

    public void Dispose()
    {
        // Clean up temporary baseline directory
        if (Directory.Exists(_tempBaselinesDir))
        {
            Directory.Delete(_tempBaselinesDir, recursive: true);
        }
    }

    [Fact]
    public void RenderingBenchmarks_ShouldExecuteAndGenerateResults()
    {
        // Arrange
        var config = CreateFastConfig();

        // Act
        var summary = BenchmarkRunner.Run<RenderingBenchmarks>(config);

        // Assert
        summary.Should().NotBeNull();
        summary.Reports.Should().NotBeEmpty("benchmark should produce results");
        summary.HasCriticalValidationErrors.Should().BeFalse("benchmark should not have validation errors");

        // Verify at least some benchmarks completed successfully
        var successfulRuns = summary.Reports.Count(r => r.Success);
        successfulRuns.Should().BeGreaterThan(0, "at least one benchmark should complete successfully");

        // Verify output files exist
        var resultsPath = Path.Combine(summary.ResultsDirectoryPath, "*.json");
        var jsonFiles = Directory.GetFiles(summary.ResultsDirectoryPath, "*.json");
        jsonFiles.Should().NotBeEmpty("JSON results should be generated");
    }

    [Fact]
    public void MemoryBenchmarks_ShouldExecuteAndTrackMemoryMetrics()
    {
        // Arrange
        var config = CreateFastConfig();

        // Act
        var summary = BenchmarkRunner.Run<MemoryBenchmarks>(config);

        // Assert
        summary.Should().NotBeNull();
        summary.Reports.Should().NotBeEmpty();
        summary.HasCriticalValidationErrors.Should().BeFalse();

        // Verify memory diagnostics are present
        var reportsWithMemoryMetrics = summary.Reports
            .Where(r => r.Success)
            .ToList();

        reportsWithMemoryMetrics.Should().NotBeEmpty("memory metrics should be tracked");

        // Verify at least one report has memory allocation data
        var hasMemoryData = reportsWithMemoryMetrics.Any(r =>
            r.GcStats.GetBytesAllocatedPerOperation(r.BenchmarkCase) > 0 ||
            r.GcStats.Gen0Collections > 0);

        hasMemoryData.Should().BeTrue("memory diagnostics should track allocations or GC collections");
    }

    [Fact]
    public void StartupBenchmarks_ShouldExecuteAndMeetTimingRequirements()
    {
        // Arrange
        var config = CreateFastConfig();

        // Act
        var summary = BenchmarkRunner.Run<StartupBenchmarks>(config);

        // Assert
        summary.Should().NotBeNull();
        summary.Reports.Should().NotBeEmpty();
        summary.HasCriticalValidationErrors.Should().BeFalse();

        // Verify all benchmarks completed
        var allSuccessful = summary.Reports.All(r => r.Success);
        allSuccessful.Should().BeTrue("all startup benchmarks should complete successfully");

        // Verify timing results exist
        foreach (var report in summary.Reports.Where(r => r.Success))
        {
            report.ResultStatistics.Should().NotBeNull("timing statistics should be available");
            report.ResultStatistics!.Mean.Should().BeGreaterThan(0, "mean execution time should be measured");
        }
    }

    [Fact]
    public void BaselineManager_ShouldSaveAndLoadBaselines()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);
        var runInfo = CreateTestBenchmarkRunInfo("abc123", DateTime.UtcNow);

        // Act - Save baseline
        var saveResult = manager.SaveBaseline(runInfo);

        // Assert - Save succeeded
        saveResult.IsSuccess.Should().BeTrue("baseline should save successfully");

        // Verify file was created
        var baselineFiles = manager.ListBaselines();
        baselineFiles.Should().ContainSingle("one baseline file should exist");

        // Act - Load baseline
        var loadResult = manager.LoadBaseline(baselineFiles[0]);

        // Assert - Load succeeded
        loadResult.IsSuccess.Should().BeTrue("baseline should load successfully");
        loadResult.Value.Should().NotBeNull();
        loadResult.Value.CommitSha.Should().Be("abc123");
        loadResult.Value.Results.Should().HaveCount(2);
    }

    [Fact]
    public void BaselineManager_ShouldLoadLatestBaseline()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);

        // Create multiple baselines with different timestamps
        var older = CreateTestBenchmarkRunInfo("old123", DateTime.UtcNow.AddDays(-2));
        var newer = CreateTestBenchmarkRunInfo("new456", DateTime.UtcNow);

        manager.SaveBaseline(older);
        Thread.Sleep(100); // Ensure different file timestamps
        manager.SaveBaseline(newer);

        // Act
        var latestResult = manager.LoadLatestBaseline();

        // Assert
        latestResult.IsSuccess.Should().BeTrue("should load latest baseline");
        latestResult.Value.CommitSha.Should().Be("new456", "should load the most recent baseline");
    }

    [Fact]
    public void BaselineManager_ShouldCompareResults()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);

        var baseline = new Utils.BenchmarkRunInfo
        {
            CommitSha = "baseline",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Results = new List<BenchmarkResult>
            {
                new() { BenchmarkName = "Test1", MeanNanoseconds = 1000000 },
                new() { BenchmarkName = "Test2", MeanNanoseconds = 2000000 }
            }
        };

        var current = new Utils.BenchmarkRunInfo
        {
            CommitSha = "current",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>
            {
                new() { BenchmarkName = "Test1", MeanNanoseconds = 1100000 }, // 10% slower
                new() { BenchmarkName = "Test2", MeanNanoseconds = 1800000 }, // 10% faster
                new() { BenchmarkName = "Test3", MeanNanoseconds = 500000 }   // New benchmark
            }
        };

        // Act
        var compareResult = manager.Compare(current, baseline);

        // Assert
        compareResult.IsSuccess.Should().BeTrue("comparison should succeed");
        compareResult.Value.ComparisonResults.Should().HaveCount(3);

        // Verify Test1 shows 10% regression
        var test1 = compareResult.Value.ComparisonResults.First(r => r.BenchmarkName == "Test1");
        test1.PercentChange.Should().BeApproximately(10.0, 0.1);
        test1.IsNew.Should().BeFalse();

        // Verify Test2 shows 10% improvement
        var test2 = compareResult.Value.ComparisonResults.First(r => r.BenchmarkName == "Test2");
        test2.PercentChange.Should().BeApproximately(-10.0, 0.1);
        test2.IsNew.Should().BeFalse();

        // Verify Test3 is marked as new
        var test3 = compareResult.Value.ComparisonResults.First(r => r.BenchmarkName == "Test3");
        test3.IsNew.Should().BeTrue();
        test3.PercentChange.Should().BeNull();
    }

    [Fact]
    public void BaselineManager_ShouldDetectRegressions_At10Percent()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);
        var comparison = CreateComparisonWithRegressions(5.0, 15.0, 25.0);

        // Act
        var regressionResult = manager.HasRegression(comparison, thresholdPercent: 10.0);

        // Assert
        regressionResult.IsSuccess.Should().BeTrue();
        regressionResult.Value.HasRegressions.Should().BeTrue("should detect regressions above 10%");
        regressionResult.Value.Regressions.Should().HaveCount(2, "15% and 25% regressions should be detected");
        regressionResult.Value.Improvements.Should().BeEmpty("no improvements above 10% threshold");
    }

    [Fact]
    public void BaselineManager_ShouldDetectRegressions_At20Percent()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);
        var comparison = CreateComparisonWithRegressions(5.0, 15.0, 25.0);

        // Act
        var regressionResult = manager.HasRegression(comparison, thresholdPercent: 20.0);

        // Assert
        regressionResult.IsSuccess.Should().BeTrue();
        regressionResult.Value.HasRegressions.Should().BeTrue("should detect regressions above 20%");
        regressionResult.Value.Regressions.Should().ContainSingle("only 25% regression should be detected");

        var regression = regressionResult.Value.Regressions[0];
        regression.PercentChange.Should().BeApproximately(25.0, 0.1);
    }

    [Fact]
    public void BaselineManager_ShouldDetectImprovements()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);

        var baseline = new Utils.BenchmarkRunInfo
        {
            CommitSha = "baseline",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>
            {
                new() { BenchmarkName = "Fast", MeanNanoseconds = 1000000 }
            }
        };

        var current = new Utils.BenchmarkRunInfo
        {
            CommitSha = "current",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>
            {
                new() { BenchmarkName = "Fast", MeanNanoseconds = 700000 } // 30% faster
            }
        };

        var comparison = manager.Compare(current, baseline).Value;

        // Act
        var regressionResult = manager.HasRegression(comparison, thresholdPercent: 10.0);

        // Assert
        regressionResult.IsSuccess.Should().BeTrue();
        regressionResult.Value.HasRegressions.Should().BeFalse("no regressions should be detected");
        regressionResult.Value.Improvements.Should().ContainSingle("30% improvement should be detected");

        var improvement = regressionResult.Value.Improvements[0];
        improvement.PercentChange.Should().BeApproximately(-30.0, 0.1);
    }

    [Fact]
    public void BaselineManager_ShouldHandleMissingBaseline()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);

        // Act
        var loadResult = manager.LoadBaseline("nonexistent-baseline.json");

        // Assert
        loadResult.IsFailed.Should().BeTrue("loading nonexistent baseline should fail gracefully");
        loadResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void BaselineManager_ShouldValidateBaselineFormat()
    {
        // Arrange
        var manager = new BaselineManager(_tempBaselinesDir);
        var invalidFile = Path.Combine(_tempBaselinesDir, "invalid-baseline.json");

        // Create invalid JSON file
        File.WriteAllText(invalidFile, "{ invalid json }");

        // Act
        var loadResult = manager.LoadBaseline("invalid-baseline.json");

        // Assert
        loadResult.IsFailed.Should().BeTrue("loading invalid JSON should fail");
        loadResult.Errors.Should().Contain(e => e.Message.Contains("JSON"));
    }

    // Helper methods

    private static IConfig CreateFastConfig()
    {
        // Create a minimal config for fast test execution
        return ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddLogger(ConsoleLogger.Default)
            .WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default.WithMaxParameterColumnWidth(50));
    }

    private static Utils.BenchmarkRunInfo CreateTestBenchmarkRunInfo(string commitSha, DateTime timestamp)
    {
        return new Utils.BenchmarkRunInfo
        {
            CommitSha = commitSha,
            Timestamp = timestamp,
            HardwareInfo = new HardwareInfo
            {
                Cpu = "Test CPU",
                RamGb = 16,
                OperatingSystem = "Test OS",
                RuntimeVersion = "8.0.0"
            },
            Results = new List<BenchmarkResult>
            {
                new()
                {
                    BenchmarkName = "TestBenchmark1",
                    MeanNanoseconds = 1000000,
                    StdDevNanoseconds = 10000,
                    P50Nanoseconds = 950000,
                    P95Nanoseconds = 1100000,
                    P99Nanoseconds = 1200000,
                    AllocatedBytes = 1024,
                    Gen0Collections = 1,
                    Gen1Collections = 0,
                    Gen2Collections = 0
                },
                new()
                {
                    BenchmarkName = "TestBenchmark2",
                    MeanNanoseconds = 2000000,
                    StdDevNanoseconds = 20000,
                    P50Nanoseconds = 1950000,
                    P95Nanoseconds = 2100000,
                    P99Nanoseconds = 2200000,
                    AllocatedBytes = 2048,
                    Gen0Collections = 2,
                    Gen1Collections = 0,
                    Gen2Collections = 0
                }
            }
        };
    }

    private static BenchmarkComparison CreateComparisonWithRegressions(params double[] percentChanges)
    {
        var baseline = new Utils.BenchmarkRunInfo
        {
            CommitSha = "baseline",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>()
        };

        var current = new Utils.BenchmarkRunInfo
        {
            CommitSha = "current",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>()
        };

        var comparisonResults = new List<BenchmarkResultComparison>();

        for (int i = 0; i < percentChanges.Length; i++)
        {
            var baselineMean = 1000000.0;
            var currentMean = baselineMean * (1 + percentChanges[i] / 100.0);

            var baselineResult = new BenchmarkResult
            {
                BenchmarkName = $"Test{i + 1}",
                MeanNanoseconds = baselineMean
            };

            var currentResult = new BenchmarkResult
            {
                BenchmarkName = $"Test{i + 1}",
                MeanNanoseconds = currentMean
            };

            baseline.Results.Add(baselineResult);
            current.Results.Add(currentResult);

            comparisonResults.Add(new BenchmarkResultComparison
            {
                BenchmarkName = $"Test{i + 1}",
                Current = currentResult,
                Baseline = baselineResult,
                PercentChange = percentChanges[i],
                IsNew = false,
                IsRegression = false // Will be set by HasRegression method
            });
        }

        return new BenchmarkComparison
        {
            CurrentRun = current,
            BaselineRun = baseline,
            ComparisonResults = comparisonResults
        };
    }
}
