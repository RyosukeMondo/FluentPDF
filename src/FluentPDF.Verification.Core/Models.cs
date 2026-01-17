namespace FluentPDF.Verification.Core;

/// <summary>
/// Contains information about an analyzed DLL.
/// </summary>
public record DllInfo
{
    /// <summary>
    /// Path to the analyzed DLL file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// DLL file version, if available.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Target architecture (x64, x86, ARM64, etc.).
    /// </summary>
    public required string Architecture { get; init; }

    /// <summary>
    /// Total number of exported functions.
    /// </summary>
    public required int ExportCount { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Additional metadata about the DLL.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a function signature extracted from a DLL.
/// </summary>
public record FunctionSignature
{
    /// <summary>
    /// Name of the function.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Return type of the function.
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// Ordered list of parameter types.
    /// </summary>
    public required IReadOnlyList<ParameterInfo> Parameters { get; init; }

    /// <summary>
    /// Calling convention (Cdecl, StdCall, etc.).
    /// </summary>
    public string? CallingConvention { get; init; }

    /// <summary>
    /// Full signature as a string (e.g., "int FunctionName(char*, int)").
    /// </summary>
    public string FullSignature => $"{ReturnType} {Name}({string.Join(", ", Parameters.Select(p => $"{p.Type} {p.Name}"))})";
}

/// <summary>
/// Represents a function parameter.
/// </summary>
public record ParameterInfo
{
    /// <summary>
    /// Parameter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Parameter type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Whether this is an out/ref parameter.
    /// </summary>
    public bool IsOut { get; init; }

    /// <summary>
    /// Whether this parameter is marked as [In].
    /// </summary>
    public bool IsIn { get; init; }

    /// <summary>
    /// Marshalling attributes, if any.
    /// </summary>
    public string? MarshalAs { get; init; }
}

/// <summary>
/// Summary of all verification results.
/// </summary>
public record VerificationSummary
{
    /// <summary>
    /// Total number of verification tests executed.
    /// </summary>
    public required int TotalTests { get; init; }

    /// <summary>
    /// Number of tests that passed.
    /// </summary>
    public required int PassedTests { get; init; }

    /// <summary>
    /// Number of tests that failed.
    /// </summary>
    public required int FailedTests { get; init; }

    /// <summary>
    /// Individual verification results.
    /// </summary>
    public required IReadOnlyList<VerificationResult> Results { get; init; }

    /// <summary>
    /// Total time taken to execute all verifications.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Library version being verified (e.g., PDFium version).
    /// </summary>
    public string? LibraryVersion { get; init; }

    /// <summary>
    /// Overall success status.
    /// </summary>
    public bool Success => FailedTests == 0;
}

/// <summary>
/// Result of a single verification test.
/// </summary>
public record VerificationResult
{
    /// <summary>
    /// Name of the test that was executed.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Whether the test passed.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Suggested fix for the failure, if applicable.
    /// </summary>
    public string? SuggestedFix { get; init; }

    /// <summary>
    /// Additional metadata about the test result.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Time taken to execute this test.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}

/// <summary>
/// Represents a function signature match result.
/// </summary>
public record SignatureMatch
{
    /// <summary>
    /// Name of the function being compared.
    /// </summary>
    public required string FunctionName { get; init; }

    /// <summary>
    /// Whether the signatures match.
    /// </summary>
    public required bool Matches { get; init; }

    /// <summary>
    /// The P/Invoke declaration signature.
    /// </summary>
    public required string DeclaredSignature { get; init; }

    /// <summary>
    /// The actual DLL export signature.
    /// </summary>
    public required string ActualSignature { get; init; }

    /// <summary>
    /// Suggested fix if signatures don't match.
    /// </summary>
    public string? SuggestedFix { get; init; }

    /// <summary>
    /// Detailed differences between signatures.
    /// </summary>
    public IReadOnlyList<string> Differences { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Options for configuring verification execution.
/// </summary>
public record VerificationOptions
{
    /// <summary>
    /// Path to the native DLL to verify.
    /// </summary>
    public required string DllPath { get; init; }

    /// <summary>
    /// Directory containing test files (e.g., test PDFs).
    /// </summary>
    public string? TestFileDirectory { get; init; }

    /// <summary>
    /// Whether to execute verifications in parallel.
    /// </summary>
    public bool ParallelExecution { get; init; } = true;

    /// <summary>
    /// Output format for the verification report.
    /// </summary>
    public ReportFormat OutputFormat { get; init; } = ReportFormat.Console;

    /// <summary>
    /// Path where the report should be written. If null, output to stdout.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Timeout for individual verification tests.
    /// </summary>
    public TimeSpan TestTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Supported report output formats.
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// Human-readable console output with color coding.
    /// </summary>
    Console,

    /// <summary>
    /// Machine-readable JSON format for CI/CD.
    /// </summary>
    Json,

    /// <summary>
    /// HTML format with visual formatting.
    /// </summary>
    Html
}
