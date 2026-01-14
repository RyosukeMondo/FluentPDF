using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for BookmarksViewModel demonstrating headless MVVM testing.
/// </summary>
public class BookmarksViewModelTests
{
    private readonly Mock<IBookmarkService> _bookmarkServiceMock;
    private readonly Mock<PdfViewerViewModel> _pdfViewerViewModelMock;
    private readonly Mock<ILogger<BookmarksViewModel>> _loggerMock;

    public BookmarksViewModelTests()
    {
        _bookmarkServiceMock = new Mock<IBookmarkService>();
        _pdfViewerViewModelMock = new Mock<PdfViewerViewModel>(
            Mock.Of<Core.Services.IPdfDocumentService>(),
            Mock.Of<Core.Services.IPdfRenderingService>(),
            Mock.Of<ILogger<PdfViewerViewModel>>());
        _loggerMock = new Mock<ILogger<BookmarksViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Bookmarks.Should().BeNull("no document is loaded initially");
        viewModel.IsPanelVisible.Should().BeTrue("panel should be visible by default");
        viewModel.PanelWidth.Should().Be(250, "default panel width");
        viewModel.IsLoading.Should().BeFalse("not loading initially");
        viewModel.EmptyMessage.Should().Be("No bookmarks in this document");
        viewModel.SelectedBookmark.Should().BeNull("no bookmark selected initially");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenBookmarkServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new BookmarksViewModel(
            null!,
            _pdfViewerViewModelMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("bookmarkService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenPdfViewerViewModelIsNull()
    {
        // Arrange & Act
        Action act = () => new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pdfViewerViewModel");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            _pdfViewerViewModelMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LoadBookmarksCommand_ShouldPopulateBookmarks_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testBookmarks = CreateTestBookmarks();

        _bookmarkServiceMock
            .Setup(x => x.ExtractBookmarksAsync(document))
            .ReturnsAsync(Result.Ok(testBookmarks));

        // Act
        await viewModel.LoadBookmarksCommand.ExecuteAsync(document);

        // Assert
        viewModel.Bookmarks.Should().NotBeNull();
        viewModel.Bookmarks.Should().HaveCount(2);
        viewModel.Bookmarks![0].Title.Should().Be("Chapter 1");
        viewModel.Bookmarks[1].Title.Should().Be("Chapter 2");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadBookmarksCommand_ShouldSetEmptyList_WhenServiceFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();

        _bookmarkServiceMock
            .Setup(x => x.ExtractBookmarksAsync(document))
            .ReturnsAsync(Result.Fail("Extraction failed"));

        // Act
        await viewModel.LoadBookmarksCommand.ExecuteAsync(document);

        // Assert
        viewModel.Bookmarks.Should().NotBeNull();
        viewModel.Bookmarks.Should().BeEmpty();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadBookmarksCommand_ShouldSetIsLoadingToTrue_DuringExecution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var wasLoadingDuringExecution = false;

        _bookmarkServiceMock
            .Setup(x => x.ExtractBookmarksAsync(document))
            .ReturnsAsync(() =>
            {
                wasLoadingDuringExecution = viewModel.IsLoading;
                return Result.Ok(new List<BookmarkNode>());
            });

        // Act
        await viewModel.LoadBookmarksCommand.ExecuteAsync(document);

        // Assert
        wasLoadingDuringExecution.Should().BeTrue("IsLoading should be set during execution");
        viewModel.IsLoading.Should().BeFalse("IsLoading should be reset after execution");
    }

    [Fact]
    public async Task LoadBookmarksCommand_ShouldHandleNullDocument_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadBookmarksCommand.ExecuteAsync(null!);

        // Assert
        viewModel.Bookmarks.Should().BeNull("bookmarks should remain null when document is null");
    }

    [Fact]
    public async Task LoadBookmarksCommand_ShouldHandleException_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();

        _bookmarkServiceMock
            .Setup(x => x.ExtractBookmarksAsync(document))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        Func<Task> act = async () => await viewModel.LoadBookmarksCommand.ExecuteAsync(document);

        // Assert
        await act.Should().NotThrowAsync("command should handle exceptions gracefully");
        viewModel.Bookmarks.Should().NotBeNull();
        viewModel.Bookmarks.Should().BeEmpty();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void TogglePanelCommand_ShouldTogglePanelVisibility()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var initialState = viewModel.IsPanelVisible;

        // Act
        viewModel.TogglePanelCommand.Execute(null);

        // Assert
        viewModel.IsPanelVisible.Should().Be(!initialState);
    }

    [Fact]
    public void TogglePanelCommand_ShouldToggleMultipleTimes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.IsPanelVisible.Should().BeTrue();
        viewModel.TogglePanelCommand.Execute(null);
        viewModel.IsPanelVisible.Should().BeFalse();
        viewModel.TogglePanelCommand.Execute(null);
        viewModel.IsPanelVisible.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateToBookmarkCommand_ShouldCallGoToPageAsync_WhenBookmarkHasPageNumber()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var bookmark = new BookmarkNode
        {
            Title = "Test Bookmark",
            PageNumber = 5
        };

        // Act
        await viewModel.NavigateToBookmarkCommand.ExecuteAsync(bookmark);

        // Assert
        _pdfViewerViewModelMock.Verify(
            x => x.GoToPageCommand.ExecuteAsync(5),
            Times.Once,
            "GoToPageCommand should be called with the bookmark's page number");
        viewModel.SelectedBookmark.Should().Be(bookmark);
    }

