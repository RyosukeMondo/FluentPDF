using FluentPDF.App.Services;
using FluentPDF.Core.Logging;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Navigation;
using Serilog;

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
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // Initialize Serilog before anything else
            Log.Logger = SerilogConfiguration.CreateLogger();

            Log.Information("FluentPDF application starting");

            // Configure dependency injection container
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure logging with Serilog
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddSerilog(dispose: true);
                    });

                    // Register services
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<ITelemetryService, TelemetryService>();

                    // ViewModels will be registered here when created
                    // Example: services.AddTransient<MainViewModel>();
                })
                .Build();
        }

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
            // Start the host
            await _host.StartAsync();

            _window ??= new Window();

            if (_window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                _window.Content = rootFrame;

                // Configure NavigationService with the frame
                var navigationService = GetService<INavigationService>() as NavigationService;
                if (navigationService is not null)
                {
                    navigationService.Frame = rootFrame;
                }
            }

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            _window.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            var correlationId = Guid.NewGuid();
            Log.Fatal(e.Exception, "Navigation failed to {PageType} [CorrelationId: {CorrelationId}]",
                e.SourcePageType.FullName, correlationId);
            throw new Exception($"Failed to load Page {e.SourcePageType.FullName}. CorrelationId: {correlationId}");
        }

        /// <summary>
        /// Invoked when the application exits. Ensures proper disposal of Serilog.
        /// </summary>
        /// <param name="sender">The application instance.</param>
        /// <param name="e">Event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("FluentPDF application shutting down");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
