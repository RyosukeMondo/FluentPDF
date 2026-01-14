using FluentPDF.Validation.Models;
using FluentResults;

namespace FluentPDF.Validation.Wrappers;

/// <summary>
/// Interface for QPDF structural validation wrapper.
/// </summary>
public interface IQpdfWrapper
{
    /// <summary>
    /// Validates a PDF file's structural integrity using QPDF.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to validate.</param>
    /// <param name="correlationId">Optional correlation ID for logging.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Result containing QpdfResult with validation status and errors.</returns>
    Task<Result<QpdfResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
