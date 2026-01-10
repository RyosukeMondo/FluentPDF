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
/// Benchmark suite measuring PDF rendering performance across different document types and zoom levels.
/// Tests rendering performance for text-heavy, image-heavy, vector graphics, and complex layout PDFs.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class RenderingBenchmarks
{
    private ServiceProvider? _serviceProvider;
    private IPdfDocumentService? _documentService;
    private IPdfRenderingService? _renderingService;

    // Loaded PDF documents
    private PdfDocument? _textHeavyDoc;
    private PdfDocument? _imageHeavyDoc;
    private PdfDocument? _vectorGraphicsDoc;
    private PdfDocument? _complexLayoutDoc;

    // Fixture paths
    private const string TextHeavyPath = "Fixtures/text-heavy.pdf";
    private const string ImageHeavyPath = "Fixtures/image-heavy.pdf";
    private const string VectorGraphicsPath = "Fixtures/vector-graphics.pdf";
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

        // Load all sample PDFs
        _textHeavyDoc = LoadDocument(TextHeavyPath);
        _imageHeavyDoc = LoadDocument(ImageHeavyPath);
        _vectorGraphicsDoc = LoadDocument(VectorGraphicsPath);
        _complexLayoutDoc = LoadDocument(ComplexLayoutPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Dispose all loaded documents
        _textHeavyDoc?.Dispose();
        _imageHeavyDoc?.Dispose();
        _vectorGraphicsDoc?.Dispose();
        _complexLayoutDoc?.Dispose();

        // Dispose service provider
        _serviceProvider?.Dispose();

        // Shutdown PDFium
        PdfiumInterop.Shutdown();
    }

    private PdfDocument LoadDocument(string relativePath)
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Fixture not found: {fullPath}");
        }

        var result = _documentService!.LoadDocumentAsync(fullPath).GetAwaiter().GetResult();

        if (result.IsFailed)
        {
            throw new InvalidOperationException($"Failed to load {relativePath}: {result.Errors[0].Message}");
        }

        return result.Value;
    }

    // Text-heavy benchmarks
    [Benchmark]
    [Arguments(0.5)]
    [Arguments(1.0)]
    [Arguments(1.5)]
    [Arguments(2.0)]
    public async Task RenderTextHeavy(double zoomLevel)
    {
        var result = await _renderingService!.RenderPageAsync(_textHeavyDoc!, 1, zoomLevel);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
    }

    // Image-heavy benchmarks
    [Benchmark]
    [Arguments(0.5)]
    [Arguments(1.0)]
    [Arguments(1.5)]
    [Arguments(2.0)]
    public async Task RenderImageHeavy(double zoomLevel)
    {
        var result = await _renderingService!.RenderPageAsync(_imageHeavyDoc!, 1, zoomLevel);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
    }

    // Vector graphics benchmarks
    [Benchmark]
    [Arguments(0.5)]
    [Arguments(1.0)]
    [Arguments(1.5)]
    [Arguments(2.0)]
    public async Task RenderVectorGraphics(double zoomLevel)
    {
        var result = await _renderingService!.RenderPageAsync(_vectorGraphicsDoc!, 1, zoomLevel);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
    }

    // Complex layout benchmarks
    [Benchmark]
    [Arguments(0.5)]
    [Arguments(1.0)]
    [Arguments(1.5)]
    [Arguments(2.0)]
    public async Task RenderComplexLayout(double zoomLevel)
    {
        var result = await _renderingService!.RenderPageAsync(_complexLayoutDoc!, 1, zoomLevel);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
    }

    // Baseline benchmark - text-heavy at 100% zoom (requirement: P99 < 1 second)
    [Benchmark(Baseline = true)]
    public async Task RenderTextHeavy_100Percent_Baseline()
    {
        var result = await _renderingService!.RenderPageAsync(_textHeavyDoc!, 1, 1.0);

        if (result.IsSuccess)
        {
            await result.Value.DisposeAsync();
        }
    }
}
