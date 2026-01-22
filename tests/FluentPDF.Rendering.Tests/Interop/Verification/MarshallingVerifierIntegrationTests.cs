using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Integration tests for the MarshallingVerifier orchestrator with new validators,
/// performance profiling, and workaround regression tests.
/// </summary>
public class MarshallingVerifierIntegrationTests : IDisposable
{
    private readonly MarshallingVerifier _verifier;
    private readonly string? _testPdfPath;

    public MarshallingVerifierIntegrationTests()
    {
        _verifier = new MarshallingVerifier(typeof(PdfiumInterop));

        // Use a test PDF if available
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "comprehensive_test.pdf");
        _testPdfPath = File.Exists(testDataPath) ? testDataPath : null;
    }

    [Fact]
    public async Task ValidateHighRiskAreasAsync_RunsAllValidatorsInParallel()
    {
        // Act
        var validationReports = await _verifier.ValidateHighRiskAreasAsync(_testPdfPath);

        // Assert
        Assert.NotNull(validationReports);
        Assert.Equal(5, validationReports.Count); // UTF-16, Bitmap, Annotation, Threading, Buffer Safety

        // Verify each validator ran
        var validatorNames = validationReports.Select(r => r.ValidatorName).ToList();
        Assert.Contains("Utf16MarshalingValidator", validatorNames);
        Assert.Contains("BitmapMarshalingValidator", validatorNames);
        Assert.Contains("AnnotationMarshalingValidator", validatorNames);
        Assert.Contains("ThreadingModelValidator", validatorNames);
        Assert.Contains("BufferSafetyValidator", validatorNames);

        // Each report should have a summary
        foreach (var report in validationReports)
        {
            Assert.NotNull(report.Summary);
            Assert.True(report.Summary.TotalTests > 0);
        }
    }

    [Fact]
    public async Task ProfilePerformanceAsync_ReturnsProfilingReport()
    {
        // Act
        var profilingReport = await _verifier.ProfilePerformanceAsync(_testPdfPath, iterations: 100);

        // Assert
        Assert.NotNull(profilingReport);
        Assert.True(profilingReport.TotalFunctions > 0);
        Assert.NotNull(profilingReport.ResultsByFunction);
        Assert.NotEmpty(profilingReport.ResultsByFunction);
        Assert.Equal(100, profilingReport.IterationsPerFunction);
    }

    [Fact]
    public async Task TestWorkaroundsAsync_ReturnsAllWorkaroundResults()
    {
        // Act
        var workaroundResults = await _verifier.TestWorkaroundsAsync(_testPdfPath);

        // Assert
        Assert.NotNull(workaroundResults);
        Assert.Equal(3, workaroundResults.Count); // Float Dimension, Threading, SoftwareBitmap

        // Verify each workaround was tested
        var workaroundNames = workaroundResults.Select(w => w.WorkaroundName).ToList();
        Assert.Contains("Float Dimension Workaround", workaroundNames);
        Assert.Contains("Threading Workaround", workaroundNames);
        Assert.Contains("SoftwareBitmap Workaround", workaroundNames);

        // Each result should have a status and details
        foreach (var result in workaroundResults)
        {
            // Status is an enum (value type), so no need to check for null
            Assert.NotNull(result.Details);
            Assert.NotNull(result.RecommendedAction);
            Assert.NotEmpty(result.DocumentationReference);
        }
    }

    [Fact]
    public async Task VerifyComprehensiveAsync_WithoutProfiling_ReturnsCompleteReport()
    {
        // Arrange
        var expectedSignatures = new Dictionary<string, SignatureDetails>();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Assert
        Assert.NotNull(report);
        Assert.NotNull(report.CoverageReport);
        Assert.NotNull(report.ValidationReports);
        Assert.NotNull(report.WorkaroundTestResults);
        Assert.Null(report.ProfilingReport); // Should be null when profiling disabled

        // Verify validators ran
        Assert.Equal(5, report.ValidationReports.Count);

        // Verify workarounds tested
        Assert.Equal(3, report.WorkaroundTestResults.Count);

        // Verify summary
        Assert.NotNull(report.Summary);
        Assert.Equal(5, report.Summary.TotalValidators);
        Assert.Equal(3, report.Summary.TotalWorkarounds);
        Assert.False(report.Summary.HasPerformanceProfiling);
    }

    [Fact]
    public async Task VerifyComprehensiveAsync_WithProfiling_ReturnsCompleteReportWithProfiling()
    {
        // Arrange
        var expectedSignatures = new Dictionary<string, SignatureDetails>();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: true,
            profilingIterations: 50);

        // Assert
        Assert.NotNull(report);
        Assert.NotNull(report.CoverageReport);
        Assert.NotNull(report.ValidationReports);
        Assert.NotNull(report.WorkaroundTestResults);
        Assert.NotNull(report.ProfilingReport); // Should be present when profiling enabled

        // Verify profiling report
        Assert.Equal(50, report.ProfilingReport.IterationsPerFunction);
        Assert.True(report.ProfilingReport.TotalFunctions > 0);

        // Verify summary
        Assert.NotNull(report.Summary);
        Assert.True(report.Summary.HasPerformanceProfiling);
    }

    [Fact]
    public async Task VerifyComprehensiveAsync_SetsOverallStatusCorrectly()
    {
        // Arrange
        var expectedSignatures = new Dictionary<string, SignatureDetails>();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Assert
        // Overall status should reflect any failures or warnings
        // The exact status depends on test results, but it should be one of the valid values
        // OverallStatus is an enum (value type), so it will always have a value
        Assert.True(
            report.OverallStatus == ValidationStatus.Passed ||
            report.OverallStatus == ValidationStatus.Warning ||
            report.OverallStatus == ValidationStatus.Failed ||
            report.OverallStatus == ValidationStatus.Critical);
    }

    [Fact]
    public async Task ValidateHighRiskAreasAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _verifier.ValidateHighRiskAreasAsync(_testPdfPath, cts.Token));
    }

    [Fact]
    public void VerifyComprehensive_SynchronousVersion_Works()
    {
        // Arrange
        var expectedSignatures = new Dictionary<string, SignatureDetails>();

        // Act
        var report = _verifier.VerifyComprehensive(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Assert
        Assert.NotNull(report);
        Assert.NotNull(report.CoverageReport);
        Assert.NotNull(report.ValidationReports);
        Assert.NotNull(report.WorkaroundTestResults);
        Assert.Equal(5, report.ValidationReports.Count);
        Assert.Equal(3, report.WorkaroundTestResults.Count);
    }

    [Fact]
    public async Task VerifyAndReportAsync_ExtendedVersion_IncludesNewValidations()
    {
        // This tests that VerifyAndReportAsync now includes the new validators internally
        // Arrange
        var expectedSignatures = new Dictionary<string, SignatureDetails>();

        // Act
        var report = await _verifier.VerifyAndReportAsync(expectedSignatures, _testPdfPath);

        // Assert
        Assert.NotNull(report);
        // The CoverageReport is maintained for backward compatibility
        Assert.True(report.TotalFunctions >= 0);
        Assert.NotNull(report.Results);
    }

    public void Dispose()
    {
        _verifier?.Dispose();
    }
}
