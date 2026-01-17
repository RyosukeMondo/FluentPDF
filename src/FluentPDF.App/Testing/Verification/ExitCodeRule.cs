namespace FluentPDF.App.Testing.Verification;

/// <summary>
/// Verification rule that checks if a test produced the expected exit code.
/// Used to verify command-line operations returned correct status codes.
/// </summary>
public sealed class ExitCodeRule : IVerificationRule
{
    private readonly int _expectedExitCode;
    private int? _actualExitCode;

    /// <summary>
    /// Gets the name of this verification rule.
    /// </summary>
    public string Name => "ExitCode";

    /// <summary>
    /// Initializes a new instance of the ExitCodeRule class.
    /// </summary>
    /// <param name="expectedExitCode">The expected exit code (0 for success, non-zero for failure)</param>
    public ExitCodeRule(int expectedExitCode)
    {
        _expectedExitCode = expectedExitCode;
    }

    /// <summary>
    /// Verifies that the test result contains the expected exit code.
    /// </summary>
    /// <param name="result">The test result to verify</param>
    /// <returns>True if exit code matches expectation, false otherwise</returns>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Check if ExitCode key exists in outputs
        if (!result.Outputs.TryGetValue("ExitCode", out var exitCodeObj))
        {
            _actualExitCode = null;
            return Task.FromResult(false);
        }

        // Parse exit code
        _actualExitCode = exitCodeObj switch
        {
            int exitCode => exitCode,
            string exitCodeStr when int.TryParse(exitCodeStr, out var parsed) => parsed,
            _ => null
        };

        return Task.FromResult(_actualExitCode == _expectedExitCode);
    }

    /// <summary>
    /// Gets a descriptive message explaining the exit code mismatch.
    /// </summary>
    /// <returns>Human-readable failure message</returns>
    public string GetFailureMessage()
    {
        if (_actualExitCode is null)
        {
            return $"Exit code not found in test outputs. Expected: {_expectedExitCode}";
        }

        if (_actualExitCode == _expectedExitCode)
        {
            return "Exit code verification passed";
        }

        return $"Exit code mismatch. Expected: {_expectedExitCode}, Actual: {_actualExitCode}";
    }
}
