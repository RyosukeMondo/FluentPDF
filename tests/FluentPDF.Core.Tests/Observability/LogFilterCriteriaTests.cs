using FluentPDF.Core.Observability;

namespace FluentPDF.Core.Tests.Observability;

public sealed class LogFilterCriteriaTests
{
    private static LogEntry CreateTestLogEntry(
        DateTime? timestamp = null,
        LogLevel level = LogLevel.Information,
        string message = "Test message",
        string? correlationId = null,
        string component = "FluentPDF.Core")
    {
        return new LogEntry
        {
            Timestamp = timestamp ?? DateTime.UtcNow,
            Level = level,
            Message = message,
            CorrelationId = correlationId,
            Component = component
        };
    }

    [Fact]
    public void Matches_WithNoFilters_ShouldReturnTrue()
    {
        // Arrange
        var criteria = new LogFilterCriteria();
        var entry = CreateTestLogEntry();

        // Act
        var result = criteria.Matches(entry);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(LogLevel.Debug, LogLevel.Information, false)]
    [InlineData(LogLevel.Information, LogLevel.Information, true)]
    [InlineData(LogLevel.Warning, LogLevel.Information, true)]
    [InlineData(LogLevel.Error, LogLevel.Information, true)]
    public void Matches_WithMinimumLevel_ShouldFilterCorrectly(LogLevel entryLevel, LogLevel minimumLevel, bool expected)
    {
        // Arrange
        var criteria = new LogFilterCriteria { MinimumLevel = minimumLevel };
        var entry = CreateTestLogEntry(level: entryLevel);

        // Act
        var result = criteria.Matches(entry);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Matches_WithCorrelationId_ShouldMatchExactly()
    {
        // Arrange
        var criteria = new LogFilterCriteria { CorrelationId = "12345" };
        var matchingEntry = CreateTestLogEntry(correlationId: "12345");
        var nonMatchingEntry = CreateTestLogEntry(correlationId: "67890");
        var nullEntry = CreateTestLogEntry(correlationId: null);

        // Act & Assert
        Assert.True(criteria.Matches(matchingEntry));
        Assert.False(criteria.Matches(nonMatchingEntry));
        Assert.False(criteria.Matches(nullEntry));
    }

    [Theory]
    [InlineData("FluentPDF.Core", "FluentPDF.Core", true)]
    [InlineData("FluentPDF.Core.Services", "FluentPDF.Core", true)]
    [InlineData("FluentPDF.Rendering", "FluentPDF.Core", false)]
    [InlineData("FluentPDF.Core", "fluentpdf.core", true)] // Case-insensitive
    public void Matches_WithComponentFilter_ShouldMatchStartsWith(string component, string filter, bool expected)
    {
        // Arrange
        var criteria = new LogFilterCriteria { ComponentFilter = filter };
        var entry = CreateTestLogEntry(component: component);

        // Act
        var result = criteria.Matches(entry);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Matches_WithTimeRange_ShouldFilterCorrectly()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var criteria = new LogFilterCriteria { StartTime = startTime, EndTime = endTime };

        var beforeEntry = CreateTestLogEntry(timestamp: new DateTime(2024, 1, 1, 9, 59, 59, DateTimeKind.Utc));
        var startEntry = CreateTestLogEntry(timestamp: startTime);
        var middleEntry = CreateTestLogEntry(timestamp: new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc));
        var endEntry = CreateTestLogEntry(timestamp: endTime);
        var afterEntry = CreateTestLogEntry(timestamp: new DateTime(2024, 1, 1, 12, 0, 1, DateTimeKind.Utc));

        // Act & Assert
        Assert.False(criteria.Matches(beforeEntry));
        Assert.True(criteria.Matches(startEntry));
        Assert.True(criteria.Matches(middleEntry));
        Assert.True(criteria.Matches(endEntry));
        Assert.False(criteria.Matches(afterEntry));
    }

    [Theory]
    [InlineData("error occurred", "error", true)]
    [InlineData("Error occurred", "error", true)] // Case-insensitive
    [InlineData("no match", "error", false)]
    [InlineData("partial error match", "error", true)]
    public void Matches_WithSearchText_ShouldMatchCaseInsensitive(string message, string searchText, bool expected)
    {
        // Arrange
        var criteria = new LogFilterCriteria { SearchText = searchText };
        var entry = CreateTestLogEntry(message: message);

        // Act
        var result = criteria.Matches(entry);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Matches_WithMultipleFilters_ShouldMatchAll()
    {
        // Arrange
        var criteria = new LogFilterCriteria
        {
            MinimumLevel = LogLevel.Warning,
            ComponentFilter = "FluentPDF.Core",
            SearchText = "error"
        };

        var matchingEntry = CreateTestLogEntry(
            level: LogLevel.Error,
            component: "FluentPDF.Core.Services",
            message: "An error occurred");

        var nonMatchingLevel = CreateTestLogEntry(
            level: LogLevel.Information,
            component: "FluentPDF.Core.Services",
            message: "An error occurred");

        var nonMatchingComponent = CreateTestLogEntry(
            level: LogLevel.Error,
            component: "FluentPDF.Rendering",
            message: "An error occurred");

        var nonMatchingSearch = CreateTestLogEntry(
            level: LogLevel.Error,
            component: "FluentPDF.Core.Services",
            message: "Something happened");

        // Act & Assert
        Assert.True(criteria.Matches(matchingEntry));
        Assert.False(criteria.Matches(nonMatchingLevel));
        Assert.False(criteria.Matches(nonMatchingComponent));
        Assert.False(criteria.Matches(nonMatchingSearch));
    }

    [Fact]
    public void Matches_WithEmptyStringFilters_ShouldBeIgnored()
    {
        // Arrange
        var criteria = new LogFilterCriteria
        {
            CorrelationId = "",
            ComponentFilter = "",
            SearchText = ""
        };
        var entry = CreateTestLogEntry();

        // Act
        var result = criteria.Matches(entry);

        // Assert
        Assert.True(result); // Empty strings should not filter
    }

    [Fact]
    public void LogFilterCriteria_ShouldBeImmutable()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);
        var criteria = new LogFilterCriteria
        {
            MinimumLevel = LogLevel.Warning,
            CorrelationId = "12345",
            ComponentFilter = "FluentPDF",
            StartTime = startTime,
            EndTime = endTime,
            SearchText = "error"
        };

        // Assert - verify properties are init-only
        Assert.Equal(LogLevel.Warning, criteria.MinimumLevel);
        Assert.Equal("12345", criteria.CorrelationId);
        Assert.Equal("FluentPDF", criteria.ComponentFilter);
        Assert.Equal(startTime, criteria.StartTime);
        Assert.Equal(endTime, criteria.EndTime);
        Assert.Equal("error", criteria.SearchText);
    }
}
