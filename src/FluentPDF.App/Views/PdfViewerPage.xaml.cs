using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
    /// Disposes resources used by the page.
    /// </summary>
    public void Dispose()
    {
        this.KeyDown -= OnPageKeyDown;
        ViewModel?.Dispose();
    }
}
