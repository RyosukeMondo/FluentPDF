using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using FluentPDF.Rendering.Interop.Verification.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

public sealed class ThreadingModelValidatorTests
{
    private readonly ThreadingModelValidator _validator;

    public ThreadingModelValidatorTests()
    {
        var logger = NullLogger<ThreadingModelValidator>.Instance;
        _validator = new ThreadingModelValidator(logger);
    }

    [Fact]
    public void ValidatorName_ShouldReturnCorrectValue()
    {
        Assert.Equal("Threading Model Validator", _validator.ValidatorName);
    }

    [Fact]
    public void TargetArea_ShouldReturnCorrectValue()
    {
        Assert.Equal("Threading Model", _validator.TargetArea);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnValidationReport()
    {
        var report = await _validator.ValidateAsync();

        Assert.NotNull(report);
        Assert.Equal("Threading Model Validator", report.ValidatorName);
        Assert.Equal("Threading Model", report.TargetArea);
        Assert.NotNull(report.Summary);
        Assert.NotNull(report.ResultsByArea);
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeAllAreas()
    {
        var report = await _validator.ValidateAsync();

        Assert.True(report.ResultsByArea.ContainsKey("Task.Yield() Workaround"));
        Assert.True(report.ResultsByArea.ContainsKey("Concurrent Access"));
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveCorrectTestCount()
    {
        var report = await _validator.ValidateAsync();

        // 2 Task.Yield() tests + 2 concurrent access tests = 4 total
        Assert.Equal(4, report.Summary.TotalTests);
    }

    [Fact]
    public async Task ValidateTaskYieldWorkaroundAsync_ShouldTestSequentialOperations()
    {
        var results = await _validator.ValidateTaskYieldWorkaroundAsync();

        var sequentialTest = results.FirstOrDefault(r =>
            r.TestName.Contains("Sequential operations with Task.Yield()"));

        Assert.NotNull(sequentialTest);
        Assert.True(sequentialTest.Passed, "Sequential operations should succeed with Task.Yield()");
        Assert.NotNull(sequentialTest.Context);
        Assert.True(sequentialTest.Context.ContainsKey("OperationCount"));
    }

    [Fact]
    public async Task ValidateTaskYieldWorkaroundAsync_ShouldTestHighVolumeOperations()
    {
        var results = await _validator.ValidateTaskYieldWorkaroundAsync();

        var highVolumeTest = results.FirstOrDefault(r =>
            r.TestName.Contains("High-volume sequential operations"));

        Assert.NotNull(highVolumeTest);
        Assert.True(highVolumeTest.Passed, "High-volume operations should complete successfully");
        Assert.NotNull(highVolumeTest.Context);
        Assert.Equal("100", highVolumeTest.Context["IterationCount"]);
    }

    [Fact]
    public async Task ValidateConcurrentAccessAsync_ShouldHandleMissingTestExecutable()
    {
        // Without test executable configured, should return warning
        var results = await _validator.ValidateConcurrentAccessAsync();

        var accessViolationTest = results.FirstOrDefault(r =>
            r.TestName.Contains("Concurrent access AccessViolation detection"));

        Assert.NotNull(accessViolationTest);
        // Should pass with warning when test executable not configured
        Assert.True(accessViolationTest.Passed || accessViolationTest.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task ValidateConcurrentAccessAsync_ShouldTestHighConcurrency()
    {
        var results = await _validator.ValidateConcurrentAccessAsync();

        var concurrencyTest = results.FirstOrDefault(r =>
            r.TestName.Contains("High concurrency with Task.Yield()"));

        Assert.NotNull(concurrencyTest);
        Assert.True(concurrencyTest.Passed, "High concurrency with Task.Yield() should succeed");
        Assert.NotNull(concurrencyTest.Context);
        Assert.Equal("100", concurrencyTest.Context["ConcurrentOperations"]);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellation_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should handle cancellation gracefully
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            // May throw or may complete with warning, depending on when cancellation occurs
            var report = await _validator.ValidateAsync(cts.Token);
        });
    }

    [Fact]
    public async Task ValidateTaskYieldWorkaroundAsync_ShouldReturnTwoTests()
    {
        var results = await _validator.ValidateTaskYieldWorkaroundAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ValidateConcurrentAccessAsync_ShouldReturnTwoTests()
    {
        var results = await _validator.ValidateConcurrentAccessAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ValidateAsync_SummaryStatistics_ShouldBeAccurate()
    {
        var report = await _validator.ValidateAsync();

        // Verify summary calculations
        var totalResultsCount = report.ResultsByArea.Values.Sum(list => list.Count);
        Assert.Equal(report.Summary.TotalTests, totalResultsCount);

        var actualPassedCount = report.ResultsByArea.Values
            .SelectMany(list => list)
            .Count(r => r.Passed);
        Assert.Equal(report.Summary.PassedCount, actualPassedCount);

        var actualFailedCount = report.ResultsByArea.Values
            .SelectMany(list => list)
            .Count(r => !r.Passed && r.Severity != ValidationSeverity.Warning);
        Assert.Equal(report.Summary.FailedCount, actualFailedCount);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ThreadingModelValidator(null!));
    }

    [Fact]
    public void Constructor_WithTestExecutablePath_ShouldAcceptPath()
    {
        var logger = NullLogger<ThreadingModelValidator>.Instance;
        var validator = new ThreadingModelValidator(logger, "C:\\test\\path.exe");

        Assert.NotNull(validator);
        Assert.Equal("Threading Model Validator", validator.ValidatorName);
    }

    [Fact]
    public async Task SequentialOperations_ContextData_ShouldContainPattern()
    {
        var results = await _validator.ValidateTaskYieldWorkaroundAsync();

        var sequentialTest = results.First(r =>
            r.TestName.Contains("Sequential operations with Task.Yield()"));

        Assert.NotNull(sequentialTest.Context);
        Assert.True(sequentialTest.Context.ContainsKey("Pattern"));
        Assert.Contains("Task.Yield", sequentialTest.Context["Pattern"]);
    }

    [Fact]
    public async Task HighConcurrency_ResultCount_ShouldMatch()
    {
        var results = await _validator.ValidateConcurrentAccessAsync();

        var concurrencyTest = results.First(r =>
            r.TestName.Contains("High concurrency with Task.Yield()"));

        Assert.NotNull(concurrencyTest.Context);
        Assert.Equal("100", concurrencyTest.Context["ConcurrentOperations"]);
        Assert.Equal("100", concurrencyTest.Context["ResultCount"]);
    }
}
