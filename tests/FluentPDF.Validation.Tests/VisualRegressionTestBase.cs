using FluentPDF.Rendering.Tests.Services;
using FluentPDF.Validation.Tests.Exceptions;
using FluentResults;

namespace FluentPDF.Validation.Tests;

/// <summary>
/// Abstract base class for visual regression tests.
/// Provides common functionality for rendering PDFs, comparing against baselines,
/// and managing test results and baseline images.
/// </summary>
public abstract class VisualRegressionTestBase : IDisposable
{
    private readonly IHeadlessRenderingService _renderingService;
    private readonly IVisualComparisonService _comparisonService;
    private readonly IBaselineManager _baselineManager;
    private readonly string _testResultsDirectory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualRegressionTestBase"/> class.
    /// </summary>
    /// <param name="renderingService">Service for rendering PDF pages to images.</param>
    /// <param name="comparisonService">Service for comparing images.</param>
    /// <param name="baselineManager">Manager for baseline storage and retrieval.</param>
    /// <param name="testResultsDirectory">Directory where test results should be stored. Defaults to "tests/TestResults".</param>
    /// <exception cref="ArgumentNullException">Thrown when any service is null.</exception>
    protected VisualRegressionTestBase(
        IHeadlessRenderingService renderingService,
        IVisualComparisonService comparisonService,
        IBaselineManager baselineManager,
        string? testResultsDirectory = null)
    {
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
        _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
        _baselineManager = baselineManager ?? throw new ArgumentNullException(nameof(baselineManager));
        _testResultsDirectory = testResultsDirectory ?? Path.Combine("tests", "TestResults");

        // Ensure test results directory exists
        Directory.CreateDirectory(_testResultsDirectory);
    }

    /// <summary>
    /// Asserts that a rendered PDF page matches the baseline image within the specified threshold.
    /// If no baseline exists, creates one automatically on first run.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file to test.</param>
    /// <param name="category">Test category (e.g., "CoreRendering", "Zoom").</param>
    /// <param name="testName">Name of the test case.</param>
    /// <param name="pageNumber">1-based page number to test.</param>
    /// <param name="threshold">SSIM threshold for passing (0.0 to 1.0). Default: 0.95.</param>
    /// <param name="dpi">Resolution for rendering in dots per inch. Default: 96.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="VisualRegressionException">Thrown when the visual comparison fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when rendering or comparison fails.</exception>
    protected async Task AssertVisualMatchAsync(
        string pdfPath,
        string category,
        string testName,
        int pageNumber = 1,
        double threshold = 0.95,
        int dpi = 96,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category cannot be null or empty", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("Test name cannot be null or empty", nameof(testName));
        }

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be 1 or greater");
        }

        if (threshold < 0.0 || threshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0.0 and 1.0");
        }

        // Convert to 0-based for internal use
        var pageIndex = pageNumber - 1;

        // Determine baseline path
        var baselinePath = _baselineManager.GetBaselinePath(category, testName, pageIndex);
        var baselineExists = _baselineManager.BaselineExists(category, testName, pageIndex);

        // Generate timestamp for test results
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var testRunDir = Path.Combine(_testResultsDirectory, category, testName, timestamp);
        Directory.CreateDirectory(testRunDir);

        // Render the actual PDF page
        var actualPath = Path.Combine(testRunDir, $"actual_page{pageNumber}.png");
        var renderResult = await _renderingService.RenderPageToFileAsync(
            pdfPath,
            pageNumber,
            actualPath,
            dpi,
            cancellationToken);

        if (renderResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"Failed to render PDF page {pageNumber}: {string.Join(", ", renderResult.Errors.Select(e => e.Message))}");
        }

        // If no baseline exists, create it from the actual render (first-run behavior)
        if (!baselineExists)
        {
            var createResult = await _baselineManager.CreateBaselineAsync(
                actualPath,
                category,
                testName,
                pageIndex,
                cancellationToken);

            if (createResult.IsFailed)
            {
                throw new InvalidOperationException(
                    $"Failed to create baseline: {string.Join(", ", createResult.Errors.Select(e => e.Message))}");
            }

            // First run - baseline created, test passes automatically
            return;
        }

        // Compare actual with baseline
        var differencePath = Path.Combine(testRunDir, $"difference_page{pageNumber}.png");
        var comparisonResult = await _comparisonService.CompareImagesAsync(
            baselinePath,
            actualPath,
            differencePath,
            threshold,
            cancellationToken);

        if (comparisonResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"Failed to compare images: {string.Join(", ", comparisonResult.Errors.Select(e => e.Message))}");
        }

        var comparison = comparisonResult.Value;

        // If comparison failed, throw descriptive exception
        if (!comparison.Passed)
        {
            throw new VisualRegressionException(
                category,
                testName,
                pageNumber,
                comparison.SsimScore,
                comparison.Threshold,
                baselinePath,
                actualPath,
                differencePath);
        }

        // Test passed - optionally clean up test results directory
        // We keep them for now for debugging purposes
    }

    /// <summary>
    /// Disposes resources used by the test base.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources used by the test base.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _renderingService?.Dispose();
            _comparisonService?.Dispose();
            _baselineManager?.Dispose();
        }

        _disposed = true;
    }
}
