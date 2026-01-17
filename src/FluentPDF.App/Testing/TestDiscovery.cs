using System.Reflection;
using Serilog;

namespace FluentPDF.App.Testing;

/// <summary>
/// Discovers CLI test implementations using reflection.
/// Finds all ICliTest implementations in the application assembly and caches results.
/// </summary>
public sealed class TestDiscovery
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private List<ICliTest>? _cachedTests;

    /// <summary>
    /// Initializes a new instance of the TestDiscovery class.
    /// </summary>
    /// <param name="services">Service provider for creating test instances</param>
    /// <param name="logger">Logger for discovery operations</param>
    public TestDiscovery(IServiceProvider services, ILogger logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers all ICliTest implementations in the application assembly.
    /// Results are cached after first discovery for performance.
    /// </summary>
    /// <returns>List of all discovered CLI tests</returns>
    public List<ICliTest> DiscoverAllTests()
    {
        if (_cachedTests is not null)
        {
            _logger.Debug("Returning {Count} cached tests", _cachedTests.Count);
            return _cachedTests;
        }

        _logger.Information("Starting test discovery");
        var tests = new List<ICliTest>();

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ICliTest).IsAssignableFrom(t))
                .ToList();

            _logger.Debug("Found {Count} test types in assembly", testTypes.Count);

            foreach (var testType in testTypes)
            {
                try
                {
                    // Try to create instance using DI first, fallback to Activator
                    var test = (_services.GetService(testType) as ICliTest)
                               ?? (Activator.CreateInstance(testType) as ICliTest);

                    if (test is not null)
                    {
                        tests.Add(test);
                        _logger.Debug("Discovered test: {TestName} ({TypeName})", test.Name, testType.Name);
                    }
                    else
                    {
                        _logger.Warning("Failed to create instance of test type: {TypeName}", testType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to instantiate test type: {TypeName}", testType.Name);
                }
            }

            _cachedTests = tests;
            _logger.Information("Test discovery complete. Found {Count} tests", tests.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Test discovery failed");
            throw;
        }

        return tests;
    }

    /// <summary>
    /// Finds a specific test by name.
    /// </summary>
    /// <param name="name">The test name to search for</param>
    /// <returns>The matching test, or null if not found</returns>
    public ICliTest? FindTest(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Test name cannot be null or whitespace", nameof(name));
        }

        var allTests = DiscoverAllTests();
        var test = allTests.FirstOrDefault(t =>
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

        if (test is null)
        {
            _logger.Warning("Test not found: {TestName}", name);
        }

        return test;
    }

    /// <summary>
    /// Clears the test cache, forcing rediscovery on next call.
    /// Useful for testing or when test implementations may have changed.
    /// </summary>
    public void ClearCache()
    {
        _logger.Debug("Clearing test discovery cache");
        _cachedTests = null;
    }
}
