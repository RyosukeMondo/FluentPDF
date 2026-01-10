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

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The main view model.</param>
    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        this.InitializeComponent();

        Title = "FluentPDF";

        // Set up keyboard shortcuts
        SetupKeyboardAccelerators();
    }

    /// <summary>
    /// Sets up keyboard accelerators for tab management.
    /// </summary>
    private void SetupKeyboardAccelerators()
    {
        // Ctrl+Tab: Next tab
        var nextTabAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Tab,
            Modifiers = VirtualKeyModifiers.Control
        };
        nextTabAccelerator.Invoked += OnNextTabAccelerator;
        this.KeyboardAccelerators.Add(nextTabAccelerator);

        // Ctrl+Shift+Tab: Previous tab
        var prevTabAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Tab,
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
        };
        prevTabAccelerator.Invoked += OnPreviousTabAccelerator;
        this.KeyboardAccelerators.Add(prevTabAccelerator);

        // Ctrl+W: Close current tab
        var closeTabAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.W,
            Modifiers = VirtualKeyModifiers.Control
        };
        closeTabAccelerator.Invoked += OnCloseTabAccelerator;
        this.KeyboardAccelerators.Add(closeTabAccelerator);

        // Ctrl+O: Open file
        var openFileAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.O,
            Modifiers = VirtualKeyModifiers.Control
        };
        openFileAccelerator.Invoked += OnOpenFileAccelerator;
        this.KeyboardAccelerators.Add(openFileAccelerator);
    }

    /// <summary>
    /// Handles Ctrl+Tab to switch to the next tab.
    /// </summary>
    private void OnNextTabAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.Tabs.Count <= 1)
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
        if (ViewModel.Tabs.Count <= 1)
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
}
