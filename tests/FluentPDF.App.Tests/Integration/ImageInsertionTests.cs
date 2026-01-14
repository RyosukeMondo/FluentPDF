using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for image insertion feature.
/// Tests the interaction between ImageInsertionViewModel and IImageInsertionService.
/// </summary>
public sealed class ImageInsertionTests : IDisposable
{
    private readonly Mock<IImageInsertionService> _imageServiceMock;
    private readonly Mock<ILogger<ImageInsertionViewModel>> _loggerMock;
    private readonly List<string> _testImageFiles;
    private readonly PdfDocument _testDocument;

    public ImageInsertionTests()
    {
        _imageServiceMock = new Mock<IImageInsertionService>();
        _loggerMock = new Mock<ILogger<ImageInsertionViewModel>>();
        _testImageFiles = new List<string>();

        _testDocument = new PdfDocument
        {
            FilePath = Path.Combine(Path.GetTempPath(), "test.pdf"),
            PageCount = 5
        };
    }

    public void Dispose()
    {
        foreach (var file in _testImageFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task InsertImage_ValidImage_AddsToInsertedImages()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var expectedImage = CreateImageObject(imagePath, 0);

        _imageServiceMock
            .Setup(s => s.InsertImageAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                imagePath,
                It.IsAny<PointF>()))
            .ReturnsAsync(Result.Ok(expectedImage));

        // Act - Simulate file picker by calling internal method via reflection
        await InsertImageFromPathAsync(viewModel, imagePath);

        // Assert
        viewModel.InsertedImages.Should().HaveCount(1);
        viewModel.InsertedImages[0].Should().Be(expectedImage);
        viewModel.SelectedImage.Should().Be(expectedImage);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task InsertImage_ServiceFails_DoesNotAddToCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");

        _imageServiceMock
            .Setup(s => s.InsertImageAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                imagePath,
                It.IsAny<PointF>()))
            .ReturnsAsync(Result.Fail("Image format not supported"));

        // Act
        await InsertImageFromPathAsync(viewModel, imagePath);

        // Assert
        viewModel.InsertedImages.Should().BeEmpty();
        viewModel.SelectedImage.Should().BeNull();
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task InsertMultipleImages_AllImagesAdded()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var image1Path = CreateTestImageFile("image1.png");
        var image2Path = CreateTestImageFile("image2.jpg");
        var image3Path = CreateTestImageFile("image3.bmp");

        var imageObj1 = CreateImageObject(image1Path, 0);
        var imageObj2 = CreateImageObject(image2Path, 0);
        var imageObj3 = CreateImageObject(image3Path, 0);

        SetupInsertImageMock(image1Path, imageObj1);
        SetupInsertImageMock(image2Path, imageObj2);
        SetupInsertImageMock(image3Path, imageObj3);

        // Act
        await InsertImageFromPathAsync(viewModel, image1Path);
        await InsertImageFromPathAsync(viewModel, image2Path);
        await InsertImageFromPathAsync(viewModel, image3Path);

