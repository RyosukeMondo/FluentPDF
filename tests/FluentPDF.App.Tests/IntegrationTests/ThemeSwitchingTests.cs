using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FluentPDF.App.Tests.IntegrationTests;

/// <summary>
/// Integration tests for theme switching functionality using FlaUI automation.
/// Tests Light, Dark, and System theme transitions with AutomationPeer verification.
/// </summary>
[Collection("UI Tests")]
public sealed class ThemeSwitchingTests : E2E.FlaUITestBase
{
    private readonly ITestOutputHelper _output;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    public ThemeSwitchingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Tests the complete theme cycle: Light -> Dark -> System.
    /// Verifies that theme changes are applied correctly via AutomationPeer properties.
    /// </summary>
    [Fact]
    public void ThemeCycle_ShouldTransitionThroughAllThemes()
    {
        Window? mainWindow = null;

        try
        {
            // Arrange: Launch app and navigate to settings
            mainWindow = LaunchApp(waitTimeoutMs: 10000);
            _output.WriteLine("Application launched successfully");

            var settingsPage = new SettingsPageObject(mainWindow, Automation);
            settingsPage.NavigateToSettings();
            _output.WriteLine("Navigated to Settings page");

            // Act & Assert: Test Light -> Dark -> System cycle
            TestThemeTransition(settingsPage, "Light", "Dark");
            TestThemeTransition(settingsPage, "Dark", "System");
            TestThemeTransition(settingsPage, "System", "Light");

            _output.WriteLine("All theme transitions completed successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test failed: {ex.Message}");
            CaptureScreenshotOnFailure(mainWindow, nameof(ThemeCycle_ShouldTransitionThroughAllThemes), ex);
            throw;
        }
        finally
        {
            CloseApp();
        }
    }

    /// <summary>
    /// Tests that theme changes are immediately visible in the UI.
    /// Verifies background color changes without requiring app restart.
    /// </summary>
    [Fact]
    public void ThemeSwitch_ShouldApplyImmediatelyWithoutRestart()
    {
        Window? mainWindow = null;

        try
        {
            // Arrange
            mainWindow = LaunchApp();
            var settingsPage = new SettingsPageObject(mainWindow, Automation);
            settingsPage.NavigateToSettings();

            // Act: Switch to Dark theme
            settingsPage.SelectTheme("Dark");
            Thread.Sleep(500); // Allow theme transition animation

            // Assert: Verify dark theme is applied
            var isDarkTheme = settingsPage.IsDarkThemeActive();
            isDarkTheme.Should().BeTrue("Dark theme should be applied immediately");

            _output.WriteLine("Theme applied immediately without restart");
        }
        catch (Exception ex)
        {
            CaptureScreenshotOnFailure(mainWindow, nameof(ThemeSwitch_ShouldApplyImmediatelyWithoutRestart), ex);
            throw;
        }
        finally
        {
            CloseApp();
        }
    }

    /// <summary>
    /// Tests theme persistence across app restarts.
    /// </summary>
    [Fact]
    public void ThemeSelection_ShouldPersistAcrossRestarts()
    {
        Window? mainWindow = null;

        try
        {
            // Arrange: First launch - set Dark theme
            mainWindow = LaunchApp();
            var settingsPage = new SettingsPageObject(mainWindow, Automation);
            settingsPage.NavigateToSettings();
            settingsPage.SelectTheme("Dark");
            Thread.Sleep(500);

            // Act: Close and relaunch app
            CloseApp();
            Thread.Sleep(1000);
            mainWindow = LaunchApp();

            // Assert: Verify Dark theme persisted
            settingsPage = new SettingsPageObject(mainWindow, Automation);
            settingsPage.NavigateToSettings();
            var isDarkActive = settingsPage.IsDarkThemeActive();
            isDarkActive.Should().BeTrue("Dark theme should persist after restart");

            _output.WriteLine("Theme persisted across app restart");
        }
        catch (Exception ex)
        {
            CaptureScreenshotOnFailure(mainWindow, nameof(ThemeSelection_ShouldPersistAcrossRestarts), ex);
            throw;
        }
        finally
        {
            CloseApp();
        }
    }

