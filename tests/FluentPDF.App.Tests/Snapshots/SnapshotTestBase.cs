using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Tests.Snapshots;

/// <summary>
/// Base class for snapshot tests using Verify framework.
/// Provides helper methods for WinUI 3 control verification.
/// </summary>
[UsesVerify]
public abstract class SnapshotTestBase
{
    /// <summary>
    /// Verifies the visual appearance of a WinUI control against an approved snapshot.
    /// </summary>
    /// <param name="control">The control to verify.</param>
    /// <param name="settings">Optional Verify settings to customize snapshot behavior.</param>
    /// <returns>Task that completes when verification is done.</returns>
    protected Task VerifyControl(UIElement control, VerifySettings? settings = null)
    {
        settings ??= new VerifySettings();
        return Verifier.Verify(control, settings);
    }

    /// <summary>
    /// Verifies the visual appearance of a WinUI control with a specific scenario name.
    /// Use this when testing multiple states of the same control.
    /// </summary>
    /// <param name="control">The control to verify.</param>
    /// <param name="scenarioName">Name describing the scenario (e.g., "DefaultState", "ZoomedIn").</param>
    /// <returns>Task that completes when verification is done.</returns>
    protected Task VerifyControl(UIElement control, string scenarioName)
    {
        var settings = new VerifySettings();
        settings.UseParameters(scenarioName);
        return Verifier.Verify(control, settings);
    }

    /// <summary>
    /// Verifies a window's appearance against an approved snapshot.
    /// </summary>
    /// <param name="window">The window to verify.</param>
    /// <param name="settings">Optional Verify settings to customize snapshot behavior.</param>
    /// <returns>Task that completes when verification is done.</returns>
    protected Task VerifyWindow(Window window, VerifySettings? settings = null)
    {
        settings ??= new VerifySettings();
        return Verifier.Verify(window, settings);
    }

    /// <summary>
    /// Creates a VerifySettings instance configured for testing control states.
    /// Useful for parameterized tests or when you need custom scrubbing.
    /// </summary>
    /// <param name="stateDescription">Description of the control state being tested.</param>
    /// <returns>Configured VerifySettings instance.</returns>
    protected VerifySettings CreateSettings(string stateDescription)
    {
        var settings = new VerifySettings();
        settings.UseParameters(stateDescription);
        return settings;
    }

    /// <summary>
    /// Creates a VerifySettings instance with custom scrubbers for non-deterministic values.
    /// </summary>
    /// <param name="scrubbers">Action to configure scrubbers on the settings.</param>
    /// <returns>Configured VerifySettings instance.</returns>
    protected VerifySettings CreateSettingsWithScrubbers(Action<VerifySettings> scrubbers)
    {
        var settings = new VerifySettings();
        scrubbers(settings);
        return settings;
    }
}
