using FluentPDF.App.Helpers;
using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace FluentPDF.App.Views;

/// <summary>
/// PDF viewer page that displays PDF documents with navigation and zoom controls.
/// Implements data binding to PdfViewerViewModel following MVVM pattern.
/// </summary>
public sealed partial class PdfViewerPage : Page, IDisposable
{
    /// <summary>
    /// Gets the view model for this page.
    /// </summary>
    public PdfViewerViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerPage"/> class.
    /// </summary>
    public PdfViewerPage()
    {
        this.InitializeComponent();

        // Resolve ViewModel from DI container
        var app = (App)Application.Current;
        ViewModel = app.GetService<PdfViewerViewModel>();

        // Set DataContext for runtime binding (x:Bind doesn't need this, but good practice)
        this.DataContext = ViewModel;

        // Hook up keyboard handlers for form field navigation
        this.KeyDown += OnPageKeyDown;

        // Hook up event handler for search panel visibility changes
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Handles ViewModel property changes to manage search TextBox focus and update highlights.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsSearchPanelVisible) && ViewModel.IsSearchPanelVisible)
        {
            // Focus the search TextBox when panel becomes visible
            _ = SearchTextBox.DispatcherQueue.TryEnqueue(() =>
            {
                SearchTextBox.Focus(FocusState.Programmatic);
                SearchTextBox.SelectAll();
            });
        }
        else if (e.PropertyName == nameof(ViewModel.SearchMatches) ||
                 e.PropertyName == nameof(ViewModel.CurrentMatchIndex) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageNumber) ||
                 e.PropertyName == nameof(ViewModel.ZoomLevel) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageImage) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageHeight))
        {
            // Update search highlights when matches, page, zoom, or dimensions change
            _ = SearchHighlightCanvas.DispatcherQueue.TryEnqueue(() =>
            {
                UpdateSearchHighlights();
            });
        }
    }

    /// <summary>
    /// Handles navigation to the DOCX conversion page.
    /// </summary>
    private void OnConvertDocxClick(object sender, RoutedEventArgs e)
    {
        var navigationService = ((App)Application.Current).GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ConversionPage));
    }

    /// <summary>
    /// Handles keyboard events for form field navigation.
    /// Tab/Shift+Tab navigate between form fields in tab order.
    /// </summary>
    private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Only handle Tab key when form fields are present
        if (!ViewModel.FormFieldViewModel.HasFormFields)
        {
            return;
        }

        if (e.Key == VirtualKey.Tab)
        {
            // Check if Shift is pressed
            var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (shiftPressed)
            {
                // Shift+Tab: Navigate to previous field
                ViewModel.FormFieldViewModel.FocusPreviousFieldCommand.Execute(null);
            }
            else
            {
                // Tab: Navigate to next field
                ViewModel.FormFieldViewModel.FocusNextFieldCommand.Execute(null);
            }

            // Mark event as handled to prevent default Tab behavior
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles scroll viewer view changes (zoom/scroll).
    /// Updates form field positions when the view changes.
    /// </summary>
    private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Form field positions are bound to ZoomLevel which is already updated
        // The FormFieldControl will automatically recalculate positions based on zoom
        // This handler is primarily for future enhancements if needed
    }

    /// <summary>
    /// Handles "Go to field" requests from the validation error panel.
    /// Focuses the specified form field.
    /// </summary>
    private void OnGoToFieldRequested(object sender, string fieldName)
    {
        ViewModel.FormFieldViewModel.FocusFieldByNameCommand.Execute(fieldName);
    }

    /// <summary>
    /// Handles Ctrl+F keyboard accelerator to toggle search panel.
    /// </summary>
    private void OnSearchKeyboardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ToggleSearchPanelCommand.Execute(null);
        args.Handled = true;
    }

    /// <summary>
    /// Handles Escape key in search TextBox to close search panel.
    /// </summary>
    private void OnSearchEscapePressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.IsSearchPanelVisible)
        {
            ViewModel.ToggleSearchPanelCommand.Execute(null);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Updates the search highlight overlays on the current page.
    /// Renders rectangles for all search matches on the current page.
    /// </summary>
    private void UpdateSearchHighlights()
    {
        // Clear existing highlights
        SearchHighlightCanvas.Children.Clear();

        // If we don't have valid dimensions or no matches, nothing to render
        if (ViewModel.CurrentPageHeight == 0 || ViewModel.SearchMatches.Count == 0 || ViewModel.CurrentPageImage == null)
        {
            return;
        }

        // Get matches for the current page (1-based page number in ViewModel, 0-based in SearchMatch)
        var currentPageMatches = ViewModel.SearchMatches
            .Where(m => m.PageNumber == ViewModel.CurrentPageNumber - 1)
            .ToList();

        if (currentPageMatches.Count == 0)
        {
            return;
        }

        // Determine which match is the current match
        int currentMatchIndexOnPage = -1;
        if (ViewModel.CurrentMatchIndex >= 0 && ViewModel.CurrentMatchIndex < ViewModel.SearchMatches.Count)
        {
            var currentMatch = ViewModel.SearchMatches[ViewModel.CurrentMatchIndex];
            if (currentMatch.PageNumber == ViewModel.CurrentPageNumber - 1)
            {
                currentMatchIndexOnPage = currentPageMatches.IndexOf(currentMatch);
            }
        }

        // Render highlight rectangles for each match
        for (int i = 0; i < currentPageMatches.Count; i++)
        {
            var match = currentPageMatches[i];
            var screenRect = CoordinateTransformHelper.TransformPdfToScreen(
                match.BoundingBox,
                ViewModel.CurrentPageHeight,
                ViewModel.ZoomLevel);

            // Create highlight rectangle
            var rect = new Rectangle
            {
                Width = screenRect.Width,
                Height = screenRect.Height,
                Fill = i == currentMatchIndexOnPage
                    ? new SolidColorBrush(Colors.Orange) { Opacity = 0.5 }  // Current match: orange
                    : new SolidColorBrush(Colors.Yellow) { Opacity = 0.3 }, // Other matches: yellow
                Stroke = i == currentMatchIndexOnPage
                    ? new SolidColorBrush(Colors.DarkOrange)
                    : new SolidColorBrush(Colors.Gold),
                StrokeThickness = 1
            };

            // Position the rectangle on the canvas
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(rect, screenRect.X);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(rect, screenRect.Y);

            // Add to canvas
            SearchHighlightCanvas.Children.Add(rect);
        }

        // Update canvas size to match the image
        if (PdfPageImage.ActualWidth > 0 && PdfPageImage.ActualHeight > 0)
        {
            SearchHighlightCanvas.Width = PdfPageImage.ActualWidth;
            SearchHighlightCanvas.Height = PdfPageImage.ActualHeight;
        }
    }

    /// <summary>
    /// Disposes resources used by the page.
    /// </summary>
    public void Dispose()
    {
        this.KeyDown -= OnPageKeyDown;
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.Dispose();
        }
    }
}
