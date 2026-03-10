using FluentPDF.Avalonia.Api;
using FluentPDF.Avalonia.Services;
using FluentPDF.Avalonia.Services.RenderingStrategies;
using FluentPDF.Avalonia.ViewModels;
using FluentPDF.Core.Logging;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia;

public partial class App
{
    /// <summary>
    /// Registers all application services in the DI container.
    /// </summary>
    private static void ConfigureAllServices(IServiceCollection services, System.IO.StreamWriter? earlyLog)
    {
        // Configure logging with Serilog
        DiagnosticLogger.Log("Adding Serilog to DI...");
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
        DiagnosticLogger.Log("Serilog added to DI");

        // Configure OpenTelemetry with graceful fallback
        ConfigureOpenTelemetry(services);

        // Register PDF services
        RegisterPdfServices(services);

        // Register Avalonia-specific application services
        RegisterApplicationServices(services);

        // Register ViewModels
        RegisterViewModels(services);

        // Register animation service (implements Core.IAnimationService for ViewModel injection)
        services.AddSingleton<Core.Services.IAnimationService, FluentPDF.Avalonia.Services.AnimationService>();

        // Register API server
        services.AddSingleton<IVerificationApiServer, VerificationApiServer>();
    }

    /// <summary>
    /// Registers PDF-related services including rendering, editing, and shape drawing.
    /// </summary>
    private static void RegisterPdfServices(IServiceCollection services)
    {
        services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
        services.AddSingleton<IPdfRenderingService>(sp =>
            new PdfRenderingService(
                sp.GetRequiredService<ILogger<PdfRenderingService>>(),
                sp.GetRequiredService<FluentPDF.Rendering.Services.PageHandleCache>()));
        services.AddSingleton<IDocumentEditingService, DocumentEditingService>();
        services.AddSingleton<IPageOperationsService, PageOperationsService>();
        services.AddSingleton<IBookmarkService, BookmarkService>();
        services.AddSingleton<IPdfFormService, PdfFormService>();
        services.AddSingleton<IFormValidationService, FormValidationService>();
        services.AddSingleton<ITextExtractionService, TextExtractionService>();
        services.AddSingleton<ITextSearchService, TextSearchService>();
        services.AddSingleton<IAnnotationService, AnnotationService>();
        services.AddSingleton<ICoordinateMapper, CoordinateMapper>();
        services.AddSingleton<IThumbnailRenderingService, ThumbnailRenderingService>();
        services.AddSingleton<IImageInsertionService, ImageInsertionService>();
        services.AddSingleton<IWatermarkService, WatermarkService>();
        services.AddSingleton<IImageExportService, ImageExportService>();
        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddSingleton<IStampService, StampService>();
        services.AddSingleton<ITextReplacementService, TextReplacementService>();
        services.AddSingleton<IFdfService, FdfService>();
        services.AddSingleton<Core.Services.IMetricsCollectionService, MetricsCollectionService>();
        services.AddSingleton<Core.Services.ILogExportService, LogExportService>();
        services.AddSingleton<IUndoRedoService, UndoRedoService>();

        // Register page handle cache and content stream patcher
        services.AddSingleton<FluentPDF.Rendering.Services.PageHandleCache>();
        services.AddSingleton<FluentPDF.Rendering.Services.ContentStreamPatcher>();

        // Register shape drawing service with document resolver
        services.AddSingleton<IShapeService>(sp =>
        {
            var shapeLogger = sp.GetRequiredService<ILogger<FluentPDF.Rendering.Services.ShapeService>>();
            var patcher = sp.GetRequiredService<FluentPDF.Rendering.Services.ContentStreamPatcher>();
            var pageCache = sp.GetRequiredService<FluentPDF.Rendering.Services.PageHandleCache>();
            Func<string, FluentPDF.Core.Models.PdfDocument?> docResolver = filePath =>
            {
                var mainVm = sp.GetRequiredService<FluentPDF.Core.ViewModels.MainViewModel>();
                var activeDoc = mainVm.ActiveTab?.ViewerViewModel?.CurrentDocument;
                if (activeDoc != null && activeDoc.FilePath == filePath)
                    return activeDoc;
                // Search all tabs
                foreach (var tab in mainVm.Tabs)
                {
                    if (tab.ViewerViewModel?.CurrentDocument?.FilePath == filePath)
                        return tab.ViewerViewModel.CurrentDocument;
                }
                return null;
            };
            return new FluentPDF.Rendering.Services.ShapeService(shapeLogger, docResolver, patcher, pageCache);
        });

        // Register conversion services
        services.AddSingleton<IDocxParserService, DocxParserService>();
        services.AddSingleton<IHtmlToPdfService, HtmlToPdfService>();
        services.AddSingleton<IQualityValidationService, LibreOfficeValidator>();
        services.AddSingleton<IDocxConverterService, DocxConverterService>();
    }

    /// <summary>
    /// Registers Avalonia-specific application services (navigation, settings, rendering).
    /// </summary>
    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // Register HiDPI and rendering services
        services.AddSingleton<IDpiDetectionService, DpiDetectionService>();
        services.AddSingleton<IRenderingSettingsService, RenderingSettingsService>();

