using System.Text;
using FluentPDF.Benchmarks.Utils;
using FluentResults;

namespace FluentPDF.Benchmarks.Reporting;

/// <summary>
/// Generates HTML performance reports from benchmark results and baseline comparisons.
/// </summary>
public class ReportGenerator
{
    private readonly string _templatePath;

    /// <summary>
    /// Initializes a new instance of ReportGenerator with the specified template path.
    /// </summary>
    /// <param name="templatePath">Path to the HTML report template file.</param>
    public ReportGenerator(string templatePath)
    {
        _templatePath = templatePath;
    }

    /// <summary>
    /// Generates an HTML performance report from benchmark results and optional baseline comparison.
    /// </summary>
    /// <param name="current">Current benchmark run information.</param>
    /// <param name="comparison">Optional benchmark comparison with baseline.</param>
    /// <param name="outputPath">Path where the HTML report should be written.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    public Result GenerateReport(BenchmarkRunInfo current, BenchmarkComparison? comparison, string outputPath)
    {
        try
        {
            if (current == null)
                return Result.Fail("Current benchmark run info cannot be null");

            if (string.IsNullOrWhiteSpace(outputPath))
                return Result.Fail("Output path cannot be empty");

            // Load template
            if (!File.Exists(_templatePath))
                return Result.Fail($"Template file not found: {_templatePath}");

            var template = File.ReadAllText(_templatePath);

            // Replace placeholders
            var html = template
                .Replace("{{TITLE}}", "FluentPDF Performance Report")
                .Replace("{{TIMESTAMP}}", current.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                .Replace("{{COMMIT_SHA}}", current.CommitSha)
                .Replace("{{HARDWARE_INFO}}", GenerateHardwareInfoHtml(current.HardwareInfo))
                .Replace("{{SUMMARY_TABLE}}", GenerateSummaryTableHtml(current.Results))
                .Replace("{{MEMORY_PROFILE}}", GenerateMemoryProfileHtml(current.Results))
                .Replace("{{COMPARISON_SECTION}}", GenerateComparisonSectionHtml(comparison))
                .Replace("{{CHART_DATA}}", GenerateChartDataJson(current.Results, comparison));

            // Write to output file
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(outputPath, html);

            return Result.Ok().WithSuccess($"Report generated successfully: {outputPath}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to generate report: {ex.Message}").WithError(new ExceptionalError(ex));
        }
    }

    private string GenerateHardwareInfoHtml(HardwareInfo hardware)
    {
        return $@"
            <div class=""hardware-info"">
                <div class=""info-item""><strong>CPU:</strong> {Escape(hardware.Cpu)}</div>
                <div class=""info-item""><strong>RAM:</strong> {hardware.RamGb} GB</div>
                <div class=""info-item""><strong>OS:</strong> {Escape(hardware.OperatingSystem)}</div>
                <div class=""info-item""><strong>Runtime:</strong> {Escape(hardware.RuntimeVersion)}</div>
            </div>";
    }

    private string GenerateSummaryTableHtml(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"
            <table class=""summary-table"">
                <thead>
                    <tr>
                        <th>Benchmark</th>
                        <th>Mean</th>
                        <th>StdDev</th>
                        <th>P50</th>
                        <th>P95</th>
                        <th>P99</th>
                        <th>Allocated</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var result in results.OrderBy(r => r.BenchmarkName))
        {
            sb.AppendLine($@"
                    <tr>
                        <td class=""benchmark-name"">{Escape(result.BenchmarkName)}</td>
                        <td>{FormatTime(result.MeanNanoseconds)}</td>
                        <td>{FormatTime(result.StdDevNanoseconds)}</td>
                        <td>{FormatTime(result.P50Nanoseconds)}</td>
                        <td>{FormatTime(result.P95Nanoseconds)}</td>
                        <td>{FormatTime(result.P99Nanoseconds)}</td>
                        <td>{FormatBytes(result.AllocatedBytes)}</td>
                    </tr>");
        }

        sb.AppendLine(@"
                </tbody>
            </table>");

        return sb.ToString();
    }

    private string GenerateMemoryProfileHtml(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"
            <table class=""memory-table"">
                <thead>
                    <tr>
                        <th>Benchmark</th>
                        <th>Allocated</th>
                        <th>Gen0</th>
                        <th>Gen1</th>
                        <th>Gen2</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var result in results.OrderBy(r => r.BenchmarkName))
        {
            sb.AppendLine($@"
                    <tr>
                        <td class=""benchmark-name"">{Escape(result.BenchmarkName)}</td>
                        <td>{FormatBytes(result.AllocatedBytes)}</td>
                        <td>{result.Gen0Collections}</td>
                        <td>{result.Gen1Collections}</td>
                        <td>{result.Gen2Collections}</td>
                    </tr>");
        }

        sb.AppendLine(@"
                </tbody>
            </table>");

        return sb.ToString();
    }

    private string GenerateComparisonSectionHtml(BenchmarkComparison? comparison)
    {
        if (comparison == null || comparison.ComparisonResults.Count == 0)
        {
            return @"<div class=""no-comparison"">No baseline comparison available</div>";
        }

        var sb = new StringBuilder();
        sb.AppendLine($@"
            <div class=""comparison-info"">
                <p><strong>Baseline:</strong> {comparison.BaselineRun.CommitSha} ({comparison.BaselineRun.Timestamp:yyyy-MM-dd})</p>
            </div>
            <table class=""comparison-table"">
                <thead>
                    <tr>
                        <th>Benchmark</th>
                        <th>Current</th>
                        <th>Baseline</th>
                        <th>Change</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var compResult in comparison.ComparisonResults.OrderBy(r => r.BenchmarkName))
        {
            string statusClass = "neutral";
            string statusText = "Same";
            string changeText = "-";

            if (compResult.IsNew)
            {
                statusClass = "new";
                statusText = "New";
            }
            else if (compResult.PercentChange.HasValue)
            {
                var change = compResult.PercentChange.Value;
                changeText = $"{change:+0.00;-0.00;0}%";

                if (compResult.IsRegression)
                {
                    statusClass = "regression";
                    statusText = "Regression";
                }
                else if (change <= -10)
                {
                    statusClass = "improvement";
                    statusText = "Improvement";
                }
            }

            var currentTime = compResult.Current != null ? FormatTime(compResult.Current.MeanNanoseconds) : "-";
            var baselineTime = compResult.Baseline != null ? FormatTime(compResult.Baseline.MeanNanoseconds) : "-";

            sb.AppendLine($@"
                    <tr class=""{statusClass}"">
                        <td class=""benchmark-name"">{Escape(compResult.BenchmarkName)}</td>
                        <td>{currentTime}</td>
                        <td>{baselineTime}</td>
                        <td class=""change"">{changeText}</td>
                        <td class=""status"">{statusText}</td>
                    </tr>");
        }

        sb.AppendLine(@"
                </tbody>
            </table>");

        return sb.ToString();
    }

    private string GenerateChartDataJson(List<BenchmarkResult> results, BenchmarkComparison? comparison)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");

        // Latency distribution data
        sb.AppendLine("  \"latencyDistribution\": {");
        sb.AppendLine($"    \"labels\": [{string.Join(", ", results.Select(r => $"\"{Escape(r.BenchmarkName)}\""))}],");
        sb.AppendLine($"    \"p50\": [{string.Join(", ", results.Select(r => r.P50Nanoseconds / 1_000_000))}],");
        sb.AppendLine($"    \"p95\": [{string.Join(", ", results.Select(r => r.P95Nanoseconds / 1_000_000))}],");
        sb.AppendLine($"    \"p99\": [{string.Join(", ", results.Select(r => r.P99Nanoseconds / 1_000_000))}]");
        sb.AppendLine("  },");

        // Memory allocation data
        sb.AppendLine("  \"memoryAllocation\": {");
        sb.AppendLine($"    \"labels\": [{string.Join(", ", results.Select(r => $"\"{Escape(r.BenchmarkName)}\""))}],");
        sb.AppendLine($"    \"allocated\": [{string.Join(", ", results.Select(r => r.AllocatedBytes / 1024.0 / 1024.0))}],");
        sb.AppendLine($"    \"gen0\": [{string.Join(", ", results.Select(r => r.Gen0Collections))}],");
        sb.AppendLine($"    \"gen1\": [{string.Join(", ", results.Select(r => r.Gen1Collections))}],");
        sb.AppendLine($"    \"gen2\": [{string.Join(", ", results.Select(r => r.Gen2Collections))}]");
        sb.AppendLine("  }");

        // Comparison data if available
        if (comparison != null && comparison.ComparisonResults.Count > 0)
        {
            sb.AppendLine("  ,");
            sb.AppendLine("  \"comparison\": {");
            sb.AppendLine($"    \"labels\": [{string.Join(", ", comparison.ComparisonResults.Select(r => $"\"{Escape(r.BenchmarkName)}\""))}],");
            sb.AppendLine($"    \"percentChanges\": [{string.Join(", ", comparison.ComparisonResults.Select(r => r.PercentChange?.ToString("F2") ?? "null"))}]");
            sb.AppendLine("  }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string FormatTime(double nanoseconds)
    {
        if (nanoseconds < 1_000)
            return $"{nanoseconds:F2} ns";
        if (nanoseconds < 1_000_000)
            return $"{nanoseconds / 1_000:F2} μs";
        if (nanoseconds < 1_000_000_000)
            return $"{nanoseconds / 1_000_000:F2} ms";
        return $"{nanoseconds / 1_000_000_000:F2} s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / 1024.0 / 1024.0:F2} MB";
        return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
    }

    private static string Escape(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
