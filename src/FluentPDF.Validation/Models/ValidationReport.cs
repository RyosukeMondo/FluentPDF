namespace FluentPDF.Validation.Models;

/// <summary>
/// Comprehensive validation report aggregating results from all validation tools.
/// </summary>
public sealed class ValidationReport
{
    /// <summary>
    /// Gets the overall validation status (Pass/Warn/Fail).
    /// Derived from individual tool results.
    /// </summary>
    public required ValidationStatus OverallStatus { get; init; }

    /// <summary>
    /// Gets the full path to the validated PDF file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the timestamp when validation was performed.
    /// </summary>
    public required DateTime ValidationDate { get; init; }

    /// <summary>
    /// Gets the validation profile used (Quick/Standard/Full).
    /// </summary>
    public required ValidationProfile Profile { get; init; }

    /// <summary>
    /// Gets the QPDF structural validation result.
    /// Null if QPDF validation was not performed.
    /// </summary>
    public QpdfResult? QpdfResult { get; init; }

    /// <summary>
    /// Gets the JHOVE format validation result.
    /// Null if JHOVE validation was not performed.
    /// </summary>
    public JhoveResult? JhoveResult { get; init; }

    /// <summary>
    /// Gets the VeraPDF PDF/A compliance validation result.
    /// Null if VeraPDF validation was not performed.
    /// </summary>
    public VeraPdfResult? VeraPdfResult { get; init; }

    /// <summary>
    /// Gets the total validation duration.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets a summary message describing the validation outcome.
    /// </summary>
    public string Summary => OverallStatus switch
    {
        ValidationStatus.Pass => "All validation checks passed.",
        ValidationStatus.Warn => "Validation completed with warnings.",
        ValidationStatus.Fail => "Validation failed. See individual tool results for details.",
        _ => "Unknown validation status."
    };

    /// <summary>
    /// Gets a value indicating whether all performed validations passed.
    /// </summary>
    public bool IsValid => OverallStatus == ValidationStatus.Pass;
}
