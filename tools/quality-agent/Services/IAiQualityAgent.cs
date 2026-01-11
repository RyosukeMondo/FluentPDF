using FluentPDF.QualityAgent.Models;
using FluentResults;

namespace FluentPDF.QualityAgent.Services;

/// <summary>
/// Service interface for AI-powered quality analysis.
/// </summary>
public interface IAiQualityAgent
{
    /// <summary>
    /// Perform comprehensive quality analysis on the provided inputs.
    /// </summary>
    /// <param name="input">Analysis input containing paths to test results, logs, and visual regression data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Quality report with comprehensive analysis.</returns>
    Task<Result<QualityReport>> AnalyzeAsync(AnalysisInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input data for quality analysis.
/// </summary>
public class AnalysisInput
{
    /// <summary>
    /// Path to TRX test results file.
    /// </summary>
    public string? TrxFilePath { get; init; }

    /// <summary>
    /// Path to Serilog JSON log file.
    /// </summary>
    public string? LogFilePath { get; init; }

    /// <summary>
    /// Path to SSIM visual regression results file.
    /// </summary>
    public string? SsimResultsPath { get; init; }

    /// <summary>
    /// Path to validation results file (optional).
    /// </summary>
    public string? ValidationResultsPath { get; init; }

    /// <summary>
    /// Build information.
    /// </summary>
    public required BuildInfo BuildInfo { get; init; }

    /// <summary>
    /// Baseline error rate per hour (optional, will calculate if not provided).
    /// </summary>
    public double? BaselineErrorsPerHour { get; init; }

    /// <summary>
    /// Path to JSON schema file for report validation.
    /// </summary>
    public required string SchemaPath { get; init; }

    /// <summary>
    /// Path to output file for the quality report.
    /// </summary>
    public string? OutputPath { get; init; }
}
