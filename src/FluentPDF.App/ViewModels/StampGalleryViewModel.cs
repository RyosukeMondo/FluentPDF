using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// View model for the stamp gallery dialog.
/// Manages stamp selection, configuration, and preview.
/// </summary>
public partial class StampGalleryViewModel : ObservableObject
{
    /// <summary>
    /// Gets the collection of built-in stamps.
    /// </summary>
    public List<StampItemViewModel> BuiltInStamps { get; }

    /// <summary>
    /// Gets or sets the currently selected stamp.
    /// </summary>
    [ObservableProperty]
    private StampItemViewModel? _selectedStamp;

    /// <summary>
    /// Gets or sets the custom stamp file name.
    /// </summary>
    [ObservableProperty]
    private string _customStampFileName = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a custom stamp is imported.
    /// </summary>
    [ObservableProperty]
    private bool _hasCustomStamp;

    /// <summary>
    /// Gets or sets the transform for the stamp preview (rotation).
    /// </summary>
    [ObservableProperty]
    private TransformGroup? _stampPreviewTransform;

    /// <summary>
    /// Initializes a new instance of the <see cref="StampGalleryViewModel"/> class.
    /// </summary>
    public StampGalleryViewModel()
    {
        BuiltInStamps = new List<StampItemViewModel>
        {
            CreateStampItem(StampType.Approved, "Approved", Color.FromArgb(255, 0, 128, 0)),
            CreateStampItem(StampType.Rejected, "Rejected", Color.FromArgb(255, 255, 0, 0)),
            CreateStampItem(StampType.Draft, "Draft", Color.FromArgb(255, 0, 0, 255)),
            CreateStampItem(StampType.Final, "Final", Color.FromArgb(255, 128, 0, 128)),
            CreateStampItem(StampType.Confidential, "Confidential", Color.FromArgb(255, 255, 0, 0)),
            CreateStampItem(StampType.ForReview, "For Review", Color.FromArgb(255, 255, 165, 0)),
            CreateStampItem(StampType.Copy, "Copy", Color.FromArgb(255, 128, 128, 128)),
        };

        // Select first stamp by default
        if (BuiltInStamps.Count > 0)
        {
            SelectedStamp = BuiltInStamps[0];
        }

        // Update preview transform when stamp properties change
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedStamp) && SelectedStamp != null)
            {
                UpdatePreviewTransform();
            }
        };
    }

    /// <summary>
    /// Imports a custom stamp image.
    /// </summary>
    [RelayCommand]
    private async Task ImportCustomStampAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");

            // Set picker context to the main window
            var window = Microsoft.UI.Xaml.Window.Current;
            if (window != null)
            {
                var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(window.AppWindow.Id);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                CustomStampFileName = file.Name;
                HasCustomStamp = true;

                if (SelectedStamp != null)
                {
                    SelectedStamp.CustomImagePath = file.Path;
                    SelectedStamp.StampType = StampType.Custom;
                }
            }
        }
        catch (Exception)
        {
            // Silently continue on error (debug logging removed in production)
        }
    }

    /// <summary>
    /// Gets the currently selected stamp model.
    /// </summary>
    /// <returns>The selected stamp model.</returns>
    public Stamp? GetSelectedStamp()
    {
        if (SelectedStamp == null)
        {
            return null;
        }

        var stamp = new Stamp
        {
            Type = SelectedStamp.StampType,
            Text = SelectedStamp.StampText,
            BackgroundColor = System.Drawing.Color.FromArgb(
                SelectedStamp.StampColor.A,
                SelectedStamp.StampColor.R,
                SelectedStamp.StampColor.G,
                SelectedStamp.StampColor.B),
            BorderColor = System.Drawing.Color.Black,
            BorderWidth = 2f,
            RotationAngle = SelectedStamp.RotationAngle,
            Opacity = SelectedStamp.Opacity,
            Width = SelectedStamp.Width,
            Height = SelectedStamp.Height,
            CustomImagePath = SelectedStamp.CustomImagePath
        };

        return stamp;
    }

    private StampItemViewModel CreateStampItem(StampType type, string text, Color color)
    {
        return new StampItemViewModel
        {
            StampType = type,
            StampText = text,
            StampColor = color,
            RotationAngle = -45f,
            Opacity = 0.5,
            Width = 100f,
            Height = 50f
        };
    }

    private void UpdatePreviewTransform()
    {
        if (SelectedStamp == null)
        {
            return;
        }

        var transformGroup = new TransformGroup();

        // Add rotation transform
        var rotateTransform = new RotateTransform
        {
            Angle = SelectedStamp.RotationAngle,
            CenterX = 75,
            CenterY = 37.5
        };
        transformGroup.Children.Add(rotateTransform);

        StampPreviewTransform = transformGroup;
    }
}

/// <summary>
/// Represents a stamp item for display in the gallery.
/// </summary>
public partial class StampItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the stamp type.
    /// </summary>
    [ObservableProperty]
    private StampType _stampType;

    /// <summary>
    /// Gets or sets the stamp display text.
    /// </summary>
    [ObservableProperty]
    private string _stampText = string.Empty;

    /// <summary>
    /// Gets or sets the stamp color.
    /// </summary>
    [ObservableProperty]
    private Color _stampColor = Colors.White;

    /// <summary>
    /// Gets or sets the rotation angle.
    /// </summary>
    [ObservableProperty]
    private float _rotationAngle = -45f;

    /// <summary>
    /// Gets or sets the opacity.
    /// </summary>
    [ObservableProperty]
    private double _opacity = 0.5;

    /// <summary>
    /// Gets or sets the width in PDF points.
    /// </summary>
    [ObservableProperty]
    private float _width = 100f;

    /// <summary>
    /// Gets or sets the height in PDF points.
    /// </summary>
    [ObservableProperty]
    private float _height = 50f;

    /// <summary>
    /// Gets or sets the custom image path (for custom stamps).
    /// </summary>
    [ObservableProperty]
    private string? _customImagePath;
}
