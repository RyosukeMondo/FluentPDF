namespace FluentPDF.Core.ErrorHandling;

/// <summary>
/// Categorizes PDF-related errors for classification and routing.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Validation errors from input data or user input (e.g., invalid file path, malformed PDF).
    /// </summary>
    Validation,

    /// <summary>
    /// System-level errors such as out of memory, thread pool exhaustion, or runtime failures.
    /// </summary>
    System,

    /// <summary>
    /// Security-related errors including permission denied, encryption failures, or unauthorized access.
    /// </summary>
    Security,

    /// <summary>
    /// Input/Output errors such as file not found, disk full, or network failures.
    /// </summary>
    IO,

    /// <summary>
    /// PDF rendering errors from PDFium or graphics subsystem failures.
    /// </summary>
    Rendering,

    /// <summary>
    /// Conversion errors when transforming PDF documents or manipulating content.
    /// </summary>
    Conversion
}
