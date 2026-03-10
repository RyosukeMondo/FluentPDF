using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FluentPDF.Avalonia.Api;
using FluentPDF.Avalonia.Services;
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
using Serilog;
using System;
using System.Linq;
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
    private ILogger<App>? _logger;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public static IServiceProvider Services => _host.Services;

    /// <summary>
    /// Gets the main application window for API access.
    /// </summary>
    public MainWindow? MainWindow => _window;

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
#if DEBUG
        // Create early debug log BEFORE anything else (DEBUG mode only)
        var earlyLogDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(earlyLogDir);
        var earlyLogPath = Path.Combine(
            earlyLogDir,
            $"FluentPDF-Early-Debug-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        var earlyLog = new System.IO.StreamWriter(earlyLogPath, append: true) { AutoFlush = true };
#else
        System.IO.StreamWriter? earlyLog = null;
#endif
        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> App.Initialize() STARTED");

        DiagnosticLogger.LogSection("APP INITIALIZE");

        // CRITICAL: Call base.Initialize() FIRST to let Avalonia load XAML automatically
        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Calling base.Initialize() for XAML loading...");
        DiagnosticLogger.Log("Calling base.Initialize() for XAML loading...");
        try
        {
            base.Initialize();
            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> base.Initialize() completed - XAML loaded");
            DiagnosticLogger.Log("base.Initialize() completed successfully - XAML loaded");
        }
        catch (Exception ex)
        {
            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> base.Initialize() FAILED: {ex.Message}");
            DiagnosticLogger.LogError("Failed in base.Initialize()", ex);
#if DEBUG
            earlyLog?.Close();
#endif
            throw;
        }

        // Initialize Serilog after XAML is loaded
        try
        {
            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Creating Serilog logger...");
            DiagnosticLogger.Log("Creating Serilog logger...");
            Log.Logger = SerilogConfiguration.CreateLogger();
            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Serilog logger created");
            DiagnosticLogger.Log("Serilog logger created successfully");
        }
        catch (Exception ex)
        {
            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> SERILOG FAILED: {ex.Message}");
            DiagnosticLogger.LogError("Failed to initialize Serilog", ex);
#if DEBUG
            earlyLog?.Close();
#endif
            throw;
        }

        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Serilog startup logged");
        Log.Information("FluentPDF Avalonia application starting");
        DiagnosticLogger.Log("Logged startup message via Serilog");

        // Configure global exception handlers
        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Setting up exception handlers...");
        DiagnosticLogger.Log("Setting up exception handlers...");
        SetupExceptionHandlers();
        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Exception handlers configured");
        DiagnosticLogger.Log("Exception handlers configured");

        // Configure dependency injection container
        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Creating DI host...");
        DiagnosticLogger.Log("Creating DI host...");
        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> Configuring services...");
                    DiagnosticLogger.Log("Configuring services...");
                    ConfigureAllServices(services, earlyLog);
                    earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> All services registered");
                    DiagnosticLogger.Log("All services registered successfully");
                })
                .Build();

            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> DI host built");
            DiagnosticLogger.Log("DI host built successfully");
        }
        catch (Exception ex)
        {
            earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> DI host build FAILED: {ex.Message}");
            DiagnosticLogger.LogError("Failed to build DI host", ex);
#if DEBUG
            earlyLog?.Close();
#endif
            throw;
        }

        earlyLog?.WriteLine($"{DateTime.Now:HH:mm:ss.fff} >>> App.Initialize() COMPLETE");
#if DEBUG
        earlyLog?.Close();
