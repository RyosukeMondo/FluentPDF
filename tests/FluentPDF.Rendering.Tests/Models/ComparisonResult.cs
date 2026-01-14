namespace FluentPDF.Rendering.Tests.Models;

/// <summary>
/// Represents the result of a visual comparison between two images.
/// Contains SSIM score, pass/fail status, and paths to all comparison artifacts.
/// </summary>
public sealed class ComparisonResult
{
    /// <summary>
    /// Gets the Structural Similarity Index (SSIM) score.
    /// Range: 0.0 (completely different) to 1.0 (identical).
    /// </summary>
    public required double SsimScore { get; init; }

    /// <summary>
    /// Gets whether the comparison passed based on the threshold.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Gets the threshold value used for comparison.
    /// </summary>
    public required double Threshold { get; init; }

    /// <summary>
    /// Gets the path to the baseline (expected) image.
    /// </summary>
    public required string BaselinePath { get; init; }

    /// <summary>
    /// Gets the path to the actual (current) image.
    /// </summary>
    public required string ActualPath { get; init; }

    /// <summary>
    /// Gets the path to the difference image showing visual differences.
    /// Null if no difference image was generated.
    /// </summary>
    public string? DifferencePath { get; init; }

    /// <summary>
    /// Gets the timestamp when the comparison was performed.
    /// </summary>
    public required DateTime ComparedAt { get; init; }

    /// <summary>
    /// Validates the comparison result values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when values are invalid.</exception>
    public void Validate()
    {
        if (SsimScore < 0.0 || SsimScore > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SsimScore),
                SsimScore,
                "SSIM score must be between 0.0 and 1.0");
        }

        if (Threshold < 0.0 || Threshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Threshold),
                Threshold,
                "Threshold must be between 0.0 and 1.0");
        }

        if (string.IsNullOrWhiteSpace(BaselinePath))
        {
            throw new ArgumentException(
                "Baseline path cannot be null or empty",
                nameof(BaselinePath));
        }

        if (string.IsNullOrWhiteSpace(ActualPath))
        {
            throw new ArgumentException(
                "Actual path cannot be null or empty",
                nameof(ActualPath));
        }

        if (Passed && SsimScore < Threshold)
        {
            throw new InvalidOperationException(
                $"Result marked as passed but SSIM score {SsimScore} is below threshold {Threshold}");
        }

        if (!Passed && SsimScore >= Threshold)
        {
            throw new InvalidOperationException(
                $"Result marked as failed but SSIM score {SsimScore} meets or exceeds threshold {Threshold}");
        }
    }
}
