using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using FluentPDF.App.Views;
using FluentPDF.Core.Logging;
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
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace FluentPDF.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;
        private Window? _window;

        /// <summary>
        /// Gets the main application window.
        /// </summary>
        public static Window MainWindow { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            System.Diagnostics.Debug.WriteLine("FluentPDF: App constructor starting...");
            System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: App constructor starting\n");

            try
            {
                this.InitializeComponent();
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: InitializeComponent completed\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: InitializeComponent failed: {ex}\n");
                throw;
            }

            // Initialize Serilog before anything else
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Creating Serilog logger...\n");
                Log.Logger = SerilogConfiguration.CreateLogger();
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Serilog logger created\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Serilog creation failed: {ex}\n");
                throw;
            }

            Log.Information("FluentPDF application starting");
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Setting up exception handlers...\n");

            // Configure global exception handlers
            SetupExceptionHandlers();
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Exception handlers configured\n");

            // Configure dependency injection container
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Creating Host...\n");
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure logging with Serilog
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddSerilog(dispose: true);
                    });

                    // Configure OpenTelemetry with graceful fallback if Aspire not running
                    ConfigureOpenTelemetry(services);

                    // Register PDF services (PDFium will be initialized lazily on first use)
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
                    services.AddSingleton<IThumbnailRenderingService, ThumbnailRenderingService>();
                    services.AddSingleton<IImageInsertionService, ImageInsertionService>();
                    services.AddSingleton<IWatermarkService, WatermarkService>();

                    // Register HiDPI and rendering services
                    services.AddSingleton<IDpiDetectionService, DpiDetectionService>();
                    services.AddSingleton<IRenderingSettingsService, RenderingSettingsService>();

                    // Register conversion services
                    services.AddSingleton<IDocxParserService, DocxParserService>();
                    services.AddSingleton<IHtmlToPdfService, HtmlToPdfService>();
                    services.AddSingleton<IQualityValidationService, LibreOfficeValidator>();
                    services.AddSingleton<IDocxConverterService, DocxConverterService>();

                    // Register application services
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<ITelemetryService, TelemetryService>();
                    services.AddSingleton<IRecentFilesService, RecentFilesService>();
                    services.AddSingleton<JumpListService>();
                    services.AddSingleton<ISettingsService, SettingsService>();

                    // Register observability services
                    services.AddSingleton<IMetricsCollectionService, MetricsCollectionService>();
                    services.AddSingleton<ILogExportService, LogExportService>();

                    // Register ViewModels
                    services.AddSingleton<MainViewModel>(); // Singleton for main window state
                    services.AddTransient<PdfViewerViewModel>();
                    services.AddTransient<ConversionViewModel>();
                    services.AddTransient<BookmarksViewModel>();
                    services.AddTransient<FormFieldViewModel>();
                    services.AddTransient<AnnotationViewModel>();
                    services.AddTransient<DiagnosticsPanelViewModel>();
                    services.AddTransient<LogViewerViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<ThumbnailsViewModel>();
                    services.AddTransient<ImageInsertionViewModel>();
                    services.AddTransient<WatermarkViewModel>();
                })
                .Build();
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Host built successfully. Constructor complete.\n");
        }

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        public IServiceProvider Services => _host.Services;

        /// <summary>
        /// Gets a service from the dependency injection container.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public T GetService<T>() where T : notnull
        {
            return _host.Services.GetRequiredService<T>();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: OnLaunched called\n");

            // Initialize PDFium library before starting the application
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Initializing PDFium...\n");
            var initialized = PdfiumInterop.Initialize();
            if (!initialized)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: PDFium initialization FAILED\n");
                Log.Fatal("Failed to initialize PDFium library");
                throw new InvalidOperationException("Failed to initialize PDFium. Please ensure pdfium.dll is available.");
            }
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: PDFium initialized successfully\n");
            Log.Information("PDFium library initialized successfully");

            // Start the host
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Starting host...\n");
            await _host.StartAsync();
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Host started\n");

            // Initialize and load settings
            try
            {
                var settingsService = GetService<ISettingsService>();
                await settingsService.LoadAsync();

                // Subscribe to settings changes and apply current theme
                settingsService.SettingsChanged += OnSettingsChanged;
                ApplyTheme(settingsService.Settings.Theme);

                Log.Information("Settings service initialized and theme applied");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to initialize settings service. Application will continue with default settings.");
            }

            // Initialize metrics collection service
            try
            {
                var metricsService = GetService<IMetricsCollectionService>();
                Log.Information("Metrics collection service initialized");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to initialize metrics collection service. Application will continue without metrics.");
            }

            // Log form services registration
            Log.Information("Form services registered: IPdfFormService, IFormValidationService, FormFieldViewModel");

            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Creating window...\n");
            if (_window is null)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Getting MainViewModel from DI...\n");
                var mainViewModel = GetService<MainViewModel>();
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Got MainViewModel. Getting JumpListService...\n");
                var jumpListService = GetService<JumpListService>();
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Got JumpListService. Creating MainWindow instance...\n");
                _window = new Views.MainWindow(mainViewModel, jumpListService);
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: MainWindow created\n");
                MainWindow = _window;
                _window.Closed += async (s, e) => await ShutdownAsync();
            }

            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Activating window...\n");
            _window.Activate();
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Window activated\n");

            // Handle file activation from Jump List or command line
            await HandleFileActivationAsync();
        }

        /// <summary>
        /// Handles file activation from Jump List, file associations, or command line arguments.
        /// </summary>
        private async Task HandleFileActivationAsync()
        {
            try
            {
                // Check command line arguments for file paths
                // This handles both Jump List and file association activation
                var args = Environment.GetCommandLineArgs();

                // First argument is the executable path, check for file paths after that
                for (int i = 1; i < args.Length; i++)
                {
                    var arg = args[i];

                    // Skip flags/options that start with - or /
                    if (arg.StartsWith("-") || arg.StartsWith("/"))
                    {
                        continue;
                    }

                    // Check if it's a valid PDF file path
                    if (File.Exists(arg) && arg.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Information("Opening file from activation: {FilePath}", arg);
                        var mainViewModel = GetService<MainViewModel>();
                        await mainViewModel.OpenRecentFileCommand.ExecuteAsync(arg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to handle file activation");
            }
        }

        /// <summary>
        /// Sets up global exception handlers to catch and log unhandled exceptions.
        /// </summary>
        private void SetupExceptionHandlers()
        {
            // UI Thread exceptions
            this.UnhandledException += OnUnhandledException;

            // Background task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            Log.Debug("Global exception handlers configured");
        }

        /// <summary>
        /// Handles unhandled exceptions on the UI thread.
        /// </summary>
        private async void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            var correlationId = Guid.NewGuid();
            Log.Fatal(e.Exception, "Unhandled UI exception [CorrelationId: {CorrelationId}]", correlationId);

            // Try to show error dialog
            try
            {
                var dialog = new ErrorDialog(
                    "An unexpected error occurred. The application will attempt to continue.",
                    correlationId.ToString()
                );

                // Set XamlRoot for the dialog
                if (_window?.Content is FrameworkElement rootElement)
                {
                    dialog.XamlRoot = rootElement.XamlRoot;
                    await dialog.ShowAsync();
                }
            }
            catch (Exception dialogException)
            {
                Log.Error(dialogException, "Failed to display error dialog [CorrelationId: {CorrelationId}]", correlationId);
            }

            // Mark as handled to prevent crash if possible
            e.Handled = true;
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
        private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
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
        /// This is called from the Window.Closed event.
        /// </summary>
        internal async Task ShutdownAsync()
        {
            Log.Information("FluentPDF application shutting down");

            // Cleanup observability services
            try
            {
                // Services will be disposed automatically when the host is disposed
                // as they are registered as singletons in the DI container
                Log.Information("Observability services cleanup initiated");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to cleanup observability services");
            }

            // Shutdown PDFium library
            PdfiumInterop.Shutdown();
            Log.Information("PDFium library shut down");

            await _host.StopAsync();
            _host.Dispose();
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Handles settings changes and applies theme updates.
        /// </summary>
        private void OnSettingsChanged(object? sender, AppSettings settings)
        {
            ApplyTheme(settings.Theme);
        }

        /// <summary>
        /// Applies the specified theme to the application.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        private void ApplyTheme(AppTheme theme)
        {
            try
            {
                // Note: RequestedTheme can only be set before the first window is created
                // For runtime theme changes, we need to set it on the window's content
                if (_window?.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = theme switch
                    {
                        AppTheme.Light => ElementTheme.Light,
                        AppTheme.Dark => ElementTheme.Dark,
                        AppTheme.UseSystem => ElementTheme.Default,
                        _ => ElementTheme.Default
                    };
                    Log.Information("Theme changed to {Theme}", theme);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to apply theme {Theme}", theme);
            }
        }

        /// <summary>
        /// Configures OpenTelemetry metrics and tracing with OTLP exporters for .NET Aspire Dashboard.
        /// Gracefully degrades if Aspire is not running.
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
                    .AddService("FluentPDF.Desktop", serviceVersion: version)
                    .AddAttributes(new Dictionary<string, object>
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
                Log.Warning(ex, "Failed to configure OpenTelemetry. Application will continue without OTLP export to Aspire Dashboard.");
            }
        }

        /// <summary>
        /// Test helper method to load a document from a file path.
        /// This method is intended for E2E testing and bypasses the file picker UI.
        /// </summary>
        /// <param name="filePath">The path to the PDF file to load.</param>
        public async Task LoadDocumentForTestingAsync(string filePath)
        {
            // Get the current PdfViewerViewModel from the active tab
            var mainViewModel = GetService<MainViewModel>();
            var activeTab = mainViewModel.ActiveTab;

            if (activeTab?.Content is PdfViewerPage pdfViewerPage)
            {
                var viewModel = pdfViewerPage.ViewModel;
                await viewModel.LoadDocumentFromPathAsync(filePath);
            }
            else
            {
                throw new InvalidOperationException("No active PdfViewerPage found. Ensure a PDF viewer tab is open.");
            }
        }
    }
}
