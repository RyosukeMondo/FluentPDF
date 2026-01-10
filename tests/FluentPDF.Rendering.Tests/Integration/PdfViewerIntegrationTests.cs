using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for PDF viewer functionality using real PDFium library.
/// These tests verify the complete workflow from document loading to page rendering.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PdfViewerIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfRenderingService _renderingService;
    private readonly string _fixturesPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public PdfViewerIntegrationTests()
    {
        // Initialize PDFium once for all tests
        lock (_initLock)
        {
            if (!_pdfiumInitialized)
            {
                var initialized = PdfiumInterop.Initialize();
                if (!initialized)
                {
                    throw new InvalidOperationException(
                        "Failed to initialize PDFium. Ensure pdfium.dll is in the test output directory.");
                }
                _pdfiumInitialized = true;
            }
        }

        // Setup services
        var documentLogger = new LoggerFactory().CreateLogger<PdfDocumentService>();
        var renderingLogger = new LoggerFactory().CreateLogger<PdfRenderingService>();

        _documentService = new PdfDocumentService(documentLogger);
        _renderingService = new PdfRenderingService(renderingLogger);

        // Setup fixtures path
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _documentsToCleanup = new List<PdfDocument>();
    }

    public void Dispose()
    {
        // Clean up any documents that were loaded
        foreach (var doc in _documentsToCleanup)
        {
            try
            {
                _documentService.CloseDocument(doc);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Document Loading Tests

    [Fact]
    public async Task LoadDocument_WithValidPdf_Succeeds()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        // Act
        var result = await _documentService.LoadDocumentAsync(samplePdfPath);

        // Assert
        result.IsSuccess.Should().BeTrue("loading a valid PDF should succeed");
        result.Value.Should().NotBeNull();
        result.Value.PageCount.Should().BeGreaterThan(0, "PDF should have at least one page");
        result.Value.FilePath.Should().Be(samplePdfPath);
        result.Value.Handle.Should().NotBeNull();

        // Verify handle is valid by casting to SafePdfDocumentHandle
        var safeHandle = result.Value.Handle as SafePdfDocumentHandle;
        safeHandle.Should().NotBeNull("handle should be SafePdfDocumentHandle");
        safeHandle!.IsInvalid.Should().BeFalse("handle should be valid");

        result.Value.FileSizeBytes.Should().BeGreaterThan(0);
        result.Value.LoadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Track for cleanup
        _documentsToCleanup.Add(result.Value);
    }

    [Fact]
    public async Task LoadDocument_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_fixturesPath, "nonexistent.pdf");

        // Act
        var result = await _documentService.LoadDocumentAsync(nonExistentPath);

        // Assert
        result.IsFailed.Should().BeTrue("loading non-existent file should fail");
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_FILE_NOT_FOUND");
        error.Category.Should().Be(ErrorCategory.IO);
    }

    [Fact]
    public async Task LoadDocument_WithCorruptedPdf_ReturnsError()
    {
        // Arrange
        var corruptedPdfPath = Path.Combine(_fixturesPath, "corrupted.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(corruptedPdfPath))
        {
            return;
        }

        // Act
        var result = await _documentService.LoadDocumentAsync(corruptedPdfPath);

        // Assert
        result.IsFailed.Should().BeTrue("loading corrupted PDF should fail");
        result.Errors.Should().ContainSingle();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().BeOneOf("PDF_CORRUPTED", "PDF_INVALID_FORMAT", "PDF_LOAD_FAILED");
    }

    #endregion

    #region Page Rendering Tests

    [Fact]
    public async Task RenderPage_WithValidDocument_ReturnsImage()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(loadResult.Value);

        // Act
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0);

        // Assert
        renderResult.IsSuccess.Should().BeTrue("rendering first page should succeed");
        renderResult.Value.Should().NotBeNull();
        renderResult.Value.Length.Should().BeGreaterThan(0, "rendered stream should contain image data");
        renderResult.Value.CanRead.Should().BeTrue("stream should be readable");
    }

    [Fact]
    public async Task RenderPage_WithInvalidPageNumber_ReturnsError()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(loadResult.Value);

        // Act - Try to render page beyond document bounds
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: loadResult.Value.PageCount + 1,
            zoomLevel: 1.0);

        // Assert
        renderResult.IsFailed.Should().BeTrue("rendering invalid page should fail");
        var error = renderResult.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_PAGE_INVALID");
    }

    [Fact]
    public async Task RenderAllPages_Succeeds()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(loadResult.Value);

        var document = loadResult.Value;

        // Act & Assert - Render all pages
        for (int pageNum = 1; pageNum <= document.PageCount; pageNum++)
        {
            var renderResult = await _renderingService.RenderPageAsync(
                document,
                pageNumber: pageNum,
                zoomLevel: 1.0);

            renderResult.IsSuccess.Should().BeTrue(
                $"rendering page {pageNum} of {document.PageCount} should succeed");
            renderResult.Value.Should().NotBeNull();
            renderResult.Value.Length.Should().BeGreaterThan(0, "rendered stream should contain image data");
            renderResult.Value.CanRead.Should().BeTrue("stream should be readable");
        }
    }

    #endregion

    #region Zoom Level Tests

    [Fact]
    public async Task ZoomLevels_RenderCorrectly()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(loadResult.Value);

        var document = loadResult.Value;
        var zoomLevels = new[] { 0.5, 1.0, 1.5, 2.0 };
        var renderedSizes = new Dictionary<double, long>();

        // Act - Render at different zoom levels and check stream sizes
        foreach (var zoom in zoomLevels)
        {
            var renderResult = await _renderingService.RenderPageAsync(
                document,
                pageNumber: 1,
                zoomLevel: zoom);

            renderResult.IsSuccess.Should().BeTrue($"rendering at {zoom:P0} zoom should succeed");
            renderResult.Value.Should().NotBeNull();
            renderResult.Value.Length.Should().BeGreaterThan(0);
            renderedSizes[zoom] = renderResult.Value.Length;
        }

        // Assert - Verify stream sizes scale (higher zoom = larger file size generally)
        var size50 = renderedSizes[0.5];
        var size100 = renderedSizes[1.0];
        var size200 = renderedSizes[2.0];

        // Higher zoom levels should generally produce larger streams
        // (though PNG compression may vary, so we just check they're all different and positive)
        size50.Should().BeGreaterThan(0, "50% zoom should produce valid stream");
        size100.Should().BeGreaterThan(0, "100% zoom should produce valid stream");
        size200.Should().BeGreaterThan(0, "200% zoom should produce valid stream");

        // Typically, higher zoom = larger image = larger stream (though not guaranteed with compression)
        size200.Should().BeGreaterThan(size50, "200% zoom typically produces larger stream than 50%");
    }

    #endregion

    #region Page Info Tests

    [Fact]
    public async Task GetPageInfo_WithValidPage_ReturnsCorrectDimensions()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(loadResult.Value);

        // Act
        var pageInfoResult = await _documentService.GetPageInfoAsync(loadResult.Value, 1);

        // Assert
        pageInfoResult.IsSuccess.Should().BeTrue("getting page info should succeed");
        pageInfoResult.Value.Should().NotBeNull();
        pageInfoResult.Value.PageNumber.Should().Be(1);
        pageInfoResult.Value.Width.Should().BeGreaterThan(0, "page width should be positive");
        pageInfoResult.Value.Height.Should().BeGreaterThan(0, "page height should be positive");
        pageInfoResult.Value.AspectRatio.Should().BeGreaterThan(0);
    }

    #endregion

    #region Memory Cleanup Tests

    [Fact]
    public async Task MemoryCleanup_NoLeaks()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var documentsLoaded = new List<PdfDocument>();

        // Act - Load and dispose multiple documents
        for (int i = 0; i < 5; i++)
        {
            var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
            loadResult.IsSuccess.Should().BeTrue();

            var doc = loadResult.Value;
            documentsLoaded.Add(doc);

            // Render a page to ensure resources are allocated
            var renderResult = await _renderingService.RenderPageAsync(doc, 1, 1.0);
            renderResult.IsSuccess.Should().BeTrue();

            // Close the document
            var closeResult = _documentService.CloseDocument(doc);
            closeResult.IsSuccess.Should().BeTrue();
        }

        // Assert - Verify all handles were disposed
        foreach (var doc in documentsLoaded)
        {
            var safeHandle = doc.Handle as SafePdfDocumentHandle;
            safeHandle.Should().NotBeNull("handle should be SafePdfDocumentHandle");
            safeHandle!.IsInvalid.Should().BeTrue(
                "document handle should be invalid after disposal");
            safeHandle.IsClosed.Should().BeTrue(
                "document handle should be closed after disposal");
        }
    }

    [Fact]
    public async Task DocumentDisposal_HandlesMultipleDisposes()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        var doc = loadResult.Value;

        // Act - Close document multiple times
        var closeResult1 = _documentService.CloseDocument(doc);
        var closeResult2 = _documentService.CloseDocument(doc);

        // Assert - Both should succeed (idempotent disposal)
        closeResult1.IsSuccess.Should().BeTrue();
        closeResult2.IsSuccess.Should().BeTrue();

        var safeHandle = doc.Handle as SafePdfDocumentHandle;
        safeHandle.Should().NotBeNull("handle should be SafePdfDocumentHandle");
        safeHandle!.IsInvalid.Should().BeTrue("handle should be invalid after disposal");
    }

    #endregion

    #region Complete Workflow Test

    [Fact]
    public async Task CompleteWorkflow_LoadNavigateZoom_Succeeds()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        // Act & Assert - Simulate complete user workflow

        // 1. Load document
        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue("document loading should succeed");
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        document.PageCount.Should().BeGreaterThan(0);

        // 2. Render first page at 100%
        var renderPage1 = await _renderingService.RenderPageAsync(document, 1, 1.0);
        renderPage1.IsSuccess.Should().BeTrue("rendering page 1 should succeed");

        // 3. Navigate to next page (if exists)
        if (document.PageCount > 1)
        {
            var renderPage2 = await _renderingService.RenderPageAsync(document, 2, 1.0);
            renderPage2.IsSuccess.Should().BeTrue("rendering page 2 should succeed");

            // 4. Navigate back to first page
            var renderPage1Again = await _renderingService.RenderPageAsync(document, 1, 1.0);
            renderPage1Again.IsSuccess.Should().BeTrue("rendering page 1 again should succeed");
        }

        // 5. Zoom in (150%)
        var renderZoomed = await _renderingService.RenderPageAsync(document, 1, 1.5);
        renderZoomed.IsSuccess.Should().BeTrue("rendering at 150% zoom should succeed");

        // 6. Zoom out (50%)
        var renderZoomedOut = await _renderingService.RenderPageAsync(document, 1, 0.5);
        renderZoomedOut.IsSuccess.Should().BeTrue("rendering at 50% zoom should succeed");

        // 7. Get page info
        var pageInfo = await _documentService.GetPageInfoAsync(document, 1);
        pageInfo.IsSuccess.Should().BeTrue("getting page info should succeed");
        pageInfo.Value.Width.Should().BeGreaterThan(0);

        // 8. Close document
        var closeResult = _documentService.CloseDocument(document);
        closeResult.IsSuccess.Should().BeTrue("closing document should succeed");
    }

    #endregion
}
