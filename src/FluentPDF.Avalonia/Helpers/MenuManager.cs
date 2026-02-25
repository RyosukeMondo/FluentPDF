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

        // Edit menu items
        var rotateClockwiseMenuItem = _owner.FindControl<MenuItem>("RotateClockwiseMenuItem");
        if (rotateClockwiseMenuItem != null) rotateClockwiseMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to page rotation when ViewModel command available
            _logger?.LogInformation("Rotate clockwise requested");
        };

        var rotateCounterClockwiseMenuItem = _owner.FindControl<MenuItem>("RotateCounterClockwiseMenuItem");
        if (rotateCounterClockwiseMenuItem != null) rotateCounterClockwiseMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to page rotation when ViewModel command available
            _logger?.LogInformation("Rotate counterclockwise requested");
        };

        var deletePageMenuItem = _owner.FindControl<MenuItem>("DeletePageMenuItem");
        if (deletePageMenuItem != null) deletePageMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to delete page command
            _logger?.LogInformation("Delete page requested");
        };

        var insertBlankPageMenuItem = _owner.FindControl<MenuItem>("InsertBlankPageMenuItem");
        if (insertBlankPageMenuItem != null) insertBlankPageMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to insert blank page command
            _logger?.LogInformation("Insert blank page requested");
        };

        var selectAllTextMenuItem = _owner.FindControl<MenuItem>("SelectAllTextMenuItem");
        if (selectAllTextMenuItem != null) selectAllTextMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to select all text command
            _logger?.LogInformation("Select all text requested");
        };

        // View menu items
        var viewThumbnailsMenuItem = _owner.FindControl<MenuItem>("ViewThumbnailsMenuItem");
        if (viewThumbnailsMenuItem != null) viewThumbnailsMenuItem.Click += (s, e) =>
        {
            var viewer = GetActiveViewer();
            if (viewer != null) viewer.ToggleThumbnailsCommand.Execute(null);
        };

        var viewBookmarksMenuItem = _owner.FindControl<MenuItem>("ViewBookmarksMenuItem");
        if (viewBookmarksMenuItem != null) viewBookmarksMenuItem.Click += (s, e) =>
        {
            var viewer = GetActiveViewer();
            if (viewer != null) viewer.ToggleBookmarksCommand.Execute(null);
        };

        var viewSearchMenuItem = _owner.FindControl<MenuItem>("ViewSearchMenuItem");
        if (viewSearchMenuItem != null) viewSearchMenuItem.Click += (s, e) =>
        {
            var viewer = GetActiveViewer();
            if (viewer != null) viewer.ShowSearchCommand.Execute(null);
        };

        // Tools menu items
        var watermarkMenuItem = _owner.FindControl<MenuItem>("WatermarkMenuItem");
        if (watermarkMenuItem != null) watermarkMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to watermark dialog
            _logger?.LogInformation("Add watermark requested");
        };

        var stampMenuItem = _owner.FindControl<MenuItem>("StampMenuItem");
        if (stampMenuItem != null) stampMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to stamp dialog
            _logger?.LogInformation("Add stamp requested");
        };

        var exportImagesMenuItem = _owner.FindControl<MenuItem>("ExportImagesMenuItem");
        if (exportImagesMenuItem != null) exportImagesMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to export images dialog
            _logger?.LogInformation("Export pages as images requested");
        };

        var exportFdfMenuItem = _owner.FindControl<MenuItem>("ExportFdfMenuItem");
        if (exportFdfMenuItem != null) exportFdfMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to FDF export
            _logger?.LogInformation("Export FDF requested");
        };

        var securityMenuItem = _owner.FindControl<MenuItem>("SecurityMenuItem");
        if (securityMenuItem != null) securityMenuItem.Click += (s, e) =>
        {
            // TODO: Wire to security dialog
            _logger?.LogInformation("Document security requested");
        };
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

        UpdateEditMenuStates();
    }

    /// <summary>
    /// Updates the enabled state of Edit menu items based on whether a document is open.
    /// </summary>
    public void UpdateEditMenuStates()
    {
        var hasActiveTab = _viewModel.ActiveTab != null;

        var editMenuItems = new[]
        {
            "RotateClockwiseMenuItem",
            "RotateCounterClockwiseMenuItem",
            "DeletePageMenuItem",
            "InsertBlankPageMenuItem",
            "SelectAllTextMenuItem",
        };

        foreach (var name in editMenuItems)
        {
            var item = _owner.FindControl<MenuItem>(name);
            if (item != null) item.IsEnabled = hasActiveTab;
        }

        // View menu items also depend on active document
        var viewMenuItems = new[]
        {
            "ViewThumbnailsMenuItem",
            "ViewBookmarksMenuItem",
            "ViewSearchMenuItem",
        };

        foreach (var name in viewMenuItems)
        {
            var item = _owner.FindControl<MenuItem>(name);
            if (item != null) item.IsEnabled = hasActiveTab;
        }

        // Tools menu items that require an open document
        var toolsMenuItems = new[]
        {
            "WatermarkMenuItem",
            "StampMenuItem",
            "ExportImagesMenuItem",
            "ExportFdfMenuItem",
            "SecurityMenuItem",
        };

        foreach (var name in toolsMenuItems)
        {
            var item = _owner.FindControl<MenuItem>(name);
            if (item != null) item.IsEnabled = hasActiveTab;
        }
    }

    private PdfViewerViewModel? GetActiveViewer() => _viewModel.ActiveTab?.ViewerViewModel;
}
