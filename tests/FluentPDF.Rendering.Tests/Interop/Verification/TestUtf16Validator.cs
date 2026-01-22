// Quick verification script to test Utf16MarshalingValidator
// Run this manually with: dotnet run in a console project

using FluentPDF.Rendering.Interop.Verification.Validators;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

public static class TestUtf16Validator
{
    public static async Task RunQuickTest()
    {
        var logger = NullLogger<Utf16MarshalingValidator>.Instance;
        var validator = new Utf16MarshalingValidator(logger);

        Console.WriteLine("Testing Utf16MarshalingValidator...");
        Console.WriteLine($"Validator Name: {validator.ValidatorName}");
        Console.WriteLine($"Target Area: {validator.TargetArea}");

        var report = await validator.ValidateAsync();

        Console.WriteLine($"\nValidation Report:");
        Console.WriteLine($"  Total Tests: {report.Summary.TotalTests}");
        Console.WriteLine($"  Passed: {report.Summary.PassedCount}");
        Console.WriteLine($"  Failed: {report.Summary.FailedCount}");
        Console.WriteLine($"  Warnings: {report.Summary.WarningCount}");
        Console.WriteLine($"  Critical: {report.Summary.CriticalCount}");
        Console.WriteLine($"  Pass Percentage: {report.Summary.PassPercentage:F2}%");
        Console.WriteLine($"  All Passed: {report.AllPassed}");

        Console.WriteLine($"\nResults by Area:");
        foreach (var area in report.ResultsByArea)
        {
            Console.WriteLine($"  {area.Key}: {area.Value.Count} tests");
            foreach (var result in area.Value)
            {
                var status = result.Passed ? "✓" : "✗";
                Console.WriteLine($"    {status} {result.TestName}");
            }
        }
    }
}
