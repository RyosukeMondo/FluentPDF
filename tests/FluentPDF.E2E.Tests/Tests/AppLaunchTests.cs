using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// Smoke tests for application launch functionality.
/// Verifies that the app launches successfully, displays the main window,
/// and doesn't log any errors during startup.
/// </summary>
public class AppLaunchTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public AppLaunchTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the application launches within 10 seconds.
    /// This is verified by the AppLaunchFixture initialization.
    /// </summary>
    [Fact]
    public void AppLaunches_WithinTimeout()
    {
        // Arrange & Act
        // The fixture already launched the app in InitializeAsync
        // If we reached here, the app launched successfully within the timeout

        // Assert
        _fixture.Application.Should().NotBeNull("application should be launched");
        _fixture.Application.HasExited.Should().BeFalse("application should still be running");
    }

    /// <summary>
    /// Test that the main window is visible and responsive.
    /// </summary>
    [Fact]
    public void MainWindow_IsVisibleAndResponsive()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act & Assert
        mainWindow.Should().NotBeNull("main window should be acquired");
        mainWindow.IsAvailable.Should().BeTrue("main window should be available");

        // Verify window properties
        mainWindow.Properties.Name.ValueOrDefault.Should().NotBeNullOrEmpty("window should have a name");

        // Verify window is responsive (doesn't hang)
        var isResponsive = mainWindow.IsOffscreen == false;
        isResponsive.Should().BeTrue("window should be responsive and on screen");
    }

    /// <summary>
    /// Test that no errors are logged during application launch.
    /// Checks for ERROR or FATAL level log entries since the test started.
    /// </summary>
    [Fact]
    public void AppLaunch_LogsNoErrors()
    {
        // Arrange
        // Give the app a moment to finish initialization and log any startup errors
        Thread.Sleep(1000);

        // Act & Assert
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that PDFium initialization succeeds without native load errors.
    /// This is verified by checking that no PDFium-related errors are logged.
    /// </summary>
    [Fact]
    public void PDFiumInitialization_Succeeds()
    {
        // Arrange
        // Give the app time to initialize PDFium
        Thread.Sleep(1000);

        // Act
        var logEntries = _logVerifier.GetLogEntries();
        var pdfiumErrors = logEntries
            .Where(e => (e.Level == "Error" || e.Level == "Fatal") &&
                       (e.MessageTemplate?.Contains("PDFium", StringComparison.OrdinalIgnoreCase) == true ||
                        e.MessageTemplate?.Contains("pdfium", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Message?.Contains("PDFium", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Message?.Contains("pdfium", StringComparison.OrdinalIgnoreCase) == true))
            .ToList();

        // Assert
        pdfiumErrors.Should().BeEmpty("no PDFium initialization errors should be logged");
    }

    /// <summary>
    /// Test that the application window has a valid title.
    /// </summary>
    [Fact]
    public void MainWindow_HasValidTitle()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var windowTitle = mainWindow.Title;

        // Assert
        windowTitle.Should().NotBeNullOrEmpty("window should have a title");
        windowTitle.Should().Contain("FluentPDF", "title should contain application name");
    }

    /// <summary>
    /// Test that the application process has started and is running.
    /// </summary>
    [Fact]
    public void ApplicationProcess_IsRunning()
    {
        // Arrange
        var application = _fixture.Application;

        // Act
        var processId = application.ProcessId;
        var process = System.Diagnostics.Process.GetProcessById(processId);

        // Assert
        process.Should().NotBeNull("process should exist");
        process.HasExited.Should().BeFalse("process should still be running");
        process.ProcessName.Should().Contain("FluentPDF", "process name should contain FluentPDF");
    }
}
