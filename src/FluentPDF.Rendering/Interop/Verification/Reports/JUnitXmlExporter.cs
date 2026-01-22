using System.Xml.Linq;

namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Exports validation reports to JUnit XML format compatible with GitHub Actions and Azure DevOps.
/// </summary>
public class JUnitXmlExporter : IReportExporter<ValidationReport>
{
    /// <summary>
    /// Exports the validation report to a JUnit XML file.
    /// </summary>
    /// <param name="report">The validation report to export.</param>
    /// <param name="outputPath">The file path where the XML should be saved.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task ExportAsync(ValidationReport report, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var xml = GenerateJUnitXml(report);

        await Task.Run(() => xml.Save(outputPath), cancellationToken);
    }

    /// <summary>
    /// Generates JUnit XML document from validation report.
    /// </summary>
    private static XDocument GenerateJUnitXml(ValidationReport report)
    {
        var totalTests = report.Summary.TotalTests;
        var failures = report.Summary.FailedCount + report.Summary.CriticalCount;
        var errors = report.Summary.CriticalCount;
        var timeInSeconds = 0.0; // No timing data available

        // Create root testsuites element
        var testsuites = new XElement("testsuites",
            new XAttribute("name", "Marshaling Validation"),
            new XAttribute("tests", totalTests),
            new XAttribute("failures", failures),
            new XAttribute("errors", errors),
            new XAttribute("time", timeInSeconds.ToString("F3"))
        );

        // Create testsuite element for this validator
        var testsuite = new XElement("testsuite",
            new XAttribute("name", report.ValidatorName),
            new XAttribute("tests", totalTests),
            new XAttribute("failures", failures),
            new XAttribute("errors", errors),
            new XAttribute("time", timeInSeconds.ToString("F3")),
            new XAttribute("timestamp", report.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"))
        );

        // Add properties section
        var properties = new XElement("properties",
            new XElement("property",
                new XAttribute("name", "target_area"),
                new XAttribute("value", report.TargetArea)
            ),
            new XElement("property",
                new XAttribute("name", "validator_name"),
                new XAttribute("value", report.ValidatorName)
            ),
            new XElement("property",
                new XAttribute("name", "overall_status"),
                new XAttribute("value", report.Summary.OverallStatus.ToString())
            ),
            new XElement("property",
                new XAttribute("name", "pass_percentage"),
                new XAttribute("value", report.Summary.PassPercentage.ToString("F2"))
            )
        );
        testsuite.Add(properties);

        // Add test cases for each validation result
        foreach (var (area, results) in report.ResultsByArea)
        {
            foreach (var result in results)
            {
                var testcase = new XElement("testcase",
                    new XAttribute("name", result.TestName),
                    new XAttribute("classname", $"{report.ValidatorName}.{area}"),
                    new XAttribute("time", "0.000")
                );

                if (!result.Passed)
                {
                    var failureType = result.Severity == ValidationSeverity.Critical ? "error" : "failure";
                    var failureElement = new XElement(failureType,
                        new XAttribute("message", result.Message ?? "Validation failed"),
                        new XAttribute("type", result.Severity.ToString())
                    );

                    // Add detailed error information
                    var details = BuildFailureDetails(result);
                    if (!string.IsNullOrEmpty(details))
                    {
                        failureElement.Add(new XCData(details));
                    }

                    testcase.Add(failureElement);
                }
                else if (result.Severity == ValidationSeverity.Warning)
                {
                    // Add warning as system-out
                    var warning = new XElement("system-out",
                        new XCData($"Warning: {result.Message}")
                    );
                    testcase.Add(warning);
                }

                testsuite.Add(testcase);
            }
        }

        testsuites.Add(testsuite);

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            testsuites
        );
    }

    /// <summary>
    /// Builds detailed failure information for a validation result.
    /// </summary>
    private static string BuildFailureDetails(ValidationResult result)
    {
        var details = new List<string>();

        if (!string.IsNullOrEmpty(result.ErrorDetails))
        {
            details.Add($"Error Details: {result.ErrorDetails}");
        }

        if (result.ExpectedValue != null)
        {
            details.Add($"Expected: {result.ExpectedValue}");
        }

        if (result.ActualValue != null)
        {
            details.Add($"Actual: {result.ActualValue}");
        }

        if (!string.IsNullOrEmpty(result.SuggestedFix))
        {
            details.Add($"Suggested Fix: {result.SuggestedFix}");
        }

        if (!string.IsNullOrEmpty(result.DocumentationUrl))
        {
            details.Add($"Documentation: {result.DocumentationUrl}");
        }

        if (result.Context != null && result.Context.Count > 0)
        {
            details.Add("Context:");
            foreach (var (key, value) in result.Context)
            {
                details.Add($"  {key}: {value}");
            }
        }

        return string.Join(Environment.NewLine, details);
    }
}
