namespace FluentPDF.Validation.Tests.Exceptions;

/// <summary>
/// Exception thrown when a visual regression test fails.
/// Provides detailed information about the failure including image paths and SSIM scores.
/// </summary>
public sealed class VisualRegressionException : Exception
{
    /// <summary>
    /// Gets the test category that failed.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the test name that failed.
    /// </summary>
    public string TestName { get; }

    /// <summary>
    /// Gets the page number that failed.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the SSIM score achieved.
    /// </summary>
    public double SsimScore { get; }

    /// <summary>
    /// Gets the threshold that was required to pass.
    /// </summary>
    public double Threshold { get; }

    /// <summary>
    /// Gets the path to the baseline image.
    /// </summary>
    public string BaselinePath { get; }

    /// <summary>
    /// Gets the path to the actual rendered image.
    /// </summary>
    public string ActualPath { get; }

    /// <summary>
    /// Gets the path to the difference image, if generated.
    /// </summary>
    public string? DifferencePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualRegressionException"/> class.
    /// </summary>
    /// <param name="category">Test category.</param>
    /// <param name="testName">Test name.</param>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="ssimScore">Actual SSIM score.</param>
    /// <param name="threshold">Required threshold.</param>
    /// <param name="baselinePath">Path to baseline image.</param>
    /// <param name="actualPath">Path to actual image.</param>
    /// <param name="differencePath">Path to difference image.</param>
    public VisualRegressionException(
        string category,
        string testName,
        int pageNumber,
        double ssimScore,
        double threshold,
        string baselinePath,
        string actualPath,
        string? differencePath)
        : base(BuildMessage(category, testName, pageNumber, ssimScore, threshold, baselinePath, actualPath, differencePath))
    {
        Category = category;
        TestName = testName;
        PageNumber = pageNumber;
        SsimScore = ssimScore;
        Threshold = threshold;
        BaselinePath = baselinePath;
        ActualPath = actualPath;
        DifferencePath = differencePath;
    }

    private static string BuildMessage(
        string category,
        string testName,
        int pageNumber,
        double ssimScore,
        double threshold,
        string baselinePath,
        string actualPath,
        string? differencePath)
    {
        var message = $@"Visual regression test failed for {category}/{testName} page {pageNumber}

SSIM Score: {ssimScore:F4} (threshold: {threshold:F4})
Difference: {(threshold - ssimScore):F4}

Image Paths:
  Baseline:   {baselinePath}
  Actual:     {actualPath}";

        if (!string.IsNullOrEmpty(differencePath))
        {
            message += $"\n  Difference: {differencePath}";
        }

        message += @"

Review the difference image to determine if this is a legitimate change.
If the change is expected, update the baseline using BaselineManager.UpdateBaselineAsync().";

        return message;
    }
}
