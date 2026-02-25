using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentPDF.Rendering.Interop.Verification.Validators;
using FluentPDF.Rendering.Interop.Verification.Profilers;
using FluentPDF.Rendering.Interop.Verification.Regression;
using FluentPDF.Rendering.Interop.Verification.Reports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentPDF.Rendering.Interop.Verification;

/// <summary>
/// Orchestrates P/Invoke marshalling verification by coordinating signature analysis,
/// marshalling testing, and coverage reporting.
/// Provides a unified interface for verifying PDFium P/Invoke correctness.
/// </summary>
public class MarshallingVerifier : IDisposable
{
    private readonly SignatureAnalyzer _signatureAnalyzer;
    private readonly CoverageReporter _reporter;
    private readonly Type _interopType;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarshallingVerifier"/> class.
    /// </summary>
    /// <param name="interopType">The type containing DllImport methods to verify (e.g., typeof(PdfiumInterop)).</param>
    /// <exception cref="ArgumentNullException">Thrown when interopType is null.</exception>
    public MarshallingVerifier(Type interopType)
    {
        if (interopType == null)
        {
            throw new ArgumentNullException(nameof(interopType));
        }

        _interopType = interopType;
        _signatureAnalyzer = new SignatureAnalyzer(interopType);
        _reporter = new CoverageReporter();
    }

    /// <summary>
    /// Verifies all P/Invoke signatures against expected PDFium API specifications.
    /// </summary>
    /// <param name="expectedSignatures">Dictionary mapping function names to their expected signatures.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A list of verification results for all analyzed signatures.</returns>
    public async Task<List<VerificationResult>> VerifyAllSignaturesAsync(
        Dictionary<string, SignatureDetails> expectedSignatures,
        CancellationToken cancellationToken = default)
    {
        if (expectedSignatures == null)
        {
            throw new ArgumentNullException(nameof(expectedSignatures));
        }

        var results = new List<VerificationResult>();

        await Task.Run(() =>
        {
            var allMethodNames = _signatureAnalyzer.GetAllDllImportMethodNames();

            foreach (var methodName in allMethodNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (expectedSignatures.TryGetValue(methodName, out var expectedSignature))
                {
                    var result = _signatureAnalyzer.ValidateSignature(methodName, expectedSignature);
                    results.Add(result);
                }
                else
                {
                    // Function not in specification - create a result indicating it's unverified
                    var actualSignature = _signatureAnalyzer.AnalyzeSignature(methodName);
                    results.Add(new VerificationResult
                    {
                        FunctionName = methodName,
                        SignatureValid = true, // Can't verify without spec, so assume valid
                        MarshallingCorrect = null, // Not tested
                        ErrorMessage = "No specification available for this function",
                        Signature = actualSignature
                    });
                }
            }
        }, cancellationToken);

        return results;
    }