    [Fact]
    public async Task NavigateToBookmarkCommand_ShouldNotNavigate_WhenBookmarkHasNoPageNumber()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var bookmark = new BookmarkNode
        {
            Title = "Test Bookmark",
            PageNumber = null
        };

        // Act
        await viewModel.NavigateToBookmarkCommand.ExecuteAsync(bookmark);

        // Assert
        _pdfViewerViewModelMock.Verify(
            x => x.GoToPageCommand.ExecuteAsync(It.IsAny<int>()),
            Times.Never,
            "GoToPageCommand should not be called when bookmark has no page number");
    }

    [Fact]
    public async Task NavigateToBookmarkCommand_ShouldHandleNullBookmark_Gracefully()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.NavigateToBookmarkCommand.ExecuteAsync(null!);

        // Assert
        _pdfViewerViewModelMock.Verify(
            x => x.GoToPageCommand.ExecuteAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void IsPanelVisible_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(BookmarksViewModel.IsPanelVisible))
                eventRaised = true;
        };

        // Act
        viewModel.IsPanelVisible = false;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.IsPanelVisible.Should().BeFalse();
    }

    [Fact]
    public void PanelWidth_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(BookmarksViewModel.PanelWidth))
                eventRaised = true;
        };

        // Act
        viewModel.PanelWidth = 300;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.PanelWidth.Should().Be(300);
    }

    [Fact]
    public void PanelWidth_ShouldBeClampedToMinimum()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.PanelWidth = 100;

        // Assert
        viewModel.PanelWidth.Should().Be(150, "width should be clamped to minimum of 150");
    }

    [Fact]
    public void PanelWidth_ShouldBeClampedToMaximum()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.PanelWidth = 700;

        // Assert
        viewModel.PanelWidth.Should().Be(600, "width should be clamped to maximum of 600");
    }

    [Fact]
    public void Bookmarks_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(BookmarksViewModel.Bookmarks))
                eventRaised = true;
        };

        // Act
        viewModel.Bookmarks = new List<BookmarkNode>();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void SelectedBookmark_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(BookmarksViewModel.SelectedBookmark))
                eventRaised = true;
        };

        var bookmark = new BookmarkNode { Title = "Test" };

        // Act
        viewModel.SelectedBookmark = bookmark;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.SelectedBookmark.Should().Be(bookmark);
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
    public async Task LoadBookmarksCommand_ShouldLogBookmarkCount()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var document = CreateTestDocument();
        var testBookmarks = CreateTestBookmarks();

        _bookmarkServiceMock
            .Setup(x => x.ExtractBookmarksAsync(document))
            .ReturnsAsync(Result.Ok(testBookmarks));

        // Act
        await viewModel.LoadBookmarksCommand.ExecuteAsync(document);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loaded") && v.ToString()!.Contains("bookmarks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Should log bookmark loading");
    }

    private BookmarksViewModel CreateViewModel()
    {
        return new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            _pdfViewerViewModelMock.Object,
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

    private List<BookmarkNode> CreateTestBookmarks()
    {
        return new List<BookmarkNode>
        {
            new BookmarkNode
            {
                Title = "Chapter 1",
                PageNumber = 1,
                Children = new List<BookmarkNode>
                {
                    new BookmarkNode { Title = "Section 1.1", PageNumber = 2 },
                    new BookmarkNode { Title = "Section 1.2", PageNumber = 5 }
                }
            },
            new BookmarkNode
            {
                Title = "Chapter 2",
                PageNumber = 10
            }
        };
    }
}
