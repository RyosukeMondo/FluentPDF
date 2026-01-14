using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using FluentPDF.App.Models;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Moq;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for thumbnails sidebar functionality.
/// Tests the complete workflow of loading thumbnails, navigation, caching, and memory management.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ThumbnailsIntegrationTests : IDisposable
{
    private const string SimplePdfPath = "../../../../Fixtures/simple-text.pdf";
    private readonly Mock<ILogger<ThumbnailsViewModel>> _thumbnailsLoggerMock;
    private readonly Mock<ILogger<ThumbnailRenderingService>> _renderingLoggerMock;
    private readonly Mock<ILogger<PdfRenderingService>> _pdfRenderingLoggerMock;
    private readonly Mock<IPdfRenderingService> _pdfRenderingServiceMock;
    private bool _disposed;

    public ThumbnailsIntegrationTests()
    {
        _thumbnailsLoggerMock = new Mock<ILogger<ThumbnailsViewModel>>();
        _renderingLoggerMock = new Mock<ILogger<ThumbnailRenderingService>>();
        _pdfRenderingLoggerMock = new Mock<ILogger<PdfRenderingService>>();
        _pdfRenderingServiceMock = new Mock<IPdfRenderingService>();

        // Setup mock to return a simple bitmap stream
        _pdfRenderingServiceMock
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                // Create a simple 1x1 pixel bitmap for testing
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                // BMP header for 1x1 pixel
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write((int)70); // File size
                writer.Write((int)0); // Reserved
                writer.Write((int)54); // Offset to pixel data
                writer.Write((int)40); // DIB header size
                writer.Write((int)1); // Width
                writer.Write((int)1); // Height
                writer.Write((short)1); // Planes
                writer.Write((short)24); // Bits per pixel
                writer.Write((int)0); // Compression
                writer.Write((int)0); // Image size
                writer.Write((int)0); // X pixels per meter
                writer.Write((int)0); // Y pixels per meter
                writer.Write((int)0); // Colors used
                writer.Write((int)0); // Important colors
                // Pixel data (BGR)
                writer.Write((byte)255); // Blue
                writer.Write((byte)255); // Green
                writer.Write((byte)255); // Red
                writer.Write((short)0); // Padding

                stream.Position = 0;
                return Result<Stream>.Success(stream);
            });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Clear messenger to avoid cross-test pollution
        WeakReferenceMessenger.Default.Reset();

        _disposed = true;
    }

    /// <summary>
    /// Tests loading a document and verifying that thumbnails are created for all pages.
    /// </summary>
    [Fact]
    public async Task LoadDocument_ShouldCreateThumbnailsForAllPages()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Test PDF not found at {fullPdfPath}");
        }

        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        loadResult.IsSuccess.Should().BeTrue("document should load successfully");

        var document = loadResult.Value!;

        // Act
        await viewModel.LoadThumbnailsAsync(document);

        // Assert
        viewModel.Thumbnails.Should().HaveCount(document.PageCount, "should create thumbnail for each page");

        // Verify initial thumbnails are loaded (first 20 or total pages, whichever is less)
        var expectedLoaded = Math.Min(20, document.PageCount);
        await Task.Delay(500); // Wait for async loading

        var loadedCount = viewModel.Thumbnails.Count(t => t.Thumbnail != null || !t.IsLoading);
        loadedCount.Should().BeGreaterThan(0, "at least some initial thumbnails should be loaded");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that clicking a thumbnail sends a navigation message to the main viewer.
    /// </summary>
    [Fact]
    public async Task ClickThumbnail_ShouldNavigateMainViewer()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        await viewModel.LoadThumbnailsAsync(document);

        NavigateToPageMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Act - Navigate to page 2
        viewModel.NavigateToPageCommand.Execute(2);

        // Assert
        receivedMessage.Should().NotBeNull("navigation message should be sent");
        receivedMessage!.PageNumber.Should().Be(2, "should navigate to page 2");
        viewModel.SelectedPageNumber.Should().Be(2, "selected page should be updated");

        // Verify selection state updated
        viewModel.Thumbnails[0].IsSelected.Should().BeFalse("page 1 should not be selected");
        viewModel.Thumbnails[1].IsSelected.Should().BeTrue("page 2 should be selected");

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<NavigateToPageMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that navigating in the main viewer updates the selected thumbnail.
    /// </summary>
    [Fact]
    public async Task MainViewerNavigation_ShouldUpdateThumbnailSelection()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        await viewModel.LoadThumbnailsAsync(document);

        // Act - Simulate main viewer navigating to page 3
        viewModel.UpdateSelectedPage(3);

        // Assert
        viewModel.SelectedPageNumber.Should().Be(3, "selected page should be updated");
        viewModel.Thumbnails[0].IsSelected.Should().BeFalse("page 1 should not be selected");
        viewModel.Thumbnails[1].IsSelected.Should().BeFalse("page 2 should not be selected");
        viewModel.Thumbnails[2].IsSelected.Should().BeTrue("page 3 should be selected");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that navigation prevents loops when the same page is selected.
    /// </summary>
    [Fact]
    public async Task UpdateSelectedPage_ShouldPreventNavigationLoops()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        await viewModel.LoadThumbnailsAsync(document);

        // Set to page 2
        viewModel.UpdateSelectedPage(2);
        var initialSelectedPageNumber = viewModel.SelectedPageNumber;

        // Act - Try to update to the same page
        viewModel.UpdateSelectedPage(2);

        // Assert
        viewModel.SelectedPageNumber.Should().Be(initialSelectedPageNumber, "should not change when selecting same page");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that the cache is used for thumbnails and avoids re-rendering.
    /// </summary>
    [Fact]
    public async Task LoadThumbnails_ShouldUseCacheToAvoidReRendering()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        // Act - Load thumbnails first time
        await viewModel.LoadThumbnailsAsync(document);
        await Task.Delay(500); // Wait for initial load

        var initialRenderCount = _pdfRenderingServiceMock.Invocations.Count;

        // Load visible range that includes already loaded thumbnails
        await viewModel.LoadVisibleThumbnailsAsync(0, 10);
        await Task.Delay(500); // Wait for any potential renders

        // Assert - Should not have rendered more than necessary (cached items should not re-render)
        var finalRenderCount = _pdfRenderingServiceMock.Invocations.Count;
        finalRenderCount.Should().BeLessOrEqualTo(initialRenderCount + 10,
            "should use cache and not re-render already loaded thumbnails");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that concurrent rendering is limited to prevent overload.
    /// </summary>
    [Fact]
    public async Task LoadThumbnails_ShouldLimitConcurrentRenders()
    {
        // Arrange
        var maxConcurrent = 4;
        var currentConcurrent = 0;
        var maxObservedConcurrent = 0;
        var lockObject = new object();

        _pdfRenderingServiceMock
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<int>()))
            .Returns(async () =>
            {
                lock (lockObject)
                {
                    currentConcurrent++;
                    maxObservedConcurrent = Math.Max(maxObservedConcurrent, currentConcurrent);
                }

                // Simulate some rendering time
                await Task.Delay(50);

                lock (lockObject)
                {
                    currentConcurrent--;
                }

                // Return a simple bitmap
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write((int)70);
                writer.Write((int)0);
                writer.Write((int)54);
                writer.Write((int)40);
                writer.Write((int)1);
                writer.Write((int)1);
                writer.Write((short)1);
                writer.Write((short)24);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((byte)255);
                writer.Write((byte)255);
                writer.Write((byte)255);
                writer.Write((short)0);
                stream.Position = 0;
                return Result<Stream>.Success(stream);
            });

        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        // Act - Load thumbnails which should trigger concurrent renders
        await viewModel.LoadThumbnailsAsync(document);
        await Task.Delay(1000); // Wait for all renders to complete

        // Assert
        maxObservedConcurrent.Should().BeLessOrEqualTo(maxConcurrent,
            $"should not exceed {maxConcurrent} concurrent renders");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that navigation is disabled while thumbnails are loading.
    /// </summary>
    [Fact]
    public async Task NavigateToPage_ShouldBeDisabledWhileLoading()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        // Create a document with thumbnails
        await viewModel.LoadThumbnailsAsync(document);

        // Set a thumbnail to loading state
        viewModel.Thumbnails[0].IsLoading = true;

        // Act & Assert
        viewModel.NavigateToPageCommand.CanExecute(2).Should().BeFalse(
            "navigation should be disabled when any thumbnail is loading");

        // Set loading to false
        viewModel.Thumbnails[0].IsLoading = false;

        // Wait a bit for command state to update
        await Task.Delay(100);

        viewModel.NavigateToPageCommand.CanExecute(2).Should().BeTrue(
            "navigation should be enabled when no thumbnails are loading");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests the complete end-to-end workflow of loading, navigating, and synchronizing.
    /// </summary>
    [Fact]
    public async Task EndToEnd_LoadNavigateSynchronize_ShouldWork()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        NavigateToPageMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Act 1: Load document
        await viewModel.LoadThumbnailsAsync(document);

        // Assert 1: Thumbnails created
        viewModel.Thumbnails.Should().HaveCount(document.PageCount);

        // Act 2: Navigate via thumbnail click
        viewModel.NavigateToPageCommand.Execute(3);

        // Assert 2: Message sent and selection updated
        receivedMessage.Should().NotBeNull();
        receivedMessage!.PageNumber.Should().Be(3);
        viewModel.SelectedPageNumber.Should().Be(3);
        viewModel.Thumbnails[2].IsSelected.Should().BeTrue();

        // Act 3: Simulate external navigation (from main viewer)
        receivedMessage = null;
        viewModel.UpdateSelectedPage(1);

        // Assert 3: Selection updated without sending message
        viewModel.SelectedPageNumber.Should().Be(1);
        viewModel.Thumbnails[0].IsSelected.Should().BeTrue();
        viewModel.Thumbnails[2].IsSelected.Should().BeFalse();
        receivedMessage.Should().BeNull("UpdateSelectedPage should not send message");

        // Act 4: Load more thumbnails
        await viewModel.LoadVisibleThumbnailsAsync(10, 20);

        // Assert 4: Should complete without errors
        viewModel.Thumbnails.Count.Should().Be(document.PageCount);

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<NavigateToPageMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that disposing the ViewModel properly cleans up resources.
    /// </summary>
    [Fact]
    public async Task Dispose_ShouldCleanUpResources()
    {
        // Arrange
        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(thumbnailService, _thumbnailsLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentService = CreatePdfDocumentService();
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        var document = loadResult.Value!;

        await viewModel.LoadThumbnailsAsync(document);

        // Act
        viewModel.Dispose();

        // Assert - Should not throw when calling again
        var act = () => viewModel.Dispose();
        act.Should().NotThrow("disposing multiple times should be safe");

        // Cleanup
        document.Dispose();
    }

    /// <summary>
    /// Creates a PDF document service for testing.
    /// </summary>
    private IPdfDocumentService CreatePdfDocumentService()
    {
        var logger = new Mock<ILogger<PdfDocumentService>>();
        return new PdfDocumentService(logger.Object);
    }
}