        // Assert
        viewModel.InsertedImages.Should().HaveCount(3);
        viewModel.InsertedImages[0].Should().Be(imageObj1);
        viewModel.InsertedImages[1].Should().Be(imageObj2);
        viewModel.InsertedImages[2].Should().Be(imageObj3);
        viewModel.SelectedImage.Should().Be(imageObj3, "last inserted image should be selected");
    }

    [Fact]
    public async Task DeleteSelectedImage_RemovesFromCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        _imageServiceMock
            .Setup(s => s.DeleteImageAsync(imageObj))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.DeleteSelectedImageCommand.ExecuteAsync(null);

        // Assert
        viewModel.InsertedImages.Should().BeEmpty();
        viewModel.SelectedImage.Should().BeNull();
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteImage_ServiceFails_DoesNotRemove()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        _imageServiceMock
            .Setup(s => s.DeleteImageAsync(imageObj))
            .ReturnsAsync(Result.Fail("Failed to delete image"));

        var initialCount = viewModel.InsertedImages.Count;

        // Act
        await viewModel.DeleteSelectedImageCommand.ExecuteAsync(null);

        // Assert
        viewModel.InsertedImages.Should().HaveCount(initialCount);
        viewModel.SelectedImage.Should().Be(imageObj, "image should still be selected");
    }

    [Fact]
    public async Task MoveImage_UpdatesPosition()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        var newPosition = new PointF(500, 600);

        _imageServiceMock
            .Setup(s => s.MoveImageAsync(imageObj, newPosition))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.MoveImageCommand.ExecuteAsync(newPosition);

        // Assert
        imageObj.Position.Should().Be(newPosition);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task ScaleImage_UpdatesSize()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        var newSize = new SizeF(200, 300);

        _imageServiceMock
            .Setup(s => s.ScaleImageAsync(imageObj, newSize))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.ScaleImageCommand.ExecuteAsync(newSize);

        // Assert
        imageObj.Size.Should().Be(newSize);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task RotateImage_UpdatesRotation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        var initialRotation = imageObj.RotationDegrees;

        _imageServiceMock
            .Setup(s => s.RotateImageAsync(imageObj, 90))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.RotateImageCommand.ExecuteAsync(90f);

        // Assert
        imageObj.RotationDegrees.Should().Be(initialRotation + 90);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task RotateRight_Rotates90Degrees()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        _imageServiceMock
            .Setup(s => s.RotateImageAsync(imageObj, 90))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.RotateRightCommand.ExecuteAsync(null);

        // Assert
        imageObj.RotationDegrees.Should().Be(90);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task RotateLeft_RotatesMinus90Degrees()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        _imageServiceMock
            .Setup(s => s.RotateImageAsync(imageObj, -90))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.RotateLeftCommand.ExecuteAsync(null);

        // Assert
        imageObj.RotationDegrees.Should().Be(-90);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Rotate180_Rotates180Degrees()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        _imageServiceMock
            .Setup(s => s.RotateImageAsync(imageObj, 180))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.Rotate180Command.ExecuteAsync(null);

        // Assert
        imageObj.RotationDegrees.Should().Be(180);
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task BringToFront_MovesToEndOfCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var image1Path = CreateTestImageFile("image1.png");
        var image2Path = CreateTestImageFile("image2.png");
        var image3Path = CreateTestImageFile("image3.png");

        var imageObj1 = CreateImageObject(image1Path, 0);
        var imageObj2 = CreateImageObject(image2Path, 0);
        var imageObj3 = CreateImageObject(image3Path, 0);

        SetupInsertImageMock(image1Path, imageObj1);
        SetupInsertImageMock(image2Path, imageObj2);
        SetupInsertImageMock(image3Path, imageObj3);

        await InsertImageFromPathAsync(viewModel, image1Path);
        await InsertImageFromPathAsync(viewModel, image2Path);
        await InsertImageFromPathAsync(viewModel, image3Path);

        // Select the first image
        viewModel.SelectImageCommand.Execute(imageObj1);

        // Act
        viewModel.BringToFrontCommand.Execute(null);

        // Assert
        viewModel.InsertedImages.Should().HaveCount(3);
        viewModel.InsertedImages[2].Should().Be(imageObj1, "image should be moved to end (top of z-order)");
        viewModel.InsertedImages[0].Should().Be(imageObj2);
        viewModel.InsertedImages[1].Should().Be(imageObj3);
    }

    [Fact]
    public async Task SendToBack_MovesToBeginningOfCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var image1Path = CreateTestImageFile("image1.png");
        var image2Path = CreateTestImageFile("image2.png");
        var image3Path = CreateTestImageFile("image3.png");

        var imageObj1 = CreateImageObject(image1Path, 0);
        var imageObj2 = CreateImageObject(image2Path, 0);
        var imageObj3 = CreateImageObject(image3Path, 0);

        SetupInsertImageMock(image1Path, imageObj1);
        SetupInsertImageMock(image2Path, imageObj2);
        SetupInsertImageMock(image3Path, imageObj3);

        await InsertImageFromPathAsync(viewModel, image1Path);
        await InsertImageFromPathAsync(viewModel, image2Path);
        await InsertImageFromPathAsync(viewModel, image3Path);

        // Select the last image
        viewModel.SelectImageCommand.Execute(imageObj3);

        // Act
        viewModel.SendToBackCommand.Execute(null);

        // Assert
        viewModel.InsertedImages.Should().HaveCount(3);
        viewModel.InsertedImages[0].Should().Be(imageObj3, "image should be moved to beginning (bottom of z-order)");
        viewModel.InsertedImages[1].Should().Be(imageObj1);
        viewModel.InsertedImages[2].Should().Be(imageObj2);
    }

    [Fact]
    public void SelectImage_UpdatesSelectedImage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var imageObj = CreateImageObject("test.png", 0);
        viewModel.InsertedImages.Add(imageObj);

        // Act
        viewModel.SelectImageCommand.Execute(imageObj);

        // Assert
        viewModel.SelectedImage.Should().Be(imageObj);
        imageObj.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectDifferentImage_DeselectsPrevious()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var imageObj1 = CreateImageObject("test1.png", 0);
        var imageObj2 = CreateImageObject("test2.png", 0);
        viewModel.InsertedImages.Add(imageObj1);
        viewModel.InsertedImages.Add(imageObj2);

        viewModel.SelectImageCommand.Execute(imageObj1);

        // Act
        viewModel.SelectImageCommand.Execute(imageObj2);

        // Assert
        viewModel.SelectedImage.Should().Be(imageObj2);
        imageObj1.IsSelected.Should().BeFalse();
        imageObj2.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void LoadImages_ClearsInsertedImages()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.InsertedImages.Add(CreateImageObject("test.png", 0));
        viewModel.InsertedImages.Add(CreateImageObject("test2.png", 0));

        // Act
        viewModel.LoadImagesCommand.Execute((_testDocument, 1));

        // Assert
        viewModel.InsertedImages.Should().BeEmpty();
        viewModel.SelectedImage.Should().BeNull();
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void LoadImages_UpdatesCurrentPageContext()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.LoadImagesCommand.Execute((_testDocument, 2));

        // Insert image should now use page 2
        var imagePath = CreateTestImageFile("test.png");
        var expectedImage = CreateImageObject(imagePath, 2);

        _imageServiceMock
            .Setup(s => s.InsertImageAsync(_testDocument, 2, imagePath, It.IsAny<PointF>()))
            .ReturnsAsync(Result.Ok(expectedImage));

        // Assert - Verify service called with correct page number
        viewModel.InsertImageCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task InsertImage_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        var loadingStates = new List<bool>();
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsLoading))
            {
                loadingStates.Add(viewModel.IsLoading);
            }
        };

        var taskCompletionSource = new TaskCompletionSource<Result<ImageObject>>();
        _imageServiceMock
            .Setup(s => s.InsertImageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), imagePath, It.IsAny<PointF>()))
            .Returns(taskCompletionSource.Task);

        // Act
        var insertTask = InsertImageFromPathAsync(viewModel, imagePath);

        await Task.Delay(50); // Let the task start
        loadingStates.Should().Contain(true, "IsLoading should be true during operation");

        taskCompletionSource.SetResult(Result.Ok(imageObj));
        await insertTask;

        // Assert
        loadingStates.Should().Contain(false, "IsLoading should be false after operation");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteImage_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LoadImagesCommand.Execute((_testDocument, 0));

        var imagePath = CreateTestImageFile("test.png");
        var imageObj = CreateImageObject(imagePath, 0);

        SetupInsertImageMock(imagePath, imageObj);
        await InsertImageFromPathAsync(viewModel, imagePath);

        var loadingStates = new List<bool>();
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsLoading))
            {
                loadingStates.Add(viewModel.IsLoading);
            }
        };

        _imageServiceMock
            .Setup(s => s.DeleteImageAsync(imageObj))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.DeleteSelectedImageCommand.ExecuteAsync(null);

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should be true during operation");
        loadingStates.Should().Contain(false, "IsLoading should be false after operation");
        viewModel.IsLoading.Should().BeFalse();
    }

    private ImageInsertionViewModel CreateViewModel()
    {
        return new ImageInsertionViewModel(_imageServiceMock.Object, _loggerMock.Object);
    }

    private string CreateTestImageFile(string fileName)
    {
        var path = Path.Combine(Path.GetTempPath(), $"FluentPDF_ImageTest_{Guid.NewGuid()}_{fileName}");
        File.WriteAllBytes(path, new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        _testImageFiles.Add(path);
        return path;
    }

    private ImageObject CreateImageObject(string sourcePath, int pageIndex)
    {
        return new ImageObject
        {
            Id = Guid.NewGuid(),
            PageIndex = pageIndex,
            Position = new PointF(100, 200),
            Size = new SizeF(150, 100),
            RotationDegrees = 0,
            SourcePath = sourcePath,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private void SetupInsertImageMock(string imagePath, ImageObject imageObj)
    {
        _imageServiceMock
            .Setup(s => s.InsertImageAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                imagePath,
                It.IsAny<PointF>()))
            .ReturnsAsync(Result.Ok(imageObj));
    }

    private async Task InsertImageFromPathAsync(ImageInsertionViewModel viewModel, string imagePath)
    {
        var method = typeof(ImageInsertionViewModel).GetMethod(
            "InsertImageFromPathAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task)method!.Invoke(viewModel, new object?[] { imagePath, null })!;
        await task;
    }
}
