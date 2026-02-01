// Copyright (c) 2025 FluentPDF. All rights reserved.

namespace FluentPDF.Avalonia;

/// <summary>
/// Parses and stores command-line options for the application.
/// </summary>
public sealed class CommandLineOptions
{
    /// <summary>
    /// Gets whether API server mode is enabled.
    /// </summary>
    public bool ApiServer { get; }

    /// <summary>
    /// Gets the port for the API server (default: 5000).
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets whether headless mode is enabled (no UI).
    /// </summary>
    public bool Headless { get; }

    /// <summary>
    /// Gets whether verbose logging is enabled.
    /// </summary>
    public bool Verbose { get; }

    /// <summary>
    /// Gets whether to run in test mode and exit after completion.
    /// </summary>
    public bool TestMode { get; }

    /// <summary>
    /// Gets the PDF file path to test with (used with --test-render or --test-load).
    /// </summary>
    public string? TestPdfPath { get; }

    /// <summary>
    /// Gets the output directory for test results.
    /// </summary>
    public string? OutputDirectory { get; }

    /// <summary>
    /// Gets whether to test PDF rendering.
    /// </summary>
    public bool TestRender { get; }

    /// <summary>
    /// Gets whether to test PDF loading.
    /// </summary>
    public bool TestLoad { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineOptions"/> class.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public CommandLineOptions(string[] args)
    {
        ApiServer = args.Contains("--api-server", StringComparer.OrdinalIgnoreCase);
        Headless = args.Contains("--headless", StringComparer.OrdinalIgnoreCase);
        Verbose = args.Contains("--verbose", StringComparer.OrdinalIgnoreCase);
        TestMode = args.Contains("--test-mode", StringComparer.OrdinalIgnoreCase);
        TestRender = args.Contains("--test-render", StringComparer.OrdinalIgnoreCase);
        TestLoad = args.Contains("--test-load", StringComparer.OrdinalIgnoreCase);

        // Parse port
        Port = 5000; // Default
        var portIndex = Array.FindIndex(args, a => a.Equals("--port", StringComparison.OrdinalIgnoreCase));
        if (portIndex >= 0 && portIndex + 1 < args.Length)
        {
            if (int.TryParse(args[portIndex + 1], out var port) && port > 0 && port <= 65535)
            {
                Port = port;
            }
        }

        // Parse test PDF path
        var testRenderIndex = Array.FindIndex(args, a => a.Equals("--test-render", StringComparison.OrdinalIgnoreCase));
        var testLoadIndex = Array.FindIndex(args, a => a.Equals("--test-load", StringComparison.OrdinalIgnoreCase));

        if (testRenderIndex >= 0 && testRenderIndex + 1 < args.Length)
        {
            TestPdfPath = args[testRenderIndex + 1];
        }
        else if (testLoadIndex >= 0 && testLoadIndex + 1 < args.Length)
        {
            TestPdfPath = args[testLoadIndex + 1];
        }

        // Parse output directory
        var outputIndex = Array.FindIndex(args, a => a.Equals("--output", StringComparison.OrdinalIgnoreCase));
        if (outputIndex >= 0 && outputIndex + 1 < args.Length)
        {
            OutputDirectory = args[outputIndex + 1];
        }
    }

    /// <summary>
    /// Gets a singleton instance from the parsed arguments.
    /// </summary>
    public static CommandLineOptions? Current { get; set; }
}
