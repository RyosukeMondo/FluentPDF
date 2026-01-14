using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace FluentPDF.E2E.Tests.Fixtures;

/// <summary>
/// Test fixture for launching and managing FluentPDF app lifecycle during E2E tests.
/// Implements IAsyncLifetime for proper async setup/teardown with xUnit.
/// </summary>
public class AppLaunchFixture : IAsyncLifetime, IDisposable
{
    private FlaUI.Core.Application? _application;
    private UIA3Automation? _automation;
    private bool _disposed;

    /// <summary>
    /// Gets the launched application instance.
    /// </summary>
    public FlaUI.Core.Application Application
    {
        get => _application ?? throw new InvalidOperationException("Application not launched. Call InitializeAsync first.");
    }

    /// <summary>
    /// Gets the UIA3 automation instance for UI automation.
    /// </summary>
    public UIA3Automation Automation
    {
        get => _automation ?? throw new InvalidOperationException("Automation not initialized. Call InitializeAsync first.");
    }

    /// <summary>
    /// Gets the main window of the application.
    /// </summary>
    public Window MainWindow { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the timeout for window acquisition (default: 10 seconds).
    /// </summary>
    public TimeSpan WindowTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Initializes the fixture by launching the app and acquiring the main window.
    /// Called automatically by xUnit before tests run.
    /// </summary>
    public async Task InitializeAsync()
    {
        _automation = new UIA3Automation();

        var exePath = LocateExecutable();
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException(
                $"FluentPDF.App.exe not found at: {exePath}. " +
                "Build the application first using: dotnet build src/FluentPDF.App -p:Platform=x64");
        }

        // Launch the application
        _application = FlaUI.Core.Application.Launch(exePath);

        // Wait for main window to appear with timeout
        await WaitForMainWindowAsync();
    }

    /// <summary>
    /// Cleans up resources by closing the app and disposing automation.
    /// Called automatically by xUnit after tests complete.
    /// </summary>
    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the fixture resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            // Close the application gracefully
            if (_application != null && !_application.HasExited)
            {
                _application.Close();

                // Wait a bit for graceful shutdown
                if (!_application.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(5)))
                {
                    // Force kill if graceful close failed
                    _application.Kill();
                }

                _application.Dispose();
            }

            _automation?.Dispose();
        }
        catch (Exception)
        {
            // Suppress exceptions during cleanup
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Locates the FluentPDF.App.exe executable.
    /// Searches in typical build output locations.
    /// </summary>
    private string LocateExecutable()
    {
        // Get the solution root directory (assuming tests are in tests/FluentPDF.E2E.Tests)
        var solutionRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", ".."));

        // Search for the executable in common build locations
        var searchPaths = new[]
        {
            // Release builds
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "x64", "Release", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "Release", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),

            // Debug builds
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "x64", "Debug", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "Debug", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Return the most likely path (first Release path) even if not found
        // This provides a helpful error message
        return searchPaths[0];
    }

    /// <summary>
    /// Waits for the main window to appear with a timeout.
    /// </summary>
    private async Task WaitForMainWindowAsync()
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < WindowTimeout)
        {
            try
            {
                // Try to get the main window
                var mainWindow = Application.GetMainWindow(Automation, WindowTimeout);

                if (mainWindow != null)
                {
                    MainWindow = mainWindow;

                    // Wait for window to be fully initialized and visible
                    mainWindow.WaitUntilResponsive(TimeSpan.FromSeconds(5));

                    return;
                }
            }
            catch
            {
                // Window not ready yet, continue waiting
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"Failed to acquire main window within {WindowTimeout.TotalSeconds} seconds. " +
            "The application may have failed to start or crashed during initialization.");
    }

    /// <summary>
    /// Restarts the application by closing and relaunching it.
    /// Useful for tests that need a fresh application state.
    /// </summary>
    public async Task RestartApplicationAsync()
    {
        // Close current instance
        if (_application != null && !_application.HasExited)
        {
            _application.Close();
            _application.Dispose();
        }

        // Relaunch
        var exePath = LocateExecutable();
        _application = FlaUI.Core.Application.Launch(exePath);
        await WaitForMainWindowAsync();
    }
}
