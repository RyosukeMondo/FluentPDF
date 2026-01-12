using System.Runtime.CompilerServices;

namespace FluentPDF.App.Tests.Snapshots;

/// <summary>
/// Configures Verify settings for snapshot testing across the test assembly.
/// This module initializer runs once before any tests execute.
/// </summary>
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Configure snapshot directory relative to test project
        Verifier.UseProjectRelativeDirectory("Snapshots/Verified");

        // Scrub GUIDs to ensure deterministic snapshots
        VerifierSettings.ScrubInlineGuids();

        // Scrub machine-specific paths
        VerifierSettings.AddScrubber(s => s.Replace(Environment.CurrentDirectory, "{CurrentDirectory}"));
    }
}
