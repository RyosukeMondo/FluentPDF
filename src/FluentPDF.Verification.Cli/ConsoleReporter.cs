using System.Text;
using FluentPDF.Verification.Core;
using FluentResults;
using Serilog;

namespace FluentPDF.Verification.Cli;

/// <summary>
/// Console reporter with color-coded output for CLI usage.
/// Provides enhanced readability with ANSI color codes and emoji indicators.
/// </summary>
public class ConsoleReporter : ReportGeneratorBase
{
    private const string GreenCheck = "\u2713"; // ✓
    private const string RedCross = "\u2717";   // ✗
    private const string AnsiReset = "\u001b[0m";
    private const string AnsiGreen = "\u001b[32m";
    private const string AnsiRed = "\u001b[31m";
    private const string AnsiYellow = "\u001b[33m";
    private const string AnsiCyan = "\u001b[36m";
    private const string AnsiBold = "\u001b[1m";
    private const string AnsiDim = "\u001b[2m";

    private readonly bool _useColors;

    /// <summary>
    /// Initializes a new instance of the ConsoleReporter class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    /// <param name="useColors">Whether to use ANSI color codes (default: true).</param>
    public ConsoleReporter(ILogger logger, bool useColors = true) : base(logger)
    {
        _useColors = useColors;
    }

    /// <inheritdoc />
    public override Result<string> GenerateConsoleReport(VerificationSummary summary)
    {
        try
        {
            var sb = new StringBuilder();

            // Header with overall status
            AppendSeparator(sb, '=');
            AppendLine(sb, "PDFIUM VERIFICATION REPORT", AnsiBold + AnsiCyan);
            AppendSeparator(sb, '=');
            sb.AppendLine();

            // Summary section with color-coded status
            AppendLine(sb, "Summary", AnsiBold);
            AppendLine(sb, new string('-', 40), AnsiDim);

            sb.Append($"  Library Version:  ");
            AppendLine(sb, summary.LibraryVersion ?? "Unknown", AnsiCyan);

            sb.Append($"  Total Tests:      ");
            AppendLine(sb, summary.TotalTests.ToString());

            sb.Append($"  Passed:           ");
            AppendLine(sb, $"{GreenCheck} {summary.PassedTests}", AnsiGreen);

            sb.Append($"  Failed:           ");
            if (summary.FailedTests > 0)
            {
                AppendLine(sb, $"{RedCross} {summary.FailedTests}", AnsiRed);
            }
            else
            {
                AppendLine(sb, "0");
            }

            sb.Append($"  Duration:         ");
            AppendLine(sb, FormatDuration(summary.TotalDuration), AnsiYellow);

            sb.Append($"  Overall Status:   ");
            if (summary.Success)
            {
                AppendLine(sb, $"{GreenCheck} PASS", AnsiBold + AnsiGreen);
            }
            else
            {
                AppendLine(sb, $"{RedCross} FAIL", AnsiBold + AnsiRed);
            }

            sb.AppendLine();

            // Detailed results
            if (summary.Results.Any())
            {
                AppendLine(sb, "Detailed Results", AnsiBold);
                AppendLine(sb, new string('-', 40), AnsiDim);
                sb.AppendLine();

                // Group by success/failure for better readability
                var failed = summary.Results.Where(r => !r.Success).ToList();
                var passed = summary.Results.Where(r => r.Success).ToList();

                // Show failures first (most important)
                if (failed.Any())
                {
                    AppendLine(sb, $"Failed Tests ({failed.Count}):", AnsiBold + AnsiRed);
                    sb.AppendLine();

                    foreach (var result in failed)
                    {
                        AppendFailedTest(sb, result);
                    }
                }

                // Show passed tests (concise format)
                if (passed.Any())
                {
                    AppendLine(sb, $"Passed Tests ({passed.Count}):", AnsiBold + AnsiGreen);
                    sb.AppendLine();

                    foreach (var result in passed)
                    {
                        AppendPassedTest(sb, result);
                    }
                }
            }

            AppendSeparator(sb, '=');

            // Final summary line
            if (summary.Success)
            {
                AppendLine(sb, $"{GreenCheck} All verifications passed successfully!", AnsiBold + AnsiGreen);
            }
            else
            {
                AppendLine(sb, $"{RedCross} {summary.FailedTests} verification(s) failed. See details above.", AnsiBold + AnsiRed);
            }

            AppendSeparator(sb, '=');

            Logger.Information("Generated color-coded console report for {TotalTests} tests", summary.TotalTests);
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
        // Delegate to JsonReporter for consistency
        Logger.Warning("ConsoleReporter.GenerateJsonReport called - consider using JsonReporter instead");
        return SerializeToJson(summary);
    }

    /// <inheritdoc />
    public override Result<string> GenerateHtmlReport(VerificationSummary summary)
    {
        Logger.Warning("HTML report generation not supported by ConsoleReporter");
        return Result.Fail("HTML report generation is not supported by ConsoleReporter. Use DefaultReportGenerator instead.");
    }

    private void AppendFailedTest(StringBuilder sb, VerificationResult result)
    {
        // Test header with failure indicator
        sb.Append("  ");
        AppendLine(sb, $"{RedCross} {result.TestName}", AnsiBold + AnsiRed);

        // Duration
        sb.Append("    ");
        AppendLine(sb, $"Duration: {FormatDuration(result.Duration)}", AnsiDim);

        // Error message
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            sb.Append("    ");
            AppendLine(sb, "Error:", AnsiRed);
            foreach (var line in result.ErrorMessage.Split('\n'))
            {
                sb.Append("      ");
                AppendLine(sb, line.TrimEnd(), AnsiRed);
            }
        }

        // Suggested fix
        if (!string.IsNullOrWhiteSpace(result.SuggestedFix))
        {
            sb.Append("    ");
            AppendLine(sb, "Suggested Fix:", AnsiYellow);
            foreach (var line in result.SuggestedFix.Split('\n'))
            {
                sb.Append("      ");
                AppendLine(sb, line.TrimEnd(), AnsiYellow);
            }
        }

        // Metadata
        if (result.Metadata.Any())
        {
            sb.Append("    ");
            AppendLine(sb, "Details:", AnsiDim);
            foreach (var kvp in result.Metadata)
            {
                sb.Append("      ");
                AppendLine(sb, $"{kvp.Key}: {kvp.Value}", AnsiDim);
            }
        }

        sb.AppendLine();
    }

    private void AppendPassedTest(StringBuilder sb, VerificationResult result)
    {
        sb.Append("  ");
        AppendText(sb, $"{GreenCheck} {result.TestName}", AnsiGreen);
        sb.Append(" ");
        AppendLine(sb, $"({FormatDuration(result.Duration)})", AnsiDim);
    }

    private void AppendSeparator(StringBuilder sb, char ch)
    {
        AppendLine(sb, new string(ch, 80), AnsiDim);
    }

    private void AppendLine(StringBuilder sb, string text, string? colorCode = null)
    {
        if (_useColors && !string.IsNullOrEmpty(colorCode))
        {
            sb.Append(colorCode);
            sb.Append(text);
            sb.AppendLine(AnsiReset);
        }
        else
        {
            sb.AppendLine(text);
        }
    }

    private void AppendText(StringBuilder sb, string text, string? colorCode = null)
    {
        if (_useColors && !string.IsNullOrEmpty(colorCode))
        {
            sb.Append(colorCode);
            sb.Append(text);
            sb.Append(AnsiReset);
        }
        else
        {
            sb.Append(text);
        }
    }
}
