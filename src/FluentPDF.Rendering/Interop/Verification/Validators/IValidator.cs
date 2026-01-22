using FluentPDF.Rendering.Interop.Verification.Reports;

namespace FluentPDF.Rendering.Interop.Verification.Validators;

/// <summary>
/// Defines the contract for marshaling validators that test specific high-risk areas.
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Gets the name of this validator.
    /// </summary>
    string ValidatorName { get; }

    /// <summary>
    /// Gets the area of marshaling this validator targets (e.g., "UTF-16 Marshaling", "Bitmap Marshaling").
    /// </summary>
    string TargetArea { get; }

    /// <summary>
    /// Validates the marshaling behavior for the target area.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the validation operation.</param>
    /// <returns>A task that represents the asynchronous validation operation and contains the validation report.</returns>
    Task<ValidationReport> ValidateAsync(CancellationToken cancellationToken = default);
}
