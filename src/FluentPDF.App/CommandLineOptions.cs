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
    /// Gets or sets the baseline file path for performance profiling comparison.
    /// Used with --compare-baseline to detect marshaling performance regressions.
    /// Exit code: 0=no regressions, 1=regressions detected.
    /// </summary>
    public string? CompareBaseline { get; set; }

    /// <summary>
    /// Gets or sets whether to enable console logging output.
    /// </summary>
    public bool EnableConsoleLogging { get; set; }

    /// <summary>
    /// Gets or sets whether to enable verbose/debug logging.
    /// </summary>
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// Gets or sets the HTML report output file path.
    /// Used with validation commands to generate HTML-formatted validation reports.
    /// </summary>
    public string? HtmlOutput { get; set; }

    /// <summary>
    /// Gets or sets the JSON report output file path.
    /// Used with validation commands to generate JSON-formatted validation reports.
    /// </summary>
    public string? JsonOutput { get; set; }

    /// <summary>
    /// Gets or sets the JUnit XML report output file path.
    /// Used with validation commands to generate JUnit XML test reports for CI/CD integration.
    /// Compatible with GitHub Actions and Azure DevOps test reporting.
    /// </summary>
    public string? JunitOutput { get; set; }

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
    /// Gets or sets whether to run marshalling performance profiling.
    /// Measures execution time and memory overhead for all PDFium P/Invoke operations.
    /// Exit code: 0=success, 1=profiling failed.
    /// </summary>
    public bool ProfileMarshalling { get; set; }

    /// <summary>
    /// Gets or sets whether to run workaround regression tests.
    /// Tests documented workarounds (float dimension, Task.Yield threading, SoftwareBitmap) to detect breakage.
    /// Exit code: 0=all workarounds still needed, 1=workaround broken or removable.
    /// </summary>
    public bool TestWorkarounds { get; set; }

    /// <summary>
    /// Gets or sets whether to validate all high-risk marshalling areas.
    /// Runs all validators: UTF-16, bitmap, annotation, threading, buffer safety, API signatures, profiling, and workarounds.
    /// Exit code: 0=all validations passed, 1=validation failed, 2=critical error.
    /// </summary>
    public bool ValidateAll { get; set; }

    /// <summary>
    /// Gets or sets whether to validate annotation geometry marshalling.
    /// Validates FS_QUADPOINTSF and FS_RECTF struct marshalling with edge cases.
    /// Exit code: 0=validation passed, 1=validation failed.
    /// </summary>
    public bool ValidateAnnotationMarshalling { get; set; }

    /// <summary>
    /// Gets or sets whether to validate bitmap buffer marshalling and stride calculations.
    /// Validates bitmap buffer marshalling with overflow prevention and edge-case buffer sizes.
    /// Exit code: 0=validation passed, 1=validation failed.
    /// </summary>
    public bool ValidateBitmapMarshalling { get; set; }

    /// <summary>
    /// Gets or sets whether to validate buffer overflow and memory safety.
    /// Analyzes all Marshal.Copy call sites and simulates buffer overflow scenarios.
    /// Exit code: 0=validation passed, 1=validation failed.
    /// </summary>
    public bool ValidateBufferSafety { get; set; }

    /// <summary>
    /// Gets or sets whether to validate threading model (Task.Yield workaround).
    /// Validates that Task.Yield prevents AccessViolation crashes in PDFium operations.
    /// Exit code: 0=validation passed, 1=validation failed.
    /// </summary>
    public bool ValidateThreadingModel { get; set; }

    /// <summary>
    /// Gets or sets whether to validate UTF-16 string marshalling.
    /// Validates UTF-16LE string marshalling for bookmarks, text search, and form fields.
    /// Exit code: 0=validation passed, 1=validation failed.
    /// </summary>
    public bool ValidateUtf16Marshalling { get; set; }

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
    /// Gets or sets whether to start the verification API server.
    /// When set, application will start a REST API server for autonomous verification.
    /// </summary>
    public bool ApiServer { get; set; }

    /// <summary>
    /// Gets or sets the port for the API server (default: 5000).
    /// Used with --api-server option.
    /// </summary>
    public int ApiPort { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to run in headless mode (no UI).
    /// Used with --api-server option for CI/CD environments.
    /// </summary>
    public bool Headless { get; set; }

    /// <summary>
    /// Gets or sets the bind address for the API server (default: localhost).
    /// Used with --api-server option.
    /// </summary>
    public string ApiBindAddress { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the file path for image export test.
    /// When set, application will export all pages as images.
    /// </summary>
    public string? TestExportImages { get; set; }

    /// <summary>
    /// Gets or sets the image export format (png, jpeg, bmp). Default: png.
    /// </summary>
    public string? ExportImageFormat { get; set; }

    /// <summary>
    /// Gets or sets the DPI for exported images. Default: 300.
    /// </summary>
    public int ExportImageDpi { get; set; } = 300;

    /// <summary>
    /// Gets or sets the JPEG quality (1-100) when exporting as JPEG. Default: 90.
    /// </summary>
    public int ExportImageQuality { get; set; } = 90;

    /// <summary>
    /// Gets or sets the page range to export (e.g., "1-5", "all"). Default: "all".
    /// </summary>
    public string? ExportImagePageRange { get; set; }

    /// <summary>
    /// Gets or sets the file path for PDF merge test.
    /// </summary>
    public string? TestMerge { get; set; }

    /// <summary>
    /// Gets or sets the file path for PDF split test.
    /// </summary>
    public string? TestSplit { get; set; }

    /// <summary>
    /// Gets or sets the page ranges for split operation (e.g., "1-5,10-15").
    /// </summary>
    public string? SplitRanges { get; set; }

    /// <summary>
    /// Gets or sets the file path for PDF optimization test.
    /// </summary>
    public string? TestOptimize { get; set; }

    /// <summary>
    /// Gets or sets the minimum reduction percentage for optimization.
    /// </summary>
    public int MinReduction { get; set; }

    /// <summary>
    /// Gets or sets whether to verify visual quality after optimization.
    /// </summary>
    public bool VerifyVisual { get; set; }

    /// <summary>
    /// Gets or sets whether to verify PDF structure integrity.
    /// </summary>
    public bool VerifyStructure { get; set; }

    /// <summary>
    /// Gets or sets whether to verify page count and content.
    /// </summary>
    public bool VerifyPages { get; set; }

    /// <summary>
    /// Gets or sets the file path for watermark test.
    /// </summary>
    public string? TestWatermark { get; set; }

    /// <summary>
    /// Gets or sets the watermark text content.
    /// </summary>
    public string? WatermarkText { get; set; }

    /// <summary>
    /// Gets or sets the watermark image path.
    /// </summary>
    public string? WatermarkImage { get; set; }

    /// <summary>
    /// Gets or sets the watermark position (e.g., "center", "top-left").
    /// </summary>
    public string? WatermarkPosition { get; set; }

    /// <summary>
    /// Gets or sets the watermark opacity (0.0 to 1.0).
    /// </summary>
    public double WatermarkOpacity { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the file path for encryption test.
    /// </summary>
    public string? TestEncrypt { get; set; }

    /// <summary>
    /// Gets or sets the user password for encryption.
    /// </summary>
    public string? EncryptUserPassword { get; set; }

    /// <summary>
    /// Gets or sets the owner password for encryption.
    /// </summary>
    public string? EncryptOwnerPassword { get; set; }

    /// <summary>
    /// Gets or sets the encryption strength (128 or 256).
    /// </summary>
    public int EncryptionStrength { get; set; } = 256;

    /// <summary>
    /// Gets or sets whether to allow printing in encrypted PDF.
    /// </summary>
    public bool AllowPrint { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow copying in encrypted PDF.
    /// </summary>
    public bool AllowCopy { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow modification in encrypted PDF.
    /// </summary>
    public bool AllowModify { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow annotations in encrypted PDF.
    /// </summary>
    public bool AllowAnnotate { get; set; } = true;

    /// <summary>
    /// Gets or sets the file path for annotations test.
    /// </summary>
    public string? TestAnnotationsCmd { get; set; }

    /// <summary>
    /// Gets or sets the JSON file path containing annotations data.
    /// </summary>
    public string? AnnotationsJsonPath { get; set; }

    /// <summary>
    /// Gets or sets whether to verify annotation persistence.
    /// </summary>
    public bool VerifyPersistence { get; set; }

    /// <summary>
    /// Gets or sets the file path for form fields test.
    /// </summary>
    public string? TestFormsCmd { get; set; }

    /// <summary>
    /// Gets or sets the JSON file path containing form data.
    /// </summary>
    public string? FormDataJsonPath { get; set; }

    /// <summary>
    /// Gets or sets whether to verify form validation.
    /// </summary>
    public bool VerifyValidation { get; set; }

    /// <summary>
    /// Gets or sets the file path for stamp test.
    /// </summary>
    public string? TestStamp { get; set; }

    /// <summary>
    /// Gets or sets the stamp type (e.g., "Approved", "Rejected").
    /// </summary>
    public string? StampType { get; set; }

    /// <summary>
    /// Gets or sets the file path for document conversion test.
    /// </summary>
    public string? TestConversion { get; set; }

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

                case "--compare-baseline":
                    if (i + 1 < args.Length)
                    {
                        options.CompareBaseline = args[++i];
                    }
                    break;

                case "--html-output":
                    if (i + 1 < args.Length)
                    {
                        options.HtmlOutput = args[++i];
                    }
                    break;

                case "--json-output":
                    if (i + 1 < args.Length)
                    {
                        options.JsonOutput = args[++i];
                    }
                    break;

                case "--junit-output":
                    if (i + 1 < args.Length)
                    {
                        options.JunitOutput = args[++i];
                    }
                    break;

                case "--profile-marshalling":
                    options.ProfileMarshalling = true;
                    break;

                case "--test-thumbnails":
                    if (i + 1 < args.Length)
                    {
                        options.TestThumbnails = args[++i];
                    }
                    break;

                case "--test-workarounds":
                    options.TestWorkarounds = true;
                    break;

                case "--validate-all":
                    options.ValidateAll = true;
                    break;

                case "--validate-annotation-marshalling":
                    options.ValidateAnnotationMarshalling = true;
                    break;

                case "--validate-bitmap-marshalling":
                    options.ValidateBitmapMarshalling = true;
                    break;

                case "--validate-buffer-safety":
                    options.ValidateBufferSafety = true;
                    break;

                case "--validate-threading-model":
                    options.ValidateThreadingModel = true;
                    break;

                case "--validate-utf16-marshalling":
                    options.ValidateUtf16Marshalling = true;
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

                case "--test-merge":
                    if (i + 1 < args.Length)
                    {
                        options.TestMerge = args[++i];
                    }
                    break;

                case "--test-split":
                    if (i + 1 < args.Length)
                    {
                        options.TestSplit = args[++i];
                    }
                    break;

                case "--split-ranges":
                    if (i + 1 < args.Length)
                    {
                        options.SplitRanges = args[++i];
                    }
                    break;

                case "--test-forms":
                    if (i + 1 < args.Length)
                    {
                        options.TestFormsCmd = args[++i];
                    }
                    break;

                case "--test-annotations-cmd":
                    if (i + 1 < args.Length)
                    {
                        options.TestAnnotationsCmd = args[++i];
                    }
                    break;

                case "--test-watermark":
                    if (i + 1 < args.Length)
                    {
                        options.TestWatermark = args[++i];
                    }
                    break;

                case "--watermark-text":
                    if (i + 1 < args.Length)
                    {
                        options.WatermarkText = args[++i];
                    }
                    break;

                case "--watermark-opacity":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out var wOpacity))
                    {
                        options.WatermarkOpacity = wOpacity;
                    }
                    break;

                case "--api-server":
                    options.ApiServer = true;
                    break;

                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var port))
                    {
                        options.ApiPort = port;
                    }
                    break;

                case "--headless":
                    options.Headless = true;
                    break;

                case "--bind-address":
                    if (i + 1 < args.Length)
                    {
                        options.ApiBindAddress = args[++i];
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

Marshaling Validation Commands:
  --validate-all                Run all marshaling validators (UTF-16, bitmap, annotation, threading, buffer safety)
                                Returns exit code: 0=all passed, 1=failed, 2=critical error
  --validate-utf16-marshalling  Validate UTF-16LE string marshaling for bookmarks, text search, form fields
                                Returns exit code: 0=passed, 1=failed
  --validate-bitmap-marshalling Validate bitmap buffer marshaling with stride calculations and overflow prevention
                                Returns exit code: 0=passed, 1=failed
  --validate-annotation-marshalling
                                Validate annotation geometry marshaling (FS_QUADPOINTSF, FS_RECTF structs)
                                Returns exit code: 0=passed, 1=failed
  --validate-threading-model    Validate Task.Yield threading workaround prevents AccessViolation crashes
                                Returns exit code: 0=passed, 1=failed
  --validate-buffer-safety      Validate buffer overflow prevention and analyze Marshal.Copy call sites
                                Returns exit code: 0=passed, 1=failed
  --profile-marshalling         Profile marshaling performance (execution time, memory allocations)
                                Returns exit code: 0=success, 1=profiling failed
  --test-workarounds            Test documented workarounds (float dimension, threading, SoftwareBitmap)
                                Returns exit code: 0=workarounds still needed, 1=broken or removable
  --compare-baseline <path>     Compare profiling results against baseline to detect regressions
                                Returns exit code: 0=no regressions, 1=regressions detected

Report Output Options:
  --json-output <path>          Export validation report as JSON file
  --junit-output <path>         Export validation report as JUnit XML (GitHub Actions/Azure DevOps compatible)
  --html-output <path>          Export validation report as HTML file with color-coded severity levels

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

Document Editing Test Commands:
  --test-merge <path>           Test PDF merge functionality (merges 2-3 test PDFs)
                                Returns exit code: 0=success, 1=failure
  --test-split <path>           Test PDF split by page ranges
  --split-ranges <ranges>       Page ranges for split (e.g., ""1-5,10-15"")
                                Returns exit code: 0=success, 1=failure
  --test-forms <path>           Test form field filling (text fields, checkboxes, radio buttons)
                                Verifies persistence after save/reload
                                Returns exit code: 0=success, 1=failure
  --test-annotations-cmd <path> Test annotation creation (highlight, underline, shapes, notes)
                                Saves to FDF and verifies export
                                Returns exit code: 0=success, 1=failure
  --test-watermark <path>       Test text watermark application
  --watermark-text <text>       Watermark text to apply (default: ""CONFIDENTIAL"")
  --watermark-opacity <value>   Watermark opacity 0.0-1.0 (default: 0.5)
                                Returns exit code: 0=success, 1=failure

Verification API Server:
  --api-server                  Start the verification REST API server
  --port <port>                 Port for API server (default: 5000)
  --headless                    Run in headless mode (no UI window)
  --bind-address <addr>         Bind address for API server (default: localhost)

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

  # Marshaling validation tests
  FluentPDF.App.exe --validate-all --junit-output ""results.xml"" --json-output ""report.json""
  FluentPDF.App.exe --validate-utf16-marshalling --verbose
  FluentPDF.App.exe --validate-bitmap-marshalling --html-output ""bitmap-report.html""
  FluentPDF.App.exe --profile-marshalling --compare-baseline ""baseline.json"" --json-output ""profile.json""
  FluentPDF.App.exe --test-workarounds --verbose

  # Document editing tests (Feature F2.1.1-F5.2.5)
  FluentPDF.App.exe --test-merge ""test.pdf"" --verbose
  FluentPDF.App.exe --test-split ""multi-page.pdf"" --split-ranges ""1-5,10-15"" --output ""C:/output""
  FluentPDF.App.exe --test-forms ""form.pdf"" --verbose
  FluentPDF.App.exe --test-annotations-cmd ""sample.pdf"" --verbose
  FluentPDF.App.exe --test-watermark ""doc.pdf"" --watermark-text ""DRAFT"" --watermark-opacity 0.3

  # Start verification API server
  FluentPDF.App.exe --api-server
  FluentPDF.App.exe --api-server --port 8080 --headless
  FluentPDF.App.exe --api-server --bind-address ""0.0.0.0"" --port 5000
";
    }
}
