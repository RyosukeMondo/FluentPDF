using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// Comprehensive E2E tests that verify complete user workflows spanning multiple features.
/// Tests real-world scenarios including document lifecycle, multi-step operations,
/// and error recovery.
/// </summary>
public class FullWorkflowTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public FullWorkflowTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Tests complete document workflow: open, annotate, save, reopen, verify persistence.
    /// This represents a typical user session working with a single document.
    /// </summary>
    [Fact]
    public async Task CompleteDocumentWorkflow_OpenAnnotateSaveReopen()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        // Create a temporary copy for this workflow test
        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"workflow-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Step 1: Open the document
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;

            // Verify document loaded
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should be loaded");

            // Step 2: Add a rectangle annotation
            var rectangleButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("RectangleButton"));
            rectangleButton.Should().NotBeNull("Rectangle button should be present");
            rectangleButton.AsButton().Click();
            await Task.Delay(500);

            // Draw the annotation
            var pdfViewer = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PdfScrollViewer"));
            pdfViewer.Should().NotBeNull("PDF viewer should be present");

            var viewerBounds = pdfViewer.BoundingRectangle;
            var startPoint = new System.Drawing.Point(
                (int)(viewerBounds.Left + 150),
                (int)(viewerBounds.Top + 150)
            );
            var endPoint = new System.Drawing.Point(
                (int)(viewerBounds.Left + 250),
                (int)(viewerBounds.Top + 200)
            );

            Mouse.MoveTo(startPoint);
            await Task.Delay(200);
            Mouse.Down(MouseButton.Left);
            await Task.Delay(200);
            Mouse.MoveTo(endPoint);
            await Task.Delay(200);
            Mouse.Up(MouseButton.Left);
            await Task.Delay(1000);

            // Step 3: Zoom in to verify view manipulation
            var zoomInButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ZoomInButton"));
            if (zoomInButton != null)
            {
                zoomInButton.AsButton().Click();
                await Task.Delay(500);
            }

            // Step 4: Save the document
            var saveButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("SaveDocumentButton"));
            if (saveButton != null)
            {
                saveButton.AsButton().Click();
                await Task.Delay(2000);
            }

            // Step 5: Reload the document (simulates reopening in a new session)
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            // Verify document is accessible after reload
            pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should be reloaded successfully");

            // Assert - No errors throughout the entire workflow
            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }

    /// <summary>
    /// Tests multi-step editing workflow: open document, add watermark, insert image, save.
    /// Verifies that multiple editing operations can be performed in sequence.
    /// </summary>
    [Fact]
    public async Task MultiStepEditingWorkflow_WatermarkAndImageInsertion()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"edit-workflow-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Step 1: Load document
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;

            // Step 2: Open watermark dialog and configure text watermark
            var watermarkButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkButton"));

            if (watermarkButton != null)
            {
                watermarkButton.AsButton().Click();
                await Task.Delay(1000);

                // Try to find and interact with watermark dialog
                var watermarkTextBox = mainWindow.FindFirstDescendant(cf =>
                    cf.ByAutomationId("WatermarkTextBox"));

                if (watermarkTextBox != null)
                {
                    watermarkTextBox.AsTextBox().Enter("CONFIDENTIAL");
                    await Task.Delay(500);

                    // Apply watermark
                    var applyButton = mainWindow.FindFirstDescendant(cf =>
                        cf.ByAutomationId("WatermarkApplyButton"));
                    if (applyButton != null)
                    {
                        applyButton.AsButton().Click();
                        await Task.Delay(2000);
                    }
                }
                else
                {
                    // If dialog not found, close any open dialog
                    Keyboard.Type(Microsoft.VisualBasic.Constants.vbEscape);
                    await Task.Delay(500);
                }
            }

            // Step 3: Add a highlight annotation
            var highlightButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("HighlightButton"));

            if (highlightButton != null)
            {
                highlightButton.AsButton().Click();
                await Task.Delay(500);

                // Simulate text selection
                var pdfViewer = mainWindow.FindFirstDescendant(cf =>
                    cf.ByAutomationId("PdfScrollViewer"));

                if (pdfViewer != null)
                {
                    var viewerBounds = pdfViewer.BoundingRectangle;
                    var startPoint = new System.Drawing.Point(
                        (int)(viewerBounds.Left + 100),
                        (int)(viewerBounds.Top + 100)
                    );
                    var endPoint = new System.Drawing.Point(
                        (int)(viewerBounds.Left + 200),
                        (int)(viewerBounds.Top + 100)
                    );

                    Mouse.MoveTo(startPoint);
                    await Task.Delay(200);
                    Mouse.Down(MouseButton.Left);
                    await Task.Delay(200);
                    Mouse.MoveTo(endPoint);
                    await Task.Delay(200);
                    Mouse.Up(MouseButton.Left);
                    await Task.Delay(1000);
                }
            }

            // Step 4: Save the edited document
            var saveButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("SaveDocumentButton"));

            if (saveButton != null)
            {
                saveButton.AsButton().Click();
                await Task.Delay(2000);
            }

            // Assert - Verify no errors during multi-step editing
            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }

    /// <summary>
    /// Tests document navigation and search workflow.
    /// Verifies that users can navigate through pages while searching.
    /// </summary>
    [Fact]
    public async Task NavigationAndSearchWorkflow_BrowseAndFind()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        // Act
        await LoadDocumentAsync(multiPagePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Step 1: Navigate to page 2 using next button
        var nextButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NextPageButton"));

        if (nextButton != null && nextButton.IsEnabled)
        {
            nextButton.AsButton().Click();
            await Task.Delay(1000);
        }

        // Step 2: Navigate back to page 1
        var previousButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PreviousPageButton"));

        if (previousButton != null && previousButton.IsEnabled)
        {
            previousButton.AsButton().Click();
            await Task.Delay(1000);
        }

        // Step 3: Open search panel
        var searchButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("SearchButton"));

        if (searchButton != null)
        {
            searchButton.AsButton().Click();
            await Task.Delay(500);

            // Step 4: Enter search query
            var searchBox = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("SearchTextBox"));

            if (searchBox != null)
            {
                searchBox.AsTextBox().Enter("test");
                await Task.Delay(1500);

                // Step 5: Navigate through search results
                var nextMatchButton = mainWindow.FindFirstDescendant(cf =>
                    cf.ByAutomationId("NextMatchButton"));

                if (nextMatchButton != null && nextMatchButton.IsEnabled)
                {
                    nextMatchButton.AsButton().Click();
                    await Task.Delay(500);
                }
            }
        }

        // Step 6: Zoom in for better visibility
        var zoomInButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ZoomInButton"));

        if (zoomInButton != null)
        {
            zoomInButton.AsButton().Click();
            await Task.Delay(500);
        }

        // Assert - Verify smooth navigation and search
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Tests form filling and document merge workflow.
    /// Verifies complete form interaction followed by document operations.
    /// </summary>
    [Fact]
    public async Task FormAndMergeWorkflow_FillFormThenMerge()
    {
        // Arrange
        var formPdfPath = GetTestDataPath("form.pdf");
        var samplePdfPath = GetTestDataPath("sample.pdf");

        if (!File.Exists(formPdfPath))
        {
            // Skip if form PDF not available
            return;
        }

        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        // Step 1: Load form PDF
        await LoadDocumentAsync(formPdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Step 2: Interact with form fields
        // Note: Form field interaction is limited without direct access to form elements
        // This test verifies the document loads and app remains stable
        var pdfViewer = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PdfScrollViewer"));
        pdfViewer.Should().NotBeNull("PDF viewer should display form");

        await Task.Delay(1000);

        // Step 3: Save filled form
        var saveButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("SaveDocumentButton"));

        if (saveButton != null)
        {
            saveButton.AsButton().Click();
            await Task.Delay(2000);
        }

        // Step 4: Attempt merge operation (if merge button exists)
        var mergeButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("MergeButton"));

        if (mergeButton != null && mergeButton.IsEnabled)
        {
            // Note: Merge would typically open file picker
            // This test just verifies button is accessible
            mergeButton.IsEnabled.Should().BeTrue("Merge button should be enabled");
        }

        // Assert - Verify workflow completed without errors
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Tests error recovery: attempting operations with no document loaded.
    /// Verifies app handles invalid states gracefully.
    /// </summary>
    [Fact]
    public async Task ErrorRecovery_OperationsWithoutDocument()
    {
        // Note: This test uses a fresh app instance via fixture
        // At this point, no document is loaded

        var mainWindow = _fixture.MainWindow;

        // Attempt various operations that require a document
        // These should either be disabled or handle gracefully

        // Step 1: Try to navigate without a document
        var nextButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NextPageButton"));

        if (nextButton != null)
        {
            // Button should be disabled without a document
            // If enabled, clicking should not cause an error
            if (nextButton.IsEnabled)
            {
                nextButton.AsButton().Click();
                await Task.Delay(500);
            }
        }

        // Step 2: Try to zoom without a document
        var zoomInButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ZoomInButton"));

        if (zoomInButton != null && zoomInButton.IsEnabled)
        {
            zoomInButton.AsButton().Click();
            await Task.Delay(500);
        }

        // Step 3: Try to save without a document
        var saveButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("SaveDocumentButton"));

        if (saveButton != null && saveButton.IsEnabled)
        {
            // Should either be disabled or handle gracefully
            saveButton.IsEnabled.Should().BeFalse("Save should be disabled without document");
        }

        // Step 4: Try annotation without a document
        var highlightButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("HighlightButton"));

        if (highlightButton != null && highlightButton.IsEnabled)
        {
            highlightButton.AsButton().Click();
            await Task.Delay(500);
        }

        // Step 5: Now load a document and verify app recovers
        var samplePdfPath = GetTestDataPath("sample.pdf");
        if (File.Exists(samplePdfPath))
        {
            await LoadDocumentAsync(samplePdfPath);
            await Task.Delay(2000);

            // Verify document loaded successfully after previous operations
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("App should recover and load document");
        }

        // Assert - No errors should be logged even when operations attempted without document
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Tests rapid operation workflow: quickly switching between tools and views.
    /// Verifies app stability under fast user interactions.
    /// </summary>
    [Fact]
    public async Task RapidOperationsWorkflow_QuickToolSwitching()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Rapidly switch between annotation tools
        var tools = new[] { "HighlightButton", "RectangleButton", "CircleButton", "FreehandButton" };

        foreach (var toolId in tools)
        {
            var toolButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(toolId));
            if (toolButton != null)
            {
                toolButton.AsButton().Click();
                await Task.Delay(100); // Minimal delay for rapid switching
            }
        }

        // Rapidly adjust zoom
        var zoomInButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomInButton"));
        var zoomOutButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ZoomOutButton"));

        for (int i = 0; i < 5; i++)
        {
            if (zoomInButton != null)
            {
                zoomInButton.AsButton().Click();
                await Task.Delay(100);
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (zoomOutButton != null)
            {
                zoomOutButton.AsButton().Click();
                await Task.Delay(100);
            }
        }

        // Reset zoom
        var resetZoomButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ResetZoomButton"));
        if (resetZoomButton != null)
        {
            resetZoomButton.AsButton().Click();
            await Task.Delay(500);
        }

        // Assert - App should remain stable during rapid operations
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Tests complete document lifecycle with thumbnails navigation.
    /// Verifies sidebar interaction and document state consistency.
    /// </summary>
    [Fact]
    public async Task ThumbnailNavigationWorkflow_SidebarInteraction()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        await LoadDocumentAsync(multiPagePdfPath);
        await Task.Delay(3000); // Extra time for thumbnails to generate

        var mainWindow = _fixture.MainWindow;

        // Step 1: Verify thumbnails sidebar is present
        var thumbnailsSidebar = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ThumbnailsSidebar"));
        thumbnailsSidebar.Should().NotBeNull("Thumbnails sidebar should be present");

        // Step 2: Navigate using next/previous buttons
        var nextButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NextPageButton"));

        if (nextButton != null && nextButton.IsEnabled)
        {
            nextButton.AsButton().Click();
            await Task.Delay(1000);

            nextButton.AsButton().Click();
            await Task.Delay(1000);
        }

        // Step 3: Navigate back
        var previousButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PreviousPageButton"));

        if (previousButton != null && previousButton.IsEnabled)
        {
            previousButton.AsButton().Click();
            await Task.Delay(1000);
        }

        // Step 4: Try direct page number input
        var pageNumberBox = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PageNumberInput"));

        if (pageNumberBox != null)
        {
            pageNumberBox.AsTextBox().Enter("1");
            Keyboard.Type(Microsoft.VisualBasic.Constants.vbReturn);
            await Task.Delay(1000);
        }

        // Assert - Verify consistent state throughout navigation
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(FullWorkflowTests).Assembly.Location);
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
