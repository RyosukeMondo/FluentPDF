namespace FluentPDF.Validation.Models;

/// <summary>
/// Overall validation status for a PDF file.
/// </summary>
public enum ValidationStatus
{
    /// <summary>
    /// All validation checks passed successfully.
    /// </summary>
    Pass,

    /// <summary>
    /// Validation completed with warnings but no critical failures.
    /// </summary>
    Warn,

    /// <summary>
    /// One or more validation checks failed.
    /// </summary>
    Fail
}
