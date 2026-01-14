using FluentPDF.Validation.Models;
using FluentResults;

namespace FluentPDF.Validation.Services;

/// <summary>
/// Service for orchestrating PDF validation across multiple tools.
/// </summary>
public interface IPdfValidationService
{
    /// <summary>
    /// Validates a PDF file using the specified validation profile.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to validate.</param>
    /// <param name="profile">Validation profile determining which tools to execute.</param>
    /// <param name="correlationId">Optional correlation ID for logging.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Result containing ValidationReport with aggregated results from all executed tools.</returns>
    Task<Result<ValidationReport>> ValidateAsync(
        string filePath,
        ValidationProfile profile,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that all required validation tools are installed and accessible.
    /// </summary>
    /// <param name="profile">Validation profile to check tools for.</param>
    /// <returns>Result indicating success if all required tools are available, or failure with details.</returns>
    Task<Result> VerifyToolsInstalledAsync(ValidationProfile profile);
}
