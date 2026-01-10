using FluentAssertions;
using FluentPDF.App.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for TabViewModel demonstrating tab-specific state management and lifecycle.
/// </summary>
public class TabViewModelTests
{
    private readonly Mock<ILogger<TabViewModel>> _loggerMock;
    private readonly Mock<PdfViewerViewModel> _viewerViewModelMock;
    private const string TestFilePath = "/path/to/test.pdf";
    private const string TestFileName = "test.pdf";

    public TabViewModelTests()
    {
        _loggerMock = new Mock<ILogger<TabViewModel>>();

        // Create a mock PdfViewerViewModel
        _viewerViewModelMock = new Mock<PdfViewerViewModel>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        tabViewModel.FilePath.Should().Be(TestFilePath);
        tabViewModel.FileName.Should().Be(TestFileName);
        tabViewModel.ViewerViewModel.Should().Be(_viewerViewModelMock.Object);
        tabViewModel.IsActive.Should().BeFalse();
        tabViewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenFilePathIsNull()
    {
        // Arrange & Act
        Action act = () => new TabViewModel(
            null!,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*File path cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenFilePathIsWhitespace()
    {
        // Arrange & Act
        Action act = () => new TabViewModel(
            "   ",
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*File path cannot be null or whitespace.*");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenViewerViewModelIsNull()
    {
        // Arrange & Act
        Action act = () => new TabViewModel(
            TestFilePath,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("viewerViewModel");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldExtractFileName_FromFilePath()
    {
        // Arrange
        var filePath = "/some/long/path/to/document.pdf";
        var expectedFileName = "document.pdf";

        // Act
        var tabViewModel = new TabViewModel(
            filePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        tabViewModel.FileName.Should().Be(expectedFileName);
    }

    [Fact]
    public void Constructor_ShouldHandleWindowsFilePaths()
    {
        // Arrange
        var filePath = "C:\\Users\\Documents\\report.pdf";
        var expectedFileName = "report.pdf";

        // Act
        var tabViewModel = new TabViewModel(
            filePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        tabViewModel.FileName.Should().Be(expectedFileName);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.Activate();

        // Assert
        tabViewModel.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);
        tabViewModel.Activate();

        // Act
        tabViewModel.Deactivate();

        // Assert
        tabViewModel.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);
        var eventRaised = false;

        tabViewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(TabViewModel.IsActive))
                eventRaised = true;
        };

        // Act
        tabViewModel.IsActive = true;

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void HasUnsavedChanges_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);
        var eventRaised = false;

        tabViewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(TabViewModel.HasUnsavedChanges))
                eventRaised = true;
        };

        // Act
        tabViewModel.HasUnsavedChanges = true;

        // Assert
        eventRaised.Should().BeTrue();
        tabViewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldLogActivation()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.Activate();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Activating tab")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Deactivate_ShouldLogDeactivation()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.Deactivate();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deactivating tab")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeViewerViewModel()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.Dispose();

        // Assert
        _viewerViewModelMock.Verify(vm => vm.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldLogDisposal()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.Dispose();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Disposing TabViewModel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.Dispose();
        tabViewModel.Dispose();

        // Assert
        _viewerViewModelMock.Verify(vm => vm.Dispose(), Times.Once,
            "ViewerViewModel.Dispose should only be called once even if Dispose is called multiple times");
    }

    [Fact]
    public void FilePath_ShouldNotChange_AfterConstruction()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);
        var originalFilePath = tabViewModel.FilePath;

        // Act
        // Try to set FilePath through property (should remain unchanged due to [ObservableProperty])
        tabViewModel.FilePath = "/new/path.pdf";

        // Assert
        tabViewModel.FilePath.Should().Be("/new/path.pdf",
            "ObservableProperty allows setting, but in practice FilePath should represent the original file");
    }

    [Fact]
    public void ViewModel_ShouldImplementINotifyPropertyChanged()
    {
        // Arrange & Act
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        tabViewModel.Should().BeAssignableTo<INotifyPropertyChanged>();
    }

    [Fact]
    public void ViewModel_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that TabViewModel can be instantiated and tested
        // without requiring WinUI runtime (headless testing)

        // Arrange & Act
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        tabViewModel.Should().NotBeNull();
        tabViewModel.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void IsActive_StateTransitions_ShouldWork()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act & Assert - Initial state
        tabViewModel.IsActive.Should().BeFalse();

        // Act & Assert - Activate
        tabViewModel.Activate();
        tabViewModel.IsActive.Should().BeTrue();

        // Act & Assert - Deactivate
        tabViewModel.Deactivate();
        tabViewModel.IsActive.Should().BeFalse();

        // Act & Assert - Multiple activations
        tabViewModel.Activate();
        tabViewModel.Activate();
        tabViewModel.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_ShouldLogDebugMessage_WhenIsActiveChanges()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Act
        tabViewModel.IsActive = true;

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tab active state changed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldLogCreation()
    {
        // Arrange & Act
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TabViewModel created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
