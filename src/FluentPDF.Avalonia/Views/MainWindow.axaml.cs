using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Main application window with TabControl for multi-document interface.
/// Avalonia port of the WinUI 3 MainWindow with full feature parity.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow>? _logger;
    private Services.ILogBufferService? _logBuffer;  // Not readonly - assigned in SetupDebugConsole
    private readonly DispatcherTimer _logRefreshTimer;
    private readonly ObservableCollection<Services.BufferedLogEntry> _logEntries;
    private ScrollViewer? _logScrollViewer;

    /// <summary>
    /// Gets the view model for this window.
    /// </summary>
    public MainViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The main view model.</param>
    /// <param name="logger">Optional logger for tracking operations.</param>
    public MainWindow(MainViewModel viewModel, ILogger<MainWindow>? logger = null)
    {
        Console.WriteLine(">>> MainWindow constructor [1/10] - ENTRY");
        try
        {
            Console.WriteLine(">>> MainWindow constructor [2/10] - Setting ViewModel");
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger;

            Console.WriteLine(">>> MainWindow constructor [3/10] - Creating log entries collection");
            _logEntries = new ObservableCollection<Services.BufferedLogEntry>();

            Console.WriteLine(">>> MainWindow constructor [4/10] - Creating timer");
            _logRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _logRefreshTimer.Tick += OnLogRefreshTimerTick;

            Console.WriteLine(">>> MainWindow constructor [5/10] - Calling InitializeComponent...");
            InitializeComponent();
            Console.WriteLine(">>> MainWindow constructor [6/10] - InitializeComponent complete");

            Console.WriteLine(">>> MainWindow constructor [7/10] - Setting window properties");
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Console.WriteLine(">>> MainWindow constructor [8/10] - Setting DataContext");
            DataContext = ViewModel;

            Console.WriteLine(">>> MainWindow constructor [9/10] - Registering Loaded event");
            this.Loaded += OnWindowLoaded;

            Console.WriteLine(">>> MainWindow constructor [10/10] - COMPLETE");
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> FATAL in MainWindow constructor: {ex.Message}");
            Console.WriteLine($">>> Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($">>> Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// Handles window loaded event - performs full initialization after UI message loop is running.
    /// </summary>
    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("=== Window Loaded - Deferring initialization ===");

        // Run ALL initialization in background thread - NEVER block UI thread
        Task.Run(() =>
        {
            try
            {
                Console.WriteLine(">>> Deferred init [1/8] Starting...");

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Console.WriteLine(">>> Deferred init [2/8] SetupMenuHandlers...");
                        SetupMenuHandlers();
                        Console.WriteLine(">>> Deferred init [3/8] Menu handlers done");
                    }
                    catch (Exception ex) { Console.WriteLine($">>> Menu handlers error: {ex.Message}"); }
                });

                System.Threading.Thread.Sleep(100); // Let UI thread process

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Console.WriteLine(">>> Deferred init [4/8] SetupKeyboardShortcuts...");
                        SetupKeyboardShortcuts();
                        Console.WriteLine(">>> Deferred init [5/8] Keyboard shortcuts done");
                    }
                    catch (Exception ex) { Console.WriteLine($">>> Keyboard shortcuts error: {ex.Message}"); }
                });

                System.Threading.Thread.Sleep(100);

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Console.WriteLine(">>> Deferred init [6/8] Event handlers...");
                        ViewModel.Tabs.CollectionChanged += (s, evt) =>
                        {
                            Dispatcher.UIThread.Post(UpdateEmptyStateVisibility, DispatcherPriority.Background);
                        };
                        UpdateEmptyStateVisibility();
                        ViewModel.PropertyChanged += (s, evt) =>
                        {
                            if (evt.PropertyName == nameof(ViewModel.ActiveTab))
                            {
                                // CRITICAL: Must marshal to UI thread - these methods access UI controls
                                Dispatcher.UIThread.Post(() =>
                                {
                                    try { UpdateMenuItemStates(); SubscribeToActiveTabChanges(); }
                                    catch { }
                                }, DispatcherPriority.Background);
                            }
                        };
                        Console.WriteLine(">>> Deferred init [7/8] Event handlers done");
                    }
                    catch (Exception ex) { Console.WriteLine($">>> Event handlers error: {ex.Message}"); }
                });

                System.Threading.Thread.Sleep(100);

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Console.WriteLine(">>> Deferred init [8/8] SetupDebugConsole...");
                        SetupDebugConsole();
                        Console.WriteLine(">>> Deferred init COMPLETE - UI should be responsive now!");
                    }
                    catch (Exception ex) { Console.WriteLine($">>> Debug console error: {ex.Message}"); }
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> Deferred initialization FAILED: {ex.Message}");
            }
        });

        Console.WriteLine("=== Window Loaded event complete ===");
    }

    /// <summary>
    /// Initializes the component by loading the XAML.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Sets up event handlers for menu items.
    /// </summary>
    private void SetupMenuHandlers()
    {
        var openMenuItem = this.FindControl<MenuItem>("OpenMenuItem");
        var saveMenuItem = this.FindControl<MenuItem>("SaveMenuItem");
        var saveAsMenuItem = this.FindControl<MenuItem>("SaveAsMenuItem");
        var clearRecentFilesMenuItem = this.FindControl<MenuItem>("ClearRecentFilesMenuItem");
        var exitMenuItem = this.FindControl<MenuItem>("ExitMenuItem");
        var settingsMenuItem = this.FindControl<MenuItem>("SettingsMenuItem");
        var emptyStateOpenButton = this.FindControl<Button>("EmptyStateOpenButton");

        if (openMenuItem != null)
            openMenuItem.Click += OnOpenFileClick;

        if (saveMenuItem != null)
            saveMenuItem.Click += OnSaveClick;

        if (saveAsMenuItem != null)
            saveAsMenuItem.Click += OnSaveAsClick;

        if (clearRecentFilesMenuItem != null)
            clearRecentFilesMenuItem.Click += OnClearRecentFilesClick;

        if (exitMenuItem != null)
            exitMenuItem.Click += OnExitClick;

        if (settingsMenuItem != null)
            settingsMenuItem.Click += OnSettingsClick;

        if (emptyStateOpenButton != null)
            emptyStateOpenButton.Click += OnOpenFileClick;

        SetupToolbarHandlers();
    }

    /// <summary>
    /// Sets up event handlers for toolbar buttons.
    /// </summary>
    private void SetupToolbarHandlers()
    {
        // File Operations
        var toolbarOpenButton = this.FindControl<Controls.LiquidButton>("ToolbarOpenButton");
        if (toolbarOpenButton != null)
            toolbarOpenButton.Click += OnOpenFileClick;

        // Navigation
        var toolbarPreviousPageButton = this.FindControl<Controls.LiquidButton>("ToolbarPreviousPageButton");
        if (toolbarPreviousPageButton != null)
            toolbarPreviousPageButton.Click += OnToolbarPreviousPageClick;

        var toolbarNextPageButton = this.FindControl<Controls.LiquidButton>("ToolbarNextPageButton");
        if (toolbarNextPageButton != null)
            toolbarNextPageButton.Click += OnToolbarNextPageClick;

        var toolbarPageNumberBox = this.FindControl<TextBox>("ToolbarPageNumberBox");
        if (toolbarPageNumberBox != null)
            toolbarPageNumberBox.KeyDown += OnToolbarPageNumberKeyDown;

        // Zoom
        var toolbarZoomInButton = this.FindControl<Controls.LiquidButton>("ToolbarZoomInButton");
        if (toolbarZoomInButton != null)
            toolbarZoomInButton.Click += OnToolbarZoomInClick;

        var toolbarZoomOutButton = this.FindControl<Controls.LiquidButton>("ToolbarZoomOutButton");
        if (toolbarZoomOutButton != null)
            toolbarZoomOutButton.Click += OnToolbarZoomOutClick;

        var toolbarZoomComboBox = this.FindControl<ComboBox>("ToolbarZoomComboBox");
        if (toolbarZoomComboBox != null)
            toolbarZoomComboBox.SelectionChanged += OnToolbarZoomSelectionChanged;

        // View Toggles
        var toolbarToggleThumbnailsButton = this.FindControl<ToggleButton>("ToolbarToggleThumbnailsButton");
        if (toolbarToggleThumbnailsButton != null)
            toolbarToggleThumbnailsButton.Click += OnToolbarToggleThumbnailsClick;

        var toolbarToggleBookmarksButton = this.FindControl<ToggleButton>("ToolbarToggleBookmarksButton");
        if (toolbarToggleBookmarksButton != null)
            toolbarToggleBookmarksButton.Click += OnToolbarToggleBookmarksClick;

        var toolbarSearchButton = this.FindControl<Controls.LiquidButton>("ToolbarSearchButton");
        if (toolbarSearchButton != null)
            toolbarSearchButton.Click += OnToolbarSearchClick;
    }

    /// <summary>
    /// Sets up keyboard shortcuts for the window.
    /// Implements global shortcuts and tab navigation.
    /// </summary>
    private void SetupKeyboardShortcuts()
    {
        // Global keyboard shortcut handler
        this.KeyDown += async (sender, e) =>
        {
            var modifiers = e.KeyModifiers;
            var key = e.Key;

            // Ctrl+Tab: Next tab
            if (key == Key.Tab && modifiers == KeyModifiers.Control)
            {
                OnNextTab();
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+Tab: Previous tab
            if (key == Key.Tab && modifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                OnPreviousTab();
                e.Handled = true;
                return;
            }

            // Ctrl+W: Close current tab
            if (key == Key.W && modifiers == KeyModifiers.Control)
            {
                await OnCloseCurrentTabAsync();
                e.Handled = true;
                return;
            }

            // Ctrl+O: Open file
            if (key == Key.O && modifiers == KeyModifiers.Control)
            {
                await OnOpenFileClickAsync();
                e.Handled = true;
                return;
            }

            // Ctrl+S: Save document
            if (key == Key.S && modifiers == KeyModifiers.Control)
            {
                await OnSaveClickAsync();
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+S: Save As
            if (key == Key.S && modifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                await OnSaveAsClickAsync();
                e.Handled = true;
                return;
            }
        };
    }

    /// <summary>
    /// Updates the visibility of the empty state overlay based on tab count.
    /// </summary>
    private void UpdateEmptyStateVisibility()
    {
        _logger?.LogDebug("Updating empty state visibility. Tab count: {Count}", ViewModel.Tabs.Count);
        // Visibility is handled by binding in XAML
    }

    /// <summary>
    /// Handles Ctrl+Tab to switch to the next tab.
    /// </summary>
    private void OnNextTab()
    {
        if (ViewModel.Tabs.Count <= 1 || ViewModel.ActiveTab is null)
            return;

        var currentIndex = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab);
        var nextIndex = (currentIndex + 1) % ViewModel.Tabs.Count;
        ViewModel.ActiveTab = ViewModel.Tabs[nextIndex];

        _logger?.LogDebug("Switched to next tab: {Index}", nextIndex);
    }

    /// <summary>
    /// Handles Ctrl+Shift+Tab to switch to the previous tab.
    /// </summary>
    private void OnPreviousTab()
    {
        if (ViewModel.Tabs.Count <= 1 || ViewModel.ActiveTab is null)
            return;

        var currentIndex = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab);
        var prevIndex = currentIndex - 1;
        if (prevIndex < 0)
            prevIndex = ViewModel.Tabs.Count - 1;

        ViewModel.ActiveTab = ViewModel.Tabs[prevIndex];

        _logger?.LogDebug("Switched to previous tab: {Index}", prevIndex);
    }

    /// <summary>
    /// Handles Ctrl+W to close the current tab.
    /// </summary>
    private async Task OnCloseCurrentTabAsync()
    {
        if (ViewModel.ActiveTab != null)
        {
            await OnTabCloseRequestedAsync(ViewModel.ActiveTab);
        }
    }

    /// <summary>
    /// Handles tab close requests with save confirmation if needed.
    /// </summary>
    private async Task OnTabCloseRequestedAsync(TabViewModel tab)
    {
        if (tab == null)
            return;

        _logger?.LogInformation("Tab close requested for: {FilePath}", tab.FilePath);

        // If no unsaved changes, close immediately
        if (!tab.HasUnsavedChanges)
        {
            ViewModel.CloseTabCommand.Execute(tab);
            return;
        }

        // Show save confirmation dialog
        var result = await ShowSaveConfirmationDialogAsync(tab.FileName);

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
                _logger?.LogDebug("Tab close cancelled by user");
                break;
        }
    }

    /// <summary>
    /// Shows a save confirmation dialog and returns the user's choice.
    /// </summary>
    private async Task<SaveConfirmationResult> ShowSaveConfirmationDialogAsync(string filename)
    {
        var messageBox = new Window
        {
            Width = 400,
            Height = 200,
            Title = "Unsaved Changes",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var result = SaveConfirmationResult.Cancel;

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = $"Do you want to save changes to {filename}?",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });

        var buttonPanel = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8
        };

        var saveButton = new Button { Content = "Save", Width = 100 };
        saveButton.Click += (s, e) =>
        {
            result = SaveConfirmationResult.Save;
            messageBox.Close();
        };

        var dontSaveButton = new Button { Content = "Don't Save", Width = 100 };
        dontSaveButton.Click += (s, e) =>
        {
            result = SaveConfirmationResult.DontSave;
            messageBox.Close();
        };

        var cancelButton = new Button { Content = "Cancel", Width = 100 };
        cancelButton.Click += (s, e) =>
        {
            result = SaveConfirmationResult.Cancel;
            messageBox.Close();
        };

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(dontSaveButton);
        buttonPanel.Children.Add(cancelButton);

        panel.Children.Add(buttonPanel);
        messageBox.Content = panel;

        await messageBox.ShowDialog(this);
        return result;
    }

    /// <summary>
    /// Handles the Open File menu item click.
    /// </summary>
    private async void OnOpenFileClick(object? sender, RoutedEventArgs e)
    {
        await OnOpenFileClickAsync();
    }

    /// <summary>
    /// Opens the file picker and loads the selected PDF.
    /// </summary>
    private async Task OnOpenFileClickAsync()
    {
        try
        {
            _logger?.LogInformation("Opening file picker");

            var storageProvider = StorageProvider;
            if (storageProvider == null)
            {
                _logger?.LogError("StorageProvider is null");
                return;
            }

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open PDF File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PDF Files")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            });

            if (files.Count == 0)
            {
                _logger?.LogInformation("File picker cancelled");
                return;
            }

            var file = files[0];
            var filePath = file.Path.LocalPath;

            _logger?.LogInformation("File selected: {FilePath}", filePath);
            _logBuffer?.AddLog("Info", $">>> USER CLICKED: Open file: {filePath}", "MainWindow");
            _logBuffer?.AddLog("Info", $">>> Calling ViewModel.OpenRecentFileCommand...", "MainWindow");

            // Open file using MainViewModel
            await ViewModel.OpenRecentFileCommand.ExecuteAsync(filePath);

            _logBuffer?.AddLog("Info", $">>> OpenRecentFileCommand completed", "MainWindow");
            _logBuffer?.AddLog("Info", $">>> Active tab count: {ViewModel.Tabs.Count}", "MainWindow");
            _logBuffer?.AddLog("Info", $">>> Active tab: {ViewModel.ActiveTab?.FileName ?? "NULL"}", "MainWindow");

            // Refresh Recent Files menu
            PopulateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open file");
            await ShowErrorDialogAsync("Error Opening File", $"Failed to open file:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Handles the Exit menu item click.
    /// </summary>
    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        _logger?.LogInformation("Exit menu item clicked");
        Close();
    }

    /// <summary>
    /// Handles the Save menu item click.
    /// </summary>
    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        await OnSaveClickAsync();
    }

    /// <summary>
    /// Saves the current document.
    /// </summary>
    private async Task OnSaveClickAsync()
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.SaveCommand is { } saveCommand && saveCommand.CanExecute(null))
        {
            _logger?.LogInformation("Saving current document");
            await saveCommand.ExecuteAsync(null);
        }
        else
        {
            _logger?.LogWarning("Save command is not available");
        }
    }

    /// <summary>
    /// Handles the Save As menu item click.
    /// </summary>
    private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
    {
        await OnSaveAsClickAsync();
    }

    /// <summary>
    /// Saves the current document with a new name.
    /// </summary>
    private async Task OnSaveAsClickAsync()
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.SaveAsCommand is { } saveAsCommand)
        {
            _logger?.LogInformation("Save As command invoked");
            await saveAsCommand.ExecuteAsync(null);
        }
        else
        {
            _logger?.LogWarning("Save As command is not available");
        }
    }

    /// <summary>
    /// Handles the Settings menu item click.
    /// </summary>
    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger?.LogInformation("Opening settings dialog");

            // TODO: Create SettingsPage for Avalonia
            await ShowErrorDialogAsync("Not Implemented", "Settings dialog is not yet implemented in Avalonia version.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open settings dialog");
        }
    }

    /// <summary>
    /// Populates the Recent Files submenu with recent file entries.
    /// </summary>
    private void PopulateRecentFilesMenu()
    {
        var recentFilesSubMenu = this.FindControl<MenuItem>("RecentFilesSubMenu");
        if (recentFilesSubMenu == null)
            return;

        // Clear existing items
        recentFilesSubMenu.Items?.Clear();

        var recentFiles = ViewModel.GetRecentFiles();

        if (recentFiles.Count == 0)
        {
            var noFilesItem = new MenuItem
            {
                Header = "No recent files",
                IsEnabled = false
            };
            recentFilesSubMenu.Items?.Add(noFilesItem);
        }
        else
        {
            foreach (var file in recentFiles)
            {
                var menuItem = new MenuItem
                {
                    Header = file.DisplayName
                };

                // Add tooltip with full path
                ToolTip.SetTip(menuItem, file.FilePath);

                // Handle click to open the recent file
                var filePath = file.FilePath; // Capture for lambda
                menuItem.Click += async (sender, e) =>
                {
                    _logger?.LogInformation("Opening recent file: {FilePath}", filePath);
                    await ViewModel.OpenRecentFileCommand.ExecuteAsync(filePath);
                    PopulateRecentFilesMenu();
                };

                recentFilesSubMenu.Items?.Add(menuItem);
            }
        }

        _logger?.LogDebug("Recent files menu populated with {Count} items", recentFiles.Count);
    }

    /// <summary>
    /// Handles the Clear Recent Files menu item click.
    /// </summary>
    private async void OnClearRecentFilesClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logger?.LogInformation("Clear recent files requested");

            var result = await ShowConfirmationDialogAsync(
                "Clear Recent Files",
                "Are you sure you want to clear all recent files?");

            if (result)
            {
                ViewModel.ClearRecentFilesCommand.Execute(null);
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
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    private async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        var messageBox = new Window
        {
            Width = 400,
            Height = 180,
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var result = false;

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });

        var buttonPanel = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8
        };

        var yesButton = new Button { Content = "Yes", Width = 100 };
        yesButton.Click += (s, e) =>
        {
            result = true;
            messageBox.Close();
        };

        var noButton = new Button { Content = "No", Width = 100 };
        noButton.Click += (s, e) =>
        {
            result = false;
            messageBox.Close();
        };

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);

        panel.Children.Add(buttonPanel);
        messageBox.Content = panel;

        await messageBox.ShowDialog(this);
        return result;
    }

    /// <summary>
    /// Shows an error dialog to the user.
    /// </summary>
    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var messageBox = new Window
        {
            Width = 400,
            Height = 200,
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });

        var button = new Button
        {
            Content = "OK",
            Width = 100,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        button.Click += (s, e) => messageBox.Close();

        panel.Children.Add(button);
        messageBox.Content = panel;

        await messageBox.ShowDialog(this);
    }

    /// <summary>
    /// Updates the enabled state of Save menu items based on active tab state.
    /// </summary>
    private void UpdateMenuItemStates()
    {
        var saveMenuItem = this.FindControl<MenuItem>("SaveMenuItem");
        var saveAsMenuItem = this.FindControl<MenuItem>("SaveAsMenuItem");

        if (saveMenuItem != null && saveAsMenuItem != null)
        {
            var hasActiveTab = ViewModel.ActiveTab != null;
            var hasUnsavedChanges = ViewModel.ActiveTab?.HasUnsavedChanges ?? false;

            saveMenuItem.IsEnabled = hasActiveTab && hasUnsavedChanges;
            saveAsMenuItem.IsEnabled = hasActiveTab;

            _logger?.LogDebug(
                "Menu states updated. HasActiveTab={HasActiveTab}, HasUnsavedChanges={HasUnsavedChanges}",
                hasActiveTab, hasUnsavedChanges);
        }
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
                    // CRITICAL: Must marshal to UI thread - UpdateMenuItemStates accesses UI controls
                    Dispatcher.UIThread.Post(UpdateMenuItemStates, DispatcherPriority.Background);
                }
            };

            // Subscribe to viewer page changes to update toolbar
            if (ViewModel.ActiveTab.ViewerViewModel != null)
            {
                ViewModel.ActiveTab.ViewerViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageIndex))
                    {
                        Dispatcher.UIThread.Post(UpdateToolbarPageNumber, DispatcherPriority.Background);
                    }
                };

                // Initial toolbar update
                UpdateToolbarPageNumber();
            }
        }
    }

    /// <summary>
    /// Sets up the debug console with real-time log streaming.
    /// </summary>
    private void SetupDebugConsole()
    {
        try
        {
            // Get LogBufferService from DI
            _logBuffer = App.Services.GetService(typeof(Services.ILogBufferService)) as Services.ILogBufferService;

            // Wire up controls
            _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            var logItemsControl = this.FindControl<ItemsControl>("LogItemsControl");
            var copyLogsButton = this.FindControl<Button>("CopyLogsButton");
            var downloadLogButton = this.FindControl<Button>("DownloadLogButton");
            var clearLogsButton = this.FindControl<Button>("ClearLogsButton");

            if (logItemsControl != null)
            {
                logItemsControl.ItemsSource = _logEntries;
            }

            if (copyLogsButton != null)
            {
                copyLogsButton.Click += OnCopyLogsClick;
            }

            if (downloadLogButton != null)
            {
                downloadLogButton.Click += OnDownloadLogClick;
            }

            if (clearLogsButton != null)
            {
                clearLogsButton.Click += OnClearLogsClick;
            }

            // Add initial log message
            if (_logBuffer != null)
            {
                _logBuffer.AddLog("Info", "Debug console initialized - real-time logging enabled", "MainWindow");
                _logBuffer.AddLog("Info", "Click 'Copy Last 50' to copy recent logs to clipboard", "MainWindow");
                _logBuffer.AddLog("Info", "Click 'Download Log' to save full log file", "MainWindow");
                _logBuffer.AddLog("Info", "---", "MainWindow");
            }

            // DISABLED: Start log refresh timer
            // _logRefreshTimer.Start();
            Console.WriteLine(">>> Log refresh timer DISABLED to test UI responsiveness");

            _logger?.LogInformation("Debug console initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize debug console");
            Console.WriteLine($"Debug console setup error: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the debug console with latest logs from buffer.
    /// </summary>
    private void OnLogRefreshTimerTick(object? sender, EventArgs e)
    {
        try
        {
            if (_logBuffer == null)
                return;

            var latestLogs = _logBuffer.GetRecentLogs(100);

            // Only update if there are new logs
            if (latestLogs.Count > _logEntries.Count ||
                (latestLogs.Count > 0 && _logEntries.Count > 0 &&
                 latestLogs[latestLogs.Count - 1].Timestamp != _logEntries[_logEntries.Count - 1].Timestamp))
            {
                _logEntries.Clear();
                foreach (var log in latestLogs)
                {
                    _logEntries.Add(log);
                }

                // Auto-scroll to bottom
                Dispatcher.UIThread.Post(() =>
                {
                    _logScrollViewer?.ScrollToEnd();
                }, DispatcherPriority.Background);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing debug console logs");
        }
    }

    /// <summary>
    /// Copies the last 50 log entries to clipboard.
    /// </summary>
    private async void OnCopyLogsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_logBuffer == null)
                return;

            var logs = _logBuffer.GetRecentLogs(50);
            var sb = new StringBuilder();
            sb.AppendLine("=== FluentPDF Debug Logs (Last 50 entries) ===");
            sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:HH:mm:ss.fff}] [{log.Level}] [{log.Source}] {log.Message}");
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(sb.ToString());
                _logBuffer.AddLog("Info", "Last 50 logs copied to clipboard", "DebugConsole");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to copy logs to clipboard");
        }
    }

    /// <summary>
    /// Downloads all logs to a file.
    /// </summary>
    private async void OnDownloadLogClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_logBuffer == null)
                return;

            var storageProvider = StorageProvider;
            if (storageProvider == null)
                return;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Debug Log",
                SuggestedFileName = $"fluentpdf-debug-{DateTime.Now:yyyyMMdd-HHmmss}.log",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Log Files")
                    {
                        Patterns = new[] { "*.log" }
                    },
                    new FilePickerFileType("Text Files")
                    {
                        Patterns = new[] { "*.txt" }
                    }
                }
            });

            if (file == null)
                return;

            var logs = _logBuffer.GetRecentLogs(1000);
            var sb = new StringBuilder();
            sb.AppendLine("=== FluentPDF Debug Logs ===");
            sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Entries: {logs.Count}");
            sb.AppendLine();

            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{log.Level}] [{log.Source}] {log.Message}");
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(sb.ToString());

            _logBuffer.AddLog("Info", $"Debug log saved to: {file.Name}", "DebugConsole");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download log file");
        }
    }

    /// <summary>
    /// Clears all logs from the buffer.
    /// </summary>
    private void OnClearLogsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _logBuffer?.Clear();
            _logEntries.Clear();
            _logBuffer?.AddLog("Info", "Debug console cleared", "DebugConsole");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear logs");
        }
    }

    #region Toolbar Event Handlers

    /// <summary>
    /// Handles toolbar previous page button click.
    /// </summary>
    private void OnToolbarPreviousPageClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.GoToPreviousPageCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
            UpdateToolbarPageNumber();
        }
    }

    /// <summary>
    /// Handles toolbar next page button click.
    /// </summary>
    private void OnToolbarNextPageClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.GoToNextPageCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
            UpdateToolbarPageNumber();
        }
    }

    /// <summary>
    /// Handles toolbar page number text box key press (Enter to navigate).
    /// </summary>
    private void OnToolbarPageNumberKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = sender as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int pageNumber))
            {
                if (ViewModel.ActiveTab?.ViewerViewModel?.GoToPageCommand is { } command && command.CanExecute(pageNumber - 1))
                {
                    command.Execute(pageNumber - 1);
                    UpdateToolbarPageNumber();
                }
            }
        }
    }

    /// <summary>
    /// Handles toolbar zoom in button click.
    /// </summary>
    private void OnToolbarZoomInClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.ZoomInCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>
    /// Handles toolbar zoom out button click.
    /// </summary>
    private void OnToolbarZoomOutClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.ZoomOutCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>
    /// Handles toolbar zoom combo box selection change.
    /// </summary>
    private void OnToolbarZoomSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem is ComboBoxItem item && item.Content is string zoomText)
        {
            if (ViewModel.ActiveTab?.ViewerViewModel != null)
            {
                // Parse zoom level and apply
                if (zoomText.EndsWith("%") && double.TryParse(zoomText.TrimEnd('%'), out double zoomPercent))
                {
                    var zoomLevel = zoomPercent / 100.0;
                    if (ViewModel.ActiveTab.ViewerViewModel.SetZoomCommand?.CanExecute(zoomLevel) == true)
                    {
                        ViewModel.ActiveTab.ViewerViewModel.SetZoomCommand.Execute(zoomLevel);
                    }
                }
                else if (zoomText == "Fit Width" && ViewModel.ActiveTab.ViewerViewModel.FitWidthCommand?.CanExecute(null) == true)
                {
                    ViewModel.ActiveTab.ViewerViewModel.FitWidthCommand.Execute(null);
                }
                else if (zoomText == "Fit Page" && ViewModel.ActiveTab.ViewerViewModel.FitPageCommand?.CanExecute(null) == true)
                {
                    ViewModel.ActiveTab.ViewerViewModel.FitPageCommand.Execute(null);
                }
            }
        }
    }

    /// <summary>
    /// Handles toolbar toggle thumbnails button click.
    /// </summary>
    private void OnToolbarToggleThumbnailsClick(object? sender, RoutedEventArgs e)
    {
        var button = sender as ToggleButton;
        if (button != null && ViewModel.ActiveTab?.ViewerViewModel?.ToggleThumbnailsCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>
    /// Handles toolbar toggle bookmarks button click.
    /// </summary>
    private void OnToolbarToggleBookmarksClick(object? sender, RoutedEventArgs e)
    {
        var button = sender as ToggleButton;
        if (button != null && ViewModel.ActiveTab?.ViewerViewModel?.ToggleBookmarksCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>
    /// Handles toolbar search button click.
    /// </summary>
    private void OnToolbarSearchClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveTab?.ViewerViewModel?.ShowSearchCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>
    /// Updates toolbar page number display based on active tab.
    /// </summary>
    private void UpdateToolbarPageNumber()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var pageNumberBox = this.FindControl<TextBox>("ToolbarPageNumberBox");
            var totalPagesText = this.FindControl<TextBlock>("ToolbarTotalPagesText");

            if (ViewModel.ActiveTab?.ViewerViewModel != null)
            {
                var currentPage = ViewModel.ActiveTab.ViewerViewModel.CurrentPageIndex + 1;
                var totalPages = ViewModel.ActiveTab.ViewerViewModel.PageCount;

                if (pageNumberBox != null)
                    pageNumberBox.Text = currentPage.ToString();

                if (totalPagesText != null)
                    totalPagesText.Text = $"of {totalPages}";

                // Update button states
                var prevButton = this.FindControl<Controls.LiquidButton>("ToolbarPreviousPageButton");
                var nextButton = this.FindControl<Controls.LiquidButton>("ToolbarNextPageButton");

                if (prevButton != null)
                    prevButton.IsEnabled = currentPage > 1;

                if (nextButton != null)
                    nextButton.IsEnabled = currentPage < totalPages;
            }
            else
            {
                // No active document
                if (pageNumberBox != null)
                    pageNumberBox.Text = "1";

                if (totalPagesText != null)
                    totalPagesText.Text = "of 0";

                var prevButton = this.FindControl<Controls.LiquidButton>("ToolbarPreviousPageButton");
                var nextButton = this.FindControl<Controls.LiquidButton>("ToolbarNextPageButton");

                if (prevButton != null)
                    prevButton.IsEnabled = false;

                if (nextButton != null)
                    nextButton.IsEnabled = false;
            }
        }, DispatcherPriority.Background);
    }

    #endregion
}

/// <summary>
/// Result of save confirmation dialog.
/// </summary>
public enum SaveConfirmationResult
{
    /// <summary>User chose to save the document.</summary>
    Save,

    /// <summary>User chose not to save the document.</summary>
    DontSave,

    /// <summary>User cancelled the operation.</summary>
    Cancel
}
