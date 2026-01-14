using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace FluentPDF.App.Views;

/// <summary>
/// Main application window with TabView for multi-document interface.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Gets the view model for this window.
    /// </summary>
    public MainViewModel ViewModel { get; }

    private readonly JumpListService _jumpListService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The main view model.</param>
    /// <param name="jumpListService">Service for Windows Jump List integration.</param>
    public MainWindow(MainViewModel viewModel, JumpListService jumpListService)
    {
        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: MainWindow constructor starting\n");

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _jumpListService = jumpListService ?? throw new ArgumentNullException(nameof(jumpListService));

        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: MainWindow InitializeComponent starting\n");
        this.InitializeComponent();
        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: MainWindow InitializeComponent completed\n");

        Title = "FluentPDF";

        // Set up keyboard shortcuts
        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Setting up keyboard accelerators\n");
        SetupKeyboardAccelerators();

        // Populate Recent Files menu
        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Populating recent files menu\n");
        PopulateRecentFilesMenu();

        // Set up empty state visibility handling
        ViewModel.Tabs.CollectionChanged += (s, e) => UpdateEmptyStateVisibility();
        UpdateEmptyStateVisibility();

        // Set up menu item state updates
        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Setting up menu item state updates\n");
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.ActiveTab))
            {
                UpdateMenuItemStates();
                SubscribeToActiveTabChanges();
            }
        };
        UpdateMenuItemStates();
        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: MainWindow constructor completed\n");
    }

    /// <summary>
    /// Updates the visibility of the empty state overlay based on tab count.
    /// </summary>
    private void UpdateEmptyStateVisibility()
    {
        EmptyStateOverlay.Visibility = ViewModel.Tabs.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Sets up keyboard accelerators for tab management.
    /// </summary>
    private void SetupKeyboardAccelerators()
    {
        // Get the root content element for adding keyboard accelerators
        if (this.Content is not UIElement rootElement)
            return;

        // Ctrl+Tab: Next tab
        var nextTabAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Tab,
            Modifiers = VirtualKeyModifiers.Control
        };
        nextTabAccelerator.Invoked += OnNextTabAccelerator;
        rootElement.KeyboardAccelerators.Add(nextTabAccelerator);

        // Ctrl+Shift+Tab: Previous tab
        var prevTabAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Tab,
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
        };
        prevTabAccelerator.Invoked += OnPreviousTabAccelerator;
        rootElement.KeyboardAccelerators.Add(prevTabAccelerator);

        // Ctrl+W: Close current tab
        var closeTabAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.W,
            Modifiers = VirtualKeyModifiers.Control
        };
        closeTabAccelerator.Invoked += OnCloseTabAccelerator;
        rootElement.KeyboardAccelerators.Add(closeTabAccelerator);

        // Ctrl+O: Open file
        var openFileAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.O,
            Modifiers = VirtualKeyModifiers.Control
        };
        openFileAccelerator.Invoked += OnOpenFileAccelerator;
        rootElement.KeyboardAccelerators.Add(openFileAccelerator);
    }

    /// <summary>
    /// Handles Ctrl+Tab to switch to the next tab.
    /// </summary>
    private void OnNextTabAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.Tabs.Count <= 1 || ViewModel.ActiveTab is null)
        {
            args.Handled = true;
            return;
        }

        var currentIndex = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab);
        var nextIndex = (currentIndex + 1) % ViewModel.Tabs.Count;
        ViewModel.ActiveTab = ViewModel.Tabs[nextIndex];

        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+Shift+Tab to switch to the previous tab.
    /// </summary>
    private void OnPreviousTabAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.Tabs.Count <= 1 || ViewModel.ActiveTab is null)
        {
            args.Handled = true;
            return;
        }

        var currentIndex = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab);
        var prevIndex = currentIndex - 1;
        if (prevIndex < 0)
        {
            prevIndex = ViewModel.Tabs.Count - 1;
        }
        ViewModel.ActiveTab = ViewModel.Tabs[prevIndex];

        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+W to close the current tab.
    /// </summary>
    private void OnCloseTabAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.ActiveTab != null)
        {
            ViewModel.CloseTabCommand.Execute(ViewModel.ActiveTab);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+O to open a file.
    /// </summary>
    private void OnOpenFileAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        OnOpenFileClick(sender, null);
        args.Handled = true;
    }

    /// <summary>
    /// Handles the Open File menu item click.
    /// </summary>
    private async void OnOpenFileClick(object sender, RoutedEventArgs? e)
    {
        var debugLog = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log");
        try
        {
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: OnOpenFileClick invoked\n");
            await ViewModel.OpenFileInNewTabCommand.ExecuteAsync(null);
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: OpenFileInNewTabCommand completed\n");
            // Refresh Recent Files menu and Jump List
            PopulateRecentFilesMenu();
            await UpdateJumpListAsync();
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: OnOpenFileClick ERROR: {ex.Message}\n{ex.StackTrace}\n");
        }
    }

    /// <summary>
    /// Handles the Exit menu item click.
    /// </summary>
    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    /// <summary>
    /// Handles the Add Tab button click.
    /// </summary>
    private async void OnAddTabClick(TabView sender, object args)
    {
        await ViewModel.OpenFileInNewTabCommand.ExecuteAsync(null);
        // Refresh Recent Files menu and Jump List
        PopulateRecentFilesMenu();
        await UpdateJumpListAsync();
    }

    /// <summary>
    /// Handles tab close requests.
    /// </summary>
    private async void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not TabViewModel tab)
            return;

        // If no unsaved changes, close immediately
        if (!tab.HasUnsavedChanges)
        {
            ViewModel.CloseTabCommand.Execute(tab);
            return;
        }

        // Show save confirmation dialog
        var result = await SaveConfirmationDialog.ShowAsync(tab.FileName, this.Content.XamlRoot);

        switch (result)
        {
            case SaveConfirmationResult.Save:
                // Save the document first
                if (tab.ViewerViewModel?.SaveCommand is { } saveCommand && saveCommand.CanExecute(null))
                {
                    await saveCommand.ExecuteAsync(null);
                }
                // Then close the tab
                ViewModel.CloseTabCommand.Execute(tab);
                break;

            case SaveConfirmationResult.DontSave:
                // Close without saving
                ViewModel.CloseTabCommand.Execute(tab);
                break;

            case SaveConfirmationResult.Cancel:
                // Do nothing - tab remains open
                break;
        }
    }

    /// <summary>
    /// Populates the Recent Files submenu with recent file entries.
    /// </summary>
    private void PopulateRecentFilesMenu()
    {
        RecentFilesSubMenu.Items.Clear();

        var recentFiles = ViewModel.GetRecentFiles();

        if (recentFiles.Count == 0)
        {
            var noFilesItem = new MenuFlyoutItem
            {
                Text = "No recent files",
                IsEnabled = false
            };
            RecentFilesSubMenu.Items.Add(noFilesItem);
        }
        else
        {
            foreach (var file in recentFiles)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = file.DisplayName
                };

                // Add tooltip with full path
                ToolTipService.SetToolTip(menuItem, file.FilePath);

                // Handle click to open the recent file
                menuItem.Click += async (sender, e) =>
                {
                    await ViewModel.OpenRecentFileCommand.ExecuteAsync(file.FilePath);
                    // Refresh menu and Jump List after opening
                    PopulateRecentFilesMenu();
                    await UpdateJumpListAsync();
                };

                RecentFilesSubMenu.Items.Add(menuItem);
            }
        }
    }

    /// <summary>
    /// Handles the Clear Recent Files menu item click.
    /// </summary>
    private async void OnClearRecentFilesClick(object sender, RoutedEventArgs e)
    {
        // Show confirmation dialog
        var dialog = new ContentDialog
        {
            Title = "Clear Recent Files",
            Content = "Are you sure you want to clear all recent files?",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.ClearRecentFilesCommand.Execute(null);
            PopulateRecentFilesMenu();
            await _jumpListService.ClearJumpListAsync();
        }
    }

    /// <summary>
    /// Updates the Windows Jump List with current recent files.
    /// </summary>
    private async Task UpdateJumpListAsync()
    {
        var recentFiles = ViewModel.GetRecentFiles();
        await _jumpListService.UpdateJumpListAsync(recentFiles);
    }

    /// <summary>
    /// Handles the Settings menu item click.
    /// </summary>
    private async void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var debugLog = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log");
        try
        {
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: OnSettingsClick invoked\n");
            var settingsPage = new SettingsPage();
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: SettingsPage created\n");

            var dialog = new ContentDialog
            {
                Title = "Settings",
                Content = settingsPage,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: ContentDialog created, showing...\n");

            await dialog.ShowAsync();
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: ContentDialog closed\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(debugLog, $"{DateTime.Now}: Settings ERROR: {ex.Message}\n{ex.StackTrace}\n");
        }
    }

    /// <summary>
    /// Handles the Save menu item click.
    /// </summary>
    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.SaveCommand is { } saveCommand && saveCommand.CanExecute(null))
        {
            await saveCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Handles the Save As menu item click.
    /// </summary>
    private async void OnSaveAsClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.SaveAsCommand is { } saveAsCommand)
        {
            await saveAsCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Updates the enabled state of Save menu items based on active tab state.
    /// </summary>
    private void UpdateMenuItemStates()
    {
        var hasActiveTab = ViewModel.ActiveTab != null;
        var hasUnsavedChanges = ViewModel.ActiveTab?.HasUnsavedChanges ?? false;

        SaveMenuItem.IsEnabled = hasActiveTab && hasUnsavedChanges;
        SaveAsMenuItem.IsEnabled = hasActiveTab;
    }

    /// <summary>
    /// Subscribes to property changes on the active tab to update menu states.
    /// </summary>
    private void SubscribeToActiveTabChanges()
    {
        if (ViewModel.ActiveTab != null)
        {
            ViewModel.ActiveTab.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TabViewModel.HasUnsavedChanges))
                {
                    UpdateMenuItemStates();
                }
            };
        }
    }
}
