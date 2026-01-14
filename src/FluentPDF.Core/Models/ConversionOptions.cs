namespace FluentPDF.Core.Models;

/// <summary>
/// Configuration options for DOCX to PDF conversion operations.
/// Provides control over conversion behavior, timeouts, and quality validation.
/// </summary>
public sealed class ConversionOptions
{
    /// <summary>
    /// Gets or initializes the timeout duration for the entire conversion operation.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or initializes a value indicating whether to enable quality validation
    /// by comparing output against LibreOffice baseline using SSIM metrics.
    /// Default is false. Requires LibreOffice to be installed.
    /// </summary>
    public bool EnableQualityValidation { get; init; } = false;

    /// <summary>
    /// Gets or initializes the directory path for temporary files during conversion.
    /// If null, uses the system temp directory.
    /// </summary>
    public string? TempDirectory { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether to preserve temporary files
    /// for debugging purposes. Default is false (temp files are cleaned up).
    /// </summary>
    public bool PreserveTempFiles { get; init; } = false;
}
