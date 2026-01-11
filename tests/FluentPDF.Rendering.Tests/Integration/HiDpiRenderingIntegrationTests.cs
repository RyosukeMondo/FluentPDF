using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for HiDPI rendering functionality using real PDFium library.
/// These tests verify rendering at various DPI levels with dimension validation,
/// memory management, and OOM fallback behavior.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HiDpiRenderingIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfRenderingService _renderingService;
    private readonly string _fixturesPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    // Test DPI values representing common display configurations
    private const double StandardDpi = 96.0;     // 100% scaling (1x)
    private const double MediumDpi = 144.0;      // 150% scaling (1.5x)
    private const double HighDpi = 192.0;        // 200% scaling (2x)
    private const double UltraHighDpi = 288.0;   // 300% scaling (3x)

    public HiDpiRenderingIntegrationTests()
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

    #region DPI Level Rendering Tests

    [Theory]
    [InlineData(96.0)]   // Standard DPI (100% scaling)
    [InlineData(144.0)]  // Medium DPI (150% scaling)
    [InlineData(192.0)]  // High DPI (200% scaling)
    [InlineData(288.0)]  // Ultra-high DPI (300% scaling)
    public async Task RenderPageAsync_AtVariousDpiLevels_Succeeds(double dpi)
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
            zoomLevel: 1.0,
            dpi: dpi);

        // Assert
        renderResult.IsSuccess.Should().BeTrue($"rendering at {dpi} DPI should succeed");
        renderResult.Value.Should().NotBeNull();
        renderResult.Value.Length.Should().BeGreaterThan(0, "rendered stream should contain image data");
        renderResult.Value.CanRead.Should().BeTrue("stream should be readable");
        renderResult.Value.Position.Should().Be(0, "stream should be positioned at the start");
    }

    [Fact]
    public async Task RenderPageAsync_DpiProgression_ProducesLargerImages()
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

        var dpiLevels = new[] { StandardDpi, MediumDpi, HighDpi, UltraHighDpi };
        var streamSizes = new Dictionary<double, long>();

        // Act - Render at different DPI levels
        foreach (var dpi in dpiLevels)
        {
            var renderResult = await _renderingService.RenderPageAsync(
                loadResult.Value,
                pageNumber: 1,
                zoomLevel: 1.0,
                dpi: dpi);

            renderResult.IsSuccess.Should().BeTrue($"rendering at {dpi} DPI should succeed");
            streamSizes[dpi] = renderResult.Value.Length;
        }

        // Assert - Higher DPI should generally produce larger file sizes
        // (though PNG compression may vary)
        streamSizes[StandardDpi].Should().BeGreaterThan(0);
        streamSizes[MediumDpi].Should().BeGreaterThan(0);
        streamSizes[HighDpi].Should().BeGreaterThan(0);
        streamSizes[UltraHighDpi].Should().BeGreaterThan(0);

        // Ultra-high DPI should produce larger stream than standard DPI
        streamSizes[UltraHighDpi].Should().BeGreaterThan(streamSizes[StandardDpi],
            "3x DPI should produce larger stream than 1x DPI");
    }

    #endregion

    #region Image Dimension Verification Tests

    [Theory]
    [InlineData(96.0, 1.0)]    // Standard DPI, 100% zoom
    [InlineData(144.0, 1.0)]   // Medium DPI, 100% zoom
    [InlineData(192.0, 1.0)]   // High DPI, 100% zoom
    [InlineData(96.0, 2.0)]    // Standard DPI, 200% zoom
    [InlineData(144.0, 1.5)]   // Medium DPI, 150% zoom
    public async Task RenderPageAsync_VerifyOutputDimensions_MatchesExpectedSize(double dpi, double zoomLevel)
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

        // Get page dimensions
        var pageInfoResult = await _documentService.GetPageInfoAsync(loadResult.Value, 1);
        pageInfoResult.IsSuccess.Should().BeTrue();
        var pageInfo = pageInfoResult.Value;

        // Calculate expected dimensions
        var scaleFactor = (dpi / 72.0) * zoomLevel;
        var expectedWidth = (int)(pageInfo.Width * scaleFactor);
        var expectedHeight = (int)(pageInfo.Height * scaleFactor);

        // Act
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: zoomLevel,
            dpi: dpi);

        // Assert
        renderResult.IsSuccess.Should().BeTrue();

        // Load the image to verify dimensions
        using var image = await Image.LoadAsync(renderResult.Value);
        image.Width.Should().Be(expectedWidth,
            $"rendered width should match expected for {dpi} DPI and {zoomLevel:P0} zoom");
        image.Height.Should().Be(expectedHeight,
            $"rendered height should match expected for {dpi} DPI and {zoomLevel:P0} zoom");
    }

    [Fact]
    public async Task RenderPageAsync_DpiScaling_ProducesCorrectPixelDimensions()
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

        // Get page dimensions
        var pageInfoResult = await _documentService.GetPageInfoAsync(loadResult.Value, 1);
        pageInfoResult.IsSuccess.Should().BeTrue();
        var pageInfo = pageInfoResult.Value;

        // Render at standard DPI
        var renderStandard = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: StandardDpi);

        // Render at 2x DPI
        var renderHigh = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: HighDpi);

        // Act & Assert
        renderStandard.IsSuccess.Should().BeTrue();
        renderHigh.IsSuccess.Should().BeTrue();

        using var imageStandard = await Image.LoadAsync(renderStandard.Value);
        using var imageHigh = await Image.LoadAsync(renderHigh.Value);

        // High DPI should be exactly 2x the dimensions of standard DPI
        imageHigh.Width.Should().Be(imageStandard.Width * 2,
            "192 DPI (2x) should produce image with 2x width of 96 DPI (1x)");
        imageHigh.Height.Should().Be(imageStandard.Height * 2,
            "192 DPI (2x) should produce image with 2x height of 96 DPI (1x)");
    }

    #endregion

    #region Memory Usage Tests

    [Fact]
    public async Task RenderPageAsync_MultipleHighDpiRenders_NoMemoryLeaks()
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

        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Act - Render multiple times at high DPI
        for (int i = 0; i < 10; i++)
        {
            var renderResult = await _renderingService.RenderPageAsync(
                loadResult.Value,
                pageNumber: 1,
                zoomLevel: 1.0,
                dpi: HighDpi);

            renderResult.IsSuccess.Should().BeTrue();

            // Dispose the stream immediately to free memory
            await renderResult.Value.DisposeAsync();
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(forceFullCollection: false);

        // Assert - Memory should not grow significantly (allow some overhead)
        var memoryGrowth = finalMemory - initialMemory;
        var allowedGrowth = 50 * 1024 * 1024; // 50 MB tolerance

        memoryGrowth.Should().BeLessThan(allowedGrowth,
            "memory usage should not grow significantly after multiple renders and GC");
    }

    [Fact]
    public async Task RenderPageAsync_AtUltraHighDpi_CompletesWithinReasonableTime()
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: UltraHighDpi);
        stopwatch.Stop();

        // Assert
        renderResult.IsSuccess.Should().BeTrue("ultra-high DPI rendering should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            "ultra-high DPI rendering should complete within 5 seconds");
    }

    #endregion

    #region OOM Fallback Tests

    [Fact]
    public async Task RenderPageAsync_ExtremelyLargeDimensions_FallsBackOrFails()
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

        // Act - Try to render with extreme zoom that would produce huge dimensions
        // This should either fall back to lower DPI or fail gracefully
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 10.0,  // 10x zoom
            dpi: UltraHighDpi);

        // Assert - Either succeeds with fallback or fails with appropriate error
        if (renderResult.IsFailed)
        {
            // Should fail with validation or OOM error
            renderResult.Errors.Should().NotBeEmpty();
            var errorCode = renderResult.Errors[0].Message;
            errorCode.Should().Match(msg =>
                msg.Contains("dimensions") ||
                msg.Contains("memory") ||
                msg.Contains("large"),
                "error should indicate dimension or memory issue");
        }
        else
        {
            // If it succeeded, it should have produced valid output
            renderResult.Value.Should().NotBeNull();
            renderResult.Value.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task RenderPageAsync_HighDpiWithFallback_StillProducesValidImage()
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

        // Act - Render at very high DPI (might trigger fallback on some systems)
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: 576.0);  // 6x DPI (extremely high)

        // Assert - Should either succeed or fail gracefully
        if (renderResult.IsSuccess)
        {
            renderResult.Value.Should().NotBeNull();
            renderResult.Value.Length.Should().BeGreaterThan(0);

            // Verify it's a valid image
            using var image = await Image.LoadAsync(renderResult.Value);
            image.Width.Should().BeGreaterThan(0);
            image.Height.Should().BeGreaterThan(0);
        }
        else
        {
            // If it fails, should have appropriate error
            renderResult.Errors.Should().NotBeEmpty();
        }
    }

    #endregion

    #region Performance Degradation Tests

    [Fact]
    public async Task RenderPageAsync_At2xDpi_CompletesWithinPerformanceTarget()
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var renderResult = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: HighDpi);  // 2x DPI
        stopwatch.Stop();

        // Assert
        renderResult.IsSuccess.Should().BeTrue("2x DPI rendering should succeed");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000,
            "2x DPI rendering should complete within 2 seconds (performance requirement)");
    }

    [Fact]
    public async Task RenderPageAsync_DpiScaling_MaintainsReasonablePerformanceRatio()
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

        // Act - Measure render time at standard DPI
        var stopwatchStandard = System.Diagnostics.Stopwatch.StartNew();
        var renderStandard = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: StandardDpi);
        stopwatchStandard.Stop();

        // Measure render time at 2x DPI
        var stopwatchHigh = System.Diagnostics.Stopwatch.StartNew();
        var renderHigh = await _renderingService.RenderPageAsync(
            loadResult.Value,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: HighDpi);
        stopwatchHigh.Stop();

        // Assert
        renderStandard.IsSuccess.Should().BeTrue();
        renderHigh.IsSuccess.Should().BeTrue();

        // High DPI should not take more than 5x the time of standard DPI
        // (2x dimensions = 4x pixels, but some overhead is acceptable)
        var performanceRatio = (double)stopwatchHigh.ElapsedMilliseconds /
                               Math.Max(stopwatchStandard.ElapsedMilliseconds, 1);

        performanceRatio.Should().BeLessThan(5.0,
            "2x DPI rendering should not be more than 5x slower than 1x DPI");
    }

    #endregion

    #region Multiple Page Tests

    [Fact]
    public async Task RenderAllPages_AtHighDpi_Succeeds()
    {
        // Arrange
        var samplePdfPath = Path.Combine(_fixturesPath, "sample-with-text.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(samplePdfPath))
        {
            return;
        }

        var loadResult = await _documentService.LoadDocumentAsync(samplePdfPath);
        loadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(loadResult.Value);

        var document = loadResult.Value;

        // Act & Assert - Render all pages at high DPI
        for (int pageNum = 1; pageNum <= Math.Min(document.PageCount, 5); pageNum++)
        {
            var renderResult = await _renderingService.RenderPageAsync(
                document,
                pageNumber: pageNum,
                zoomLevel: 1.0,
                dpi: HighDpi);

            renderResult.IsSuccess.Should().BeTrue(
                $"rendering page {pageNum} at high DPI should succeed");
            renderResult.Value.Should().NotBeNull();
            renderResult.Value.Length.Should().BeGreaterThan(0);
        }
    }

    #endregion
}
