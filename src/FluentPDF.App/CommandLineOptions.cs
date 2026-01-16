// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace FluentPDF.App;

/// <summary>
/// Parsed command-line options for FluentPDF application.
/// </summary>
public class CommandLineOptions
{
    /// <summary>
    /// Gets or sets the file path to open on startup.
    /// </summary>
    public string? OpenFilePath { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically close the app after opening the file.
    /// Useful for automated testing scenarios.
    /// </summary>
    public bool AutoClose { get; set; }

    /// <summary>
    /// Gets or sets the delay (in seconds) before auto-closing.
    /// Default is 2 seconds to allow rendering to complete.
    /// </summary>
    public int AutoCloseDelay { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to enable console logging output.
    /// </summary>
    public bool EnableConsoleLogging { get; set; }

    /// <summary>
    /// Gets or sets whether to enable verbose/debug logging.
    /// </summary>
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// Gets or sets the custom log output path.
    /// </summary>
    public string? LogOutputPath { get; set; }

    /// <summary>
    /// Gets or sets the file path to test rendering diagnostics.
    /// When set, application will load the PDF, render first page, save diagnostic info, and exit.
    /// </summary>
    public string? TestRender { get; set; }

    /// <summary>
    /// Gets or sets whether to output system diagnostics information.
    /// Includes OS version, .NET version, memory, PDFium version, and capabilities.
    /// </summary>
    public bool Diagnostics { get; set; }

    /// <summary>
    /// Gets or sets the file path for comprehensive rendering test.
    /// When set, all pages will be rendered to PNG files in the output directory.
    /// </summary>
    public string? RenderTest { get; set; }

    /// <summary>
    /// Gets or sets the output directory for render test PNG files.
    /// Used with --render-test option. Defaults to current directory.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to capture a crash dump on application failure.
    /// Useful for debugging hard-to-reproduce rendering issues.
    /// </summary>
    public bool CaptureCrashDump { get; set; }

    /// <summary>
    /// Parses command-line arguments into structured options.
    /// </summary>
    /// <param name="args">Command-line arguments from Environment.GetCommandLineArgs().</param>
    /// <returns>Parsed command-line options.</returns>
    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 1; i < args.Length; i++) // Skip first arg (executable path)
        {
            var arg = args[i];

            switch (arg.ToLowerInvariant())
            {
                case "--open-file":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OpenFilePath = args[++i];
                    }
                    break;

                case "--auto-close":
                case "-ac":
                    options.AutoClose = true;
                    break;

                case "--auto-close-delay":
                case "-acd":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var delay))
                    {
                        options.AutoCloseDelay = delay;
                    }
                    break;

                case "--console":
                case "-c":
                    options.EnableConsoleLogging = true;
                    break;

                case "--verbose":
                case "-v":
                    options.VerboseLogging = true;
                    break;

                case "--log-output":
                case "-l":
                    if (i + 1 < args.Length)
                    {
                        options.LogOutputPath = args[++i];
                    }
                    break;

                case "--test-render":
                    if (i + 1 < args.Length)
                    {
                        options.TestRender = args[++i];
                    }
                    break;

                case "--diagnostics":
                    options.Diagnostics = true;
                    break;

                case "--render-test":
                    if (i + 1 < args.Length)
                    {
                        options.RenderTest = args[++i];
                    }
                    break;

                case "--output":
                    if (i + 1 < args.Length)
                    {
                        options.OutputDirectory = args[++i];
                    }
                    break;

                case "--capture-crash-dump":
                    options.CaptureCrashDump = true;
                    break;

                default:
                    // If it's a PDF file path without flag, treat as --open-file
                    if (!arg.StartsWith("-") && !arg.StartsWith("/") &&
                        System.IO.File.Exists(arg) &&
                        arg.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenFilePath = arg;
                    }
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Gets help text for command-line usage.
    /// </summary>
    public static string GetHelpText()
    {
        return @"
FluentPDF Command-Line Options:

Usage: FluentPDF.App.exe [options] [file.pdf]

General Options:
  --open-file, -o <path>        Open the specified PDF file on startup
  --auto-close, -ac             Automatically close app after opening file
  --auto-close-delay, -acd <s>  Delay in seconds before auto-close (default: 2)
  --console, -c                 Enable console logging output
  --verbose, -v                 Enable verbose/debug logging
  --log-output, -l <path>       Custom log output path
  --help, -h                    Show this help message

Diagnostic Commands:
  --test-render <path>          Test render first page of PDF and save diagnostic info
                                Returns exit code: 0=success, 1=load fail, 2=render fail, 3=UI fail
  --diagnostics                 Output system diagnostics (OS, .NET, memory, PDFium version)
  --render-test <path>          Render all pages to PNG files in output directory
  --output <path>               Output directory for render test (used with --render-test)
  --capture-crash-dump          Capture crash dump on application failure for debugging

Examples:
  # Open a PDF file
  FluentPDF.App.exe --open-file ""C:\Documents\test.pdf""
  FluentPDF.App.exe ""C:\Documents\test.pdf""

  # Automated testing workflow
  FluentPDF.App.exe -o ""test.pdf"" -ac -acd 3 -c -v

  # Open with custom logging
  FluentPDF.App.exe -o ""test.pdf"" -l ""C:\logs\fluentpdf.log"" -c

  # Test rendering diagnostics
  FluentPDF.App.exe --test-render ""test.pdf"" --verbose

  # Render all pages to PNGs
  FluentPDF.App.exe --render-test ""test.pdf"" --output ""C:\output"" --verbose

  # Display system diagnostics
  FluentPDF.App.exe --diagnostics
";
    }
}
