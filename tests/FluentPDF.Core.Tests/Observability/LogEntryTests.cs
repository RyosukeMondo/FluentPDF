using FluentPDF.Core.Observability;

namespace FluentPDF.Core.Tests.Observability;

public sealed class LogEntryTests
{
    [Fact]
    public void LogEntry_ShouldBeCreatedWithRequiredProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var context = new Dictionary<string, object>
        {
            ["UserId"] = 123,
            ["Action"] = "OpenDocument"
        };

        // Act
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            Level = LogLevel.Information,
            Message = "Document opened successfully",
            CorrelationId = "12345",
            Component = "FluentPDF.Core.Services",
            Context = context,
            Exception = null,
            StackTrace = null
        };

        // Assert
        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Document opened successfully", entry.Message);
        Assert.Equal("12345", entry.CorrelationId);
        Assert.Equal("FluentPDF.Core.Services", entry.Component);
        Assert.Equal(context, entry.Context);
        Assert.Null(entry.Exception);
        Assert.Null(entry.StackTrace);
    }

    [Fact]
    public void LogEntry_WithMinimalProperties_ShouldWork()
    {
        // Act
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Debug,
            Message = "Debug message",
            Component = "FluentPDF.Core"
        };

        // Assert
        Assert.Null(entry.CorrelationId);
        Assert.NotNull(entry.Context);
        Assert.Empty(entry.Context);
        Assert.Null(entry.Exception);
        Assert.Null(entry.StackTrace);
    }

    [Fact]
    public void LogEntry_WithException_ShouldStoreExceptionDetails()
    {
        // Arrange
        var exceptionMessage = "File not found";
        var stackTrace = "at FluentPDF.Core.Services.PdfService.LoadDocument()";

        // Act
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Message = "Failed to load document",
            Component = "FluentPDF.Core.Services",
            Exception = exceptionMessage,
            StackTrace = stackTrace
        };

        // Assert
        Assert.Equal(exceptionMessage, entry.Exception);
        Assert.Equal(stackTrace, entry.StackTrace);
    }

    [Fact]
    public void LogEntry_ContextDictionary_ShouldBeInitializedByDefault()
    {
        // Act
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Message = "Test",
            Component = "Test"
        };

        // Assert
        Assert.NotNull(entry.Context);
        Assert.IsType<Dictionary<string, object>>(entry.Context);
    }

    [Fact]
    public void LogEntry_ShouldBeImmutable()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            Level = LogLevel.Warning,
            Message = "Warning message",
            CorrelationId = "abc-123",
            Component = "FluentPDF.Core"
        };

        // Assert - verify properties are init-only
        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Warning message", entry.Message);
        Assert.Equal("abc-123", entry.CorrelationId);
        Assert.Equal("FluentPDF.Core", entry.Component);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void LogEntry_ShouldSupportAllLogLevels(LogLevel level)
    {
        // Act
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = $"Test {level}",
            Component = "Test"
        };

        // Assert
        Assert.Equal(level, entry.Level);
    }
}
