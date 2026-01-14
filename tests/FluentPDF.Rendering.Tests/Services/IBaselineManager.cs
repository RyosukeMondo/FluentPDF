using FluentResults;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Service contract for managing visual test baseline images.
/// Handles baseline storage, retrieval, and updates with organized directory structure.
/// </summary>
public interface IBaselineManager : IDisposable
{
    /// <summary>
    /// Gets the full path to a baseline image for a specific test.
    /// </summary>
    /// <param name="category">Test category (e.g., "CoreRendering", "Zoom").</param>
    /// <param name="testName">Name of the test case.</param>
    /// <param name="pageNumber">Page number being tested (0-based).</param>
    /// <returns>Full path to the baseline image file (may not exist yet).</returns>
    string GetBaselinePath(string category, string testName, int pageNumber);

    /// <summary>
    /// Checks if a baseline image exists for the specified test.
    /// </summary>
    /// <param name="category">Test category (e.g., "CoreRendering", "Zoom").</param>
    /// <param name="testName">Name of the test case.</param>
    /// <param name="pageNumber">Page number being tested (0-based).</param>
    /// <returns>True if baseline exists, false otherwise.</returns>
    bool BaselineExists(string category, string testName, int pageNumber);

    /// <summary>
    /// Creates a new baseline image from a source image file.
    /// </summary>
    /// <param name="sourcePath">Full path to the source image to use as baseline.</param>
    /// <param name="category">Test category (e.g., "CoreRendering", "Zoom").</param>
    /// <param name="testName">Name of the test case.</param>
    /// <param name="pageNumber">Page number being tested (0-based).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result containing the path to the created baseline image.
    /// Error codes: INVALID_SOURCE_PATH, SOURCE_NOT_FOUND, IO_ERROR, BASELINE_CREATE_FAILED.
    /// </returns>
    Task<Result<string>> CreateBaselineAsync(
        string sourcePath,
        string category,
        string testName,
        int pageNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing baseline image with a new version.
    /// </summary>
    /// <param name="sourcePath">Full path to the source image to use as new baseline.</param>
    /// <param name="category">Test category (e.g., "CoreRendering", "Zoom").</param>
    /// <param name="testName">Name of the test case.</param>
    /// <param name="pageNumber">Page number being tested (0-based).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A Result containing the path to the updated baseline image.
    /// Error codes: INVALID_SOURCE_PATH, SOURCE_NOT_FOUND, BASELINE_NOT_FOUND, IO_ERROR, BASELINE_UPDATE_FAILED.
    /// </returns>
    Task<Result<string>> UpdateBaselineAsync(
        string sourcePath,
        string category,
        string testName,
        int pageNumber,
        CancellationToken cancellationToken = default);
}
