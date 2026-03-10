using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Avalonia.Services;
using FluentPDF.Core.Models;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

/// <summary>
/// Preview rendering and property wrappers for WatermarkViewModel.
/// </summary>
public partial class WatermarkViewModel
{
    /// <summary>
    /// Gets or sets the preview image bytes (PNG format).
    /// </summary>
    [ObservableProperty]
    private byte[]? _previewImage;

    /// <summary>Gets a value indicating whether a preview is available.</summary>
    public bool HasPreview => PreviewImage != null && PreviewImage.Length > 0 && !IsLoading;

    /// <summary>Gets a value indicating whether custom position is selected.</summary>
    public bool IsCustomPosition => SelectedPosition == WatermarkPosition.Custom;

    #region Config Property Wrappers

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

    #endregion

    #region Config Change Handlers

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

    #endregion

    #region Preview & Image Selection

    /// <summary>
    /// Opens a file picker to select an image for the watermark.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectImage))]
    private async Task SelectImageAsync()
    {
        _logger.LogInformation("SelectImage command invoked");

        try
        {
            var fileDialogService = App.GetService<IFileDialogService>();

            var filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "Image Files",
                    Extensions = new List<string> { "png", "jpg", "jpeg", "bmp" }
                },
                new FileDialogFilter
                {
                    Name = "All Files",
                    Extensions = new List<string> { "*" }
                }
            };

            var filePath = await fileDialogService.OpenFileAsync("Select Watermark Image", filters);

            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation("Image file picker cancelled");
                return;
            }

            _logger.LogInformation("Image file selected: {FilePath}", filePath);

            ImageConfig.ImagePath = filePath;
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
    /// Sets rotation to diagonal (45 degrees) and regenerates preview.
    /// </summary>
    public async Task SetDiagonalRotation()
    {
        _logger.LogInformation("Setting diagonal rotation");
        Rotation = 45f; // Uses existing Rotation property
        await GeneratePreviewAsync();
    }

    #endregion
}
