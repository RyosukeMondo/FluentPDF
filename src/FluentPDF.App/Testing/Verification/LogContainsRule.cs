namespace FluentPDF.App.Testing.Verification;

/// <summary>
/// Verification rule that checks if test logs contain expected content.
/// Used to verify tests logged expected information or error messages.
/// </summary>
public sealed class LogContainsRule : IVerificationRule
{
    private readonly List<string> _expectedPatterns;
    private readonly List<string> _missingPatterns = new();
    private readonly bool _caseSensitive;

    /// <summary>
    /// Gets the name of this verification rule.
    /// </summary>
    public string Name => "LogContains";

    /// <summary>
    /// Initializes a new instance of the LogContainsRule class.
    /// </summary>
    /// <param name="expectedPatterns">List of strings or patterns that must appear in logs</param>
    /// <param name="caseSensitive">Whether pattern matching should be case-sensitive (default: false)</param>
    public LogContainsRule(string[] expectedPatterns, bool caseSensitive = false)
    {
        _expectedPatterns = expectedPatterns?.ToList() ?? throw new ArgumentNullException(nameof(expectedPatterns));
        _caseSensitive = caseSensitive;

        if (_expectedPatterns.Count == 0)
        {
            throw new ArgumentException("At least one expected pattern must be specified", nameof(expectedPatterns));
        }
    }

    /// <summary>
    /// Convenience constructor for single pattern verification.
    /// </summary>
    /// <param name="expectedPattern">The string or pattern that must appear in logs</param>
    /// <param name="caseSensitive">Whether pattern matching should be case-sensitive (default: false)</param>
    public LogContainsRule(string expectedPattern, bool caseSensitive = false)
        : this(new[] { expectedPattern }, caseSensitive)
    {
    }

    /// <summary>
    /// Verifies that all expected patterns appear in the test log entries.
    /// </summary>
    /// <param name="result">The test result to verify</param>
    /// <returns>True if all expected patterns are found in logs, false otherwise</returns>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _missingPatterns.Clear();

        // Combine all log entries into searchable text
        var allLogs = string.Join("\n", result.LogEntries);

        // Check each expected pattern
        foreach (var pattern in _expectedPatterns)
        {
            var comparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var found = allLogs.Contains(pattern, comparison);

            if (!found)
            {
                _missingPatterns.Add(pattern);
            }
        }

        return Task.FromResult(_missingPatterns.Count == 0);
    }

    /// <summary>
    /// Gets a descriptive message explaining which patterns are missing from logs.
    /// </summary>
    /// <returns>Human-readable failure message</returns>
    public string GetFailureMessage()
    {
        if (_missingPatterns.Count == 0)
        {
            return "Log content verification passed";
        }

        return $"Missing expected patterns in logs: {string.Join(", ", _missingPatterns.Select(p => $"\"{p}\""))}";
    }
}
