using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    /// Disposes resources used by the page.
    /// </summary>
    public void Dispose()
    {
        ViewModel?.Dispose();
    }
}
