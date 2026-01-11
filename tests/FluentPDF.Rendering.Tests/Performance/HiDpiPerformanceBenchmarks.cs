using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Tests.Performance;

/// <summary>
/// Performance benchmarks for HiDPI rendering functionality.
/// Measures render time and memory consumption at various DPI levels.
///
/// To run these benchmarks:
/// 1. Build in Release mode: dotnet build -c Release
/// 2. Run benchmarks: dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj --filter *HiDpiPerformanceBenchmarks*
///
/// Or use BenchmarkDotNet directly:
/// dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj -- --filter *HiDpiPerformanceBenchmarks*
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[Config(typeof(BenchmarkConfig))]
public class HiDpiPerformanceBenchmarks
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
    private string _fixturesPath = string.Empty;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    // Test DPI values representing common display configurations
    private const double StandardDpi = 96.0;     // 100% scaling (1x)
    private const double MediumDpi = 144.0;      // 150% scaling (1.5x)
    private const double HighDpi = 192.0;        // 200% scaling (2x)
    private const double UltraHighDpi = 288.0;   // 300% scaling (3x)

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

        _documentService = new PdfDocumentService(documentLogger);
        _renderingService = new PdfRenderingService(renderingLogger);

        // Setup fixtures path - navigate from bin/Release/net8.0 to Fixtures
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
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (_document != null && _documentService != null)
        {
            _documentService.CloseDocument(_document);
        }
    }

    #region Render Time Benchmarks

    [Benchmark(Baseline = true, Description = "Render @ 96 DPI (1x scaling)")]
    public async Task<Stream> RenderAt96Dpi()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: StandardDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    [Benchmark(Description = "Render @ 144 DPI (1.5x scaling)")]
    public async Task<Stream> RenderAt144Dpi()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: MediumDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    [Benchmark(Description = "Render @ 192 DPI (2x scaling)")]
    public async Task<Stream> RenderAt192Dpi()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: HighDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    [Benchmark(Description = "Render @ 288 DPI (3x scaling)")]
    public async Task<Stream> RenderAt288Dpi()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.0,
            dpi: UltraHighDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    #endregion

    #region Combined DPI and Zoom Benchmarks

    [Benchmark(Description = "Render @ 192 DPI + 1.5x zoom")]
    public async Task<Stream> RenderAt192DpiWith150PercentZoom()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 1.5,
            dpi: HighDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    [Benchmark(Description = "Render @ 288 DPI + 2x zoom")]
    public async Task<Stream> RenderAt288DpiWith200PercentZoom()
    {
        var result = await _renderingService!.RenderPageAsync(
            _document!,
            pageNumber: 1,
            zoomLevel: 2.0,
            dpi: UltraHighDpi);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Render failed: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    #endregion
}

/// <summary>
/// Benchmark runner program for HiDPI performance benchmarks.
/// This allows running the benchmarks standalone.
///
/// Usage:
/// dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Check if we're being invoked by BenchmarkDotNet or xUnit
        if (args.Length > 0 && args.Any(a => a.Contains("benchmark", StringComparison.OrdinalIgnoreCase)))
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<HiDpiPerformanceBenchmarks>();
        }
        else
        {
            Console.WriteLine("HiDPI Performance Benchmarks");
            Console.WriteLine("============================");
            Console.WriteLine();
            Console.WriteLine("To run these benchmarks, use:");
            Console.WriteLine("  dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj -- --benchmark");
            Console.WriteLine();
            Console.WriteLine("Or use BenchmarkDotNet runner:");
            Console.WriteLine("  dotnet run -c Release --project tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj --filter *HiDpiPerformanceBenchmarks*");
        }
    }
}
