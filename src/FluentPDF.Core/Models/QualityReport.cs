namespace FluentPDF.Core.Models;

/// <summary>
/// Represents the result of PDF quality validation comparing FluentPDF output against LibreOffice baseline.
/// Contains SSIM (Structural Similarity Index) scores and paths to comparison images.
/// </summary>
public sealed class QualityReport
{
    /// <summary>
    /// Gets the average SSIM score across all pages.
    /// Range: 0.0 (completely different) to 1.0 (identical).
    /// Values above 0.95 typically indicate excellent quality.
    /// </summary>
    public required double AverageSsimScore { get; init; }

    /// <summary>
    /// Gets the lowest SSIM score among all pages.
    /// Identifies the page with the worst quality match.
    /// </summary>
    public required double MinimumSsimScore { get; init; }

    /// <summary>
    /// Gets the page number (1-based) with the lowest SSIM score.
    /// </summary>
    public required int MinimumScorePageNumber { get; init; }

    /// <summary>
    /// Gets the full path to the LibreOffice-generated baseline PDF.
    /// This file is kept for debugging purposes.
    /// </summary>
    public required string LibreOfficePdfPath { get; init; }

    /// <summary>
    /// Gets the full path to the FluentPDF-generated PDF that was validated.
    /// </summary>
    public required string FluentPdfPath { get; init; }

    /// <summary>
    /// Gets the list of page numbers where comparison images were saved.
    /// Only populated for pages with SSIM scores below the threshold.
    /// </summary>
    public required IReadOnlyList<int> ComparisonImagePages { get; init; }

    /// <summary>
    /// Gets the directory path where comparison images are stored.
    /// Null if no comparison images were generated.
    /// </summary>
    public string? ComparisonImagesDirectory { get; init; }

    /// <summary>
    /// Gets the total number of pages that were compared.
    /// </summary>
    public required int TotalPagesCompared { get; init; }

    /// <summary>
    /// Gets the timestamp when the validation was performed.
    /// </summary>
    public required DateTime ValidatedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the quality meets the specified threshold.
    /// </summary>
    public bool IsQualityAcceptable(double threshold = 0.95) => AverageSsimScore >= threshold;
}
