using FluentPDF.Rendering.Interop.Verification.Reports;

namespace FluentPDF.Rendering.Interop.Verification.Regression;

/// <summary>
/// Defines the contract for testing documented workarounds to detect if they are still needed,
/// can be removed (underlying bug fixed), or are broken.
/// </summary>
public interface IWorkaroundTest
{
    /// <summary>
    /// Gets the name of the workaround being tested (e.g., "Float Dimension Workaround", "Threading Workaround").
    /// </summary>
    string WorkaroundName { get; }

    /// <summary>
    /// Gets the documentation reference for this workaround, including file path and line numbers.
    /// Format: "FilePath.cs:StartLine-EndLine" (e.g., "PdfiumInterop.cs:177-183").
    /// </summary>
    string DocumentationReference { get; }

    /// <summary>
    /// Tests the workaround to determine its current status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the test operation.</param>
    /// <returns>A task that represents the asynchronous test operation and contains the test result.</returns>
    Task<WorkaroundTestResult> TestWorkaroundAsync(CancellationToken cancellationToken = default);
}
