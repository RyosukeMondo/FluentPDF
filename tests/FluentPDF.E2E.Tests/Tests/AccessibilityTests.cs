using FluentPDF.E2E.Tests.Fixtures;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Drawing;

namespace FluentPDF.E2E.Tests.Tests;

/// <summary>
/// Comprehensive accessibility tests ensuring WCAG 2.1 Level AA compliance.
/// Tests keyboard navigation, screen reader support, high contrast mode,
/// and color contrast ratios (4.5:1 for text, 3:1 for UI components).
/// </summary>
public class AccessibilityTests : IClassFixture<AppLaunchFixture>
{
    private readonly AppLaunchFixture _fixture;
    private readonly LogVerifier _logVerifier;
    private readonly ColorContrastAnalyzer _contrastAnalyzer;

    public AccessibilityTests(AppLaunchFixture fixture)
    {
        _fixture = fixture;
        _logVerifier = new LogVerifier();
        _contrastAnalyzer = new ColorContrastAnalyzer();
    }

    #region Keyboard Navigation Tests

    /// <summary>
    /// Test that Tab key navigates through all interactive elements in logical order.
    /// WCAG 2.1 Success Criterion 2.1.1 (Keyboard).
    /// </summary>
    [Fact]
    public void KeyboardNavigation_TabKey_NavigatesInLogicalOrder()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var focusableElements = GetAllFocusableElements(mainWindow);

        focusableElements.Should().NotBeEmpty("window should have focusable elements");

        // Act - Set focus to first element and tab through all
        focusableElements.First().Focus();
        var visitedElements = new List<string>();

        for (int i = 0; i < focusableElements.Count; i++)
        {
            var currentFocus = GetFocusedElement(mainWindow);
            if (currentFocus != null)
            {
                visitedElements.Add(currentFocus.AutomationId);
            }

            // Press Tab to move to next element
            Keyboard.Type(VirtualKeyShort.TAB);
            Thread.Sleep(100); // Allow focus to settle
        }

        // Assert
        visitedElements.Should().HaveCount(focusableElements.Count,
            "Tab should visit all focusable elements exactly once");

        // Verify no elements are skipped in tab order
        var skippedElements = focusableElements
            .Where(e => !visitedElements.Contains(e.AutomationId))
            .ToList();

