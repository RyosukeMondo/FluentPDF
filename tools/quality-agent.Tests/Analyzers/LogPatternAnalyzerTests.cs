using FluentPDF.QualityAgent.Analyzers;
using FluentPDF.QualityAgent.Models;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Analyzers;

public class LogPatternAnalyzerTests
{
    [Fact]
    public void Analyze_WithErrorSpike_DetectsSpike()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer(errorRateThresholdMultiplier: 2.0);
        var logResults = CreateLogResults(
            errorCount: 100,
            warningCount: 10,
            infoCount: 50,
            timeSpanHours: 1.0
        );

        // Act - baseline is 10 errors/hour, actual is 100 errors/hour (10x spike)
        var result = analyzer.Analyze(logResults, baselineErrorsPerHour: 10.0);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.True(patterns.ErrorRate.IsSpike);
        Assert.Equal(100.0, patterns.ErrorRate.ErrorsPerHour, 2); // Allow 2 decimal precision
        Assert.Equal(10.0, patterns.ErrorRate.BaselineErrorsPerHour);
        Assert.Equal(10.0, patterns.ErrorRate.SpikeMultiplier, 1); // Allow 1 decimal precision
    }

    [Fact]
    public void Analyze_WithNormalErrorRate_DoesNotDetectSpike()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer(errorRateThresholdMultiplier: 2.0);
        var logResults = CreateLogResults(
            errorCount: 15,
            warningCount: 10,
            infoCount: 50,
            timeSpanHours: 1.0
        );

        // Act - baseline is 10 errors/hour, actual is 15 errors/hour (1.5x, below 2.0 threshold)
        var result = analyzer.Analyze(logResults, baselineErrorsPerHour: 10.0);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.False(patterns.ErrorRate.IsSpike);
        Assert.Equal(15.0, patterns.ErrorRate.ErrorsPerHour, 1); // Allow 1 decimal precision
        Assert.Equal(1.5, patterns.ErrorRate.SpikeMultiplier, 1); // Allow 1 decimal precision
    }

    [Fact]
    public void Analyze_WithRepeatedExceptions_DetectsPattern()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer(repeatedExceptionThreshold: 5);
        var exception = new ExceptionInfo
        {
            Type = "NullReferenceException",
            Message = "Object reference not set to an instance of an object",
            StackTrace = "at MyApp.Service.Process()\nat MyApp.Controller.Execute()"
        };

        var entries = new List<LogEntry>();
        for (int i = 0; i < 10; i++)
        {
            entries.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                Level = "Error",
                Message = "Operation failed",
                Exception = exception
            });
        }

        var logResults = new LogResults
        {
            Entries = entries,
            EntriesByCorrelationId = new(),
            ErrorCount = 10,
            WarningCount = 0,
            InfoCount = 0
        };

        // Act
        var result = analyzer.Analyze(logResults);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.Single(patterns.RepeatedExceptions);
        var repeatedPattern = patterns.RepeatedExceptions.First();
        Assert.Equal("NullReferenceException", repeatedPattern.ExceptionType);
        Assert.Equal(10, repeatedPattern.Occurrences);
        Assert.Equal(10, repeatedPattern.Timestamps.Count);
    }

    [Fact]
    public void Analyze_WithDifferentExceptions_GroupsSeparately()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer(repeatedExceptionThreshold: 3);

        var entries = new List<LogEntry>();

        // Add 5 NullReferenceExceptions
        for (int i = 0; i < 5; i++)
        {
            entries.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                Level = "Error",
                Message = "Null error",
                Exception = new ExceptionInfo
                {
                    Type = "NullReferenceException",
                    Message = "Object reference not set",
                    StackTrace = "at MyApp.ServiceA.Process()"
                }
            });
        }

        // Add 4 ArgumentExceptions
        for (int i = 0; i < 4; i++)
        {
            entries.Add(new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(i + 10),
                Level = "Error",
                Message = "Argument error",
                Exception = new ExceptionInfo
                {
                    Type = "ArgumentException",
                    Message = "Invalid argument",
                    StackTrace = "at MyApp.ServiceB.Validate()"
                }
            });
        }

        var logResults = new LogResults
        {
            Entries = entries,
            EntriesByCorrelationId = new(),
            ErrorCount = 9,
            WarningCount = 0,
            InfoCount = 0
        };

        // Act
        var result = analyzer.Analyze(logResults);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.Equal(2, patterns.RepeatedExceptions.Count);

        var nullRefPattern = patterns.RepeatedExceptions.First(p => p.ExceptionType == "NullReferenceException");
        Assert.Equal(5, nullRefPattern.Occurrences);

        var argPattern = patterns.RepeatedExceptions.First(p => p.ExceptionType == "ArgumentException");
        Assert.Equal(4, argPattern.Occurrences);
    }

    [Fact]
    public void Analyze_WithSlowOperations_DetectsPerformanceWarnings()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer(performanceThresholdMs: 1000.0);

        var entries = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Message = "Database query completed",
                Properties = new Dictionary<string, object>
                {
                    ["Operation"] = "DatabaseQuery",
                    ["Duration"] = 1500.0
                }
            },
            new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(1),
                Level = "Information",
                Message = "API call completed",
                Properties = new Dictionary<string, object>
                {
                    ["Operation"] = "ExternalApiCall",
                    ["DurationMs"] = 2500.0
                }
            },
            new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(2),
                Level = "Information",
                Message = "Fast operation",
                Properties = new Dictionary<string, object>
                {
                    ["Operation"] = "LocalCache",
                    ["Duration"] = 50.0
                }
            }
        };

        var logResults = new LogResults
        {
            Entries = entries,
            EntriesByCorrelationId = new(),
            ErrorCount = 0,
            WarningCount = 0,
            InfoCount = 3
        };

        // Act
        var result = analyzer.Analyze(logResults);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.Equal(2, patterns.PerformanceWarnings.Count);

        // Should be ordered by duration descending
        Assert.Equal(2500.0, patterns.PerformanceWarnings[0].DurationMs);
        Assert.Equal("ExternalApiCall", patterns.PerformanceWarnings[0].Operation);
        Assert.Equal(1500.0, patterns.PerformanceWarnings[1].DurationMs);
    }

    [Fact]
    public void Analyze_WithMissingCorrelationIds_DetectsEntries()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer();

        var entries = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Message = "Entry with correlation ID",
                CorrelationId = "correlation-123"
            },
            new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(1),
                Level = "Error",
                Message = "Entry without correlation ID",
                CorrelationId = null
            },
            new LogEntry
            {
                Timestamp = DateTime.UtcNow.AddMinutes(2),
                Level = "Warning",
                Message = "Another entry without correlation ID",
                CorrelationId = string.Empty
            }
        };

        var logResults = new LogResults
        {
            Entries = entries,
            EntriesByCorrelationId = new(),
            ErrorCount = 1,
            WarningCount = 1,
            InfoCount = 1
        };

        // Act
        var result = analyzer.Analyze(logResults);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.Equal(2, patterns.MissingCorrelationIds.Count);
        Assert.Contains("Error", patterns.MissingCorrelationIds[0]);
        Assert.Contains("Warning", patterns.MissingCorrelationIds[1]);
    }

    [Fact]
    public void Analyze_WithEmptyLogs_ReturnsEmptyPatterns()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer();
        var logResults = new LogResults
        {
            Entries = new List<LogEntry>(),
            EntriesByCorrelationId = new(),
            ErrorCount = 0,
            WarningCount = 0,
            InfoCount = 0
        };

        // Act
        var result = analyzer.Analyze(logResults);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        Assert.False(patterns.ErrorRate.IsSpike);
        Assert.Empty(patterns.RepeatedExceptions);
        Assert.Empty(patterns.PerformanceWarnings);
        Assert.Empty(patterns.MissingCorrelationIds);
    }

    [Fact]
    public void Analyze_WithNoBaseline_CalculatesMovingAverage()
    {
        // Arrange
        var analyzer = new LogPatternAnalyzer();
        var logResults = CreateLogResults(
            errorCount: 20,
            warningCount: 10,
            infoCount: 50,
            timeSpanHours: 1.0
        );

        // Act - no baseline provided
        var result = analyzer.Analyze(logResults);

        // Assert
        Assert.True(result.IsSuccess);
        var patterns = result.Value;
        // Baseline should be calculated (50% of current rate = 10)
        Assert.Equal(10.0, patterns.ErrorRate.BaselineErrorsPerHour, 1); // Allow 1 decimal precision
        Assert.Equal(20.0, patterns.ErrorRate.ErrorsPerHour, 1); // Allow 1 decimal precision
    }

    private LogResults CreateLogResults(int errorCount, int warningCount, int infoCount, double timeSpanHours)
    {
        var entries = new List<LogEntry>();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(timeSpanHours);

        // Add error entries - ensure they stay within exact timespan
        for (int i = 0; i < errorCount; i++)
        {
            var fraction = errorCount > 1 ? (double)i / (errorCount - 1) : 0.5;
            entries.Add(new LogEntry
            {
                Timestamp = startTime.AddHours(fraction * timeSpanHours),
                Level = "Error",
                Message = $"Error {i}"
            });
        }

        // Add warning entries
        for (int i = 0; i < warningCount; i++)
        {
            var fraction = warningCount > 1 ? (double)i / (warningCount - 1) : 0.5;
            entries.Add(new LogEntry
            {
                Timestamp = startTime.AddHours(fraction * timeSpanHours),
                Level = "Warning",
                Message = $"Warning {i}"
            });
        }

        // Add info entries
        for (int i = 0; i < infoCount; i++)
        {
            var fraction = infoCount > 1 ? (double)i / (infoCount - 1) : 0.5;
            entries.Add(new LogEntry
            {
                Timestamp = startTime.AddHours(fraction * timeSpanHours),
                Level = "Information",
                Message = $"Info {i}"
            });
        }

        return new LogResults
        {
            Entries = entries,
            EntriesByCorrelationId = new(),
            ErrorCount = errorCount,
            WarningCount = warningCount,
            InfoCount = infoCount
        };
    }
}
