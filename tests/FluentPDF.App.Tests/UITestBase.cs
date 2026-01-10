using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace FluentPDF.App.Tests;

/// <summary>
/// Base class for UI automation tests using FlaUI.
/// Provides helpers for launching and interacting with the WinUI 3 application.
/// </summary>
public abstract class UITestBase : IDisposable
{
    private UIA3Automation? _automation;
    private Application? _application;
    private bool _disposed;

    /// <summary>
    /// Gets the FlaUI automation instance for UI element interaction.
    /// </summary>
    protected UIA3Automation Automation
    {
        get
        {
            _automation ??= new UIA3Automation();
            return _automation;
        }
    }

    /// <summary>
    /// Launches the FluentPDF application and returns the main window.
    /// </summary>
    /// <param name="executablePath">Path to the FluentPDF.App.exe file.</param>
    /// <param name="waitTimeoutMs">Timeout in milliseconds to wait for the main window to appear.</param>
    /// <returns>The main window of the application.</returns>
    /// <exception cref="TimeoutException">Thrown if the main window doesn't appear within the timeout.</exception>
    protected Window LaunchApplication(string executablePath, int waitTimeoutMs = 5000)
    {
        _application = Application.Launch(executablePath);

        var mainWindow = _application.GetMainWindow(Automation, TimeSpan.FromMilliseconds(waitTimeoutMs));

        if (mainWindow == null)
        {
            throw new TimeoutException($"Main window did not appear within {waitTimeoutMs}ms");
        }

        return mainWindow;
    }

    /// <summary>
    /// Closes the application if it's running.
    /// </summary>
    protected void CloseApplication()
    {
        _application?.Close();
        _application?.Dispose();
        _application = null;
    }

    /// <summary>
    /// Disposes resources used by the test base.
    /// Ensures the application is closed and automation is disposed.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources used by the test base.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CloseApplication();
            _automation?.Dispose();
            _automation = null;
        }

        _disposed = true;
    }
}
