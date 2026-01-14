using FluentPDF.Validation.Models;
using FluentResults;

namespace FluentPDF.Validation.Wrappers;

/// <summary>
/// Interface for JHOVE format validation and characterization wrapper.
/// </summary>
public interface IJhoveWrapper
{
    /// <summary>
    /// Validates and characterizes a PDF file using JHOVE.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to validate.</param>
    /// <param name="correlationId">Optional correlation ID for logging.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Result containing JhoveResult with format validation status and metadata.</returns>
    Task<Result<JhoveResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
