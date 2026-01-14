using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Tests.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Tests.Performance;

/// <summary>
/// Performance benchmarks for visual regression testing functionality.
/// Measures headless rendering time, SSIM comparison time, and memory consumption.
///
/// To run these benchmarks:
/// 1. Build in Release mode: dotnet build -c Release
/// 2. Run benchmarks: dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj --filter *VisualTestPerformanceBenchmarks*
///
/// Or use BenchmarkDotNet directly:
/// dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj -- --filter *VisualTestPerformanceBenchmarks*
///
/// Baseline Performance Targets (based on initial measurements):
/// - Headless rendering @ 96 DPI: ~100-150ms per page
/// - SSIM comparison (1920x1080): ~50-80ms
/// - End-to-end visual test: ~200-300ms
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[Config(typeof(BenchmarkConfig))]
public class VisualTestPerformanceBenchmarks
{
    private class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithWarmupCount(3)
                .WithIterationCount(10)
                .WithInvocationCount(1));

            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Min);
            AddColumn(StatisticColumn.Max);
        }
    }

    private IHeadlessRenderingService? _renderingService;
    private IVisualComparisonService? _comparisonService;
    private string _fixturesPath = string.Empty;
    private string _tempPath = string.Empty;
    private string _baselineImagePath = string.Empty;
    private string _actualImagePath = string.Empty;
    private string _diffImagePath = string.Empty;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    // Test DPI values
    private const int StandardDpi = 96;     // 100% scaling
    private const int HighDpi = 192;        // 200% scaling
    private const int UltraHighDpi = 288;   // 300% scaling

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Initialize PDFium once
        lock (_initLock)
        {
            if (!_pdfiumInitialized)
            {
                var initialized = PdfiumInterop.Initialize();
                if (!initialized)
                {
                    throw new InvalidOperationException(
                        "Failed to initialize PDFium. Ensure pdfium.dll is in the benchmark output directory.");
                }
                _pdfiumInitialized = true;
            }
        }

        // Setup services
        var renderingLogger = new LoggerFactory().CreateLogger<HeadlessRenderingService>();
        var comparisonLogger = new LoggerFactory().CreateLogger<VisualComparisonService>();

        _renderingService = new HeadlessRenderingService(renderingLogger);
        _comparisonService = new VisualComparisonService(comparisonLogger);

        // Setup paths - navigate from bin/Release/net8.0 to Fixtures
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _tempPath = Path.Combine(Path.GetTempPath(), "FluentPDF.Benchmarks", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        // Pre-render baseline and actual images for comparison benchmarks
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        if (!File.Exists(samplePdfPath))
        {
            throw new FileNotFoundException(
                $"Sample PDF not found at {samplePdfPath}. Ensure Fixtures/sample.pdf exists.");
        }

        _baselineImagePath = Path.Combine(_tempPath, "baseline.png");
        _actualImagePath = Path.Combine(_tempPath, "actual.png");
        _diffImagePath = Path.Combine(_tempPath, "diff.png");

        // Generate baseline and actual images
        var baselineResult = _renderingService.RenderPageToFileAsync(
            samplePdfPath, 1, _baselineImagePath, StandardDpi).GetAwaiter().GetResult();

        var actualResult = _renderingService.RenderPageToFileAsync(
            samplePdfPath, 1, _actualImagePath, StandardDpi).GetAwaiter().GetResult();

        if (!baselineResult.IsSuccess || !actualResult.IsSuccess)
        {
            throw new InvalidOperationException("Failed to generate baseline/actual images for benchmarks");
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _renderingService?.Dispose();
        _comparisonService?.Dispose();

        // Clean up temp files
        if (Directory.Exists(_tempPath))
        {
            try
            {
                Directory.Delete(_tempPath, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region Headless Rendering Benchmarks

    [Benchmark(Baseline = true, Description = "Headless Render @ 96 DPI")]
    public async Task HeadlessRenderAt96Dpi()
    {
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        var outputPath = Path.Combine(_tempPath, $"render-96dpi-{Guid.NewGuid()}.png");

        var result = await _renderingService!.RenderPageToFileAsync(
            samplePdfPath,
            pageNumber: 1,
            outputPath,
            dpi: StandardDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        // Clean up
        File.Delete(outputPath);
    }

    [Benchmark(Description = "Headless Render @ 192 DPI")]
    public async Task HeadlessRenderAt192Dpi()
    {
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        var outputPath = Path.Combine(_tempPath, $"render-192dpi-{Guid.NewGuid()}.png");

        var result = await _renderingService!.RenderPageToFileAsync(
            samplePdfPath,
            pageNumber: 1,
            outputPath,
            dpi: HighDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        // Clean up
        File.Delete(outputPath);
    }

    [Benchmark(Description = "Headless Render @ 288 DPI")]
    public async Task HeadlessRenderAt288Dpi()
    {
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        var outputPath = Path.Combine(_tempPath, $"render-288dpi-{Guid.NewGuid()}.png");

        var result = await _renderingService!.RenderPageToFileAsync(
            samplePdfPath,
            pageNumber: 1,
            outputPath,
            dpi: UltraHighDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        // Clean up
        File.Delete(outputPath);
    }

    #endregion

    #region SSIM Comparison Benchmarks

    [Benchmark(Description = "SSIM Comparison (identical images)")]
    public async Task SsimComparisonIdentical()
    {
        var diffPath = Path.Combine(_tempPath, $"diff-{Guid.NewGuid()}.png");

        var result = await _comparisonService!.CompareImagesAsync(
            _baselineImagePath,
            _baselineImagePath,  // Compare with itself
            diffPath,
            threshold: 0.95);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Comparison failed: {string.Join(", ", result.Errors)}");
        }

        // Clean up
        File.Delete(diffPath);
    }

    [Benchmark(Description = "SSIM Comparison (different images)")]
    public async Task SsimComparisonDifferent()
    {
        var diffPath = Path.Combine(_tempPath, $"diff-{Guid.NewGuid()}.png");

        var result = await _comparisonService!.CompareImagesAsync(
            _baselineImagePath,
            _actualImagePath,
            diffPath,
            threshold: 0.95);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Comparison failed: {string.Join(", ", result.Errors)}");
        }

        // Clean up
        File.Delete(diffPath);
    }

    #endregion

    #region End-to-End Visual Test Workflow

    [Benchmark(Description = "End-to-End Visual Test (render + compare)")]
    public async Task EndToEndVisualTest()
    {
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        var actualPath = Path.Combine(_tempPath, $"e2e-actual-{Guid.NewGuid()}.png");
        var diffPath = Path.Combine(_tempPath, $"e2e-diff-{Guid.NewGuid()}.png");

        // Step 1: Render PDF to image
        var renderResult = await _renderingService!.RenderPageToFileAsync(
            samplePdfPath,
            pageNumber: 1,
            actualPath,
            dpi: StandardDpi);

        if (!renderResult.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", renderResult.Errors)}");
        }

        // Step 2: Compare with baseline
        var compareResult = await _comparisonService!.CompareImagesAsync(
            _baselineImagePath,
            actualPath,
            diffPath,
            threshold: 0.95);

        if (!compareResult.IsSuccess)
        {
            throw new InvalidOperationException($"Comparison failed: {string.Join(", ", compareResult.Errors)}");
        }

        // Clean up
        File.Delete(actualPath);
        File.Delete(diffPath);
    }

    #endregion

    #region Multi-Page Rendering Benchmarks

    [Benchmark(Description = "Multi-page render (5 pages @ 96 DPI)")]
    public async Task MultiPageRenderFivePages()
    {
        var multiPagePdfPath = Path.Combine(_fixturesPath, "multi-page.pdf");

        if (!File.Exists(multiPagePdfPath))
        {
            // Skip if multi-page PDF doesn't exist
            return;
        }

        for (int page = 1; page <= 5; page++)
        {
            var outputPath = Path.Combine(_tempPath, $"multipage-p{page}-{Guid.NewGuid()}.png");

            var result = await _renderingService!.RenderPageToFileAsync(
                multiPagePdfPath,
                pageNumber: page,
                outputPath,
                dpi: StandardDpi);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Render page {page} failed: {string.Join(", ", result.Errors)}");
            }

            // Clean up
            File.Delete(outputPath);
        }
    }

    #endregion
}