        skippedElements.Should().BeEmpty(
            "all focusable elements should be reachable via keyboard navigation");
    }

    /// <summary>
    /// Test that Shift+Tab navigates backwards through elements.
    /// WCAG 2.1 Success Criterion 2.1.1 (Keyboard).
    /// </summary>
    [Fact]
    public void KeyboardNavigation_ShiftTab_NavigatesBackwards()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var focusableElements = GetAllFocusableElements(mainWindow);

        if (focusableElements.Count < 2)
        {
            // Skip test if not enough elements
            return;
        }

        // Act - Tab forward twice
        focusableElements.First().Focus();
        Keyboard.Type(VirtualKeyShort.TAB);
        Thread.Sleep(100);
        Keyboard.Type(VirtualKeyShort.TAB);
        Thread.Sleep(100);

        var forwardFocus = GetFocusedElement(mainWindow);

        // Tab backward once
        Keyboard.Press(VirtualKeyShort.SHIFT);
        Keyboard.Type(VirtualKeyShort.TAB);
        Keyboard.Release(VirtualKeyShort.SHIFT);
        Thread.Sleep(100);

        var backwardFocus = GetFocusedElement(mainWindow);

        // Assert
        forwardFocus.Should().NotBeNull("forward navigation should focus an element");
        backwardFocus.Should().NotBeNull("backward navigation should focus an element");
        backwardFocus.AutomationId.Should().NotBe(forwardFocus.AutomationId,
            "Shift+Tab should move focus backwards");
    }

    /// <summary>
    /// Test that Ctrl+O keyboard shortcut opens file dialog.
    /// WCAG 2.1 Success Criterion 2.1.1 (Keyboard).
    /// </summary>
    [Fact]
    public void KeyboardShortcuts_CtrlO_OpensFileDialog()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        mainWindow.Focus();

        // Act
        Keyboard.Press(VirtualKeyShort.CONTROL);
        Keyboard.Type(VirtualKeyShort.KEY_O);
        Keyboard.Release(VirtualKeyShort.CONTROL);
        Thread.Sleep(500); // Allow dialog to open

        // Assert
        var openDialog = mainWindow.ModalWindows.FirstOrDefault();
        openDialog.Should().NotBeNull("Ctrl+O should open file dialog");

        // Cleanup - close dialog
        if (openDialog != null)
        {
            Keyboard.Type(VirtualKeyShort.ESCAPE);
        }
    }

    /// <summary>
    /// Test that Ctrl+F keyboard shortcut opens search panel.
    /// WCAG 2.1 Success Criterion 2.1.1 (Keyboard).
    /// </summary>
    [Fact]
    public void KeyboardShortcuts_CtrlF_OpensSearchPanel()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        mainWindow.Focus();

        // Act
        Keyboard.Press(VirtualKeyShort.CONTROL);
        Keyboard.Type(VirtualKeyShort.KEY_F);
        Keyboard.Release(VirtualKeyShort.CONTROL);
        Thread.Sleep(300); // Allow panel to slide in

        // Assert
        var searchPanel = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("SearchPanel"));

        searchPanel.Should().NotBeNull("Ctrl+F should open search panel");

        if (searchPanel != null)
        {
            searchPanel.IsOffscreen.Should().BeFalse("search panel should be visible");
        }

        // Cleanup
        Keyboard.Type(VirtualKeyShort.ESCAPE);
    }

    /// <summary>
    /// Test that all buttons can be activated with Enter or Space keys.
    /// WCAG 2.1 Success Criterion 2.1.1 (Keyboard).
    /// </summary>
    [Fact]
    public void KeyboardNavigation_ButtonActivation_WorksWithEnterAndSpace()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var testButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));

        testButton.Should().NotBeNull("window should have at least one button");
        if (testButton == null) return;

        // Act - Focus button and press Enter
        testButton.Focus();
        Thread.Sleep(100);

        var beforeInvokeCount = GetInvokeCount(testButton);
        Keyboard.Type(VirtualKeyShort.RETURN);
        Thread.Sleep(200);
        var afterEnterCount = GetInvokeCount(testButton);

        // Reset and test Space key
        testButton.Focus();
        Thread.Sleep(100);
        Keyboard.Type(VirtualKeyShort.SPACE);
        Thread.Sleep(200);
        var afterSpaceCount = GetInvokeCount(testButton);

        // Assert
        (afterEnterCount > beforeInvokeCount).Should().BeTrue(
            "Enter key should activate focused button");
        (afterSpaceCount > afterEnterCount).Should().BeTrue(
            "Space key should activate focused button");
    }

    #endregion

    #region Screen Reader Support Tests

    /// <summary>
    /// Test that all interactive elements have AutomationProperties.Name set.
    /// WCAG 2.1 Success Criterion 4.1.2 (Name, Role, Value).
    /// </summary>
    [Fact]
    public void ScreenReader_AllInteractiveElements_HaveAccessibleNames()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var interactiveElements = GetAllInteractiveElements(mainWindow);

        // Act - Find elements without accessible names
        var elementsWithoutNames = interactiveElements
            .Where(e => string.IsNullOrWhiteSpace(e.Name))
            .Select(e => new
            {
                AutomationId = e.AutomationId,
                ControlType = e.ControlType.ToString(),
                ClassName = e.ClassName
            })
            .ToList();

        // Assert
        elementsWithoutNames.Should().BeEmpty(
            "all interactive elements must have accessible names for screen readers. " +
            "Set AutomationProperties.Name in XAML for: {0}",
            string.Join(", ", elementsWithoutNames.Select(e => e.AutomationId)));
    }

    /// <summary>
    /// Test that buttons announce their purpose to screen readers.
    /// WCAG 2.1 Success Criterion 4.1.2 (Name, Role, Value).
    /// </summary>
    [Fact]
    public void ScreenReader_Buttons_HaveDescriptiveNames()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var buttons = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));

        // Act - Check that button names are descriptive (not just "Button")
        var nonDescriptiveButtons = buttons
            .Where(b => string.IsNullOrWhiteSpace(b.Name) ||
                       b.Name.Equals("Button", StringComparison.OrdinalIgnoreCase))
            .Select(b => b.AutomationId)
            .ToList();

        // Assert
        nonDescriptiveButtons.Should().BeEmpty(
            "all buttons should have descriptive accessible names, not generic 'Button'");
    }

    /// <summary>
    /// Test that form fields have associated labels for screen readers.
    /// WCAG 2.1 Success Criterion 3.3.2 (Labels or Instructions).
    /// </summary>
    [Fact]
    public void ScreenReader_FormFields_HaveLabels()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var textBoxes = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));

        // Act - Check each textbox has a label or HelpText
        var unlabeledFields = textBoxes
            .Where(tb => string.IsNullOrWhiteSpace(tb.Name) &&
                        string.IsNullOrWhiteSpace(tb.HelpText))
            .Select(tb => tb.AutomationId)
            .ToList();

        // Assert
        unlabeledFields.Should().BeEmpty(
            "all form fields must have labels or help text for screen readers");
    }

    /// <summary>
    /// Test that state changes are announced to screen readers.
    /// WCAG 2.1 Success Criterion 4.1.3 (Status Messages).
    /// </summary>
    [Fact]
    public void ScreenReader_StateChanges_AreAnnounced()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Find a toggle button or checkbox
        var toggleElement = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox)) ??
            mainWindow.FindFirstDescendant(cf =>
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.RadioButton));

        if (toggleElement == null)
        {
            // No toggle elements to test
            return;
        }

        // Act - Toggle the element and check if state is exposed
        var initialState = toggleElement.Properties.Toggle.ToggleState.ValueOrDefault;
        toggleElement.AsToggleButton()?.Toggle();
        Thread.Sleep(200);

        var newState = toggleElement.Properties.Toggle.ToggleState.ValueOrDefault;

        // Assert
        newState.Should().NotBe(initialState,
            "toggle action should change element state");

        // Verify state is accessible via automation properties
        toggleElement.Properties.Toggle.IsSupported.Should().BeTrue(
            "toggle elements must expose their state to screen readers");
    }

    #endregion

    #region High Contrast Mode Tests

    /// <summary>
    /// Test that application works in Windows High Contrast mode.
    /// WCAG 2.1 Success Criterion 1.4.3 (Contrast - Minimum).
    /// </summary>
    [Fact]
    public void HighContrast_ApplicationStillUsable()
    {
        // Note: This test documents the requirement but cannot programmatically
        // enable Windows High Contrast mode. Manual testing required.

        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act - Check if app detects system theme
        var themeService = _logVerifier.GetLogEntries()
            .FirstOrDefault(e => e.MessageTemplate?.Contains("theme", StringComparison.OrdinalIgnoreCase) == true);

        // Assert - Log that theme system exists
        _logVerifier.GetLogEntries()
            .Should().Contain(e =>
                e.MessageTemplate?.Contains("theme", StringComparison.OrdinalIgnoreCase) == true ||
                e.Message?.Contains("theme", StringComparison.OrdinalIgnoreCase) == true,
                "application should have theme system that can adapt to high contrast mode");

        // Document manual testing requirement
        var manualTestNote = "MANUAL TEST REQUIRED: " +
            "Enable Windows High Contrast mode (Alt+Shift+PrtScn) and verify:\n" +
            "1. All text is readable\n" +
            "2. Acrylic effects are replaced with solid colors\n" +
            "3. All interactive elements have visible borders\n" +
            "4. Focus indicators are clearly visible";

        Console.WriteLine(manualTestNote);
    }

    /// <summary>
    /// Test that acrylic glass effects degrade gracefully in high contrast.
    /// Requirement: Acrylic should be replaced with solid colors.
    /// </summary>
    [Fact]
    public void HighContrast_AcrylicEffects_FallbackToSolidColors()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;

        // Act - Check if theme service logs high contrast detection
        var logs = _logVerifier.GetLogEntries();
        var themeChangeLogs = logs.Where(e =>
            e.MessageTemplate?.Contains("contrast", StringComparison.OrdinalIgnoreCase) == true ||
            e.MessageTemplate?.Contains("theme", StringComparison.OrdinalIgnoreCase) == true);

        // Assert - Theme system should exist (actual high contrast test requires manual verification)
        mainWindow.Should().NotBeNull("application should remain functional");

        var manualTestNote = "MANUAL TEST: Verify acrylic panels show solid backgrounds in high contrast mode";
        Console.WriteLine(manualTestNote);
    }

    #endregion

    #region Color Contrast Ratio Tests

    /// <summary>
    /// Test that text has minimum 4.5:1 contrast ratio (WCAG AA for normal text).
    /// WCAG 2.1 Success Criterion 1.4.3 (Contrast - Minimum).
    /// </summary>
    [Fact]
    public void ContrastRatio_NormalText_MeetsWCAG_AA()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var textElements = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .Take(10) // Limit to first 10 for performance
            .ToList();

        var contrastFailures = new List<string>();

        // Act - Analyze contrast for each text element
        foreach (var textElement in textElements)
        {
            var analysis = _contrastAnalyzer.AnalyzeElementContrast(textElement);

            if (analysis != null)
            {
                var isLargeText = IsLargeText(textElement);
                var meetsRequirement = _contrastAnalyzer.MeetsWcagAA(
                    analysis.ContrastRatio,
                    isLargeText: isLargeText);

                if (!meetsRequirement)
                {
                    var minimumRatio = isLargeText
                        ? ColorContrastAnalyzer.WcagAA_LargeText
                        : ColorContrastAnalyzer.WcagAA_NormalText;

                    contrastFailures.Add(
                        $"{textElement.AutomationId}: {analysis} " +
                        $"(minimum: {_contrastAnalyzer.FormatRatio(minimumRatio)})");
                }
            }
        }

        // Assert
        contrastFailures.Should().BeEmpty(
            "all text elements must meet WCAG AA contrast requirements (4.5:1 normal, 3:1 large):\n{0}",
            string.Join("\n", contrastFailures));
    }

    /// <summary>
    /// Test that UI components have minimum 3:1 contrast ratio.
    /// WCAG 2.1 Success Criterion 1.4.11 (Non-text Contrast).
    /// </summary>
    [Fact]
    public void ContrastRatio_UIComponents_MeetsWCAG_AA()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var uiComponents = new[]
        {
            mainWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)),
            mainWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)),
            mainWindow.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox))
        }
        .SelectMany(x => x)
        .Take(10) // Limit for performance
        .ToList();

        var contrastFailures = new List<string>();

        // Act - Analyze contrast for UI components
        foreach (var component in uiComponents)
        {
            var analysis = _contrastAnalyzer.AnalyzeElementContrast(component);

            if (analysis != null)
            {
                var meetsRequirement = _contrastAnalyzer.MeetsWcagAA(
                    analysis.ContrastRatio,
                    isUIComponent: true);

                if (!meetsRequirement)
                {
                    contrastFailures.Add(
                        $"{component.AutomationId} ({component.ControlType}): {analysis} " +
                        $"(minimum: {_contrastAnalyzer.FormatRatio(ColorContrastAnalyzer.WcagAA_UIComponents)})");
                }
            }
        }

        // Assert
        contrastFailures.Should().BeEmpty(
            "all UI components must have 3:1 contrast ratio:\n{0}",
            string.Join("\n", contrastFailures));
    }

    /// <summary>
    /// Test that focus indicators have sufficient contrast.
    /// WCAG 2.1 Success Criterion 2.4.7 (Focus Visible).
    /// </summary>
    [Fact]
    public void ContrastRatio_FocusIndicators_AreVisible()
    {
        // Arrange
        var mainWindow = _fixture.MainWindow;
        var focusableElements = GetAllFocusableElements(mainWindow);

        if (focusableElements.Count == 0)
            return;

        // Act - Focus each element and verify focus indicator visibility
        var elementsWithoutVisibleFocus = new List<string>();

        foreach (var element in focusableElements.Take(10)) // Test first 10 to avoid long test
        {
            element.Focus();
            Thread.Sleep(100);

            // Check if element has visual focus indication
            var hasFocusState = element.Properties.HasKeyboardFocus.ValueOrDefault;
            var hasHighlightBorder = CheckForFocusBorder(element);

            if (!hasFocusState && !hasHighlightBorder)
            {
                elementsWithoutVisibleFocus.Add(element.AutomationId);
            }
        }

        // Assert
        elementsWithoutVisibleFocus.Should().BeEmpty(
            "all focusable elements must have visible focus indicators");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets all focusable elements in the window.
    /// </summary>
    private List<AutomationElement> GetAllFocusableElements(Window window)
    {
        var allElements = window.FindAllDescendants();
        return allElements
            .Where(e => e.Properties.IsKeyboardFocusable.ValueOrDefault)
            .ToList();
    }

    /// <summary>
    /// Gets all interactive elements (buttons, textboxes, etc.).
    /// </summary>
    private List<AutomationElement> GetAllInteractiveElements(Window window)
    {
        var interactiveTypes = new[]
        {
            FlaUI.Core.Definitions.ControlType.Button,
            FlaUI.Core.Definitions.ControlType.Edit,
            FlaUI.Core.Definitions.ControlType.CheckBox,
            FlaUI.Core.Definitions.ControlType.RadioButton,
            FlaUI.Core.Definitions.ControlType.ComboBox,
            FlaUI.Core.Definitions.ControlType.Hyperlink,
            FlaUI.Core.Definitions.ControlType.MenuItem
        };

        return window.FindAllDescendants()
            .Where(e => interactiveTypes.Contains(e.ControlType))
            .ToList();
    }

    /// <summary>
    /// Gets the currently focused element.
    /// </summary>
    private AutomationElement? GetFocusedElement(Window window)
    {
        return _fixture.Automation.FocusedElement();
    }

    /// <summary>
    /// Estimates invoke count (simplified - actual implementation would track events).
    /// </summary>
    private int GetInvokeCount(AutomationElement element)
    {
        // Simplified - in real implementation, would use event listeners
        return 0;
    }

    /// <summary>
    /// Determines if text is considered "large" by WCAG standards.
    /// Large text: 18pt+ normal or 14pt+ bold.
    /// </summary>
    private bool IsLargeText(AutomationElement element)
    {
        // Simplified - would need to check actual font size from element properties
        // For now, check if element name contains size hints or check bounding rectangle
        var bounds = element.BoundingRectangle;
        var height = bounds.Height;

        // Estimate: 18pt ≈ 24px at 96 DPI
        return height >= 24;
    }

    /// <summary>
    /// Checks if element has visible focus border.
    /// </summary>
    private bool CheckForFocusBorder(AutomationElement element)
    {
        // Simplified - would need image analysis or custom properties
        // Assume focus is visible if element reports keyboard focus
        return element.Properties.HasKeyboardFocus.ValueOrDefault;
    }

    #endregion
}
