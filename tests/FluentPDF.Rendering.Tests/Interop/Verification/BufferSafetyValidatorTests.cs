using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using FluentPDF.Rendering.Interop.Verification.Validators;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Tests for BufferSafetyValidator to ensure buffer overflow detection and edge-case handling.
/// </summary>
public class BufferSafetyValidatorTests
{
    private readonly BufferSafetyValidator _validator;

    public BufferSafetyValidatorTests()
    {
        _validator = new BufferSafetyValidator();
    }

    [Fact]
    public void ValidatorName_ReturnsExpectedName()
    {
        // Arrange & Act
        var name = _validator.ValidatorName;

        // Assert
        Assert.Equal("Buffer Safety Validator", name);
    }

    [Fact]
    public void TargetArea_ReturnsExpectedArea()
    {
        // Arrange & Act
        var area = _validator.TargetArea;

        // Assert
        Assert.Equal("Buffer Overflow & Memory Safety", area);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValidationReport()
    {
        // Arrange & Act
        var report = await _validator.ValidateAsync();

        // Assert
        Assert.NotNull(report);
        Assert.Equal("Buffer Safety Validator", report.ValidatorName);
        Assert.Equal("Buffer Overflow & Memory Safety", report.TargetArea);
        Assert.True(report.Timestamp <= DateTime.UtcNow);
        Assert.NotNull(report.Summary);
        Assert.NotNull(report.ResultsByArea);
    }

    [Fact]
    public async Task ValidateAsync_ContainsStaticAnalysisResults()
    {
        // Arrange & Act
        var report = await _validator.ValidateAsync();

        // Assert
        Assert.True(report.ResultsByArea.ContainsKey("Static Analysis"));
        var staticResults = report.ResultsByArea["Static Analysis"];
        Assert.NotEmpty(staticResults);

        // Should have results for known Marshal.Copy locations
        var knownLocationResults = staticResults.Where(r => r.TestName.StartsWith("Marshal.Copy Call Site:")).ToList();
        Assert.NotEmpty(knownLocationResults);

        // Should have call site discovery result
        var discoveryResult = staticResults.FirstOrDefault(r => r.TestName == "Marshal.Copy Call Site Discovery");
        Assert.NotNull(discoveryResult);
        Assert.True(discoveryResult.Passed);
    }

    [Fact]
    public async Task ValidateAsync_ContainsRuntimeSimulationResults()
    {
        // Arrange & Act
        var report = await _validator.ValidateAsync();

        // Assert
        Assert.True(report.ResultsByArea.ContainsKey("Runtime Simulation"));
        var simulationResults = report.ResultsByArea["Runtime Simulation"];
        Assert.Equal(5, simulationResults.Count); // 5 edge-case tests
    }

    [Fact]
    public async Task ValidateAsync_SummaryHasCorrectCounts()
    {
        // Arrange & Act
        var report = await _validator.ValidateAsync();

        // Assert
        var summary = report.Summary;
        Assert.True(summary.TotalTests > 0);
        Assert.True(summary.PassedCount >= 0);
        Assert.True(summary.FailedCount >= 0);
        Assert.True(summary.WarningCount >= 0);
        Assert.True(summary.CriticalCount >= 0);
        Assert.Equal(summary.TotalTests, summary.PassedCount + summary.FailedCount);
    }

    [Fact]
    public async Task AnalyzeMarshalCopyCallsAsync_FindsKnownLocations()
    {
        // Arrange & Act
        var results = await _validator.AnalyzeMarshalCopyCallsAsync();

        // Assert
        Assert.NotEmpty(results);

        // Should identify known Marshal.Copy locations
        var knownLocations = new[]
        {
            "FluentPDF.Rendering.Services.PdfRenderingService.ConvertToPngStreamAsync",
            "FluentPDF.Rendering.Interop.Verification.Validators.BitmapMarshalingValidator",
            "FluentPDF.Rendering.Services.PdfFormService",
            "FluentPDF.Rendering.Interop.PdfiumFormInterop"
        };

        foreach (var location in knownLocations)
        {
            var result = results.FirstOrDefault(r => r.TestName.Contains(location));
            Assert.NotNull(result);
            Assert.Contains("Buffer safety review", result.Message);
        }
    }

    [Fact]
    public async Task AnalyzeMarshalCopyCallsAsync_IncludesContext()
    {
        // Arrange & Act
        var results = await _validator.AnalyzeMarshalCopyCallsAsync();

        // Assert
        var locationResults = results.Where(r => r.TestName.StartsWith("Marshal.Copy Call Site:")).ToList();
        Assert.NotEmpty(locationResults);

        foreach (var result in locationResults)
        {
            Assert.NotNull(result.Context);
            Assert.True(result.Context.ContainsKey("Location"));
            Assert.True(result.Context.ContainsKey("RequiresReview"));
        }
    }

    [Fact]
    public async Task SimulateBufferOverflowAsync_TestsZeroLengthBuffer()
    {
        // Arrange & Act
        var results = await _validator.SimulateBufferOverflowAsync();

        // Assert
        var zeroLengthTest = results.FirstOrDefault(r => r.TestName == "Zero-Length Buffer");
        Assert.NotNull(zeroLengthTest);
        Assert.True(zeroLengthTest.Passed);
        Assert.Contains("zero-length", zeroLengthTest.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SimulateBufferOverflowAsync_TestsMinimalBuffer()
    {
        // Arrange & Act
        var results = await _validator.SimulateBufferOverflowAsync();

        // Assert
        var minimalBufferTest = results.FirstOrDefault(r => r.TestName == "1-Byte Buffer (UTF-16 Minimum)");
        Assert.NotNull(minimalBufferTest);
        Assert.False(minimalBufferTest.Passed); // 1 byte is insufficient for UTF-16
        Assert.Equal(ValidationSeverity.Warning, minimalBufferTest.Severity);
        Assert.Contains("UTF-16", minimalBufferTest.Message);
    }

    [Fact]
    public async Task SimulateBufferOverflowAsync_TestsLargeAllocationBoundary()
    {
        // Arrange & Act
        var results = await _validator.SimulateBufferOverflowAsync();

        // Assert
        var boundaryTest = results.FirstOrDefault(r => r.TestName == "2GB Allocation Boundary");
        Assert.NotNull(boundaryTest);
        Assert.NotNull(boundaryTest.Context);
        Assert.True(boundaryTest.Context.ContainsKey("MaxWidth"));
        Assert.True(boundaryTest.Context.ContainsKey("MaxHeight"));
    }

    [Fact]
    public async Task SimulateBufferOverflowAsync_TestsStrideOverflow()
    {
        // Arrange & Act
        var results = await _validator.SimulateBufferOverflowAsync();

        // Assert
        var strideTest = results.FirstOrDefault(r => r.TestName == "Stride Overflow Detection");
        Assert.NotNull(strideTest);
        Assert.Contains("overflow", strideTest.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(strideTest.Context);
    }

    [Fact]
    public async Task SimulateBufferOverflowAsync_TestsTwoPhaseAllocation()
    {
        // Arrange & Act
        var results = await _validator.SimulateBufferOverflowAsync();

        // Assert
        var twoPhaseTest = results.FirstOrDefault(r => r.TestName == "Two-Phase Allocation Pattern");
        Assert.NotNull(twoPhaseTest);
        Assert.True(twoPhaseTest.Passed);
        Assert.Contains("two-phase", twoPhaseTest.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(twoPhaseTest.Context);
        Assert.True(twoPhaseTest.Context.ContainsKey("Pattern"));
    }

    [Fact]
    public async Task SimulateBufferOverflowAsync_AllTestsHaveDocumentation()
    {
        // Arrange & Act
        var results = await _validator.SimulateBufferOverflowAsync();

        // Assert
        var testsRequiringDocs = results.Where(r => !r.Passed || r.Severity >= ValidationSeverity.Warning);
        foreach (var test in testsRequiringDocs)
        {
            if (!string.IsNullOrEmpty(test.SuggestedFix))
            {
                Assert.NotNull(test.DocumentationUrl);
                Assert.Contains("docs/marshaling/", test.DocumentationUrl);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_CancellationTokenWorks()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _validator.ValidateAsync(cts.Token);
        });
    }

    [Fact]
    public async Task AnalyzeMarshalCopyCallsAsync_CancellationTokenWorks()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _validator.AnalyzeMarshalCopyCallsAsync(cts.Token);
        });
    }

    [Fact]
    public async Task ValidateAsync_IsThreadSafe()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _validator.ValidateAsync())
            .ToArray();

        var reports = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, reports.Length);
        foreach (var report in reports)
        {
            Assert.NotNull(report);
            Assert.True(report.Summary.TotalTests > 0);
        }
    }

    [Fact]
    public async Task ValidateAsync_AllResultsHaveSeverity()
    {
        // Arrange & Act
        var report = await _validator.ValidateAsync();

        // Assert
        var allResults = report.ResultsByArea.Values.SelectMany(r => r).ToList();
        foreach (var result in allResults)
        {
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), result.Severity));
        }
    }

    [Fact]
    public async Task ValidateAsync_FailedTestsHaveContext()
    {
        // Arrange & Act
        var report = await _validator.ValidateAsync();

        // Assert
        var failedResults = report.ResultsByArea.Values
            .SelectMany(r => r)
            .Where(r => !r.Passed)
            .ToList();

        foreach (var result in failedResults)
        {
            // Failed tests should have either error details or context
            Assert.True(!string.IsNullOrEmpty(result.ErrorDetails) || result.Context != null);
        }
    }
}
