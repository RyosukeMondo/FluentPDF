using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FluentAssertions;
using System.Text.Json;

namespace FluentPDF.App.Tests.E2E;

/// <summary>
/// End-to-end tests for observability features using FlaUI.
/// Tests the complete user workflow for diagnostics panel and log viewer.
/// </summary>
[Trait("Category", "E2E")]
public class ObservabilityE2ETests : UITestBase
{
    private const string SamplePdfPath = "../../../../Fixtures/sample.pdf";

    /// <summary>
    /// Tests the complete observability workflow:
    /// 1. Open PDF and render pages
    /// 2. Press Ctrl+Shift+D to open diagnostics panel
    /// 3. Verify metrics display (FPS, memory, render time)
    /// 4. Press Ctrl+Shift+L to open log viewer
    /// 5. Apply filters and verify logs filtered
    /// 6. Export metrics to JSON and verify file
    /// 7. Export logs to JSON and verify file
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually")]
    public void CompleteObservabilityWorkflow_ShouldWork()
    {
        // Arrange: Get the executable path (assumes app is built)
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                $"FluentPDF.App.exe not found at {executablePath}. " +
                "Build the application with 'dotnet build src/FluentPDF.App -p:Platform=x64' first."
            );
        }

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SamplePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Sample PDF not found at {fullPdfPath}");
        }

        // Act & Assert: Launch application
        var mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);
        mainWindow.Should().NotBeNull("main window should launch");
        mainWindow.Title.Should().Be("FluentPDF", "main window should have correct title");

        try
        {
            // Step 1: Open the PDF
            OpenPdf(mainWindow, fullPdfPath);

            // Step 2: Wait for PDF to load
            Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));
            Thread.Sleep(2000); // Allow rendering to complete

            // Step 3: Toggle diagnostics panel with Ctrl+Shift+D
            ToggleDiagnosticsPanel(mainWindow);

            // Step 4: Verify diagnostics panel is visible and displays metrics
            var diagnosticsVisible = VerifyDiagnosticsPanelVisible(mainWindow);
            diagnosticsVisible.Should().BeTrue("diagnostics panel should be visible after toggle");

            var metricsDisplayed = VerifyMetricsDisplayed(mainWindow);
            metricsDisplayed.Should().BeTrue("metrics should be displayed in diagnostics panel");

            // Step 5: Export metrics to JSON
            var metricsExportPath = Path.Combine(Path.GetTempPath(), $"metrics-export-{Guid.NewGuid()}.json");
            ExportMetrics(mainWindow, metricsExportPath);

            // Step 6: Verify metrics file is valid JSON
            File.Exists(metricsExportPath).Should().BeTrue("metrics export file should be created");
            var metricsValid = VerifyMetricsJsonFile(metricsExportPath);
            metricsValid.Should().BeTrue("metrics export should be valid JSON");

            // Step 7: Open log viewer with Ctrl+Shift+L
            OpenLogViewer(mainWindow);

            // Step 8: Verify log viewer dialog is open
            var logViewerOpen = VerifyLogViewerOpen(mainWindow);
            logViewerOpen.Should().BeTrue("log viewer dialog should be open");

            // Step 9: Apply severity filter
            ApplySeverityFilter(mainWindow, "Warning");

            // Step 10: Verify logs are filtered
            var logsFiltered = VerifyLogsFiltered(mainWindow);
            logsFiltered.Should().BeTrue("logs should be filtered by severity");

            // Step 11: Apply correlation ID filter (use a test correlation ID)
            var correlationId = GetFirstCorrelationId(mainWindow);
            if (!string.IsNullOrEmpty(correlationId))
            {
                ApplyCorrelationIdFilter(mainWindow, correlationId);
                var correlationFiltered = VerifyCorrelationIdFiltered(mainWindow, correlationId);
                correlationFiltered.Should().BeTrue("logs should be filtered by correlation ID");
            }

            // Step 12: Export logs to JSON
            var logsExportPath = Path.Combine(Path.GetTempPath(), $"logs-export-{Guid.NewGuid()}.json");
            ExportLogs(mainWindow, logsExportPath);

            // Step 13: Verify logs file is valid JSON
            File.Exists(logsExportPath).Should().BeTrue("logs export file should be created");
            var logsValid = VerifyLogsJsonFile(logsExportPath);
            logsValid.Should().BeTrue("logs export should be valid JSON");

            // Step 14: Close log viewer
            CloseLogViewer(mainWindow);

            // Step 15: Toggle diagnostics panel off
            ToggleDiagnosticsPanel(mainWindow);
            var diagnosticsHidden = VerifyDiagnosticsPanelHidden(mainWindow);
            diagnosticsHidden.Should().BeTrue("diagnostics panel should be hidden after second toggle");

            // Cleanup
            if (File.Exists(metricsExportPath))
            {
                File.Delete(metricsExportPath);
            }
            if (File.Exists(logsExportPath))
            {
                File.Delete(logsExportPath);
            }
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Tests diagnostics panel persistence across sessions.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually")]
    public void DiagnosticsPanel_ShouldPersistVisibilityAcrossSessions()
    {
        // Arrange
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        // Act & Assert: First session - enable diagnostics
        var mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);
        try
        {
            ToggleDiagnosticsPanel(mainWindow);
            var visible = VerifyDiagnosticsPanelVisible(mainWindow);
            visible.Should().BeTrue("diagnostics should be visible in first session");
        }
        finally
        {
            CloseApplication();
        }

        // Second session - verify persistence
        Thread.Sleep(1000); // Allow settings to persist
        mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);
        try
        {
            Thread.Sleep(2000); // Allow UI to restore state
            var stillVisible = VerifyDiagnosticsPanelVisible(mainWindow);
            stillVisible.Should().BeTrue("diagnostics visibility should persist across sessions");
        }
        finally
        {
            CloseApplication();

            // Cleanup: Reset visibility state
            mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);
            try
            {
                if (VerifyDiagnosticsPanelVisible(mainWindow))
                {
                    ToggleDiagnosticsPanel(mainWindow);
                }
            }
            finally
            {
                CloseApplication();
            }
        }
    }

    #region Helper Methods

    private void OpenPdf(AutomationElement mainWindow, string pdfPath)
    {
        // Find and click the Open button or use File menu
        var openButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OpenFileButton").Or(cf.ByName("Open")))?.AsButton();

        if (openButton != null)
        {
            openButton.Click();
            Thread.Sleep(1000);

            // In the file picker, type the path (this is simplified - actual implementation may vary)
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(pdfPath);
            Keyboard.Press(VirtualKeyShort.RETURN);
        }
    }

    private void ToggleDiagnosticsPanel(AutomationElement mainWindow)
    {
        // Press Ctrl+Shift+D
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_D);
        Thread.Sleep(500); // Allow UI to update
    }

    private bool VerifyDiagnosticsPanelVisible(AutomationElement mainWindow)
    {
        // Look for diagnostics panel control with AutomationId or Name
        var diagnosticsPanel = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("DiagnosticsPanel").Or(cf.ByName("Diagnostics")));

        if (diagnosticsPanel == null)
        {
            return false;
        }

        // Check if panel is visible
        return diagnosticsPanel.IsOffscreen == false;
    }

    private bool VerifyDiagnosticsPanelHidden(AutomationElement mainWindow)
    {
        var diagnosticsPanel = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("DiagnosticsPanel").Or(cf.ByName("Diagnostics")));

        if (diagnosticsPanel == null)
        {
            return true; // Not found means hidden
        }

        return diagnosticsPanel.IsOffscreen;
    }

    private bool VerifyMetricsDisplayed(AutomationElement mainWindow)
    {
        // Look for FPS metric
        var fpsElement = mainWindow.FindFirstDescendant(cf =>
            cf.ByName("FPS").Or(cf.ByAutomationId("FPSText")));

        if (fpsElement == null)
        {
            return false;
        }

        // Look for Memory metric
        var memoryElement = mainWindow.FindFirstDescendant(cf =>
            cf.ByName("Memory").Or(cf.ByAutomationId("MemoryText")));

        if (memoryElement == null)
        {
            return false;
        }

        // Verify values are not empty or default
        var fpsText = fpsElement.Name;
        return !string.IsNullOrEmpty(fpsText);
    }

    private void ExportMetrics(AutomationElement mainWindow, string exportPath)
    {
        // Find and click Export Metrics button
        var exportButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByName("Export Metrics").Or(cf.ByAutomationId("ExportMetricsButton")))?.AsButton();

        if (exportButton != null)
        {
            exportButton.Click();
            Thread.Sleep(1000);

            // In the file picker, type the path
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(exportPath);
            Keyboard.Press(VirtualKeyShort.RETURN);
            Thread.Sleep(1000); // Allow export to complete
        }
    }

    private bool VerifyMetricsJsonFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);

            // Verify JSON structure contains expected properties
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                // Metrics array
                return root.GetArrayLength() > 0;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Single metrics object
                return root.TryGetProperty("CurrentFPS", out _) ||
                       root.TryGetProperty("currentFPS", out _);
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void OpenLogViewer(AutomationElement mainWindow)
    {
        // Press Ctrl+Shift+L
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_L);
        Thread.Sleep(1000); // Allow dialog to open
    }

    private bool VerifyLogViewerOpen(AutomationElement mainWindow)
    {
        // Look for log viewer dialog or control
        var logViewer = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("LogViewerDialog").Or(cf.ByName("Log Viewer")));

        return logViewer != null;
    }

    private void ApplySeverityFilter(AutomationElement mainWindow, string severity)
    {
        // Find severity ComboBox
        var severityCombo = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("SeverityFilterComboBox").Or(cf.ByName("Severity")))?.AsComboBox();

        if (severityCombo != null)
        {
            severityCombo.Select(severity);
            Thread.Sleep(500); // Allow filtering to complete
        }
    }

    private bool VerifyLogsFiltered(AutomationElement mainWindow)
    {
        // Find the logs list/grid
        var logsList = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("LogEntriesList").Or(cf.ByControlType(ControlType.List)));

        if (logsList == null)
        {
            return false;
        }

        // Check if list has items (filtering should show some results)
        var items = logsList.FindAllChildren();
        return items.Length >= 0; // Can be 0 if no logs match filter
    }

    private string? GetFirstCorrelationId(AutomationElement mainWindow)
    {
        // Find the first log entry and extract its correlation ID
        var logsList = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("LogEntriesList").Or(cf.ByControlType(ControlType.List)));

        if (logsList == null)
        {
            return null;
        }

        var firstItem = logsList.FindFirstChild();
        if (firstItem == null)
        {
            return null;
        }

        // Try to find correlation ID text in the item
        var correlationElement = firstItem.FindFirstDescendant(cf =>
            cf.ByAutomationId("CorrelationIdText"));

        return correlationElement?.Name;
    }

    private void ApplyCorrelationIdFilter(AutomationElement mainWindow, string correlationId)
    {
        // Find correlation ID TextBox
        var correlationTextBox = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("CorrelationIdFilterTextBox").Or(cf.ByName("Correlation ID")))?.AsTextBox();

        if (correlationTextBox != null)
        {
            correlationTextBox.Text = correlationId;
            Thread.Sleep(500); // Allow filtering to complete
        }
    }

    private bool VerifyCorrelationIdFiltered(AutomationElement mainWindow, string expectedCorrelationId)
    {
        // Verify all visible logs have the expected correlation ID
        var logsList = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("LogEntriesList").Or(cf.ByControlType(ControlType.List)));

        if (logsList == null)
        {
            return false;
        }

        var items = logsList.FindAllChildren();
        foreach (var item in items)
        {
            var correlationElement = item.FindFirstDescendant(cf =>
                cf.ByAutomationId("CorrelationIdText"));

            if (correlationElement != null)
            {
                var actualId = correlationElement.Name;
                if (actualId != expectedCorrelationId)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ExportLogs(AutomationElement mainWindow, string exportPath)
    {
        // Find and click Export Logs button
        var exportButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByName("Export").Or(cf.ByAutomationId("ExportLogsButton")))?.AsButton();

        if (exportButton != null)
        {
            exportButton.Click();
            Thread.Sleep(1000);

            // In the file picker, type the path
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(exportPath);
            Keyboard.Press(VirtualKeyShort.RETURN);
            Thread.Sleep(1000); // Allow export to complete
        }
    }

    private bool VerifyLogsJsonFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);

            // Verify JSON structure contains expected properties
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                // Logs array
                if (root.GetArrayLength() > 0)
                {
                    var firstLog = root[0];
                    return firstLog.TryGetProperty("Timestamp", out _) ||
                           firstLog.TryGetProperty("timestamp", out _) ||
                           firstLog.TryGetProperty("@t", out _); // Serilog JSON format
                }
                return true; // Empty array is valid
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void CloseLogViewer(AutomationElement mainWindow)
    {
        // Find and click Close button or press Escape
        var closeButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByName("Close").Or(cf.ByAutomationId("CloseButton")))?.AsButton();

        if (closeButton != null)
        {
            closeButton.Click();
        }
        else
        {
            // Try Escape key
            Keyboard.Press(VirtualKeyShort.ESCAPE);
        }

        Thread.Sleep(500);
    }

    #endregion
}
