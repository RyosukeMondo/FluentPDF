using System.Text.Json;
using System.Text.Json.Serialization;
using FluentPDF.Verification.Core;
using FluentResults;
using Serilog;

namespace FluentPDF.Verification.Cli;

/// <summary>
/// JSON reporter optimized for CI/CD pipelines and automated processing.
/// Produces compact, valid JSON with standardized schema for easy parsing.
/// </summary>
public class JsonReporter : ReportGeneratorBase
{
    private readonly bool _indented;

    /// <summary>
    /// Initializes a new instance of the JsonReporter class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    /// <param name="indented">Whether to produce indented JSON (default: true for readability).</param>
    public JsonReporter(ILogger logger, bool indented = true) : base(logger)
    {
        _indented = indented;
    }

    /// <inheritdoc />
    public override Result<string> GenerateConsoleReport(VerificationSummary summary)
    {
        Logger.Warning("JsonReporter.GenerateConsoleReport called - consider using ConsoleReporter instead");
        return Result.Fail("Console report generation is not supported by JsonReporter. Use ConsoleReporter instead.");
    }

    /// <inheritdoc />
    public override Result<string> GenerateJsonReport(VerificationSummary summary)
    {
        try
        {
            // Create CI/CD-friendly JSON structure
            var report = new JsonVerificationReport
            {
                SchemaVersion = "1.0",
                Timestamp = DateTime.UtcNow,
                Library = new LibraryInfo
                {
                    Name = "PDFium",
                    Version = summary.LibraryVersion ?? "unknown"
                },
                Summary = new TestSummary
                {
                    Total = summary.TotalTests,
                    Passed = summary.PassedTests,
                    Failed = summary.FailedTests,
                    Success = summary.Success,
                    DurationMs = (int)summary.TotalDuration.TotalMilliseconds,
                    DurationSeconds = summary.TotalDuration.TotalSeconds
                },
                Results = summary.Results.Select(r => new JsonTestResult
                {
                    Name = r.TestName,
                    Status = r.Success ? "passed" : "failed",
                    DurationMs = (int)r.Duration.TotalMilliseconds,
                    DurationSeconds = r.Duration.TotalSeconds,
                    Error = r.Success ? null : new ErrorInfo
                    {
                        Message = r.ErrorMessage ?? "Unknown error",
                        SuggestedFix = r.SuggestedFix
                    },
                    Metadata = r.Metadata.Any() ? r.Metadata : null
                }).ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = _indented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            var json = JsonSerializer.Serialize(report, options);

            Logger.Information("Generated JSON report for {TotalTests} tests ({Size} bytes)",
                summary.TotalTests, json.Length);

            return Result.Ok(json);
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "JSON serialization failed");
            return Result.Fail($"JSON report generation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error generating JSON report");
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public override Result<string> GenerateHtmlReport(VerificationSummary summary)
    {
        Logger.Warning("HTML report generation not supported by JsonReporter");
        return Result.Fail("HTML report generation is not supported by JsonReporter. Use DefaultReportGenerator instead.");
    }

    #region JSON Schema Models

    /// <summary>
    /// Root JSON report structure optimized for CI/CD consumption.
    /// </summary>
    private class JsonVerificationReport
    {
        /// <summary>
        /// Schema version for compatibility tracking.
        /// </summary>
        public required string SchemaVersion { get; init; }

        /// <summary>
        /// UTC timestamp when the report was generated.
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        /// Information about the library being verified.
        /// </summary>
        public required LibraryInfo Library { get; init; }

        /// <summary>
        /// Test execution summary.
        /// </summary>
        public required TestSummary Summary { get; init; }

        /// <summary>
        /// Individual test results.
        /// </summary>
        public required List<JsonTestResult> Results { get; init; }
    }

    private class LibraryInfo
    {
        public required string Name { get; init; }
        public required string Version { get; init; }
    }

    private class TestSummary
    {
        public required int Total { get; init; }
        public required int Passed { get; init; }
        public required int Failed { get; init; }
        public required bool Success { get; init; }

        /// <summary>
        /// Duration in milliseconds (integer for easy comparison in CI/CD).
        /// </summary>
        public required int DurationMs { get; init; }

        /// <summary>
        /// Duration in seconds (decimal for human readability).
        /// </summary>
        public required double DurationSeconds { get; init; }
    }

    private class JsonTestResult
    {
        public required string Name { get; init; }

        /// <summary>
        /// Status as lowercase string: "passed" or "failed".
        /// </summary>
        public required string Status { get; init; }

        /// <summary>
        /// Duration in milliseconds (integer for easy comparison).
        /// </summary>
        public required int DurationMs { get; init; }

        /// <summary>
        /// Duration in seconds (decimal for human readability).
        /// </summary>
        public required double DurationSeconds { get; init; }

        /// <summary>
        /// Error information (null if test passed).
        /// </summary>
        public ErrorInfo? Error { get; init; }

        /// <summary>
        /// Additional metadata (null if empty).
        /// </summary>
        public Dictionary<string, object>? Metadata { get; init; }
    }

    private class ErrorInfo
    {
        public required string Message { get; init; }
        public string? SuggestedFix { get; init; }
    }

    #endregion
}
