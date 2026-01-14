namespace FluentPDF.Core.Models;

/// <summary>
/// Represents the result of a DOCX to PDF conversion operation.
/// Contains output path, conversion metrics, and optional quality validation results.
/// </summary>
public sealed class ConversionResult
{
    /// <summary>
    /// Gets the full path to the generated PDF file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets the full path to the source DOCX file.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the total time taken for the conversion operation.
    /// </summary>
    public required TimeSpan ConversionTime { get; init; }

    /// <summary>
    /// Gets the size of the output PDF file in bytes.
    /// </summary>
    public required long OutputSizeBytes { get; init; }

    /// <summary>
    /// Gets the size of the source DOCX file in bytes.
    /// </summary>
    public required long SourceSizeBytes { get; init; }

    /// <summary>
    /// Gets the estimated page count of the generated PDF.
    /// May be null if page counting failed.
    /// </summary>
    public int? PageCount { get; init; }

    /// <summary>
    /// Gets the quality validation score (SSIM) if quality validation was enabled.
    /// Range: 0.0 (completely different) to 1.0 (identical).
    /// Null if quality validation was not performed.
    /// </summary>
    public double? QualityScore { get; init; }

    /// <summary>
    /// Gets the timestamp when the conversion completed.
    /// </summary>
    public required DateTime CompletedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether quality validation was performed.
    /// </summary>
    public bool QualityValidationPerformed => QualityScore.HasValue;
}
