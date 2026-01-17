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
    /// Gets or sets the file path for thumbnail testing.
    /// When set, application will load the PDF, render all thumbnails, and report results.
    /// </summary>
    public string? TestThumbnails { get; set; }

    /// <summary>
    /// Gets or sets whether to run P/Invoke marshalling verification.
    /// When set, application will verify all PDFium P/Invoke signatures and marshalling correctness.
    /// </summary>
    public bool VerifyMarshalling { get; set; }

    /// <summary>
    /// Gets or sets whether to generate P/Invoke marshalling coverage report.
    /// When set, application will generate a detailed coverage report for PDFium P/Invoke signatures.
    /// </summary>
    public bool MarshallingReport { get; set; }

    /// <summary>
    /// Gets or sets the output file path for reports.
    /// Used with --marshalling-report to save report to a file instead of console.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets whether to list all available CLI tests.
    /// When set, application will discover and display all ICliTest implementations and exit.
    /// </summary>
    public bool ListTests { get; set; }

    /// <summary>
    /// Gets or sets the name of a specific CLI test to run.
    /// When set, application will execute the named test and exit with appropriate code.
    /// </summary>
    public string? RunTest { get; set; }

    /// <summary>
    /// Gets or sets whether to run all available CLI tests.
    /// When set, application will execute all discovered tests and exit with 0 if all pass, 1 otherwise.
    /// </summary>
    public bool RunAllTests { get; set; }

    /// <summary>
    /// Gets or sets the file path for page rendering test.
    /// When set, application will run the page-render CLI test on the specified PDF.
    /// </summary>
    public string? TestPageRender { get; set; }

    /// <summary>
    /// Gets or sets the file path for all-thumbnails test.
    /// When set, application will run the thumbnail-all-pages CLI test on the specified PDF.
    /// </summary>
    public string? TestAllThumbnails { get; set; }

    /// <summary>
    /// Gets or sets the file path for text extraction test.
    /// When set, application will run the text-extraction CLI test on the specified PDF.
    /// </summary>
    public string? TestTextExtract { get; set; }

    /// <summary>
    /// Gets or sets the file path for form fields rendering test.
    /// When set, application will run the form-field-render CLI test on the specified PDF.
    /// </summary>
    public string? TestFormFields { get; set; }

    /// <summary>
    /// Gets or sets the file path for batch rendering all pages.
    /// When set, application will run the batch-render CLI test on the specified PDF.
    /// </summary>
    public string? RenderAllPages { get; set; }

    /// <summary>
    /// Gets or sets the file path for bookmarks extraction test.
    /// When set, application will run the bookmarks CLI test on the specified PDF.
    /// </summary>
    public string? TestBookmarks { get; set; }

    /// <summary>
    /// Gets or sets the file path for search functionality test.
    /// When set, application will run the search CLI test on the specified PDF.
    /// </summary>
    public string? TestSearch { get; set; }

    /// <summary>
    /// Gets or sets the search term for the search functionality test.
    /// Used with --test-search option to specify what text to search for.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the file path for page rotation test.
    /// When set, application will run the page rotation CLI test on the specified PDF.
    /// </summary>
    public string? TestPageRotate { get; set; }

    /// <summary>
    /// Gets or sets the file path for page deletion test.
    /// When set, application will run the page deletion CLI test on the specified PDF.
    /// </summary>
    public string? TestPageDelete { get; set; }

    /// <summary>
    /// Gets or sets the file path for page reorder test.
    /// When set, application will run the page reorder CLI test on the specified PDF.
    /// </summary>
    public string? TestPageReorder { get; set; }

    /// <summary>
    /// Gets or sets the file path for annotations detection test.
    /// When set, application will run the annotations CLI test on the specified PDF.
    /// </summary>
    public string? TestAnnotations { get; set; }

    /// <summary>
    /// Gets or sets the file path for metadata extraction test.
    /// When set, application will run the metadata CLI test on the specified PDF.
    /// </summary>
    public string? TestMetadata { get; set; }

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

                case "--test-thumbnails":
                    if (i + 1 < args.Length)
                    {
                        options.TestThumbnails = args[++i];
                    }
                    break;

                case "--verify-marshalling":
                    options.VerifyMarshalling = true;
                    break;

                case "--marshalling-report":
                    options.MarshallingReport = true;
                    break;

                case "--output-path":
                    if (i + 1 < args.Length)
                    {
                        options.OutputPath = args[++i];
                    }
                    break;

                case "--list-tests":
                    options.ListTests = true;
                    break;

                case "--run-test":
                    if (i + 1 < args.Length)
                    {
                        options.RunTest = args[++i];
                    }
                    break;

                case "--run-all-tests":
                    options.RunAllTests = true;
                    break;

                case "--test-page-render":
                    if (i + 1 < args.Length)
                    {
                        options.TestPageRender = args[++i];
                    }
                    break;

                case "--test-all-thumbnails":
                    if (i + 1 < args.Length)
                    {
                        options.TestAllThumbnails = args[++i];
                    }
                    break;

                case "--test-text-extract":
                    if (i + 1 < args.Length)
                    {
                        options.TestTextExtract = args[++i];
                    }
                    break;

                case "--test-form-fields":
                    if (i + 1 < args.Length)
                    {
                        options.TestFormFields = args[++i];
                    }
                    break;

                case "--render-all-pages":
                    if (i + 1 < args.Length)
                    {
                        options.RenderAllPages = args[++i];
                    }
                    break;

                case "--test-bookmarks":
                    if (i + 1 < args.Length)
                    {
                        options.TestBookmarks = args[++i];
                    }
                    break;

                case "--test-search":
                    if (i + 1 < args.Length)
                    {
                        options.TestSearch = args[++i];
                    }
                    break;

                case "--search-term":
                    if (i + 1 < args.Length)
                    {
                        options.SearchTerm = args[++i];
                    }
                    break;

                case "--test-page-rotate":
                    if (i + 1 < args.Length)
                    {
                        options.TestPageRotate = args[++i];
                    }
                    break;

                case "--test-page-delete":
                    if (i + 1 < args.Length)
                    {
                        options.TestPageDelete = args[++i];
                    }
                    break;

                case "--test-page-reorder":
                    if (i + 1 < args.Length)
                    {
                        options.TestPageReorder = args[++i];
                    }
                    break;

                case "--test-annotations":
                    if (i + 1 < args.Length)
                    {
                        options.TestAnnotations = args[++i];
                    }
                    break;

                case "--test-metadata":
                    if (i + 1 < args.Length)
                    {
                        options.TestMetadata = args[++i];
                    }
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
  --test-thumbnails <path>      Test thumbnail generation for all pages
                                Returns exit code: 0=success, 1=fail
  --diagnostics                 Output system diagnostics (OS, .NET, memory, PDFium version)
  --render-test <path>          Render all pages to PNG files in output directory
  --output <path>               Output directory for render test (used with --render-test)
  --capture-crash-dump          Capture crash dump on application failure for debugging
  --verify-marshalling          Verify P/Invoke marshalling correctness for PDFium API
                                Returns exit code: 0=success, 1=verification failed
  --marshalling-report          Generate marshalling coverage report
  --output-path <path>          Save report to file (used with --marshalling-report)

