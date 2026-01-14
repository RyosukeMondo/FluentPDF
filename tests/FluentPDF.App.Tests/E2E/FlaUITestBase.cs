using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;

namespace FluentPDF.App.Tests.E2E;

/// <summary>
/// Base class for FlaUI-based E2E tests with screenshot capture on failure.
/// Extends UITestBase with additional test infrastructure for E2E scenarios.
/// </summary>
public abstract class FlaUITestBase : UITestBase
{
    private readonly string _screenshotDirectory;

    /// <summary>
    /// Initializes a new instance of FlaUITestBase.
    /// </summary>
    protected FlaUITestBase()
    {
        _screenshotDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Screenshots",
            DateTime.Now.ToString("yyyy-MM-dd")
        );

        Directory.CreateDirectory(_screenshotDirectory);
    }

    /// <summary>
    /// Captures a screenshot of the specified window.
    /// </summary>
    /// <param name="window">The window to capture.</param>
    /// <param name="testName">Name of the test for the screenshot filename.</param>
    /// <returns>Path to the captured screenshot.</returns>
    protected string CaptureScreenshot(Window window, string testName)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        var timestamp = DateTime.Now.ToString("HHmmss");
        var fileName = $"{testName}_{timestamp}.png";
        var filePath = Path.Combine(_screenshotDirectory, fileName);

        using var capture = Capture.Window(window);
        capture.ToFile(filePath);

        return filePath;
    }

    /// <summary>
    /// Captures a screenshot when a test fails.
    /// Call this method in catch blocks or assertion failures.
    /// </summary>
    /// <param name="window">The window to capture.</param>
    /// <param name="testName">Name of the test for the screenshot filename.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    protected void CaptureScreenshotOnFailure(Window? window, string testName, Exception exception)
    {
        if (window == null)
        {
            return;
        }

        try
        {
            var screenshotPath = CaptureScreenshot(window, $"{testName}_FAILED");
            Console.WriteLine($"Screenshot captured on failure: {screenshotPath}");
            Console.WriteLine($"Failure reason: {exception.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the path to the FluentPDF.App executable.
    /// </summary>
    /// <returns>Path to the executable.</returns>
    /// <exception cref="FileNotFoundException">Thrown if executable is not found.</exception>
    protected string GetExecutablePath()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                $"FluentPDF.App.exe not found at {executablePath}. " +
                "Build the application with 'dotnet build src/FluentPDF.App -p:Platform=x64' first."
            );
        }

        return executablePath;
    }

    /// <summary>
    /// Launches the FluentPDF application and returns the main window.
    /// </summary>
    /// <param name="waitTimeoutMs">Timeout in milliseconds to wait for the main window.</param>
    /// <returns>The main window of the application.</returns>
    protected Window LaunchApp(int waitTimeoutMs = 10000)
    {
        var executablePath = GetExecutablePath();
        return LaunchApplication(executablePath, waitTimeoutMs);
    }

    /// <summary>
    /// Closes the application.
    /// </summary>
    protected void CloseApp()
    {
        CloseApplication();
    }
}
