// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// End-to-end CLI integration tests for marshaling validation commands.
/// Tests CLI commands by invoking FluentPDF.App.exe as a separate process.
/// </summary>
public class CliIntegrationTests
{
    private readonly string _appExePath;
    private readonly string _testPdfPath;
    private readonly string _outputDirectory;

    public CliIntegrationTests()
    {
        // Find FluentPDF.App.exe in the build output directory
        var baseDirectory = AppContext.BaseDirectory;
        var solutionRoot = FindSolutionRoot(baseDirectory);

        // Use x64/Debug build by default
        _appExePath = Path.Combine(solutionRoot, "src", "FluentPDF.App", "bin", "x64", "Debug",
            "net8.0-windows10.0.19041.0", "win-x64", "FluentPDF.App.exe");

        // Use comprehensive test PDF
        _testPdfPath = Path.Combine(baseDirectory, "TestData", "comprehensive_test.pdf");

        // Create temp output directory for test artifacts
        _outputDirectory = Path.Combine(Path.GetTempPath(), "FluentPDF.CliTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outputDirectory);
    }

    /// <summary>
    /// Scenario 1: Developer quick validation - run specific validator during development.
    /// Tests --validate-utf16-marshalling with console output.
    /// </summary>
    [Fact]
    public async Task ValidateUtf16Marshalling_ReturnsSuccessExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var args = "--validate-utf16-marshalling --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.Equal(0, exitCode); // Should pass
        Assert.Contains("UTF-16", output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scenario 2: CI full validation suite - run all validators with JUnit XML output.
    /// Tests --validate-all with --junit-output and --json-output flags.
    /// </summary>
    [Fact]
    public async Task ValidateAll_WithJunitAndJsonOutput_GeneratesReportsAndPassesExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var junitPath = Path.Combine(_outputDirectory, "validation-results.xml");
        var jsonPath = Path.Combine(_outputDirectory, "validation-report.json");
        var args = $"--validate-all --junit-output \"{junitPath}\" --json-output \"{jsonPath}\" --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.True(exitCode == 0 || exitCode == 1, $"Expected exit code 0 or 1, got {exitCode}");

        // Verify JUnit XML file was created and is valid
        Assert.True(File.Exists(junitPath), "JUnit XML file should be created");
        var junitXml = await File.ReadAllTextAsync(junitPath);
        Assert.False(string.IsNullOrWhiteSpace(junitXml), "JUnit XML should not be empty");

        // Validate XML is parseable
        var xmlDoc = XDocument.Parse(junitXml);
        Assert.NotNull(xmlDoc.Root);
        Assert.Equal("testsuites", xmlDoc.Root.Name.LocalName);

        // Verify JSON file was created and is valid
        Assert.True(File.Exists(jsonPath), "JSON file should be created");
        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        Assert.False(string.IsNullOrWhiteSpace(jsonContent), "JSON should not be empty");

        // Validate JSON is parseable
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        Assert.NotNull(jsonDoc.RootElement);

        // Verify console output
        Assert.Contains("validation", output, StringComparison.OrdinalIgnoreCase);

        // Clean up
        File.Delete(junitPath);
        File.Delete(jsonPath);
    }

    /// <summary>
    /// Scenario 3: Performance profiling with baseline comparison.
    /// Tests --profile-marshalling with --compare-baseline flag.
    /// </summary>
    [Fact]
    public async Task ProfileMarshalling_WithBaseline_ComparesAndReturnsExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange - Create a baseline file first
        var baselinePath = Path.Combine(_outputDirectory, "baseline.json");
        var jsonPath = Path.Combine(_outputDirectory, "profile.json");

        // First run: Generate baseline
        var argsBaseline = $"--profile-marshalling --json-output \"{baselinePath}\" --verbose";
        var (exitCodeBaseline, _) = await RunCliCommandAsync(argsBaseline);
        Assert.Equal(0, exitCodeBaseline);
        Assert.True(File.Exists(baselinePath));

        // Second run: Compare against baseline
        var args = $"--profile-marshalling --compare-baseline \"{baselinePath}\" --json-output \"{jsonPath}\" --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.True(exitCode == 0 || exitCode == 1, $"Expected exit code 0 or 1, got {exitCode}");
        Assert.True(File.Exists(jsonPath), "Profiling JSON should be created");
        Assert.Contains("profiling", output, StringComparison.OrdinalIgnoreCase);

        // Verify JSON structure
        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        Assert.NotNull(jsonDoc.RootElement);

        // Clean up
        File.Delete(baselinePath);
        File.Delete(jsonPath);
    }

