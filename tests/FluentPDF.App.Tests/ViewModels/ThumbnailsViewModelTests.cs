using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for ThumbnailsViewModel demonstrating headless MVVM testing.
/// </summary>
public class ThumbnailsViewModelTests : IDisposable
{
    private readonly Mock<IThumbnailRenderingService> _thumbnailServiceMock;
    private readonly Mock<ILogger<ThumbnailsViewModel>> _loggerMock;
    private readonly ThumbnailsViewModel _viewModel;

    public ThumbnailsViewModelTests()
    {
        _thumbnailServiceMock = new Mock<IThumbnailRenderingService>();
        _loggerMock = new Mock<ILogger<ThumbnailsViewModel>>();
        _viewModel = CreateViewModel();

        // Clear messenger to avoid cross-test contamination
        WeakReferenceMessenger.Default.Cleanup();
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
        WeakReferenceMessenger.Default.Cleanup();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Thumbnails.Should().BeEmpty("no document loaded initially");
        viewModel.SelectedPageNumber.Should().Be(1, "default page number is 1");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenThumbnailServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new ThumbnailsViewModel(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("thumbnailService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new ThumbnailsViewModel(
            _thumbnailServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LoadThumbnailsAsync_ShouldCreateThumbnailItems()
    {
        // Arrange
        var document = CreateMockDocument(10);
        SetupMockRenderingService();

        // Act
        await _viewModel.LoadThumbnailsAsync(document);

        // Assert
        _viewModel.Thumbnails.Should().HaveCount(10);
        _viewModel.Thumbnails.Select(t => t.PageNumber).Should().BeEquivalentTo(Enumerable.Range(1, 10));
    }

    [Fact]
    public async Task LoadThumbnailsAsync_ShouldLoadFirst20Thumbnails()
    {
        // Arrange
        var document = CreateMockDocument(50);
        SetupMockRenderingService();

        // Act
        await _viewModel.LoadThumbnailsAsync(document);

        // Assert - first 20 should be rendered
        _thumbnailServiceMock.Verify(
            s => s.RenderThumbnailAsync(It.IsAny<PdfDocument>(), It.IsInRange(1, 20, Moq.Range.Inclusive)),
            Times.Exactly(20));

        // Pages beyond 20 should not be rendered yet
        _thumbnailServiceMock.Verify(
            s => s.RenderThumbnailAsync(It.IsAny<PdfDocument>(), It.IsInRange(21, 50, Moq.Range.Inclusive)),
            Times.Never);
    }

    [Fact]
    public async Task LoadThumbnailsAsync_ShouldCacheThumbnails()
    {
        // Arrange
        var document = CreateMockDocument(5);
        SetupMockRenderingService();

        // Act - load twice
        await _viewModel.LoadThumbnailsAsync(document);
        _thumbnailServiceMock.Invocations.Clear();
        await _viewModel.LoadThumbnailsAsync(document);

        // Assert - second load should use cache for first 5 pages
        _thumbnailServiceMock.Verify(
            s => s.RenderThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>()),
            Times.Exactly(5), // Only rendering the new batch
            "thumbnails should be cached and not re-rendered");
    }

    [Fact]
    public async Task LoadThumbnailsAsync_ShouldHandleRenderingFailure()
    {
        // Arrange
        var document = CreateMockDocument(3);
        _thumbnailServiceMock
            .Setup(s => s.RenderThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Fail("Rendering failed"));

        // Act
        await _viewModel.LoadThumbnailsAsync(document);

        // Assert
        _viewModel.Thumbnails.Should().HaveCount(3);
        _viewModel.Thumbnails.Should().OnlyContain(t => t.Thumbnail == null, "rendering failed");
        _viewModel.Thumbnails.Should().OnlyContain(t => !t.IsLoading, "loading should be complete");
    }

    [Fact]
    public void NavigateToPageCommand_ShouldSendMessage()
    {
        // Arrange
        NavigateToPageMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) => receivedMessage = m);

        // Act
        _viewModel.NavigateToPageCommand.Execute(5);

        // Assert
        receivedMessage.Should().NotBeNull();
        receivedMessage!.PageNumber.Should().Be(5);
    }

