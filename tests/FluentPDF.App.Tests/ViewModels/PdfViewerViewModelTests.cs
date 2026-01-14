using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for PdfViewerViewModel demonstrating headless MVVM testing.
/// Tests all commands, properties, and CanExecute logic without requiring WinUI runtime.
/// </summary>
public class PdfViewerViewModelTests : IDisposable
{
    private readonly Mock<IPdfDocumentService> _documentServiceMock;
    private readonly Mock<IPdfRenderingService> _renderingServiceMock;
    private readonly Mock<ILogger<PdfViewerViewModel>> _loggerMock;
    private readonly PdfViewerViewModel _viewModel;

    public PdfViewerViewModelTests()
    {
        _documentServiceMock = new Mock<IPdfDocumentService>();
        _renderingServiceMock = new Mock<IPdfRenderingService>();
        _loggerMock = new Mock<ILogger<PdfViewerViewModel>>();

        _viewModel = new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _viewModel.CurrentPageImage.Should().BeNull();
        _viewModel.CurrentPageNumber.Should().Be(1);
        _viewModel.TotalPages.Should().Be(0);
        _viewModel.ZoomLevel.Should().Be(1.0);
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.StatusMessage.Should().Be("Open a PDF file to get started");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenDocumentServiceIsNull()
    {
        // Act
        Action act = () => new PdfViewerViewModel(
            null!,
            _renderingServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenRenderingServiceIsNull()
    {
        // Act
        Action act = () => new PdfViewerViewModel(
            _documentServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("renderingService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void CurrentPageImage_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageImage))
                eventRaised = true;
        };

        // Act
        _viewModel.CurrentPageImage = null;

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void CurrentPageNumber_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageNumber))
                eventRaised = true;
        };

        // Act
        _viewModel.CurrentPageNumber = 5;

        // Assert
        eventRaised.Should().BeTrue();
        _viewModel.CurrentPageNumber.Should().Be(5);
    }

    [Fact]
    public void ZoomLevel_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PdfViewerViewModel.ZoomLevel))
                eventRaised = true;
        };

        // Act
        _viewModel.ZoomLevel = 1.5;

        // Assert
        eventRaised.Should().BeTrue();
        _viewModel.ZoomLevel.Should().Be(1.5);
    }

    [Fact]
    public void IsLoading_ShouldTriggerCommandCanExecuteChanged()
    {
        // Arrange
        SetupLoadedDocument();
        var canExecuteChangedCount = 0;

        _viewModel.GoToNextPageCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;
        _viewModel.GoToPreviousPageCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;
        _viewModel.ZoomInCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;
        _viewModel.ZoomOutCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;
        _viewModel.ResetZoomCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

        // Act
        _viewModel.IsLoading = true;

        // Assert
        canExecuteChangedCount.Should().BeGreaterOrEqualTo(5, "all navigation and zoom commands should update");
    }

    [Fact]
    public void GoToPreviousPageCommand_CanExecute_ShouldReturnFalse_WhenOnFirstPage()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.CurrentPageNumber = 1;

        // Act
        var canExecute = _viewModel.GoToPreviousPageCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot go to previous page when on first page");
    }

    [Fact]
    public void GoToPreviousPageCommand_CanExecute_ShouldReturnTrue_WhenNotOnFirstPage()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.CurrentPageNumber = 2;

        // Act
        var canExecute = _viewModel.GoToPreviousPageCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can go to previous page when not on first page");
    }

    [Fact]
    public void GoToPreviousPageCommand_CanExecute_ShouldReturnFalse_WhenLoading()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.CurrentPageNumber = 2;
        _viewModel.IsLoading = true;

        // Act
        var canExecute = _viewModel.GoToPreviousPageCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot navigate while loading");
    }

    [Fact]
    public void GoToNextPageCommand_CanExecute_ShouldReturnFalse_WhenOnLastPage()
    {
        // Arrange
        SetupLoadedDocument(totalPages: 5);
        _viewModel.CurrentPageNumber = 5;

        // Act
        var canExecute = _viewModel.GoToNextPageCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot go to next page when on last page");
    }

    [Fact]
    public void GoToNextPageCommand_CanExecute_ShouldReturnTrue_WhenNotOnLastPage()
    {
        // Arrange
        SetupLoadedDocument(totalPages: 5);
        _viewModel.CurrentPageNumber = 2;

        // Act
        var canExecute = _viewModel.GoToNextPageCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can go to next page when not on last page");
    }

    [Fact]
    public void GoToNextPageCommand_CanExecute_ShouldReturnFalse_WhenLoading()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.CurrentPageNumber = 1;
        _viewModel.IsLoading = true;

        // Act
        var canExecute = _viewModel.GoToNextPageCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot navigate while loading");
    }

    [Fact]
    public void ZoomInCommand_CanExecute_ShouldReturnFalse_WhenAtMaxZoom()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.ZoomLevel = 2.0;

        // Act
        var canExecute = _viewModel.ZoomInCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot zoom in beyond 200%");
    }

    [Fact]
    public void ZoomInCommand_CanExecute_ShouldReturnTrue_WhenNotAtMaxZoom()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.ZoomLevel = 1.0;

        // Act
        var canExecute = _viewModel.ZoomInCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can zoom in when not at max zoom");
    }

    [Fact]
    public void ZoomOutCommand_CanExecute_ShouldReturnFalse_WhenAtMinZoom()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.ZoomLevel = 0.5;

        // Act
        var canExecute = _viewModel.ZoomOutCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot zoom out beyond 50%");
    }

    [Fact]
    public void ZoomOutCommand_CanExecute_ShouldReturnTrue_WhenNotAtMinZoom()
    {
        // Arrange
        SetupLoadedDocument();
        _viewModel.ZoomLevel = 1.0;

        // Act
        var canExecute = _viewModel.ZoomOutCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can zoom out when not at min zoom");
    }

    [Fact]
    public void ResetZoomCommand_CanExecute_ShouldReturnFalse_WhenNoDocumentLoaded()
    {
        // Act
        var canExecute = _viewModel.ResetZoomCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("cannot reset zoom when no document loaded");
    }

    [Fact]
    public void ResetZoomCommand_CanExecute_ShouldReturnTrue_WhenDocumentLoaded()
    {
        // Arrange
        SetupLoadedDocument();

        // Act
        var canExecute = _viewModel.ResetZoomCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("can reset zoom when document loaded");
    }

    [Fact]
    public async Task ZoomInCommand_ShouldIncreaseZoomLevel()
    {
        // Arrange
        SetupLoadedDocument();
        SetupRenderingServiceSuccess();
        _viewModel.ZoomLevel = 1.0;

        // Act
        await _viewModel.ZoomInCommand.ExecuteAsync(null);

        // Assert
        _viewModel.ZoomLevel.Should().Be(1.25, "zoom should increase to next step");
    }

    [Fact]
    public async Task ZoomInCommand_ShouldProgressThroughZoomLevels()
    {
        // Arrange
        SetupLoadedDocument();
        SetupRenderingServiceSuccess();

        var zoomLevels = new[] { 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0 };

        // Act & Assert
        for (int i = 0; i < zoomLevels.Length - 1; i++)
        {
            _viewModel.ZoomLevel = zoomLevels[i];
            await _viewModel.ZoomInCommand.ExecuteAsync(null);
            _viewModel.ZoomLevel.Should().Be(zoomLevels[i + 1]);
        }
    }

    [Fact]
    public async Task ZoomOutCommand_ShouldDecreaseZoomLevel()
    {
        // Arrange
        SetupLoadedDocument();
        SetupRenderingServiceSuccess();
        _viewModel.ZoomLevel = 1.25;

        // Act
        await _viewModel.ZoomOutCommand.ExecuteAsync(null);

        // Assert
        _viewModel.ZoomLevel.Should().Be(1.0, "zoom should decrease to previous step");
    }

    [Fact]
    public async Task ZoomOutCommand_ShouldProgressThroughZoomLevels()
    {
        // Arrange
        SetupLoadedDocument();
        SetupRenderingServiceSuccess();

        var zoomLevels = new[] { 2.0, 1.75, 1.5, 1.25, 1.0, 0.75, 0.5 };

        // Act & Assert
        for (int i = 0; i < zoomLevels.Length - 1; i++)
        {
            _viewModel.ZoomLevel = zoomLevels[i];
            await _viewModel.ZoomOutCommand.ExecuteAsync(null);
            _viewModel.ZoomLevel.Should().Be(zoomLevels[i + 1]);
        }
    }

    [Fact]
    public async Task ResetZoomCommand_ShouldSetZoomToOneHundredPercent()
    {
        // Arrange
        SetupLoadedDocument();
        SetupRenderingServiceSuccess();
        _viewModel.ZoomLevel = 1.5;

        // Act
        await _viewModel.ResetZoomCommand.ExecuteAsync(null);

        // Assert
        _viewModel.ZoomLevel.Should().Be(1.0, "reset should return zoom to 100%");
    }

    [Fact]
    public void Dispose_ShouldCloseDocument()
    {
        // Arrange
        SetupLoadedDocument();

        // Act
        _viewModel.Dispose();

        // Assert
        _documentServiceMock.Verify(
            x => x.CloseDocument(It.IsAny<PdfDocument>()),
            Times.Once,
            "Dispose should close the document");
    }

    [Fact]
    public void Dispose_ShouldNotThrow_WhenCalledMultipleTimes()
    {
        // Arrange
        SetupLoadedDocument();

        // Act
        Action act = () =>
        {
            _viewModel.Dispose();
            _viewModel.Dispose();
        };

        // Assert
        act.Should().NotThrow("Dispose should be idempotent");
    }

    [Fact]
    public void Dispose_ShouldNotThrow_WhenNoDocumentLoaded()
    {
        // Act
        Action act = () => _viewModel.Dispose();

        // Assert
        act.Should().NotThrow("Dispose should handle null document gracefully");
    }

    [Fact]
    public void ViewModel_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that the ViewModel can be instantiated and tested
        // without requiring WinUI runtime (headless testing)

        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Should().BeAssignableTo<System.ComponentModel.INotifyPropertyChanged>();
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PdfViewerViewModel initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Constructor should log initialization");
    }

    /// <summary>
    /// Sets up the ViewModel to simulate a loaded document state.
    /// This is necessary for testing navigation and zoom commands.
    /// </summary>
    private void SetupLoadedDocument(int totalPages = 10)
    {
        // Create a mock PdfDocument
        var mockHandle = new Mock<SafePdfDocumentHandle>(true);
        var mockDocument = new PdfDocument
        {
            FilePath = "test.pdf",
            PageCount = totalPages,
            Handle = mockHandle.Object,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Use reflection to set private field _currentDocument
        var fieldInfo = typeof(PdfViewerViewModel).GetField("_currentDocument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo?.SetValue(_viewModel, mockDocument);

        // Set TotalPages to match the mock document
        _viewModel.TotalPages = totalPages;
    }

    /// <summary>
    /// Sets up the rendering service mock to return a successful result.
    /// </summary>
    private void SetupRenderingServiceSuccess()
    {
        var stream = new MemoryStream();
        _renderingServiceMock
            .Setup(x => x.RenderPageAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<double>()))
            .ReturnsAsync(Result.Ok<Stream>(stream));
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }
}
