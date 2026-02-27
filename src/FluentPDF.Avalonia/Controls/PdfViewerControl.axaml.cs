using Avalonia;
using Avalonia.Controls;
using FluentPDF.Core.ViewModels;
using FluentPDF.Avalonia.Views;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Control that hosts the PDF viewer page.
/// Acts as a container that creates and manages the PdfViewerPage instance.
/// </summary>
public partial class PdfViewerControl : UserControl
{
    /// <summary>
    /// Defines the ViewerViewModel property for binding the PDF viewer view model.
    /// </summary>
    public static readonly StyledProperty<PdfViewerViewModel?> ViewerViewModelProperty =
        AvaloniaProperty.Register<PdfViewerControl, PdfViewerViewModel?>(nameof(ViewerViewModel));

    private PdfViewerPage? _viewerPage;

    /// <summary>
    /// Gets the active PdfViewerPage instance for API automation.
    /// </summary>
    public PdfViewerPage? ActiveViewerPage => _viewerPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerControl"/> class.
    /// </summary>
    public PdfViewerControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the PDF viewer view model.
    /// When set, creates a new PdfViewerPage instance.
    /// </summary>
    public PdfViewerViewModel? ViewerViewModel
    {
        get => GetValue(ViewerViewModelProperty);
        set => SetValue(ViewerViewModelProperty, value);
    }

    /// <summary>
    /// Called when a property value changes.
    /// Creates PdfViewerPage when ViewerViewModel is set.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ViewerViewModelProperty)
        {
            if (change.NewValue is PdfViewerViewModel viewModel)
            {
                // Create PdfViewerPage and set as content
                _viewerPage = new PdfViewerPage(viewModel);
                ContentGrid.Children.Clear();
                ContentGrid.Children.Add(_viewerPage);
            }
            else
            {
                // Clear content if viewModel is null
                ContentGrid.Children.Clear();
                _viewerPage = null;
            }
        }
    }
}