        services.AddSingleton<INavigationService, AvaloniaNavigationService>();
        services.AddSingleton<ISettingsService, AvaloniaSettingsService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();
        services.AddSingleton<ILogBufferService, LogBufferService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // Register rendering strategies (Avalonia-specific)
        services.AddTransient<IRenderingStrategy>(sp =>
            new SkiaRenderingStrategy(
                sp.GetRequiredService<ILogger<SkiaRenderingStrategy>>()));
        services.AddSingleton<RenderingStrategyFactory>();
        services.AddSingleton<RenderingCoordinator>();
    }

    /// <summary>
    /// Registers ViewModels and their factory functions.
    /// </summary>
    private static void RegisterViewModels(IServiceCollection services)
    {
        // Register ViewModels (100% reusable from WinUI 3 and Core)
        services.AddTransient<FluentPDF.Core.ViewModels.NavigationViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.ZoomViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.SearchPanelViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.ViewStateViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.PdfViewerViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.ConversionViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.BookmarksViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.AnnotationsListViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.FormFieldViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.AnnotationViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.SettingsViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.ThumbnailsViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.ImageInsertionViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.WatermarkViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.DiagnosticsPanelViewModel>();
        services.AddTransient<FluentPDF.Avalonia.ViewModels.LogViewerViewModel>();
        services.AddTransient<FluentPDF.Core.ViewModels.MetadataViewModel>();

        // Register factory functions for ViewModels that require dynamic creation
        services.AddSingleton<Func<FluentPDF.Core.ViewModels.PdfViewerViewModel>>(sp =>
            () =>
            {
                var vm = sp.GetRequiredService<FluentPDF.Core.ViewModels.PdfViewerViewModel>();
                vm.Thumbnails = sp.GetRequiredService<FluentPDF.Core.ViewModels.ThumbnailsViewModel>();
                vm.Bookmarks = sp.GetRequiredService<FluentPDF.Core.ViewModels.BookmarksViewModel>();
                vm.AnnotationsList = sp.GetRequiredService<FluentPDF.Core.ViewModels.AnnotationsListViewModel>();
                vm.Search = sp.GetRequiredService<FluentPDF.Core.ViewModels.SearchPanelViewModel>();
                vm.Metadata = sp.GetRequiredService<FluentPDF.Core.ViewModels.MetadataViewModel>();
                return vm;
            });

        services.AddSingleton<Func<string, FluentPDF.Core.ViewModels.PdfViewerViewModel, FluentPDF.Core.ViewModels.TabViewModel>>(sp =>
            (filePath, viewerViewModel) => new FluentPDF.Core.ViewModels.TabViewModel(
                filePath,
                viewerViewModel,
                sp.GetRequiredService<ILogger<FluentPDF.Core.ViewModels.TabViewModel>>()));

        // Register MainViewModel after factories are configured
        services.AddSingleton<FluentPDF.Core.ViewModels.MainViewModel>();
    }

    /// <summary>
    /// Starts the API server in background if requested via CLI or in debug mode.
    /// </summary>
    private void StartApiServerIfRequested()
    {
        var cmdOptions = CommandLineOptions.Current;
        if (cmdOptions?.ApiServer == true)
        {
            _logger!.LogInformation("API server mode enabled - starting verification API (step {Step}/{Total})", 4, 5);
            _logger.LogInformation("API server will listen on port {Port}", cmdOptions.Port);

            // Start API server in background
            Task.Run(async () =>
            {
                try
                {
                    // Wait for UI to be fully ready
                    await Task.Delay(2000);

                    _apiServer = GetService<IVerificationApiServer>();
                    await _apiServer.StartAsync(cmdOptions.Port, "localhost");

                    _logger.LogInformation("Verification API server started successfully on port {Port}", cmdOptions.Port);
                    Log.Information("Verification API server started on port {Port}", cmdOptions.Port);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start verification API server on port {Port}", cmdOptions.Port);
                    Log.Error(ex, "API server startup failed");
                }
            });
        }

#if DEBUG
        // Auto-start API server in debug mode for GUI automation testing
        if (cmdOptions?.ApiServer != true)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000); // Wait for UI to be ready
                    _apiServer = GetService<IVerificationApiServer>();
                    await _apiServer.StartAsync(5000, "localhost");
                    _logger!.LogInformation("Debug API server started on port 5000");
                }
                catch (Exception ex)
                {
                    _logger!.LogWarning(ex, "Debug API server failed to start (non-fatal)");
                }
            });
        }
#endif
    }

    /// <summary>
    /// Configures OpenTelemetry metrics and tracing with OTLP exporters.
    /// Gracefully degrades if endpoint is not available.
    /// </summary>
    private static void ConfigureOpenTelemetry(IServiceCollection services)
    {
        try
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";

            // Configure resource attributes
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService("FluentPDF.Avalonia", serviceVersion: version)
                .AddAttributes(new System.Collections.Generic.Dictionary<string, object>
                {
                    ["deployment.environment"] = "development"
                });

            // Configure MeterProvider with OTLP exporter
            services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddMeter("FluentPDF.Rendering")
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri("http://localhost:4317");
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });
                })
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddSource("FluentPDF.Rendering")
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri("http://localhost:4317");
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                        });
                });

            Log.Debug("OpenTelemetry configured with OTLP exporters to localhost:4317");
        }
        catch (Exception ex)
        {
            // Graceful fallback: log error but continue without OpenTelemetry
            Log.Warning(ex, "Failed to configure OpenTelemetry. Application will continue without OTLP export.");
        }
    }
}
