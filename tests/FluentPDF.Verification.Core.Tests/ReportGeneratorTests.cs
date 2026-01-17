using FluentPDF.Verification.Core;
using FluentResults;
using Serilog;
using Serilog.Core;

namespace FluentPDF.Verification.Core.Tests;

/// <summary>
/// Unit tests for DefaultReportGenerator and ReportGeneratorBase classes.
/// Tests all report formats and error handling scenarios.
/// </summary>
public class ReportGeneratorTests
{
    private readonly ILogger _logger;

    public ReportGeneratorTests()
    {
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    private VerificationSummary CreateSampleSummary(bool includeFailures = false)
    {
        var results = new List<VerificationResult>
        {
            new()
            {
                TestName = "Test 1: Basic Functionality",
                Success = true,
                Duration = TimeSpan.FromMilliseconds(150),
                Metadata = new Dictionary<string, object>
                {
                    ["Category"] = "Functional",
                    ["Priority"] = "High"
                }
            },
            new()
            {
                TestName = "Test 2: Performance Check",
                Success = true,
                Duration = TimeSpan.FromMilliseconds(250)
            }
        };

        if (includeFailures)
        {
            results.Add(new VerificationResult
            {
                TestName = "Test 3: Signature Validation",
                Success = false,
                ErrorMessage = "Function signature mismatch for FPDF_GetPageWidthF",
                SuggestedFix = "Change return type from float to double",
                Duration = TimeSpan.FromMilliseconds(75),
                Metadata = new Dictionary<string, object>
                {
                    ["ExpectedSignature"] = "double FPDF_GetPageWidthF(FPDF_PAGE)",
                    ["ActualSignature"] = "float FPDF_GetPageWidthF(FPDF_PAGE)"
                }
            });
        }

        return new VerificationSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            Results = results,
            TotalDuration = TimeSpan.FromMilliseconds(results.Sum(r => r.Duration.TotalMilliseconds)),
            LibraryVersion = "1.2.3"
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultReportGenerator(null!));
    }

    [Fact]
    public void GenerateConsoleReport_WithValidSummary_ReturnsSuccess()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary();

