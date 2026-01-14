using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage.Pickers;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for PDF image insertion operations.
/// Manages image insertion, selection, and manipulation state.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class ImageInsertionViewModel : ObservableObject
{
    private readonly IImageInsertionService _imageInsertionService;
    private readonly ILogger<ImageInsertionViewModel> _logger;
    private PdfDocument? _currentDocument;
    private int _currentPageNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageInsertionViewModel"/> class.
    /// </summary>
    /// <param name="imageInsertionService">Service for image insertion operations.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public ImageInsertionViewModel(
        IImageInsertionService imageInsertionService,
        ILogger<ImageInsertionViewModel> logger)
    {
        _imageInsertionService = imageInsertionService ?? throw new ArgumentNullException(nameof(imageInsertionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("ImageInsertionViewModel initialized");
    }

    /// <summary>
    /// Gets the collection of images inserted on the current page.
    /// </summary>
    public ObservableCollection<ImageObject> InsertedImages { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected image.
    /// </summary>
    [ObservableProperty]
    private ImageObject? _selectedImage;

    /// <summary>
    /// Gets or sets a value indicating whether an image operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved image changes.
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Triggered when a selection changes to notify parent view models.
    /// </summary>
    partial void OnSelectedImageChanged(ImageObject? oldValue, ImageObject? newValue)
    {
        // Deselect old image
        if (oldValue != null)
        {
            oldValue.IsSelected = false;
        }

        // Select new image
        if (newValue != null)
        {
            newValue.IsSelected = true;
        }

        _logger.LogInformation(
            "Selected image changed. OldId={OldId}, NewId={NewId}",
            oldValue?.Id, newValue?.Id);
    }

    /// <summary>
    /// Opens a file picker to select and insert an image into the current page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInsertImage))]
    private async Task InsertImageAsync()
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot insert image: no document loaded");
            return;
        }

        _logger.LogInformation("InsertImage command invoked");

        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                _logger.LogInformation("Image file picker cancelled");
                return;
            }

            _logger.LogInformation(
                "Image file selected: {FilePath}, inserting into page {Page}",
                file.Path, _currentPageNumber);

            await InsertImageFromPathAsync(file.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert image");
        }
    }

    private bool CanInsertImage() => _currentDocument != null && !IsLoading;

    /// <summary>
    /// Inserts an image from the specified file path into the current page.
    /// </summary>
    /// <param name="imagePath">The path to the image file.</param>
    /// <param name="position">Optional position for the image. If null, defaults to center of page.</param>
    private async Task InsertImageFromPathAsync(string imagePath, PointF? position = null)
    {
        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot insert image: no document loaded");
            return;
        }

        try
        {
            IsLoading = true;

            // Default to center of page if no position specified
            // TODO: Get actual page dimensions and calculate center
            var insertPosition = position ?? new PointF(300, 400);

            var result = await _imageInsertionService.InsertImageAsync(
                _currentDocument,
                _currentPageNumber,
                imagePath,
                insertPosition);

            if (result.IsSuccess)
            {
                InsertedImages.Add(result.Value);
                SelectedImage = result.Value;
                HasUnsavedChanges = true;

                _logger.LogInformation(
                    "Image inserted successfully. Id={Id}, Path={Path}, Position=({X},{Y})",
                    result.Value.Id, imagePath, insertPosition.X, insertPosition.Y);
            }
            else
            {
                _logger.LogError("Failed to insert image: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while inserting image from path: {Path}", imagePath);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Deletes the currently selected image.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSelectedImage))]
    private async Task DeleteSelectedImageAsync()
    {
        if (SelectedImage == null)
        {
            _logger.LogWarning("Cannot delete image: no image selected");
            return;
        }

        _logger.LogInformation(
            "Deleting selected image. Id={Id}, Page={Page}",
            SelectedImage.Id, SelectedImage.PageIndex);

        try
        {
            IsLoading = true;

            var result = await _imageInsertionService.DeleteImageAsync(SelectedImage);

            if (result.IsSuccess)
            {
                InsertedImages.Remove(SelectedImage);
                HasUnsavedChanges = true;
                _logger.LogInformation("Image deleted successfully. Id={Id}", SelectedImage.Id);
                SelectedImage = null;
            }
            else
            {
                _logger.LogError("Failed to delete image: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting image");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeleteSelectedImage() => SelectedImage != null && !IsLoading;

    /// <summary>
    /// Moves the selected image to a new position.
    /// </summary>
    /// <param name="newPosition">The new position for the image (in PDF points).</param>
    [RelayCommand(CanExecute = nameof(CanMoveImage))]
    private async Task MoveImageAsync(PointF newPosition)
    {
        if (SelectedImage == null)
        {
            _logger.LogWarning("Cannot move image: no image selected");
            return;
        }

        _logger.LogInformation(
            "Moving image. Id={Id}, OldPosition=({OldX},{OldY}), NewPosition=({NewX},{NewY})",
            SelectedImage.Id, SelectedImage.Position.X, SelectedImage.Position.Y, newPosition.X, newPosition.Y);

        try
        {
            var result = await _imageInsertionService.MoveImageAsync(SelectedImage, newPosition);

            if (result.IsSuccess)
            {
                SelectedImage.Position = newPosition;
                SelectedImage.ModifiedDate = DateTime.UtcNow;
                HasUnsavedChanges = true;
                _logger.LogInformation("Image moved successfully. Id={Id}", SelectedImage.Id);
            }
            else
            {
                _logger.LogError("Failed to move image: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while moving image");
        }
    }

    private bool CanMoveImage() => SelectedImage != null && !IsLoading;

    /// <summary>
    /// Scales the selected image to a new size.
    /// </summary>
    /// <param name="newSize">The new size for the image (in PDF points).</param>
    [RelayCommand(CanExecute = nameof(CanScaleImage))]
    private async Task ScaleImageAsync(SizeF newSize)
    {
        if (SelectedImage == null)
        {
            _logger.LogWarning("Cannot scale image: no image selected");
            return;
        }

        _logger.LogInformation(
            "Scaling image. Id={Id}, OldSize=({OldW},{OldH}), NewSize=({NewW},{NewH})",
            SelectedImage.Id, SelectedImage.Size.Width, SelectedImage.Size.Height, newSize.Width, newSize.Height);

        try
        {
            var result = await _imageInsertionService.ScaleImageAsync(SelectedImage, newSize);

            if (result.IsSuccess)
            {
                SelectedImage.Size = newSize;
                SelectedImage.ModifiedDate = DateTime.UtcNow;
                HasUnsavedChanges = true;
                _logger.LogInformation("Image scaled successfully. Id={Id}", SelectedImage.Id);
            }
            else
            {
                _logger.LogError("Failed to scale image: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while scaling image");
        }
    }

    private bool CanScaleImage() => SelectedImage != null && !IsLoading;

    /// <summary>
    /// Rotates the selected image by a specified angle.
    /// </summary>
    /// <param name="angleDegrees">The rotation angle in degrees.</param>
    [RelayCommand(CanExecute = nameof(CanRotateImage))]
    private async Task RotateImageAsync(float angleDegrees)
    {
        if (SelectedImage == null)
        {
            _logger.LogWarning("Cannot rotate image: no image selected");
            return;
        }

        _logger.LogInformation(
            "Rotating image. Id={Id}, CurrentRotation={Current}, AngleDelta={Delta}",
            SelectedImage.Id, SelectedImage.RotationDegrees, angleDegrees);

        try
        {
            IsLoading = true;

            var result = await _imageInsertionService.RotateImageAsync(SelectedImage, angleDegrees);

            if (result.IsSuccess)
            {
                SelectedImage.RotationDegrees += angleDegrees;
                SelectedImage.ModifiedDate = DateTime.UtcNow;
                HasUnsavedChanges = true;
                _logger.LogInformation(
                    "Image rotated successfully. Id={Id}, NewRotation={NewRotation}",
                    SelectedImage.Id, SelectedImage.RotationDegrees);
            }
            else
            {
                _logger.LogError("Failed to rotate image: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while rotating image");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRotateImage() => SelectedImage != null && !IsLoading;

    /// <summary>
    /// Rotates the selected image right by 90 degrees.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRotateImage))]
    private async Task RotateRightAsync()
    {
        await RotateImageAsync(90);
    }

    /// <summary>
    /// Rotates the selected image left by 90 degrees.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRotateImage))]
    private async Task RotateLeftAsync()
    {
        await RotateImageAsync(-90);
    }

    /// <summary>
    /// Rotates the selected image by 180 degrees.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRotateImage))]
    private async Task Rotate180Async()
    {
        await RotateImageAsync(180);
    }

    /// <summary>
    /// Brings the selected image to the front (top of z-order).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChangeZOrder))]
    private void BringToFront()
    {
        if (SelectedImage == null)
        {
            _logger.LogWarning("Cannot bring to front: no image selected");
            return;
        }

        _logger.LogInformation(
            "Bringing image to front. Id={Id}",
            SelectedImage.Id);

        // Move image to end of collection (rendered last = on top)
        InsertedImages.Remove(SelectedImage);
        InsertedImages.Add(SelectedImage);
        HasUnsavedChanges = true;

        _logger.LogInformation("Image brought to front. Id={Id}", SelectedImage.Id);
    }

    /// <summary>
    /// Sends the selected image to the back (bottom of z-order).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChangeZOrder))]
    private void SendToBack()
    {
        if (SelectedImage == null)
        {
            _logger.LogWarning("Cannot send to back: no image selected");
            return;
        }

        _logger.LogInformation(
            "Sending image to back. Id={Id}",
            SelectedImage.Id);

        // Move image to beginning of collection (rendered first = on bottom)
        var index = InsertedImages.IndexOf(SelectedImage);
        if (index > 0)
        {
            InsertedImages.Remove(SelectedImage);
            InsertedImages.Insert(0, SelectedImage);
            HasUnsavedChanges = true;
            _logger.LogInformation("Image sent to back. Id={Id}", SelectedImage.Id);
        }
    }

    private bool CanChangeZOrder() => SelectedImage != null && InsertedImages.Count > 1 && !IsLoading;

    /// <summary>
    /// Loads images for the specified document and page.
    /// </summary>
    /// <param name="parameters">Tuple containing the document and page number.</param>
    [RelayCommand]
    private void LoadImages((PdfDocument document, int pageNumber) parameters)
    {
        _currentDocument = parameters.document;
        _currentPageNumber = parameters.pageNumber;

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot load images: no document provided");
            InsertedImages.Clear();
            return;
        }

        _logger.LogInformation(
            "Loading images. Document={FilePath}, Page={Page}",
            _currentDocument.FilePath, _currentPageNumber);

        // Clear images when switching pages
        // Note: Actual image persistence and loading from PDF will be handled later
        InsertedImages.Clear();
        SelectedImage = null;
        HasUnsavedChanges = false;

        _logger.LogInformation(
            "Images loaded for page. Page={Page}, Count={Count}",
            _currentPageNumber, InsertedImages.Count);
    }

    /// <summary>
    /// Selects an image.
    /// </summary>
    /// <param name="image">The image to select, or null to deselect.</param>
    [RelayCommand]
    private void SelectImage(ImageObject? image)
    {
        _logger.LogInformation(
            "Selecting image. Id={Id}",
            image?.Id);

        SelectedImage = image;
    }
}
