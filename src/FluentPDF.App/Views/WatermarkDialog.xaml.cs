using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using System.Drawing;

namespace FluentPDF.App.Views;

/// <summary>
/// Dialog for configuring and applying watermarks to PDF documents.
/// Provides UI for text and image watermark creation with live preview.
/// </summary>
public sealed partial class WatermarkDialog : ContentDialog
{
    private readonly WatermarkViewModel _viewModel;

    /// <summary>
    /// Gets the ViewModel for this dialog.
    /// </summary>
    public WatermarkViewModel ViewModel => _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatermarkDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The watermark view model.</param>
    private WatermarkDialog(WatermarkViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        this.InitializeComponent();

        // Set DataContext for bindings
        this.DataContext = _viewModel;

        // Initialize color picker with current text color
        TextColorPicker.Color = Windows.UI.Color.FromArgb(
            255,
            _viewModel.TextConfig.Color.R,
            _viewModel.TextConfig.Color.G,
            _viewModel.TextConfig.Color.B);

        // Subscribe to preview image updates
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Handle dialog button clicks
        this.PrimaryButtonClick += OnPrimaryButtonClick;
        this.CloseButtonClick += OnCloseButtonClick;

        Log.Debug("WatermarkDialog initialized");
    }

    /// <summary>
    /// Shows the watermark dialog for the specified document.
    /// </summary>
    /// <param name="xamlRoot">The XamlRoot for proper dialog hosting.</param>
    /// <param name="viewModel">The watermark view model.</param>
    /// <param name="document">The PDF document to apply watermark to.</param>
    /// <param name="currentPageNumber">The current page number (1-based).</param>
    /// <param name="totalPages">Total number of pages in the document.</param>
    /// <returns>True if watermark was applied, false if cancelled.</returns>
    public static async Task<bool> ShowAsync(
        XamlRoot xamlRoot,
        WatermarkViewModel viewModel,
        PdfDocument document,
        int currentPageNumber,
        int totalPages)
    {
        // Initialize the view model with document context
        viewModel.InitializeCommand.Execute((document, currentPageNumber, totalPages));

        var dialog = new WatermarkDialog(viewModel)
        {
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();

        return viewModel.DialogApplied;
    }

    /// <summary>
    /// Handles changes to the text color picker.
    /// </summary>
    private void OnTextColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        _viewModel.TextConfig.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        // Trigger preview update
        _ = _viewModel.GeneratePreviewCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Handles ViewModel property changes to update the preview image.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WatermarkViewModel.PreviewImage))
        {
            UpdatePreviewImage();
        }
    }

    /// <summary>
    /// Updates the preview image display from the ViewModel's byte array.
    /// </summary>
    private async void UpdatePreviewImage()
    {
        if (_viewModel.PreviewImage == null || _viewModel.PreviewImage.Length == 0)
        {
            PreviewImage.Source = null;
            return;
        }

        try
        {
            using var memoryStream = new MemoryStream(_viewModel.PreviewImage);
            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
            PreviewImage.Source = bitmapImage;

            Log.Debug("Preview image updated successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update preview image");
            PreviewImage.Source = null;
        }
    }

    /// <summary>
    /// Handles the primary button (Apply) click.
    /// </summary>
    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Get deferral to allow async operation
        var deferral = args.GetDeferral();

        try
        {
            // Apply the watermark
            await _viewModel.ApplyCommand.ExecuteAsync(null);

            Log.Information("Watermark applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply watermark");
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// Handles the close button (Cancel) click.
    /// </summary>
    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _viewModel.DialogApplied = false;
        Log.Debug("Watermark dialog cancelled");
    }
}
