using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;

namespace FluentPDF.Benchmarks.Utils;

/// <summary>
/// Manages storage, loading, and comparison of benchmark baselines for regression detection.
/// </summary>
public class BaselineManager
{
    private readonly string _baselinesDirectory;
    private const string BaselineFilePattern = "baseline-{0}-{1}.json"; // baseline-YYYY-MM-DD-{SHA}.json

    /// <summary>
    /// Initializes a new instance of the BaselineManager with the specified baselines directory.
    /// </summary>
    /// <param name="baselinesDirectory">Directory where baseline files are stored.</param>
    public BaselineManager(string baselinesDirectory)
    {
        _baselinesDirectory = baselinesDirectory;
    }

    /// <summary>
    /// Saves benchmark results as a baseline file.
    /// </summary>
    /// <param name="runInfo">Benchmark run information to save.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    public Result SaveBaseline(BenchmarkRunInfo runInfo)
    {
        try
        {
            // Validate input
            if (runInfo == null)
                return Result.Fail("BenchmarkRunInfo cannot be null");

            if (string.IsNullOrWhiteSpace(runInfo.CommitSha))
                return Result.Fail("CommitSha is required");

            if (runInfo.Results == null || runInfo.Results.Count == 0)
                return Result.Fail("Results collection cannot be empty");

            // Ensure directory exists
            Directory.CreateDirectory(_baselinesDirectory);

            // Generate filename: baseline-YYYY-MM-DD-{SHA}.json
            var dateString = runInfo.Timestamp.ToString("yyyy-MM-dd");
            var shortSha = runInfo.CommitSha.Length > 8 ? runInfo.CommitSha.Substring(0, 8) : runInfo.CommitSha;
            var filename = string.Format(BaselineFilePattern, dateString, shortSha);
            var filePath = Path.Combine(_baselinesDirectory, filename);

            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(runInfo, options);
            File.WriteAllText(filePath, json);

            return Result.Ok().WithSuccess($"Baseline saved to {filename}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to save baseline: {ex.Message}").WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Loads a baseline from a file.
    /// </summary>
    /// <param name="filename">Name of the baseline file to load.</param>
    /// <returns>Result containing the loaded BenchmarkRunInfo or error details.</returns>
    public Result<BenchmarkRunInfo> LoadBaseline(string filename)
    {
        try
        {
            var filePath = Path.Combine(_baselinesDirectory, filename);

            if (!File.Exists(filePath))
                return Result.Fail<BenchmarkRunInfo>($"Baseline file not found: {filename}");

            var json = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var runInfo = JsonSerializer.Deserialize<BenchmarkRunInfo>(json, options);

            if (runInfo == null)
                return Result.Fail<BenchmarkRunInfo>("Failed to deserialize baseline JSON");

            // Validate loaded data
            if (string.IsNullOrWhiteSpace(runInfo.CommitSha))
                return Result.Fail<BenchmarkRunInfo>("Invalid baseline: missing CommitSha");

            if (runInfo.Results == null || runInfo.Results.Count == 0)
                return Result.Fail<BenchmarkRunInfo>("Invalid baseline: no results found");

            return Result.Ok(runInfo).WithSuccess($"Loaded baseline from {filename}");
        }
        catch (JsonException ex)
        {
            return Result.Fail<BenchmarkRunInfo>($"Invalid baseline JSON format: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
        catch (Exception ex)
        {
            return Result.Fail<BenchmarkRunInfo>($"Failed to load baseline: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Loads the most recent baseline file from the baselines directory.
    /// </summary>
    /// <returns>Result containing the most recent BenchmarkRunInfo or error details.</returns>
    public Result<BenchmarkRunInfo> LoadLatestBaseline()
    {
        try
        {
            if (!Directory.Exists(_baselinesDirectory))
                return Result.Fail<BenchmarkRunInfo>("Baselines directory does not exist");

            var files = Directory.GetFiles(_baselinesDirectory, "baseline-*.json")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToList();

            if (files.Count == 0)
                return Result.Fail<BenchmarkRunInfo>("No baseline files found");

            return LoadBaseline(Path.GetFileName(files[0]));
        }
        catch (Exception ex)
        {
            return Result.Fail<BenchmarkRunInfo>($"Failed to load latest baseline: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Compares current benchmark results against a baseline and calculates percent changes.
    /// </summary>
    /// <param name="current">Current benchmark run information.</param>
    /// <param name="baseline">Baseline benchmark run information.</param>
    /// <returns>Result containing comparison results with percent changes.</returns>
    public Result<BenchmarkComparison> Compare(BenchmarkRunInfo current, BenchmarkRunInfo baseline)
    {
        try
        {
            if (current == null)
                return Result.Fail<BenchmarkComparison>("Current run info cannot be null");

            if (baseline == null)
                return Result.Fail<BenchmarkComparison>("Baseline run info cannot be null");

            var comparison = new BenchmarkComparison
            {
                CurrentRun = current,
                BaselineRun = baseline,
                ComparisonResults = new List<BenchmarkResultComparison>()
            };

            // Compare each benchmark result
            foreach (var currentResult in current.Results)
            {
                var baselineResult = baseline.Results.FirstOrDefault(r => r.BenchmarkName == currentResult.BenchmarkName);

                if (baselineResult == null)
                {
                    // New benchmark not in baseline
                    comparison.ComparisonResults.Add(new BenchmarkResultComparison
                    {
                        BenchmarkName = currentResult.BenchmarkName,
                        Current = currentResult,
                        Baseline = null,
                        PercentChange = null,
                        IsRegression = false,
                        IsNew = true
                    });
                    continue;
                }

                // Calculate percent change: ((current - baseline) / baseline) * 100
                double percentChange = 0;
                if (baselineResult.MeanNanoseconds > 0)
                {
                    percentChange = ((currentResult.MeanNanoseconds - baselineResult.MeanNanoseconds) / baselineResult.MeanNanoseconds) * 100;
                }

                comparison.ComparisonResults.Add(new BenchmarkResultComparison
                {
                    BenchmarkName = currentResult.BenchmarkName,
                    Current = currentResult,
                    Baseline = baselineResult,
                    PercentChange = percentChange,
                    IsRegression = false, // Will be determined by HasRegression method
                    IsNew = false
                });
            }

            return Result.Ok(comparison);
        }
        catch (Exception ex)
        {
            return Result.Fail<BenchmarkComparison>($"Failed to compare benchmarks: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Determines if a benchmark comparison indicates a performance regression at the specified threshold.
    /// </summary>
    /// <param name="comparison">Benchmark comparison to evaluate.</param>
    /// <param name="thresholdPercent">Regression threshold percentage (e.g., 10.0 for 10%).</param>
    /// <returns>Result containing regression detection information.</returns>
    public Result<RegressionDetectionResult> HasRegression(BenchmarkComparison comparison, double thresholdPercent)
    {
        try
        {
            if (comparison == null)
                return Result.Fail<RegressionDetectionResult>("Comparison cannot be null");

            if (thresholdPercent < 0)
                return Result.Fail<RegressionDetectionResult>("Threshold must be non-negative");

            var result = new RegressionDetectionResult
            {
                Threshold = thresholdPercent,
                Regressions = new List<BenchmarkResultComparison>(),
                Improvements = new List<BenchmarkResultComparison>()
            };

            foreach (var comparisonResult in comparison.ComparisonResults)
            {
                // Skip new benchmarks
                if (comparisonResult.IsNew || comparisonResult.PercentChange == null)
                    continue;

                var percentChange = comparisonResult.PercentChange.Value;

                // Positive percent change = slower = regression
                if (percentChange >= thresholdPercent)
                {
                    comparisonResult.IsRegression = true;
                    result.Regressions.Add(comparisonResult);
                }
                // Negative percent change = faster = improvement
                else if (percentChange <= -thresholdPercent)
                {
                    result.Improvements.Add(comparisonResult);
                }
            }

            result.HasRegressions = result.Regressions.Count > 0;

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            return Result.Fail<RegressionDetectionResult>($"Failed to detect regressions: {ex.Message}")
                .WithError(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// Lists all baseline files in the baselines directory.
    /// </summary>
    /// <returns>List of baseline filenames.</returns>
    public List<string> ListBaselines()
    {
        if (!Directory.Exists(_baselinesDirectory))
            return new List<string>();

        return Directory.GetFiles(_baselinesDirectory, "baseline-*.json")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Select(f => f!)
            .OrderByDescending(f => f)
            .ToList();
    }
}

/// <summary>
/// Represents benchmark run information including metadata and results.
/// </summary>
public class BenchmarkRunInfo
{
    /// <summary>
    /// Git commit SHA for the benchmark run.
    /// </summary>
    public string CommitSha { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the benchmark was run.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Hardware information for the system where benchmarks were executed.
    /// </summary>
    public HardwareInfo HardwareInfo { get; set; } = new();

    /// <summary>
    /// Collection of benchmark results.
    /// </summary>
    public List<BenchmarkResult> Results { get; set; } = new();
}

/// <summary>
/// Hardware information for benchmark execution context.
/// </summary>
public class HardwareInfo
{
    /// <summary>
    /// CPU model and specifications.
    /// </summary>
    public string Cpu { get; set; } = string.Empty;

    /// <summary>
    /// Total RAM in GB.
    /// </summary>
    public int RamGb { get; set; }

    /// <summary>
    /// Operating system information.
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// .NET runtime version.
    /// </summary>
    public string RuntimeVersion { get; set; } = string.Empty;
}

/// <summary>
/// Individual benchmark result with timing and memory metrics.
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Name of the benchmark method.
    /// </summary>
    public string BenchmarkName { get; set; } = string.Empty;

    /// <summary>
    /// Mean execution time in nanoseconds.
    /// </summary>
    public double MeanNanoseconds { get; set; }

    /// <summary>
    /// Standard deviation of execution time.
    /// </summary>
    public double StdDevNanoseconds { get; set; }

    /// <summary>
    /// P50 (median) percentile in nanoseconds.
    /// </summary>
    public double P50Nanoseconds { get; set; }

    /// <summary>
    /// P95 percentile in nanoseconds.
    /// </summary>
    public double P95Nanoseconds { get; set; }

    /// <summary>
    /// P99 percentile in nanoseconds.
    /// </summary>
    public double P99Nanoseconds { get; set; }

    /// <summary>
    /// Total allocated bytes (managed memory).
    /// </summary>
    public long AllocatedBytes { get; set; }

    /// <summary>
    /// Number of Gen0 garbage collections.
    /// </summary>
    public int Gen0Collections { get; set; }

    /// <summary>
    /// Number of Gen1 garbage collections.
    /// </summary>
    public int Gen1Collections { get; set; }

    /// <summary>
    /// Number of Gen2 garbage collections.
    /// </summary>
    public int Gen2Collections { get; set; }
}

/// <summary>
/// Comparison between current and baseline benchmark runs.
/// </summary>
public class BenchmarkComparison
{
    /// <summary>
    /// Current benchmark run information.
    /// </summary>
    public BenchmarkRunInfo CurrentRun { get; set; } = new();

    /// <summary>
    /// Baseline benchmark run information.
    /// </summary>
    public BenchmarkRunInfo BaselineRun { get; set; } = new();

    /// <summary>
    /// Individual benchmark comparisons.
    /// </summary>
    public List<BenchmarkResultComparison> ComparisonResults { get; set; } = new();
}

/// <summary>
/// Comparison of a single benchmark result between current and baseline.
/// </summary>
public class BenchmarkResultComparison
{
    /// <summary>
    /// Name of the benchmark.
    /// </summary>
    public string BenchmarkName { get; set; } = string.Empty;

    /// <summary>
    /// Current benchmark result.
    /// </summary>
    public BenchmarkResult? Current { get; set; }

    /// <summary>
    /// Baseline benchmark result (null if benchmark is new).
    /// </summary>
    public BenchmarkResult? Baseline { get; set; }

    /// <summary>
    /// Percent change from baseline (positive = slower, negative = faster).
    /// Null if benchmark is new or baseline is missing.
    /// </summary>
    public double? PercentChange { get; set; }

    /// <summary>
    /// Indicates if this result represents a performance regression.
    /// </summary>
    public bool IsRegression { get; set; }

    /// <summary>
    /// Indicates if this is a new benchmark not present in baseline.
    /// </summary>
    public bool IsNew { get; set; }
}

/// <summary>
/// Result of regression detection analysis.
/// </summary>
public class RegressionDetectionResult
{
    /// <summary>
    /// Regression threshold percentage used for detection.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Indicates if any regressions were detected.
    /// </summary>
    public bool HasRegressions { get; set; }

    /// <summary>
    /// List of benchmarks with detected regressions.
    /// </summary>
    public List<BenchmarkResultComparison> Regressions { get; set; } = new();

    /// <summary>
    /// List of benchmarks with improvements.
    /// </summary>
    public List<BenchmarkResultComparison> Improvements { get; set; } = new();
}
