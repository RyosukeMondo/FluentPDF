namespace FluentPDF.Validation.Models;

/// <summary>
/// Defines which validation tools to execute.
/// </summary>
public enum ValidationProfile
{
    /// <summary>
    /// Quick validation using only QPDF for structural checks.
    /// Fastest option, suitable for basic validation.
    /// </summary>
    Quick,

    /// <summary>
    /// Standard validation using QPDF and JHOVE.
    /// Provides structural and format validation.
    /// </summary>
    Standard,

    /// <summary>
    /// Full validation using all tools (QPDF, JHOVE, VeraPDF).
    /// Most comprehensive, includes PDF/A compliance checking.
    /// </summary>
    Full
}
