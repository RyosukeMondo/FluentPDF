using Serilog;

namespace FluentPDF.App.Testing;

/// <summary>
/// Orchestrates verification rules to validate test results.
/// Applies multiple verification rules and aggregates results into actionable reports.
/// </summary>
public sealed class ResultVerifier
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the ResultVerifier class.
    /// </summary>
    /// <param name="logger">Logger for verification operations</param>
    public ResultVerifier(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies a test result against a single verification rule.
    /// </summary>
    /// <param name="result">The test result to verify</param>
    /// <param name="rule">The verification rule to apply</param>
    /// <returns>True if verification passes, false otherwise</returns>
    public async Task<bool> VerifyResultAsync(CliTestResult result, IVerificationRule rule)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        try
        {
            _logger.Debug("Applying verification rule: {RuleName} to test: {TestName}", rule.Name, result.TestName);

            var passed = await rule.VerifyAsync(result);

            if (passed)
            {
                _logger.Debug("Verification rule {RuleName} passed", rule.Name);
            }
            else
            {
                var failureMessage = rule.GetFailureMessage();
                _logger.Warning("Verification rule {RuleName} failed: {FailureMessage}", rule.Name, failureMessage);
            }

            return passed;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Verification rule {RuleName} threw exception", rule.Name);
            return false;
        }
    }

    /// <summary>
    /// Verifies a test result against multiple verification rules.
    /// All rules must pass for verification to succeed.
    /// </summary>
    /// <param name="result">The test result to verify</param>
    /// <param name="rules">The verification rules to apply</param>
    /// <returns>True if all rules pass, false if any rule fails</returns>
    public async Task<bool> VerifyResultAsync(CliTestResult result, params IVerificationRule[] rules)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (rules is null || rules.Length == 0)
        {
            _logger.Warning("No verification rules provided for test: {TestName}", result.TestName);
            return true; // No rules means verification passes by default
        }

        _logger.Information("Verifying test {TestName} with {RuleCount} rules", result.TestName, rules.Length);

        var allPassed = true;
        foreach (var rule in rules)
        {
            var passed = await VerifyResultAsync(result, rule);
            if (!passed)
            {
                allPassed = false;
                // Continue checking other rules to collect all failures
            }
        }

        return allPassed;
    }

    /// <summary>
    /// Generates a verification report for a collection of test results.
    /// </summary>
    /// <param name="results">The test results to report on</param>
    /// <returns>Formatted verification report</returns>
    public VerificationReport GenerateReport(List<CliTestResult> results)
    {
        if (results is null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        var report = new VerificationReport
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            TotalDuration = TimeSpan.FromMilliseconds(results.Sum(r => r.Duration.TotalMilliseconds)),
            TestResults = results
        };

        _logger.Information("Generated verification report: {Passed}/{Total} tests passed", report.PassedTests, report.TotalTests);

        return report;
    }
}

/// <summary>
/// Represents a verification report for a collection of test results.
/// </summary>
public sealed class VerificationReport
{
    /// <summary>
    /// Gets or sets the total number of tests in the report.
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Gets or sets the number of tests that passed verification.
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// Gets or sets the number of tests that failed verification.
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// Gets or sets the total duration for all tests.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the individual test results.
    /// </summary>
    public List<CliTestResult> TestResults { get; set; } = new();

    /// <summary>
    /// Gets whether all tests passed.
    /// </summary>
    public bool AllPassed => FailedTests == 0 && TotalTests > 0;

    /// <summary>
    /// Generates a formatted text summary of the report.
    /// </summary>
    /// <returns>Human-readable report summary</returns>
    public string GetSummary()
    {
        var lines = new List<string>
        {
            "Test Verification Report",
            "=======================",
            $"Total Tests: {TotalTests}",
            $"Passed: {PassedTests}",
            $"Failed: {FailedTests}",
            $"Duration: {TotalDuration.TotalSeconds:F2}s",
            ""
        };

        if (FailedTests > 0)
        {
            lines.Add("Failed Tests:");
            foreach (var result in TestResults.Where(r => !r.Success))
            {
                lines.Add($"  - {result.TestName}: {result.ErrorMessage}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
