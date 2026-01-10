using BenchmarkDotNet.Attributes;
using FluentPDF.Benchmarks.Config;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Benchmarks.Suites;

/// <summary>
/// Benchmark suite measuring navigation and zoom operation performance.
/// Tests page transitions (next/previous), zoom changes, and random page access.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class NavigationBenchmarks
{
    private ServiceProvider? _serviceProvider;
    private IPdfDocumentService? _documentService;
    private IPdfRenderingService? _renderingService;

    // Loaded multi-page PDF document for navigation testing
    private PdfDocument? _document;

    // Current state for navigation simulation
    private int _currentPage = 1;
    private double _currentZoom = 1.0;
    private Random? _random;

    // Fixture path - using complex layout as it has multiple pages
    private const string TestDocumentPath = "Fixtures/complex-layout.pdf";

    [GlobalSetup]
    public void Setup()
    {
        // Initialize PDFium
        if (!PdfiumInterop.Initialize())
        {
            throw new InvalidOperationException("Failed to initialize PDFium library");
        }

        // Configure DI container with same setup as main app
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise during benchmarks
        });

        // Register PDF services
        services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
        services.AddSingleton<IPdfRenderingService, PdfRenderingService>();

        _serviceProvider = services.BuildServiceProvider();
        _documentService = _serviceProvider.GetRequiredService<IPdfDocumentService>();
        _renderingService = _serviceProvider.GetRequiredService<IPdfRenderingService>();

        // Load test document
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestDocumentPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Fixture not found: {fullPath}");
        }

        var result = _documentService.LoadDocumentAsync(fullPath).GetAwaiter().GetResult();

        if (result.IsFailed)
        {
            throw new InvalidOperationException($"Failed to load {TestDocumentPath}: {result.Errors[0].Message}");
        }

        _document = result.Value;

        // Initialize random for random page access benchmark
        _random = new Random(42); // Fixed seed for reproducibility
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Dispose loaded document
        _document?.Dispose();

        // Dispose service provider
        _serviceProvider?.Dispose();

        // Shutdown PDFium
        PdfiumInterop.Shutdown();
    }

    /// <summary>
    /// Measures performance of navigating to the next page.
    /// Simulates user clicking "next page" button by rendering the subsequent page.
    /// Requirement: P99 latency less than 1 second for smooth user experience.
    /// </summary>
    [Benchmark]
    public async Task Navigate_NextPage()
    {
        // Move to next page (wrap around if at end)
        _currentPage = (_currentPage % _document!.PageCount) + 1;

        var result = await _renderingService!.RenderPageAsync(_document, _currentPage, _currentZoom);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to render page {_currentPage}: {result.Errors[0].Message}");
        }
    }

    /// <summary>
    /// Measures performance of navigating to the previous page.
    /// Simulates user clicking "previous page" button by rendering the prior page.
    /// Requirement: P99 latency less than 1 second for smooth user experience.
    /// </summary>
    [Benchmark]
    public async Task Navigate_PreviousPage()
    {
        // Move to previous page (wrap around if at beginning)
        _currentPage = _currentPage == 1 ? _document!.PageCount : _currentPage - 1;

        var result = await _renderingService!.RenderPageAsync(_document!, _currentPage, _currentZoom);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to render page {_currentPage}: {result.Errors[0].Message}");
        }
    }

    /// <summary>
    /// Measures performance of changing zoom level from 100% to 150%.
    /// Simulates user zooming in, requiring full page re-render at new zoom level.
    /// Requirement: P99 latency less than 2 seconds for zoom operations.
    /// </summary>
    [Benchmark]
    public async Task ZoomChange_100To150()
    {
        // Simulate zoom change: render at 100%, then at 150%
        var result100 = await _renderingService!.RenderPageAsync(_document!, _currentPage, 1.0);

        if (result100.IsSuccess)
        {
            await result100.Value.DisposeAsync();
        }

        var result150 = await _renderingService.RenderPageAsync(_document!, _currentPage, 1.5);

        if (result150.IsSuccess)
        {
            await result150.Value.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to render page at 150% zoom: {result150.Errors[0].Message}");
        }

        _currentZoom = 1.5;
    }

    /// <summary>
    /// Measures performance of jumping to a random page in the document.
    /// Simulates user using page navigation controls to jump to arbitrary pages.
    /// Tests cache behavior and random access patterns.
    /// Requirement: P99 latency less than 1 second for smooth user experience.
    /// </summary>
    [Benchmark]
    public async Task JumpToPage_Random()
    {
        // Jump to random page (1-based indexing)
        int randomPage = _random!.Next(1, _document!.PageCount + 1);
        _currentPage = randomPage;

        var result = await _renderingService!.RenderPageAsync(_document, _currentPage, _currentZoom);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to render page {_currentPage}: {result.Errors[0].Message}");
        }
    }

    /// <summary>
    /// Baseline benchmark for sequential page rendering.
    /// Used as reference point for navigation performance comparisons.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task Navigate_Sequential_Baseline()
    {
        // Simple sequential page render at 100% zoom
        _currentPage = (_currentPage % _document!.PageCount) + 1;

        var result = await _renderingService!.RenderPageAsync(_document, _currentPage, 1.0);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to render page {_currentPage}: {result.Errors[0].Message}");
        }
    }
}
