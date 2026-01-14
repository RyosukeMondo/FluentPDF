using FluentPDF.Validation.Models;
using FluentResults;

namespace FluentPDF.Validation.Wrappers;

/// <summary>
/// Interface for VeraPDF PDF/A compliance validation wrapper.
/// </summary>
public interface IVeraPdfWrapper
{
    /// <summary>
    /// Validates a PDF file's PDF/A compliance using VeraPDF.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to validate.</param>
    /// <param name="correlationId">Optional correlation ID for logging.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Result containing VeraPdfResult with compliance status and errors.</returns>
    Task<Result<VeraPdfResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
