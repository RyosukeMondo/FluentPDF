using FluentPDF.App.ViewModels;
using FluentPDF.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Controls;

/// <summary>
/// Wrapper control for PdfViewerPage that accepts a ViewModel via dependency property.
/// Used in TabView to host PDF viewer with specific ViewModel.
/// </summary>
public sealed partial class PdfViewerControl : UserControl
{
    /// <summary>
    /// Dependency property for the PdfViewerViewModel.
    /// </summary>
    public static readonly DependencyProperty ViewerViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewerViewModel),
            typeof(PdfViewerViewModel),
            typeof(PdfViewerControl),
            new PropertyMetadata(null, OnViewerViewModelChanged));

    private PdfViewerPage? _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerControl"/> class.
    /// </summary>
    public PdfViewerControl()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the PdfViewerViewModel for this control.
    /// </summary>
    public PdfViewerViewModel? ViewerViewModel
    {
        get => (PdfViewerViewModel?)GetValue(ViewerViewModelProperty);
        set => SetValue(ViewerViewModelProperty, value);
    }

    /// <summary>
    /// Handles the Loaded event to initialize the PdfViewerPage.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewerViewModel != null && _page == null)
        {
            CreatePage();
        }
    }

    /// <summary>
    /// Handles changes to the ViewerViewModel property.
    /// </summary>
    private static void OnViewerViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PdfViewerControl)d;
        if (e.NewValue is PdfViewerViewModel)
        {
            control.CreatePage();
        }
    }

    /// <summary>
    /// Creates and hosts the PdfViewerPage with the provided ViewModel.
    /// </summary>
    private void CreatePage()
    {
        if (ViewerViewModel == null)
        {
            return;
        }

        // Dispose existing page if any
        if (_page != null)
        {
            ContentGrid.Children.Clear();
            _page.Dispose();
            _page = null;
        }

        // Create new page with the ViewModel
        _page = new PdfViewerPage(ViewerViewModel);
        ContentGrid.Children.Add(_page);
    }
}
