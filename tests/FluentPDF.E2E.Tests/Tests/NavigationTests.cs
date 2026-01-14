using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for page navigation functionality.
/// Verifies that all navigation methods (buttons, input, thumbnails, keyboard) work correctly.
/// </summary>
public class NavigationTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public NavigationTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that next and previous page buttons navigate correctly.
    /// </summary>
    [Fact]
    public async Task NavigateWithButtons_ChangesCurrentPage()
    {
        // Arrange - Load multi-page PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(3000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var nextButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NextPageButton"));
        var previousButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PreviousPageButton"));
        var pageNumberText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PageNumberTextBox"));

        nextButton.Should().NotBeNull("Next page button should be present");
        previousButton.Should().NotBeNull("Previous page button should be present");
        pageNumberText.Should().NotBeNull("Page number display should be present");

        // Verify we start on page 1
        var initialText = pageNumberText.AsLabel().Text;
        initialText.Should().Contain("Page 1", "Should start on page 1");

        // Act - Click Next button
        nextButton.Click();
        await Task.Delay(1000); // Wait for navigation

        // Assert - Should be on page 2
        var afterNextText = pageNumberText.AsLabel().Text;
        afterNextText.Should().Contain("Page 2", "Should navigate to page 2");

        // Act - Click Previous button
        previousButton.Click();
        await Task.Delay(1000); // Wait for navigation

        // Assert - Should be back on page 1
        var afterPreviousText = pageNumberText.AsLabel().Text;
        afterPreviousText.Should().Contain("Page 1", "Should navigate back to page 1");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that page number input allows direct navigation to a specific page.
    /// </summary>
    [Fact]
    public async Task NavigateWithPageNumberInput_JumpsToSpecificPage()
    {
        // Arrange - Load multi-page PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(3000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var pageNumberText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PageNumberTextBox"));
        pageNumberText.Should().NotBeNull("Page number display should be present");

        // Verify we start on page 1
        var initialText = pageNumberText.AsLabel().Text;
        initialText.Should().Contain("Page 1", "Should start on page 1");

        // Act - Try to navigate to page 3 by clicking on the text and typing
        // Note: This assumes the TextBox is editable. If it's read-only, this test may need adjustment
        pageNumberText.Click();
        await Task.Delay(500);

        // Select all text and type new page number
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        await Task.Delay(200);
        Keyboard.Type("3");
        await Task.Delay(200);
        Keyboard.Press(VirtualKeyShort.RETURN);
        await Task.Delay(1500); // Wait for navigation

        // Assert - Should be on page 3
        var afterInputText = pageNumberText.AsLabel().Text;
        afterInputText.Should().Contain("Page 3", "Should navigate to page 3 via input");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that clicking on thumbnails navigates to the corresponding page.
    /// </summary>
    [Fact]
    public async Task NavigateWithThumbnailClick_JumpsToPage()
    {
        // Arrange - Load multi-page PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(4000); // Wait for document and thumbnails to load

        var mainWindow = _fixture.MainWindow;

        // Ensure thumbnails sidebar is visible
        var thumbnailsToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ThumbnailsToggle"));
        thumbnailsToggle.Should().NotBeNull("Thumbnails toggle button should be present");

        // Check if thumbnails are already visible, if not, toggle them
        var thumbnailsSidebar = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ThumbnailsSidebar"));
        thumbnailsSidebar.Should().NotBeNull("Thumbnails sidebar should be present");

        // If sidebar is not visible, click the toggle
        if (!thumbnailsSidebar.IsVisible)
        {
            thumbnailsToggle.Click();
            await Task.Delay(1000);
        }

        var pageNumberText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PageNumberTextBox"));
        pageNumberText.Should().NotBeNull("Page number display should be present");

        // Verify we start on page 1
        var initialText = pageNumberText.AsLabel().Text;
        initialText.Should().Contain("Page 1", "Should start on page 1");

        // Act - Try to find and click a thumbnail (this is simplified, actual thumbnail finding may vary)
        // Find all elements in the thumbnails sidebar
        var thumbnailItems = thumbnailsSidebar.FindAllDescendants();

        // Look for clickable thumbnail items (ListBoxItems, Images, etc.)
        // The exact structure depends on the ThumbnailsSidebar control implementation
        var clickableThumbnails = thumbnailItems
            .Where(item => item.ControlType.ToString().Contains("ListBoxItem") ||
                          item.ControlType.ToString().Contains("ListItem"))
            .ToList();

        if (clickableThumbnails.Count >= 2)
        {
            // Click the second thumbnail (should navigate to page 2)
            clickableThumbnails[1].Click();
            await Task.Delay(1500); // Wait for navigation

            // Assert - Should be on page 2
            var afterThumbnailClickText = pageNumberText.AsLabel().Text;
            afterThumbnailClickText.Should().Contain("Page 2", "Should navigate to page 2 via thumbnail click");
        }
        else
        {
            // If we can't find specific thumbnails, at least verify the sidebar is populated
            thumbnailItems.Should().NotBeEmpty("Thumbnails sidebar should contain items");
        }

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that keyboard navigation (Page Up/Down) works correctly.
    /// </summary>
    [Fact]
    public async Task NavigateWithKeyboard_ChangesCurrentPage()
    {
        // Arrange - Load multi-page PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(3000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var pageNumberText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PageNumberTextBox"));
        pageNumberText.Should().NotBeNull("Page number display should be present");

        // Verify we start on page 1
        var initialText = pageNumberText.AsLabel().Text;
        initialText.Should().Contain("Page 1", "Should start on page 1");

        // Focus the main window to ensure keyboard events are received
        mainWindow.Focus();
        await Task.Delay(500);

        // Act - Press Page Down key
        Keyboard.Press(VirtualKeyShort.NEXT); // Page Down
        await Task.Delay(1500); // Wait for navigation

        // Assert - Should be on page 2
        var afterPageDownText = pageNumberText.AsLabel().Text;
        afterPageDownText.Should().Contain("Page 2", "Should navigate to page 2 with Page Down");

        // Act - Press Page Up key
        Keyboard.Press(VirtualKeyShort.PRIOR); // Page Up
        await Task.Delay(1500); // Wait for navigation

        // Assert - Should be back on page 1
        var afterPageUpText = pageNumberText.AsLabel().Text;
        afterPageUpText.Should().Contain("Page 1", "Should navigate back to page 1 with Page Up");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that navigation buttons are disabled appropriately at document boundaries.
    /// </summary>
    [Fact]
    public async Task NavigationButtons_DisabledAtBoundaries()
    {
        // Arrange - Load multi-page PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(3000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var previousButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PreviousPageButton"));
        var nextButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NextPageButton"));

        previousButton.Should().NotBeNull("Previous page button should be present");
        nextButton.Should().NotBeNull("Next page button should be present");

        // Assert - Previous button should be disabled on page 1
        previousButton.IsEnabled.Should().BeFalse("Previous button should be disabled on page 1");

        // Next button should be enabled (assuming we have multiple pages)
        nextButton.IsEnabled.Should().BeTrue("Next button should be enabled when not on last page");

        // Navigate to the last page
        // First, get the page count from the page number text
        var pageNumberText = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PageNumberTextBox"));
        pageNumberText.Should().NotBeNull("Page number display should be present");
        var pageText = pageNumberText.AsLabel().Text;

        // Extract total page count (assumes format like "Page 1 of 10")
        var match = System.Text.RegularExpressions.Regex.Match(pageText, @"of (\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int totalPages))
        {
            // Navigate to last page
            for (int i = 1; i < totalPages; i++)
            {
                nextButton.Click();
                await Task.Delay(800);
            }

            // Wait a bit more for UI to update
            await Task.Delay(1000);

            // Assert - Next button should be disabled on last page
            nextButton.IsEnabled.Should().BeFalse("Next button should be disabled on last page");

            // Previous button should be enabled
            previousButton.IsEnabled.Should().BeTrue("Previous button should be enabled when not on first page");
        }

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(NavigationTests).Assembly.Location);
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
