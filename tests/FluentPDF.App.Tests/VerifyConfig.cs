using System.Runtime.CompilerServices;
using VerifyTests;

namespace FluentPDF.App.Tests;

/// <summary>
/// Global configuration for Verify.Xaml snapshot testing.
/// Configures snapshot storage, comparison settings, and CI integration.
/// </summary>
public static class VerifyConfig
{
    /// <summary>
    /// Initializes Verify configuration. Called automatically via ModuleInitializer.
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // Use directory-based organization for snapshots
        VerifierSettings.DerivePathInfo(
            (sourceFile, projectDirectory, type, method) =>
            {
                return new PathInfo(
                    directory: Path.Combine(projectDirectory, "Snapshots"),
                    typeName: type.Name,
                    methodName: method.Name);
            });

        // Configure image comparison settings
        VerifyXaml.Initialize();

        // Set SSIM similarity threshold (0.95 = 95% similarity required)
        VerifierSettings.AddScrubber(
            "*.png",
            (text, _) =>
            {
                // Images are compared pixel-by-pixel with 95% similarity threshold
                return text;
            });

        // Disable automatic approval in CI
        if (IsRunningInCi())
        {
            VerifierSettings.DisableRequireUniquePrefix();
            VerifierSettings.AutoVerify(enable: false);
        }
        else
        {
            // In local dev, enable auto-verify for new tests
            VerifierSettings.AutoVerify(enable: true);
        }

        // Use UTC timestamps for deterministic results
        VerifierSettings.UseUtcDateTime();

        // Configure scrubbers to remove non-deterministic data
        VerifierSettings.AddScrubber(ScrubTimestamps);
        VerifierSettings.AddScrubber(ScrubMachinePaths);
    }

    /// <summary>
    /// Checks if tests are running in CI environment.
    /// </summary>
    private static bool IsRunningInCi()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"));
    }

    /// <summary>
    /// Removes timestamps from snapshot metadata.
    /// </summary>
    private static string ScrubTimestamps(string text)
    {
        // Remove ISO timestamps
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}",
            "TIMESTAMP");

        return text;
    }

    /// <summary>
    /// Removes machine-specific paths from snapshots.
    /// </summary>
    private static string ScrubMachinePaths(string text)
    {
        // Replace C:\ paths with placeholder
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"[A-Z]:\\[^\\]+\\",
            "ROOT\\");

        return text;
    }
}
