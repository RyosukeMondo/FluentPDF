using BenchmarkDotNet.Attributes;
using FluentPDF.Benchmarks.Config;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Benchmarks.Suites;

/// <summary>
/// Benchmark suite measuring application startup and initialization performance.
/// Tests PDFium initialization, DI container setup, and full cold start scenarios.
/// Requirements: PDFium &lt; 100ms, DI &lt; 50ms, Cold start P99 &lt; 2 seconds
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class StartupBenchmarks
{
    /// <summary>
    /// Measures PDFium library initialization time.
    /// Requirement: &lt; 100ms
    /// </summary>
    [Benchmark]
    public void Initialize_PDFium()
    {
        // Ensure PDFium is shut down before initialization
        PdfiumInterop.Shutdown();

        // Measure initialization time
        var initialized = PdfiumInterop.Initialize();

        if (!initialized)
        {
            throw new InvalidOperationException("Failed to initialize PDFium library");
        }

        // Clean up for next iteration
        PdfiumInterop.Shutdown();
    }

    /// <summary>
    /// Measures DI container setup time including service registration and build.
    /// Requirement: &lt; 50ms
    /// </summary>
    [Benchmark]
    public void Initialize_DIContainer()
    {
        // Configure DI container with same setup as main app
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Register core PDF services
        services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
        services.AddSingleton<IPdfRenderingService, PdfRenderingService>();

        // Build the service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Verify services are registered correctly
        var documentService = serviceProvider.GetRequiredService<IPdfDocumentService>();
        var renderingService = serviceProvider.GetRequiredService<IPdfRenderingService>();

        if (documentService == null || renderingService == null)
        {
            throw new InvalidOperationException("Service registration failed");
        }
    }

    /// <summary>
    /// Measures full application cold start including PDFium init, DI setup, and service creation.
    /// Simulates the complete startup sequence as in App.xaml.cs.
    /// Requirement: P99 &lt; 2 seconds
    /// </summary>
    [Benchmark(Baseline = true)]
    public void ColdStart_FullApplication()
    {
        // 1. Ensure clean state
        PdfiumInterop.Shutdown();

        // 2. Initialize PDFium library
        var initialized = PdfiumInterop.Initialize();
        if (!initialized)
        {
            throw new InvalidOperationException("Failed to initialize PDFium library");
        }

        // 3. Configure DI container
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Register PDF services
        services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
        services.AddSingleton<IPdfRenderingService, PdfRenderingService>();

        // 4. Build service provider
        using var serviceProvider = services.BuildServiceProvider();

        // 5. Resolve services (simulates first use)
        var documentService = serviceProvider.GetRequiredService<IPdfDocumentService>();
        var renderingService = serviceProvider.GetRequiredService<IPdfRenderingService>();

        // Verify services are available
        if (documentService == null || renderingService == null)
        {
            throw new InvalidOperationException("Service resolution failed");
        }

        // 6. Clean up
        PdfiumInterop.Shutdown();
    }

    /// <summary>
    /// Measures the overhead of iterative setup/teardown to establish baseline.
    /// This helps identify the cost of repeated initialization/cleanup cycles.
    /// </summary>
    [Benchmark]
    public void Overhead_SetupTeardown()
    {
        // Measure minimal setup/teardown overhead
        PdfiumInterop.Shutdown();
        var initialized = PdfiumInterop.Initialize();
        if (!initialized)
        {
            throw new InvalidOperationException("PDFium initialization failed");
        }
        PdfiumInterop.Shutdown();
    }

    /// <summary>
    /// Measures DI container service resolution time (hot path).
    /// Tests how fast we can resolve services after container is built.
    /// </summary>
    [Benchmark]
    public void ServiceResolution_HotPath()
    {
        // Build container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
        services.AddSingleton<IPdfRenderingService, PdfRenderingService>();

        using var serviceProvider = services.BuildServiceProvider();

        // Measure resolution time (hot path)
        var documentService = serviceProvider.GetRequiredService<IPdfDocumentService>();
        var renderingService = serviceProvider.GetRequiredService<IPdfRenderingService>();

        if (documentService == null || renderingService == null)
        {
            throw new InvalidOperationException("Service resolution failed");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Ensure PDFium is shut down after all benchmarks complete
        PdfiumInterop.Shutdown();
    }
}
