using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using Serilog;

namespace FluentPDF.Verification.Core;

/// <summary>
/// Base abstract class for generating verification reports in various formats.
/// Provides thread-safe streaming output and structured logging integration.
/// </summary>
public abstract class ReportGeneratorBase : IVerificationReporter
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the ReportGeneratorBase class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    protected ReportGeneratorBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract Result<string> GenerateConsoleReport(VerificationSummary summary);

    /// <inheritdoc />
    public abstract Result<string> GenerateJsonReport(VerificationSummary summary);

    /// <inheritdoc />
    public abstract Result<string> GenerateHtmlReport(VerificationSummary summary);

    /// <inheritdoc />
    public async Task<Result> WriteReportAsync(string content, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result.Fail("Report content cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return Result.Fail("Output path cannot be empty");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Thread-safe file writing with streaming for large reports
            await WriteLock.WaitAsync(cancellationToken);
            try
            {
                await using var stream = new FileStream(
                    outputPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true);

                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(content.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
            finally
            {
                WriteLock.Release();
            }

            Logger.Information("Report written successfully to {OutputPath}", outputPath);
            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Report write operation was cancelled");
            return Result.Fail("Operation was cancelled");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error(ex, "Access denied writing report to {OutputPath}", outputPath);
            return Result.Fail($"Access denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            Logger.Error(ex, "I/O error writing report to {OutputPath}", outputPath);
            return Result.Fail($"I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error writing report to {OutputPath}", outputPath);
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats a duration in a human-readable format.
    /// </summary>
    protected static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:F0}ms";
        }
        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:F2}s";
        }
        return $"{duration.TotalMinutes:F1}m";
    }

    /// <summary>
    /// Safely serializes an object to JSON with consistent formatting.
    /// </summary>
    protected static Result<string> SerializeToJson<T>(T obj, bool indented = true)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(obj, options);
            return Result.Ok(json);
        }
        catch (JsonException ex)
        {
            return Result.Fail($"JSON serialization failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Unexpected serialization error: {ex.Message}");
        }
    }
}

/// <summary>
/// Default implementation of the report generator supporting all formats.
/// </summary>
public class DefaultReportGenerator : ReportGeneratorBase
{
    /// <summary>
    /// Initializes a new instance of the DefaultReportGenerator class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    public DefaultReportGenerator(ILogger logger) : base(logger)
    {
    }

    /// <inheritdoc />
    public override Result<string> GenerateConsoleReport(VerificationSummary summary)
    {
        try
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("VERIFICATION REPORT");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            // Summary section
            sb.AppendLine($"Library Version: {summary.LibraryVersion ?? "Unknown"}");
            sb.AppendLine($"Total Tests:     {summary.TotalTests}");
            sb.AppendLine($"Passed:          {summary.PassedTests}");
            sb.AppendLine($"Failed:          {summary.FailedTests}");
            sb.AppendLine($"Duration:        {FormatDuration(summary.TotalDuration)}");
            sb.AppendLine($"Status:          {(summary.Success ? "PASS" : "FAIL")}");
            sb.AppendLine();

            // Detailed results
            if (summary.Results.Any())
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("DETAILED RESULTS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();

                foreach (var result in summary.Results)
                {
                    var status = result.Success ? "[PASS]" : "[FAIL]";
                    sb.AppendLine($"{status} {result.TestName} ({FormatDuration(result.Duration)})");

                    if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        sb.AppendLine($"  Error: {result.ErrorMessage}");
                    }

                    if (!string.IsNullOrWhiteSpace(result.SuggestedFix))
                    {
                        sb.AppendLine($"  Fix:   {result.SuggestedFix}");
                    }

                    if (result.Metadata.Any())
                    {
                        foreach (var kvp in result.Metadata)
                        {
                            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                        }
                    }

                    sb.AppendLine();
                }
            }

            sb.AppendLine("=".PadRight(80, '='));

