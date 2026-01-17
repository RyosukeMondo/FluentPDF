using System.Diagnostics;
using Serilog;

namespace FluentPDF.App.Testing;

/// <summary>
/// Orchestrates test discovery, execution, verification, and reporting.
/// Provides the main entry point for running CLI tests.
/// </summary>
public sealed class TestRunner
{
    private readonly TestDiscovery _discovery;
    private readonly TestExecutor _executor;
    private readonly ResultVerifier _verifier;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the TestRunner class.
    /// </summary>
    /// <param name="discovery">Test discovery component</param>
    /// <param name="executor">Test executor component</param>
    /// <param name="verifier">Result verifier component</param>
    /// <param name="logger">Logger for test operations</param>
    public TestRunner(
        TestDiscovery discovery,
        TestExecutor executor,
        ResultVerifier verifier,
        ILogger logger)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers all available CLI tests.
    /// </summary>
    /// <returns>List of discovered tests</returns>
    public List<ICliTest> DiscoverTests()
    {
        _logger.Information("Discovering CLI tests");
        try
        {
            var tests = _discovery.DiscoverAllTests();
            _logger.Information("Discovered {Count} CLI tests", tests.Count);
            return tests;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to discover tests");
            throw;
        }
    }

    /// <summary>
    /// Runs a single test by name.
    /// </summary>
    /// <param name="testName">Name of the test to run</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Test suite result containing the single test result</returns>
    public async Task<TestSuiteResult> RunTestAsync(string testName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("Test name cannot be null or whitespace", nameof(testName));
        }

        _logger.Information("Running test: {TestName}", testName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Find the test
            var test = _discovery.FindTest(testName);
            if (test is null)
            {
                _logger.Error("Test not found: {TestName}", testName);
                return new TestSuiteResult
                {
                    TotalTests = 0,
                    PassedTests = 0,
                    FailedTests = 0,
                    TotalDuration = stopwatch.Elapsed
                };
            }

            // Execute the test
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _executor.ExecuteTestAsync(test);

            stopwatch.Stop();

            // Build test suite result
            var suiteResult = new TestSuiteResult
            {
                TotalTests = 1,
                PassedTests = result.Success ? 1 : 0,
                FailedTests = result.Success ? 0 : 1,
                TotalDuration = stopwatch.Elapsed,
                Results = new List<CliTestResult> { result }
            };

            _logger.Information(
                "Test {TestName} completed: {Status} in {Duration}ms",
                testName,
                result.Success ? "PASSED" : "FAILED",
                result.Duration.TotalMilliseconds);

            return suiteResult;
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Test execution cancelled: {TestName}", testName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to run test: {TestName}", testName);
            stopwatch.Stop();

            return new TestSuiteResult
            {
                TotalTests = 1,
                PassedTests = 0,
                FailedTests = 1,
                TotalDuration = stopwatch.Elapsed,
                Results = new List<CliTestResult>
                {
                    new CliTestResult
                    {
                        TestName = testName,
                        Success = false,
                        Duration = stopwatch.Elapsed,
                        ErrorMessage = $"Test runner failed: {ex.Message}"
                    }
                }
            };
        }
    }

    /// <summary>
    /// Runs all discovered tests.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Test suite result containing all test results</returns>
    public async Task<TestSuiteResult> RunAllTestsAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Running all CLI tests");
        var stopwatch = Stopwatch.StartNew();
        var results = new List<CliTestResult>();

        try
        {
            // Discover tests
            var tests = DiscoverTests();
            if (tests.Count == 0)
            {
                _logger.Warning("No tests discovered");
                return new TestSuiteResult
                {
                    TotalTests = 0,
                    PassedTests = 0,
                    FailedTests = 0,
                    TotalDuration = stopwatch.Elapsed
                };
            }

            // Execute each test
            for (int i = 0; i < tests.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var test = tests[i];
                _logger.Information("Running test {Current}/{Total}: {TestName}", i + 1, tests.Count, test.Name);

                try
                {
                    var result = await _executor.ExecuteTestAsync(test);
                    results.Add(result);

                    _logger.Information(
                        "Test {TestName} {Status} in {Duration}ms",
                        test.Name,
                        result.Success ? "PASSED" : "FAILED",
                        result.Duration.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Test {TestName} threw exception", test.Name);
                    results.Add(new CliTestResult
                    {
                        TestName = test.Name,
                        Success = false,
                        Duration = TimeSpan.Zero,
                        ErrorMessage = $"Test threw exception: {ex.Message}"
                    });
                }
            }

            stopwatch.Stop();

            // Build test suite result
            var suiteResult = new TestSuiteResult
            {
                TotalTests = results.Count,
                PassedTests = results.Count(r => r.Success),
                FailedTests = results.Count(r => !r.Success),
                TotalDuration = stopwatch.Elapsed,
                Results = results
            };

            _logger.Information(
                "All tests completed: {Passed}/{Total} passed in {Duration}s",
                suiteResult.PassedTests,
                suiteResult.TotalTests,
                suiteResult.TotalDuration.TotalSeconds);

            return suiteResult;
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Test execution cancelled after running {Count} tests", results.Count);
            stopwatch.Stop();

            return new TestSuiteResult
            {
                TotalTests = results.Count,
                PassedTests = results.Count(r => r.Success),
                FailedTests = results.Count(r => !r.Success),
                TotalDuration = stopwatch.Elapsed,
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to run all tests");
            stopwatch.Stop();

            return new TestSuiteResult
            {
                TotalTests = results.Count,
                PassedTests = results.Count(r => r.Success),
                FailedTests = results.Count(r => !r.Success),
                TotalDuration = stopwatch.Elapsed,
                Results = results
            };
        }
    }
}
