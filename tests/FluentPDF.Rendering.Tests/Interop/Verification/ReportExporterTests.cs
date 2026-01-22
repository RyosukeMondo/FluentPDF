using System.Text.Json;
using System.Xml.Linq;
using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

public class ReportExporterTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ValidationReport _sampleReport;

    public ReportExporterTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ReportExporterTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        _sampleReport = CreateSampleReport();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task JsonReportExporter_ExportsValidJson()
    {
        // Arrange
        var exporter = new JsonReportExporter();
        var outputPath = Path.Combine(_tempDirectory, "report.json");

        // Act
        await exporter.ExportAsync(_sampleReport, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));

        var json = await File.ReadAllTextAsync(outputPath);
        Assert.NotEmpty(json);

        // Verify JSON is valid and deserializable
        var deserializedReport = JsonSerializer.Deserialize<ValidationReport>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(deserializedReport);
        Assert.Equal(_sampleReport.ValidatorName, deserializedReport.ValidatorName);
        Assert.Equal(_sampleReport.TargetArea, deserializedReport.TargetArea);
        Assert.Equal(_sampleReport.Summary.TotalTests, deserializedReport.Summary.TotalTests);
    }

    [Fact]
    public async Task JsonReportExporter_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var exporter = new JsonReportExporter();
        var subDirectory = Path.Combine(_tempDirectory, "nested", "path");
        var outputPath = Path.Combine(subDirectory, "report.json");

        Assert.False(Directory.Exists(subDirectory));

        // Act
        await exporter.ExportAsync(_sampleReport, outputPath);

        // Assert
        Assert.True(Directory.Exists(subDirectory));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task JsonReportExporter_ThrowsOnNullReport()
    {
        // Arrange
        var exporter = new JsonReportExporter();
        var outputPath = Path.Combine(_tempDirectory, "report.json");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await exporter.ExportAsync(null!, outputPath));
    }

    [Fact]
    public async Task JUnitXmlExporter_ExportsValidXml()
    {
        // Arrange
        var exporter = new JUnitXmlExporter();
        var outputPath = Path.Combine(_tempDirectory, "junit.xml");

        // Act
        await exporter.ExportAsync(_sampleReport, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));

        // Verify XML is valid and parseable
        var xml = XDocument.Load(outputPath);
        Assert.NotNull(xml.Root);
        Assert.Equal("testsuites", xml.Root.Name.LocalName);

        var testsuite = xml.Root.Element("testsuite");
        Assert.NotNull(testsuite);
        Assert.Equal(_sampleReport.ValidatorName, testsuite.Attribute("name")?.Value);
        Assert.Equal(_sampleReport.Summary.TotalTests.ToString(), testsuite.Attribute("tests")?.Value);
    }

    [Fact]
    public async Task JUnitXmlExporter_IncludesFailuresAsErrors()
    {
        // Arrange
        var exporter = new JUnitXmlExporter();
        var outputPath = Path.Combine(_tempDirectory, "junit-failures.xml");

        // Act
        await exporter.ExportAsync(_sampleReport, outputPath);

        // Assert
        var xml = XDocument.Load(outputPath);
        var testcases = xml.Descendants("testcase").ToList();

        Assert.NotEmpty(testcases);

        // Check for failure/error elements
        var failedTestcases = testcases.Where(tc =>
            tc.Element("failure") != null || tc.Element("error") != null).ToList();

        Assert.NotEmpty(failedTestcases);

        var firstFailure = failedTestcases.First();
        var failureElement = firstFailure.Element("failure") ?? firstFailure.Element("error");
        Assert.NotNull(failureElement);
        Assert.NotNull(failureElement.Attribute("message"));
    }

    [Fact]
    public async Task JUnitXmlExporter_IncludesProperties()
    {
        // Arrange
        var exporter = new JUnitXmlExporter();
        var outputPath = Path.Combine(_tempDirectory, "junit-properties.xml");

        // Act
        await exporter.ExportAsync(_sampleReport, outputPath);

        // Assert
        var xml = XDocument.Load(outputPath);
        var properties = xml.Descendants("properties").FirstOrDefault();
        Assert.NotNull(properties);

        var propertyElements = properties.Elements("property").ToList();
        Assert.NotEmpty(propertyElements);

        var targetAreaProperty = propertyElements.FirstOrDefault(p =>
            p.Attribute("name")?.Value == "target_area");
        Assert.NotNull(targetAreaProperty);
        Assert.Equal(_sampleReport.TargetArea, targetAreaProperty.Attribute("value")?.Value);
    }

    [Fact]
    public async Task HtmlReportGenerator_ExportsValidHtml()
    {
        // Arrange
        var generator = new HtmlReportGenerator();
        var outputPath = Path.Combine(_tempDirectory, "report.html");

        // Act
        await generator.ExportAsync(_sampleReport, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));

        var html = await File.ReadAllTextAsync(outputPath);
        Assert.NotEmpty(html);

        // Verify HTML structure
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html", html);
        Assert.Contains("</html>", html);
        Assert.Contains(_sampleReport.ValidatorName, html);
        Assert.Contains(_sampleReport.TargetArea, html);
    }

    [Fact]
    public async Task HtmlReportGenerator_IncludesEmbeddedCss()
    {
        // Arrange
        var generator = new HtmlReportGenerator();
        var outputPath = Path.Combine(_tempDirectory, "report-styled.html");

        // Act
        await generator.ExportAsync(_sampleReport, outputPath);

        // Assert
        var html = await File.ReadAllTextAsync(outputPath);

        Assert.Contains("<style>", html);
        Assert.Contains("</style>", html);
        Assert.Contains("background-color", html);
        Assert.Contains(".status-pass", html);
        Assert.Contains(".status-fail", html);
    }

    [Fact]
    public async Task HtmlReportGenerator_ColorCodesSeverityLevels()
    {
        // Arrange
        var generator = new HtmlReportGenerator();
        var outputPath = Path.Combine(_tempDirectory, "report-severity.html");

        // Act
        await generator.ExportAsync(_sampleReport, outputPath);

        // Assert
        var html = await File.ReadAllTextAsync(outputPath);

        // Verify severity classes exist in CSS
        Assert.Contains("severity-info", html);
        Assert.Contains("severity-warning", html);
        Assert.Contains("severity-error", html);
        Assert.Contains("severity-critical", html);
    }

    [Fact]
    public async Task HtmlReportGenerator_DisplaysSummaryStatistics()
    {
        // Arrange
        var generator = new HtmlReportGenerator();
        var outputPath = Path.Combine(_tempDirectory, "report-summary.html");

        // Act
        await generator.ExportAsync(_sampleReport, outputPath);

        // Assert
        var html = await File.ReadAllTextAsync(outputPath);

        Assert.Contains("Total Tests", html);
        Assert.Contains("Passed", html);
        Assert.Contains("Failed", html);
        Assert.Contains("Warnings", html);
        Assert.Contains("Critical", html);
        Assert.Contains(_sampleReport.Summary.TotalTests.ToString(), html);
    }

    [Fact]
    public async Task HtmlReportGenerator_HandlesHtmlSpecialCharacters()
    {
        // Arrange
        var reportWithSpecialChars = new ValidationReport
        {
            ValidatorName = "Test<Validator>",
            TargetArea = "Area & \"Tests\"",
            Summary = new ValidationSummary
            {
                TotalTests = 1,
                PassedCount = 0,
                FailedCount = 1,
                WarningCount = 0,
                CriticalCount = 0
            },
            ResultsByArea = new Dictionary<string, List<ValidationResult>>
            {
                ["Test<Area>"] = new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        TestName = "Test with <html> & \"quotes\"",
                        Passed = false,
                        Message = "Error with <tags> & symbols"
                    }
                }
            }
        };

        var generator = new HtmlReportGenerator();
        var outputPath = Path.Combine(_tempDirectory, "report-escaped.html");

        // Act
        await generator.ExportAsync(reportWithSpecialChars, outputPath);

        // Assert
        var html = await File.ReadAllTextAsync(outputPath);

        // Verify HTML entities are properly encoded
        Assert.Contains("Test&lt;Validator&gt;", html);
        Assert.Contains("Area &amp; &quot;Tests&quot;", html);
        Assert.DoesNotContain("Test<Validator>", html.Replace("<title>", "").Replace("</title>", "")); // Except in title tag
    }

    [Fact]
    public async Task AllExporters_HandleEmptyResults()
    {
        // Arrange
        var emptyReport = new ValidationReport
        {
            ValidatorName = "EmptyValidator",
            TargetArea = "Empty Area",
            Summary = new ValidationSummary
            {
                TotalTests = 0,
                PassedCount = 0,
                FailedCount = 0,
                WarningCount = 0,
                CriticalCount = 0
            },
            ResultsByArea = new Dictionary<string, List<ValidationResult>>()
        };

        var jsonExporter = new JsonReportExporter();
        var xmlExporter = new JUnitXmlExporter();
        var htmlGenerator = new HtmlReportGenerator();

        var jsonPath = Path.Combine(_tempDirectory, "empty.json");
        var xmlPath = Path.Combine(_tempDirectory, "empty.xml");
        var htmlPath = Path.Combine(_tempDirectory, "empty.html");

        // Act
        await jsonExporter.ExportAsync(emptyReport, jsonPath);
        await xmlExporter.ExportAsync(emptyReport, xmlPath);
        await htmlGenerator.ExportAsync(emptyReport, htmlPath);

        // Assert
        Assert.True(File.Exists(jsonPath));
        Assert.True(File.Exists(xmlPath));
        Assert.True(File.Exists(htmlPath));

        var json = await File.ReadAllTextAsync(jsonPath);
        var xml = XDocument.Load(xmlPath);
        var html = await File.ReadAllTextAsync(htmlPath);

        Assert.NotEmpty(json);
        Assert.NotNull(xml.Root);
        Assert.NotEmpty(html);
    }

    /// <summary>
    /// Creates a sample validation report for testing.
    /// </summary>
    private static ValidationReport CreateSampleReport()
    {
        return new ValidationReport
        {
            ValidatorName = "TestValidator",
            TargetArea = "UTF-16 Marshaling",
            Summary = new ValidationSummary
            {
                TotalTests = 10,
                PassedCount = 7,
                FailedCount = 2,
                WarningCount = 1,
                CriticalCount = 1
            },
            ResultsByArea = new Dictionary<string, List<ValidationResult>>
            {
                ["Bookmarks"] = new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        TestName = "Emoji in bookmark title",
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = "Emoji characters marshaled correctly"
                    },
                    new ValidationResult
                    {
                        TestName = "Null character handling",
                        Passed = false,
                        Severity = ValidationSeverity.Error,
                        Message = "Null character not properly terminated",
                        ErrorDetails = "Expected null terminator at position 10, found 0x00 at position 8",
                        SuggestedFix = "Ensure buffer size includes null terminator",
                        DocumentationUrl = "https://docs.example.com/marshaling#null-terminators",
                        ExpectedValue = "10",
                        ActualValue = "8"
                    }
                },
                ["Text Search"] = new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        TestName = "Large text search",
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = "Large text search completed successfully"
                    },
                    new ValidationResult
                    {
                        TestName = "Unicode normalization",
                        Passed = false,
                        Severity = ValidationSeverity.Critical,
                        Message = "Unicode normalization failed",
                        ErrorDetails = "AccessViolationException thrown during text search",
                        SuggestedFix = "Apply Unicode NFC normalization before marshaling",
                        Context = new Dictionary<string, string>
                        {
                            ["SearchTerm"] = "café",
                            ["NormalizationForm"] = "NFD",
                            ["ExpectedForm"] = "NFC"
                        }
                    }
                },
                ["Form Fields"] = new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        TestName = "Form field text extraction",
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = "Form field text extracted successfully"
                    },
                    new ValidationResult
                    {
                        TestName = "Long form field value",
                        Passed = true,
                        Severity = ValidationSeverity.Warning,
                        Message = "Form field value is very long (10000 characters)",
                        SuggestedFix = "Consider implementing pagination for long values"
                    }
                }
            }
        };
    }
}
