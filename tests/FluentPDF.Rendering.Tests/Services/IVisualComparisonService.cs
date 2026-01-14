using FluentPDF.Rendering.Tests.Models;
using FluentResults;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Service contract for visual comparison of images using SSIM (Structural Similarity Index).
/// Provides perceptual image comparison with difference visualization for visual regression testing.
/// </summary>
public interface IVisualComparisonService : IDisposable
{
    /// <summary>
    /// Compares two images using SSIM and generates a difference image highlighting changes.
    /// </summary>
    /// <param name="baselinePath">Full path to the baseline (expected) image.</param>
    /// <param name="actualPath">Full path to the actual (current) image.</param>
    /// <param name="differencePath">Full path where the difference image should be saved.</param>
    /// <param name="threshold">SSIM threshold for pass/fail (default: 0.95). Range: 0.0 to 1.0.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result containing ComparisonResult with SSIM score, pass/fail status, and image paths.
    /// Error codes: IMAGE_NOT_FOUND, IMAGE_LOAD_FAILED, SIZE_MISMATCH, COMPARISON_FAILED, IO_ERROR.
    /// </returns>
    Task<Result<ComparisonResult>> CompareImagesAsync(
        string baselinePath,
        string actualPath,
        string differencePath,
        double threshold = 0.95,
        CancellationToken cancellationToken = default);
}
