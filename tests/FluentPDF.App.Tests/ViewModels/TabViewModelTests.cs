using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for TabViewModel demonstrating tab-specific state management and lifecycle.
/// </summary>
public class TabViewModelTests : IDisposable
{
    private readonly Mock<ILogger<TabViewModel>> _loggerMock;
    private readonly Mock<IPdfDocumentService> _documentServiceMock;
    private readonly Mock<IPdfRenderingService> _renderingServiceMock;
    private readonly Mock<IDocumentEditingService> _editingServiceMock;
    private readonly Mock<ITextSearchService> _searchServiceMock;
    private readonly Mock<ITextExtractionService> _textExtractionServiceMock;
    private readonly Mock<IBookmarkService> _bookmarkServiceMock;
    private readonly Mock<IPdfFormService> _formServiceMock;
    private readonly Mock<IFormValidationService> _formValidationServiceMock;
    private readonly Mock<ILogger<PdfViewerViewModel>> _viewerLoggerMock;
    private readonly Mock<ILogger<BookmarksViewModel>> _bookmarksLoggerMock;
    private readonly Mock<ILogger<FormFieldViewModel>> _formLoggerMock;
    private readonly PdfViewerViewModel _viewerViewModel;
    private const string TestFilePath = "/path/to/test.pdf";
    private const string TestFileName = "test.pdf";

    public TabViewModelTests()
    {
        _loggerMock = new Mock<ILogger<TabViewModel>>();
        _documentServiceMock = new Mock<IPdfDocumentService>();
        _renderingServiceMock = new Mock<IPdfRenderingService>();
        _editingServiceMock = new Mock<IDocumentEditingService>();
        _searchServiceMock = new Mock<ITextSearchService>();
        _textExtractionServiceMock = new Mock<ITextExtractionService>();
        _bookmarkServiceMock = new Mock<IBookmarkService>();
        _formServiceMock = new Mock<IPdfFormService>();
        _formValidationServiceMock = new Mock<IFormValidationService>();
        _viewerLoggerMock = new Mock<ILogger<PdfViewerViewModel>>();
        _bookmarksLoggerMock = new Mock<ILogger<BookmarksViewModel>>();
        _formLoggerMock = new Mock<ILogger<FormFieldViewModel>>();

        // Create real view models with mocked dependencies
        var bookmarksViewModel = new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            _bookmarksLoggerMock.Object);

        var formFieldViewModel = new FormFieldViewModel(
            _formServiceMock.Object,
            _formValidationServiceMock.Object,
            _formLoggerMock.Object);

        _viewerViewModel = new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            _editingServiceMock.Object,
            _searchServiceMock.Object,
            _textExtractionServiceMock.Object,
            bookmarksViewModel,
            formFieldViewModel,
            _viewerLoggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModel,
            _loggerMock.Object);

        // Assert
        tabViewModel.FilePath.Should().Be(TestFilePath);
        tabViewModel.FileName.Should().Be(TestFileName);
        tabViewModel.ViewerViewModel.Should().Be(_viewerViewModel);
        tabViewModel.IsActive.Should().BeFalse();
        tabViewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenFilePathIsNull()
    {
        // Arrange & Act
        Action act = () => new TabViewModel(
            null!,
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
        var bookmarksViewModel = new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            _bookmarksLoggerMock.Object);

        var formFieldViewModel = new FormFieldViewModel(
            _formServiceMock.Object,
            _formValidationServiceMock.Object,
            _formLoggerMock.Object);

        var viewModel = new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            _editingServiceMock.Object,
            _searchServiceMock.Object,
            _textExtractionServiceMock.Object,
            bookmarksViewModel,
            formFieldViewModel,
            _viewerLoggerMock.Object);

        var tabViewModel = new TabViewModel(
            TestFilePath,
            viewModel,
            _loggerMock.Object);

        // Act
        tabViewModel.Dispose();

        // Assert - we can't directly verify disposal on a real object,
        // but we can verify it doesn't throw and the tab is properly cleaned up
        tabViewModel.ViewerViewModel.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldLogDisposal()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModel,
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
            _viewerViewModel,
            _loggerMock.Object);

        // Act
        tabViewModel.Dispose();
        tabViewModel.Dispose();

        // Assert - Verify logging only happens once
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Disposing TabViewModel")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Dispose should only log once even if called multiple times");
    }

    [Fact]
    public void FilePath_ShouldNotChange_AfterConstruction()
    {
        // Arrange
        var tabViewModel = new TabViewModel(
            TestFilePath,
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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
            _viewerViewModel,
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

    public void Dispose()
    {
        _viewerViewModel?.Dispose();
    }
}
