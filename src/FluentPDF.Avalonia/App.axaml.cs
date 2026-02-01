using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FluentPDF.Avalonia.Api;
using FluentPDF.Avalonia.Services;
using FluentPDF.Avalonia.Services.RenderingStrategies;
using FluentPDF.Avalonia.ViewModels;
using FluentPDF.Avalonia.Views;
using FluentPDF.Core.Logging;
using FluentPDF.Core.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static IHost _host = null!;
    private MainWindow? _window;
    private IVerificationApiServer? _apiServer;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public static IServiceProvider Services => _host.Services;

    /// <summary>
    /// Gets a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    public static T GetService<T>() where T : notnull
    {
        return _host.Services.GetRequiredService<T>();
    }

    public override void Initialize()
    {
        // Create early debug log BEFORE anything else
        var earlyLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"FluentPDF-Early-Debug-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        var earlyLog = new System.IO.StreamWriter(earlyLogPath, append: true) { AutoFlush = true };
        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> App.Initialize() STARTED");

        DiagnosticLogger.LogSection("APP INITIALIZE");

        // Initialize Serilog before anything else
        try
        {
            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Creating Serilog logger...");
            DiagnosticLogger.Log("Creating Serilog logger...");
            Log.Logger = SerilogConfiguration.CreateLogger();
            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Serilog logger created");
            DiagnosticLogger.Log("Serilog logger created successfully");
        }
        catch (Exception ex)
        {
            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> SERILOG FAILED: {ex.Message}");
            DiagnosticLogger.LogError("Failed to initialize Serilog", ex);
            earlyLog.Close();
            throw;
        }

        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Serilog startup logged");
        Log.Information("FluentPDF Avalonia application starting");
        DiagnosticLogger.Log("Logged startup message via Serilog");

        // Configure global exception handlers
        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Setting up exception handlers...");
        DiagnosticLogger.Log("Setting up exception handlers...");
        SetupExceptionHandlers();
        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Exception handlers configured");
        DiagnosticLogger.Log("Exception handlers configured");

        // Configure dependency injection container
        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Creating DI host...");
        DiagnosticLogger.Log("Creating DI host...");
        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Configuring services...");
                    DiagnosticLogger.Log("Configuring services...");

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
                services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
                services.AddSingleton<IPdfRenderingService, PdfRenderingService>();
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

                // Register HiDPI and rendering services
                services.AddSingleton<IDpiDetectionService, DpiDetectionService>();
                services.AddSingleton<IRenderingSettingsService, RenderingSettingsService>();

                // Register conversion services
                services.AddSingleton<IDocxParserService, DocxParserService>();
                services.AddSingleton<IHtmlToPdfService, HtmlToPdfService>();
                services.AddSingleton<IQualityValidationService, LibreOfficeValidator>();
                services.AddSingleton<IDocxConverterService, DocxConverterService>();

                // Register Avalonia-specific application services
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

                // Register ViewModels (100% reusable from WinUI 3 and Core)
                services.AddSingleton<FluentPDF.Core.ViewModels.MainViewModel>();
                services.AddTransient<FluentPDF.Core.ViewModels.PdfViewerViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.ConversionViewModel>();
                services.AddTransient<FluentPDF.Core.ViewModels.BookmarksViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.FormFieldViewModel>();
                services.AddTransient<FluentPDF.Core.ViewModels.AnnotationViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.SettingsViewModel>();
                services.AddSingleton<FluentPDF.Core.ViewModels.ThumbnailsViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.ImageInsertionViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.WatermarkViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.DiagnosticsPanelViewModel>();
                services.AddTransient<FluentPDF.Avalonia.ViewModels.LogViewerViewModel>();

                // Register operation watchdog for autonomous error detection
                services.AddSingleton<IOperationWatchdog, OperationWatchdog>();

                // Register API server
                services.AddSingleton<IVerificationApiServer, VerificationApiServer>();

                earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> All services registered");
                DiagnosticLogger.Log("All services registered successfully");
            })
            .Build();

            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> DI host built");
            DiagnosticLogger.Log("DI host built successfully");
        }
        catch (Exception ex)
        {
            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> DI host build FAILED: {ex.Message}");
            DiagnosticLogger.LogError("Failed to build DI host", ex);
            earlyLog.Close();
            throw;
        }

        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Loading XAML...");
        DiagnosticLogger.Log("Loading XAML...");
        try
        {
            AvaloniaXamlLoader.Load(this);
            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> XAML loaded");
            DiagnosticLogger.Log("XAML loaded successfully");
        }
        catch (Exception ex)
        {
            earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> XAML load FAILED: {ex.Message}");
            DiagnosticLogger.LogError("Failed to load XAML", ex);
            earlyLog.Close();
            throw;
        }

        earlyLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> App.Initialize() COMPLETE");
        earlyLog.Close();
        DiagnosticLogger.LogSection("APP INITIALIZE COMPLETE");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Create debug log file for hang diagnostics
        var debugLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"FluentPDF-Hang-Debug-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        var debugLog = new System.IO.StreamWriter(debugLogPath, append: true) { AutoFlush = true };

        void LogBoth(string message)
        {
            Console.WriteLine(message);
            debugLog.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
        }

        LogBoth(">>> [1/7] OnFrameworkInitializationCompleted STARTED");
        LogBoth($">>> Debug log: {debugLogPath}");

        try
        {
            LogBoth(">>> [2/7] Initializing PDFium...");
            if (!PdfiumInterop.Initialize())
            {
                LogBoth("FATAL: PDFium initialization failed");
                debugLog.Close();
                Environment.Exit(1);
            }
            LogBoth(">>> [3/7] PDFium initialized successfully");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                LogBoth(">>> [4/7] Getting MainViewModel from DI...");
                var mainViewModel = GetService<FluentPDF.Core.ViewModels.MainViewModel>();
                LogBoth(">>> [4.5/7] MainViewModel obtained");

                LogBoth(">>> [5/7] Creating MainWindow...");
                _window = new MainWindow(mainViewModel, GetService<ILogger<MainWindow>>());
                LogBoth(">>> [5.5/7] MainWindow created");

                LogBoth(">>> [6/7] Setting desktop.MainWindow...");
                desktop.MainWindow = _window;
                LogBoth(">>> [6.5/7] desktop.MainWindow set");

                desktop.ShutdownRequested += (s, e) =>
                {
                    debugLog.Close();
                    ShutdownAsync().GetAwaiter().GetResult();
                };
            }

            LogBoth(">>> [7/7] Calling base.OnFrameworkInitializationCompleted()...");
            base.OnFrameworkInitializationCompleted();
            LogBoth(">>> [7.5/7] base.OnFrameworkInitializationCompleted() returned");

            // AFTER base call - force window visible and responsive
            LogBoth(">>> [7.6/7] Post-init: Making window visible...");
            if (_window != null)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        _window?.Show();
                        _window?.Activate();
                        _window?.Focus();
                        Console.WriteLine(">>> Window should now be visible and focused");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($">>> Error making window visible: {ex.Message}");
                    }
                }, global::Avalonia.Threading.DispatcherPriority.Send);
            }

            // API server - start in background thread to avoid blocking UI
            var cmdOptions = CommandLineOptions.Current;
            if (cmdOptions?.ApiServer == true)
            {
                LogBoth(">>> API server mode detected");
                LogBoth($">>> Starting API server on port {cmdOptions.Port}...");

                // Start API server in background
                Task.Run(async () =>
                {
                    try
                    {
                        // Wait for UI to be fully ready
                        await Task.Delay(2000);

                        _apiServer = GetService<IVerificationApiServer>();
                        await _apiServer.StartAsync(cmdOptions.Port, "localhost");

                        var msg = $">>> API server started successfully on port {cmdOptions.Port}";
                        Console.WriteLine(msg);
                        Log.Information(msg);
                    }
                    catch (Exception ex)
                    {
                        var msg = $">>> API server failed to start: {ex.Message}";
                        Console.WriteLine(msg);
                        Log.Error(ex, "API server startup failed");
                    }
                });
            }

            LogBoth(">>> OnFrameworkInitializationCompleted COMPLETE");

            // Keep debug log open a bit longer if API server is starting
            if (cmdOptions?.ApiServer == true)
            {
                LogBoth(">>> Waiting for API server initialization (3 seconds)...");
                System.Threading.Thread.Sleep(3000);
                LogBoth(">>> API server should be starting now");
            }

            debugLog.Close();
        }
        catch (Exception ex)
        {
            LogBoth($"FATAL ERROR in OnFrameworkInitializationCompleted: {ex.Message}");
            LogBoth($"Stack trace: {ex.StackTrace}");
            debugLog.Close();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Disables Avalonia's built-in data annotation validation to avoid conflicts with CommunityToolkit.Mvvm.
    /// </summary>
    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    /// <summary>
    /// Sets up global exception handlers to catch and log unhandled exceptions.
    /// </summary>
    private void SetupExceptionHandlers()
    {
        // Background task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        Log.Debug("Global exception handlers configured");
    }

    /// <summary>
    /// Handles unobserved exceptions from background tasks.
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var correlationId = Guid.NewGuid();
        Log.Fatal(e.Exception, "Unobserved task exception [CorrelationId: {CorrelationId}]", correlationId);

        // Mark as observed to prevent crash
        e.SetObserved();
    }

    /// <summary>
    /// Handles unhandled exceptions on non-UI threads.
    /// </summary>
    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var correlationId = Guid.NewGuid();
        var exception = e.ExceptionObject as Exception;

        Log.Fatal(exception, "Domain unhandled exception [CorrelationId: {CorrelationId}] [IsTerminating: {IsTerminating}]",
            correlationId, e.IsTerminating);

        // Cannot prevent crash in this handler, but log is saved
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Ensures proper cleanup when the application host stops.
    /// </summary>
    internal async Task ShutdownAsync()
    {
        Log.Information("FluentPDF Avalonia application shutting down");

        // Stop API server if running
        if (_apiServer?.IsRunning == true)
        {
            await _apiServer.StopAsync();
            Log.Information("API server stopped");
        }

        // Shutdown PDFium library
        PdfiumInterop.Shutdown();
        Log.Information("PDFium library shut down");

        await _host.StopAsync();
        _host.Dispose();
        Log.CloseAndFlush();
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
