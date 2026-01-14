using FluentPDF.Benchmarks.Reporting;
using FluentPDF.Benchmarks.Utils;
using Xunit;

namespace FluentPDF.Benchmarks.Tests;

public class ReportGeneratorTests
{
    private readonly string _templatePath;
    private readonly string _outputDirectory;

    public ReportGeneratorTests()
    {
        // Find template relative to test assembly location
        var testDir = Path.GetDirectoryName(typeof(ReportGeneratorTests).Assembly.Location);
        var projectRoot = Path.GetFullPath(Path.Combine(testDir!, "..", "..", "..", ".."));
        _templatePath = Path.Combine(projectRoot, "FluentPDF.Benchmarks", "Reporting", "Templates", "report.html");
        _outputDirectory = Path.Combine(Path.GetTempPath(), "FluentPDF.Benchmarks.Tests");
        Directory.CreateDirectory(_outputDirectory);
    }

    [Fact]
    public void GenerateReport_WithValidData_ShouldSucceed()
    {
        // Arrange
        var generator = new ReportGenerator(_templatePath);
        var runInfo = CreateSampleRunInfo();
        var outputPath = Path.Combine(_outputDirectory, "test-report.html");

        // Act
        var result = generator.GenerateReport(runInfo, null, outputPath);

        // Assert
        Assert.True(result.IsSuccess, $"Report generation failed: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        Assert.True(File.Exists(outputPath), "Report file was not created");

        // Verify content
        var content = File.ReadAllText(outputPath);
        Assert.Contains("FluentPDF Performance Report", content);
        Assert.Contains("test-commit-sha", content);
        Assert.Contains("TestBenchmark", content);
        Assert.Contains("Intel Core i9", content);
    }

    [Fact]
    public void GenerateReport_WithComparison_ShouldIncludeComparisonSection()
    {
        // Arrange
        var generator = new ReportGenerator(_templatePath);
        var currentRun = CreateSampleRunInfo();
        var baselineRun = CreateSampleRunInfo("baseline-sha");

        // Make baseline slower to simulate a regression
        baselineRun.Results[0].MeanNanoseconds = 500_000;

        var comparison = new BenchmarkComparison
        {
            CurrentRun = currentRun,
            BaselineRun = baselineRun,
            ComparisonResults = new List<BenchmarkResultComparison>
            {
                new BenchmarkResultComparison
                {
                    BenchmarkName = "TestBenchmark",
                    Current = currentRun.Results[0],
                    Baseline = baselineRun.Results[0],
                    PercentChange = 100.0, // 100% slower
                    IsRegression = true,
                    IsNew = false
                }
            }
        };

        var outputPath = Path.Combine(_outputDirectory, "test-report-comparison.html");

        // Act
        var result = generator.GenerateReport(currentRun, comparison, outputPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(outputPath));

        var content = File.ReadAllText(outputPath);
        Assert.Contains("Baseline Comparison", content);
        Assert.Contains("baseline-sha", content);
        Assert.Contains("regression", content);
    }

    [Fact]
    public void GenerateReport_WithNullRunInfo_ShouldFail()
    {
        // Arrange
        var generator = new ReportGenerator(_templatePath);
        var outputPath = Path.Combine(_outputDirectory, "test-report-null.html");

        // Act
        var result = generator.GenerateReport(null!, null, outputPath);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("cannot be null", result.Errors[0].Message);
    }

    [Fact]
    public void GenerateReport_WithInvalidTemplate_ShouldFail()
    {
        // Arrange
        var generator = new ReportGenerator("/invalid/template/path.html");
        var runInfo = CreateSampleRunInfo();
        var outputPath = Path.Combine(_outputDirectory, "test-report-invalid-template.html");

        // Act
        var result = generator.GenerateReport(runInfo, null, outputPath);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Template file not found", result.Errors[0].Message);
    }

    [Fact]
    public void GenerateReport_ShouldFormatTimesCorrectly()
    {
        // Arrange
        var generator = new ReportGenerator(_templatePath);
        var runInfo = new BenchmarkRunInfo
        {
            CommitSha = "test-sha",
            Timestamp = DateTime.UtcNow,
            HardwareInfo = new HardwareInfo { Cpu = "Test CPU", RamGb = 16, OperatingSystem = "Test OS", RuntimeVersion = ".NET 8" },
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult { BenchmarkName = "NanosecondTest", MeanNanoseconds = 500 },
                new BenchmarkResult { BenchmarkName = "MicrosecondTest", MeanNanoseconds = 5_000 },
                new BenchmarkResult { BenchmarkName = "MillisecondTest", MeanNanoseconds = 5_000_000 },
                new BenchmarkResult { BenchmarkName = "SecondTest", MeanNanoseconds = 5_000_000_000 }
            }
        };

        var outputPath = Path.Combine(_outputDirectory, "test-report-formatting.html");

        // Act
        var result = generator.GenerateReport(runInfo, null, outputPath);

        // Assert
        Assert.True(result.IsSuccess);
        var content = File.ReadAllText(outputPath);
        Assert.Contains("ns", content); // nanoseconds
        Assert.Contains("μs", content); // microseconds
        Assert.Contains("ms", content); // milliseconds
        Assert.Contains("s", content);  // seconds (not just 'ms')
    }

    private BenchmarkRunInfo CreateSampleRunInfo(string commitSha = "test-commit-sha")
    {
        return new BenchmarkRunInfo
        {
            CommitSha = commitSha,
            Timestamp = new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc),
            HardwareInfo = new HardwareInfo
            {
                Cpu = "Intel Core i9-9900K @ 3.60GHz",
                RamGb = 32,
                OperatingSystem = "Windows 11 (10.0.22631)",
                RuntimeVersion = ".NET 8.0.0"
            },
            Results = new List<BenchmarkResult>
            {
                new BenchmarkResult
                {
                    BenchmarkName = "TestBenchmark",
                    MeanNanoseconds = 1_000_000,
                    StdDevNanoseconds = 50_000,
                    P50Nanoseconds = 950_000,
                    P95Nanoseconds = 1_100_000,
                    P99Nanoseconds = 1_200_000,
                    AllocatedBytes = 1024 * 1024, // 1 MB
                    Gen0Collections = 1,
                    Gen1Collections = 0,
                    Gen2Collections = 0
                },
                new BenchmarkResult
                {
                    BenchmarkName = "AnotherBenchmark",
                    MeanNanoseconds = 2_500_000,
                    StdDevNanoseconds = 100_000,
                    P50Nanoseconds = 2_400_000,
                    P95Nanoseconds = 2_700_000,
                    P99Nanoseconds = 2_800_000,
                    AllocatedBytes = 512 * 1024, // 512 KB
                    Gen0Collections = 0,
                    Gen1Collections = 0,
                    Gen2Collections = 0
                }
            }
        };
    }
}
