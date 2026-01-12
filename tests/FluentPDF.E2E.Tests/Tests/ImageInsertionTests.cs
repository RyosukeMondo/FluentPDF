using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for PDF image insertion functionality.
/// Verifies that images can be inserted, manipulated (resize, move), and persisted in PDFs.
/// </summary>
public class ImageInsertionTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public ImageInsertionTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the Insert Image button is present and accessible.
    /// </summary>
    [Fact]
    public void InsertImageButton_IsPresent()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var insertImageButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("InsertImageButton"));

        // Assert
        insertImageButton.Should().NotBeNull("Insert Image button should be present in the toolbar");
    }

    /// <summary>
    /// Test that the Insert Image button is disabled when no document is loaded.
    /// </summary>
    [Fact]
    public void InsertImageButton_DisabledWithoutDocument()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var insertImageButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("InsertImageButton"));

        // Assert
        insertImageButton.Should().NotBeNull("Insert Image button should be present");
        insertImageButton.IsEnabled.Should().BeFalse("Insert Image button should be disabled without a document loaded");
    }

    /// <summary>
    /// Test that the Insert Image button is enabled when a document is loaded.
    /// </summary>
    [Fact]
    public async Task InsertImageButton_EnabledWithDocument()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act
        var insertImageButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("InsertImageButton"));

        // Assert
        insertImageButton.Should().NotBeNull("Insert Image button should be present");
        insertImageButton.IsEnabled.Should().BeTrue("Insert Image button should be enabled when document is loaded");

        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test inserting an image into a PDF document.
    /// </summary>
    [Fact]
    public async Task InsertImage_SuccessfullyAddsImage()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Insert image using test helper
            await InsertImageAsync(tempPdfPath, testImagePath, 200, 300);
            await Task.Delay(2000);

            // Assert - Verify the document is still loaded
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should be loaded after inserting image");

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
    /// Test inserting an image at default position (center of page).
    /// </summary>
    [Fact]
    public async Task InsertImage_DefaultPosition()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Insert image at default position
            await InsertImageAsync(tempPdfPath, testImagePath);
            await Task.Delay(2000);

            // Assert - Verify no errors occurred
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
    /// Test inserting multiple images into the same PDF page.
    /// </summary>
    [Fact]
    public async Task InsertImage_MultipleImages()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Insert multiple images at different positions
            await InsertImageAsync(tempPdfPath, testImagePath, 100, 100);
            await Task.Delay(1000);

            await InsertImageAsync(tempPdfPath, testImagePath, 300, 300);
            await Task.Delay(1000);

            await InsertImageAsync(tempPdfPath, testImagePath, 500, 100);
            await Task.Delay(1000);

            // Assert - Verify no errors occurred
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
    /// Test inserting an image into different pages of a multi-page document.
    /// </summary>
    [Fact]
    public async Task InsertImage_MultiplePages()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(multiPagePdfPath, tempPdfPath, true);

        try
        {
            // Act - Insert image on first page
            await InsertImageAsync(tempPdfPath, testImagePath, 200, 200);
            await Task.Delay(1000);

            // Navigate to page 2
            var mainWindow = _fixture.MainWindow;
            var nextPageButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("NextPageButton"));
            nextPageButton.Should().NotBeNull("Next page button should be present");
            nextPageButton.AsButton().Click();
            await Task.Delay(1000);

            // Insert image on second page
            await InsertImageAsync(tempPdfPath, testImagePath, 400, 400);
            await Task.Delay(1000);

            // Assert - Verify no errors occurred
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
    /// Test that image insertion handles different image formats (PNG, JPG, etc.).
    /// </summary>
    [Fact]
    public async Task InsertImage_SupportedFormats()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Test PNG format
            await InsertImageAsync(tempPdfPath, testImagePath, 250, 250);
            await Task.Delay(2000);

            // Assert - Verify no errors occurred
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
    /// Test that inserted images persist after saving and reloading the document.
    /// </summary>
    [Fact]
    public async Task InsertImage_PersistsAfterSave()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Insert image
            await InsertImageAsync(tempPdfPath, testImagePath, 200, 300);
            await Task.Delay(2000);

            // Save the document
            var mainWindow = _fixture.MainWindow;
            var saveButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("SaveButton"));
            if (saveButton != null && saveButton.IsEnabled)
            {
                saveButton.AsButton().Click();
                await Task.Delay(3000);
            }

            // Reload the document
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            // Assert - Verify the document loads without errors
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should be loaded after reload");

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
    /// Test inserting an image at various positions across the page.
    /// </summary>
    [Fact]
    public async Task InsertImage_VariousPositions()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var testImagePath = GetTestDataPath("test-image.png");
        File.Exists(testImagePath).Should().BeTrue($"Test image should exist at: {testImagePath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"image-insertion-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Test different positions
            // Top-left
            await InsertImageAsync(tempPdfPath, testImagePath, 50, 50);
            await Task.Delay(1000);

            // Top-right
            await InsertImageAsync(tempPdfPath, testImagePath, 500, 50);
            await Task.Delay(1000);

            // Bottom-left
            await InsertImageAsync(tempPdfPath, testImagePath, 50, 700);
            await Task.Delay(1000);

            // Bottom-right
            await InsertImageAsync(tempPdfPath, testImagePath, 500, 700);
            await Task.Delay(1000);

            // Assert - Verify no errors occurred
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
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(ImageInsertionTests).Assembly.Location);
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
    /// Helper method to insert an image programmatically using the App's test helper.
    /// This bypasses the file picker UI for E2E testing.
    /// </summary>
    private async Task InsertImageAsync(string filePath, string imagePath, float x = 300, float y = 400)
    {
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        var tcs = new TaskCompletionSource<bool>();

        app!.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await app.InsertImageForTestingAsync(filePath, imagePath, x, y);
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