            Logger.Information("Generated console report for {TotalTests} tests", summary.TotalTests);
            return Result.Ok(sb.ToString());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to generate console report");
            return Result.Fail($"Console report generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override Result<string> GenerateJsonReport(VerificationSummary summary)
    {
        try
        {
            // Create a JSON-friendly structure
            var report = new
            {
                summary.LibraryVersion,
                summary.TotalTests,
                summary.PassedTests,
                summary.FailedTests,
                summary.Success,
                DurationSeconds = summary.TotalDuration.TotalSeconds,
                Results = summary.Results.Select(r => new
                {
                    r.TestName,
                    r.Success,
                    r.ErrorMessage,
                    r.SuggestedFix,
                    DurationSeconds = r.Duration.TotalSeconds,
                    r.Metadata
                }).ToList()
            };

            var jsonResult = SerializeToJson(report);
            if (jsonResult.IsSuccess)
            {
                Logger.Information("Generated JSON report for {TotalTests} tests", summary.TotalTests);
            }
            return jsonResult;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to generate JSON report");
            return Result.Fail($"JSON report generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override Result<string> GenerateHtmlReport(VerificationSummary summary)
    {
        try
        {
            var sb = new StringBuilder();

            // HTML structure
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>Verification Report</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("        h1 { color: #333; border-bottom: 3px solid #007acc; padding-bottom: 10px; }");
            sb.AppendLine("        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }");
            sb.AppendLine("        .summary-item { padding: 15px; border-radius: 5px; background: #f9f9f9; }");
            sb.AppendLine("        .summary-item strong { display: block; color: #666; font-size: 0.9em; margin-bottom: 5px; }");
            sb.AppendLine("        .summary-item .value { font-size: 1.5em; font-weight: bold; color: #333; }");
            sb.AppendLine("        .status-pass { color: #28a745; }");
            sb.AppendLine("        .status-fail { color: #dc3545; }");
            sb.AppendLine("        .results { margin-top: 30px; }");
            sb.AppendLine("        .result-item { margin: 15px 0; padding: 15px; border-left: 4px solid #ddd; background: #fafafa; border-radius: 4px; }");
            sb.AppendLine("        .result-item.pass { border-left-color: #28a745; }");
            sb.AppendLine("        .result-item.fail { border-left-color: #dc3545; background: #fff5f5; }");
            sb.AppendLine("        .result-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }");
            sb.AppendLine("        .result-name { font-weight: bold; font-size: 1.1em; }");
            sb.AppendLine("        .result-duration { color: #666; font-size: 0.9em; }");
            sb.AppendLine("        .error-message { color: #dc3545; margin: 10px 0; padding: 10px; background: #fff; border-radius: 4px; }");
            sb.AppendLine("        .suggested-fix { color: #0066cc; margin: 10px 0; padding: 10px; background: #e7f3ff; border-radius: 4px; }");
            sb.AppendLine("        .metadata { margin: 10px 0; font-size: 0.9em; color: #666; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");

            // Header
            sb.AppendLine("        <h1>Verification Report</h1>");

            // Summary
            sb.AppendLine("        <div class=\"summary\">");
            sb.AppendLine($"            <div class=\"summary-item\"><strong>Library Version</strong><div class=\"value\">{summary.LibraryVersion ?? "Unknown"}</div></div>");
            sb.AppendLine($"            <div class=\"summary-item\"><strong>Total Tests</strong><div class=\"value\">{summary.TotalTests}</div></div>");
            sb.AppendLine($"            <div class=\"summary-item\"><strong>Passed</strong><div class=\"value status-pass\">{summary.PassedTests}</div></div>");
            sb.AppendLine($"            <div class=\"summary-item\"><strong>Failed</strong><div class=\"value status-fail\">{summary.FailedTests}</div></div>");
            sb.AppendLine($"            <div class=\"summary-item\"><strong>Duration</strong><div class=\"value\">{FormatDuration(summary.TotalDuration)}</div></div>");
            sb.AppendLine($"            <div class=\"summary-item\"><strong>Status</strong><div class=\"value {(summary.Success ? "status-pass" : "status-fail")}\">{(summary.Success ? "PASS" : "FAIL")}</div></div>");
            sb.AppendLine("        </div>");

            // Detailed results
            if (summary.Results.Any())
            {
                sb.AppendLine("        <div class=\"results\">");
                sb.AppendLine("            <h2>Detailed Results</h2>");

                foreach (var result in summary.Results)
                {
                    var resultClass = result.Success ? "pass" : "fail";
                    sb.AppendLine($"            <div class=\"result-item {resultClass}\">");
                    sb.AppendLine("                <div class=\"result-header\">");
                    sb.AppendLine($"                    <span class=\"result-name\">{System.Net.WebUtility.HtmlEncode(result.TestName)}</span>");
                    sb.AppendLine($"                    <span class=\"result-duration\">{FormatDuration(result.Duration)}</span>");
                    sb.AppendLine("                </div>");

                    if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        sb.AppendLine($"                <div class=\"error-message\"><strong>Error:</strong> {System.Net.WebUtility.HtmlEncode(result.ErrorMessage)}</div>");
                    }

                    if (!string.IsNullOrWhiteSpace(result.SuggestedFix))
                    {
                        sb.AppendLine($"                <div class=\"suggested-fix\"><strong>Suggested Fix:</strong> {System.Net.WebUtility.HtmlEncode(result.SuggestedFix)}</div>");
                    }

                    if (result.Metadata.Any())
                    {
                        sb.AppendLine("                <div class=\"metadata\">");
                        foreach (var kvp in result.Metadata)
                        {
                            sb.AppendLine($"                    <div><strong>{System.Net.WebUtility.HtmlEncode(kvp.Key)}:</strong> {System.Net.WebUtility.HtmlEncode(kvp.Value.ToString() ?? "")}</div>");
                        }
                        sb.AppendLine("                </div>");
                    }

                    sb.AppendLine("            </div>");
                }

                sb.AppendLine("        </div>");
            }

            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            Logger.Information("Generated HTML report for {TotalTests} tests", summary.TotalTests);
            return Result.Ok(sb.ToString());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to generate HTML report");
            return Result.Fail($"HTML report generation failed: {ex.Message}");
        }
    }
}
