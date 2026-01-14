namespace FluentPDF.Validation.Models;

/// <summary>
/// PDF/A flavour (conformance level) as defined by ISO standards.
/// </summary>
public enum PdfFlavour
{
    /// <summary>
    /// Not a PDF/A compliant document.
    /// </summary>
    None,

    /// <summary>
    /// PDF/A-1a (ISO 19005-1, Level A - Accessible).
    /// </summary>
    PdfA1a,

    /// <summary>
    /// PDF/A-1b (ISO 19005-1, Level B - Basic).
    /// </summary>
    PdfA1b,

    /// <summary>
    /// PDF/A-2a (ISO 19005-2, Level A - Accessible).
    /// </summary>
    PdfA2a,

    /// <summary>
    /// PDF/A-2b (ISO 19005-2, Level B - Basic).
    /// </summary>
    PdfA2b,

    /// <summary>
    /// PDF/A-2u (ISO 19005-2, Level U - Unicode).
    /// </summary>
    PdfA2u,

    /// <summary>
    /// PDF/A-3a (ISO 19005-3, Level A - Accessible).
    /// </summary>
    PdfA3a,

    /// <summary>
    /// PDF/A-3b (ISO 19005-3, Level B - Basic).
    /// </summary>
    PdfA3b,

    /// <summary>
    /// PDF/A-3u (ISO 19005-3, Level U - Unicode).
    /// </summary>
    PdfA3u
}
