using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Windows.Storage.Pickers;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for PDF watermark operations.
/// Manages watermark configuration, preview, and application state.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class WatermarkViewModel : ObservableObject
{
    private readonly IWatermarkService _watermarkService;
    private readonly ILogger<WatermarkViewModel> _logger;
    private PdfDocument? _currentDocument;
    private int _currentPageNumber;
    private int _totalPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatermarkViewModel"/> class.
    /// </summary>
    /// <param name="watermarkService">Service for watermark operations.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public WatermarkViewModel(
        IWatermarkService watermarkService,
        ILogger<WatermarkViewModel> logger)
    {
        _watermarkService = watermarkService ?? throw new ArgumentNullException(nameof(watermarkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize with default configurations
        TextConfig = new TextWatermarkConfig
        {
            Text = "",
            FontFamily = "Arial",
            FontSize = 72f,
            Color = Color.Gray,
            Opacity = 0.5f,
            RotationDegrees = 0f,
            Position = WatermarkPosition.Center,
            BehindContent = true
        };

        ImageConfig = new ImageWatermarkConfig
        {
            ImagePath = "",
            Scale = 1.0f,
            Opacity = 0.5f,
            RotationDegrees = 0f,
            Position = WatermarkPosition.Center,
            BehindContent = true
        };

        _logger.LogInformation("WatermarkViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the text watermark configuration.
    /// </summary>
    [ObservableProperty]
    private TextWatermarkConfig _textConfig;

    /// <summary>
    /// Gets or sets the image watermark configuration.
    /// </summary>
    [ObservableProperty]
    private ImageWatermarkConfig _imageConfig;

    /// <summary>
    /// Gets or sets the type of watermark (Text or Image).
    /// </summary>
    [ObservableProperty]
    private WatermarkType _selectedType = WatermarkType.Text;

    /// <summary>
    /// Gets or sets the page range type for watermark application.
    /// </summary>
    [ObservableProperty]
    private PageRangeType _pageRangeType = PageRangeType.All;

    /// <summary>
    /// Gets or sets the custom page range string (e.g., "1-5, 10, 15-20").
    /// </summary>
    [ObservableProperty]
    private string _customPageRange = string.Empty;

    /// <summary>
    /// Gets or sets the preview image bytes (PNG format).
    /// </summary>
    [ObservableProperty]
    private byte[]? _previewImage;

    /// <summary>
    /// Gets or sets a value indicating whether a watermark operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved watermark changes.
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Gets or sets the error message for page range validation.
    /// </summary>
    [ObservableProperty]
    private string? _pageRangeError;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog was applied (vs canceled).
    /// </summary>
    [ObservableProperty]
    private bool _dialogApplied;

    /// <summary>
    /// Gets a value indicating whether text mode is selected.
    /// </summary>
    public bool IsTextMode => SelectedType == WatermarkType.Text;

    /// <summary>
    /// Gets a value indicating whether image mode is selected.
    /// </summary>
    public bool IsImageMode => SelectedType == WatermarkType.Image;

    /// <summary>
    /// Gets a value indicating whether an image has been selected.
    /// </summary>
    public bool HasImageSelected => !string.IsNullOrWhiteSpace(ImageConfig.ImagePath);

    /// <summary>
    /// Gets a value indicating whether a preview is available.
    /// </summary>
    public bool HasPreview => PreviewImage != null && PreviewImage.Length > 0 && !IsLoading;

    /// <summary>
    /// Gets a value indicating whether there is a page range error.
    /// </summary>
    public bool HasPageRangeError => !string.IsNullOrWhiteSpace(PageRangeError);

    /// <summary>
    /// Gets a value indicating whether custom position is selected.
    /// </summary>
    public bool IsCustomPosition => SelectedPosition == WatermarkPosition.Custom;

    /// <summary>
    /// Gets a value indicating whether custom page range is selected.
    /// </summary>
    public bool IsCustomPageRange => PageRangeType == PageRangeType.Custom;

    /// <summary>
    /// Gets or sets the opacity as a percentage (0-100).
    /// </summary>
    public float OpacityPercentage
    {
        get => (SelectedType == WatermarkType.Text ? TextConfig.Opacity : ImageConfig.Opacity) * 100f;
        set
        {
            var opacity = value / 100f;
            if (SelectedType == WatermarkType.Text)
            {
                TextConfig.Opacity = opacity;
                OnPropertyChanged(nameof(TextConfig));
            }
            else
            {
                ImageConfig.Opacity = opacity;
                OnPropertyChanged(nameof(ImageConfig));
            }
        }
    }

    /// <summary>
    /// Gets or sets the rotation in degrees.
    /// </summary>
    public float Rotation
    {
        get => SelectedType == WatermarkType.Text ? TextConfig.RotationDegrees : ImageConfig.RotationDegrees;
        set
        {
            if (SelectedType == WatermarkType.Text)
            {
                TextConfig.RotationDegrees = value;
                OnPropertyChanged(nameof(TextConfig));
            }
            else
            {
                ImageConfig.RotationDegrees = value;
                OnPropertyChanged(nameof(ImageConfig));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether watermark is behind content.
    /// </summary>
    public bool BehindContent
    {
        get => SelectedType == WatermarkType.Text ? TextConfig.BehindContent : ImageConfig.BehindContent;
        set
        {
            if (SelectedType == WatermarkType.Text)
            {
                TextConfig.BehindContent = value;
                OnPropertyChanged(nameof(TextConfig));
            }
            else
            {
                ImageConfig.BehindContent = value;
                OnPropertyChanged(nameof(ImageConfig));
            }
        }
    }

    /// <summary>
    /// Gets or sets the image scale as a percentage (10-200).
    /// </summary>
    public float ImageScalePercentage
    {
        get => ImageConfig.Scale * 100f;
        set
        {
            ImageConfig.Scale = value / 100f;
            OnPropertyChanged(nameof(ImageConfig));
        }
    }

    /// <summary>
    /// Gets or sets the selected position preset.
    /// </summary>
    public WatermarkPosition SelectedPosition
    {
        get => SelectedType == WatermarkType.Text ? TextConfig.Position : ImageConfig.Position;
        set
        {
            if (SelectedType == WatermarkType.Text)
            {
                TextConfig.Position = value;
                OnPropertyChanged(nameof(TextConfig));
            }
            else
            {
                ImageConfig.Position = value;
                OnPropertyChanged(nameof(ImageConfig));
            }
            OnPropertyChanged(nameof(IsCustomPosition));
        }
    }

    /// <summary>
    /// Gets or sets the custom X position.
    /// </summary>
    public float CustomX
    {
        get => SelectedType == WatermarkType.Text ? TextConfig.CustomX : ImageConfig.CustomX;
        set
        {
            if (SelectedType == WatermarkType.Text)
            {
                TextConfig.CustomX = value;
                OnPropertyChanged(nameof(TextConfig));
            }
            else
            {
                ImageConfig.CustomX = value;
                OnPropertyChanged(nameof(ImageConfig));
            }
        }
    }

    /// <summary>
    /// Gets or sets the custom Y position.
    /// </summary>
    public float CustomY
    {
        get => SelectedType == WatermarkType.Text ? TextConfig.CustomY : ImageConfig.CustomY;
        set
        {
            if (SelectedType == WatermarkType.Text)
            {
                TextConfig.CustomY = value;
                OnPropertyChanged(nameof(TextConfig));
            }
            else
            {
                ImageConfig.CustomY = value;
                OnPropertyChanged(nameof(ImageConfig));
            }
        }
    }

    /// <summary>
    /// Gets the list of available font families.
    /// </summary>
    public List<string> AvailableFonts { get; } = new()
    {
        "Arial",
        "Calibri",
        "Courier New",
        "Georgia",
        "Times New Roman",
        "Trebuchet MS",
        "Verdana"
    };

    /// <summary>
    /// Gets the list of available position presets.
    /// </summary>
    public List<WatermarkPosition> AvailablePositions { get; } = new()
    {
        WatermarkPosition.Center,
        WatermarkPosition.TopLeft,
        WatermarkPosition.TopRight,
        WatermarkPosition.BottomLeft,
        WatermarkPosition.BottomRight,
        WatermarkPosition.Custom
    };

    /// <summary>
    /// Gets the list of available page range types.
    /// </summary>
    public List<PageRangeType> AvailablePageRangeTypes { get; } = new()
    {
        PageRangeType.All,
        PageRangeType.CurrentPage,
        PageRangeType.OddPages,
        PageRangeType.EvenPages,
        PageRangeType.Custom
    };

    /// <summary>
    /// Triggered when configuration changes to update preview.
    /// </summary>
    partial void OnTextConfigChanged(TextWatermarkConfig value)
    {
        _logger.LogDebug("TextConfig changed");
        if (SelectedType == WatermarkType.Text)
        {
            _ = GeneratePreviewAsync();
        }
    }

    /// <summary>
    /// Triggered when configuration changes to update preview.
    /// </summary>
    partial void OnImageConfigChanged(ImageWatermarkConfig value)
    {
        _logger.LogDebug("ImageConfig changed");
        if (SelectedType == WatermarkType.Image)
        {
            _ = GeneratePreviewAsync();
        }
    }

    /// <summary>
    /// Triggered when watermark type changes to update preview.
    /// </summary>
    partial void OnSelectedTypeChanged(WatermarkType value)
    {
        _logger.LogInformation("SelectedType changed to {Type}", value);
        OnPropertyChanged(nameof(IsTextMode));
        OnPropertyChanged(nameof(IsImageMode));
        OnPropertyChanged(nameof(OpacityPercentage));
        OnPropertyChanged(nameof(Rotation));
        OnPropertyChanged(nameof(BehindContent));
        OnPropertyChanged(nameof(SelectedPosition));
        OnPropertyChanged(nameof(CustomX));
        OnPropertyChanged(nameof(CustomY));
        _ = GeneratePreviewAsync();
    }

    /// <summary>
    /// Opens a file picker to select an image for the watermark.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectImage))]
    private async Task SelectImageAsync()
    {
        _logger.LogInformation("SelectImage command invoked");

        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                _logger.LogInformation("Image file picker cancelled");
                return;
            }

            _logger.LogInformation("Image file selected: {FilePath}", file.Path);

            ImageConfig.ImagePath = file.Path;
            OnPropertyChanged(nameof(ImageConfig));
            OnPropertyChanged(nameof(HasImageSelected));
            await GeneratePreviewAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select image");
        }
    }

    private bool CanSelectImage() => !IsLoading;

    /// <summary>
    /// Generates a preview of the watermark on the current page.
    /// </summary>
    [RelayCommand]
    private async Task GeneratePreviewAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot generate preview: no document loaded");
            return;
        }

        try
        {
            IsLoading = true;

            var pageIndex = _currentPageNumber - 1; // Convert to 0-based

            Result<byte[]> result = SelectedType == WatermarkType.Text
                ? await _watermarkService.GeneratePreviewAsync(_currentDocument, pageIndex, TextConfig, null)
                : await _watermarkService.GeneratePreviewAsync(_currentDocument, pageIndex, null, ImageConfig);

            if (result.IsSuccess)
            {
                PreviewImage = result.Value;
                _logger.LogInformation("Preview generated successfully");
            }
            else
            {
                _logger.LogError("Failed to generate preview: {Errors}", result.Errors);
                PreviewImage = null;
            }

            OnPropertyChanged(nameof(HasPreview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating preview");
            PreviewImage = null;
            OnPropertyChanged(nameof(HasPreview));
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasPreview));
        }
    }

    /// <summary>
    /// Applies the watermark to the selected pages.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot apply watermark: no document loaded");
            return;
        }

        // Validate page range
        if (!ValidatePageRange())
        {
            _logger.LogWarning("Invalid page range: {Error}", PageRangeError);
            return;
        }

        try
        {
            IsLoading = true;

            var pageRange = BuildPageRange();

            _logger.LogInformation(
                "Applying {Type} watermark to {RangeType}",
                SelectedType, pageRange.Type);

            Result result = SelectedType == WatermarkType.Text
                ? await _watermarkService.ApplyTextWatermarkAsync(_currentDocument, TextConfig, pageRange)
                : await _watermarkService.ApplyImageWatermarkAsync(_currentDocument, ImageConfig, pageRange);

            if (result.IsSuccess)
            {
                HasUnsavedChanges = true;
                DialogApplied = true;
                _logger.LogInformation("Watermark applied successfully");
            }
            else
            {
                _logger.LogError("Failed to apply watermark: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while applying watermark");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanApply()
    {
        if (_currentDocument == null || IsLoading)
        {
            return false;
        }

        if (SelectedType == WatermarkType.Text)
        {
            return !string.IsNullOrWhiteSpace(TextConfig.Text);
        }

        return !string.IsNullOrWhiteSpace(ImageConfig.ImagePath);
    }

    /// <summary>
    /// Removes watermarks from the selected pages.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private async Task RemoveAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot remove watermarks: no document loaded");
            return;
        }

        // Validate page range
        if (!ValidatePageRange())
        {
            _logger.LogWarning("Invalid page range: {Error}", PageRangeError);
            return;
        }

        try
        {
            IsLoading = true;

            var pageRange = BuildPageRange();

            _logger.LogInformation("Removing watermarks from {RangeType}", pageRange.Type);

            var result = await _watermarkService.RemoveWatermarksAsync(_currentDocument, pageRange);

            if (result.IsSuccess)
            {
                HasUnsavedChanges = true;
                _logger.LogInformation("Watermarks removed successfully");
            }
            else
            {
                _logger.LogError("Failed to remove watermarks: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while removing watermarks");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRemove() => _currentDocument != null && !IsLoading;

    /// <summary>
    /// Applies a preset watermark configuration.
    /// </summary>
    /// <param name="presetName">Name of the preset (CONFIDENTIAL, DRAFT, COPY, APPROVED).</param>
    [RelayCommand]
    private async Task ApplyPresetAsync(string presetName)
    {
        _logger.LogInformation("Applying preset: {Preset}", presetName);

        SelectedType = WatermarkType.Text;

        switch (presetName.ToUpperInvariant())
        {
            case "CONFIDENTIAL":
                TextConfig.Text = "CONFIDENTIAL";
                TextConfig.Color = Color.Red;
                TextConfig.FontSize = 72f;
                TextConfig.RotationDegrees = 45f;
                TextConfig.Opacity = 0.3f;
                TextConfig.Position = WatermarkPosition.Center;
                break;

            case "DRAFT":
                TextConfig.Text = "DRAFT";
                TextConfig.Color = Color.Gray;
                TextConfig.FontSize = 96f;
                TextConfig.RotationDegrees = 45f;
                TextConfig.Opacity = 0.2f;
                TextConfig.Position = WatermarkPosition.Center;
                break;

            case "COPY":
                TextConfig.Text = "COPY";
                TextConfig.Color = Color.Blue;
                TextConfig.FontSize = 72f;
                TextConfig.RotationDegrees = 45f;
                TextConfig.Opacity = 0.3f;
                TextConfig.Position = WatermarkPosition.Center;
                break;

            case "APPROVED":
                TextConfig.Text = "APPROVED";
                TextConfig.Color = Color.Green;
                TextConfig.FontSize = 72f;
                TextConfig.RotationDegrees = 0f;
                TextConfig.Opacity = 0.4f;
                TextConfig.Position = WatermarkPosition.Center;
                break;

            default:
                _logger.LogWarning("Unknown preset: {Preset}", presetName);
                return;
        }

        OnPropertyChanged(nameof(TextConfig));
        await GeneratePreviewAsync();
    }

    /// <summary>
    /// Sets rotation to diagonal (45 degrees).
    /// </summary>
    [RelayCommand]
    private async Task SetDiagonalAsync()
    {
        _logger.LogInformation("Setting diagonal rotation");

        if (SelectedType == WatermarkType.Text)
        {
            TextConfig.RotationDegrees = 45f;
            OnPropertyChanged(nameof(TextConfig));
        }
        else
        {
            ImageConfig.RotationDegrees = 45f;
            OnPropertyChanged(nameof(ImageConfig));
        }

        await GeneratePreviewAsync();
    }

    /// <summary>
    /// Loads the watermark dialog for the specified document.
    /// </summary>
    /// <param name="parameters">Tuple containing the document, current page number, and total pages.</param>
    [RelayCommand]
    private void Initialize((PdfDocument document, int pageNumber, int totalPages) parameters)
    {
        _currentDocument = parameters.document;
        _currentPageNumber = parameters.pageNumber;
        _totalPages = parameters.totalPages;
        DialogApplied = false;
        PageRangeError = null;

        _logger.LogInformation(
            "WatermarkViewModel initialized. Document={FilePath}, Page={Page}/{Total}",
            _currentDocument?.FilePath, _currentPageNumber, _totalPages);

        _ = GeneratePreviewAsync();
    }

    /// <summary>
    /// Validates the page range based on current settings.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    private bool ValidatePageRange()
    {
        PageRangeError = null;

        if (PageRangeType == PageRangeType.Custom)
        {
            if (string.IsNullOrWhiteSpace(CustomPageRange))
            {
                PageRangeError = "Custom page range cannot be empty";
                return false;
            }

            try
            {
                var pageRange = WatermarkPageRange.Parse(CustomPageRange);
                var pages = pageRange.GetPages(_totalPages);

                if (pages.Length == 0)
                {
                    PageRangeError = $"No valid pages in range (document has {_totalPages} pages)";
                    return false;
                }
            }
            catch (ArgumentException ex)
            {
                PageRangeError = ex.Message;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Builds a WatermarkPageRange based on current settings.
    /// </summary>
    /// <returns>The page range for watermark application.</returns>
    private WatermarkPageRange BuildPageRange()
    {
        return PageRangeType switch
        {
            PageRangeType.All => WatermarkPageRange.All,
            PageRangeType.CurrentPage => WatermarkPageRange.Current(_currentPageNumber),
            PageRangeType.OddPages => WatermarkPageRange.OddPages,
            PageRangeType.EvenPages => WatermarkPageRange.EvenPages,
            PageRangeType.Custom => WatermarkPageRange.Parse(CustomPageRange),
            _ => WatermarkPageRange.All
        };
    }
}

/// <summary>
/// Defines the type of watermark (Text or Image).
/// </summary>
public enum WatermarkType
{
    /// <summary>
    /// Text-based watermark.
    /// </summary>
    Text = 0,

    /// <summary>
    /// Image-based watermark.
    /// </summary>
    Image = 1
}