Test Commands:
  --list-tests                  List all available CLI tests
  --run-test <name>             Run a specific CLI test by name
                                Returns exit code: 0=pass, 1=fail
  --run-all-tests               Run all CLI tests
                                Returns exit code: 0=all pass, 1=any fail

Rendering Test Commands:
  --test-page-render <path>     Test single page rendering verification
                                Returns exit code: 0=pass, 1=fail
  --test-all-thumbnails <path>  Test thumbnail generation for all pages
                                Returns exit code: 0=pass, 1=fail
  --test-text-extract <path>    Test text extraction verification
                                Returns exit code: 0=pass, 1=fail
  --test-form-fields <path>     Test form field rendering verification
                                Returns exit code: 0=pass, 1=fail
  --render-all-pages <path>     Test batch rendering of all pages
                                Returns exit code: 0=pass, 1=fail

Document Operations Test Commands:
  --test-bookmarks <path>       Test bookmark extraction from PDF
                                Returns exit code: 0=pass, 1=fail
  --test-search <path>          Test PDF text search functionality
  --search-term <term>          Search term to use with --test-search
                                Returns exit code: 0=pass, 1=fail
  --test-page-rotate <path>     Test page rotation operations
                                Returns exit code: 0=pass, 1=fail
  --test-page-delete <path>     Test page deletion operations
                                Returns exit code: 0=pass, 1=fail
  --test-page-reorder <path>    Test page reordering operations
                                Returns exit code: 0=pass, 1=fail
  --test-annotations <path>     Test annotation detection
                                Returns exit code: 0=pass, 1=fail
  --test-metadata <path>        Test metadata extraction
                                Returns exit code: 0=pass, 1=fail

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

  # List available CLI tests
  FluentPDF.App.exe --list-tests

  # Run a specific test
  FluentPDF.App.exe --run-test render-pdf --verbose

  # Run all CLI tests
  FluentPDF.App.exe --run-all-tests --verbose

  # Rendering verification tests
  FluentPDF.App.exe --test-page-render ""test.pdf"" --verbose
  FluentPDF.App.exe --test-all-thumbnails ""test.pdf"" --output ""C:\output""
  FluentPDF.App.exe --test-text-extract ""test.pdf"" --verbose
  FluentPDF.App.exe --test-form-fields ""form.pdf"" --verbose
  FluentPDF.App.exe --render-all-pages ""test.pdf"" --output ""C:\output"" --verbose

  # Document operations tests
  FluentPDF.App.exe --test-bookmarks ""test.pdf"" --verbose
  FluentPDF.App.exe --test-search ""test.pdf"" --search-term ""example"" --verbose
  FluentPDF.App.exe --test-page-rotate ""test.pdf"" --verbose
  FluentPDF.App.exe --test-page-delete ""test.pdf"" --verbose
  FluentPDF.App.exe --test-page-reorder ""test.pdf"" --verbose
  FluentPDF.App.exe --test-annotations ""test.pdf"" --verbose
  FluentPDF.App.exe --test-metadata ""test.pdf"" --verbose
";
    }
}
