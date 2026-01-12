using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for zoom functionality.
/// Verifies that zoom in, zoom out, and reset zoom controls work correctly.
/// </summary>
public class ZoomTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public ZoomTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that Zoom In button increases zoom level.
    /// </summary>
    [Fact]
    public async Task ZoomInButton_IncreasesZoomLevel()
    {
        // Arrange - Load a PDF first
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var zoomInButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomInButton"));
        var zoomLevelText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomLevelText"));

        zoomInButton.Should().NotBeNull("Zoom In button should be present");
        zoomLevelText.Should().NotBeNull("Zoom level display should be present");

        // Get initial zoom level (e.g., "100%")
        var initialZoomText = zoomLevelText.AsLabel().Text;
        initialZoomText.Should().NotBeNullOrEmpty("Zoom level should display initial value");

        // Extract percentage value (remove % sign)
        var initialZoom = ExtractZoomPercentage(initialZoomText);
        initialZoom.Should().BeGreaterThan(0, "Initial zoom should be a positive value");

        // Act - Click Zoom In button
        zoomInButton.Click();
        await Task.Delay(1000); // Wait for zoom to apply

        // Assert - Zoom level should have increased
        var newZoomText = zoomLevelText.AsLabel().Text;
        var newZoom = ExtractZoomPercentage(newZoomText);

        newZoom.Should().BeGreaterThan(initialZoom, "Zoom level should increase after clicking Zoom In");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that Zoom Out button decreases zoom level.
    /// </summary>
    [Fact]
    public async Task ZoomOutButton_DecreasesZoomLevel()
    {
        // Arrange - Load a PDF first
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var zoomOutButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomOutButton"));
        var zoomLevelText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomLevelText"));

        zoomOutButton.Should().NotBeNull("Zoom Out button should be present");
        zoomLevelText.Should().NotBeNull("Zoom level display should be present");

        // Get initial zoom level
        var initialZoomText = zoomLevelText.AsLabel().Text;
        var initialZoom = ExtractZoomPercentage(initialZoomText);
        initialZoom.Should().BeGreaterThan(0, "Initial zoom should be a positive value");

        // Act - Click Zoom Out button
        zoomOutButton.Click();
        await Task.Delay(1000); // Wait for zoom to apply

        // Assert - Zoom level should have decreased
        var newZoomText = zoomLevelText.AsLabel().Text;
        var newZoom = ExtractZoomPercentage(newZoomText);

        newZoom.Should().BeLessThan(initialZoom, "Zoom level should decrease after clicking Zoom Out");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that Reset Zoom button returns zoom to 100%.
    /// </summary>
    [Fact]
    public async Task ResetZoomButton_ReturnsToOneHundredPercent()
    {
        // Arrange - Load a PDF first
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var zoomInButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomInButton"));
        var resetZoomButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ResetZoomButton"));
        var zoomLevelText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomLevelText"));

        zoomInButton.Should().NotBeNull("Zoom In button should be present");
        resetZoomButton.Should().NotBeNull("Reset Zoom button should be present");
        zoomLevelText.Should().NotBeNull("Zoom level display should be present");

        // First, change the zoom level by zooming in multiple times
        for (int i = 0; i < 3; i++)
        {
            zoomInButton.Click();
            await Task.Delay(500);
        }

        // Wait for zoom changes to complete
        await Task.Delay(1000);

        // Verify zoom has changed from default
        var zoomedText = zoomLevelText.AsLabel().Text;
        var zoomedLevel = ExtractZoomPercentage(zoomedText);
        zoomedLevel.Should().BeGreaterThan(100, "Zoom should be greater than 100% after zooming in");

        // Act - Click Reset Zoom button
        resetZoomButton.Click();
        await Task.Delay(1000); // Wait for zoom to reset

        // Assert - Zoom level should be back to 100%
        var resetZoomText = zoomLevelText.AsLabel().Text;
        var resetZoom = ExtractZoomPercentage(resetZoomText);

        resetZoom.Should().Be(100, "Zoom level should be 100% after reset");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that zoom controls are enabled when a document is loaded.
    /// </summary>
    [Fact]
    public async Task ZoomControls_EnabledWhenDocumentLoaded()
    {
        // Arrange - Load a PDF
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var zoomInButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomInButton"));
        var zoomOutButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomOutButton"));
        var resetZoomButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ResetZoomButton"));

        // Assert - All zoom controls should be present and enabled
        zoomInButton.Should().NotBeNull("Zoom In button should be present");
        zoomOutButton.Should().NotBeNull("Zoom Out button should be present");
        resetZoomButton.Should().NotBeNull("Reset Zoom button should be present");

        zoomInButton.IsEnabled.Should().BeTrue("Zoom In button should be enabled with document loaded");
        zoomOutButton.IsEnabled.Should().BeTrue("Zoom Out button should be enabled with document loaded");
        resetZoomButton.IsEnabled.Should().BeTrue("Reset Zoom button should be enabled with document loaded");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that multiple zoom operations work correctly in sequence.
    /// </summary>
    [Fact]
    public async Task MultipleZoomOperations_WorkCorrectly()
    {
        // Arrange - Load a PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var zoomInButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomInButton"));
        var zoomOutButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomOutButton"));
        var resetZoomButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ResetZoomButton"));
        var zoomLevelText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomLevelText"));

        // Get initial zoom level (should be 100%)
        var initialZoomText = zoomLevelText.AsLabel().Text;
        var initialZoom = ExtractZoomPercentage(initialZoomText);

        // Act & Assert - Zoom in twice
        zoomInButton.Click();
        await Task.Delay(700);
        var afterFirstZoomIn = ExtractZoomPercentage(zoomLevelText.AsLabel().Text);
        afterFirstZoomIn.Should().BeGreaterThan(initialZoom, "First zoom in should increase zoom");

        zoomInButton.Click();
        await Task.Delay(700);
        var afterSecondZoomIn = ExtractZoomPercentage(zoomLevelText.AsLabel().Text);
        afterSecondZoomIn.Should().BeGreaterThan(afterFirstZoomIn, "Second zoom in should increase zoom further");

        // Zoom out once
        zoomOutButton.Click();
        await Task.Delay(700);
        var afterZoomOut = ExtractZoomPercentage(zoomLevelText.AsLabel().Text);
        afterZoomOut.Should().BeLessThan(afterSecondZoomIn, "Zoom out should decrease zoom");
        afterZoomOut.Should().BeGreaterThan(initialZoom, "Should still be zoomed in compared to start");

        // Reset zoom
        resetZoomButton.Click();
        await Task.Delay(1000);
        var afterReset = ExtractZoomPercentage(zoomLevelText.AsLabel().Text);
        afterReset.Should().Be(initialZoom, "Reset should return to initial zoom level");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to extract zoom percentage from text like "100%" or "125%".
    /// </summary>
    private double ExtractZoomPercentage(string zoomText)
    {
        // Remove % sign and any whitespace, then parse as double
        var cleanText = zoomText.Replace("%", "").Trim();

        if (double.TryParse(cleanText, out double percentage))
        {
            return percentage;
        }

        throw new FormatException($"Could not parse zoom percentage from: {zoomText}");
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(ZoomTests).Assembly.Location);
        assemblyLocation.Should().NotBeNullOrEmpty("Assembly location should be valid");

        var testDataPath = Path.GetFullPath(Path.Combine(
            assemblyLocation!,
            "..", "..", "..", "..", "..", "TestData", filename));

        return testDataPath;
    }

    /// <summary>
    /// Helper method to load a document programmatically using the App's test helper.
    /// This bypasses the file picker UI for E2E testing.
    /// </summary>
    private async Task LoadDocumentAsync(string filePath)
    {
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        var tcs = new TaskCompletionSource<bool>();

        app!.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await app.LoadDocumentForTestingAsync(filePath);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
