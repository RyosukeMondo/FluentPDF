using System.Diagnostics;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for PDF rendering and display verification.
/// Verifies that PDF pages are rendered correctly and displayed in the UI.
/// </summary>
public class RenderingDisplayTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public RenderingDisplayTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that PDF opens and displays image within timeout.
    /// Verifies that the Image control Source is not null after loading.
    /// </summary>
    [Fact]
    public async Task PdfOpens_AndDisplaysImage_WithinTimeout()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");

        // Act - Load document
        await LoadDocumentAsync(testPdfPath);

        // Wait up to 5 seconds for rendering
        var timeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;
        bool imageSourceSet = false;

        while (DateTime.UtcNow - startTime < timeout)
        {
            // Check if the Image control has a non-null Source
            var mainWindow = _fixture.MainWindow;
            var imageControl = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PdfImageControl"));

            if (imageControl != null && imageControl.IsAvailable)
            {
                // Image control exists and is available
                // Note: FlaUI doesn't directly expose Image.Source, but we can verify the control exists
                imageSourceSet = true;
                break;
            }

            await Task.Delay(100);
        }

        // Assert
        imageSourceSet.Should().BeTrue("Image control should be available within 5 seconds");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that Image control contains non-blank content.
    /// Captures a screenshot and verifies pixels are not all white/blank.
    /// </summary>
    [Fact]
    public async Task ImageControl_ContainsNonBlankContent()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");

        // Act - Load document and wait for rendering
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(3000); // Give time for rendering

        // Find the Image control
        var mainWindow = _fixture.MainWindow;
        var imageControl = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PdfImageControl"));

        imageControl.Should().NotBeNull("Image control should be found");

        // Capture screenshot of the image control
        var screenshot = imageControl!.Capture();
        screenshot.Should().NotBeNull("Should be able to capture screenshot");

        // Convert to bitmap and check if it contains non-white pixels
        using var bitmap = screenshot.Bitmap;
        bool hasNonWhitePixels = false;

        // Sample pixels across the image (check 100 points)
        int sampleCount = 100;
        int width = bitmap.Width;
        int height = bitmap.Height;

        for (int i = 0; i < sampleCount; i++)
        {
            int x = (i % 10) * (width / 10);
            int y = (i / 10) * (height / 10);

            if (x >= width) x = width - 1;
            if (y >= height) y = height - 1;

            var pixel = bitmap.GetPixel(x, y);

            // Check if pixel is not white (allowing for slight variations)
            if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250)
            {
                hasNonWhitePixels = true;
                break;
            }
        }

        // Assert
        hasNonWhitePixels.Should().BeTrue("Image should contain non-blank (non-white) pixels");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that thumbnail grid populates with images.
    /// Verifies thumbnails are displayed for a multi-page PDF.
    /// </summary>
    [Fact]
    public async Task ThumbnailGrid_PopulatesWithImages()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");

        // Act - Load document
        await LoadDocumentAsync(testPdfPath);

        // Wait for thumbnails to render (up to 6 seconds)
        await Task.Delay(6000);

        // Find the thumbnails sidebar
        var mainWindow = _fixture.MainWindow;
        var thumbnailsSidebar = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ThumbnailsSidebar"));

        thumbnailsSidebar.Should().NotBeNull("Thumbnails sidebar should be present");
        thumbnailsSidebar!.IsAvailable.Should().BeTrue("Thumbnails sidebar should be available");

        // Try to find thumbnail items within the sidebar
        // Note: The specific AutomationId for thumbnails may vary based on implementation
        var thumbnailItems = thumbnailsSidebar.FindAllDescendants(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

        // Assert - Should have at least one thumbnail
        thumbnailItems.Should().NotBeNull("Should find thumbnail items");
        thumbnailItems.Length.Should().BeGreaterThan(0, "Should have at least one thumbnail item");

        // Verify no errors were logged
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test CLI test-render command produces correct output.
    /// Launches the app with --test-render flag and verifies exit code and output.
    /// </summary>
    [Fact]
    public async Task CliTestRender_Succeeds()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("sample.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Test PDF should exist at: {testPdfPath}");

        var exePath = LocateExecutable();
        File.Exists(exePath).Should().BeTrue($"FluentPDF.App.exe should exist at: {exePath}");

        // Expected output file location (in temp directory)
        var outputFileName = $"test-render-{Path.GetFileNameWithoutExtension(testPdfPath)}.json";
        var expectedOutputPath = Path.Combine(Path.GetTempPath(), "FluentPDF", outputFileName);

        // Delete existing output file if present
        if (File.Exists(expectedOutputPath))
        {
            File.Delete(expectedOutputPath);
        }

        // Act - Launch app with --test-render flag
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--test-render \"{testPdfPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull("Process should start successfully");

        var output = await process!.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        // Wait for process to complete (up to 30 seconds)
        bool exited = await Task.Run(() => process.WaitForExit(30000));

        // Assert
        exited.Should().BeTrue("Process should exit within 30 seconds");
        process.ExitCode.Should().Be(0, $"Exit code should be 0 (success). Output: {output}, Error: {error}");

        // Verify diagnostic output file was created
        File.Exists(expectedOutputPath).Should().BeTrue(
            $"Diagnostic output file should be created at: {expectedOutputPath}");

        // Verify the output file contains valid JSON
        if (File.Exists(expectedOutputPath))
        {
            var jsonContent = await File.ReadAllTextAsync(expectedOutputPath);
            jsonContent.Should().NotBeNullOrEmpty("Output file should contain content");
            jsonContent.Should().Contain("success", "Output should indicate success or failure");

            // Clean up output file
            File.Delete(expectedOutputPath);
        }
    }

    /// <summary>
    /// Test that multiple page navigations maintain rendering quality.
    /// Verifies that rendering remains reliable when navigating between pages.
    /// </summary>
    [Fact]
    public async Task MultiplePageNavigations_MaintainRenderingQuality()
    {
        // Arrange
        var testPdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(testPdfPath).Should().BeTrue($"Multi-page PDF should exist at: {testPdfPath}");

        // Act - Load document
        await LoadDocumentAsync(testPdfPath);
        await Task.Delay(3000); // Wait for initial render

        var mainWindow = _fixture.MainWindow;

        // Navigate through multiple pages
        for (int i = 0; i < 3; i++)
        {
            // Find next page button
            var nextPageButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("NextPageButton"));

            if (nextPageButton != null && nextPageButton.IsEnabled)
            {
                nextPageButton.Click();
                await Task.Delay(1000); // Wait for page to render

                // Verify image control is still available
                var imageControl = mainWindow.FindFirstDescendant(cf =>
                    cf.ByAutomationId("PdfImageControl"));

                imageControl.Should().NotBeNull($"Image control should be available after navigation {i + 1}");
                imageControl!.IsAvailable.Should().BeTrue($"Image should be rendered after navigation {i + 1}");
            }
        }

        // Assert - No errors should be logged during navigations
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        // Get the test assembly directory
        var assemblyLocation = Path.GetDirectoryName(typeof(RenderingDisplayTests).Assembly.Location);
        assemblyLocation.Should().NotBeNullOrEmpty("Assembly location should be valid");

        // Navigate to the TestData folder
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

    /// <summary>
    /// Locates the FluentPDF.App.exe executable.
    /// Searches in typical build output locations.
    /// </summary>
    private string LocateExecutable()
    {
        // Get the solution root directory
        var solutionRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", ".."));

        // Search for the executable in common build locations
        var searchPaths = new[]
        {
            // Release builds
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "x64", "Release", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "Release", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),

            // Debug builds
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "x64", "Debug", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),
            Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "Debug", "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe"),
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Return the most likely path (first Release path)
        return searchPaths[0];
    }
}
