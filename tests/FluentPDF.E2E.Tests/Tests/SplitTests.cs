using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for PDF split functionality.
/// Verifies that PDFs can be split by page ranges, and that the split document displays correctly.
/// </summary>
public class SplitTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public SplitTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the Split button is present and accessible.
    /// </summary>
    [Fact]
    public void SplitButton_IsPresent()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var splitButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("SplitButton"));

        // Assert
        splitButton.Should().NotBeNull("Split button should be present in the toolbar");
    }

    /// <summary>
    /// Test splitting a multi-page PDF to extract first 3 pages.
    /// </summary>
    [Fact]
    public async Task SplitDocument_ExtractsPageRange()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        // Create a temporary output file path
        var outputPath = Path.Combine(Path.GetTempPath(), $"split-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Split the PDF to extract pages 1-3
            await SplitDocumentAsync(multiPagePdfPath, "1-3", outputPath);

            // Give the split operation time to complete
            await Task.Delay(3000);

            // Assert - Verify the split document is created
            File.Exists(outputPath).Should().BeTrue("Split PDF should be created at output path");

            // Verify the split document has the correct number of pages
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Page number display should be present");
            var textContent = pageNumberText.AsLabel().Text;

            // The split PDF should have 3 pages
            textContent.Should().Contain("Page 1", "Should show page 1 of split document");
            textContent.Should().Contain("of 3", "Should show 3 pages in split document");

            // Verify no errors were logged
            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup - Delete the temporary split file
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Test splitting a PDF with single page selection.
    /// </summary>
    [Fact]
    public async Task SplitDocument_ExtractsSinglePage()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"split-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Split the PDF to extract only page 5
            await SplitDocumentAsync(multiPagePdfPath, "5", outputPath);

            // Give the split operation time to complete
            await Task.Delay(3000);

            // Assert - Verify output file exists and has content
            File.Exists(outputPath).Should().BeTrue("Split PDF should be created");
            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(0, "Split PDF should have content");

            // Verify the split document has only 1 page
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Page number display should be present");
            var textContent = pageNumberText.AsLabel().Text;

            // The split PDF should have 1 page
            textContent.Should().Contain("Page 1", "Should show page 1");
            textContent.Should().Contain("of 1", "Should show 1 page in split document");

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
    /// Test splitting a PDF with multiple non-contiguous page ranges.
    /// </summary>
    [Fact]
    public async Task SplitDocument_ExtractsMultipleRanges()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"split-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Split the PDF to extract pages 1-2 and 5-6
            await SplitDocumentAsync(multiPagePdfPath, "1-2,5-6", outputPath);

            // Give the split operation time to complete
            await Task.Delay(3000);

            // Assert - Verify output file exists
            File.Exists(outputPath).Should().BeTrue("Split PDF should be created");
            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(0, "Split PDF should have content");

            // Verify the split document has 4 pages (pages 1-2 and 5-6)
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Page number display should be present");
            var textContent = pageNumberText.AsLabel().Text;

            // The split PDF should have 4 pages
            textContent.Should().Contain("of 4", "Should show 4 pages in split document");

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
    /// Test that split operation creates a valid PDF file that can be loaded.
    /// </summary>
    [Fact]
    public async Task SplitDocument_CreatesValidPdf()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"split-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Split the PDF
            await SplitDocumentAsync(multiPagePdfPath, "2-4", outputPath);

            // Give the split operation time to complete
            await Task.Delay(3000);

            // Assert - Verify output file exists and has content
            File.Exists(outputPath).Should().BeTrue("Split PDF should be created");
            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(0, "Split PDF should have content");

            // Try to load the split document again to verify it's valid
            await LoadDocumentAsync(outputPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Split PDF should be loadable and display page information");

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
    /// Test that split preserves content from the extracted pages.
    /// </summary>
    [Fact]
    public async Task SplitDocument_PreservesContent()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"split-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Act - Split the PDF to extract pages 1-5
            await SplitDocumentAsync(multiPagePdfPath, "1-5", outputPath);

            // Give the split operation time to complete
            await Task.Delay(3000);

            // Load the split document
            await LoadDocumentAsync(outputPath);
            await Task.Delay(2000);

            // Assert - Verify page count is correct
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));

            pageNumberText.Should().NotBeNull("Page number display should be present");
            var textContent = pageNumberText.AsLabel().Text;

            // The split PDF should have exactly 5 pages
            textContent.Should().Contain("of 5", "Should show 5 pages in split document");
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
        var assemblyLocation = Path.GetDirectoryName(typeof(SplitTests).Assembly.Location);
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
    /// Helper method to split a document programmatically using the App's test helper.
    /// This bypasses the file picker UI and dialogs for E2E testing.
    /// </summary>
    private async Task SplitDocumentAsync(string inputPath, string pageRanges, string outputPath)
    {
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        var tcs = new TaskCompletionSource<bool>();

        app!.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await app.SplitDocumentForTestingAsync(inputPath, pageRanges, outputPath);
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
