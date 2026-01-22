using FluentPDF.Rendering.Interop.Verification;

namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Represents the result of a single validation test within a validator.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Gets the name of the test that was performed.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the test passed.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Gets the severity level of this result.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Info;

    /// <summary>
    /// Gets a detailed message describing the test result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the error details if the test failed.
    /// </summary>
    public string? ErrorDetails { get; init; }

    /// <summary>
    /// Gets a suggested fix for the issue, if applicable.
    /// </summary>
    public string? SuggestedFix { get; init; }

    /// <summary>
    /// Gets the URL to documentation for this issue or validation pattern.
    /// </summary>
    public string? DocumentationUrl { get; init; }

    /// <summary>
    /// Gets additional context information as key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Context { get; init; }

    /// <summary>
    /// Gets the expected value for the test, if applicable.
    /// </summary>
    public object? ExpectedValue { get; init; }

    /// <summary>
    /// Gets the actual value from the test, if applicable.
    /// </summary>
    public object? ActualValue { get; init; }
}
