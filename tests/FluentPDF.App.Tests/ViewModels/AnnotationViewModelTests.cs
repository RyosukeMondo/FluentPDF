using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;
using Windows.UI;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for AnnotationViewModel demonstrating headless MVVM testing.
/// </summary>
public class AnnotationViewModelTests
{
    private readonly Mock<IAnnotationService> _annotationServiceMock;
    private readonly Mock<ILogger<AnnotationViewModel>> _loggerMock;

    public AnnotationViewModelTests()
    {
        _annotationServiceMock = new Mock<IAnnotationService>();
        _loggerMock = new Mock<ILogger<AnnotationViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ActiveTool.Should().Be(AnnotationTool.None, "no tool should be active initially");
        viewModel.SelectedColor.Should().Be(Colors.Yellow, "default color should be yellow");
        viewModel.StrokeWidth.Should().Be(2.0, "default stroke width");
        viewModel.Opacity.Should().Be(0.5, "default opacity");
        viewModel.Annotations.Should().BeEmpty("no annotations loaded initially");
        viewModel.SelectedAnnotation.Should().BeNull("no annotation selected initially");
        viewModel.IsLoading.Should().BeFalse("not loading initially");
        viewModel.IsToolbarVisible.Should().BeFalse("toolbar should be hidden by default");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenAnnotationServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new AnnotationViewModel(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("annotationService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new AnnotationViewModel(
            _annotationServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void SelectToolCommand_ShouldSetActiveTool()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectToolCommand.Execute(AnnotationTool.Highlight);

        // Assert
        viewModel.ActiveTool.Should().Be(AnnotationTool.Highlight);
    }

    [Fact]
    public void SelectToolCommand_ShouldDeselectCurrentAnnotation_WhenSwitchingTools()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var annotation = new Annotation
        {
            Id = "test-1",
            Type = AnnotationType.Highlight,
            IsSelected = true
        };
        viewModel.Annotations.Add(annotation);
        viewModel.SelectAnnotationCommand.Execute(annotation);

        // Act
        viewModel.SelectToolCommand.Execute(AnnotationTool.Rectangle);

        // Assert
        viewModel.ActiveTool.Should().Be(AnnotationTool.Rectangle);
        viewModel.SelectedAnnotation.Should().BeNull();
        annotation.IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAnnotationsCommand_ShouldPopulateAnnotations_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testAnnotations = CreateTestAnnotations();

        _annotationServiceMock
            .Setup(x => x.GetAnnotationsAsync(document, 0))
            .ReturnsAsync(Result.Ok(testAnnotations));

        // Act
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        // Assert
        viewModel.Annotations.Should().HaveCount(2);
        viewModel.Annotations[0].Type.Should().Be(AnnotationType.Highlight);
        viewModel.Annotations[1].Type.Should().Be(AnnotationType.Square);
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAnnotationsCommand_ShouldClearAnnotations_WhenServiceFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();

        _annotationServiceMock
            .Setup(x => x.GetAnnotationsAsync(document, 0))
            .ReturnsAsync(Result.Fail("Failed to load annotations"));

        // Act
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        // Assert
        viewModel.Annotations.Should().BeEmpty();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAnnotationsCommand_ShouldSetIsLoadingToTrue_DuringExecution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var wasLoadingDuringExecution = false;

        _annotationServiceMock
            .Setup(x => x.GetAnnotationsAsync(document, 0))
            .ReturnsAsync(() =>
            {
                wasLoadingDuringExecution = viewModel.IsLoading;
                return Result.Ok(new List<Annotation>());
            });

        // Act
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        // Assert
        wasLoadingDuringExecution.Should().BeTrue("IsLoading should be set during execution");
        viewModel.IsLoading.Should().BeFalse("IsLoading should be reset after execution");
    }

    [Fact]
    public async Task CreateAnnotationCommand_ShouldAddAnnotation_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        viewModel.SelectToolCommand.Execute(AnnotationTool.Highlight);

        var bounds = new PdfRectangle { Left = 10, Top = 10, Right = 100, Bottom = 50 };
        var createdAnnotation = new Annotation
        {
            Id = "new-1",
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = bounds
        };

        _annotationServiceMock
            .Setup(x => x.CreateAnnotationAsync(document, It.IsAny<Annotation>()))
            .ReturnsAsync(Result.Ok(createdAnnotation));

        // Act
        await viewModel.CreateAnnotationCommand.ExecuteAsync(bounds);

        // Assert
        viewModel.Annotations.Should().Contain(createdAnnotation);
        viewModel.ActiveTool.Should().Be(AnnotationTool.None, "tool should reset after creating annotation");
    }

