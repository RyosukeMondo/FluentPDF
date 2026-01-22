namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Defines the contract for exporting validation reports to various formats.
/// </summary>
/// <typeparam name="TReport">The type of report to export.</typeparam>
public interface IReportExporter<in TReport>
{
    /// <summary>
    /// Exports the specified report to a file at the given output path.
    /// </summary>
    /// <param name="report">The report to export.</param>
    /// <param name="outputPath">The file path where the report should be saved.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous export operation.</returns>
    Task ExportAsync(TReport report, string outputPath, CancellationToken cancellationToken = default);
}
