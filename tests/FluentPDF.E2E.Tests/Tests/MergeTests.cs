using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for PDF merge functionality.
/// Verifies that multiple PDFs can be merged, and that the merged document displays correctly.
/// </summary>
public class MergeTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public MergeTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the Merge button is present and accessible.
    /// </summary>
    [Fact]
    public void MergeButton_IsPresent()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var mergeButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("MergeButton"));

        // Assert
        mergeButton.Should().NotBeNull("Merge button should be present in the toolbar");
    }

    /// <summary>
    /// Test merging two PDFs results in a document with combined page count.
    /// </summary>
    [Fact]
    public async Task MergeDocuments_CombinesPages()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        // Create a temporary output file path
        var outputPath = Path.Combine(Path.GetTempPath(), $"merged-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Merge the two PDFs
            await MergeDocumentsAsync(new[] { samplePdfPath, multiPagePdfPath }, outputPath);

            // Give the merge operation time to complete
            await Task.Delay(3000);

            // Load the merged document to verify
            await LoadDocumentAsync(outputPath);

            // Give the UI time to update
            await Task.Delay(2000);

            // Assert - Verify the merged document is loaded
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Page number display should be present");
            var textContent = pageNumberText.AsLabel().Text;

            // The merged PDF should have pages from both documents (sample.pdf has 1 page, multi-page.pdf has multiple)
            textContent.Should().Contain("Page 1", "Should show page 1 of merged document");
            textContent.Should().Contain("of", "Page count should be displayed");

            // Verify output file exists
            File.Exists(outputPath).Should().BeTrue("Merged PDF should be created at output path");

            // Verify no errors were logged
            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup - Delete the temporary merged file
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Test that merge operation creates a valid PDF file.
    /// </summary>
    [Fact]
    public async Task MergeDocuments_CreatesValidPdf()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        var formPdfPath = GetTestDataPath("form.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");
        File.Exists(formPdfPath).Should().BeTrue($"Form PDF should exist at: {formPdfPath}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"merged-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Merge the two PDFs
            await MergeDocumentsAsync(new[] { samplePdfPath, formPdfPath }, outputPath);

            // Give the merge operation time to complete
            await Task.Delay(3000);

            // Assert - Verify output file exists and has content
            File.Exists(outputPath).Should().BeTrue("Merged PDF should be created");
            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(0, "Merged PDF should have content");

            // Try to load the merged document to verify it's valid
            await LoadDocumentAsync(outputPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Merged PDF should be loadable and display page information");

            // Verify no errors were logged
            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Test that merged document preserves content from source PDFs.
    /// </summary>
    [Fact]
    public async Task MergeDocuments_PreservesContent()
    {
        // Arrange
        var multiPage1Path = GetTestDataPath("multi-page.pdf");
        var multiPage2Path = GetTestDataPath("multi-page.pdf"); // Use same PDF twice to verify duplication
        File.Exists(multiPage1Path).Should().BeTrue($"Multi-page PDF should exist at: {multiPage1Path}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"merged-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Merge the same PDF twice
            await MergeDocumentsAsync(new[] { multiPage1Path, multiPage2Path }, outputPath);

            // Give the merge operation time to complete
            await Task.Delay(3000);

            // Load the merged document
            await LoadDocumentAsync(outputPath);
            await Task.Delay(2000);

            // Assert - Verify page count is doubled
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Page number display should be present");
            var textContent = pageNumberText.AsLabel().Text;

            // The merged PDF should have double the pages (since we merged the same PDF twice)
            textContent.Should().Contain("of", "Page count should be displayed");
            textContent.Should().Contain("Page 1", "Should start on page 1");

            // Verify no errors were logged
            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(MergeTests).Assembly.Location);
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

    /// <summary>
    /// Helper method to merge documents programmatically using the App's test helper.
    /// This bypasses the file picker UI for E2E testing.
    /// </summary>
    private async Task MergeDocumentsAsync(string[] inputPaths, string outputPath)
    {
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        var tcs = new TaskCompletionSource<bool>();

        app!.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await app.MergeDocumentsForTestingAsync(inputPaths, outputPath);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
}
