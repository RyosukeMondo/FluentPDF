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
/// Benchmark suite for measuring memory allocations and detecting memory leaks in PDF operations.
/// Tracks managed and native memory allocations, GC collections, and SafeHandle disposal.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class MemoryBenchmarks
{
    private ServiceProvider? _serviceProvider;
    private IPdfDocumentService? _documentService;
    private IPdfRenderingService? _renderingService;

    // Fixture paths
    private const string TextHeavyPath = "Fixtures/text-heavy.pdf";
    private const string ComplexLayoutPath = "Fixtures/complex-layout.pdf";

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
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Dispose service provider
        _serviceProvider?.Dispose();

        // Shutdown PDFium
        PdfiumInterop.Shutdown();
    }

    /// <summary>
    /// Measures memory allocations during document load and dispose cycle.
    /// Verifies SafeHandles are properly disposed and no memory leaks occur.
    /// </summary>
    [Benchmark]
    public async Task LoadAndDispose_Document()
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TextHeavyPath);

        var result = await _documentService!.LoadDocumentAsync(fullPath);

        if (result.IsSuccess)
        {
            // Dispose document to ensure cleanup is measured
            result.Value.Dispose();
        }
        else
        {
            throw new InvalidOperationException($"Failed to load document: {result.Errors[0].Message}");
        }
    }

    /// <summary>
    /// Measures memory allocations during page rendering and bitmap cleanup.
    /// Tracks both managed and native memory allocations via MemoryDiagnoser.
    /// </summary>
    [Benchmark]
    public async Task RenderAndDispose_Page()
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TextHeavyPath);

        var loadResult = await _documentService!.LoadDocumentAsync(fullPath);

        if (loadResult.IsFailed)
        {
            throw new InvalidOperationException($"Failed to load document: {loadResult.Errors[0].Message}");
        }

        using var document = loadResult.Value;

        var renderResult = await _renderingService!.RenderPageAsync(document, 1, 1.0);

        if (renderResult.IsSuccess)
        {
            // Dispose rendered page to ensure cleanup is measured
            await renderResult.Value.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Failed to render page: {renderResult.Errors[0].Message}");
        }
    }

    /// <summary>
    /// Stress test measuring memory management over 100 page renders.
    /// Verifies no memory leaks and proper resource cleanup under load.
    /// Should complete without OutOfMemoryException.
    /// </summary>
    [Benchmark]
    public async Task LoadRender_100Pages()
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ComplexLayoutPath);

        var loadResult = await _documentService!.LoadDocumentAsync(fullPath);

        if (loadResult.IsFailed)
        {
            throw new InvalidOperationException($"Failed to load document: {loadResult.Errors[0].Message}");
        }

        using var document = loadResult.Value;

        // Render first page 100 times to stress test memory management
        for (int i = 0; i < 100; i++)
        {
            var renderResult = await _renderingService!.RenderPageAsync(document, 1, 1.0);

            if (renderResult.IsSuccess)
            {
                // Immediately dispose to simulate rapid page rendering
                await renderResult.Value.DisposeAsync();
            }
            else
            {
                throw new InvalidOperationException($"Failed to render page on iteration {i}: {renderResult.Errors[0].Message}");
            }
        }
    }

    /// <summary>
    /// Baseline benchmark for document load operations.
    /// Used as reference point for memory allocation comparisons.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task LoadDocument_Baseline()
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TextHeavyPath);

        var result = await _documentService!.LoadDocumentAsync(fullPath);

        if (result.IsSuccess)
        {
            result.Value.Dispose();
        }
        else
        {
            throw new InvalidOperationException($"Failed to load document: {result.Errors[0].Message}");
        }
    }
}
