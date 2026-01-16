using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentPDF.Rendering.Tests.Performance;

/// <summary>
/// Performance benchmarks for rendering reliability features.
/// Measures overhead of observability, memory monitoring, and fallback strategies.
///
/// To run these benchmarks:
/// 1. Build in Release mode: dotnet build -c Release
/// 2. Run benchmarks: dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj --filter *RenderingReliabilityBenchmarks*
///
/// Performance targets:
/// - Observability overhead: &lt;50ms per render operation
/// - Memory snapshot capture: &lt;10ms
/// - UI binding verification: &lt;500ms (with timeout)
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[Config(typeof(BenchmarkConfig))]
public class RenderingReliabilityBenchmarks
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

    private IPdfDocumentService? _documentService;
    private IPdfRenderingService? _renderingService;
    private PdfDocument? _document;
    private MemoryMonitor? _memoryMonitor;
    private RenderingObservabilityService? _observabilityService;
    private RenderContext? _renderContext;
    private string _fixturesPath = string.Empty;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    // Pre-rendered stream for strategy benchmarks (to isolate strategy overhead)
    private MemoryStream? _preRenderedStream;

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
        var documentLogger = new LoggerFactory().CreateLogger<PdfDocumentService>();
        var renderingLogger = new LoggerFactory().CreateLogger<PdfRenderingService>();
        var observabilityLogger = new LoggerFactory().CreateLogger<RenderingObservabilityService>();

        _documentService = new PdfDocumentService(documentLogger);
        _renderingService = new PdfRenderingService(renderingLogger);
        _memoryMonitor = new MemoryMonitor();
        _observabilityService = new RenderingObservabilityService(observabilityLogger, _memoryMonitor);

        // Setup fixtures path - navigate from bin/Release/net9.0-windows10.0.19041.0 to Fixtures
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        // Load the sample PDF document
        var samplePdfPath = Path.Combine(_fixturesPath, "sample.pdf");
        if (!File.Exists(samplePdfPath))
        {
            throw new FileNotFoundException(
                $"Sample PDF not found at {samplePdfPath}. Ensure Fixtures/sample.pdf exists.");
        }

        var loadResult = _documentService.LoadDocumentAsync(samplePdfPath).GetAwaiter().GetResult();
        if (!loadResult.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Failed to load sample PDF: {string.Join(", ", loadResult.Errors)}");
        }

        _document = loadResult.Value;

        // Pre-render a page for strategy benchmarks
        var renderResult = _renderingService.RenderPageAsync(
            _document,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: 96.0).GetAwaiter().GetResult();

        if (!renderResult.IsSuccess)
        {
            throw new InvalidOperationException("Failed to pre-render page for benchmarks");
        }

        // Copy to MemoryStream for reuse
        _preRenderedStream = new MemoryStream();
        renderResult.Value.CopyTo(_preRenderedStream);
        _preRenderedStream.Position = 0;

        // Create render context for observability benchmarks
        _renderContext = new RenderContext(
            DocumentPath: samplePdfPath,
            PageNumber: 1,
            TotalPages: _document.PageCount,
            RenderDpi: 96.0,
            RequestSource: "Benchmark",
            RequestTime: DateTime.UtcNow,
            OperationId: Guid.NewGuid()
        );
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _preRenderedStream?.Dispose();

        if (_document != null && _documentService != null)
        {
            _documentService.CloseDocument(_document);
        }
    }

    #region Memory Monitor Benchmarks

    /// <summary>
    /// Benchmark: Memory snapshot capture performance.
    /// Target: &lt;10ms per snapshot
    /// </summary>
    [Benchmark(Description = "MemoryMonitor.CaptureSnapshot")]
    public MemorySnapshot Benchmark_MemoryMonitorSnapshot()
    {
        return _memoryMonitor!.CaptureSnapshot("BenchmarkSnapshot");
    }

    /// <summary>
    /// Benchmark: Memory delta calculation performance.
    /// Target: &lt;1ms for delta calculation
    /// </summary>
    [Benchmark(Description = "MemoryMonitor.CalculateDelta")]
    public MemoryDelta Benchmark_MemoryDeltaCalculation()
    {
        var snapshot1 = _memoryMonitor!.CaptureSnapshot("Before");
        var snapshot2 = _memoryMonitor.CaptureSnapshot("After");
        return _memoryMonitor.CalculateDelta(snapshot1, snapshot2);
    }

    #endregion

    #region Observability Overhead Benchmarks

    /// <summary>
    /// Baseline: Render without any observability instrumentation.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Render without observability")]
    public async Task<Stream> Benchmark_RenderWithoutObservability()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: 96.0);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    /// <summary>
    /// Benchmark: Render with observability instrumentation.
    /// Measures overhead of RenderingObservabilityService.
    /// Target: &lt;50ms overhead compared to baseline
    /// </summary>
    [Benchmark(Description = "Render with observability")]
    public async Task<Stream> Benchmark_RenderWithObservability()
    {
        using var operation = _observabilityService!.BeginRenderOperation("BenchmarkRender", _renderContext!);

        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: 96.0);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        // Simulate setting success on operation scope (would be done via reflection in real benchmark)
        // Note: This is a simplified benchmark. In production, RenderOperationScope.SetSuccess would be called.

        return result.Value;
    }

    /// <summary>
    /// Benchmark: Just the observability scope overhead (no actual rendering).
    /// Isolates the cost of BeginRenderOperation/Dispose cycle.
    /// </summary>
    [Benchmark(Description = "Observability scope overhead only")]
    public void Benchmark_ObservabilityScopeOverhead()
    {
        using var operation = _observabilityService!.BeginRenderOperation("BenchmarkScope", _renderContext!);
        // Scope automatically disposes and logs
    }

    #endregion

    #region Rendering Strategy Benchmarks

    /// <summary>
    /// Benchmark: WriteableBitmap strategy performance.
    /// Note: This requires WinUI runtime, so we simulate with just the stream processing part.
    /// </summary>
    [Benchmark(Description = "Stream processing (WriteableBitmap simulation)")]
    public async Task<int> Benchmark_StreamProcessing_WriteableBitmap()
    {
        // Simulate the work WriteableBitmapRenderingStrategy does:
        // 1. Read PNG stream
        // 2. Decode with ImageSharp (if we had it)
        // 3. Create WriteableBitmap (WinUI-specific, can't benchmark here)

        // For benchmark purposes, just measure stream reading overhead
        _preRenderedStream!.Position = 0;
        using var ms = new MemoryStream();
        await _preRenderedStream.CopyToAsync(ms);
        return (int)ms.Length;
    }

    /// <summary>
    /// Benchmark: File-based strategy I/O performance.
    /// Measures temp file write + read cycle.
    /// </summary>
    [Benchmark(Description = "File-based strategy I/O")]
    public async Task<long> Benchmark_FileBasedStrategyIO()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"benchmark_{Guid.NewGuid()}.png");

        try
        {
            // Write PNG stream to temp file
            _preRenderedStream!.Position = 0;
            using (var fileStream = File.Create(tempFilePath))
            {
                await _preRenderedStream.CopyToAsync(fileStream);
            }

            // Read back to verify
            var fileInfo = new FileInfo(tempFilePath);
            return fileInfo.Length;
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    #endregion

    #region Combined Rendering + Observability Benchmarks

    /// <summary>
    /// Benchmark: Full rendering pipeline with memory monitoring.
    /// Simulates a complete render operation with all reliability features.
    /// </summary>
    [Benchmark(Description = "Full render with memory monitoring")]
    public async Task<Stream> Benchmark_FullRenderWithMonitoring()
    {
        var memoryBefore = _memoryMonitor!.CaptureSnapshot("BeforeRender");

        using var operation = _observabilityService!.BeginRenderOperation("FullRender", _renderContext!);

        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: 96.0);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        var memoryAfter = _memoryMonitor.CaptureSnapshot("AfterRender");
        var delta = _memoryMonitor.CalculateDelta(memoryBefore, memoryAfter);

        // Check for abnormal growth (would trigger logging in production)
        _ = delta.IsAbnormal;

        return result.Value;
    }

    #endregion
}
