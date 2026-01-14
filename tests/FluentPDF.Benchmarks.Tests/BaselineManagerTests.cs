using FluentPDF.Benchmarks.Utils;
using FluentAssertions;

namespace FluentPDF.Benchmarks.Tests;

/// <summary>
/// Unit tests for BaselineManager functionality.
/// </summary>
public class BaselineManagerTests : IDisposable
{
    private readonly string _testBaselinesDirectory;
    private readonly BaselineManager _manager;

    public BaselineManagerTests()
    {
        // Create a temporary directory for test baselines
        _testBaselinesDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF_Benchmarks_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testBaselinesDirectory);
        _manager = new BaselineManager(_testBaselinesDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testBaselinesDirectory))
        {
            Directory.Delete(_testBaselinesDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveBaseline_WithValidData_ShouldSucceed()
    {
        // Arrange
        var runInfo = CreateSampleRunInfo("abc12345", DateTime.UtcNow);

        // Act
        var result = _manager.SaveBaseline(runInfo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var files = Directory.GetFiles(_testBaselinesDirectory);
        files.Should().HaveCount(1);
        files[0].Should().Contain("baseline-");
        files[0].Should().Contain("abc12345".Substring(0, 8));
        files[0].Should().EndWith(".json");
    }

    [Fact]
    public void SaveBaseline_WithNullRunInfo_ShouldFail()
    {
        // Act
        var result = _manager.SaveBaseline(null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("cannot be null"));
    }

    [Fact]
    public void SaveBaseline_WithEmptyCommitSha_ShouldFail()
    {
        // Arrange
        var runInfo = CreateSampleRunInfo("", DateTime.UtcNow);

        // Act
        var result = _manager.SaveBaseline(runInfo);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("CommitSha is required"));
    }

    [Fact]
    public void SaveBaseline_WithEmptyResults_ShouldFail()
    {
        // Arrange
        var runInfo = new BenchmarkRunInfo
        {
            CommitSha = "abc12345",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>()
        };

        // Act
        var result = _manager.SaveBaseline(runInfo);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Results collection cannot be empty"));
    }

    [Fact]
    public void LoadBaseline_WithExistingFile_ShouldSucceed()
    {
        // Arrange
        var originalRunInfo = CreateSampleRunInfo("def67890", DateTime.UtcNow);
        _manager.SaveBaseline(originalRunInfo);
        var files = Directory.GetFiles(_testBaselinesDirectory);
        var filename = Path.GetFileName(files[0]);

        // Act
        var result = _manager.LoadBaseline(filename);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var loadedRunInfo = result.Value;
        loadedRunInfo.CommitSha.Should().Be(originalRunInfo.CommitSha);
        loadedRunInfo.Results.Should().HaveCount(originalRunInfo.Results.Count);
        loadedRunInfo.Results[0].BenchmarkName.Should().Be(originalRunInfo.Results[0].BenchmarkName);
    }

    [Fact]
    public void LoadBaseline_WithNonExistentFile_ShouldFail()
    {
        // Act
        var result = _manager.LoadBaseline("nonexistent-baseline.json");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("not found"));
    }

    [Fact]
    public void LoadBaseline_WithInvalidJson_ShouldFail()
    {
        // Arrange
        var filename = "baseline-2026-01-11-invalid.json";
        var filePath = Path.Combine(_testBaselinesDirectory, filename);
        File.WriteAllText(filePath, "{ invalid json content ");

        // Act
        var result = _manager.LoadBaseline(filename);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid baseline JSON"));
    }

    [Fact]
    public void LoadLatestBaseline_WithMultipleBaselines_ShouldReturnMostRecent()
    {
        // Arrange
        var oldRunInfo = CreateSampleRunInfo("old12345", DateTime.UtcNow.AddDays(-2));
        var newRunInfo = CreateSampleRunInfo("new67890", DateTime.UtcNow);

        _manager.SaveBaseline(oldRunInfo);
        Thread.Sleep(100); // Ensure different file timestamps
        _manager.SaveBaseline(newRunInfo);

        // Act
        var result = _manager.LoadLatestBaseline();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CommitSha.Should().Be(newRunInfo.CommitSha);
    }

    [Fact]
    public void LoadLatestBaseline_WithNoBaselines_ShouldFail()
    {
        // Act
        var result = _manager.LoadLatestBaseline();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("No baseline files found"));
    }

    [Fact]
    public void Compare_WithMatchingBenchmarks_ShouldCalculatePercentChange()
    {
        // Arrange
        var baseline = new BenchmarkRunInfo
        {
            CommitSha = "baseline123",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult
                {
                    BenchmarkName = "TestBenchmark",
                    MeanNanoseconds = 1000000, // 1ms
                    P99Nanoseconds = 1200000
                }
            }
        };

        var current = new BenchmarkRunInfo
        {
            CommitSha = "current456",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult
                {
                    BenchmarkName = "TestBenchmark",
                    MeanNanoseconds = 1100000, // 1.1ms (10% slower)
                    P99Nanoseconds = 1320000
                }
            }
        };

        // Act
        var result = _manager.Compare(current, baseline);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;
        comparison.ComparisonResults.Should().HaveCount(1);
        comparison.ComparisonResults[0].PercentChange.Should().BeApproximately(10.0, 0.01);
        comparison.ComparisonResults[0].IsNew.Should().BeFalse();
    }

    [Fact]
    public void Compare_WithNewBenchmark_ShouldMarkAsNew()
    {
        // Arrange
        var baseline = new BenchmarkRunInfo
        {
            CommitSha = "baseline123",
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult { BenchmarkName = "ExistingBenchmark", MeanNanoseconds = 1000000 }
            }
        };

        var current = new BenchmarkRunInfo
        {
            CommitSha = "current456",
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult { BenchmarkName = "ExistingBenchmark", MeanNanoseconds = 1100000 },
                new BenchmarkResult { BenchmarkName = "NewBenchmark", MeanNanoseconds = 2000000 }
            }
        };

        // Act
        var result = _manager.Compare(current, baseline);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;
        comparison.ComparisonResults.Should().HaveCount(2);

        var newBenchmark = comparison.ComparisonResults.First(r => r.BenchmarkName == "NewBenchmark");
        newBenchmark.IsNew.Should().BeTrue();
        newBenchmark.PercentChange.Should().BeNull();
    }

    [Fact]
    public void Compare_WithNullCurrent_ShouldFail()
    {
        // Arrange
        var baseline = CreateSampleRunInfo("baseline123", DateTime.UtcNow);

        // Act
        var result = _manager.Compare(null!, baseline);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Current run info cannot be null"));
    }

    [Fact]
    public void Compare_WithNullBaseline_ShouldFail()
    {
        // Arrange
        var current = CreateSampleRunInfo("current456", DateTime.UtcNow);

        // Act
        var result = _manager.Compare(current, null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Baseline run info cannot be null"));
    }

    [Fact]
    public void HasRegression_WithNoRegressions_ShouldReturnEmpty()
    {
        // Arrange
        var baseline = CreateBenchmarkRunInfo("baseline", 1000000);
        var current = CreateBenchmarkRunInfo("current", 1050000); // 5% slower
        var comparisonResult = _manager.Compare(current, baseline);
        var comparison = comparisonResult.Value;

        // Act
        var result = _manager.HasRegression(comparison, 10.0); // 10% threshold

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasRegressions.Should().BeFalse();
        result.Value.Regressions.Should().BeEmpty();
    }

    [Fact]
    public void HasRegression_WithRegression_ShouldDetectRegression()
    {
        // Arrange
        var baseline = CreateBenchmarkRunInfo("baseline", 1000000);
        var current = CreateBenchmarkRunInfo("current", 1150000); // 15% slower
        var comparisonResult = _manager.Compare(current, baseline);
        var comparison = comparisonResult.Value;

        // Act
        var result = _manager.HasRegression(comparison, 10.0); // 10% threshold

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasRegressions.Should().BeTrue();
        result.Value.Regressions.Should().HaveCount(1);
        result.Value.Regressions[0].IsRegression.Should().BeTrue();
        result.Value.Regressions[0].PercentChange.Should().BeApproximately(15.0, 0.01);
    }

    [Fact]
    public void HasRegression_WithImprovement_ShouldDetectImprovement()
    {
        // Arrange
        var baseline = CreateBenchmarkRunInfo("baseline", 1000000);
        var current = CreateBenchmarkRunInfo("current", 850000); // 15% faster
        var comparisonResult = _manager.Compare(current, baseline);
        var comparison = comparisonResult.Value;

        // Act
        var result = _manager.HasRegression(comparison, 10.0); // 10% threshold

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasRegressions.Should().BeFalse();
        result.Value.Improvements.Should().HaveCount(1);
        result.Value.Improvements[0].PercentChange.Should().BeApproximately(-15.0, 0.01);
    }

    [Fact]
    public void HasRegression_WithMultipleThresholds_ShouldWorkCorrectly()
    {
        // Arrange
        var baseline = CreateBenchmarkRunInfo("baseline", 1000000);
        var current = CreateBenchmarkRunInfo("current", 1150000); // 15% slower
        var comparisonResult = _manager.Compare(current, baseline);
        var comparison = comparisonResult.Value;

        // Act - Test with 10% threshold (should detect)
        var result10 = _manager.HasRegression(comparison, 10.0);
        // Act - Test with 20% threshold (should not detect)
        var result20 = _manager.HasRegression(comparison, 20.0);

        // Assert
        result10.Value.HasRegressions.Should().BeTrue();
        result20.Value.HasRegressions.Should().BeFalse();
    }

    [Fact]
    public void HasRegression_WithNegativeThreshold_ShouldFail()
    {
        // Arrange
        var baseline = CreateBenchmarkRunInfo("baseline", 1000000);
        var current = CreateBenchmarkRunInfo("current", 1100000);
        var comparisonResult = _manager.Compare(current, baseline);
        var comparison = comparisonResult.Value;

        // Act
        var result = _manager.HasRegression(comparison, -5.0);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Threshold must be non-negative"));
    }

    [Fact]
    public void ListBaselines_WithMultipleBaselines_ShouldReturnSortedList()
    {
        // Arrange
        _manager.SaveBaseline(CreateSampleRunInfo("aaa11111", DateTime.UtcNow.AddDays(-2)));
        Thread.Sleep(100);
        _manager.SaveBaseline(CreateSampleRunInfo("bbb22222", DateTime.UtcNow.AddDays(-1)));
        Thread.Sleep(100);
        _manager.SaveBaseline(CreateSampleRunInfo("ccc33333", DateTime.UtcNow));

        // Act
        var baselines = _manager.ListBaselines();

        // Assert
        baselines.Should().HaveCount(3);
        baselines.Should().AllSatisfy(b => b.Should().StartWith("baseline-"));
        baselines.Should().AllSatisfy(b => b.Should().EndWith(".json"));
    }

    [Fact]
    public void ListBaselines_WithNoBaselines_ShouldReturnEmptyList()
    {
        // Act
        var baselines = _manager.ListBaselines();

        // Assert
        baselines.Should().BeEmpty();
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_ShouldPreserveAllData()
    {
        // Arrange
        var original = new BenchmarkRunInfo
        {
            CommitSha = "full-data-test",
            Timestamp = DateTime.UtcNow,
            HardwareInfo = new HardwareInfo
            {
                Cpu = "Intel Core i7-12700K",
                RamGb = 32,
                OperatingSystem = "Windows 11",
                RuntimeVersion = ".NET 8.0.1"
            },
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult
                {
                    BenchmarkName = "RenderTextHeavy_100Zoom",
                    MeanNanoseconds = 1234567.89,
                    StdDevNanoseconds = 12345.67,
                    P50Nanoseconds = 1200000,
                    P95Nanoseconds = 1400000,
                    P99Nanoseconds = 1500000,
                    AllocatedBytes = 1048576,
                    Gen0Collections = 5,
                    Gen1Collections = 2,
                    Gen2Collections = 1
                }
            }
        };

        // Act - Save
        var saveResult = _manager.SaveBaseline(original);
        saveResult.IsSuccess.Should().BeTrue();

        // Act - Load
        var files = Directory.GetFiles(_testBaselinesDirectory);
        var loadResult = _manager.LoadBaseline(Path.GetFileName(files[0]));

        // Assert
        loadResult.IsSuccess.Should().BeTrue();
        var loaded = loadResult.Value;

        loaded.CommitSha.Should().Be(original.CommitSha);
        loaded.Timestamp.Should().BeCloseTo(original.Timestamp, TimeSpan.FromSeconds(1));
        loaded.HardwareInfo.Cpu.Should().Be(original.HardwareInfo.Cpu);
        loaded.HardwareInfo.RamGb.Should().Be(original.HardwareInfo.RamGb);
        loaded.Results.Should().HaveCount(1);

        var loadedResult = loaded.Results[0];
        var originalResult = original.Results[0];
        loadedResult.BenchmarkName.Should().Be(originalResult.BenchmarkName);
        loadedResult.MeanNanoseconds.Should().BeApproximately(originalResult.MeanNanoseconds, 0.01);
        loadedResult.AllocatedBytes.Should().Be(originalResult.AllocatedBytes);
        loadedResult.Gen0Collections.Should().Be(originalResult.Gen0Collections);
    }

    // Helper methods
    private BenchmarkRunInfo CreateSampleRunInfo(string commitSha, DateTime timestamp)
    {
        return new BenchmarkRunInfo
        {
            CommitSha = commitSha,
            Timestamp = timestamp,
            HardwareInfo = new HardwareInfo
            {
                Cpu = "Test CPU",
                RamGb = 16,
                OperatingSystem = "Test OS",
                RuntimeVersion = ".NET 8.0"
            },
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult
                {
                    BenchmarkName = "SampleBenchmark",
                    MeanNanoseconds = 1000000,
                    P99Nanoseconds = 1200000,
                    AllocatedBytes = 1024
                }
            }
        };
    }

    private BenchmarkRunInfo CreateBenchmarkRunInfo(string commitSha, double meanNanos)
    {
        return new BenchmarkRunInfo
        {
            CommitSha = commitSha,
            Timestamp = DateTime.UtcNow,
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult
                {
                    BenchmarkName = "TestBenchmark",
                    MeanNanoseconds = meanNanos,
                    P99Nanoseconds = meanNanos * 1.2
                }
            }
        };
    }
}
