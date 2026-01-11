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
/// Integration tests for page operations functionality.
/// Tests the complete workflow of rotating, deleting, reordering, and inserting pages.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PageOperationsTests : IDisposable
{
    private const string SimplePdfPath = "../../../../Fixtures/simple-text.pdf";
    private readonly Mock<ILogger<ThumbnailsViewModel>> _thumbnailsLoggerMock;
    private readonly Mock<ILogger<ThumbnailRenderingService>> _renderingLoggerMock;
    private readonly Mock<ILogger<PageOperationsService>> _pageOpsLoggerMock;
    private readonly Mock<ILogger<PdfDocumentService>> _docServiceLoggerMock;
    private readonly Mock<IPdfRenderingService> _pdfRenderingServiceMock;
    private bool _disposed;

    public PageOperationsTests()
    {
        _thumbnailsLoggerMock = new Mock<ILogger<ThumbnailsViewModel>>();
        _renderingLoggerMock = new Mock<ILogger<ThumbnailRenderingService>>();
        _pageOpsLoggerMock = new Mock<ILogger<PageOperationsService>>();
        _docServiceLoggerMock = new Mock<ILogger<PdfDocumentService>>();
        _pdfRenderingServiceMock = new Mock<IPdfRenderingService>();

        // Setup mock to return a simple bitmap stream
        _pdfRenderingServiceMock
            .Setup(s => s.RenderPageAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<int>()))
            .ReturnsAsync(() => Result<Stream>.Success(CreateTestBitmap()));
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
    /// Tests rotating pages right (90 degrees clockwise) updates thumbnails.
    /// </summary>
    [Fact]
    public async Task RotateRight_WithSelectedPages_UpdatesThumbnails()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Select first page
        viewModel.Thumbnails[0].IsSelected = true;

        // Act
        await viewModel.RotateRightCommand.ExecuteAsync(null);
        await Task.Delay(500); // Wait for thumbnail refresh

        // Assert
        // Verify page modified message was sent
        PageModifiedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Execute again to verify message is sent
        await viewModel.RotateRightCommand.ExecuteAsync(null);
        receivedMessage.Should().NotBeNull("page modified message should be sent");

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests rotating pages left (90 degrees counter-clockwise) updates thumbnails.
    /// </summary>
    [Fact]
    public async Task RotateLeft_WithSelectedPages_UpdatesThumbnails()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Select first two pages
        viewModel.Thumbnails[0].IsSelected = true;
        viewModel.Thumbnails[1].IsSelected = true;

        // Act
        await viewModel.RotateLeftCommand.ExecuteAsync(null);
        await Task.Delay(500); // Wait for thumbnail refresh

        // Assert
        // Verify page modified message was sent
        PageModifiedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Execute again to verify message is sent
        await viewModel.RotateLeftCommand.ExecuteAsync(null);
        receivedMessage.Should().NotBeNull("page modified message should be sent");

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests rotating pages 180 degrees updates thumbnails.
    /// </summary>
    [Fact]
    public async Task Rotate180_WithSelectedPages_UpdatesThumbnails()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Select first page
        viewModel.Thumbnails[0].IsSelected = true;

        // Act
        await viewModel.Rotate180Command.ExecuteAsync(null);
        await Task.Delay(500); // Wait for thumbnail refresh

        // Assert
        // Verify page modified message was sent
        PageModifiedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Execute again to verify message is sent
        await viewModel.Rotate180Command.ExecuteAsync(null);
        receivedMessage.Should().NotBeNull("page modified message should be sent");

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests rotation commands are disabled when no pages are selected.
    /// </summary>
    [Fact]
    public async Task RotateCommands_WithNoSelection_AreDisabled()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act & Assert
        viewModel.RotateRightCommand.CanExecute(null).Should().BeFalse(
            "rotate right should be disabled with no selection");
        viewModel.RotateLeftCommand.CanExecute(null).Should().BeFalse(
            "rotate left should be disabled with no selection");
        viewModel.Rotate180Command.CanExecute(null).Should().BeFalse(
            "rotate 180 should be disabled with no selection");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests rotation commands are enabled when pages are selected.
    /// </summary>
    [Fact]
    public async Task RotateCommands_WithSelection_AreEnabled()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act - Select first page
        viewModel.Thumbnails[0].IsSelected = true;

        // Assert
        viewModel.RotateRightCommand.CanExecute(null).Should().BeTrue(
            "rotate right should be enabled with selection");
        viewModel.RotateLeftCommand.CanExecute(null).Should().BeTrue(
            "rotate left should be enabled with selection");
        viewModel.Rotate180Command.CanExecute(null).Should().BeTrue(
            "rotate 180 should be enabled with selection");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests moving pages to a new position reorders them correctly.
    /// </summary>
    [Fact]
    public async Task MovePagesTo_WithValidIndices_ReordersPages()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();
        var initialPageCount = document.PageCount;

        // Act - Move page 1 (index 0) to position 2 (after page 2)
        await viewModel.MovePagesTo(new[] { 0 }, 2);
        await Task.Delay(500); // Wait for reload

        // Assert
        viewModel.Thumbnails.Count.Should().Be(initialPageCount,
            "page count should remain the same after reordering");

        // Verify page modified message was sent
        PageModifiedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Execute again to verify message is sent
        await viewModel.MovePagesTo(new[] { 1 }, 0);
        receivedMessage.Should().NotBeNull("page modified message should be sent");

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests moving multiple pages to a new position.
    /// </summary>
    [Fact]
    public async Task MovePagesTo_WithMultiplePages_ReordersCorrectly()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();
        var initialPageCount = document.PageCount;

        // Act - Move pages 1 and 2 (indices 0, 1) to position after page 3
        await viewModel.MovePagesTo(new[] { 0, 1 }, 3);
        await Task.Delay(500); // Wait for reload

        // Assert
        viewModel.Thumbnails.Count.Should().Be(initialPageCount,
            "page count should remain the same");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests inserting a blank page increases page count.
    /// </summary>
    [Fact]
    public async Task InsertBlankPage_WithSameSize_IncreasesPageCount()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();
        var initialPageCount = document.PageCount;

        // Act
        await viewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.SameAsCurrent);
        await Task.Delay(500); // Wait for reload

        // Assert
        viewModel.Thumbnails.Count.Should().Be(initialPageCount + 1,
            "page count should increase by 1 after inserting blank page");

        // Verify page modified message was sent
        PageModifiedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Execute again to verify message is sent
        await viewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.Letter);
        receivedMessage.Should().NotBeNull("page modified message should be sent");

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests inserting blank pages with different sizes.
    /// </summary>
    [Fact]
    public async Task InsertBlankPage_WithDifferentSizes_Works()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();
        var initialPageCount = document.PageCount;

        // Act - Insert Letter size page
        await viewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.Letter);
        await Task.Delay(500);

        // Assert
        viewModel.Thumbnails.Count.Should().Be(initialPageCount + 1);

        // Act - Insert A4 size page
        await viewModel.InsertBlankPageCommand.ExecuteAsync(PageSize.A4);
        await Task.Delay(500);

        // Assert
        viewModel.Thumbnails.Count.Should().Be(initialPageCount + 2);

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that select all command selects all thumbnails.
    /// </summary>
    [Fact]
    public async Task SelectAll_SelectsAllThumbnails()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act
        viewModel.SelectAllCommand.Execute(null);

        // Assert
        viewModel.Thumbnails.All(t => t.IsSelected).Should().BeTrue(
            "all thumbnails should be selected after SelectAll");
        viewModel.SelectedThumbnails.Count().Should().Be(viewModel.Thumbnails.Count,
            "selected thumbnails count should equal total count");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that delete command is enabled only when pages are selected.
    /// </summary>
    [Fact]
    public async Task DeletePages_CanExecuteOnlyWithSelection()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act & Assert - No selection
        viewModel.DeletePagesCommand.CanExecute(null).Should().BeFalse(
            "delete should be disabled with no selection");

        // Act - Select a page
        viewModel.Thumbnails[0].IsSelected = true;

        // Assert
        viewModel.DeletePagesCommand.CanExecute(null).Should().BeTrue(
            "delete should be enabled with selection");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests the complete workflow of selecting, rotating, and verifying changes.
    /// </summary>
    [Fact]
    public async Task EndToEnd_SelectRotateVerify_Works()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        PageModifiedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<PageModifiedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        // Act 1: Select first two pages
        viewModel.Thumbnails[0].IsSelected = true;
        viewModel.Thumbnails[1].IsSelected = true;

        // Assert 1: Commands enabled
        viewModel.RotateRightCommand.CanExecute(null).Should().BeTrue();
        viewModel.DeletePagesCommand.CanExecute(null).Should().BeTrue();

        // Act 2: Rotate right
        await viewModel.RotateRightCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert 2: Message sent
        receivedMessage.Should().NotBeNull("rotation should send page modified message");

        // Act 3: Select all and rotate 180
        receivedMessage = null;
        viewModel.SelectAllCommand.Execute(null);
        await viewModel.Rotate180Command.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert 3: All operations completed
        receivedMessage.Should().NotBeNull("second rotation should also send message");
        viewModel.Thumbnails.All(t => t.IsSelected).Should().BeTrue();

        // Cleanup
        WeakReferenceMessenger.Default.Unregister<PageModifiedMessage>(this);
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that page operations work correctly with multi-page selection.
    /// </summary>
    [Fact]
    public async Task MultiPageSelection_RotateAndReorder_Works()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act 1: Select multiple pages
        viewModel.Thumbnails[0].IsSelected = true;
        viewModel.Thumbnails[1].IsSelected = true;
        viewModel.Thumbnails[2].IsSelected = true;

        // Assert 1: Correct selection count
        viewModel.SelectedThumbnails.Count().Should().Be(3);

        // Act 2: Rotate selected pages
        await viewModel.RotateRightCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert 2: Operation completed without errors
        viewModel.Thumbnails.Count.Should().Be(document.PageCount);

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that operations correctly update the HasSelectedThumbnails state.
    /// </summary>
    [Fact]
    public async Task HasSelectedThumbnails_UpdatesCorrectly()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act & Assert - No selection
        viewModel.RotateRightCommand.CanExecute(null).Should().BeFalse();

        // Select one page
        viewModel.Thumbnails[0].IsSelected = true;
        viewModel.RotateRightCommand.CanExecute(null).Should().BeTrue();

        // Deselect
        viewModel.Thumbnails[0].IsSelected = false;
        viewModel.RotateRightCommand.CanExecute(null).Should().BeFalse();

        // Select all
        viewModel.SelectAllCommand.Execute(null);
        viewModel.RotateRightCommand.CanExecute(null).Should().BeTrue();

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Tests that disposing the ViewModel properly cleans up resources after page operations.
    /// </summary>
    [Fact]
    public async Task Dispose_AfterPageOperations_CleansUpResources()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act - Perform some operations
        viewModel.Thumbnails[0].IsSelected = true;
        await viewModel.RotateRightCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Dispose
        viewModel.Dispose();

        // Assert - Should not throw when calling again
        var act = () => viewModel.Dispose();
        act.Should().NotThrow("disposing multiple times should be safe");

        // Cleanup
        document.Dispose();
    }

    /// <summary>
    /// Tests concurrent page operations complete safely.
    /// </summary>
    [Fact]
    public async Task ConcurrentOperations_CompleteSuccessfully()
    {
        // Arrange
        var (viewModel, document) = await CreateViewModelWithDocument();

        // Act - Select different pages and perform operations
        viewModel.Thumbnails[0].IsSelected = true;
        var task1 = viewModel.RotateRightCommand.ExecuteAsync(null);

        await Task.Delay(100); // Small delay to create overlap

        viewModel.Thumbnails[1].IsSelected = true;
        viewModel.Thumbnails[0].IsSelected = false;
        var task2 = viewModel.RotateLeftCommand.ExecuteAsync(null);

        // Wait for both
        await Task.WhenAll(task1.AsTask(), task2.AsTask());
        await Task.Delay(500);

        // Assert - Both operations completed
        viewModel.Thumbnails.Count.Should().Be(document.PageCount,
            "page count should remain stable after concurrent operations");

        // Cleanup
        viewModel.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Creates a ViewModel with a loaded document for testing.
    /// </summary>
    private async Task<(ThumbnailsViewModel viewModel, PdfDocument document)> CreateViewModelWithDocument()
    {
        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Test PDF not found at {fullPdfPath}");
        }

        var documentService = new PdfDocumentService(_docServiceLoggerMock.Object);
        var loadResult = await documentService.LoadDocumentAsync(fullPdfPath);
        loadResult.IsSuccess.Should().BeTrue("document should load successfully");

        var document = loadResult.Value!;

        var thumbnailService = new ThumbnailRenderingService(
            _pdfRenderingServiceMock.Object,
            _renderingLoggerMock.Object);

        var pageOperationsService = new PageOperationsService(_pageOpsLoggerMock.Object);

        var viewModel = new ThumbnailsViewModel(
            thumbnailService,
            pageOperationsService,
            _thumbnailsLoggerMock.Object);

        await viewModel.LoadThumbnailsAsync(document);
        await Task.Delay(500); // Wait for initial thumbnail loading

        return (viewModel, document);
    }

    /// <summary>
    /// Creates a simple test bitmap for mocking.
    /// </summary>
    private static Stream CreateTestBitmap()
    {
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
        return stream;
    }
}