#endif
        DiagnosticLogger.LogSection("APP INITIALIZE COMPLETE");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Get logger after DI is initialized
        _logger = GetService<ILogger<App>>();

        _logger.LogInformation("Framework initialization started");

        try
        {
            _logger.LogInformation("Initializing PDFium library (step {Step}/{Total})", 1, 5);
            DiagnosticLogger.Log("Initializing PDFium...");
            if (!PdfiumInterop.Initialize())
            {
                DiagnosticLogger.LogError("PDFium initialization FAILED");
                _logger.LogCritical("PDFium initialization failed - application cannot continue");
                Environment.Exit(1);
            }
            DiagnosticLogger.Log("PDFium initialized OK");
            _logger.LogInformation("PDFium library initialized successfully");

            // Handle --test-render CLI mode (headless, no UI)
            var cliOptions = CommandLineOptions.Current;
            DiagnosticLogger.Log($"CLI options: TestRender={cliOptions?.TestRender}, TestPdfPath={cliOptions?.TestPdfPath}");
            if (cliOptions?.TestRender == true && !string.IsNullOrEmpty(cliOptions.TestPdfPath))
            {
                DiagnosticLogger.Log($"Running test-render for: {cliOptions.TestPdfPath}");
                _logger.LogInformation("Running in --test-render mode for: {Path}", cliOptions.TestPdfPath);
                // Run on thread pool to avoid deadlocking the UI thread
                var exitCode = Task.Run(() => RunTestRender(cliOptions.TestPdfPath)).GetAwaiter().GetResult();
                DiagnosticLogger.Log($"Test-render exit code: {exitCode}");
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dt)
                {
                    dt.Shutdown(exitCode);
                }
                return;
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _logger.LogDebug("Retrieving MainViewModel from dependency injection container");
                var mainViewModel = GetService<FluentPDF.Core.ViewModels.MainViewModel>();
                _logger.LogDebug("MainViewModel instance obtained");

                _logger.LogInformation("Creating main application window (step {Step}/{Total})", 2, 5);
                _window = new MainWindow(mainViewModel, GetService<ILogger<MainWindow>>());
                _logger.LogDebug("MainWindow instance created");

                _logger.LogDebug("Assigning main window to desktop lifetime");
                desktop.MainWindow = _window;

                desktop.ShutdownRequested += (s, e) =>
                {
                    ShutdownAsync().GetAwaiter().GetResult();
                };
            }

            _logger.LogInformation("Calling base framework initialization (step {Step}/{Total})", 3, 5);
            base.OnFrameworkInitializationCompleted();
            _logger.LogDebug("Base framework initialization completed");

            // AFTER base call - force window visible and responsive
            _logger.LogDebug("Post-initialization: ensuring window visibility");
            if (_window != null)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        _window?.Show();
                        _window?.Activate();
                        _window?.Focus();
                        _logger?.LogDebug("Main window displayed and focused");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to display main window");
                    }
                }, global::Avalonia.Threading.DispatcherPriority.Send);
            }

            // API server - start in background thread to avoid blocking UI
            StartApiServerIfRequested();

            _logger.LogInformation("Framework initialization completed (step {Step}/{Total})", 5, 5);
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Fatal error during framework initialization");
            Log.Fatal(ex, "Application initialization failed");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Runs a headless test-render of a PDF file and returns an exit code.
    /// 0 = success, 1 = load failed, 2 = render failed.
    /// </summary>
    private async Task<int> RunTestRender(string pdfPath)
    {
        try
        {
            var docService = GetService<IPdfDocumentService>();
            var renderService = GetService<IPdfRenderingService>();

            _logger!.LogInformation("Test-render: loading {Path}", pdfPath);
            var loadResult = await docService.LoadDocumentAsync(pdfPath);
            if (loadResult.IsFailed)
            {
                _logger.LogError("Test-render: load failed - {Error}", loadResult.Errors[0].Message);
                Console.WriteLine($"LOAD FAILED: {loadResult.Errors[0].Message}");
                return 1;
            }

            var doc = loadResult.Value;
            _logger.LogInformation("Test-render: loaded {Pages} pages", doc.PageCount);

            var renderResult = await renderService.RenderPageToRawAsync(doc, 1, 1.0, 96.0);
            if (!renderResult.IsSuccess)
            {
                _logger.LogError("Test-render: render failed - {Error}", renderResult.Errors.FirstOrDefault()?.Message);
                Console.WriteLine($"RENDER FAILED: {renderResult.Errors.FirstOrDefault()?.Message}");
                docService.CloseDocument(doc);
                return 2;
            }

            var raw = renderResult.Value;
            _logger.LogInformation("Test-render: success ({Width}x{Height}, {Bytes} bytes)",
                raw.Width, raw.Height, raw.Pixels.Length);
            Console.WriteLine($"TEST-RENDER OK: page 1 rendered at {raw.Width}x{raw.Height}");

            docService.CloseDocument(doc);
            return 0;
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Test-render: exception");
            Console.WriteLine($"TEST-RENDER EXCEPTION: {ex.Message}");
            return 2;
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
}
