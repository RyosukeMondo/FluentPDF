using System.Reflection;

namespace FluentPDF.Rendering.Interop.Verification;

/// <summary>
/// Represents the verification result for a single P/Invoke function.
/// </summary>
public record VerificationResult
{
    /// <summary>
    /// Gets the name of the P/Invoke function being verified.
    /// </summary>
    public required string FunctionName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the function signature matches the PDFium API specification.
    /// </summary>
    public bool SignatureValid { get; init; }

    /// <summary>
    /// Gets a value indicating whether data marshalling works correctly for this function.
    /// Null if marshalling tests were not run for this function.
    /// </summary>
    public bool? MarshallingCorrect { get; init; }

    /// <summary>
    /// Gets the error message if verification failed, or null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the signature details for this function, or null if signature analysis failed.
    /// </summary>
    public SignatureDetails? Signature { get; init; }

    /// <summary>
    /// Gets the marshalling test result, or null if marshalling tests were not run.
    /// </summary>
    public MarshallingTestResult? TestResult { get; init; }

    /// <summary>
    /// Gets the severity level of the verification result.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Info;

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
}

/// <summary>
/// Represents the severity level of a validation result.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message with no action required.
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be reviewed but doesn't block functionality.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that may cause incorrect behavior.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that may cause crashes or data corruption.
    /// </summary>
    Critical
}

/// <summary>
/// Contains detailed information about a P/Invoke function signature.
/// </summary>
public record SignatureDetails
{
    /// <summary>
    /// Gets the return type of the function.
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// Gets the list of parameters for the function.
    /// </summary>
    public required List<ParameterDetails> Parameters { get; init; }

    /// <summary>
    /// Gets the calling convention used by the function.
    /// </summary>
    public required string CallingConvention { get; init; }

    /// <summary>
    /// Gets the entry point name in the native library.
    /// </summary>
    public required string EntryPoint { get; init; }

    /// <summary>
    /// Gets the character set used for string marshalling.
    /// </summary>
    public string? CharSet { get; init; }
}

/// <summary>
/// Contains details about a single function parameter.
/// </summary>
public record ParameterDetails
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of the parameter.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets a value indicating whether this parameter is an out parameter.
    /// </summary>
    public bool IsOut { get; init; }

    /// <summary>
    /// Gets a value indicating whether this parameter is a ref parameter.
    /// </summary>
    public bool IsRef { get; init; }

    /// <summary>
    /// Gets the marshal-as attribute information, if any.
    /// </summary>
    public string? MarshalAs { get; init; }

    /// <summary>
    /// Creates parameter details from a ParameterInfo object.
    /// </summary>
    /// <param name="paramInfo">The reflection parameter information.</param>
    /// <returns>A new ParameterDetails instance.</returns>
    public static ParameterDetails FromParameterInfo(ParameterInfo paramInfo)
    {
        var marshalAs = paramInfo.GetCustomAttribute<System.Runtime.InteropServices.MarshalAsAttribute>();

        return new ParameterDetails
        {
            Name = paramInfo.Name ?? "unknown",
            Type = paramInfo.ParameterType.FullName ?? paramInfo.ParameterType.Name,
            IsOut = paramInfo.IsOut,
            IsRef = paramInfo.ParameterType.IsByRef && !paramInfo.IsOut,
            MarshalAs = marshalAs?.Value.ToString()
        };
    }
}

/// <summary>
/// Represents the result of a marshalling test for a specific function.
/// </summary>
public record MarshallingTestResult
{
    /// <summary>
    /// Gets a value indicating whether the marshalling test succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the expected value from the test case.
    /// </summary>
    public object? ExpectedValue { get; init; }

    /// <summary>
    /// Gets the actual value returned from the P/Invoke call.
    /// </summary>
    public object? ActualValue { get; init; }

    /// <summary>
    /// Gets detailed error information if the test failed.
    /// </summary>
    public string? ErrorDetails { get; init; }

    /// <summary>
    /// Gets the type of data that was tested (e.g., "int", "double", "string", "IntPtr").
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Gets the name of the test case that was executed.
    /// </summary>
    public string? TestCaseName { get; init; }
}

/// <summary>
/// Represents a comprehensive coverage report for P/Invoke marshalling verification.
/// </summary>
public record CoverageReport
{
    /// <summary>
    /// Gets the total number of P/Invoke functions found in the codebase.
    /// </summary>
    public int TotalFunctions { get; init; }

    /// <summary>
    /// Gets the number of functions with verified signatures.
    /// </summary>
    public int VerifiedFunctions { get; init; }

    /// <summary>
    /// Gets the number of functions with marshalling tests.
    /// </summary>
    public int TestedFunctions { get; init; }

    /// <summary>
    /// Gets the number of functions that passed all verification checks.
    /// </summary>
    public int PassedFunctions { get; init; }

    /// <summary>
    /// Gets the number of functions that failed verification.
    /// </summary>
    public int FailedFunctions { get; init; }

    /// <summary>
    /// Gets the list of functions that have not been tested for marshalling.
    /// </summary>
    public required List<string> UntestedFunctions { get; init; }

    /// <summary>
    /// Gets the list of functions that failed verification.
    /// </summary>
    public required List<string> FailedFunctionNames { get; init; }

    /// <summary>
    /// Gets detailed verification results for all functions.
    /// </summary>
    public required List<VerificationResult> Results { get; init; }

    /// <summary>
    /// Gets the timestamp when this report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the overall verification coverage percentage (0-100).
    /// </summary>
    public double CoveragePercentage =>
        TotalFunctions > 0 ? (double)VerifiedFunctions / TotalFunctions * 100 : 0;

    /// <summary>
    /// Gets the marshalling test coverage percentage (0-100).
    /// </summary>
    public double TestCoveragePercentage =>
        TotalFunctions > 0 ? (double)TestedFunctions / TotalFunctions * 100 : 0;

    /// <summary>
    /// Gets a value indicating whether all critical gaps have been addressed.
    /// Critical gaps are functions that failed verification or have no tests.
    /// </summary>
    public bool HasCriticalGaps => FailedFunctions > 0 || UntestedFunctions.Count > 0;
}
