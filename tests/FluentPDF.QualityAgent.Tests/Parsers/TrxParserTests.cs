using FluentAssertions;
using FluentPDF.QualityAgent.Parsers;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Parsers;

public class TrxParserTests
{
    private readonly TrxParser _parser = new();

    [Fact]
    public void Parse_WithPassedTests_ReturnsCorrectStatistics()
    {
        // Arrange
        var trxPath = Path.Combine("TestData", "passed-tests.trx");

        // Act
        var result = _parser.Parse(trxPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var testResults = result.Value;
        testResults.Total.Should().Be(3);
        testResults.Passed.Should().Be(3);
        testResults.Failed.Should().Be(0);
        testResults.Skipped.Should().Be(0);
        testResults.Failures.Should().BeEmpty();
        testResults.PassRate.Should().Be(100.0);
    }

    [Fact]
    public void Parse_WithFailedTests_ReturnsCorrectStatisticsAndFailures()
    {
        // Arrange
        var trxPath = Path.Combine("TestData", "failed-tests.trx");

        // Act
        var result = _parser.Parse(trxPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var testResults = result.Value;
        testResults.Total.Should().Be(4);
        testResults.Passed.Should().Be(1);
        testResults.Failed.Should().Be(2);
        testResults.Skipped.Should().Be(1);
        testResults.Failures.Should().HaveCount(2);
        testResults.PassRate.Should().Be(25.0);
    }

    [Fact]
    public void Parse_WithFailedTests_ExtractsErrorMessages()
    {
        // Arrange
        var trxPath = Path.Combine("TestData", "failed-tests.trx");

        // Act
        var result = _parser.Parse(trxPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var failures = result.Value.Failures;

        failures[0].TestName.Should().Be("Test2_ShouldFail");
        failures[0].ErrorMessage.Should().Contain("Expected: 5, Actual: 3");
        failures[0].StackTrace.Should().Contain("ExampleTest.cs:line 42");

        failures[1].TestName.Should().Be("Test3_ShouldFail");
        failures[1].ErrorMessage.Should().Contain("NullReferenceException");
        failures[1].StackTrace.Should().Contain("PdfDocument.cs:line 123");
    }

    [Fact]
    public void Parse_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var trxPath = "nonexistent.trx";

        // Act
        var result = _parser.Parse(trxPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("not found");
    }

    [Fact]
    public void Parse_WithMalformedXml_ReturnsFailure()
    {
        // Arrange
        var trxPath = Path.Combine("TestData", "malformed.trx");
        Directory.CreateDirectory(Path.GetDirectoryName(trxPath)!);
        File.WriteAllText(trxPath, "This is not valid XML");

        try
        {
            // Act
            var result = _parser.Parse(trxPath);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle();
            result.Errors[0].Message.Should().Contain("parsing error");
        }
        finally
        {
            // Cleanup
            if (File.Exists(trxPath))
            {
                File.Delete(trxPath);
            }
        }
    }

    [Fact]
    public void Parse_WithEmptyTestRun_ReturnsZeroStatistics()
    {
        // Arrange
        var trxPath = Path.Combine("TestData", "empty.trx");
        Directory.CreateDirectory(Path.GetDirectoryName(trxPath)!);
        File.WriteAllText(trxPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"">
  <Results>
  </Results>
</TestRun>");

        try
        {
            // Act
            var result = _parser.Parse(trxPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var testResults = result.Value;
            testResults.Total.Should().Be(0);
            testResults.Passed.Should().Be(0);
            testResults.Failed.Should().Be(0);
            testResults.Skipped.Should().Be(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(trxPath))
            {
                File.Delete(trxPath);
            }
        }
    }
}
