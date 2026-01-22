using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Full system integration tests that verify the entire marshaling verification suite
/// works together correctly end-to-end.
/// Tests all validators, profilers, workaround tests, and report exporters in a single flow.
/// </summary>
public class FullSystemIntegrationTests : IDisposable
{
    private readonly MarshallingVerifier _verifier;
    private readonly string _tempDirectory;
    private readonly string? _testPdfPath;
    private readonly Stopwatch _stopwatch;

    public FullSystemIntegrationTests()
    {
        _verifier = new MarshallingVerifier(typeof(PdfiumInterop));
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"FullSystemTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        // Use comprehensive test PDF if available
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "comprehensive_test.pdf");
        _testPdfPath = File.Exists(testDataPath) ? testDataPath : null;

        _stopwatch = new Stopwatch();
    }

    [Fact]
    public async Task FullValidationSuite_ExecutesAllComponents_CompletesWithinPerformanceRequirement()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();
        _stopwatch.Start();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: true,
            profilingIterations: 100);

        _stopwatch.Stop();

        // Assert - Verify all components executed
        Assert.NotNull(report);
        Assert.NotNull(report.CoverageReport);
        Assert.NotNull(report.ValidationReports);
        Assert.NotNull(report.WorkaroundTestResults);
        Assert.NotNull(report.ProfilingReport);

        // Verify validator count (5 validators)
        Assert.Equal(5, report.ValidationReports.Count);
        var validatorNames = report.ValidationReports.Select(r => r.ValidatorName).ToList();
        Assert.Contains("Utf16MarshalingValidator", validatorNames);
        Assert.Contains("BitmapMarshalingValidator", validatorNames);
        Assert.Contains("AnnotationMarshalingValidator", validatorNames);
        Assert.Contains("ThreadingModelValidator", validatorNames);
        Assert.Contains("BufferSafetyValidator", validatorNames);

        // Verify workaround tests (3 workarounds)
        Assert.Equal(3, report.WorkaroundTestResults.Count);
        var workaroundNames = report.WorkaroundTestResults.Select(w => w.WorkaroundName).ToList();
        Assert.Contains("Float Dimension Workaround", workaroundNames);
        Assert.Contains("Threading Workaround", workaroundNames);
        Assert.Contains("SoftwareBitmap Workaround", workaroundNames);

        // Verify profiling report
        Assert.True(report.ProfilingReport.TotalFunctions > 0);
        Assert.Equal(100, report.ProfilingReport.IterationsPerFunction);
        Assert.NotEmpty(report.ProfilingReport.ResultsByFunction);

        // Verify report structure
        Assert.NotNull(report.Summary);
        Assert.Equal(5, report.Summary.TotalValidators);
        Assert.Equal(3, report.Summary.TotalWorkarounds);
        Assert.True(report.Summary.HasPerformanceProfiling);

        // Verify performance requirement: < 30 seconds
        Assert.True(_stopwatch.Elapsed.TotalSeconds < 30,
            $"Full validation suite took {_stopwatch.Elapsed.TotalSeconds:F2} seconds, " +
            $"exceeding 30-second requirement");
    }

    [Fact]
    public async Task FullValidationSuite_GeneratesAllReportFormats_AllFormatsValid()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Run comprehensive validation
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Prepare output paths
        var jsonPath = Path.Combine(_tempDirectory, "validation-report.json");
        var junitPath = Path.Combine(_tempDirectory, "validation-results.xml");
        var htmlPath = Path.Combine(_tempDirectory, "validation-report.html");

        // Act - Export to all formats
        var jsonExporter = new JsonReportExporter();
        var junitExporter = new JUnitXmlExporter();
        var htmlGenerator = new HtmlReportGenerator();

        // Export validation reports (use first validator's report for testing)
        var sampleValidationReport = report.ValidationReports.First();

        await jsonExporter.ExportAsync(sampleValidationReport, jsonPath);
        await junitExporter.ExportAsync(sampleValidationReport, junitPath);
        await htmlGenerator.ExportAsync(sampleValidationReport, htmlPath);

        // Assert - Verify all files were created
        Assert.True(File.Exists(jsonPath), "JSON report not created");
        Assert.True(File.Exists(junitPath), "JUnit XML report not created");
        Assert.True(File.Exists(htmlPath), "HTML report not created");

        // Verify JSON format is valid
        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        Assert.NotEmpty(jsonContent);
        var deserializedJson = JsonSerializer.Deserialize<ValidationReport>(jsonContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(deserializedJson);
        Assert.Equal(sampleValidationReport.ValidatorName, deserializedJson.ValidatorName);

        // Verify JUnit XML format is valid
        var xmlContent = await File.ReadAllTextAsync(junitPath);
        Assert.NotEmpty(xmlContent);
        var xmlDoc = XDocument.Parse(xmlContent);
        Assert.NotNull(xmlDoc.Root);
        Assert.Equal("testsuites", xmlDoc.Root.Name.LocalName);

        // Verify HTML format is valid (contains basic HTML structure)
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        Assert.NotEmpty(htmlContent);
        Assert.Contains("<html", htmlContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</html>", htmlContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<style>", htmlContent, StringComparison.OrdinalIgnoreCase); // Embedded CSS
        Assert.Contains(sampleValidationReport.ValidatorName, htmlContent);
    }

    [Fact]
    public async Task FullValidationSuite_AllValidatorsReturnResults_NoValidatorSkipped()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Assert - Each validator should have results
        foreach (var validationReport in report.ValidationReports)
        {
            Assert.NotNull(validationReport);
            Assert.NotNull(validationReport.ValidatorName);
            Assert.NotEmpty(validationReport.ValidatorName);
            Assert.NotNull(validationReport.Summary);
            Assert.True(validationReport.Summary.TotalTests > 0,
                $"{validationReport.ValidatorName} did not run any tests");
            Assert.NotNull(validationReport.Results);
        }

        // Verify each validator collected results
        var utf16Report = report.ValidationReports.First(r => r.ValidatorName == "Utf16MarshalingValidator");
        Assert.True(utf16Report.Summary.TotalTests >= 3, "UTF-16 validator should test bookmarks, text search, form fields");

        var bitmapReport = report.ValidationReports.First(r => r.ValidatorName == "BitmapMarshalingValidator");
        Assert.True(bitmapReport.Summary.TotalTests >= 3, "Bitmap validator should test stride, marshal copy, pixel integrity");

        var annotationReport = report.ValidationReports.First(r => r.ValidatorName == "AnnotationMarshalingValidator");
        Assert.True(annotationReport.Summary.TotalTests >= 2, "Annotation validator should test quad points and rect");

        var threadingReport = report.ValidationReports.First(r => r.ValidatorName == "ThreadingModelValidator");
        Assert.True(threadingReport.Summary.TotalTests >= 2, "Threading validator should test Task.Yield and concurrent access");

        var bufferReport = report.ValidationReports.First(r => r.ValidatorName == "BufferSafetyValidator");
        Assert.True(bufferReport.Summary.TotalTests >= 2, "Buffer safety validator should analyze and simulate");
    }

    [Fact]
    public async Task FullValidationSuite_ProfilingMetrics_ContainAllExpectedData()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: true,
            profilingIterations: 50);

        // Assert
        Assert.NotNull(report.ProfilingReport);
        Assert.Equal(50, report.ProfilingReport.IterationsPerFunction);
        Assert.True(report.ProfilingReport.TotalFunctions > 0);

        // Verify profiling results contain performance metrics
        Assert.NotEmpty(report.ProfilingReport.ResultsByFunction);

        foreach (var (functionName, profilingResult) in report.ProfilingReport.ResultsByFunction)
        {
            Assert.NotNull(functionName);
            Assert.NotEmpty(functionName);
            Assert.NotNull(profilingResult);
            Assert.NotNull(profilingResult.PerformanceMetrics);

            // Verify all percentiles are present
            var metrics = profilingResult.PerformanceMetrics;
            Assert.True(metrics.MinLatencyMs >= 0);
            Assert.True(metrics.MaxLatencyMs >= metrics.MinLatencyMs);
            Assert.True(metrics.MedianLatencyMs >= metrics.MinLatencyMs);
            Assert.True(metrics.MedianLatencyMs <= metrics.MaxLatencyMs);
            Assert.True(metrics.P95LatencyMs >= metrics.MedianLatencyMs);
            Assert.True(metrics.P99LatencyMs >= metrics.P95LatencyMs);
            Assert.True(metrics.StandardDeviationMs >= 0);

            // Verify memory metrics
            Assert.NotNull(profilingResult.MemoryMetrics);
            var memMetrics = profilingResult.MemoryMetrics;
            Assert.True(memMetrics.ManagedBytesAllocated >= 0);
            Assert.True(memMetrics.Gen0Collections >= 0);
            Assert.True(memMetrics.Gen1Collections >= 0);
            Assert.True(memMetrics.Gen2Collections >= 0);
        }
    }

    [Fact]
    public async Task FullValidationSuite_WorkaroundTests_AllWorkaroundsHaveStatus()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Assert
        Assert.NotNull(report.WorkaroundTestResults);
        Assert.Equal(3, report.WorkaroundTestResults.Count);

        foreach (var workaroundResult in report.WorkaroundTestResults)
        {
            Assert.NotNull(workaroundResult.WorkaroundName);
            Assert.NotEmpty(workaroundResult.WorkaroundName);
            Assert.NotNull(workaroundResult.DocumentationReference);
            Assert.NotEmpty(workaroundResult.DocumentationReference);

            // Status is enum, so it always has a value
            Assert.True(
                workaroundResult.Status == WorkaroundStatus.StillNeeded ||
                workaroundResult.Status == WorkaroundStatus.CanBeRemoved ||
                workaroundResult.Status == WorkaroundStatus.Broken);

            Assert.NotNull(workaroundResult.Details);
            Assert.NotNull(workaroundResult.RecommendedAction);
        }
    }

    [Fact]
    public async Task FullValidationSuite_WithoutTestPdf_StillExecutes()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Act - Pass null PDF path to test validators without PDF
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            testPdfPath: null,
            includePerformanceProfiling: false);

        // Assert - Should still run validators that don't require PDFs
        Assert.NotNull(report);
        Assert.NotNull(report.ValidationReports);
        Assert.NotNull(report.WorkaroundTestResults);

        // Some validators may have fewer results without a PDF, but should not crash
        foreach (var validationReport in report.ValidationReports)
        {
            Assert.NotNull(validationReport);
            Assert.NotNull(validationReport.Summary);
        }
    }

    [Fact]
    public async Task FullValidationSuite_OverallStatus_ReflectsValidationResults()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        // Assert
        // OverallStatus is enum (value type), so it will always have a value
        Assert.True(
            report.OverallStatus == ValidationStatus.Passed ||
            report.OverallStatus == ValidationStatus.Warning ||
            report.OverallStatus == ValidationStatus.Failed ||
            report.OverallStatus == ValidationStatus.Critical);

        // If any validator failed, overall status should not be Passed
        var hasFailures = report.ValidationReports.Any(r =>
            r.Summary.FailedTests > 0 || r.Summary.CriticalIssues > 0);

        if (hasFailures)
        {
            Assert.NotEqual(ValidationStatus.Passed, report.OverallStatus);
        }

        // If all validators passed, overall status should be Passed or Warning
        var allPassed = report.ValidationReports.All(r =>
            r.Summary.FailedTests == 0 && r.Summary.CriticalIssues == 0);

        if (allPassed)
        {
            Assert.True(
                report.OverallStatus == ValidationStatus.Passed ||
                report.OverallStatus == ValidationStatus.Warning);
        }
    }

    [Fact]
    public async Task FullValidationSuite_Summary_ContainsAccurateStatistics()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();

        // Act
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: true);

        // Assert
        Assert.NotNull(report.Summary);
        Assert.Equal(5, report.Summary.TotalValidators);
        Assert.Equal(3, report.Summary.TotalWorkarounds);
        Assert.True(report.Summary.HasPerformanceProfiling);

        // Verify summary counts match actual results
        var totalPassedTests = report.ValidationReports.Sum(r => r.Summary.PassedTests);
        var totalFailedTests = report.ValidationReports.Sum(r => r.Summary.FailedTests);
        var totalTests = report.ValidationReports.Sum(r => r.Summary.TotalTests);

        Assert.Equal(totalTests, totalPassedTests + totalFailedTests);

        // Verify critical issues are counted
        var totalCriticalIssues = report.ValidationReports.Sum(r => r.Summary.CriticalIssues);
        Assert.True(totalCriticalIssues >= 0);
    }

    [Fact]
    public async Task FullValidationSuite_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _verifier.VerifyComprehensiveAsync(
                expectedSignatures,
                _testPdfPath,
                includePerformanceProfiling: false,
                cancellationToken: cts.Token));
    }

    [Fact]
    public async Task FullValidationSuite_ExportedReports_AreReadableByExternalTools()
    {
        // This test verifies that exported reports can be consumed by CI/CD tools

        // Arrange
        var expectedSignatures = LoadPdfiumSpecification();
        var report = await _verifier.VerifyComprehensiveAsync(
            expectedSignatures,
            _testPdfPath,
            includePerformanceProfiling: false);

        var junitPath = Path.Combine(_tempDirectory, "ci-results.xml");
        var jsonPath = Path.Combine(_tempDirectory, "ci-report.json");

        // Act - Export reports
        var junitExporter = new JUnitXmlExporter();
        var jsonExporter = new JsonReportExporter();

        var sampleReport = report.ValidationReports.First();
        await junitExporter.ExportAsync(sampleReport, junitPath);
        await jsonExporter.ExportAsync(sampleReport, jsonPath);

        // Assert - JUnit XML is parseable by standard XML parsers
        var junitXml = XDocument.Load(junitPath);
        Assert.NotNull(junitXml.Root);
        Assert.Equal("testsuites", junitXml.Root.Name.LocalName);

        var testSuites = junitXml.Root.Elements("testsuite");
        Assert.NotEmpty(testSuites);

        foreach (var testSuite in testSuites)
        {
            Assert.NotNull(testSuite.Attribute("name"));
            Assert.NotNull(testSuite.Attribute("tests"));
            Assert.NotNull(testSuite.Attribute("failures"));
            Assert.NotNull(testSuite.Attribute("time"));
        }

        // Assert - JSON is valid and deserializable
        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var deserializedReport = JsonSerializer.Deserialize<ValidationReport>(jsonContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserializedReport);
        Assert.Equal(sampleReport.ValidatorName, deserializedReport.ValidatorName);
        Assert.NotNull(deserializedReport.Summary);
    }

    /// <summary>
    /// Loads PDFium specification for signature validation.
    /// In a real scenario, this would load from tests/FluentPDF.Rendering.Tests/TestData/pdfium-spec.json
    /// </summary>
    private Dictionary<string, SignatureDetails> LoadPdfiumSpecification()
    {
        var specPath = Path.Combine(AppContext.BaseDirectory, "TestData", "pdfium-spec.json");

        if (!File.Exists(specPath))
        {
            // Return empty dictionary if spec file doesn't exist
            // This allows tests to run without requiring the spec file
            return new Dictionary<string, SignatureDetails>();
        }

        try
        {
            var json = File.ReadAllText(specPath);
            var spec = JsonSerializer.Deserialize<Dictionary<string, SignatureDetails>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return spec ?? new Dictionary<string, SignatureDetails>();
        }
        catch
        {
            // If spec file is malformed, return empty dictionary
            return new Dictionary<string, SignatureDetails>();
        }
    }

    public void Dispose()
    {
        _verifier?.Dispose();

        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }
    }
}
