using FlaUI.Core.AutomationElements;
using FluentPDF.E2E.Tests.Fixtures;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// E2E tests for PDF watermark functionality.
/// Verifies that watermarks can be configured and applied to PDFs through the dialog.
/// </summary>
public class WatermarkTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly DateTime _testStartTime;

    public WatermarkTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _testStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Test that the Watermark button is present and accessible.
    /// </summary>
    [Fact]
    public void WatermarkButton_IsPresent()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act
        var watermarkButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkButton"));

        // Assert
        watermarkButton.Should().NotBeNull("Watermark button should be present in the toolbar");
    }

    /// <summary>
    /// Test that clicking the Watermark button opens the watermark dialog.
    /// </summary>
    [Fact]
    public async Task WatermarkButton_OpensDialog()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act
        var watermarkButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkButton"));
        watermarkButton.Should().NotBeNull("Watermark button should be present");
        watermarkButton.AsButton().Click();

        await Task.Delay(1500);

        // Assert - Verify dialog appears
        var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkDialog"));
        watermarkDialog.Should().NotBeNull("Watermark dialog should appear after clicking button");

        // Close the dialog
        var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
        if (cancelButton != null)
        {
            cancelButton.AsButton().Click();
            await Task.Delay(500);
        }

        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test configuring a text watermark with custom text input.
    /// </summary>
    [Fact]
    public async Task TextWatermark_CanBeConfigured()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        // Create a temporary copy to avoid modifying the original
        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"watermark-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Apply a text watermark
            await ApplyTextWatermarkAsync(tempPdfPath, "CONFIDENTIAL", 72, 50);
            await Task.Delay(2000);

            // Assert - Verify the document is still loaded and no errors occurred
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should still be loaded after applying watermark");

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
    /// Test applying a text watermark using preset buttons.
    /// </summary>
    [Fact]
    public async Task TextWatermark_PresetButtons()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"watermark-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;

            // Act - Open watermark dialog
            var watermarkButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkButton"));
            watermarkButton.Should().NotBeNull("Watermark button should be present");
            watermarkButton.AsButton().Click();
            await Task.Delay(1500);

            var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkDialog"));
            watermarkDialog.Should().NotBeNull("Watermark dialog should be open");

            // Click the DRAFT preset button
            var presetButton = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("PresetDraftButton"));
            if (presetButton != null)
            {
                presetButton.AsButton().Click();
                await Task.Delay(500);

                // Verify text box updated
                var textBox = watermarkDialog.FindFirstDescendant(cf =>
                    cf.ByAutomationId("WatermarkTextBox"));
                textBox.Should().NotBeNull("Watermark text box should be present");
            }

            // Close dialog
            var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
            if (cancelButton != null)
            {
                cancelButton.AsButton().Click();
                await Task.Delay(500);
            }

            // Assert
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
    /// Test configuring watermark appearance settings (opacity, rotation).
    /// </summary>
    [Fact]
    public async Task Watermark_AppearanceSettings()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"watermark-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;

            // Act - Open watermark dialog
            var watermarkButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkButton"));
            watermarkButton.Should().NotBeNull("Watermark button should be present");
            watermarkButton.AsButton().Click();
            await Task.Delay(1500);

            var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkDialog"));
            watermarkDialog.Should().NotBeNull("Watermark dialog should be open");

            // Enter watermark text
            var textBox = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkTextBox"));
            textBox.Should().NotBeNull("Watermark text box should be present");

            // Test opacity slider
            var opacitySlider = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("OpacitySlider"));
            opacitySlider.Should().NotBeNull("Opacity slider should be present");

            // Test rotation slider
            var rotationSlider = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("RotationSlider"));
            rotationSlider.Should().NotBeNull("Rotation slider should be present");

            // Test diagonal button
            var diagonalButton = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("DiagonalButton"));
            if (diagonalButton != null)
            {
                diagonalButton.AsButton().Click();
                await Task.Delay(300);
            }

            // Close dialog
            var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
            if (cancelButton != null)
            {
                cancelButton.AsButton().Click();
                await Task.Delay(500);
            }

            // Assert
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
    /// Test configuring watermark position using the position dropdown.
    /// </summary>
    [Fact]
    public async Task Watermark_PositionSettings()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"watermark-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            await LoadDocumentAsync(tempPdfPath);
            await Task.Delay(2000);

            var mainWindow = _fixture.MainWindow;

            // Act - Open watermark dialog
            var watermarkButton = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkButton"));
            watermarkButton.Should().NotBeNull("Watermark button should be present");
            watermarkButton.AsButton().Click();
            await Task.Delay(1500);

            var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkDialog"));
            watermarkDialog.Should().NotBeNull("Watermark dialog should be open");

            // Test position ComboBox
            var positionComboBox = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("PositionComboBox"));
            positionComboBox.Should().NotBeNull("Position ComboBox should be present");

            // Close dialog
            var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
            if (cancelButton != null)
            {
                cancelButton.AsButton().Click();
                await Task.Delay(500);
            }

            // Assert
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
    /// Test that the watermark preview displays in the dialog.
    /// </summary>
    [Fact]
    public async Task Watermark_PreviewDisplays()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Open watermark dialog
        var watermarkButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkButton"));
        watermarkButton.Should().NotBeNull("Watermark button should be present");
        watermarkButton.AsButton().Click();
        await Task.Delay(1500);

        var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkDialog"));
        watermarkDialog.Should().NotBeNull("Watermark dialog should be open");

        // Enter text to generate preview
        var textBox = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkTextBox"));
        textBox.Should().NotBeNull("Watermark text box should be present");

        // Wait for preview to generate
        await Task.Delay(2000);

        // Assert - Verify preview image element exists
        var previewImage = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByAutomationId("PreviewImage"));
        previewImage.Should().NotBeNull("Preview image should be present in dialog");

        // Close dialog
        var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
        if (cancelButton != null)
        {
            cancelButton.AsButton().Click();
            await Task.Delay(500);
        }

        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test configuring page range for watermark application.
    /// </summary>
    [Fact]
    public async Task Watermark_PageRangeConfiguration()
    {
        // Arrange
        var multiPagePdfPath = GetTestDataPath("multi-page.pdf");
        File.Exists(multiPagePdfPath).Should().BeTrue($"Multi-page PDF should exist at: {multiPagePdfPath}");

        await LoadDocumentAsync(multiPagePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Open watermark dialog
        var watermarkButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkButton"));
        watermarkButton.Should().NotBeNull("Watermark button should be present");
        watermarkButton.AsButton().Click();
        await Task.Delay(1500);

        var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkDialog"));
        watermarkDialog.Should().NotBeNull("Watermark dialog should be open");

        // Test page range ComboBox
        var pageRangeComboBox = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByAutomationId("PageRangeComboBox"));
        pageRangeComboBox.Should().NotBeNull("Page range ComboBox should be present");

        // Close dialog
        var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
        if (cancelButton != null)
        {
            cancelButton.AsButton().Click();
            await Task.Delay(500);
        }

        // Assert
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test switching between text and image watermark modes.
    /// </summary>
    [Fact]
    public async Task Watermark_SwitchBetweenTextAndImageModes()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        await LoadDocumentAsync(samplePdfPath);
        await Task.Delay(2000);

        var mainWindow = _fixture.MainWindow;

        // Act - Open watermark dialog
        var watermarkButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkButton"));
        watermarkButton.Should().NotBeNull("Watermark button should be present");
        watermarkButton.AsButton().Click();
        await Task.Delay(1500);

        var watermarkDialog = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("WatermarkDialog"));
        watermarkDialog.Should().NotBeNull("Watermark dialog should be open");

        // Test text mode radio button
        var textRadioButton = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByAutomationId("TextTypeRadioButton"));
        textRadioButton.Should().NotBeNull("Text type radio button should be present");

        // Test image mode radio button
        var imageRadioButton = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByAutomationId("ImageTypeRadioButton"));
        imageRadioButton.Should().NotBeNull("Image type radio button should be present");

        // Switch to image mode
        if (imageRadioButton != null)
        {
            imageRadioButton.AsRadioButton().Click();
            await Task.Delay(500);

            // Verify image controls appear
            var selectImageButton = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("SelectImageButton"));
            selectImageButton.Should().NotBeNull("Select image button should appear in image mode");
        }

        // Switch back to text mode
        if (textRadioButton != null)
        {
            textRadioButton.AsRadioButton().Click();
            await Task.Delay(500);

            // Verify text controls appear
            var textBox = watermarkDialog.FindFirstDescendant(cf =>
                cf.ByAutomationId("WatermarkTextBox"));
            textBox.Should().NotBeNull("Watermark text box should appear in text mode");
        }

        // Close dialog
        var cancelButton = watermarkDialog.FindFirstDescendant(cf =>
            cf.ByName("Cancel").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
        if (cancelButton != null)
        {
            cancelButton.AsButton().Click();
            await Task.Delay(500);
        }

        // Assert
        _logVerifier.AssertNoErrorsSince(_testStartTime);
    }

    /// <summary>
    /// Test applying a watermark and verifying the document remains loaded.
    /// </summary>
    [Fact]
    public async Task Watermark_AppliesSuccessfully()
    {
        // Arrange
        var samplePdfPath = GetTestDataPath("sample.pdf");
        File.Exists(samplePdfPath).Should().BeTrue($"Sample PDF should exist at: {samplePdfPath}");

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"watermark-test-{Guid.NewGuid()}.pdf");
        File.Copy(samplePdfPath, tempPdfPath, true);

        try
        {
            // Act - Apply a text watermark
            await ApplyTextWatermarkAsync(tempPdfPath, "TEST WATERMARK", 48, 80);
            await Task.Delay(3000);

            // Assert - Verify the document is still loaded
            var mainWindow = _fixture.MainWindow;
            var pageNumberText = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("PageNumberTextBox"));
            pageNumberText.Should().NotBeNull("Document should be loaded after applying watermark");

            // Verify no errors were logged
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
        var assemblyLocation = Path.GetDirectoryName(typeof(WatermarkTests).Assembly.Location);
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
    /// Helper method to apply a text watermark programmatically using the App's test helper.
    /// This bypasses the dialog UI for E2E testing.
    /// </summary>
    private async Task ApplyTextWatermarkAsync(string filePath, string text, double fontSize, double opacity)
    {
        var app = Microsoft.UI.Xaml.Application.Current as FluentPDF.App.App;
        app.Should().NotBeNull("App instance should be available");

        var tcs = new TaskCompletionSource<bool>();

        app!.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await app.ApplyTextWatermarkForTestingAsync(filePath, text, fontSize, opacity);
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
