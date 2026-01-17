using FluentResults;

namespace FluentPDF.Verification.Core;

/// <summary>
/// Defines the contract for library-specific verifiers.
/// Supports async-first design for I/O-bound operations like DLL analysis.
/// </summary>
/// <typeparam name="TResult">The type of verification result produced by this verifier.</typeparam>
public interface ILibraryVerifier<TResult>
{
    /// <summary>
    /// Gets the name of the library being verified (e.g., "PDFium", "SkiaSharp").
    /// </summary>
    string LibraryName { get; }

    /// <summary>
    /// Executes all verification checks asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the verification outcome or error details.</returns>
    Task<Result<TResult>> VerifyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the verifier's configuration before execution.
    /// </summary>
    /// <returns>A result indicating whether the configuration is valid.</returns>
    Result ValidateConfiguration();
}

/// <summary>
/// Analyzes native DLL exports and function signatures.
/// Abstracts PE file parsing and export table analysis.
/// </summary>
public interface IDllAnalyzer
{
    /// <summary>
    /// Loads and analyzes a native DLL file.
    /// </summary>
    /// <param name="dllPath">Absolute path to the DLL file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing DLL information or error details.</returns>
    Task<Result<DllInfo>> AnalyzeAsync(string dllPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the signature of a specific exported function.
    /// </summary>
    /// <param name="functionName">Name of the exported function.</param>
    /// <returns>A result containing the function signature or an error if not found.</returns>
    Result<FunctionSignature> GetFunctionSignature(string functionName);

    /// <summary>
    /// Gets all exported function names from the analyzed DLL.
    /// </summary>
    /// <returns>A result containing the list of function names.</returns>
    Result<IReadOnlyList<string>> GetExportedFunctions();
}

/// <summary>
/// Generates verification reports in various formats.
/// Supports console, JSON, and HTML output for different consumption scenarios.
/// </summary>
public interface IVerificationReporter
{
    /// <summary>
    /// Generates a human-readable console report.
    /// </summary>
    /// <param name="summary">The verification summary to report.</param>
    /// <returns>A result containing the formatted console output.</returns>
    Result<string> GenerateConsoleReport(VerificationSummary summary);

    /// <summary>
    /// Generates a machine-readable JSON report for CI/CD consumption.
    /// </summary>
    /// <param name="summary">The verification summary to report.</param>
    /// <returns>A result containing the JSON output.</returns>
    Result<string> GenerateJsonReport(VerificationSummary summary);

    /// <summary>
    /// Generates an HTML report with visual formatting.
    /// </summary>
    /// <param name="summary">The verification summary to report.</param>
    /// <returns>A result containing the HTML output.</returns>
    Result<string> GenerateHtmlReport(VerificationSummary summary);

    /// <summary>
    /// Writes a report to a file asynchronously.
    /// </summary>
    /// <param name="content">The report content to write.</param>
    /// <param name="outputPath">Path where the report should be written.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> WriteReportAsync(string content, string outputPath, CancellationToken cancellationToken = default);
}
