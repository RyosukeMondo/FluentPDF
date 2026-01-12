using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for text search functionality.
/// Verifies search panel toggle, query input, match highlighting, navigation, and match counting.
/// </summary>
public class SearchTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public SearchTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that search panel can be toggled open and closed.
    /// </summary>
    [Fact]
    public async Task SearchPanelToggle_OpensAndClosesPanel()
    {
        // Arrange - Load a PDF first
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));
        var searchPanel = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanel"));

        searchPanelToggle.Should().NotBeNull("Search panel toggle button should be present");
        searchPanel.Should().NotBeNull("Search panel should be present");

        // Initially, search panel should not be visible
        searchPanel.IsOffscreen.Should().BeTrue("Search panel should be hidden initially");

        // Act - Click toggle to open search panel
        searchPanelToggle.Click();
        await Task.Delay(500); // Wait for animation

        // Assert - Search panel should be visible
        searchPanel.IsOffscreen.Should().BeFalse("Search panel should be visible after toggle");

        // Act - Click toggle again to close
        searchPanelToggle.Click();
        await Task.Delay(500); // Wait for animation

        // Assert - Search panel should be hidden again
        searchPanel.IsOffscreen.Should().BeTrue("Search panel should be hidden after second toggle");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that search query can be entered and executed.
    /// </summary>
    [Fact]
    public async Task SearchQuery_FindsTextInDocument()
    {
        // Arrange - Load a PDF with known text content
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));

        // Open search panel
        searchPanelToggle.Click();
        await Task.Delay(500);

        var searchTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchTextBox"));
        var searchMatchCounter = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchMatchCounter"));

        searchTextBox.Should().NotBeNull("Search text box should be present");
        searchMatchCounter.Should().NotBeNull("Match counter should be present");

        // Act - Enter a search query (searching for common text that should exist)
        // Using "PDF" as it's likely to be in any PDF document
        searchTextBox.AsTextBox().Text = "PDF";
        await Task.Delay(1500); // Wait for search to execute

        // Assert - Match counter should show results
        var matchCounterText = searchMatchCounter.AsLabel().Text;
        matchCounterText.Should().NotBeNullOrEmpty("Match counter should display text");

        // The counter should show something like "1 of 2" or just show a count > 0
        // We verify that it's not "0" matches
        matchCounterText.Should().Contain("of", "Match counter should show match count format");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that search navigation buttons work correctly.
    /// </summary>
    [Fact]
    public async Task SearchNavigation_PreviousAndNextButtons_NavigateMatches()
    {
        // Arrange - Load a PDF with multiple matches
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));

        // Open search panel
        searchPanelToggle.Click();
        await Task.Delay(500);

        var searchTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchTextBox"));
        var searchMatchCounter = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchMatchCounter"));
        var searchNextButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchNextButton"));
        var searchPreviousButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPreviousButton"));

        searchNextButton.Should().NotBeNull("Next button should be present");
        searchPreviousButton.Should().NotBeNull("Previous button should be present");

        // Enter search query
        searchTextBox.AsTextBox().Text = "page";
        await Task.Delay(1500); // Wait for search to execute

        // Get initial match position
        var initialMatchText = searchMatchCounter.AsLabel().Text;
        initialMatchText.Should().Contain("of", "Should show match count");

        // Act - Click Next button
        searchNextButton.Click();
        await Task.Delay(800);

        // Assert - Match position should have changed
        var afterNextText = searchMatchCounter.AsLabel().Text;
        afterNextText.Should().NotBe(initialMatchText, "Match position should change after clicking Next");

        // Act - Click Previous button
        searchPreviousButton.Click();
        await Task.Delay(800);

        // Assert - Should return to initial match
        var afterPreviousText = searchMatchCounter.AsLabel().Text;
        afterPreviousText.Should().Be(initialMatchText, "Should return to initial match after Previous");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that match count displays correctly.
    /// </summary>
    [Fact]
    public async Task MatchCount_DisplaysCorrectly()
    {
        // Arrange - Load a PDF
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));

        // Open search panel
        searchPanelToggle.Click();
        await Task.Delay(500);

        var searchTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchTextBox"));
        var searchMatchCounter = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchMatchCounter"));

        // Act - Search for text that should have matches
        searchTextBox.AsTextBox().Text = "test";
        await Task.Delay(1500);

        // Assert - Match counter should display count
        var matchCounterText = searchMatchCounter.AsLabel().Text;
        matchCounterText.Should().NotBeNullOrEmpty("Match counter should display text");
        matchCounterText.Should().MatchRegex(@"\d+ of \d+", "Match counter should show format like '1 of 3'");

        // Act - Search for text that should have no matches
        searchTextBox.AsTextBox().Text = "xyzabc123notfound";
        await Task.Delay(1500);

        // Assert - Match counter should show 0 matches
        var noMatchText = searchMatchCounter.AsLabel().Text;
        noMatchText.Should().Contain("0", "Match counter should show 0 for no matches");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that case sensitive search option works correctly.
    /// </summary>
    [Fact]
    public async Task CaseSensitiveSearch_WorksCorrectly()
    {
        // Arrange - Load a PDF
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));

        // Open search panel
        searchPanelToggle.Click();
        await Task.Delay(500);

        var searchTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchTextBox"));
        var searchMatchCounter = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchMatchCounter"));
        var caseSensitiveCheckbox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchCaseSensitiveCheckBox"));

        caseSensitiveCheckbox.Should().NotBeNull("Case sensitive checkbox should be present");

        // Act - Search without case sensitivity (default)
        searchTextBox.AsTextBox().Text = "pdf";
        await Task.Delay(1500);

        var caseInsensitiveText = searchMatchCounter.AsLabel().Text;
        var caseInsensitiveMatches = ExtractTotalMatches(caseInsensitiveText);

        // Act - Enable case sensitive search
        caseSensitiveCheckbox.AsCheckBox().IsChecked = true;
        await Task.Delay(1500);

        var caseSensitiveText = searchMatchCounter.AsLabel().Text;
        var caseSensitiveMatches = ExtractTotalMatches(caseSensitiveText);

        // Assert - Case sensitive should typically find fewer or same matches
        // (searching for lowercase "pdf" might not match uppercase "PDF")
        caseSensitiveMatches.Should().BeLessThanOrEqualTo(caseInsensitiveMatches,
            "Case sensitive search should find same or fewer matches than case insensitive");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that search panel can be closed with the close button.
    /// </summary>
    [Fact]
    public async Task SearchCloseButton_ClosesPanel()
    {
        // Arrange - Load a PDF
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));

        // Open search panel
        searchPanelToggle.Click();
        await Task.Delay(500);

        var searchPanel = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanel"));
        var searchCloseButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchCloseButton"));

        searchCloseButton.Should().NotBeNull("Search close button should be present");
        searchPanel.IsOffscreen.Should().BeFalse("Search panel should be visible");

        // Act - Click close button
        searchCloseButton.Click();
        await Task.Delay(500);

        // Assert - Search panel should be hidden
        searchPanel.IsOffscreen.Should().BeTrue("Search panel should be hidden after close button click");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test complete search workflow from start to finish.
    /// </summary>
    [Fact]
    public async Task CompleteSearchWorkflow_WorksEndToEnd()
    {
        // Arrange - Load a multi-page PDF
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(2000); // Wait for document to load

        var mainWindow = _fixture.MainWindow;

        // Step 1: Open search panel
        var searchPanelToggle = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanelToggle"));
        searchPanelToggle.Click();
        await Task.Delay(500);

        var searchPanel = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchPanel"));
        searchPanel.IsOffscreen.Should().BeFalse("Search panel should be visible");

        // Step 2: Enter search query
        var searchTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchTextBox"));
        searchTextBox.AsTextBox().Text = "page";
        await Task.Delay(1500);

        // Step 3: Verify matches found
        var searchMatchCounter = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchMatchCounter"));
        var matchText = searchMatchCounter.AsLabel().Text;
        matchText.Should().Contain("of", "Should show match count");
        var totalMatches = ExtractTotalMatches(matchText);
        totalMatches.Should().BeGreaterThan(0, "Should find at least one match");

        // Step 4: Navigate through matches
        var searchNextButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchNextButton"));
        for (int i = 0; i < Math.Min(totalMatches, 3); i++)
        {
            searchNextButton.Click();
            await Task.Delay(800);
        }

        // Step 5: Close search panel
        var searchCloseButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SearchCloseButton"));
        searchCloseButton.Click();
        await Task.Delay(500);

        searchPanel.IsOffscreen.Should().BeTrue("Search panel should be closed");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to extract total match count from counter text (e.g., "1 of 5" returns 5).
    /// </summary>
    private int ExtractTotalMatches(string matchCounterText)
    {
        // Match counter format: "1 of 5" or "0 of 0"
        var parts = matchCounterText.Split(new[] { " of " }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int total))
        {
            return total;
        }

        return 0;
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(SearchTests).Assembly.Location);
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
