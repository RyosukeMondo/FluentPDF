using FluentResults;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Manages visual test baseline images with organized category/test/page directory structure.
/// Baselines are stored in tests/Baselines/{category}/{testName}/page_{pageNumber}.png
/// </summary>
public sealed class BaselineManager : IBaselineManager
{
    private readonly string _baselineRootPath;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of BaselineManager.
    /// </summary>
    /// <param name="baselineRootPath">Root directory for baseline storage (e.g., "tests/Baselines").</param>
    /// <exception cref="ArgumentException">Thrown when baselineRootPath is null or empty.</exception>
    public BaselineManager(string baselineRootPath)
    {
        if (string.IsNullOrWhiteSpace(baselineRootPath))
        {
            throw new ArgumentException(
                "Baseline root path cannot be null or empty",
                nameof(baselineRootPath));
        }

        _baselineRootPath = Path.GetFullPath(baselineRootPath);
    }

    /// <inheritdoc/>
    public string GetBaselinePath(string category, string testName, int pageNumber)
    {
        ValidateParameters(category, testName, pageNumber);
        return BuildBaselinePath(category, testName, pageNumber);
    }

    /// <inheritdoc/>
    public bool BaselineExists(string category, string testName, int pageNumber)
    {
        ValidateParameters(category, testName, pageNumber);
        var path = BuildBaselinePath(category, testName, pageNumber);
        return File.Exists(path);
    }

    /// <inheritdoc/>
    public async Task<Result<string>> CreateBaselineAsync(
        string sourcePath,
        string category,
        string testName,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateParameters(category, testName, pageNumber);

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return Result.Fail<string>("INVALID_SOURCE_PATH: Source path cannot be null or empty");
            }

            if (!File.Exists(sourcePath))
            {
                return Result.Fail<string>($"SOURCE_NOT_FOUND: Source file not found at '{sourcePath}'");
            }

            var baselinePath = BuildBaselinePath(category, testName, pageNumber);

            if (File.Exists(baselinePath))
            {
                return Result.Fail<string>(
                    $"BASELINE_ALREADY_EXISTS: Baseline already exists at '{baselinePath}'. Use UpdateBaselineAsync to replace it.");
            }

            // Create directory structure if it doesn't exist
            var directory = Path.GetDirectoryName(baselinePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Copy source to baseline location
            await CopyFileAsync(sourcePath, baselinePath, cancellationToken);

            return Result.Ok(baselinePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return Result.Fail<string>($"IO_ERROR: Failed to create baseline: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail<string>($"BASELINE_CREATE_FAILED: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> UpdateBaselineAsync(
        string sourcePath,
        string category,
        string testName,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateParameters(category, testName, pageNumber);

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return Result.Fail<string>("INVALID_SOURCE_PATH: Source path cannot be null or empty");
            }

            if (!File.Exists(sourcePath))
            {
                return Result.Fail<string>($"SOURCE_NOT_FOUND: Source file not found at '{sourcePath}'");
            }

            var baselinePath = BuildBaselinePath(category, testName, pageNumber);

            if (!File.Exists(baselinePath))
            {
                return Result.Fail<string>(
                    $"BASELINE_NOT_FOUND: No existing baseline found at '{baselinePath}'. Use CreateBaselineAsync to create a new baseline.");
            }

            // Copy source to baseline location (overwrite existing)
            await CopyFileAsync(sourcePath, baselinePath, cancellationToken);

            return Result.Ok(baselinePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return Result.Fail<string>($"IO_ERROR: Failed to update baseline: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail<string>($"BASELINE_UPDATE_FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds the full path to a baseline image.
    /// Pattern: {baselineRoot}/{category}/{testName}/page_{pageNumber}.png
    /// </summary>
    private string BuildBaselinePath(string category, string testName, int pageNumber)
    {
        return Path.Combine(
            _baselineRootPath,
            SanitizePathComponent(category),
            SanitizePathComponent(testName),
            $"page_{pageNumber}.png");
    }

    /// <summary>
    /// Sanitizes a path component to remove invalid characters.
    /// </summary>
    private static string SanitizePathComponent(string component)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", component.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Validates common parameters.
    /// </summary>
    private static void ValidateParameters(string category, string testName, int pageNumber)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category cannot be null or empty", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("Test name cannot be null or empty", nameof(testName));
        }

        if (pageNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                pageNumber,
                "Page number must be non-negative");
        }
    }

    /// <summary>
    /// Copies a file asynchronously with cancellation support.
    /// </summary>
    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 81920; // 80KB buffer

        await using var sourceStream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            useAsync: true);

        await using var destinationStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            useAsync: true);

        await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
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
