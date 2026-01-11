using System.Xml.Linq;
using FluentPDF.QualityAgent.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.QualityAgent.Parsers;

public class TrxParser
{
    public Result<TestResults> Parse(string trxFilePath)
    {
        try
        {
            if (!File.Exists(trxFilePath))
            {
                return Result.Fail<TestResults>($"TRX file not found: {trxFilePath}");
            }

            Log.Information("Parsing TRX file: {TrxFile}", trxFilePath);

            var doc = XDocument.Load(trxFilePath);
            var root = doc.Root;

            if (root == null)
            {
                return Result.Fail<TestResults>("TRX file has no root element");
            }

            var ns = root.Name.Namespace;

            // Extract all test results
            var unitTestResults = doc.Descendants(ns + "UnitTestResult").ToList();

            if (!unitTestResults.Any())
            {
                Log.Warning("No test results found in TRX file");
            }

            var total = unitTestResults.Count;
            var passed = unitTestResults.Count(r => r.Attribute("outcome")?.Value == "Passed");
            var failed = unitTestResults.Count(r => r.Attribute("outcome")?.Value == "Failed");
            var skipped = unitTestResults.Count(r =>
            {
                var outcome = r.Attribute("outcome")?.Value;
                return outcome == "NotExecuted" || outcome == "Skipped";
            });

            // Extract failure details
            var failures = unitTestResults
                .Where(r => r.Attribute("outcome")?.Value == "Failed")
                .Select(r =>
                {
                    var output = r.Element(ns + "Output");
                    var errorInfo = output?.Element(ns + "ErrorInfo");

                    return new TestFailure
                    {
                        TestName = r.Attribute("testName")?.Value ?? "Unknown Test",
                        ErrorMessage = errorInfo?.Element(ns + "Message")?.Value,
                        StackTrace = errorInfo?.Element(ns + "StackTrace")?.Value
                    };
                })
                .ToList();

            var results = new TestResults
            {
                Total = total,
                Passed = passed,
                Failed = failed,
                Skipped = skipped,
                Failures = failures
            };

            Log.Information(
                "TRX parsing completed: Total={Total}, Passed={Passed}, Failed={Failed}, Skipped={Skipped}",
                total, passed, failed, skipped);

            return Result.Ok(results);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse TRX file: {TrxFile}", trxFilePath);
            return Result.Fail<TestResults>($"TRX parsing error: {ex.Message}");
        }
    }
}
