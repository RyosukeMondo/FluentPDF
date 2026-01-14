using FluentPDF.Rendering.Tests.Models;
using FluentResults;
using OpenCvSharp;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Implements visual comparison of images using SSIM (Structural Similarity Index).
/// Uses OpenCvSharp for image processing and perceptual comparison.
/// </summary>
public sealed class VisualComparisonService : IVisualComparisonService
{
    private bool _disposed;

    /// <summary>
    /// Compares two images using SSIM and generates a difference image highlighting changes.
    /// </summary>
    public async Task<Result<ComparisonResult>> CompareImagesAsync(
        string baselinePath,
        string actualPath,
        string differencePath,
        double threshold = 0.95,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return Result.Fail<ComparisonResult>("Service has been disposed");
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(baselinePath))
        {
            return Result.Fail<ComparisonResult>("Baseline path cannot be null or empty")
                .WithError("IMAGE_NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(actualPath))
        {
            return Result.Fail<ComparisonResult>("Actual path cannot be null or empty")
                .WithError("IMAGE_NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(differencePath))
        {
            return Result.Fail<ComparisonResult>("Difference path cannot be null or empty")
                .WithError("IO_ERROR");
        }

        if (threshold < 0.0 || threshold > 1.0)
        {
            return Result.Fail<ComparisonResult>($"Threshold must be between 0.0 and 1.0, got {threshold}")
                .WithError("COMPARISON_FAILED");
        }

        if (!File.Exists(baselinePath))
        {
            return Result.Fail<ComparisonResult>($"Baseline image not found: {baselinePath}")
                .WithError("IMAGE_NOT_FOUND");
        }

        if (!File.Exists(actualPath))
        {
            return Result.Fail<ComparisonResult>($"Actual image not found: {actualPath}")
                .WithError("IMAGE_NOT_FOUND");
        }

        return await Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Load images
                using var baselineImage = Cv2.ImRead(baselinePath, ImreadModes.Color);
                if (baselineImage.Empty())
                {
                    return Result.Fail<ComparisonResult>($"Failed to load baseline image: {baselinePath}")
                        .WithError("IMAGE_LOAD_FAILED");
                }

                using var actualImage = Cv2.ImRead(actualPath, ImreadModes.Color);
                if (actualImage.Empty())
                {
                    return Result.Fail<ComparisonResult>($"Failed to load actual image: {actualPath}")
                        .WithError("IMAGE_LOAD_FAILED");
                }

                // Check dimensions match
                if (baselineImage.Size() != actualImage.Size())
                {
                    return Result.Fail<ComparisonResult>(
                        $"Image size mismatch: baseline {baselineImage.Width}x{baselineImage.Height}, " +
                        $"actual {actualImage.Width}x{actualImage.Height}")
                        .WithError("SIZE_MISMATCH");
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Convert to grayscale for SSIM calculation
                using var baselineGray = new Mat();
                using var actualGray = new Mat();
                Cv2.CvtColor(baselineImage, baselineGray, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(actualImage, actualGray, ColorConversionCodes.BGR2GRAY);

                // Calculate SSIM
                var ssimScore = CalculateSsim(baselineGray, actualGray);

                cancellationToken.ThrowIfCancellationRequested();

                // Generate difference image
                GenerateDifferenceImage(baselineImage, actualImage, differencePath);

                // Create result
                var result = new ComparisonResult
                {
                    SsimScore = ssimScore,
                    Passed = ssimScore >= threshold,
                    Threshold = threshold,
                    BaselinePath = baselinePath,
                    ActualPath = actualPath,
                    DifferencePath = differencePath,
                    ComparedAt = DateTime.UtcNow
                };

                result.Validate();

                return Result.Ok(result);
            }
            catch (OperationCanceledException)
            {
                return Result.Fail<ComparisonResult>("Comparison was cancelled")
                    .WithError("COMPARISON_FAILED");
            }
            catch (Exception ex)
            {
                return Result.Fail<ComparisonResult>($"Comparison failed: {ex.Message}")
                    .WithError("COMPARISON_FAILED")
                    .WithError(ex.ToString());
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Calculates the SSIM (Structural Similarity Index) between two grayscale images.
    /// </summary>
    /// <param name="image1">First grayscale image.</param>
    /// <param name="image2">Second grayscale image.</param>
    /// <returns>SSIM score between 0.0 (completely different) and 1.0 (identical).</returns>
    private static double CalculateSsim(Mat image1, Mat image2)
    {
        // Constants for SSIM calculation (from original paper)
        const double C1 = 6.5025;      // (K1 * L)^2, K1 = 0.01, L = 255
        const double C2 = 58.5225;     // (K2 * L)^2, K2 = 0.03, L = 255

        // Convert to floating point
        using var img1Float = new Mat();
        using var img2Float = new Mat();
        image1.ConvertTo(img1Float, MatType.CV_32F);
        image2.ConvertTo(img2Float, MatType.CV_32F);

        // Calculate means
        using var img1Squared = img1Float.Mul(img1Float);
        using var img2Squared = img2Float.Mul(img2Float);
        using var img1Img2 = img1Float.Mul(img2Float);

        using var mu1 = new Mat();
        using var mu2 = new Mat();
        Cv2.GaussianBlur(img1Float, mu1, new Size(11, 11), 1.5);
        Cv2.GaussianBlur(img2Float, mu2, new Size(11, 11), 1.5);

        using var mu1Squared = mu1.Mul(mu1);
        using var mu2Squared = mu2.Mul(mu2);
        using var mu1Mu2 = mu1.Mul(mu2);

        // Calculate variances and covariance
        using var sigma1Squared = new Mat();
        using var sigma2Squared = new Mat();
        using var sigma12 = new Mat();

        Cv2.GaussianBlur(img1Squared, sigma1Squared, new Size(11, 11), 1.5);
        Cv2.Subtract(sigma1Squared, mu1Squared, sigma1Squared);

        Cv2.GaussianBlur(img2Squared, sigma2Squared, new Size(11, 11), 1.5);
        Cv2.Subtract(sigma2Squared, mu2Squared, sigma2Squared);

        Cv2.GaussianBlur(img1Img2, sigma12, new Size(11, 11), 1.5);
        Cv2.Subtract(sigma12, mu1Mu2, sigma12);

        // Calculate SSIM
        // SSIM = ((2*mu1*mu2 + C1) * (2*sigma12 + C2)) / ((mu1^2 + mu2^2 + C1) * (sigma1^2 + sigma2^2 + C2))
        using var numerator1 = new Mat();
        using var numerator2 = new Mat();
        using var denominator1 = new Mat();
        using var denominator2 = new Mat();

        // Numerator: (2*mu1*mu2 + C1) * (2*sigma12 + C2)
        Cv2.Multiply(mu1Mu2, new Scalar(2), numerator1);
        Cv2.Add(numerator1, new Scalar(C1), numerator1);

        Cv2.Multiply(sigma12, new Scalar(2), numerator2);
        Cv2.Add(numerator2, new Scalar(C2), numerator2);

        using var numerator = numerator1.Mul(numerator2);

        // Denominator: (mu1^2 + mu2^2 + C1) * (sigma1^2 + sigma2^2 + C2)
        Cv2.Add(mu1Squared, mu2Squared, denominator1);
        Cv2.Add(denominator1, new Scalar(C1), denominator1);

        Cv2.Add(sigma1Squared, sigma2Squared, denominator2);
        Cv2.Add(denominator2, new Scalar(C2), denominator2);

        using var denominator = denominator1.Mul(denominator2);

        // Divide and get mean SSIM
        using var ssimMap = new Mat();
        Cv2.Divide(numerator, denominator, ssimMap);

        var meanSsim = Cv2.Mean(ssimMap);
        return meanSsim.Val0;
    }

    /// <summary>
    /// Generates a difference image highlighting visual differences in red.
    /// </summary>
    /// <param name="baseline">Baseline image.</param>
    /// <param name="actual">Actual image.</param>
    /// <param name="outputPath">Path where the difference image should be saved.</param>
    private static void GenerateDifferenceImage(Mat baseline, Mat actual, string outputPath)
    {
        // Calculate absolute difference
        using var diff = new Mat();
        Cv2.Absdiff(baseline, actual, diff);

        // Convert to grayscale to detect changes
        using var diffGray = new Mat();
        Cv2.CvtColor(diff, diffGray, ColorConversionCodes.BGR2GRAY);

        // Threshold to get binary mask of differences
        using var mask = new Mat();
        Cv2.Threshold(diffGray, mask, 30, 255, ThresholdTypes.Binary);

        // Create result image (start with actual image)
        using var result = actual.Clone();

        // Highlight differences in red
        using var redOverlay = new Mat(baseline.Size(), MatType.CV_8UC3, new Scalar(0, 0, 255));
        redOverlay.CopyTo(result, mask);

        // Blend the red overlay with the actual image for better visibility
        using var blended = new Mat();
        Cv2.AddWeighted(actual, 0.7, result, 0.3, 0, blended);

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Save the difference image
        Cv2.ImWrite(outputPath, blended);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}
