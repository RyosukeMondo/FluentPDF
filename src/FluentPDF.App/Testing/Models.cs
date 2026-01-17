using Serilog;

namespace FluentPDF.App.Testing;

/// <summary>
/// Represents the result of executing a single CLI test.
/// Contains test status, timing, outputs, and diagnostic information.
/// </summary>
public sealed class CliTestResult
{
    /// <summary>
    /// Gets or sets the name of the test that was executed.
    /// </summary>
    public required string TestName { get; set; }

    /// <summary>
    /// Gets or sets whether the test passed all verification checks.
    /// True if test executed successfully and verification passed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the test.
    /// Includes both test execution and verification time.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the error message if the test failed.
    /// Null if test succeeded. Contains exception details or verification failure details.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the outputs produced by the test.
    /// Keys represent output types (e.g., "OutputFiles", "Metrics", "ExitCode").
    /// Values contain the actual output data for verification.
    /// </summary>
    public Dictionary<string, object> Outputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the log entries captured during test execution.
    /// Contains structured log messages for debugging and diagnostics.
    /// </summary>
    public List<string> LogEntries { get; set; } = new();
}

/// <summary>
/// Represents the aggregated results of running a test suite.
/// Contains summary statistics and individual test results.
/// </summary>
public sealed class TestSuiteResult
{
    /// <summary>
    /// Gets or sets the total number of tests in the suite.
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Gets or sets the number of tests that passed all verification checks.
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// Gets or sets the number of tests that failed execution or verification.
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// Gets or sets the total duration for executing all tests in the suite.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the individual test results for each test in the suite.
    /// </summary>
    public List<CliTestResult> Results { get; set; } = new();

    /// <summary>
    /// Gets whether all tests in the suite passed.
    /// </summary>
    public bool AllTestsPassed => FailedTests == 0 && TotalTests > 0;
}

/// <summary>
/// Defines a contract for verification rules that validate test results.
/// Verification rules check specific aspects of test output against expected criteria.
/// </summary>
public interface IVerificationRule
{
    /// <summary>
    /// Gets the name of this verification rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Verifies that the test result meets the rule's criteria.
    /// </summary>
    /// <param name="result">The test result to verify</param>
    /// <returns>True if verification passes, false otherwise</returns>
    Task<bool> VerifyAsync(CliTestResult result);

    /// <summary>
    /// Gets a descriptive message explaining why verification failed.
    /// Called after VerifyAsync returns false to provide diagnostic information.
    /// </summary>
    /// <returns>Human-readable failure message with actionable details</returns>
    string GetFailureMessage();
}

/// <summary>
/// Provides an isolated execution environment for CLI tests.
/// Each test receives its own context with temporary directories, services, and logging.
/// </summary>
public sealed class CliTestContext : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the temporary working directory for this test.
    /// Directory is automatically cleaned up when context is disposed.
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// Provides access to all FluentPDF services for test execution.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the logger for this test execution.
    /// All test logging should go through this logger for proper capture.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Gets the test data dictionary for storing test-specific information.
    /// Tests can use this to share data between RunAsync and VerifyAsync.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();

    /// <summary>
    /// Disposes the test context and cleans up temporary resources.
    /// Attempts to delete the working directory. Logs warnings if cleanup fails.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (Directory.Exists(WorkingDirectory))
            {
                Directory.Delete(WorkingDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Log warning but don't fail - per requirement 4.4
            Logger.Warning(ex, "Failed to cleanup test working directory: {WorkingDirectory}", WorkingDirectory);
        }

        _disposed = true;
    }
}
