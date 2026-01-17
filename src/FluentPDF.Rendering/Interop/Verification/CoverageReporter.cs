using System.Text;

namespace FluentPDF.Rendering.Interop.Verification;

/// <summary>
/// Generates human-readable markdown reports from marshalling verification coverage data.
/// </summary>
public class CoverageReporter
{
    /// <summary>
    /// Generates a comprehensive markdown report from coverage data.
    /// </summary>
    /// <param name="report">The coverage report to format.</param>
    /// <param name="includeDetails">Whether to include detailed results for each function.</param>
    /// <returns>A formatted markdown report.</returns>
    public string GenerateMarkdownReport(CoverageReport report, bool includeDetails = true)
    {
        var sb = new StringBuilder();

        // Report header with timestamp
        sb.AppendLine("# P/Invoke Marshalling Verification Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();

        // Summary statistics
        GenerateSummarySection(sb, report);

        // Critical issues section (if any)
        if (report.HasCriticalGaps)
        {
            GenerateCriticalIssuesSection(sb, report);
        }

        // Detailed results (if requested)
        if (includeDetails)
        {
            GenerateDetailedResultsSection(sb, report);
        }

        // Recommendations
        GenerateRecommendationsSection(sb, report);

        return sb.ToString();
    }

    /// <summary>
    /// Generates a console-friendly summary of the coverage report.
    /// </summary>
    /// <param name="report">The coverage report to summarize.</param>
    /// <returns>A formatted console summary.</returns>
    public string GenerateConsoleSummary(CoverageReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== P/Invoke Marshalling Verification Summary ===");
        sb.AppendLine();
        sb.AppendLine($"Total Functions:    {report.TotalFunctions}");
        sb.AppendLine($"Verified:           {report.VerifiedFunctions} ({report.CoveragePercentage:F1}%)");
        sb.AppendLine($"Tested:             {report.TestedFunctions} ({report.TestCoveragePercentage:F1}%)");
        sb.AppendLine($"Passed:             {report.PassedFunctions}");
        sb.AppendLine($"Failed:             {report.FailedFunctions}");
        sb.AppendLine($"Untested:           {report.UntestedFunctions.Count}");
        sb.AppendLine();

        if (report.HasCriticalGaps)
        {
            sb.AppendLine("⚠️  CRITICAL GAPS DETECTED");

            if (report.FailedFunctions > 0)
            {
                sb.AppendLine($"   - {report.FailedFunctions} function(s) failed verification");
            }

            if (report.UntestedFunctions.Count > 0)
            {
                sb.AppendLine($"   - {report.UntestedFunctions.Count} function(s) not tested");
            }
        }
        else
        {
            sb.AppendLine("✓ All functions verified successfully");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Saves a markdown report to a file.
    /// </summary>
    /// <param name="report">The coverage report to save.</param>
    /// <param name="outputPath">The file path to save to.</param>
    /// <param name="includeDetails">Whether to include detailed results.</param>
    public void SaveReport(CoverageReport report, string outputPath, bool includeDetails = true)
    {
        var markdown = GenerateMarkdownReport(report, includeDetails);
        File.WriteAllText(outputPath, markdown);
    }

    /// <summary>
    /// Generates the summary statistics section of the report.
    /// </summary>
    private void GenerateSummarySection(StringBuilder sb, CoverageReport report)
    {
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count | Percentage |");
        sb.AppendLine("|--------|-------|------------|");
        sb.AppendLine($"| Total Functions | {report.TotalFunctions} | 100% |");
        sb.AppendLine($"| Verified Signatures | {report.VerifiedFunctions} | {report.CoveragePercentage:F1}% |");
        sb.AppendLine($"| Tested Functions | {report.TestedFunctions} | {report.TestCoveragePercentage:F1}% |");
        sb.AppendLine($"| Passed All Checks | {report.PassedFunctions} | {GetPercentage(report.PassedFunctions, report.TotalFunctions):F1}% |");
        sb.AppendLine($"| Failed Verification | {report.FailedFunctions} | {GetPercentage(report.FailedFunctions, report.TotalFunctions):F1}% |");
        sb.AppendLine($"| Untested | {report.UntestedFunctions.Count} | {GetPercentage(report.UntestedFunctions.Count, report.TotalFunctions):F1}% |");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates the critical issues section of the report.
    /// </summary>
    private void GenerateCriticalIssuesSection(StringBuilder sb, CoverageReport report)
    {
        sb.AppendLine("## ⚠️ Critical Issues");
        sb.AppendLine();

        // Failed functions (highest priority)
        if (report.FailedFunctions > 0)
        {
            sb.AppendLine("### Failed Verification");
            sb.AppendLine();
            sb.AppendLine("The following functions failed marshalling verification and require immediate attention:");
            sb.AppendLine();

            var failedResults = report.Results
                .Where(r => !r.SignatureValid || (r.MarshallingCorrect.HasValue && !r.MarshallingCorrect.Value))
                .OrderBy(r => r.FunctionName);

            foreach (var result in failedResults)
            {
                sb.AppendLine($"- **{result.FunctionName}**");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    sb.AppendLine($"  - Error: {result.ErrorMessage}");
                }
                if (result.TestResult != null && !string.IsNullOrEmpty(result.TestResult.ErrorDetails))
                {
                    sb.AppendLine($"  - Test Details: {result.TestResult.ErrorDetails}");
                }
            }

            sb.AppendLine();
        }

        // Untested functions
        if (report.UntestedFunctions.Count > 0)
        {
            sb.AppendLine("### Untested Functions");
            sb.AppendLine();
            sb.AppendLine("The following functions have not been tested for marshalling correctness:");
            sb.AppendLine();

            foreach (var funcName in report.UntestedFunctions.OrderBy(f => f))
            {
                sb.AppendLine($"- {funcName}");
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates the detailed results section of the report.
    /// </summary>
    private void GenerateDetailedResultsSection(StringBuilder sb, CoverageReport report)
    {
        sb.AppendLine("## Detailed Results");
        sb.AppendLine();

        // Sort: failed first, then untested, then passed
        var sortedResults = report.Results
            .OrderBy(r => GetSortPriority(r))
            .ThenBy(r => r.FunctionName);

        sb.AppendLine("| Function | Signature | Marshalling | Status |");
        sb.AppendLine("|----------|-----------|-------------|--------|");

        foreach (var result in sortedResults)
        {
            var signatureStatus = result.SignatureValid ? "✓" : "✗";
            var marshallingStatus = result.MarshallingCorrect switch
            {
                true => "✓",
                false => "✗",
                null => "-"
            };
            var overallStatus = GetOverallStatus(result);

            sb.AppendLine($"| {result.FunctionName} | {signatureStatus} | {marshallingStatus} | {overallStatus} |");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Generates the recommendations section of the report.
    /// </summary>
    private void GenerateRecommendationsSection(StringBuilder sb, CoverageReport report)
    {
        sb.AppendLine("## Recommendations");
        sb.AppendLine();

        if (!report.HasCriticalGaps)
        {
            sb.AppendLine("✓ All P/Invoke functions have been verified. No action required.");
            return;
        }

        var recommendations = new List<string>();

        if (report.FailedFunctions > 0)
        {
            recommendations.Add($"**Immediate Action Required:** Fix {report.FailedFunctions} failed function(s)");
            recommendations.Add("  - Review error messages in the 'Critical Issues' section");
            recommendations.Add("  - Compare P/Invoke signatures against PDFium API documentation");
            recommendations.Add("  - Verify return types match expected values (e.g., float vs. double)");
            recommendations.Add("  - Update DllImport attributes as needed");
        }

        if (report.UntestedFunctions.Count > 0)
        {
            recommendations.Add($"**High Priority:** Add marshalling tests for {report.UntestedFunctions.Count} untested function(s)");
            recommendations.Add("  - Create test cases with known input/output data");
            recommendations.Add("  - Use test PDF files from tests/Fixtures directory");
            recommendations.Add("  - Verify data marshals correctly at runtime");
        }

        if (report.TestCoveragePercentage < 80)
        {
            recommendations.Add("**Improve Coverage:** Test coverage is below 80%");
            recommendations.Add($"  - Current: {report.TestCoveragePercentage:F1}%");
            recommendations.Add("  - Target: 80% minimum for critical functions");
        }

        foreach (var rec in recommendations)
        {
            sb.AppendLine(rec);
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("For more information on fixing marshalling errors, see docs/marshalling-verification.md");
    }

    /// <summary>
    /// Gets the sort priority for a verification result (lower = higher priority).
    /// </summary>
    private static int GetSortPriority(VerificationResult result)
    {
        // Failed results have highest priority (0)
        if (!result.SignatureValid || (result.MarshallingCorrect.HasValue && !result.MarshallingCorrect.Value))
        {
            return 0;
        }

        // Untested results have medium priority (1)
        if (!result.MarshallingCorrect.HasValue)
        {
            return 1;
        }

        // Passed results have lowest priority (2)
        return 2;
    }

    /// <summary>
    /// Gets the overall status string for a verification result.
    /// </summary>
    private static string GetOverallStatus(VerificationResult result)
    {
        if (!result.SignatureValid)
        {
            return "❌ Signature Invalid";
        }

        if (result.MarshallingCorrect.HasValue)
        {
            return result.MarshallingCorrect.Value ? "✅ Passed" : "❌ Marshalling Failed";
        }

        return "⚠️ Untested";
    }

    /// <summary>
    /// Calculates percentage safely, handling division by zero.
    /// </summary>
    private static double GetPercentage(int value, int total)
    {
        return total > 0 ? (double)value / total * 100 : 0;
    }
}
