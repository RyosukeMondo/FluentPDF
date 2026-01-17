using FluentPDF.Verification.Core;
using FluentPDF.Verification.Pdfium;
using FluentResults;
using Serilog;
using System.Diagnostics;

namespace FluentPDF.Verification.Cli;

/// <summary>
/// Orchestrates execution of all PDFium verification tests.
/// Coordinates verifiers, collects results, and builds summary reports.
/// </summary>
public class VerificationExecutor
{
    private readonly VerificationOptions _options;
    private readonly IDllAnalyzer _dllAnalyzer;

    /// <summary>
    /// Creates a new verification executor with the specified options.
    /// </summary>
    /// <param name="options">Configuration for verification execution.</param>
    /// <param name="dllAnalyzer">DLL analyzer for signature verification.</param>
    public VerificationExecutor(VerificationOptions options, IDllAnalyzer dllAnalyzer)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dllAnalyzer = dllAnalyzer ?? throw new ArgumentNullException(nameof(dllAnalyzer));
    }

    /// <summary>
    /// Executes all verification tests and returns a summary.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result containing verification summary or errors.</returns>
    public async Task<Result<VerificationSummary>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ParallelExecution)
        {
            return await ExecuteParallelAsync(cancellationToken);
        }
        else
        {
            return await ExecuteSequentialAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Executes all verification tests in parallel for optimal performance.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result containing verification summary or errors.</returns>
    public async Task<Result<VerificationSummary>> ExecuteParallelAsync(CancellationToken cancellationToken = default)
    {
        Log.Debug("Starting parallel verification execution");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create all verifiers
            var verifiers = CreateVerifiers();

            // Validate all configurations first
            var configValidationResults = verifiers
                .Select(v => (Verifier: v, ValidationResult: v.ValidateConfiguration()))
                .ToList();

            foreach (var (verifier, validationResult) in configValidationResults)
            {
                if (validationResult.IsFailed)
                {
                    Log.Error("Configuration validation failed for {VerifierName}: {Error}",
                        verifier.LibraryName, validationResult.Errors[0].Message);
                    return Result.Fail<VerificationSummary>(
                        $"{verifier.LibraryName} configuration validation failed: {validationResult.Errors[0].Message}");
                }
            }

            Log.Information("All verifier configurations are valid");

            // Execute all verifications in parallel
            var verificationTasks = verifiers
                .Select(async verifier =>
                {
                    try
                    {
                        Log.Debug("Starting verification: {VerifierName}", verifier.LibraryName);
                        var result = await verifier.VerifyAsync(cancellationToken);
                        Log.Debug("Completed verification: {VerifierName}", verifier.LibraryName);
                        return (Verifier: verifier, Result: result);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Verification failed with exception: {VerifierName}", verifier.LibraryName);
                        return (Verifier: verifier, Result: Result.Fail<object>($"Exception during verification: {ex.Message}"));
                    }
                })
                .ToList();

            var verificationResults = await Task.WhenAll(verificationTasks);

            // Check if any verifications failed
            var failedVerifications = verificationResults
                .Where(r => r.Result.IsFailed)
                .ToList();

            if (failedVerifications.Any())
            {
                var errors = string.Join("; ", failedVerifications
                    .Select(fv => $"{fv.Verifier.LibraryName}: {fv.Result.Errors[0].Message}"));
                Log.Error("One or more verifications failed: {Errors}", errors);
                return Result.Fail<VerificationSummary>($"Verification failed: {errors}");
            }

            // Aggregate all results
            var summary = AggregateResults(verificationResults, stopwatch.Elapsed);

            stopwatch.Stop();
            Log.Information("Parallel verification completed in {Duration}ms. Total: {Total}, Passed: {Passed}, Failed: {Failed}",
                stopwatch.ElapsedMilliseconds, summary.TotalTests, summary.PassedTests, summary.FailedTests);

            return Result.Ok(summary);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during parallel verification");
            return Result.Fail<VerificationSummary>($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes all verification tests sequentially.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result containing verification summary or errors.</returns>
    public async Task<Result<VerificationSummary>> ExecuteSequentialAsync(CancellationToken cancellationToken = default)
    {
        Log.Debug("Starting sequential verification execution");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create all verifiers
            var verifiers = CreateVerifiers();

            // Validate and execute each verifier sequentially
            var allResults = new List<(IVerifier Verifier, Result<object> Result)>();

            foreach (var verifier in verifiers)
            {
                // Validate configuration
                var validationResult = verifier.ValidateConfiguration();
                if (validationResult.IsFailed)
                {
                    Log.Error("Configuration validation failed for {VerifierName}: {Error}",
                        verifier.LibraryName, validationResult.Errors[0].Message);
                    return Result.Fail<VerificationSummary>(
                        $"{verifier.LibraryName} configuration validation failed: {validationResult.Errors[0].Message}");
                }

                // Execute verification
                try
                {
                    Log.Information("Executing verification: {VerifierName}", verifier.LibraryName);
                    var result = await verifier.VerifyAsync(cancellationToken);
                    allResults.Add((verifier, result));

                    if (result.IsFailed)
                    {
                        Log.Warning("Verification failed: {VerifierName} - {Error}",
                            verifier.LibraryName, result.Errors[0].Message);
                    }
                    else
                    {
                        Log.Information("Verification succeeded: {VerifierName}", verifier.LibraryName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Verification failed with exception: {VerifierName}", verifier.LibraryName);
                    allResults.Add((verifier, Result.Fail<object>($"Exception during verification: {ex.Message}")));
                }
            }

            // Check if any verifications failed
            var failedVerifications = allResults
                .Where(r => r.Result.IsFailed)
                .ToList();

            if (failedVerifications.Any())
            {
                var errors = string.Join("; ", failedVerifications
                    .Select(fv => $"{fv.Verifier.LibraryName}: {fv.Result.Errors[0].Message}"));
                Log.Error("One or more verifications failed: {Errors}", errors);
                return Result.Fail<VerificationSummary>($"Verification failed: {errors}");
            }

            // Aggregate all results
            var summary = AggregateResults(allResults, stopwatch.Elapsed);

            stopwatch.Stop();
            Log.Information("Sequential verification completed in {Duration}ms. Total: {Total}, Passed: {Passed}, Failed: {Failed}",
                stopwatch.ElapsedMilliseconds, summary.TotalTests, summary.PassedTests, summary.FailedTests);

            return Result.Ok(summary);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during sequential verification");
            return Result.Fail<VerificationSummary>($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates all verifier instances.
    /// </summary>
    private List<IVerifier> CreateVerifiers()
    {
        var verifiers = new List<IVerifier>
        {
            CreateVerifier(new PdfiumSignatureVerifier(_options, _dllAnalyzer))
        };

        // Only add return type and behavior verifiers if test files are available
        if (!string.IsNullOrWhiteSpace(_options.TestFileDirectory))
        {
            verifiers.Add(CreateVerifier(new PdfiumReturnTypeVerifier(_options)));
            verifiers.Add(CreateVerifier(new PdfiumBehaviorVerifier(_options)));
        }
        else
        {
            Log.Warning("Test file directory not specified. Skipping return type and behavior verification.");
        }

        return verifiers;
    }

    /// <summary>
    /// Aggregates verification results from all verifiers into a single summary.
    /// </summary>
    private VerificationSummary AggregateResults(
        IEnumerable<(IVerifier Verifier, Result<object> Result)> verificationResults,
        TimeSpan totalDuration)
    {
        var allResults = new List<VerificationResult>();

        foreach (var (verifier, result) in verificationResults)
        {
            if (result.IsSuccess)
            {
                // Extract results from the verification result object
                var resultObject = result.Value;
                var resultsProperty = resultObject.GetType().GetProperty("Results");

                if (resultsProperty != null)
                {
                    var results = resultsProperty.GetValue(resultObject) as IEnumerable<VerificationResult>;
                    if (results != null)
                    {
                        allResults.AddRange(results);
                    }
                }
            }
        }

        var totalTests = allResults.Count;
        var passedTests = allResults.Count(r => r.Success);
        var failedTests = allResults.Count(r => !r.Success);

        // Try to get library version from DLL analyzer
        string? libraryVersion = null;
        try
        {
            var dllInfoResult = _dllAnalyzer.AnalyzeAsync(_options.DllPath).GetAwaiter().GetResult();
            if (dllInfoResult.IsSuccess)
            {
                libraryVersion = dllInfoResult.Value.Version;
            }
        }
        catch
        {
            // Ignore errors getting version
        }

        return new VerificationSummary
        {
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            Results = allResults.AsReadOnly(),
            TotalDuration = totalDuration,
            LibraryVersion = libraryVersion
        };
    }

    /// <summary>
    /// Internal interface to allow uniform handling of different verifier types.
    /// </summary>
    private interface IVerifier
    {
        string LibraryName { get; }
        Result ValidateConfiguration();
        Task<Result<object>> VerifyAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Adapter to wrap ILibraryVerifier as IVerifier.
    /// </summary>
    private class VerifierAdapter<TResult> : IVerifier
    {
        private readonly ILibraryVerifier<TResult> _verifier;

        public VerifierAdapter(ILibraryVerifier<TResult> verifier)
        {
            _verifier = verifier;
        }

        public string LibraryName => _verifier.LibraryName;

        public Result ValidateConfiguration() => _verifier.ValidateConfiguration();

        public async Task<Result<object>> VerifyAsync(CancellationToken cancellationToken = default)
        {
            var result = await _verifier.VerifyAsync(cancellationToken);
            if (result.IsSuccess)
            {
                return Result.Ok<object>(result.Value!);
            }
            else
            {
                return Result.Fail<object>(result.Errors);
            }
        }
    }

    /// <summary>
    /// Helper to create verifiers with proper type wrapping.
    /// </summary>
    private static IVerifier CreateVerifier<TResult>(ILibraryVerifier<TResult> verifier)
    {
        return new VerifierAdapter<TResult>(verifier);
    }
}