    [Fact]
    public async Task NavigateToPageCommand_ShouldUpdateSelection()
    {
        // Arrange
        var document = CreateMockDocument(5);
        SetupMockRenderingService();
        await _viewModel.LoadThumbnailsAsync(document);

        // Act
        _viewModel.NavigateToPageCommand.Execute(3);

        // Assert
        _viewModel.SelectedPageNumber.Should().Be(3);
        _viewModel.Thumbnails[0].IsSelected.Should().BeFalse();
        _viewModel.Thumbnails[1].IsSelected.Should().BeFalse();
        _viewModel.Thumbnails[2].IsSelected.Should().BeTrue();
        _viewModel.Thumbnails[3].IsSelected.Should().BeFalse();
        _viewModel.Thumbnails[4].IsSelected.Should().BeFalse();
    }

    [Fact]
    public void NavigateToPageCommand_ShouldIgnoreInvalidPageNumber()
    {
        // Arrange
        var document = CreateMockDocument(5);
        NavigateToPageMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) => receivedMessage = m);

        // Act - navigate to invalid page
        _viewModel.NavigateToPageCommand.Execute(0);
        _viewModel.NavigateToPageCommand.Execute(6);

        // Assert
        receivedMessage.Should().BeNull("invalid page numbers should be ignored");
    }

    [Fact]
    public async Task UpdateSelectedPage_ShouldUpdateSelectionState()
    {
        // Arrange
        var document = CreateMockDocument(5);
        SetupMockRenderingService();
        await _viewModel.LoadThumbnailsAsync(document);

        // Act
        _viewModel.UpdateSelectedPage(4);

        // Assert
        _viewModel.SelectedPageNumber.Should().Be(4);
        _viewModel.Thumbnails[3].IsSelected.Should().BeTrue();
        _viewModel.Thumbnails.Where((t, i) => i != 3).Should().OnlyContain(t => !t.IsSelected);
    }

    [Fact]
    public async Task UpdateSelectedPage_ShouldPreventNavigationLoop()
    {
        // Arrange
        var document = CreateMockDocument(5);
        SetupMockRenderingService();
        await _viewModel.LoadThumbnailsAsync(document);
        _viewModel.UpdateSelectedPage(3);

        var messageCount = 0;
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) => messageCount++);

        // Act - update to same page
        _viewModel.UpdateSelectedPage(3);

        // Assert
        messageCount.Should().Be(0, "should not send message when page hasn't changed");
    }

    [Fact]
    public async Task LoadVisibleThumbnailsAsync_ShouldLoadSpecifiedRange()
    {
        // Arrange
        var document = CreateMockDocument(50);
        SetupMockRenderingService();
        await _viewModel.LoadThumbnailsAsync(document);
        _thumbnailServiceMock.Invocations.Clear();

        // Act - load thumbnails 20-30
        await _viewModel.LoadVisibleThumbnailsAsync(20, 30);

        // Assert
        _thumbnailServiceMock.Verify(
            s => s.RenderThumbnailAsync(It.IsAny<PdfDocument>(), It.IsInRange(21, 30, Moq.Range.Inclusive)),
            Times.Exactly(10));
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Dispose();

        // Assert - no exception should be thrown
        // Further operations should not throw
        Action act = () => viewModel.Dispose();
        act.Should().NotThrow("disposing twice should be safe");
    }

    private ThumbnailsViewModel CreateViewModel()
    {
        return new ThumbnailsViewModel(
            _thumbnailServiceMock.Object,
            _loggerMock.Object);
    }

    private PdfDocument CreateMockDocument(int pageCount)
    {
        var docMock = new Mock<PdfDocument>();
        docMock.Setup(d => d.PageCount).Returns(pageCount);
        return docMock.Object;
    }

    private void SetupMockRenderingService()
    {
        _thumbnailServiceMock
            .Setup(s => s.RenderThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
                return Result.Ok<Stream>(stream);
            });
    }
}
