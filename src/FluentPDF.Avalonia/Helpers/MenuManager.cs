using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Manages menu event handling and recent files menu population.
/// Extracted from MainWindow to reduce complexity.
/// </summary>
public sealed class MenuManager
{
    private readonly Window _owner;
    private readonly MainViewModel _viewModel;
    private readonly ILogger? _logger;
    private readonly Func<Task> _onOpenFile;
    private readonly Func<Task> _onSave;
    private readonly Func<Task> _onSaveAs;
    private readonly Func<Task> _onSettings;
    private readonly Func<Task<bool>> _onClearRecentFiles;

    public MenuManager(
        Window owner,
        MainViewModel viewModel,
        Func<Task> onOpenFile,
        Func<Task> onSave,
        Func<Task> onSaveAs,
        Func<Task> onSettings,
        Func<Task<bool>> onClearRecentFiles,
        ILogger? logger = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _onOpenFile = onOpenFile ?? throw new ArgumentNullException(nameof(onOpenFile));
        _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
        _onSaveAs = onSaveAs ?? throw new ArgumentNullException(nameof(onSaveAs));
        _onSettings = onSettings ?? throw new ArgumentNullException(nameof(onSettings));
        _onClearRecentFiles = onClearRecentFiles ?? throw new ArgumentNullException(nameof(onClearRecentFiles));
        _logger = logger;
    }

    /// <summary>
    /// Sets up all menu event handlers.
    /// </summary>
    public void Initialize()
    {
        var openMenuItem = _owner.FindControl<MenuItem>("OpenMenuItem");
        var saveMenuItem = _owner.FindControl<MenuItem>("SaveMenuItem");
        var saveAsMenuItem = _owner.FindControl<MenuItem>("SaveAsMenuItem");
        var clearRecentFilesMenuItem = _owner.FindControl<MenuItem>("ClearRecentFilesMenuItem");
        var exitMenuItem = _owner.FindControl<MenuItem>("ExitMenuItem");
        var settingsMenuItem = _owner.FindControl<MenuItem>("SettingsMenuItem");
        var emptyStateOpenButton = _owner.FindControl<Button>("EmptyStateOpenButton");

        if (openMenuItem != null) openMenuItem.Click += async (s, e) => await _onOpenFile();
        if (saveMenuItem != null) saveMenuItem.Click += async (s, e) => await _onSave();
        if (saveAsMenuItem != null) saveAsMenuItem.Click += async (s, e) => await _onSaveAs();
        if (clearRecentFilesMenuItem != null) clearRecentFilesMenuItem.Click += OnClearRecentFilesClick;
        if (exitMenuItem != null) exitMenuItem.Click += (s, e) => _owner.Close();
        if (settingsMenuItem != null) settingsMenuItem.Click += async (s, e) => await _onSettings();
        if (emptyStateOpenButton != null) emptyStateOpenButton.Click += async (s, e) => await _onOpenFile();
    }

    private async void OnClearRecentFilesClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger?.LogInformation("Clear recent files requested");

            if (await _onClearRecentFiles())
            {
                _viewModel.ClearRecentFilesCommand.Execute(null);
                PopulateRecentFilesMenu();
                _logger?.LogInformation("Recent files cleared");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear recent files");
        }
    }

    /// <summary>
    /// Populates the Recent Files submenu.
    /// </summary>
    public void PopulateRecentFilesMenu()
    {
        var recentFilesSubMenu = _owner.FindControl<MenuItem>("RecentFilesSubMenu");
        if (recentFilesSubMenu == null) return;

        recentFilesSubMenu.Items?.Clear();

        var recentFiles = _viewModel.GetRecentFiles();

        if (recentFiles.Count == 0)
        {
            recentFilesSubMenu.Items?.Add(new MenuItem
            {
                Header = "No recent files",
                IsEnabled = false
            });
        }
        else
        {
            foreach (var file in recentFiles)
            {
                var menuItem = new MenuItem { Header = file.DisplayName };
                ToolTip.SetTip(menuItem, file.FilePath);

                var filePath = file.FilePath;
                menuItem.Click += async (s, e) =>
                {
                    _logger?.LogInformation("Opening recent file: {FilePath}", filePath);
                    await _viewModel.OpenRecentFileCommand.ExecuteAsync(filePath);
                    PopulateRecentFilesMenu();
                };

                recentFilesSubMenu.Items?.Add(menuItem);
            }
        }

        _logger?.LogDebug("Recent files menu populated with {Count} items", recentFiles.Count);
    }

    /// <summary>
    /// Updates the enabled state of Save menu items.
    /// </summary>
    public void UpdateMenuItemStates()
    {
        var saveMenuItem = _owner.FindControl<MenuItem>("SaveMenuItem");
        var saveAsMenuItem = _owner.FindControl<MenuItem>("SaveAsMenuItem");

        if (saveMenuItem != null && saveAsMenuItem != null)
        {
            var hasActiveTab = _viewModel.ActiveTab != null;
            var hasUnsavedChanges = _viewModel.ActiveTab?.HasUnsavedChanges ?? false;

            saveMenuItem.IsEnabled = hasActiveTab && hasUnsavedChanges;
            saveAsMenuItem.IsEnabled = hasActiveTab;

            _logger?.LogDebug(
                "Menu states updated. HasActiveTab={HasActiveTab}, HasUnsavedChanges={HasUnsavedChanges}",
                hasActiveTab, hasUnsavedChanges);
        }
    }
}
