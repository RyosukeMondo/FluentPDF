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
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _jumpListService = jumpListService ?? throw new ArgumentNullException(nameof(jumpListService));

        this.InitializeComponent();

        Title = "FluentPDF";

        // Set up keyboard shortcuts
        SetupKeyboardAccelerators();

        // Populate Recent Files menu
        PopulateRecentFilesMenu();

        // Set up empty state visibility handling
        ViewModel.Tabs.CollectionChanged += (s, e) => UpdateEmptyStateVisibility();
        UpdateEmptyStateVisibility();
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
        await ViewModel.OpenFileInNewTabCommand.ExecuteAsync(null);
        // Refresh Recent Files menu and Jump List
        PopulateRecentFilesMenu();
        await UpdateJumpListAsync();
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
    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is TabViewModel tab)
        {
            ViewModel.CloseTabCommand.Execute(tab);
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
        var settingsPage = new SettingsPage();

        var dialog = new ContentDialog
        {
            Title = "Settings",
            Content = settingsPage,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