    [Fact]
    public void CreateAnnotationCommand_CanExecute_ShouldBeFalse_WhenNoToolSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0)).Wait();

        // Act
        var canExecute = viewModel.CreateAnnotationCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot create annotation without a tool selected");
    }

    [Fact]
    public void CreateAnnotationCommand_CanExecute_ShouldBeTrue_WhenToolSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0)).Wait();
        viewModel.SelectToolCommand.Execute(AnnotationTool.Rectangle);

        // Act
        var canExecute = viewModel.CreateAnnotationCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can create annotation when tool is selected");
    }

    [Fact]
    public async Task CreateInkAnnotationCommand_ShouldAddInkAnnotation_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        var inkPoints = new List<System.Drawing.PointF>
        {
            new System.Drawing.PointF(10, 10),
            new System.Drawing.PointF(20, 20),
            new System.Drawing.PointF(30, 15)
        };

        var createdAnnotation = new Annotation
        {
            Id = "ink-1",
            Type = AnnotationType.Ink,
            PageNumber = 0,
            InkPoints = inkPoints
        };

        _annotationServiceMock
            .Setup(x => x.CreateAnnotationAsync(document, It.IsAny<Annotation>()))
            .ReturnsAsync(Result.Ok(createdAnnotation));

        // Act
        await viewModel.CreateInkAnnotationCommand.ExecuteAsync(inkPoints);

        // Assert
        viewModel.Annotations.Should().Contain(createdAnnotation);
        viewModel.ActiveTool.Should().Be(AnnotationTool.None);
    }

    [Fact]
    public async Task DeleteAnnotationCommand_ShouldRemoveAnnotation_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var annotation = new Annotation
        {
            Id = "test-1",
            Type = AnnotationType.Highlight,
            PageNumber = 0
        };

        viewModel.Annotations.Add(annotation);
        viewModel.SelectAnnotationCommand.Execute(annotation);

        _annotationServiceMock
            .Setup(x => x.DeleteAnnotationAsync(document, 0, 0))
            .ReturnsAsync(Result.Ok());

        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        // Act
        await viewModel.DeleteAnnotationCommand.ExecuteAsync(null);

        // Assert
        viewModel.Annotations.Should().NotContain(annotation);
        viewModel.SelectedAnnotation.Should().BeNull();
    }

    [Fact]
    public void DeleteAnnotationCommand_CanExecute_ShouldBeFalse_WhenNoAnnotationSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.DeleteAnnotationCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot delete when no annotation is selected");
    }

    [Fact]
    public void DeleteAnnotationCommand_CanExecute_ShouldBeTrue_WhenAnnotationSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var annotation = new Annotation { Id = "test-1", Type = AnnotationType.Highlight };
        viewModel.Annotations.Add(annotation);
        viewModel.SelectAnnotationCommand.Execute(annotation);

        // Act
        var canExecute = viewModel.DeleteAnnotationCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can delete when annotation is selected");
    }

    [Fact]
    public async Task SaveAnnotationsCommand_ShouldCallService_WhenDocumentLoaded()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        _annotationServiceMock
            .Setup(x => x.SaveAnnotationsAsync(document, document.FilePath, true))
            .ReturnsAsync(Result.Ok());

        // Act
        await viewModel.SaveAnnotationsCommand.ExecuteAsync(null);

        // Assert
        _annotationServiceMock.Verify(
            x => x.SaveAnnotationsAsync(document, document.FilePath, true),
            Times.Once,
            "SaveAnnotationsAsync should be called with correct parameters");
    }

    [Fact]
    public void SaveAnnotationsCommand_CanExecute_ShouldBeFalse_WhenNoDocumentLoaded()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.SaveAnnotationsCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot save when no document is loaded");
    }

    [Fact]
    public void ToggleToolbarCommand_ShouldToggleToolbarVisibility()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var initialState = viewModel.IsToolbarVisible;

        // Act
        viewModel.ToggleToolbarCommand.Execute(null);

        // Assert
        viewModel.IsToolbarVisible.Should().Be(!initialState);
    }

    [Fact]
    public void ToggleToolbarCommand_ShouldClearActiveTool_WhenHidingToolbar()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectToolCommand.Execute(AnnotationTool.Highlight);
        viewModel.ToggleToolbarCommand.Execute(null); // Show toolbar

        // Act
        viewModel.ToggleToolbarCommand.Execute(null); // Hide toolbar

        // Assert
        viewModel.IsToolbarVisible.Should().BeFalse();
        viewModel.ActiveTool.Should().Be(AnnotationTool.None);
    }

    [Fact]
    public void SelectAnnotationCommand_ShouldSelectAnnotation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var annotation = new Annotation
        {
            Id = "test-1",
            Type = AnnotationType.Highlight
        };
        viewModel.Annotations.Add(annotation);

        // Act
        viewModel.SelectAnnotationCommand.Execute(annotation);

        // Assert
        viewModel.SelectedAnnotation.Should().Be(annotation);
        annotation.IsSelected.Should().BeTrue();
        viewModel.ActiveTool.Should().Be(AnnotationTool.None);
    }

    [Fact]
    public void SelectAnnotationCommand_ShouldDeselectPreviousAnnotation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var annotation1 = new Annotation { Id = "test-1", Type = AnnotationType.Highlight };
        var annotation2 = new Annotation { Id = "test-2", Type = AnnotationType.Square };
        viewModel.Annotations.Add(annotation1);
        viewModel.Annotations.Add(annotation2);

        viewModel.SelectAnnotationCommand.Execute(annotation1);

        // Act
        viewModel.SelectAnnotationCommand.Execute(annotation2);

        // Assert
        viewModel.SelectedAnnotation.Should().Be(annotation2);
        annotation1.IsSelected.Should().BeFalse();
        annotation2.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectAnnotationCommand_ShouldHandleNull_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var annotation = new Annotation { Id = "test-1", Type = AnnotationType.Highlight };
        viewModel.Annotations.Add(annotation);
        viewModel.SelectAnnotationCommand.Execute(annotation);

        // Act
        viewModel.SelectAnnotationCommand.Execute(null);

        // Assert
        viewModel.SelectedAnnotation.Should().BeNull();
        annotation.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void ActiveTool_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(AnnotationViewModel.ActiveTool))
                eventRaised = true;
        };

        // Act
        viewModel.SelectToolCommand.Execute(AnnotationTool.Rectangle);

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.ActiveTool.Should().Be(AnnotationTool.Rectangle);
    }

    [Fact]
    public void SelectedColor_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(AnnotationViewModel.SelectedColor))
                eventRaised = true;
        };

        // Act
        viewModel.SelectedColor = Colors.Red;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.SelectedColor.Should().Be(Colors.Red);
    }

    [Fact]
    public void SelectedAnnotation_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(AnnotationViewModel.SelectedAnnotation))
                eventRaised = true;
        };

        var annotation = new Annotation { Id = "test", Type = AnnotationType.Highlight };

        // Act
        viewModel.SelectAnnotationCommand.Execute(annotation);

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.SelectedAnnotation.Should().Be(annotation);
    }

    [Fact]
    public void ViewModel_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that the ViewModel can be instantiated and tested
        // without requiring WinUI runtime (headless testing)

        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Should().BeAssignableTo<INotifyPropertyChanged>();
    }

    [Fact]
    public async Task LoadAnnotationsCommand_ShouldHandleException_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();

        _annotationServiceMock
            .Setup(x => x.GetAnnotationsAsync(document, 0))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        Func<Task> act = async () => await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        // Assert
        await act.Should().NotThrowAsync("command should handle exceptions gracefully");
        viewModel.Annotations.Should().BeEmpty();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAnnotationsCommand_ShouldLogAnnotationCount()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testAnnotations = CreateTestAnnotations();

        _annotationServiceMock
            .Setup(x => x.GetAnnotationsAsync(document, 0))
            .ReturnsAsync(Result.Ok(testAnnotations));

        // Act
        await viewModel.LoadAnnotationsCommand.ExecuteAsync((document, 0));

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Annotations loaded successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Should log annotation loading");
    }

    private AnnotationViewModel CreateViewModel()
    {
        return new AnnotationViewModel(
            _annotationServiceMock.Object,
            _loggerMock.Object);
    }

    private PdfDocument CreateTestDocument()
    {
        return new PdfDocument
        {
            FilePath = "/test/document.pdf",
            PageCount = 10,
            Handle = IntPtr.Zero
        };
    }

    private List<Annotation> CreateTestAnnotations()
    {
        return new List<Annotation>
        {
            new Annotation
            {
                Id = "annot-1",
                Type = AnnotationType.Highlight,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 10, Top = 10, Right = 100, Bottom = 50 },
                FillColor = System.Drawing.Color.Yellow
            },
            new Annotation
            {
                Id = "annot-2",
                Type = AnnotationType.Square,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 150, Top = 50, Right = 300, Bottom = 150 },
                StrokeColor = System.Drawing.Color.Red
            }
        };
    }
}
