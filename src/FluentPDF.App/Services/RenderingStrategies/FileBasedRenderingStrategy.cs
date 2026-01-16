using FluentPDF.App.Interfaces;
using FluentPDF.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;

namespace FluentPDF.App.Services.RenderingStrategies;

/// <summary>
/// Fallback rendering strategy that saves PNG to a temporary file and loads via BitmapImage with file URI.
/// This approach completely avoids WinUI's stream-based image loading APIs which have known reliability issues.
/// </summary>
/// <remarks>
/// This strategy trades performance for reliability by using the file system as an intermediate buffer.
/// Temporary files are tracked and cleaned up periodically to avoid disk space leaks.
/// </remarks>
public sealed class FileBasedRenderingStrategy : IRenderingStrategy, IDisposable
{
    /// <summary>
    /// Tracks all temporary files created by this strategy for cleanup.
    /// Thread-safe collection for concurrent rendering operations.
    /// </summary>
    private static readonly ConcurrentBag<string> _tempFiles = new();

    /// <summary>
    /// Maximum number of temp files to keep before triggering cleanup.
    /// </summary>
    private const int MaxTempFilesBeforeCleanup = 100;

    /// <inheritdoc/>
    public string StrategyName => "FileBased";

    /// <inheritdoc/>
    public int Priority => 10; // Fallback priority - try after primary strategies

    /// <inheritdoc/>
    public async Task<ImageSource?> TryRenderAsync(Stream pngStream, RenderContext context)
    {
        string? tempFilePath = null;

        try
        {
            // Reset stream position to beginning
            pngStream.Seek(0, SeekOrigin.Begin);

            // Generate unique temporary file path
            var tempDir = Path.GetTempPath();
            var fileName = $"FluentPDF_{Guid.NewGuid():N}.png";
            tempFilePath = Path.Combine(tempDir, fileName);

            // Write PNG stream to temporary file
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await pngStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }

            // Track temp file for later cleanup
            _tempFiles.Add(tempFilePath);

            // Trigger cleanup if we have too many temp files
            if (_tempFiles.Count > MaxTempFilesBeforeCleanup)
            {
                _ = Task.Run(CleanupOldTempFiles);
            }

            // Load image from file URI
            // BitmapImage with file URIs is much more reliable than stream-based loading
            var bitmapImage = new BitmapImage();
            bitmapImage.UriSource = new Uri(tempFilePath, UriKind.Absolute);

            return bitmapImage;
        }
        catch (Exception)
        {
            // Clean up temp file if we created it but failed to load
            if (tempFilePath != null)
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // Ignore cleanup failures - file will be cleaned up later
                }
            }

            // Swallow all exceptions and return null to indicate failure
            // Caller (RenderingCoordinator) will log the failure
            return null;
        }
    }

    /// <summary>
    /// Cleans up old temporary files to prevent disk space leaks.
    /// Removes files that are older than 1 hour or no longer exist.
    /// </summary>
    private static void CleanupOldTempFiles()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        var filesToRemove = new List<string>();

        foreach (var filePath in _tempFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // Remove if file doesn't exist or is older than cutoff
                if (!fileInfo.Exists || fileInfo.CreationTimeUtc < cutoffTime)
                {
                    if (fileInfo.Exists)
                    {
                        File.Delete(filePath);
                    }
                    filesToRemove.Add(filePath);
                }
            }
            catch
            {
                // If we can't check/delete the file, mark it for removal from tracking
                filesToRemove.Add(filePath);
            }
        }

        // Remove tracked files (note: ConcurrentBag doesn't have Remove, so we rebuild)
        // This is acceptable since cleanup happens infrequently
        if (filesToRemove.Count > 0)
        {
            // Create new bag with files that weren't removed
            var remainingFiles = _tempFiles.Except(filesToRemove);
            _tempFiles.Clear();
            foreach (var file in remainingFiles)
            {
                _tempFiles.Add(file);
            }
        }
    }

    /// <summary>
    /// Cleans up all tracked temporary files on disposal.
    /// Called when the application exits or the strategy is no longer needed.
    /// </summary>
    public void Dispose()
    {
        foreach (var filePath in _tempFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore cleanup failures during disposal
            }
        }

        _tempFiles.Clear();
    }

    /// <summary>
    /// Static cleanup method that can be called on application exit.
    /// Ensures all temporary files are removed even if Dispose is not called.
    /// </summary>
    public static void CleanupAllTempFiles()
    {
        foreach (var filePath in _tempFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }

        _tempFiles.Clear();
    }
}
