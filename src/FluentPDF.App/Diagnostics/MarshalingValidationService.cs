// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FluentPDF.App.Diagnostics;

/// <summary>
/// Service for orchestrating marshaling validation operations from CLI commands.
/// Encapsulates validation execution, report generation, and exit code determination.
/// </summary>
public sealed class MarshalingValidationService
{
    private readonly ILogger<MarshalingValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarshalingValidationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public MarshalingValidationService(ILogger<MarshalingValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates UTF-16 string marshaling for bookmarks, text search, and form fields.
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=passed, 1=failed, 2=critical error.</returns>
    public async Task<int> ValidateUtf16MarshalingAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF UTF-16 Marshaling Validation");
            Console.WriteLine("======================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var validationReports = await verifier.ValidateHighRiskAreasAsync(null, cancellationToken);

            // Find UTF-16 validation report
            var utf16Report = validationReports.FirstOrDefault(r => r.ValidatorName.Contains("Utf16", StringComparison.OrdinalIgnoreCase));
            if (utf16Report == null)
            {
                Console.WriteLine("ERROR: UTF-16 validator not found in validation reports.");
                return 2;
            }

            // Display summary
            PrintValidationSummary(utf16Report);

            // Export reports if requested
            await ExportReportsAsync(new List<ValidationReport> { utf16Report }, options, cancellationToken);

            // Determine exit code
            return utf16Report.HasCriticalFailures ? 2 : (utf16Report.AllPassed ? 0 : 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UTF-16 marshaling validation failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Validates bitmap buffer marshaling with stride calculations and overflow prevention.
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=passed, 1=failed, 2=critical error.</returns>
    public async Task<int> ValidateBitmapMarshalingAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Bitmap Marshaling Validation");
            Console.WriteLine("=======================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var validationReports = await verifier.ValidateHighRiskAreasAsync(null, cancellationToken);

            var bitmapReport = validationReports.FirstOrDefault(r => r.ValidatorName.Contains("Bitmap", StringComparison.OrdinalIgnoreCase));
            if (bitmapReport == null)
            {
                Console.WriteLine("ERROR: Bitmap validator not found in validation reports.");
                return 2;
            }

            PrintValidationSummary(bitmapReport);
            await ExportReportsAsync(new List<ValidationReport> { bitmapReport }, options, cancellationToken);

            return bitmapReport.HasCriticalFailures ? 2 : (bitmapReport.AllPassed ? 0 : 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bitmap marshaling validation failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Validates annotation geometry marshaling (FS_QUADPOINTSF, FS_RECTF structs).
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=passed, 1=failed, 2=critical error.</returns>
    public async Task<int> ValidateAnnotationMarshalingAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Annotation Marshaling Validation");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var validationReports = await verifier.ValidateHighRiskAreasAsync(null, cancellationToken);

            var annotationReport = validationReports.FirstOrDefault(r => r.ValidatorName.Contains("Annotation", StringComparison.OrdinalIgnoreCase));
            if (annotationReport == null)
            {
                Console.WriteLine("ERROR: Annotation validator not found in validation reports.");
                return 2;
            }

            PrintValidationSummary(annotationReport);
            await ExportReportsAsync(new List<ValidationReport> { annotationReport }, options, cancellationToken);

            return annotationReport.HasCriticalFailures ? 2 : (annotationReport.AllPassed ? 0 : 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Annotation marshaling validation failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Validates Task.Yield threading workaround prevents AccessViolation crashes.
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=passed, 1=failed, 2=critical error.</returns>
    public async Task<int> ValidateThreadingModelAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Threading Model Validation");
            Console.WriteLine("====================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var validationReports = await verifier.ValidateHighRiskAreasAsync(null, cancellationToken);

            var threadingReport = validationReports.FirstOrDefault(r => r.ValidatorName.Contains("Threading", StringComparison.OrdinalIgnoreCase));
            if (threadingReport == null)
            {
                Console.WriteLine("ERROR: Threading validator not found in validation reports.");
                return 2;
            }

            PrintValidationSummary(threadingReport);
            await ExportReportsAsync(new List<ValidationReport> { threadingReport }, options, cancellationToken);

            return threadingReport.HasCriticalFailures ? 2 : (threadingReport.AllPassed ? 0 : 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Threading model validation failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Validates buffer overflow prevention and analyzes Marshal.Copy call sites.
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=passed, 1=failed, 2=critical error.</returns>
    public async Task<int> ValidateBufferSafetyAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Buffer Safety Validation");
            Console.WriteLine("===================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var validationReports = await verifier.ValidateHighRiskAreasAsync(null, cancellationToken);

            var bufferReport = validationReports.FirstOrDefault(r => r.ValidatorName.Contains("Buffer", StringComparison.OrdinalIgnoreCase));
            if (bufferReport == null)
            {
                Console.WriteLine("ERROR: Buffer safety validator not found in validation reports.");
                return 2;
            }

            PrintValidationSummary(bufferReport);
            await ExportReportsAsync(new List<ValidationReport> { bufferReport }, options, cancellationToken);

            return bufferReport.HasCriticalFailures ? 2 : (bufferReport.AllPassed ? 0 : 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Buffer safety validation failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Validates all high-risk marshaling areas (UTF-16, bitmap, annotation, threading, buffer safety).
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=all passed, 1=validation failed, 2=critical error.</returns>
    public async Task<int> ValidateAllAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Comprehensive Marshaling Validation");
            Console.WriteLine("==============================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));

            // Run comprehensive validation
            var comprehensiveReport = await verifier.VerifyComprehensiveAsync(
                new Dictionary<string, SignatureDetails>(), // Empty for now, can be extended
                testPdfPath: null,
                includePerformanceProfiling: false, // Don't include profiling in --validate-all
                profilingIterations: 1000,
                cancellationToken);

            // Display comprehensive summary
            Console.WriteLine("Validation Summary:");
            Console.WriteLine("------------------");
            Console.WriteLine($"Total Validators: {comprehensiveReport.Summary.TotalValidators}");
            Console.WriteLine($"  Passed: {comprehensiveReport.Summary.PassedValidators}");
            Console.WriteLine($"  Failed: {comprehensiveReport.Summary.FailedValidators}");
            Console.WriteLine($"  Critical: {comprehensiveReport.Summary.CriticalValidators}");
            Console.WriteLine();

            Console.WriteLine($"Workarounds Tested: {comprehensiveReport.Summary.TotalWorkarounds}");
            Console.WriteLine($"  Still Needed: {comprehensiveReport.Summary.WorkaroundsStillNeeded}");
            Console.WriteLine($"  Can Be Removed: {comprehensiveReport.Summary.WorkaroundsCanBeRemoved}");
            Console.WriteLine($"  Broken: {comprehensiveReport.Summary.WorkaroundsBroken}");
            Console.WriteLine();

            // Display detailed results for each validator
            foreach (var validationReport in comprehensiveReport.ValidationReports)
            {
                PrintValidationSummary(validationReport);
            }

            // Export reports if requested
            await ExportComprehensiveReportAsync(comprehensiveReport, options, cancellationToken);

            // Determine exit code
            var status = comprehensiveReport.OverallStatus;
            return status switch
            {
                ValidationStatus.Critical => 2,
                ValidationStatus.Failed => 1,
                ValidationStatus.Warning => 0, // Warnings don't fail the build
                ValidationStatus.Passed => 0,
                _ => 1
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Comprehensive marshaling validation failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Profiles marshaling performance (execution time, memory allocations).
    /// </summary>
    /// <param name="options">Command-line options including baseline path and output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=success, 1=regressions detected, 2=profiling failed.</returns>
    public async Task<int> ProfileMarshalingAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Marshaling Performance Profiling");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var profilingReport = await verifier.ProfilePerformanceAsync(
                testPdfPath: null,
                iterations: 1000,
                cancellationToken);

            // Display profiling summary
            Console.WriteLine($"Profiling completed at: {profilingReport.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Total functions profiled: {profilingReport.ResultsByFunction.Count}");
            Console.WriteLine();

            // Compare against baseline if provided
            if (!string.IsNullOrEmpty(options.CompareBaseline))
            {
                // TODO: Implement baseline comparison logic
                Console.WriteLine($"Baseline comparison against: {options.CompareBaseline}");
                Console.WriteLine("NOTE: Baseline comparison not yet implemented.");
            }

            // Export reports if requested
            await ExportProfilingReportAsync(profilingReport, options, cancellationToken);

            // Determine exit code based on regressions
            var hasRegressions = profilingReport.RegressedFunctions != null && profilingReport.RegressedFunctions.Count > 0;
            return hasRegressions ? 1 : 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Marshaling performance profiling failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Tests documented workarounds (float dimension, threading, SoftwareBitmap).
    /// </summary>
    /// <param name="options">Command-line options including output paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0=all workarounds still needed, 1=workaround broken or removable, 2=critical error.</returns>
    public async Task<int> TestWorkaroundsAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("FluentPDF Workaround Regression Tests");
            Console.WriteLine("======================================");
            Console.WriteLine();

            using var verifier = new MarshallingVerifier(typeof(PdfiumInterop));
            var workaroundResults = await verifier.TestWorkaroundsAsync(null, cancellationToken);

            // Display workaround test results
            Console.WriteLine($"Total workarounds tested: {workaroundResults.Count}");
            Console.WriteLine();

            foreach (var result in workaroundResults)
            {
                Console.WriteLine($"Workaround: {result.WorkaroundName}");
                Console.WriteLine($"  Status: {result.Status}");
                Console.WriteLine($"  Details: {result.Details}");
                Console.WriteLine($"  Recommended Action: {result.RecommendedAction}");
                Console.WriteLine($"  Documentation: {result.DocumentationReference}");
                Console.WriteLine();
            }

            // Export workaround results if requested
            await ExportWorkaroundResultsAsync(workaroundResults, options, cancellationToken);

            // Determine exit code
            var anyBroken = workaroundResults.Any(w => w.Status == WorkaroundStatus.Broken);
            var anyRemovable = workaroundResults.Any(w => w.Status == WorkaroundStatus.CanBeRemoved);

            return anyBroken || anyRemovable ? 1 : 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Workaround regression tests failed with exception");
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Prints a validation report summary to the console.
    /// </summary>
    private void PrintValidationSummary(ValidationReport report)
    {
        Console.WriteLine($"Validator: {report.ValidatorName}");
        Console.WriteLine($"  Total Tests: {report.Summary.TotalTests}");
        Console.WriteLine($"  Passed: {report.Summary.PassedCount}");
        Console.WriteLine($"  Failed: {report.Summary.FailedCount}");
        Console.WriteLine($"  Warnings: {report.Summary.WarningCount}");
        Console.WriteLine($"  Critical: {report.Summary.CriticalCount}");
        Console.WriteLine($"  All Passed: {(report.AllPassed ? "Yes" : "No")}");
        Console.WriteLine($"  Has Critical Failures: {(report.HasCriticalFailures ? "Yes" : "No")}");
        Console.WriteLine();
    }

    /// <summary>
    /// Exports validation reports to requested formats (JSON, JUnit XML, HTML).
    /// </summary>
    private async Task ExportReportsAsync(
        List<ValidationReport> reports,
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        // Export to JSON if requested
        if (!string.IsNullOrEmpty(options.JsonOutput))
        {
            var jsonExporter = new JsonReportExporter();
            foreach (var report in reports)
            {
                await jsonExporter.ExportAsync(report, options.JsonOutput, cancellationToken);
            }
            Console.WriteLine($"JSON report saved to: {options.JsonOutput}");
        }

        // Export to JUnit XML if requested
        if (!string.IsNullOrEmpty(options.JunitOutput))
        {
            var junitExporter = new JUnitXmlExporter();
            foreach (var report in reports)
            {
                await junitExporter.ExportAsync(report, options.JunitOutput, cancellationToken);
            }
            Console.WriteLine($"JUnit XML report saved to: {options.JunitOutput}");
        }

        // Export to HTML if requested
        if (!string.IsNullOrEmpty(options.HtmlOutput))
        {
            var htmlExporter = new HtmlReportGenerator();
            foreach (var report in reports)
            {
                await htmlExporter.ExportAsync(report, options.HtmlOutput, cancellationToken);
            }
            Console.WriteLine($"HTML report saved to: {options.HtmlOutput}");
        }
    }

    /// <summary>
    /// Exports comprehensive validation report to requested formats.
    /// </summary>
    private async Task ExportComprehensiveReportAsync(
        ComprehensiveValidationReport report,
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        // For comprehensive reports, export each validation report individually
        await ExportReportsAsync(report.ValidationReports, options, cancellationToken);

        // Also export workaround results
        await ExportWorkaroundResultsAsync(report.WorkaroundTestResults, options, cancellationToken);
    }

    /// <summary>
    /// Exports profiling report to requested formats.
    /// </summary>
    private Task ExportProfilingReportAsync(
        ProfilingReport report,
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        // Export to JSON if requested
        if (!string.IsNullOrEmpty(options.JsonOutput))
        {
            // Note: JsonReportExporter currently supports ValidationReport
            // For ProfilingReport, we'll need to serialize manually or extend the exporter
            Console.WriteLine($"NOTE: Profiling JSON export not yet implemented for ProfilingReport.");
        }

        // Export to HTML if requested
        if (!string.IsNullOrEmpty(options.HtmlOutput))
        {
            // Note: HtmlReportGenerator currently supports ValidationReport
            // For ProfilingReport, we'll need to extend the generator
            Console.WriteLine($"NOTE: Profiling HTML export not yet implemented for ProfilingReport.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Exports workaround results to requested formats.
    /// </summary>
    private Task ExportWorkaroundResultsAsync(
        List<WorkaroundTestResult> results,
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        // Export to JSON if requested
        if (!string.IsNullOrEmpty(options.JsonOutput))
        {
            // Note: JsonReportExporter currently supports ValidationReport
            // For WorkaroundTestResult, we'll need to serialize manually or extend the exporter
            Console.WriteLine($"NOTE: Workaround results JSON export not yet implemented.");
        }

        return Task.CompletedTask;
    }
}
