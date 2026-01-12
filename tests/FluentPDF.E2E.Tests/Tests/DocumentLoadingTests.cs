using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for document loading functionality.
/// Verifies that PDFs can be loaded, displayed correctly, and that UI elements are populated.
/// </summary>
public class DocumentLoadingTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public DocumentLoadingTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the Open button is present and accessible.
    /// </summary>
    [Fact]
    public void OpenButton_IsPresent()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var openButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OpenDocumentButton"));

        // Assert
        openButton.Should().NotBeNull("Open button should be present in the toolbar");
        openButton.IsEnabled.Should().BeTrue("Open button should be enabled");
    }

    /// <summary>
    /// Test loading a simple single-page PDF displays the first page.
    /// </summary>
    [Fact]
    public async Task LoadDocument_DisplaysFirstPage()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("sample.pdf");
        testPdfPath.Should().NotBeNullOrEmpty("Test PDF path should be resolved");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");

        // Act
        await LoadDocumentAsync(testPdfPath);

        // Give the UI time to update (loading, rendering, thumbnails)
        await Task.Delay(3000);

        // Assert - Check that page number shows "Page 1 of 1" (or similar)
        var mainWindow = _fixture.MainWindow;
        var pageNumberText = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PageNumberTextBox"));

        pageNumberText.Should().NotBeNull("Page number display should be present");
        var textContent = pageNumberText.AsLabel().Text;
        textContent.Should().Contain("Page", "Page number should show 'Page'");
        textContent.Should().Contain("1", "Should show page 1");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test loading a multi-page PDF displays correct page count.
    /// </summary>
    [Fact]
    public async Task LoadDocument_DisplaysCorrectPageCount()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");

        // Act
        await LoadDocumentAsync(testPdfPath);

        // Give the UI time to update
        await Task.Delay(3000);

        // Assert - Check that page count displays correctly
        var mainWindow = _fixture.MainWindow;
        var pageNumberText = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PageNumberTextBox"));

        pageNumberText.Should().NotBeNull("Page number display should be present");
        var textContent = pageNumberText.AsLabel().Text;

        // The multi-page.pdf should have multiple pages (we expect "of X" where X > 1)
        textContent.Should().Contain("of", "Page count should be displayed");
        textContent.Should().Contain("Page 1", "Should start on page 1");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that thumbnails sidebar is present after loading a document.
    /// </summary>
    [Fact]
    public async Task LoadDocument_PopulatesThumbnailsSidebar()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");

        // Act
        await LoadDocumentAsync(testPdfPath);

        // Give the UI and thumbnails time to load
        await Task.Delay(4000);

        // Assert - Check that thumbnails sidebar is present
        var mainWindow = _fixture.MainWindow;
        var thumbnailsSidebar = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ThumbnailsSidebar"));

        thumbnailsSidebar.Should().NotBeNull("Thumbnails sidebar should be present");

        // Note: We can't easily verify thumbnail count without deeper inspection,
        // but we can verify the sidebar is accessible and rendered
        thumbnailsSidebar.IsAvailable.Should().BeTrue("Thumbnails sidebar should be available");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        // Get the test assembly directory
        var assemblyLocation = Path.GetDirectoryName(typeof(DocumentLoadingTests).Assembly.Location);
        assemblyLocation.Should().NotBeNullOrEmpty("Assembly location should be valid");

        // Navigate to the TestData folder
        // Typical path: tests/FluentPDF.E2E.Tests/bin/Debug/net8.0-windows10.0.19041.0/win-x64/
        // TestData is at: tests/FluentPDF.E2E.Tests/TestData/
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
        // Get the app instance
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        // Use the test helper to load the document on the UI thread
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

        // Wait for the load to complete
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