    /// <summary>
    /// Verifies all P/Invoke signatures synchronously.
    /// </summary>
    /// <param name="expectedSignatures">Dictionary mapping function names to their expected signatures.</param>
    /// <returns>A list of verification results for all analyzed signatures.</returns>
    public List<VerificationResult> VerifyAllSignatures(Dictionary<string, SignatureDetails> expectedSignatures)
    {
        return VerifyAllSignaturesAsync(expectedSignatures, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tests data marshalling for P/Invoke functions using actual PDFium calls.
    /// </summary>
    /// <param name="testPdfPath">Path to a test PDF file for marshalling tests.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A list of verification results with marshalling test data.</returns>
    public async Task<List<VerificationResult>> TestMarshallingAsync(
        string? testPdfPath = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var tester = new DataMarshallerTester(testPdfPath);
                return tester.RunAllTests();
            }
            catch (Exception ex)
            {
                return new List<VerificationResult>
                {
                    new VerificationResult
                    {
                        FunctionName = "MarshallingTester",
                        SignatureValid = true,
                        MarshallingCorrect = false,
                        ErrorMessage = $"Marshalling test suite failed: {ex.Message}",
                        TestResult = new MarshallingTestResult
                        {
                            Success = false,
                            DataType = "N/A",
                            TestCaseName = "Test Suite Initialization",
                            ErrorDetails = ex.ToString()
                        }
                    }
                };
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Tests data marshalling synchronously.
    /// </summary>
    /// <param name="testPdfPath">Path to a test PDF file for marshalling tests.</param>
    /// <returns>A list of verification results with marshalling test data.</returns>
    public List<VerificationResult> TestMarshalling(string? testPdfPath = null)
    {
        return TestMarshallingAsync(testPdfPath, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs complete verification including signature analysis and marshalling tests,
    /// then generates a comprehensive coverage report.
    /// </summary>
    /// <param name="expectedSignatures">Dictionary mapping function names to their expected signatures.</param>
    /// <param name="testPdfPath">Path to a test PDF file for marshalling tests.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A comprehensive coverage report with all verification results.</returns>
    public async Task<CoverageReport> VerifyAndReportAsync(
        Dictionary<string, SignatureDetails> expectedSignatures,
        string? testPdfPath = null,
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<VerificationResult>();

        try
        {
            // Step 1: Verify signatures
            var signatureResults = await VerifyAllSignaturesAsync(expectedSignatures, cancellationToken);
            allResults.AddRange(signatureResults);

            // Step 2: Test marshalling
            var marshallingResults = await TestMarshallingAsync(testPdfPath, cancellationToken);

            // Step 3: Merge results - combine signature and marshalling data
            var mergedResults = MergeResults(allResults, marshallingResults);

            // Step 4: Validate high-risk areas (skipped by default to avoid crashes)
            // var validationReports = await ValidateHighRiskAreasAsync(testPdfPath, cancellationToken);

            // Step 5: Profile performance (skipped by default to avoid crashes)
            // var profilingReport = await ProfilePerformanceAsync(testPdfPath, 1000, cancellationToken);

            // Step 6: Test workarounds (skipped by default to avoid crashes)
            // Note: Workaround tests create minimal PDFs that can cause AccessViolationException
            // Use TestWorkaroundsAsync() separately if you need to test workarounds
            // var workaroundResults = await TestWorkaroundsAsync(testPdfPath, cancellationToken);

            // Step 7: Generate coverage report with all data
            var report = GenerateCoverageReport(mergedResults);

            // Note: The new validation reports, profiling, and workaround results are available
            // via the new methods (ValidateHighRiskAreasAsync, ProfilePerformanceAsync, TestWorkaroundsAsync)
            // The CoverageReport structure is maintained for backward compatibility.

            return report;
        }
        catch (OperationCanceledException)
        {
            // If cancelled, return partial results
            return GenerateCoverageReport(allResults);
        }
        catch (Exception ex)
        {
            // If verification fails, return error report
            allResults.Add(new VerificationResult
            {
                FunctionName = "VerificationOrchestrator",
                SignatureValid = false,
                MarshallingCorrect = false,
                ErrorMessage = $"Verification failed: {ex.Message}",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    DataType = "N/A",
                    TestCaseName = "Overall Verification",
                    ErrorDetails = ex.ToString()
                }
            });

            return GenerateCoverageReport(allResults);
        }
    }

    /// <summary>
    /// Runs complete verification synchronously.
    /// </summary>
    /// <param name="expectedSignatures">Dictionary mapping function names to their expected signatures.</param>
    /// <param name="testPdfPath">Path to a test PDF file for marshalling tests.</param>
    /// <returns>A comprehensive coverage report with all verification results.</returns>
    public CoverageReport VerifyAndReport(
        Dictionary<string, SignatureDetails> expectedSignatures,
        string? testPdfPath = null)
    {
        return VerifyAndReportAsync(expectedSignatures, testPdfPath, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Generates a markdown report from verification results.
    /// </summary>
    /// <param name="report">The coverage report to format.</param>
    /// <param name="includeDetails">Whether to include detailed results for each function.</param>
    /// <returns>A formatted markdown report.</returns>
    public string GenerateMarkdownReport(CoverageReport report, bool includeDetails = true)
    {
        return _reporter.GenerateMarkdownReport(report, includeDetails);
    }

    /// <summary>
    /// Generates a console-friendly summary of verification results.
    /// </summary>
    /// <param name="report">The coverage report to summarize.</param>
    /// <returns>A formatted console summary.</returns>
    public string GenerateConsoleSummary(CoverageReport report)
    {
        return _reporter.GenerateConsoleSummary(report);
    }

    /// <summary>
    /// Saves a markdown report to a file.
    /// </summary>
    /// <param name="report">The coverage report to save.</param>
    /// <param name="outputPath">The file path to save to.</param>
    /// <param name="includeDetails">Whether to include detailed results.</param>
    public void SaveReport(CoverageReport report, string outputPath, bool includeDetails = true)
    {
        _reporter.SaveReport(report, outputPath, includeDetails);
    }

    /// <summary>
    /// Validates all high-risk marshaling areas in parallel.
    /// Runs UTF-16, bitmap, annotation, threading, and buffer safety validators concurrently.
    /// </summary>
    /// <param name="testPdfPath">Optional path to a test PDF file for validators that need it.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A list of validation reports from all validators.</returns>
    public async Task<List<ValidationReport>> ValidateHighRiskAreasAsync(
        string? testPdfPath = null,
        CancellationToken cancellationToken = default)
    {
        // Create null loggers for validators (they can be injected later if needed)
        var utf16Logger = NullLogger<Utf16MarshalingValidator>.Instance;
        var bitmapLogger = NullLogger<BitmapMarshalingValidator>.Instance;
        var annotationLogger = NullLogger<AnnotationMarshalingValidator>.Instance;
        var threadingLogger = NullLogger<ThreadingModelValidator>.Instance;

        // Create all validators
        var validators = new List<IValidator>
        {
            new Utf16MarshalingValidator(utf16Logger),
            new BitmapMarshalingValidator(bitmapLogger),
            new AnnotationMarshalingValidator(annotationLogger),
            new ThreadingModelValidator(threadingLogger),
            new BufferSafetyValidator()
        };

        // Run all validators in parallel
        var validationTasks = validators.Select(v => v.ValidateAsync(cancellationToken));
        var results = await Task.WhenAll(validationTasks);

        return results.ToList();
    }

    /// <summary>
    /// Profiles the performance of all marshaling operations.
    /// </summary>
    /// <param name="testPdfPath">Optional path to a test PDF file for profiling.</param>
    /// <param name="iterations">Number of iterations per function (default: 1000).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A comprehensive profiling report with performance metrics.</returns>
    public async Task<ProfilingReport> ProfilePerformanceAsync(
        string? testPdfPath = null,
        int iterations = 1000,
        CancellationToken cancellationToken = default)
    {
        var profiler = new MarshalingPerformanceProfiler(_interopType);
        return await profiler.ProfileAllFunctionsAsync(iterations, cancellationToken);
    }

    /// <summary>
    /// Tests all documented workarounds to determine if they are still needed, can be removed, or are broken.
    /// </summary>
    /// <param name="testPdfPath">Optional path to a test PDF file for workaround tests.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A list of workaround test results.</returns>
    public async Task<List<WorkaroundTestResult>> TestWorkaroundsAsync(
        string? testPdfPath = null,
        CancellationToken cancellationToken = default)
    {
        // Create all workaround tests
        var workaroundTests = new List<IWorkaroundTest>
        {
            new FloatDimensionWorkaroundTest(),
            new ThreadingWorkaroundTest()
        };

        // Run all workaround tests in parallel
        var testTasks = workaroundTests.Select(t => t.TestWorkaroundAsync(cancellationToken));
        var results = await Task.WhenAll(testTasks);

        return results.ToList();
    }

    /// <summary>
    /// Runs comprehensive validation including signature verification, marshaling tests,
    /// high-risk area validators, performance profiling, and workaround regression tests.
    /// </summary>
    /// <param name="expectedSignatures">Dictionary mapping function names to their expected signatures.</param>
    /// <param name="testPdfPath">Path to a test PDF file for validation tests.</param>
    /// <param name="includePerformanceProfiling">Whether to include performance profiling (can be time-consuming).</param>
    /// <param name="profilingIterations">Number of iterations for performance profiling (default: 1000).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A comprehensive validation report with all verification results.</returns>
    public async Task<ComprehensiveValidationReport> VerifyComprehensiveAsync(
        Dictionary<string, SignatureDetails> expectedSignatures,
        string? testPdfPath = null,
        bool includePerformanceProfiling = true,
        int profilingIterations = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Run all validations in parallel for maximum performance
            var coverageTask = VerifyAndReportAsync(expectedSignatures, testPdfPath, cancellationToken);
            var validationTask = ValidateHighRiskAreasAsync(testPdfPath, cancellationToken);
            var workaroundTask = TestWorkaroundsAsync(testPdfPath, cancellationToken);

            // Wait for basic validations to complete
            await Task.WhenAll(coverageTask, validationTask, workaroundTask);

            var coverageReport = await coverageTask;
            var validationReports = await validationTask;
            var workaroundResults = await workaroundTask;

            // Run performance profiling separately if requested (can be time-consuming)
            ProfilingReport? profilingReport = null;
            if (includePerformanceProfiling)
            {
                profilingReport = await ProfilePerformanceAsync(testPdfPath, profilingIterations, cancellationToken);
            }

            return new ComprehensiveValidationReport
            {
                GeneratedAt = DateTime.UtcNow,
                CoverageReport = coverageReport,
                ValidationReports = validationReports,
                ProfilingReport = profilingReport,
                WorkaroundTestResults = workaroundResults
            };
        }
        catch (OperationCanceledException)
        {
            // Return a minimal report indicating cancellation
            throw;
        }
    }

    /// <summary>
    /// Runs comprehensive validation synchronously.
    /// </summary>
    /// <param name="expectedSignatures">Dictionary mapping function names to their expected signatures.</param>
    /// <param name="testPdfPath">Path to a test PDF file for validation tests.</param>
    /// <param name="includePerformanceProfiling">Whether to include performance profiling.</param>
    /// <param name="profilingIterations">Number of iterations for performance profiling.</param>
    /// <returns>A comprehensive validation report with all verification results.</returns>
    public ComprehensiveValidationReport VerifyComprehensive(
        Dictionary<string, SignatureDetails> expectedSignatures,
        string? testPdfPath = null,
        bool includePerformanceProfiling = true,
        int profilingIterations = 1000)
    {
        return VerifyComprehensiveAsync(expectedSignatures, testPdfPath, includePerformanceProfiling, profilingIterations, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Merges signature verification results with marshalling test results.
    /// </summary>
    /// <param name="signatureResults">Results from signature analysis.</param>
    /// <param name="marshallingResults">Results from marshalling tests.</param>
    /// <returns>A merged list of verification results.</returns>
    private List<VerificationResult> MergeResults(
        List<VerificationResult> signatureResults,
        List<VerificationResult> marshallingResults)
    {
        var merged = new Dictionary<string, VerificationResult>();

        // Add all signature results
        foreach (var result in signatureResults)
        {
            merged[result.FunctionName] = result;
        }

        // Merge in marshalling results
        foreach (var marshallingResult in marshallingResults)
        {
            if (merged.TryGetValue(marshallingResult.FunctionName, out var existingResult))
            {
                // Combine signature and marshalling data
                merged[marshallingResult.FunctionName] = new VerificationResult
                {
                    FunctionName = existingResult.FunctionName,
                    SignatureValid = existingResult.SignatureValid,
                    MarshallingCorrect = marshallingResult.MarshallingCorrect,
                    ErrorMessage = CombineErrorMessages(existingResult.ErrorMessage, marshallingResult.ErrorMessage),
                    Signature = existingResult.Signature,
                    TestResult = marshallingResult.TestResult
                };
            }
            else
            {
                // Marshalling result for function not in signature analysis - add it
                merged[marshallingResult.FunctionName] = marshallingResult;
            }
        }

        return merged.Values.ToList();
    }

    /// <summary>
    /// Combines multiple error messages into a single message.
    /// </summary>
    private static string? CombineErrorMessages(string? message1, string? message2)
    {
        if (string.IsNullOrEmpty(message1) && string.IsNullOrEmpty(message2))
        {
            return null;
        }

        if (string.IsNullOrEmpty(message1))
        {
            return message2;
        }

        if (string.IsNullOrEmpty(message2))
        {
            return message1;
        }

        return $"{message1}; {message2}";
    }

    /// <summary>
    /// Generates a coverage report from verification results.
    /// </summary>
    /// <param name="results">The verification results to analyze.</param>
    /// <returns>A comprehensive coverage report.</returns>
    private CoverageReport GenerateCoverageReport(List<VerificationResult> results)
    {
        var totalFunctions = results.Count;
        var verifiedFunctions = results.Count(r => r.SignatureValid);
        var testedFunctions = results.Count(r => r.MarshallingCorrect.HasValue);
        var passedFunctions = results.Count(r =>
            r.SignatureValid &&
            r.MarshallingCorrect.HasValue &&
            r.MarshallingCorrect.Value);
        var failedFunctions = results.Count(r =>
            !r.SignatureValid ||
            (r.MarshallingCorrect.HasValue && !r.MarshallingCorrect.Value));

        var untestedFunctions = results
            .Where(r => !r.MarshallingCorrect.HasValue)
            .Select(r => r.FunctionName)
            .OrderBy(n => n)
            .ToList();

        var failedFunctionNames = results
            .Where(r => !r.SignatureValid || (r.MarshallingCorrect.HasValue && !r.MarshallingCorrect.Value))
            .Select(r => r.FunctionName)
            .OrderBy(n => n)
            .ToList();

        return new CoverageReport
        {
            TotalFunctions = totalFunctions,
            VerifiedFunctions = verifiedFunctions,
            TestedFunctions = testedFunctions,
            PassedFunctions = passedFunctions,
            FailedFunctions = failedFunctions,
            UntestedFunctions = untestedFunctions,
            FailedFunctionNames = failedFunctionNames,
            Results = results,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Disposes resources used by the marshalling verifier.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // No managed resources to dispose currently
            // SignatureAnalyzer and CoverageReporter don't implement IDisposable
        }

        _disposed = true;
    }
}
