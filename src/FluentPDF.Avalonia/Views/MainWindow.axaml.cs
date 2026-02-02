using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentPDF.Avalonia.Helpers;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Views;

/// <summary>
/// Main application window with TabControl for multi-document interface.
/// Avalonia port of the WinUI 3 MainWindow with full feature parity.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow>? _logger;
    private Services.ILogBufferService? _logBuffer;
    private DebugConsoleManager? _debugConsoleManager;
    private MenuManager? _menuManager;
    private ToolbarManager? _toolbarManager;

    /// <summary>
    /// Gets the view model for this window.
    /// </summary>
    public MainViewModel ViewModel { get; }

    /// <summary>
    /// Gets the diagnostics panel view model.
    /// </summary>
    public ViewModels.DiagnosticsPanelViewModel DiagnosticsPanelViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow(MainViewModel viewModel, ILogger<MainWindow>? logger = null)
    {
        _logger = logger;
        _logger?.LogTrace("MainWindow constructor started");

        try
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DiagnosticsPanelViewModel = App.GetService<ViewModels.DiagnosticsPanelViewModel>();

            InitializeComponent();

            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = ViewModel;

            this.Loaded += OnWindowLoaded;

            _logger?.LogInformation("MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Fatal error in MainWindow constructor");
            throw;
        }
    }

    /// <summary>
    /// Handles window loaded event - performs deferred initialization.
    /// </summary>
    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        _logger?.LogInformation("Window loaded - starting deferred initialization");

        Task.Run(() =>
        {
            try
            {
                Dispatcher.UIThread.Post(() => SetupMenuHandlers());
                System.Threading.Thread.Sleep(100);

                Dispatcher.UIThread.Post(() => SetupKeyboardShortcuts());
                System.Threading.Thread.Sleep(100);

                Dispatcher.UIThread.Post(() => SetupViewModelEventHandlers());
                System.Threading.Thread.Sleep(100);

                Dispatcher.UIThread.Post(() => SetupDebugConsole(), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Deferred initialization sequence failed");
            }
        });
    }

    private void SetupMenuHandlers()
    {
        try
        {
            _menuManager = new MenuManager(
                this,
                ViewModel,
                OnOpenFileClickAsync,
                OnSaveClickAsync,
                OnSaveAsClickAsync,
                OnSettingsClickAsync,
                OnClearRecentFilesConfirmAsync,
                _logger);

            _menuManager.Initialize();
            _toolbarManager = new ToolbarManager(this, ViewModel, _logger);
            _toolbarManager.Initialize();

            _logger?.LogTrace("Menu and toolbar handlers configured successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to setup menu handlers");
        }
    }

    private void SetupKeyboardShortcuts()
    {
        try
        {
            this.KeyDown += async (sender, e) =>
            {
                var handled = await HandleKeyboardShortcutAsync(e.Key, e.KeyModifiers);
                if (handled) e.Handled = true;
            };

            _logger?.LogTrace("Keyboard shortcuts configured successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to setup keyboard shortcuts");
        }
    }

    private async Task<bool> HandleKeyboardShortcutAsync(Key key, KeyModifiers modifiers)
    {
        if (key == Key.Tab && modifiers == KeyModifiers.Control)
        {
            OnNextTab();
            return true;
        }

        if (key == Key.Tab && modifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            OnPreviousTab();
            return true;
        }

        if (key == Key.W && modifiers == KeyModifiers.Control)
        {
            await OnCloseCurrentTabAsync();
            return true;
        }

        if (key == Key.O && modifiers == KeyModifiers.Control)
        {
            await OnOpenFileClickAsync();
            return true;
        }

        if (key == Key.S && modifiers == KeyModifiers.Control)
        {
            await OnSaveClickAsync();
            return true;
        }

        if (key == Key.S && modifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            await OnSaveAsClickAsync();
            return true;
        }

        return false;
    }

    private void SetupViewModelEventHandlers()
    {
        try
        {
            ViewModel.Tabs.CollectionChanged += (s, evt) =>
            {
                Dispatcher.UIThread.Post(UpdateEmptyStateVisibility, DispatcherPriority.Background);
            };

            UpdateEmptyStateVisibility();

            ViewModel.PropertyChanged += (s, evt) =>
            {
                if (evt.PropertyName == nameof(ViewModel.ActiveTab))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            _menuManager?.UpdateMenuItemStates();
                            SubscribeToActiveTabChanges();
                        }
                        catch { }
                    }, DispatcherPriority.Background);
                }
            };

            _logger?.LogTrace("ViewModel event handlers registered successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register event handlers");
        }
    }

    private void SetupDebugConsole()
    {
        try
        {
            _logBuffer = App.Services.GetService(typeof(Services.ILogBufferService)) as Services.ILogBufferService;
            if (_logBuffer != null)
            {
                _debugConsoleManager = new DebugConsoleManager(this, _logger);
                _debugConsoleManager.Initialize(_logBuffer);
                _logger?.LogInformation("Debug console initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize debug console");
        }
    }

    private void UpdateEmptyStateVisibility()
    {
        _logger?.LogDebug("Updating empty state visibility. Tab count: {Count}", ViewModel.Tabs.Count);
    }

    private void SubscribeToActiveTabChanges()
    {
        if (ViewModel.ActiveTab != null)
        {
            ViewModel.ActiveTab.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TabViewModel.HasUnsavedChanges))
                {
                    Dispatcher.UIThread.Post(() => _menuManager?.UpdateMenuItemStates(), DispatcherPriority.Background);
                }
            };

            if (ViewModel.ActiveTab.ViewerViewModel != null)
            {
                ViewModel.ActiveTab.ViewerViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageNumber))
                    {
                        Dispatcher.UIThread.Post(() => _toolbarManager?.UpdatePageNumber(), DispatcherPriority.Background);
                    }
                };

                _toolbarManager?.UpdatePageNumber();
            }
        }
    }

    #region Tab Navigation

    private void OnNextTab()
    {
        if (ViewModel.Tabs.Count <= 1 || ViewModel.ActiveTab is null) return;

        var currentIndex = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab);
        var nextIndex = (currentIndex + 1) % ViewModel.Tabs.Count;
        ViewModel.ActiveTab = ViewModel.Tabs[nextIndex];

        _logger?.LogDebug("Switched to next tab: {Index}", nextIndex);
    }

    private void OnPreviousTab()
    {
        if (ViewModel.Tabs.Count <= 1 || ViewModel.ActiveTab is null) return;

        var currentIndex = ViewModel.Tabs.IndexOf(ViewModel.ActiveTab);
        var prevIndex = currentIndex - 1;
        if (prevIndex < 0) prevIndex = ViewModel.Tabs.Count - 1;

        ViewModel.ActiveTab = ViewModel.Tabs[prevIndex];

        _logger?.LogDebug("Switched to previous tab: {Index}", prevIndex);
    }

    private async Task OnCloseCurrentTabAsync()
    {
        if (ViewModel.ActiveTab != null)
        {
            await OnTabCloseRequestedAsync(ViewModel.ActiveTab);
        }
    }

    private async Task OnTabCloseRequestedAsync(TabViewModel tab)
    {
        if (tab == null) return;

        _logger?.LogInformation("Tab close requested for: {FilePath}", tab.FilePath);

        if (!tab.HasUnsavedChanges)
        {
            ViewModel.CloseTabCommand.Execute(tab);
            return;
        }

        var result = await DialogHelper.ShowSaveConfirmationDialogAsync(this, tab.FileName);

        switch (result)
        {
            case SaveConfirmationResult.Save:
                if (tab.ViewerViewModel?.SaveCommand is { } saveCommand && saveCommand.CanExecute(null))
                {
                    await saveCommand.ExecuteAsync(null);
                }
                ViewModel.CloseTabCommand.Execute(tab);
                break;

            case SaveConfirmationResult.DontSave:
                ViewModel.CloseTabCommand.Execute(tab);
                break;

            case SaveConfirmationResult.Cancel:
                _logger?.LogDebug("Tab close cancelled by user");
                break;
        }
    }

    #endregion

    #region File Operations

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
                    new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (files.Count == 0)
            {
                _logger?.LogInformation("File picker cancelled");
                return;
            }

            var filePath = files[0].Path.LocalPath;

            _logger?.LogInformation("File selected: {FilePath}", filePath);
            _logBuffer?.AddLog("Info", $"User opening file: {filePath}", "MainWindow");

            await ViewModel.OpenRecentFileCommand.ExecuteAsync(filePath);

            _logger?.LogDebug("OpenRecentFileCommand completed - {TabCount} tabs open", ViewModel.Tabs.Count);
            _logBuffer?.AddLog("Info", $"File opened successfully - Active tab: {ViewModel.ActiveTab?.FileName ?? "None"}", "MainWindow");

            _menuManager?.PopulateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open file");
            await DialogHelper.ShowErrorDialogAsync(this, "Error Opening File", $"Failed to open file:\n{ex.Message}");
        }
    }

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

    private async Task OnSettingsClickAsync()
    {
        try
        {
            _logger?.LogInformation("Opening settings dialog");
            await DialogHelper.ShowErrorDialogAsync(this, "Not Implemented", "Settings dialog is not yet implemented in Avalonia version.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open settings dialog");
        }
    }

    private async Task<bool> OnClearRecentFilesConfirmAsync()
    {
        return await DialogHelper.ShowConfirmationDialogAsync(
            this,
            "Clear Recent Files",
            "Are you sure you want to clear all recent files?");
    }

    #endregion
}
