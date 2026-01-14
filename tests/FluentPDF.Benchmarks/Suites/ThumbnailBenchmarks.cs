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
/// Benchmark suite measuring thumbnail rendering performance.
/// Tests P99 latency for thumbnail generation across different document types.
/// Requirement: P99 latency must be less than 200ms for thumbnail rendering.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ThumbnailBenchmarks
{
    private ServiceProvider? _serviceProvider;
    private IPdfDocumentService? _documentService;
    private IThumbnailRenderingService? _thumbnailService;

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
        services.AddSingleton<IThumbnailRenderingService, ThumbnailRenderingService>();

        _serviceProvider = services.BuildServiceProvider();
        _documentService = _serviceProvider.GetRequiredService<IPdfDocumentService>();
        _thumbnailService = _serviceProvider.GetRequiredService<IThumbnailRenderingService>();

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

    /// <summary>
    /// Benchmark for rendering thumbnails of text-heavy documents.
    /// Requirement: P99 latency < 200ms.
    /// </summary>
    [Benchmark]
    public async Task RenderThumbnail_TextHeavy()
    {
        var result = await _thumbnailService!.RenderThumbnailAsync(_textHeavyDoc!, 1);

        if (result.IsSuccess && result.Value != null)
        {
            await result.Value.DisposeAsync();
        }
    }

    /// <summary>
    /// Benchmark for rendering thumbnails of image-heavy documents.
    /// Requirement: P99 latency < 200ms.
    /// </summary>
    [Benchmark]
    public async Task RenderThumbnail_ImageHeavy()
    {
        var result = await _thumbnailService!.RenderThumbnailAsync(_imageHeavyDoc!, 1);

        if (result.IsSuccess && result.Value != null)
        {
            await result.Value.DisposeAsync();
        }
    }

    /// <summary>
    /// Benchmark for rendering thumbnails of vector graphics documents.
    /// Requirement: P99 latency < 200ms.
    /// </summary>
    [Benchmark]
    public async Task RenderThumbnail_VectorGraphics()
    {
        var result = await _thumbnailService!.RenderThumbnailAsync(_vectorGraphicsDoc!, 1);

        if (result.IsSuccess && result.Value != null)
        {
            await result.Value.DisposeAsync();
        }
    }

    /// <summary>
    /// Benchmark for rendering thumbnails of complex layout documents.
    /// Requirement: P99 latency < 200ms.
    /// </summary>
    [Benchmark]
    public async Task RenderThumbnail_ComplexLayout()
    {
        var result = await _thumbnailService!.RenderThumbnailAsync(_complexLayoutDoc!, 1);

        if (result.IsSuccess && result.Value != null)
        {
            await result.Value.DisposeAsync();
        }
    }

    /// <summary>
    /// Baseline benchmark - text-heavy thumbnail rendering.
    /// This is the baseline for comparison. P99 must be < 200ms.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task RenderThumbnail_Baseline()
    {
        var result = await _thumbnailService!.RenderThumbnailAsync(_textHeavyDoc!, 1);

        if (result.IsSuccess && result.Value != null)
        {
            await result.Value.DisposeAsync();
        }
    }

    /// <summary>
    /// Benchmark for rendering multiple thumbnails concurrently (simulates real usage).
    /// Tests concurrent rendering with semaphore limiting to 4 parallel renders.
    /// </summary>
    [Benchmark]
    public async Task RenderMultipleThumbnails_Concurrent()
    {
        var tasks = new List<Task>();

        // Render 10 thumbnails concurrently (limited by semaphore to 4 at a time)
        for (int i = 1; i <= Math.Min(10, _textHeavyDoc!.PageCount); i++)
        {
            var pageNumber = i;
            tasks.Add(Task.Run(async () =>
            {
                var result = await _thumbnailService!.RenderThumbnailAsync(_textHeavyDoc, pageNumber);
                if (result.IsSuccess && result.Value != null)
                {
                    await result.Value.DisposeAsync();
                }
            }));
        }

        await Task.WhenAll(tasks);
    }
}
