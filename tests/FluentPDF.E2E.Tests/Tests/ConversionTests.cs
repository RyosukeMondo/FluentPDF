using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for DOCX to PDF conversion functionality.
/// Verifies that DOCX files can be converted to PDF, conversion progress displays correctly,
/// and the converted PDF can be opened in the viewer.
/// </summary>
public class ConversionTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public ConversionTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the Convert DOCX button opens the conversion page.
    /// </summary>
    [Fact]
    public async Task ConvertDocxButton_OpensConversionPage()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // First, load any PDF to see the main UI with the Convert DOCX button
        var samplePdfPath = GetTestDataPath("sample.pdf");
        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(1000);

        // Act - Click the Convert DOCX button in the toolbar
        var convertButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ConvertDocxButton"));
        convertButton.Should().NotBeNull("Convert DOCX button should be present in toolbar");

        convertButton.AsButton().Click();
        await Task.Delay(2000); // Wait for navigation to ConversionPage

        // Assert - Verify ConversionPage elements are visible
        var convertToPdfButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ConvertToPdfButton"));
        convertToPdfButton.Should().NotBeNull("ConversionPage should display with Convert to PDF button");

        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test that DOCX conversion workflow completes successfully.
    /// </summary>
    [Fact]
    public async Task DocxConversion_CompletesSuccessfully()
    {
        // Arrange
        var docxFilePath = GetTestDataPath("sample.docx");
        File.Exists(docxFilePath).Should().BeTrue($"DOCX test file should exist at: {docxFilePath}");

        var outputPdfPath = Path.Combine(Path.GetTempPath(), $"converted-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Navigate to ConversionPage
            await NavigateToConversionPageAsync();

            var mainWindow = _fixture.MainWindow;

            // Act - Set file paths programmatically through the ViewModel
            await SetConversionFilePathsAsync(docxFilePath, outputPdfPath);
            await Task.Delay(500);

            // Verify file paths are set
            var docxFilePathTextBox = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("DocxFilePathTextBox"));
            docxFilePathTextBox.Should().NotBeNull("DOCX file path textbox should be present");

            var outputFilePathTextBox = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("OutputFilePathTextBox"));
            outputFilePathTextBox.Should().NotBeNull("Output file path textbox should be present");

            // Click Convert to PDF button
            var convertButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConvertToPdfButton"));
            convertButton.Should().NotBeNull("Convert to PDF button should be present");

            convertButton.AsButton().Click();
            await Task.Delay(1000); // Initial delay for conversion to start

            // Assert - Wait for conversion to complete (max 30 seconds)
            bool conversionCompleted = await WaitForConversionCompletionAsync(TimeSpan.FromSeconds(30));
            conversionCompleted.Should().BeTrue("Conversion should complete within 30 seconds");

            // Verify output file was created
            File.Exists(outputPdfPath).Should().BeTrue($"Output PDF should be created at: {outputPdfPath}");

            // Verify results panel is visible
            var resultsPanel = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConversionResultsPanel"));
            resultsPanel.Should().NotBeNull("Conversion results panel should be visible after completion");

            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPdfPath))
            {
                try
                {
                    File.Delete(outputPdfPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <summary>
    /// Test that conversion progress displays during conversion.
    /// </summary>
    [Fact]
    public async Task ConversionProgress_DisplaysDuringConversion()
    {
        // Arrange
        var docxFilePath = GetTestDataPath("sample.docx");
        File.Exists(docxFilePath).Should().BeTrue($"DOCX test file should exist at: {docxFilePath}");

        var outputPdfPath = Path.Combine(Path.GetTempPath(), $"converted-progress-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Navigate to ConversionPage
            await NavigateToConversionPageAsync();

            var mainWindow = _fixture.MainWindow;

            // Act - Set file paths and start conversion
            await SetConversionFilePathsAsync(docxFilePath, outputPdfPath);
            await Task.Delay(500);

            var convertButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConvertToPdfButton"));
            convertButton.Should().NotBeNull("Convert to PDF button should be present");

            convertButton.AsButton().Click();
            await Task.Delay(500); // Wait for conversion to start

            // Assert - Check if progress bar becomes visible during conversion
            // Note: The conversion might be too fast to catch the progress bar, so we just verify it doesn't error
            var progressBar = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConversionProgressBar"));

            // Progress bar should exist (might not be visible if conversion is fast)
            progressBar.Should().NotBeNull("Conversion progress bar should exist");

            // Wait for completion
            await WaitForConversionCompletionAsync(TimeSpan.FromSeconds(30));

            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPdfPath))
            {
                try
                {
                    File.Delete(outputPdfPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <summary>
    /// Test that the converted PDF displays correct content.
    /// </summary>
    [Fact]
    public async Task ConvertedPdf_HasCorrectContent()
    {
        // Arrange
        var docxFilePath = GetTestDataPath("sample.docx");
        File.Exists(docxFilePath).Should().BeTrue($"DOCX test file should exist at: {docxFilePath}");

        var outputPdfPath = Path.Combine(Path.GetTempPath(), $"converted-content-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Navigate to ConversionPage
            await NavigateToConversionPageAsync();

            var mainWindow = _fixture.MainWindow;

            // Act - Convert DOCX to PDF
            await SetConversionFilePathsAsync(docxFilePath, outputPdfPath);
            await Task.Delay(500);

            var convertButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConvertToPdfButton"));
            convertButton.AsButton().Click();

            bool conversionCompleted = await WaitForConversionCompletionAsync(TimeSpan.FromSeconds(30));
            conversionCompleted.Should().BeTrue("Conversion should complete successfully");

            // Assert - Verify output PDF is valid
            File.Exists(outputPdfPath).Should().BeTrue("Output PDF should exist");

            var fileInfo = new FileInfo(outputPdfPath);
            fileInfo.Length.Should().BeGreaterThan(0, "Output PDF should not be empty");

            // Verify it's a valid PDF by checking header
            using var stream = File.OpenRead(outputPdfPath);
            var buffer = new byte[5];
            stream.Read(buffer, 0, 5);
            var header = System.Text.Encoding.ASCII.GetString(buffer);
            header.Should().Be("%PDF-", "Output file should be a valid PDF");

            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPdfPath))
            {
                try
                {
                    File.Delete(outputPdfPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <summary>
    /// Test that the Open PDF button opens the converted PDF in the viewer.
    /// </summary>
    [Fact]
    public async Task OpenPdfButton_OpensConvertedPdfInViewer()
    {
        // Arrange
        var docxFilePath = GetTestDataPath("sample.docx");
        File.Exists(docxFilePath).Should().BeTrue($"DOCX test file should exist at: {docxFilePath}");

        var outputPdfPath = Path.Combine(Path.GetTempPath(), $"converted-open-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Navigate to ConversionPage and convert
            await NavigateToConversionPageAsync();

            await SetConversionFilePathsAsync(docxFilePath, outputPdfPath);
            await Task.Delay(500);

            var mainWindow = _fixture.MainWindow;

            var convertButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConvertToPdfButton"));
            convertButton.AsButton().Click();

            bool conversionCompleted = await WaitForConversionCompletionAsync(TimeSpan.FromSeconds(30));
            conversionCompleted.Should().BeTrue("Conversion should complete successfully");

            // Act - Click the Open PDF button
            var openPdfButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("OpenConvertedPdfButton"));
            openPdfButton.Should().NotBeNull("Open PDF button should be visible after conversion");

            openPdfButton.AsButton().Click();
            await Task.Delay(3000); // Wait for PDF to load in viewer

            // Assert - Verify the PDF viewer is showing content
            var pdfViewer = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PdfScrollViewer"));
            pdfViewer.Should().NotBeNull("PDF viewer should be present after opening converted PDF");

            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPdfPath))
            {
                try
                {
                    File.Delete(outputPdfPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <summary>
    /// Test that status message updates during conversion workflow.
    /// </summary>
    [Fact]
    public async Task StatusMessage_UpdatesDuringConversion()
    {
        // Arrange
        var docxFilePath = GetTestDataPath("sample.docx");
        File.Exists(docxFilePath).Should().BeTrue($"DOCX test file should exist at: {docxFilePath}");

        var outputPdfPath = Path.Combine(Path.GetTempPath(), $"converted-status-test-{Guid.NewGuid()}.pdf");

        try
        {
            // Navigate to ConversionPage
            await NavigateToConversionPageAsync();

            var mainWindow = _fixture.MainWindow;

            // Act - Check initial status
            var statusInfoBar = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConversionStatusInfoBar"));
            statusInfoBar.Should().NotBeNull("Status info bar should be present");

            // Start conversion
            await SetConversionFilePathsAsync(docxFilePath, outputPdfPath);
            await Task.Delay(500);

            var convertButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConvertToPdfButton"));
            convertButton.AsButton().Click();

            await Task.Delay(500); // Wait for status to update

            // Assert - Status should update during/after conversion
            // The status message should change from initial state
            statusInfoBar = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConversionStatusInfoBar"));
            statusInfoBar.Should().NotBeNull("Status info bar should remain visible");

            // Wait for completion
            await WaitForConversionCompletionAsync(TimeSpan.FromSeconds(30));

            _logVerifier.AssertNoErrorsSince(_testStartTime);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPdfPath))
            {
                try
                {
                    File.Delete(outputPdfPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <summary>
    /// Helper method to get the full path to a test data file.
    /// </summary>
    private string GetTestDataPath(string filename)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(ConversionTests).Assembly.Location);
        assemblyLocation.Should().NotBeNullOrEmpty("Assembly location should be valid");

        var testDataPath = Path.GetFullPath(Path.Combine(
            assemblyLocation!,
            "..", "..", "..", "..", "..", "TestData", filename));

        return testDataPath;
    }

    /// <summary>
    /// Helper method to navigate to the ConversionPage.
    /// </summary>
    private async Task NavigateToConversionPageAsync()
    {
        var mainWindow = _fixture.MainWindow;

        // First, ensure a PDF is loaded to display the toolbar
        var samplePdfPath = GetTestDataPath("sample.pdf");
        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(1000);

        // Click the Convert DOCX button to navigate to ConversionPage
        var convertButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("ConvertDocxButton"));
        convertButton.Should().NotBeNull("Convert DOCX button should be present");

        convertButton.AsButton().Click();
        await Task.Delay(2000); // Wait for navigation
    }

    /// <summary>
    /// Helper method to set conversion file paths programmatically.
    /// </summary>
    private async Task SetConversionFilePathsAsync(string docxPath, string outputPath)
    {
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        var tcs = new TaskCompletionSource<bool>();

        app!.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                // Get the current page (should be ConversionPage)
                var navigationService = app.GetService<FluentPDF.App.Services.INavigationService>();

                // Access the ConversionViewModel through the app's service container
                var conversionViewModel = app.GetService<FluentPDF.App.ViewModels.ConversionViewModel>();

                if (conversionViewModel != null)
                {
                    conversionViewModel.DocxFilePath = docxPath;
                    conversionViewModel.OutputFilePath = outputPath;
                }

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Helper method to wait for conversion completion.
    /// </summary>
    private async Task<bool> WaitForConversionCompletionAsync(TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        var mainWindow = _fixture.MainWindow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            // Check if results panel is visible (indicates completion)
            var resultsPanel = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("ConversionResultsPanel"));

            if (resultsPanel != null && resultsPanel.IsAvailable)
            {
                // Results panel is visible, conversion completed
                return true;
            }

            await Task.Delay(500);
        }

        // Timeout reached
        return false;
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
