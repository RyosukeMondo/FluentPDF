namespace FluentPDF.App.Testing;

/// <summary>
/// Defines a contract for command-line interface tests that verify FluentPDF features.
/// All CLI tests implement this interface to provide consistent execution and verification patterns.
/// </summary>
public interface ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// Used for test discovery and selective execution via --run-test command.
    /// </summary>
    /// <example>render-pdf, search-text, extract-bookmarks</example>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// Displayed in test listings and reports to explain test purpose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the test operation using the provided isolated test context.
    /// Performs the feature operation and captures results for verification.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services, working directory, and logger</param>
    /// <returns>Test result containing success/failure status, outputs, and diagnostic information</returns>
    /// <exception cref="OperationCanceledException">Thrown when test execution is cancelled</exception>
    Task<CliTestResult> RunAsync(CliTestContext context);

    /// <summary>
    /// Verifies that the test result meets expected criteria.
    /// Checks output files, exit codes, log contents, or other verifiable artifacts.
    /// </summary>
    /// <param name="result">The test result produced by RunAsync</param>
    /// <returns>True if verification passes, false if actual results don't match expectations</returns>
    Task<bool> VerifyAsync(CliTestResult result);
}
