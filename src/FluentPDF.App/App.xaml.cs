using FluentPDF.App.Interfaces;
using FluentPDF.App.Services;
using FluentPDF.App.Services.RenderingStrategies;
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
        /// Gets the parsed command-line options.
        /// </summary>
        public static CommandLineOptions CommandLineOptions { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            System.Diagnostics.Debug.WriteLine("FluentPDF: App constructor starting...");
            System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: App constructor starting\n");

            // Parse command-line options early
            var args = Environment.GetCommandLineArgs();
            CommandLineOptions = CommandLineOptions.Parse(args);
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"),
                $"{DateTime.Now}: CLI args parsed - OpenFile: {CommandLineOptions.OpenFilePath ?? "none"}, AutoClose: {CommandLineOptions.AutoClose}, Console: {CommandLineOptions.EnableConsoleLogging}\n");

            // Attach console if requested (for CLI automation scenarios)
            if (CommandLineOptions.EnableConsoleLogging)
            {
                AttachConsole();
            }

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
                Log.Logger = SerilogConfiguration.CreateLogger(
                    CommandLineOptions.VerboseLogging ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information,
                    CommandLineOptions.LogOutputPath,
                    CommandLineOptions.EnableConsoleLogging);
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Serilog logger created\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"), $"{DateTime.Now}: Serilog creation failed: {ex}\n");
                throw;
            }

            Log.Information("FluentPDF application starting");
            Log.Information("Command-line options: OpenFile={OpenFile}, AutoClose={AutoClose}, Console={Console}, Verbose={Verbose}",
                CommandLineOptions.OpenFilePath ?? "none",
                CommandLineOptions.AutoClose,
                CommandLineOptions.EnableConsoleLogging,
                CommandLineOptions.VerboseLogging);
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
                    services.AddSingleton<MemoryMonitor>();
                    services.AddSingleton<RenderingObservabilityService>();
                    services.AddSingleton<UIBindingVerifier>();

                    // Register diagnostic command handler
                    services.AddSingleton<DiagnosticCommandHandler>();

                    // Register rendering strategies
                    services.AddTransient<IRenderingStrategy, WriteableBitmapRenderingStrategy>();
                    services.AddTransient<IRenderingStrategy, FileBasedRenderingStrategy>();
                    services.AddSingleton<RenderingStrategyFactory>();
                    services.AddSingleton<RenderingCoordinator>();

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
        /// Attaches a console window for console output (useful for CLI automation).
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        private void AttachConsole()
        {
            try
            {
                // Try to attach to parent process console first (if launched from cmd/PowerShell)
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                {
                    // If no parent console, allocate a new one
                    AllocConsole();
                }

                // Redirect console output
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FluentPDF Console Logging Enabled");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Debug log: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log")}");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_Debug.log"),
                    $"{DateTime.Now}: Failed to attach console: {ex.Message}\n");
            }
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

            // Handle CLI diagnostic commands (execute and exit before UI initialization)
            if (await HandleDiagnosticCommandsAsync())
            {
                // Diagnostic command executed, exit gracefully
                return;
            }

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
                string? fileToOpen = null;

                // Check command-line options first
                if (!string.IsNullOrEmpty(CommandLineOptions.OpenFilePath))
                {
                    fileToOpen = CommandLineOptions.OpenFilePath;
                    Log.Information("Opening file from CLI option: {FilePath}", fileToOpen);
                }
                else
                {
                    // Fallback: check raw command line arguments for file paths
                    var args = Environment.GetCommandLineArgs();
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
                            fileToOpen = arg;
                            Log.Information("Opening file from argument: {FilePath}", arg);
                            break;
                        }
                    }
                }

                // Open the file if found
                if (!string.IsNullOrEmpty(fileToOpen))
                {
                    var mainViewModel = GetService<MainViewModel>();
                    await mainViewModel.OpenRecentFileCommand.ExecuteAsync(fileToOpen);

                    // Auto-close if requested (for automated testing)
                    if (CommandLineOptions.AutoClose)
                    {
                        Log.Information("Auto-close enabled. Waiting {Delay} seconds before closing...", CommandLineOptions.AutoCloseDelay);
                        await Task.Delay(CommandLineOptions.AutoCloseDelay * 1000);
                        Log.Information("Auto-close: Closing application now");
                        await ShutdownAsync();
                        Environment.Exit(0);
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
        /// Handles CLI diagnostic commands and returns true if a command was executed.
        /// </summary>
        /// <returns>True if a diagnostic command was executed; otherwise, false.</returns>
        private async Task<bool> HandleDiagnosticCommandsAsync()
        {
            var options = CommandLineOptions;

            // Check for verbose mode and increase logging level
            if (options.VerboseLogging)
            {
                Log.Information("Verbose logging enabled for diagnostic commands");
            }

            // Handle --diagnostics command
            if (options.Diagnostics)
            {
                Log.Information("Executing diagnostics command");
                var handler = GetService<DiagnosticCommandHandler>();
                var exitCode = await handler.HandleDiagnosticsAsync();
                await ShutdownAsync();
                Environment.Exit(exitCode);
                return true;
            }

            // Handle --test-render command
            if (!string.IsNullOrEmpty(options.TestRender))
            {
                Log.Information("Executing test-render command for file: {FilePath}", options.TestRender);
                var handler = GetService<DiagnosticCommandHandler>();
                var exitCode = await handler.HandleTestRenderAsync(options.TestRender);
                await ShutdownAsync();
                Environment.Exit(exitCode);
                return true;
            }

            // Handle --render-test command
            if (!string.IsNullOrEmpty(options.RenderTest))
            {
                Log.Information("Executing render-test command for file: {FilePath}", options.RenderTest);
                var handler = GetService<DiagnosticCommandHandler>();
                var exitCode = await handler.HandleRenderTestAsync(options.RenderTest, options.OutputDirectory);
                await ShutdownAsync();
                Environment.Exit(exitCode);
                return true;
            }

            // No diagnostic command found
            return false;
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

            if (activeTab?.ViewerViewModel is PdfViewerViewModel viewModel)
            {
                await viewModel.LoadDocumentFromPathAsync(filePath);
            }
            else
            {
                throw new InvalidOperationException("No active PDF viewer tab found. Ensure a PDF viewer tab is open.");
            }
        }

        /// <summary>
        /// Test helper method to merge multiple PDF documents.
        /// This method is intended for E2E testing and bypasses the file picker UI.
        /// </summary>
        /// <param name="inputPaths">The paths to the PDF files to merge.</param>
        /// <param name="outputPath">The path where the merged PDF should be saved.</param>
        public async Task MergeDocumentsForTestingAsync(string[] inputPaths, string outputPath)
        {
            if (inputPaths == null || inputPaths.Length < 2)
            {
                throw new ArgumentException("At least 2 PDF files are required for merging.", nameof(inputPaths));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
            }

            // Get the current PdfViewerViewModel from the active tab
            var mainViewModel = GetService<MainViewModel>();
            var activeTab = mainViewModel.ActiveTab;

            if (activeTab?.ViewerViewModel is PdfViewerViewModel viewModel)
            {
                var editingService = GetService<IDocumentEditingService>();

                // Perform the merge operation
                var progress = new Progress<double>();
                var result = await editingService.MergeAsync(
                    inputPaths.ToList(),
                    outputPath,
                    progress,
                    CancellationToken.None);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"Merge failed: {result.Errors[0].Message}");
                }

                // Optionally load the merged document
                await viewModel.LoadDocumentFromPathAsync(outputPath);
            }
            else
            {
                throw new InvalidOperationException("No active PDF viewer tab found. Ensure a PDF viewer tab is open.");
            }
        }

        /// <summary>
        /// Test helper method to split a PDF document by page ranges.
        /// This method is intended for E2E testing and bypasses the file picker UI and dialogs.
        /// </summary>
        /// <param name="inputPath">The path to the PDF file to split.</param>
        /// <param name="pageRanges">Page ranges string (e.g., "1-5, 10, 15-20").</param>
        /// <param name="outputPath">The path where the split PDF should be saved.</param>
        public async Task SplitDocumentForTestingAsync(string inputPath, string pageRanges, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException("Input path cannot be null or empty.", nameof(inputPath));
            }

            if (string.IsNullOrWhiteSpace(pageRanges))
            {
                throw new ArgumentException("Page ranges cannot be null or empty.", nameof(pageRanges));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
            }

            // Load the document first
            await LoadDocumentForTestingAsync(inputPath);

            // Get the current PdfViewerViewModel from the active tab
            var mainViewModel = GetService<MainViewModel>();
            var activeTab = mainViewModel.ActiveTab;

            if (activeTab?.ViewerViewModel is PdfViewerViewModel viewModel)
            {
                var editingService = GetService<IDocumentEditingService>();

                // Perform the split operation
                var progress = new Progress<double>();
                var result = await editingService.SplitAsync(
                    inputPath,
                    pageRanges,
                    outputPath,
                    progress,
                    CancellationToken.None);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"Split failed: {result.Errors[0].Message}");
                }

                // Optionally load the split document to verify it
                await viewModel.LoadDocumentFromPathAsync(outputPath);
            }
            else
            {
                throw new InvalidOperationException("No active PDF viewer tab found. Ensure a PDF viewer tab is open.");
            }
        }

        /// <summary>
        /// Test helper method to apply a text watermark to a PDF document.
        /// This method is intended for E2E testing and bypasses the watermark dialog UI.
        /// </summary>
        /// <param name="filePath">The path to the PDF file to watermark.</param>
        /// <param name="text">The watermark text to apply.</param>
        /// <param name="fontSize">The font size for the watermark text.</param>
        /// <param name="opacity">The opacity percentage (0-100).</param>
        public async Task ApplyTextWatermarkForTestingAsync(string filePath, string text, double fontSize, double opacity)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Watermark text cannot be null or empty.", nameof(text));
            }

            // Load the document first
            await LoadDocumentForTestingAsync(filePath);

            // Get the current PdfViewerViewModel from the active tab
            var mainViewModel = GetService<MainViewModel>();
            var activeTab = mainViewModel.ActiveTab;

            if (activeTab?.ViewerViewModel is PdfViewerViewModel viewModel)
            {
                var watermarkService = GetService<IWatermarkService>();

                // Create watermark configuration
                var config = new TextWatermarkConfig
                {
                    Text = text,
                    FontFamily = "Arial",
                    FontSize = (float)fontSize,
                    Color = System.Drawing.Color.FromArgb(128, 128, 128),
                    Opacity = (float)(opacity / 100.0),
                    RotationDegrees = 45f,
                    Position = WatermarkPosition.Center,
                    BehindContent = false
                };

                // Apply the watermark to all pages
                var pageRange = WatermarkPageRange.All;
                var result = await watermarkService.ApplyTextWatermarkAsync(
                    viewModel.CurrentDocument!,
                    config,
                    pageRange);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"Watermark application failed: {result.Errors[0].Message}");
                }

                // Reload the document to see the applied watermark
                await viewModel.LoadDocumentFromPathAsync(filePath);
            }
            else
            {
                throw new InvalidOperationException("No active PDF viewer tab found. Ensure a PDF viewer tab is open.");
            }
        }

        /// <summary>
        /// Test helper method to insert an image into the current page of a PDF document.
        /// This method is intended for E2E testing and bypasses the file picker UI.
        /// </summary>
        /// <param name="filePath">The path to the PDF file.</param>
        /// <param name="imagePath">The path to the image file to insert.</param>
        /// <param name="x">The X position for the image (in PDF points).</param>
        /// <param name="y">The Y position for the image (in PDF points).</param>
        public async Task InsertImageForTestingAsync(string filePath, string imagePath, float x = 300, float y = 400)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));
            }

            // Load the document first
            await LoadDocumentForTestingAsync(filePath);

            // Get the current PdfViewerViewModel from the active tab
            var mainViewModel = GetService<MainViewModel>();
            var activeTab = mainViewModel.ActiveTab;

            if (activeTab?.ViewerViewModel is PdfViewerViewModel viewModel)
            {
                var imageInsertionService = GetService<IImageInsertionService>();

                // Insert the image at the specified position
                var result = await imageInsertionService.InsertImageAsync(
                    viewModel.CurrentDocument!,
                    viewModel.CurrentPageNumber,
                    imagePath,
                    new System.Drawing.PointF(x, y));

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException($"Image insertion failed: {result.Errors[0].Message}");
                }

                // Update the ViewModel's image collection
                viewModel.ImageInsertionViewModel.InsertedImages.Add(result.Value);
                viewModel.ImageInsertionViewModel.SelectedImage = result.Value;
            }
            else
            {
                throw new InvalidOperationException("No active PDF viewer tab found. Ensure a PDF viewer tab is open.");
            }
        }
    }
}