        // Act
        var result = generator.GenerateConsoleReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateConsoleReport_ContainsExpectedSections()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateConsoleReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("VERIFICATION REPORT");
        result.Value.Should().Contain("Library Version: 1.2.3");
        result.Value.Should().Contain("Total Tests:");
        result.Value.Should().Contain("Passed:");
        result.Value.Should().Contain("Failed:");
        result.Value.Should().Contain("Duration:");
        result.Value.Should().Contain("Status:");
        result.Value.Should().Contain("DETAILED RESULTS");
    }

    [Fact]
    public void GenerateConsoleReport_WithFailures_IncludesErrorsAndFixes()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateConsoleReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("[FAIL]");
        result.Value.Should().Contain("Function signature mismatch");
        result.Value.Should().Contain("Change return type from float to double");
        result.Value.Should().Contain("ExpectedSignature:");
    }

    [Fact]
    public void GenerateConsoleReport_WithPassingTests_ShowsPassStatus()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary();

        // Act
        var result = generator.GenerateConsoleReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("[PASS]");
        result.Value.Should().Contain("Status:          PASS");
    }

    [Fact]
    public void GenerateConsoleReport_WithFailingTests_ShowsFailStatus()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateConsoleReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Status:          FAIL");
    }

    [Fact]
    public void GenerateJsonReport_WithValidSummary_ReturnsValidJson()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary();

        // Act
        var result = generator.GenerateJsonReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();

        // Verify it's valid JSON by deserializing
        var act = () => System.Text.Json.JsonDocument.Parse(result.Value);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateJsonReport_ContainsExpectedFields()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateJsonReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var doc = System.Text.Json.JsonDocument.Parse(result.Value);
        var root = doc.RootElement;

        root.GetProperty("libraryVersion").GetString().Should().Be("1.2.3");
        root.GetProperty("totalTests").GetInt32().Should().Be(3);
        root.GetProperty("passedTests").GetInt32().Should().Be(2);
        root.GetProperty("failedTests").GetInt32().Should().Be(1);
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("durationSeconds").GetDouble().Should().BeGreaterThan(0);

        var results = root.GetProperty("results");
        results.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public void GenerateJsonReport_ResultsContainMetadata()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateJsonReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var doc = System.Text.Json.JsonDocument.Parse(result.Value);
        var root = doc.RootElement;
        var firstResult = root.GetProperty("results")[0];

        firstResult.GetProperty("testName").GetString().Should().NotBeNullOrEmpty();
        firstResult.GetProperty("success").GetBoolean().Should().BeTrue();
        firstResult.GetProperty("durationSeconds").GetDouble().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateHtmlReport_WithValidSummary_ReturnsValidHtml()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary();

        // Act
        var result = generator.GenerateHtmlReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().StartWith("<!DOCTYPE html>");
        result.Value.Should().Contain("</html>");
    }

    [Fact]
    public void GenerateHtmlReport_ContainsExpectedElements()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateHtmlReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<title>Verification Report</title>");
        result.Value.Should().Contain("Verification Report");
        result.Value.Should().Contain("Library Version");
        result.Value.Should().Contain("1.2.3");
        result.Value.Should().Contain("Total Tests");
        result.Value.Should().Contain("Detailed Results");
    }

    [Fact]
    public void GenerateHtmlReport_WithFailures_IncludesErrorStyling()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = CreateSampleSummary(includeFailures: true);

        // Act
        var result = generator.GenerateHtmlReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("class=\"result-item fail\"");
        result.Value.Should().Contain("error-message");
        result.Value.Should().Contain("suggested-fix");
    }

    [Fact]
    public void GenerateHtmlReport_EscapesHtmlInContent()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var summary = new VerificationSummary
        {
            TotalTests = 1,
            PassedTests = 0,
            FailedTests = 1,
            Results = new List<VerificationResult>
            {
                new()
                {
                    TestName = "Test <script>alert('xss')</script>",
                    Success = false,
                    ErrorMessage = "Error with <tag>",
                    Duration = TimeSpan.FromMilliseconds(100)
                }
            },
            TotalDuration = TimeSpan.FromMilliseconds(100),
            LibraryVersion = "1.0"
        };

        // Act
        var result = generator.GenerateHtmlReport(summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain("<script>");
        result.Value.Should().Contain("&lt;script&gt;");
        result.Value.Should().Contain("&lt;tag&gt;");
    }

    [Fact]
    public async Task WriteReportAsync_WithEmptyContent_ReturnsFailure()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            var result = await generator.WriteReportAsync("", outputPath);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Contain("cannot be empty");
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task WriteReportAsync_WithEmptyPath_ReturnsFailure()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);

        // Act
        var result = await generator.WriteReportAsync("Some content", "");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task WriteReportAsync_WithValidInput_CreatesFile()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid()}.txt");
        var content = "Test report content";

        try
        {
            // Act
            var result = await generator.WriteReportAsync(content, outputPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            File.Exists(outputPath).Should().BeTrue();
            var writtenContent = await File.ReadAllTextAsync(outputPath);
            writtenContent.Should().Be(content);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task WriteReportAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        var outputPath = Path.Combine(tempDir, "report.txt");
        var content = "Test content";

        try
        {
            // Act
            var result = await generator.WriteReportAsync(content, outputPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            Directory.Exists(tempDir).Should().BeTrue();
            File.Exists(outputPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WriteReportAsync_WithCancellation_ReturnsFailure()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var outputPath = Path.GetTempFileName();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act
            var result = await generator.WriteReportAsync("Content", outputPath, cts.Token);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Contain("cancelled");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task WriteReportAsync_ThreadSafety_MultipleConcurrentWrites()
    {
        // Arrange
        var generator = new DefaultReportGenerator(_logger);
        var outputPaths = Enumerable.Range(0, 5)
            .Select(_ => Path.Combine(Path.GetTempPath(), $"concurrent_test_{Guid.NewGuid()}.txt"))
            .ToArray();

        try
        {
            // Act - Write to multiple files concurrently
            var tasks = outputPaths.Select((path, i) =>
                generator.WriteReportAsync($"Content {i}", path)
            ).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
            outputPaths.Should().AllSatisfy(path => File.Exists(path).Should().BeTrue());

            // Verify content
            for (int i = 0; i < outputPaths.Length; i++)
            {
                var content = await File.ReadAllTextAsync(outputPaths[i]);
                content.Should().Be($"Content {i}");
            }
        }
        finally
        {
            foreach (var path in outputPaths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    [Fact]
    public void FormatDuration_Milliseconds_FormatsCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(500);

        // Act
        var formatted = TestableReportGenerator.TestFormatDuration(duration);

        // Assert
        formatted.Should().Be("500ms");
    }

    [Fact]
    public void FormatDuration_Seconds_FormatsCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(2.5);

        // Act
        var formatted = TestableReportGenerator.TestFormatDuration(duration);

        // Assert
        formatted.Should().Be("2.50s");
    }

    [Fact]
    public void FormatDuration_Minutes_FormatsCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(3.7);

        // Act
        var formatted = TestableReportGenerator.TestFormatDuration(duration);

        // Assert
        formatted.Should().Be("3.7m");
    }

    // Helper class to expose protected methods for testing
    private class TestableReportGenerator : ReportGeneratorBase
    {
        public TestableReportGenerator(ILogger logger) : base(logger) { }

        public override Result<string> GenerateConsoleReport(VerificationSummary summary) => throw new NotImplementedException();
        public override Result<string> GenerateJsonReport(VerificationSummary summary) => throw new NotImplementedException();
        public override Result<string> GenerateHtmlReport(VerificationSummary summary) => throw new NotImplementedException();

        public static string TestFormatDuration(TimeSpan duration) => FormatDuration(duration);
    }
}
