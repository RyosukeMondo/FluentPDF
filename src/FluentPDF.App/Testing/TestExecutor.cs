using System.Diagnostics;
using Serilog;

namespace FluentPDF.App.Testing;

/// <summary>
/// Executes CLI tests with proper isolation, error handling, and timeout management.
/// Creates isolated test contexts and captures execution results.
/// </summary>
public sealed class TestExecutor
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Initializes a new instance of the TestExecutor class.
    /// </summary>
    /// <param name="services">Service provider for creating test contexts</param>
    /// <param name="logger">Logger for test execution operations</param>
    public TestExecutor(IServiceProvider services, ILogger logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a CLI test with proper isolation and error handling.
    /// Creates an isolated context, runs the test, and captures results.
    /// </summary>
    /// <param name="test">The test to execute</param>
    /// <param name="timeout">Optional timeout for test execution (default: 30 seconds)</param>
    /// <returns>Test result with execution status, timing, and outputs</returns>
    public async Task<CliTestResult> ExecuteTestAsync(ICliTest test, TimeSpan? timeout = null)
    {
        return await ExecuteTestAsync(test, contextData: null, timeout);
    }

    /// <summary>
    /// Executes a CLI test with proper isolation, error handling, and custom context data.
    /// Creates an isolated context with the provided data, runs the test, and captures results.
    /// </summary>
    /// <param name="test">The test to execute</param>
    /// <param name="contextData">Optional context data to pass to the test</param>
    /// <param name="timeout">Optional timeout for test execution (default: 30 seconds)</param>
    /// <returns>Test result with execution status, timing, and outputs</returns>
    public async Task<CliTestResult> ExecuteTestAsync(ICliTest test, Dictionary<string, object>? contextData, TimeSpan? timeout = null)
    {
        if (test is null)
        {
            throw new ArgumentNullException(nameof(test));
        }

        var effectiveTimeout = timeout ?? DefaultTimeout;
        _logger.Information("Executing test: {TestName} (timeout: {Timeout}s)", test.Name, effectiveTimeout.TotalSeconds);

        var stopwatch = Stopwatch.StartNew();
        CliTestContext? context = null;

        try
        {
            // Create isolated test context
            context = CreateContext(test.Name, contextData);
            _logger.Debug("Created test context with working directory: {WorkingDirectory}", context.WorkingDirectory);

            // Execute test with timeout
            using var cts = new CancellationTokenSource(effectiveTimeout);
            var runTask = test.RunAsync(context);
            var completedTask = await Task.WhenAny(runTask, Task.Delay(effectiveTimeout, cts.Token));

            CliTestResult result;
            if (completedTask == runTask)
            {
                // Test completed in time
                cts.Cancel(); // Cancel the delay task
                result = await runTask;
                _logger.Debug("Test {TestName} completed in {Duration}ms", test.Name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // Test timed out
                stopwatch.Stop();
                _logger.Warning("Test {TestName} timed out after {Timeout}s", test.Name, effectiveTimeout.TotalSeconds);
                result = new CliTestResult
                {
                    TestName = test.Name,
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    ErrorMessage = $"Test execution timed out after {effectiveTimeout.TotalSeconds} seconds"
                };
            }

            // Verify test result
            if (result.Success)
            {
                try
                {
                    var verified = await test.VerifyAsync(result);
                    if (!verified)
                    {
                        result.Success = false;
                        result.ErrorMessage = result.ErrorMessage ?? "Verification failed";
                        _logger.Warning("Test {TestName} verification failed", test.Name);
                    }
                    else
                    {
                        _logger.Information("Test {TestName} passed verification", test.Name);
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Verification threw exception: {ex.Message}";
                    _logger.Error(ex, "Test {TestName} verification threw exception", test.Name);
                }
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Test {TestName} execution failed with exception", test.Name);

            return new CliTestResult
            {
                TestName = test.Name,
                Success = false,
                Duration = stopwatch.Elapsed,
                ErrorMessage = $"Test execution failed: {ex.Message}\n{ex.StackTrace}"
            };
        }
        finally
        {
            // Cleanup context
            context?.Dispose();
        }
    }

    /// <summary>
    /// Creates an isolated test context with temporary working directory.
    /// Context is automatically cleaned up when disposed.
    /// </summary>
    /// <param name="testName">Name of the test for directory naming</param>
    /// <param name="contextData">Optional context data to populate the test context</param>
    /// <returns>Isolated test context</returns>
    public CliTestContext CreateContext(string testName, Dictionary<string, object>? contextData = null)
    {
        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("Test name cannot be null or whitespace", nameof(testName));
        }

        // Create unique temporary directory for this test
        var tempBasePath = Path.GetTempPath();
        var testDirName = $"FluentPDF_Test_{SanitizeFileName(testName)}_{Guid.NewGuid():N}";
        var workingDirectory = Path.Combine(tempBasePath, testDirName);

        Directory.CreateDirectory(workingDirectory);
        _logger.Debug("Created test working directory: {WorkingDirectory}", workingDirectory);

        var context = new CliTestContext
        {
            WorkingDirectory = workingDirectory,
            Services = _services,
            Logger = _logger.ForContext("TestName", testName)
        };

        // Populate context data if provided
        if (contextData != null)
        {
            foreach (var kvp in contextData)
            {
                context.Data[kvp.Key] = kvp.Value;
            }
        }

        return context;
    }

    /// <summary>
    /// Sanitizes a test name for use in file/directory names.
    /// Removes or replaces invalid file system characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(c => invalidChars.Contains(c) ? '_' : c));
    }
}
