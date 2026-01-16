using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for MainViewModel tab management functionality.
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<ILogger<MainViewModel>> _loggerMock;
    private readonly Mock<ILogger<TabViewModel>> _tabLoggerMock;
    private readonly Mock<ILogger<PdfViewerViewModel>> _viewerLoggerMock;
    private readonly Mock<IRecentFilesService> _recentFilesServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<PdfViewerViewModel> _viewerViewModelMock;

    public MainViewModelTests()
    {
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _tabLoggerMock = new Mock<ILogger<TabViewModel>>();
        _viewerLoggerMock = new Mock<ILogger<PdfViewerViewModel>>();
        _recentFilesServiceMock = new Mock<IRecentFilesService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _viewerViewModelMock = new Mock<PdfViewerViewModel>();

        // Setup service provider to return mocked dependencies
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(PdfViewerViewModel)))
            .Returns(_viewerViewModelMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<TabViewModel>)))
            .Returns(_tabLoggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Assert
        viewModel.Tabs.Should().NotBeNull();
        viewModel.Tabs.Should().BeEmpty();
        viewModel.ActiveTab.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new MainViewModel(
            null!,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenRecentFilesServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new MainViewModel(
            _loggerMock.Object,
            null!,
            _serviceProviderMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("recentFilesService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenServiceProviderIsNull()
    {
        // Arrange & Act
        Action act = () => new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void CloseTab_ShouldRemoveTabFromCollection()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        var viewerViewModel = CreateMockViewerViewModel();
        var tab = new TabViewModel("/path/to/file.pdf", viewerViewModel, _tabLoggerMock.Object);
        viewModel.Tabs.Add(tab);

        // Act
        viewModel.CloseTabCommand.Execute(tab);

        // Assert
        viewModel.Tabs.Should().NotContain(tab);
    }

    [Fact]
    public void CloseTab_ShouldActivateNextTab_WhenClosingActiveTab()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        var viewer1 = CreateMockViewerViewModel();
        var viewer2 = CreateMockViewerViewModel();
        var tab1 = new TabViewModel("/path/to/file1.pdf", viewer1, _tabLoggerMock.Object);
        var tab2 = new TabViewModel("/path/to/file2.pdf", viewer2, _tabLoggerMock.Object);

        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);

        // Set tab1 as active
        viewModel.GetType()
            .GetMethod("ActivateTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .Invoke(viewModel, new object[] { tab1 });

        // Act
        viewModel.CloseTabCommand.Execute(tab1);

        // Assert
        viewModel.ActiveTab.Should().Be(tab2);
        viewModel.Tabs.Should().NotContain(tab1);
        viewModel.Tabs.Should().Contain(tab2);
    }

    [Fact]
    public void CloseTab_ShouldSetActiveTabToNull_WhenClosingLastTab()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        var viewerViewModel = CreateMockViewerViewModel();
        var tab = new TabViewModel("/path/to/file.pdf", viewerViewModel, _tabLoggerMock.Object);
        viewModel.Tabs.Add(tab);

        // Set as active
        viewModel.GetType()
            .GetMethod("ActivateTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .Invoke(viewModel, new object[] { tab });

        // Act
        viewModel.CloseTabCommand.Execute(tab);

        // Assert
        viewModel.ActiveTab.Should().BeNull();
        viewModel.Tabs.Should().BeEmpty();
    }

    [Fact]
    public void CloseTab_ShouldThrowException_WhenTabIsNull()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Act
        Action act = () => viewModel.CloseTabCommand.Execute(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetRecentFiles_ShouldReturnFilesFromService()
    {
        // Arrange
        var recentFiles = new List<RecentFileEntry>
        {
            new() { FilePath = "/path/to/file1.pdf", LastAccessed = DateTime.UtcNow },
            new() { FilePath = "/path/to/file2.pdf", LastAccessed = DateTime.UtcNow.AddMinutes(-5) }
        };

        _recentFilesServiceMock
            .Setup(s => s.GetRecentFiles())
            .Returns(recentFiles);

        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Act
        var result = viewModel.GetRecentFiles();

        // Assert
        result.Should().BeEquivalentTo(recentFiles);
        _recentFilesServiceMock.Verify(s => s.GetRecentFiles(), Times.Once);
    }

    [Fact]
    public void ClearRecentFiles_ShouldCallService()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Act
        viewModel.ClearRecentFilesCommand.Execute(null);

        // Assert
        _recentFilesServiceMock.Verify(s => s.ClearRecentFiles(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeAllTabs()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        var viewer1 = CreateMockViewerViewModel();
        var viewer2 = CreateMockViewerViewModel();
        var tab1 = new TabViewModel("/path/to/file1.pdf", viewer1, _tabLoggerMock.Object);
        var tab2 = new TabViewModel("/path/to/file2.pdf", viewer2, _tabLoggerMock.Object);

        viewModel.Tabs.Add(tab1);
        viewModel.Tabs.Add(tab2);

        // Act
        viewModel.Dispose();

        // Assert
        viewModel.Tabs.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Act
        viewModel.Dispose();
        Action act = () => viewModel.Dispose();

        // Assert
        act.Should().NotThrow("Dispose should be idempotent");
    }

    [Fact]
    public void ActiveTab_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ActiveTab))
                eventRaised = true;
        };

        var viewerViewModel = CreateMockViewerViewModel();
        var tab = new TabViewModel("/path/to/file.pdf", viewerViewModel, _tabLoggerMock.Object);

        // Act
        viewModel.GetType()
            .GetProperty("ActiveTab")?
            .SetValue(viewModel, tab);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange & Act
        var viewModel = new MainViewModel(
            _loggerMock.Object,
            _recentFilesServiceMock.Object,
            _serviceProviderMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MainViewModel initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Constructor should log initialization");
    }

    private PdfViewerViewModel CreateMockViewerViewModel()
    {
        // Create a real instance with mocked dependencies for testing
        var documentServiceMock = new Mock<IPdfDocumentService>();
        var renderingServiceMock = new Mock<IPdfRenderingService>();
        var editingServiceMock = new Mock<IDocumentEditingService>();
        var searchServiceMock = new Mock<ITextSearchService>();
        var textExtractionServiceMock = new Mock<ITextExtractionService>();
        var bookmarksViewModelMock = new Mock<BookmarksViewModel>(
            Mock.Of<ILogger<BookmarksViewModel>>());
        var formFieldViewModelMock = new Mock<FormFieldViewModel>(
            Mock.Of<ILogger<FormFieldViewModel>>());

        return new PdfViewerViewModel(
            documentServiceMock.Object,
            renderingServiceMock.Object,
            editingServiceMock.Object,
            searchServiceMock.Object,
            textExtractionServiceMock.Object,
            bookmarksViewModelMock.Object,
            formFieldViewModelMock.Object,
            Mock.Of<FluentPDF.App.Services.RenderingCoordinator>(),
            Mock.Of<FluentPDF.App.Services.UIBindingVerifier>(),
            _viewerLoggerMock.Object);
    }
}
