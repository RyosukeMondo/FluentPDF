using FluentAssertions;
using FluentPDF.QualityAgent.Parsers;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Parsers;

public class LogParserTests
{
    private readonly LogParser _parser = new();

    [Fact]
    public void Parse_WithValidLogEntries_ReturnsCorrectStatistics()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "valid-logs.json");

        // Act
        var result = _parser.Parse(logPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logResults = result.Value;
        logResults.Entries.Should().HaveCount(5);
        logResults.InfoCount.Should().Be(2);
        logResults.WarningCount.Should().Be(1);
        logResults.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void Parse_WithCorrelationIds_GroupsEntriesCorrectly()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "valid-logs.json");

        // Act
        var result = _parser.Parse(logPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logResults = result.Value;
        logResults.EntriesByCorrelationId.Should().ContainKey("corr-123");
        logResults.EntriesByCorrelationId["corr-123"].Should().HaveCount(3);
        logResults.EntriesByCorrelationId.Should().ContainKey("corr-456");
        logResults.EntriesByCorrelationId["corr-456"].Should().HaveCount(1);
    }

    [Fact]
    public void Parse_WithException_ExtractsExceptionDetails()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "exception-log.json");

        // Act
        var result = _parser.Parse(logPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logResults = result.Value;
        var entryWithException = logResults.Entries.First(e => e.Exception != null);
        entryWithException.Exception.Should().NotBeNull();
        entryWithException.Exception!.Type.Should().Be("System.NullReferenceException");
        entryWithException.Exception.Message.Should().Contain("Object reference not set");
        entryWithException.Exception.StackTrace.Should().Contain("PdfRenderer.cs");
    }

    [Fact]
    public void Parse_WithStructuredProperties_ExtractsProperties()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "valid-logs.json");

        // Act
        var result = _parser.Parse(logPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logResults = result.Value;
        var entryWithProps = logResults.Entries.FirstOrDefault(e => e.Properties != null && e.Properties.Count > 0);
        entryWithProps.Should().NotBeNull();
        entryWithProps!.Properties.Should().ContainKey("Duration");
    }

    [Fact]
    public void Parse_WithDifferentLogLevels_CountsCorrectly()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "mixed-levels.json");

        // Act
        var result = _parser.Parse(logPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logResults = result.Value;
        logResults.InfoCount.Should().Be(3);
        logResults.WarningCount.Should().Be(2);
        logResults.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void Parse_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var logPath = "nonexistent.json";

        // Act
        var result = _parser.Parse(logPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("not found");
    }

    [Fact]
    public void Parse_WithMalformedJson_SkipsInvalidLines()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "malformed-logs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, @"{""Timestamp"":""2026-01-11T10:00:00Z"",""Level"":""Information"",""MessageTemplate"":""Valid log""}
This is not valid JSON
{""Timestamp"":""2026-01-11T10:01:00Z"",""Level"":""Error"",""MessageTemplate"":""Another valid log""}
");

        try
        {
            // Act
            var result = _parser.Parse(logPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var logResults = result.Value;
            logResults.Entries.Should().HaveCount(2); // Should skip malformed line
        }
        finally
        {
            // Cleanup
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void Parse_WithEmptyFile_ReturnsEmptyResults()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "empty-log.json");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, string.Empty);

        try
        {
            // Act
            var result = _parser.Parse(logPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var logResults = result.Value;
            logResults.Entries.Should().BeEmpty();
            logResults.ErrorCount.Should().Be(0);
            logResults.WarningCount.Should().Be(0);
            logResults.InfoCount.Should().Be(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void Parse_WithCompactSerilogFormat_ParsesCorrectly()
    {
        // Arrange
        var logPath = Path.Combine("TestData", "compact-format.json");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, @"{""@t"":""2026-01-11T10:00:00.0000000Z"",""@l"":""Information"",""@mt"":""Application started""}
{""@t"":""2026-01-11T10:01:00.0000000Z"",""@l"":""Warning"",""@mt"":""Low memory warning""}
");

        try
        {
            // Act
            var result = _parser.Parse(logPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var logResults = result.Value;
            logResults.Entries.Should().HaveCount(2);
            logResults.Entries[0].Level.Should().Be("Information");
            logResults.Entries[0].Message.Should().Be("Application started");
            logResults.Entries[1].Level.Should().Be("Warning");
            logResults.Entries[1].Message.Should().Be("Low memory warning");
        }
        finally
        {
            // Cleanup
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }
}
