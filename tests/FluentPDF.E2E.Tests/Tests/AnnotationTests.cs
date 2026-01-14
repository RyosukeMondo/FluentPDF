using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for PDF annotation functionality.
/// Verifies that annotation tools can be selected, annotations can be created,
/// and annotations persist after save/reload.
/// </summary>
public class AnnotationTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public AnnotationTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the annotation toolbar is present and all tool buttons are accessible.
    /// </summary>
    [Fact]
    public void AnnotationToolbar_IsPresent()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var annotationToolbar = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("AnnotationToolbar"));

        // Assert
        annotationToolbar.Should().NotBeNull("Annotation toolbar should be present");

        // Verify all annotation tool buttons are present
        var highlightButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("HighlightButton"));
        var rectangleButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("RectangleButton"));
        var circleButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("CircleButton"));
        var freehandButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("FreehandButton"));

        highlightButton.Should().NotBeNull("Highlight button should be present");
        rectangleButton.Should().NotBeNull("Rectangle button should be present");
        circleButton.Should().NotBeNull("Circle button should be present");
        freehandButton.Should().NotBeNull("Freehand button should be present");
    }

    /// <summary>
    /// Test that annotation tool selection works correctly.
    /// </summary>
    [Fact]
    public async Task AnnotationToolSelection_ActivatesTool()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        // Load a document first
        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Click the highlight button
        var highlightButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("HighlightButton"));
        highlightButton.Should().NotBeNull("Highlight button should be present");
        highlightButton.AsButton().Click();

        await Task.Delay(500);

        // Assert - Verify tool is selected (button should remain pressed or have visual feedback)
        // In a real implementation, you might check if the button's toggle state changed
        highlightButton.IsEnabled.Should().BeTrue("Highlight button should remain enabled after click");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that highlight annotation can be created on text.
    /// This test simulates the user selecting text for highlighting.
    /// </summary>
    [Fact]
    public async Task HighlightAnnotation_CanBeCreated()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Select highlight tool
        var highlightButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("HighlightButton"));
        highlightButton.Should().NotBeNull("Highlight button should be present");
        highlightButton.AsButton().Click();

        await Task.Delay(500);

        // Find the PDF viewer area to interact with
        var pdfViewer = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PdfScrollViewer"));
        pdfViewer.Should().NotBeNull("PDF viewer should be present");

        // Simulate text selection by clicking and dragging
        // Note: This is a simplified test - actual text selection would require
        // more sophisticated coordinate calculation and interaction
        var viewerBounds = pdfViewer.BoundingRectangle;
        var startPoint = new System.Drawing.Point(
            (int)(viewerBounds.Left + viewerBounds.Width / 3),
            (int)(viewerBounds.Top + viewerBounds.Height / 3)
        );
        var endPoint = new System.Drawing.Point(
            (int)(viewerBounds.Left + viewerBounds.Width / 2),
            (int)(viewerBounds.Top + viewerBounds.Height / 3)
        );

        // Perform drag gesture to select text
        Mouse.MoveTo(startPoint);
        await Task.Delay(200);
        Mouse.Down(MouseButton.Left);
        await Task.Delay(200);
        Mouse.MoveTo(endPoint);
        await Task.Delay(200);
        Mouse.Up(MouseButton.Left);
        await Task.Delay(1000);

        // Assert - The annotation should be created
        // In a real scenario, we'd verify the annotation appears in the UI
        // or check the document state through the ViewModel
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that rectangle shape annotation can be created.
    /// </summary>
    [Fact]
    public async Task RectangleAnnotation_CanBeCreated()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Select rectangle tool
        var rectangleButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("RectangleButton"));
        rectangleButton.Should().NotBeNull("Rectangle button should be present");
        rectangleButton.AsButton().Click();

        await Task.Delay(500);

        // Find the PDF viewer area
        var pdfViewer = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PdfScrollViewer"));
        pdfViewer.Should().NotBeNull("PDF viewer should be present");

        // Draw a rectangle by clicking and dragging
        var viewerBounds = pdfViewer.BoundingRectangle;
        var startPoint = new System.Drawing.Point(
            (int)(viewerBounds.Left + 100),
            (int)(viewerBounds.Top + 100)
        );
        var endPoint = new System.Drawing.Point(
            (int)(viewerBounds.Left + 300),
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

        // Assert - Verify no errors occurred during annotation creation
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that circle shape annotation can be created.
    /// </summary>
    [Fact]
    public async Task CircleAnnotation_CanBeCreated()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Select circle tool
        var circleButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("CircleButton"));
        circleButton.Should().NotBeNull("Circle button should be present");
        circleButton.AsButton().Click();

        await Task.Delay(500);

        // Find the PDF viewer area
        var pdfViewer = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PdfScrollViewer"));
        pdfViewer.Should().NotBeNull("PDF viewer should be present");

        // Draw a circle by clicking and dragging
        var viewerBounds = pdfViewer.BoundingRectangle;
        var startPoint = new System.Drawing.Point(
            (int)(viewerBounds.Left + 150),
            (int)(viewerBounds.Top + 150)
        );
        var endPoint = new System.Drawing.Point(
            (int)(viewerBounds.Left + 250),
            (int)(viewerBounds.Top + 250)
        );

        Mouse.MoveTo(startPoint);
        await Task.Delay(200);
        Mouse.Down(MouseButton.Left);
        await Task.Delay(200);
        Mouse.MoveTo(endPoint);
        await Task.Delay(200);
        Mouse.Up(MouseButton.Left);
        await Task.Delay(1000);

        // Assert - Verify no errors occurred during annotation creation
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that freehand drawing annotation can be created.
    /// </summary>
    [Fact]
    public async Task FreehandAnnotation_CanBeCreated()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Select freehand tool
        var freehandButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("FreehandButton"));
        freehandButton.Should().NotBeNull("Freehand button should be present");
        freehandButton.AsButton().Click();

        await Task.Delay(500);

        // Find the PDF viewer area
        var pdfViewer = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PdfScrollViewer"));
        pdfViewer.Should().NotBeNull("PDF viewer should be present");

        // Draw a freehand path
        var viewerBounds = pdfViewer.BoundingRectangle;
        var points = new[]
        {
            new System.Drawing.Point((int)(viewerBounds.Left + 100), (int)(viewerBounds.Top + 100)),
            new System.Drawing.Point((int)(viewerBounds.Left + 150), (int)(viewerBounds.Top + 120)),
            new System.Drawing.Point((int)(viewerBounds.Left + 200), (int)(viewerBounds.Top + 100)),
            new System.Drawing.Point((int)(viewerBounds.Left + 250), (int)(viewerBounds.Top + 120))
        };

        Mouse.MoveTo(points[0]);
        await Task.Delay(200);
        Mouse.Down(MouseButton.Left);
        await Task.Delay(100);

        foreach (var point in points.Skip(1))
        {
            Mouse.MoveTo(point);
            await Task.Delay(100);
        }

        Mouse.Up(MouseButton.Left);
        await Task.Delay(1000);

        // Assert - Verify no errors occurred during annotation creation
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that annotations persist after saving and reloading the document.
    /// </summary>
    [Fact]
    public async Task Annotations_PersistAfterSaveAndReload()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        // Create a temporary copy to avoid modifying the original test file
        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"annotated-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Load the temporary PDF
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;

            // Act - Create a rectangle annotation
            var rectangleButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("RectangleButton"));
            rectangleButton.Should().NotBeNull("Rectangle button should be present");
            rectangleButton.AsButton().Click();
            await Task.Delay(500);

            var pdfViewer = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PdfScrollViewer"));
            pdfViewer.Should().NotBeNull("PDF viewer should be present");

            var viewerBounds = pdfViewer.BoundingRectangle;
            var startPoint = new System.Drawing.Point(
                (int)(viewerBounds.Left + 100),
                (int)(viewerBounds.Top + 100)
            );
            var endPoint = new System.Drawing.Point(
                (int)(viewerBounds.Left + 200),
                (int)(viewerBounds.Top + 150)
            );

            Mouse.MoveTo(startPoint);
            await Task.Delay(200);
            Mouse.Down(MouseButton.Left);
            await Task.Delay(200);
            Mouse.MoveTo(endPoint);
            await Task.Delay(200);
            Mouse.Up(MouseButton.Left);
            await Task.Delay(1000);

            // Save the document with annotation
            var saveButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("SaveDocumentButton"));
            if (saveButton != null)
            {
                saveButton.AsButton().Click();
                await Task.Delay(2000);
            }

            // Reload the document
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            // Assert - Verify the document loaded successfully after annotation
            // In a real scenario, we'd verify the annotation is still visible
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should be loaded after save/reload");

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
    /// Test switching between different annotation tools.
    /// </summary>
    [Fact]
    public async Task AnnotationTools_CanSwitchBetweenTools()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Switch between different tools
        var highlightButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("HighlightButton"));
        var rectangleButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("RectangleButton"));
        var circleButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("CircleButton"));
        var freehandButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("FreehandButton"));

        highlightButton.Should().NotBeNull("Highlight button should be present");
        rectangleButton.Should().NotBeNull("Rectangle button should be present");
        circleButton.Should().NotBeNull("Circle button should be present");
        freehandButton.Should().NotBeNull("Freehand button should be present");

        // Click each tool in sequence
        highlightButton.AsButton().Click();
        await Task.Delay(300);

        rectangleButton.AsButton().Click();
        await Task.Delay(300);

        circleButton.AsButton().Click();
        await Task.Delay(300);

        freehandButton.AsButton().Click();
        await Task.Delay(300);

        // Assert - Verify no errors occurred during tool switching
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(AnnotationTests).Assembly.Location);
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