    /// <summary>
    /// Scenario 4: Workaround regression detection - test documented workarounds.
    /// Tests --test-workarounds command.
    /// </summary>
    [Fact]
    public async Task TestWorkarounds_ReturnsValidExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var args = "--test-workarounds --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.True(exitCode == 0 || exitCode == 1, $"Expected exit code 0 or 1, got {exitCode}");
        Assert.Contains("workaround", output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests --validate-bitmap-marshalling with HTML output.
    /// </summary>
    [Fact]
    public async Task ValidateBitmapMarshalling_WithHtmlOutput_GeneratesHtmlReport()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var htmlPath = Path.Combine(_outputDirectory, "bitmap-report.html");
        var args = $"--validate-bitmap-marshalling --html-output \"{htmlPath}\" --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.Equal(0, exitCode); // Should pass
        Assert.True(File.Exists(htmlPath), "HTML report should be created");

        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        Assert.False(string.IsNullOrWhiteSpace(htmlContent), "HTML should not be empty");
        Assert.Contains("<html", htmlContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bitmap", htmlContent, StringComparison.OrdinalIgnoreCase);

        // Clean up
        File.Delete(htmlPath);
    }

    /// <summary>
    /// Tests --validate-annotation-marshalling command.
    /// </summary>
    [Fact]
    public async Task ValidateAnnotationMarshalling_ReturnsSuccessExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var args = "--validate-annotation-marshalling --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.Equal(0, exitCode); // Should pass
        Assert.Contains("annotation", output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests --validate-threading-model command.
    /// </summary>
    [Fact]
    public async Task ValidateThreadingModel_ReturnsSuccessExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var args = "--validate-threading-model --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.Equal(0, exitCode); // Should pass
        Assert.Contains("threading", output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests --validate-buffer-safety command.
    /// </summary>
    [Fact]
    public async Task ValidateBufferSafety_ReturnsSuccessExitCode()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var args = "--validate-buffer-safety --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.Equal(0, exitCode); // Should pass
        Assert.Contains("buffer", output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests --validate-all with all three output formats simultaneously.
    /// </summary>
    [Fact]
    public async Task ValidateAll_WithAllOutputFormats_GeneratesAllReports()
    {
        // Skip if app doesn't exist (non-Windows environment)
        if (!File.Exists(_appExePath))
        {
            return;
        }

        // Arrange
        var junitPath = Path.Combine(_outputDirectory, "all-formats-results.xml");
        var jsonPath = Path.Combine(_outputDirectory, "all-formats-report.json");
        var htmlPath = Path.Combine(_outputDirectory, "all-formats-report.html");
        var args = $"--validate-all --junit-output \"{junitPath}\" --json-output \"{jsonPath}\" --html-output \"{htmlPath}\" --verbose";

        // Act
        var (exitCode, output) = await RunCliCommandAsync(args);

        // Assert
        Assert.True(exitCode == 0 || exitCode == 1, $"Expected exit code 0 or 1, got {exitCode}");

        // Verify all three files were created
        Assert.True(File.Exists(junitPath), "JUnit XML file should be created");
        Assert.True(File.Exists(jsonPath), "JSON file should be created");
        Assert.True(File.Exists(htmlPath), "HTML file should be created");

        // Verify JUnit XML is valid
        var junitXml = await File.ReadAllTextAsync(junitPath);
        var xmlDoc = XDocument.Parse(junitXml);
        Assert.NotNull(xmlDoc.Root);

        // Verify JSON is valid
        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        Assert.NotNull(jsonDoc.RootElement);

        // Verify HTML is valid
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        Assert.Contains("<html", htmlContent, StringComparison.OrdinalIgnoreCase);

        // Clean up
        File.Delete(junitPath);
        File.Delete(jsonPath);
        File.Delete(htmlPath);
    }

    /// <summary>
    /// Runs a CLI command and returns the exit code and output.
    /// </summary>
    private async Task<(int exitCode, string output)> RunCliCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _appExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_appExePath)
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait up to 60 seconds for command to complete
        await process.WaitForExitAsync(TimeSpan.FromSeconds(60));

        var allOutput = outputBuilder.ToString() + errorBuilder.ToString();
        return (process.ExitCode, allOutput);
    }

    /// <summary>
    /// Finds the solution root directory by walking up from the base directory.
    /// </summary>
    private static string FindSolutionRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find solution root directory");
    }
}

/// <summary>
/// Extension methods for Process class.
/// </summary>
internal static class ProcessExtensions
{
    /// <summary>
    /// Waits asynchronously for the process to exit with a timeout.
    /// </summary>
    public static async Task WaitForExitAsync(this Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred - kill the process
            if (!process.HasExited)
            {
                process.Kill();
            }
            throw new TimeoutException($"Process did not exit within {timeout.TotalSeconds} seconds");
        }
    }
}