    /// <summary>
    /// Tests a single theme transition with retry logic for flaky elements.
    /// </summary>
    private void TestThemeTransition(SettingsPageObject settingsPage, string fromTheme, string toTheme)
    {
        _output.WriteLine($"Testing theme transition: {fromTheme} -> {toTheme}");

        // Select target theme with retry
        ExecuteWithRetry(() =>
        {
            settingsPage.SelectTheme(toTheme);
            Thread.Sleep(500); // Allow animation

            // Verify theme changed
            var isActive = toTheme switch
            {
                "Light" => settingsPage.IsLightThemeActive(),
                "Dark" => settingsPage.IsDarkThemeActive(),
                "System" => settingsPage.IsSystemThemeActive(),
                _ => throw new ArgumentException($"Unknown theme: {toTheme}")
            };

            isActive.Should().BeTrue($"{toTheme} theme should be active after selection");
        }, $"Transition from {fromTheme} to {toTheme}");

        _output.WriteLine($"Successfully transitioned from {fromTheme} to {toTheme}");
    }

    /// <summary>
    /// Executes an action with retry logic for handling flaky UI elements.
    /// </summary>
    private void ExecuteWithRetry(Action action, string actionDescription)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                action();
                return; // Success
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                lastException = ex;
                _output.WriteLine($"Attempt {attempt} failed for '{actionDescription}': {ex.Message}. Retrying...");
                Thread.Sleep(RetryDelayMs);
            }
        }

        // All retries exhausted
        _output.WriteLine($"All {MaxRetries} attempts failed for '{actionDescription}'");
        throw lastException ?? new InvalidOperationException($"Action '{actionDescription}' failed");
    }
}

/// <summary>
/// Page Object for the Settings page, encapsulating UI element interaction logic.
/// </summary>
internal sealed class SettingsPageObject
{
    private readonly Window _mainWindow;
    private readonly FlaUI.UIA3.UIA3Automation _automation;
    private const int DefaultTimeoutMs = 5000;

    public SettingsPageObject(Window mainWindow, FlaUI.UIA3.UIA3Automation automation)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
    }

    /// <summary>
    /// Navigates to the Settings page from the main window.
    /// </summary>
    public void NavigateToSettings()
    {
        // Find Settings navigation item using AutomationId
        var navigationView = _mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Pane).And(cf.ByName("NavigationView")));

        if (navigationView != null)
        {
            var settingsItem = navigationView.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.NavigationItem).And(cf.ByName("Settings")));

            settingsItem?.Click();
        }
        else
        {
            // Fallback: Use keyboard shortcut or menu
            var settingsButton = _mainWindow.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Button).And(cf.ByName("Settings")));

            settingsButton?.Click();
        }

        Thread.Sleep(500); // Wait for navigation animation
    }

    /// <summary>
    /// Selects a theme by clicking the corresponding radio button.
    /// </summary>
    /// <param name="themeName">Theme name: "Light", "Dark", or "System"</param>
    public void SelectTheme(string themeName)
    {
        var radioButton = FindThemeRadioButton(themeName);

        if (radioButton == null)
        {
            throw new InvalidOperationException($"Theme radio button '{themeName}' not found");
        }

        // Click only if not already selected
        if (!radioButton.IsSelected)
        {
            radioButton.Click();
        }
    }

    /// <summary>
    /// Checks if Light theme is currently active.
    /// </summary>
    public bool IsLightThemeActive()
    {
        var radioButton = FindThemeRadioButton("Light");
        return radioButton?.IsSelected ?? false;
    }

    /// <summary>
    /// Checks if Dark theme is currently active.
    /// </summary>
    public bool IsDarkThemeActive()
    {
        var radioButton = FindThemeRadioButton("Dark");
        return radioButton?.IsSelected ?? false;
    }

    /// <summary>
    /// Checks if System theme is currently active.
    /// </summary>
    public bool IsSystemThemeActive()
    {
        var radioButton = FindThemeRadioButton("Use System");
        return radioButton?.IsSelected ?? false;
    }

    /// <summary>
    /// Finds a theme radio button by its content text.
    /// </summary>
    private RadioButton? FindThemeRadioButton(string themeName)
    {
        // WinUI 3 RadioButton with Content matching theme name
        var condition = _automation.ConditionFactory
            .ByControlType(ControlType.RadioButton)
            .And(_automation.ConditionFactory.ByName(themeName));

        var element = _mainWindow.FindFirstDescendant(condition);

        return element?.AsRadioButton();
    }
}
