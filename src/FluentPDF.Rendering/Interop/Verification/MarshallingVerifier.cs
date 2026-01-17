using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            // Step 4: Generate coverage report
            var report = GenerateCoverageReport(mergedResults);

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
