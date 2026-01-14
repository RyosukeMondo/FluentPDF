using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Windows.UI.StartScreen;

namespace FluentPDF.App.Services;

/// <summary>
/// Manages Windows Jump List integration for recent files.
/// </summary>
/// <remarks>
/// Updates the Windows taskbar Jump List with recently opened files.
/// Handles errors gracefully to prevent UI disruption.
/// Maximum of 10 items per Windows guidelines.
/// </remarks>
public sealed class JumpListService
{
    private const int MaxJumpListItems = 10;
    private readonly ILogger<JumpListService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JumpListService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking Jump List operations.</param>
    public JumpListService(ILogger<JumpListService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Updates the Windows Jump List with recent files.
    /// </summary>
    /// <param name="recentFiles">Collection of recent file entries to display in the Jump List.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Only the first 10 items are used, per Windows guidelines.
    /// Errors are logged but do not throw exceptions to prevent UI disruption.
    /// </remarks>
    public async Task UpdateJumpListAsync(IReadOnlyList<RecentFileEntry> recentFiles)
    {
        if (recentFiles == null)
        {
            throw new ArgumentNullException(nameof(recentFiles));
        }

        try
        {
            _logger.LogInformation("Updating Jump List with {Count} recent files", recentFiles.Count);

            var jumpList = await JumpList.LoadCurrentAsync();
            jumpList.Items.Clear();

            var itemsToAdd = recentFiles.Take(MaxJumpListItems);
            foreach (var file in itemsToAdd)
            {
                try
                {
                    var item = JumpListItem.CreateWithArguments(
                        file.FilePath,
                        file.DisplayName);

                    item.Description = file.FilePath;
                    item.GroupName = "Recent";

                    // Use default PDF icon - the system will use the file association icon
                    jumpList.Items.Add(item);

                    _logger.LogDebug("Added Jump List item: {FileName}", file.DisplayName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add Jump List item for: {FilePath}", file.FilePath);
                }
            }

            await jumpList.SaveAsync();
            _logger.LogInformation("Jump List updated successfully with {Count} items", jumpList.Items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Jump List");
        }
    }

    /// <summary>
    /// Clears all items from the Windows Jump List.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Errors are logged but do not throw exceptions to prevent UI disruption.
    /// </remarks>
    public async Task ClearJumpListAsync()
    {
        try
        {
            _logger.LogInformation("Clearing Jump List");

            var jumpList = await JumpList.LoadCurrentAsync();
            var itemCount = jumpList.Items.Count;
            jumpList.Items.Clear();
            await jumpList.SaveAsync();

            _logger.LogInformation("Jump List cleared ({Count} items removed)", itemCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear Jump List");
        }
    }
}
