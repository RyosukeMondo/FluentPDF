using System.Text.Json;
using FluentPDF.QualityAgent.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.QualityAgent.Parsers;

public class SsimParser
{
    public Result<SsimResults> Parse(string ssimFilePath)
    {
        try
        {
            if (!File.Exists(ssimFilePath))
            {
                return Result.Fail<SsimResults>($"SSIM results file not found: {ssimFilePath}");
            }

            Log.Information("Parsing SSIM results file: {SsimFile}", ssimFilePath);

            var jsonContent = File.ReadAllText(ssimFilePath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var tests = new List<SsimTestResult>();

            // Support both array of tests or object with tests property
            JsonElement testsArray;
            if (root.ValueKind == JsonValueKind.Array)
            {
                testsArray = root;
            }
            else if (root.TryGetProperty("tests", out var testsProperty))
            {
                testsArray = testsProperty;
            }
            else if (root.TryGetProperty("results", out var resultsProperty))
            {
                testsArray = resultsProperty;
            }
            else
            {
                return Result.Fail<SsimResults>("SSIM JSON format not recognized. Expected array or object with 'tests'/'results' property.");
            }

            foreach (var testElement in testsArray.EnumerateArray())
            {
                var testResult = ParseTestResult(testElement);
                if (testResult != null)
                {
                    tests.Add(testResult);
                }
            }

            var results = new SsimResults
            {
                Tests = tests
            };

            Log.Information(
                "SSIM parsing completed: Total={Total}, Passed={Passed}, Minor={Minor}, Major={Major}, Critical={Critical}",
                results.Total, results.Passed, results.MinorRegressions, results.MajorRegressions, results.CriticalRegressions);

            return Result.Ok(results);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Failed to parse SSIM JSON file: {SsimFile}", ssimFilePath);
            return Result.Fail<SsimResults>($"SSIM JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse SSIM file: {SsimFile}", ssimFilePath);
            return Result.Fail<SsimResults>($"SSIM parsing error: {ex.Message}");
        }
    }

    private SsimTestResult? ParseTestResult(JsonElement testElement)
    {
        try
        {
            // Extract test name (support multiple property names)
            var testName = testElement.TryGetProperty("testName", out var nameValue) ||
                          testElement.TryGetProperty("name", out nameValue) ||
                          testElement.TryGetProperty("test", out nameValue)
                ? nameValue.GetString() ?? "Unknown Test"
                : "Unknown Test";

            // Extract SSIM score (support multiple property names)
            double ssimScore;
            if (testElement.TryGetProperty("ssimScore", out var scoreValue) ||
                testElement.TryGetProperty("score", out scoreValue) ||
                testElement.TryGetProperty("ssim", out scoreValue))
            {
                if (scoreValue.ValueKind == JsonValueKind.Number)
                {
                    ssimScore = scoreValue.GetDouble();
                }
                else if (scoreValue.ValueKind == JsonValueKind.String &&
                         double.TryParse(scoreValue.GetString(), out var parsedScore))
                {
                    ssimScore = parsedScore;
                }
                else
                {
                    Log.Warning("Invalid SSIM score format for test: {TestName}", testName);
                    return null;
                }
            }
            else
            {
                Log.Warning("No SSIM score found for test: {TestName}", testName);
                return null;
            }

            // Validate score range
            if (ssimScore < 0.0 || ssimScore > 1.0)
            {
                Log.Warning("SSIM score out of range [0.0, 1.0] for test {TestName}: {Score}", testName, ssimScore);
                // Clamp to valid range
                ssimScore = Math.Max(0.0, Math.Min(1.0, ssimScore));
            }

            // Extract image paths (optional)
            var baselineImagePath = testElement.TryGetProperty("baselineImage", out var baselineValue) ||
                                   testElement.TryGetProperty("baseline", out baselineValue)
                ? baselineValue.GetString()
                : null;

            var currentImagePath = testElement.TryGetProperty("currentImage", out var currentValue) ||
                                  testElement.TryGetProperty("current", out currentValue) ||
                                  testElement.TryGetProperty("actual", out currentValue)
                ? currentValue.GetString()
                : null;

            // Classify regression severity
            var regression = ClassifyRegression(ssimScore);

            return new SsimTestResult
            {
                TestName = testName,
                SsimScore = ssimScore,
                BaselineImagePath = baselineImagePath,
                CurrentImagePath = currentImagePath,
                Regression = regression
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to parse SSIM test result");
            return null;
        }
    }

    private RegressionSeverity ClassifyRegression(double ssimScore)
    {
        // Thresholds:
        // < 0.95: Critical
        // < 0.97: Major
        // < 0.99: Minor
        // >= 0.99: None

        if (ssimScore < 0.95)
        {
            return RegressionSeverity.Critical;
        }
        else if (ssimScore < 0.97)
        {
            return RegressionSeverity.Major;
        }
        else if (ssimScore < 0.99)
        {
            return RegressionSeverity.Minor;
        }
        else
        {
            return RegressionSeverity.None;
        }
    }
}
