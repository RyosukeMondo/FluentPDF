using System.Collections.ObjectModel;
using FluentAssertions;
using FluentPDF.App.Controls;
using FluentPDF.Core.Observability;

namespace FluentPDF.App.Tests.Controls;

/// <summary>
/// Tests for LogViewerControl custom WinUI control.
/// Tests basic property and dependency property behavior.
/// </summary>
public class LogViewerControlTests
{
    [Fact]
    public void Constructor_ShouldInitializeControl()
    {
        // Arrange & Act
        var control = new LogViewerControl();

        // Assert
        control.Should().NotBeNull();
        control.LogEntries.Should().NotBeNull("log entries collection should be initialized");
        control.LogEntries.Should().BeEmpty("log entries collection should be empty initially");
        control.LogLevels.Should().NotBeNull("log levels collection should be initialized");
        control.LogLevels.Should().HaveCount(6, "should contain all 6 log levels");
        control.SelectedLogEntry.Should().BeNull("no log should be selected initially");
        control.MinimumLevel.Should().BeNull("no minimum level filter initially");
        control.CorrelationIdFilter.Should().BeEmpty("correlation ID filter should be empty");
        control.ComponentFilter.Should().BeEmpty("component filter should be empty");
        control.SearchText.Should().BeEmpty("search text should be empty");
        control.StartTime.Should().BeNull("start time filter should be null");
        control.EndTime.Should().BeNull("end time filter should be null");
        control.IsDetailsExpanded.Should().BeTrue("details panel should be expanded by default");
        control.HasSelectedLog.Should().BeFalse("no log selected");
        control.HasException.Should().BeFalse("no exception initially");
        control.HasStackTrace.Should().BeFalse("no stack trace initially");
        control.HasContext.Should().BeFalse("no context initially");
        control.ContextJson.Should().BeEmpty("context JSON should be empty");
    }

    [Fact]
    public void LogEntries_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();
        var entries = new ObservableCollection<LogEntry>
        {
            new()
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Information,
                Message = "Test message",
                Component = "TestComponent"
            }
        };

        // Act
        control.LogEntries = entries;

        // Assert
        control.LogEntries.Should().BeSameAs(entries);
        control.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public void SelectedLogEntry_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Message = "Error occurred",
            Component = "TestComponent"
        };

        // Act
        control.SelectedLogEntry = logEntry;

        // Assert
        control.SelectedLogEntry.Should().Be(logEntry);
        control.HasSelectedLog.Should().BeTrue("log is now selected");
    }

    [Fact]
    public void SelectedLogEntry_WithException_ShouldSetHasException()
    {
        // Arrange
        var control = new LogViewerControl();
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Message = "Error occurred",
            Component = "TestComponent",
            Exception = "System.InvalidOperationException: Test exception"
        };

        // Act
        control.SelectedLogEntry = logEntry;

        // Assert
        control.HasException.Should().BeTrue("log has exception");
        control.HasStackTrace.Should().BeFalse("log has no stack trace");
    }

    [Fact]
    public void SelectedLogEntry_WithStackTrace_ShouldSetHasStackTrace()
    {
        // Arrange
        var control = new LogViewerControl();
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Message = "Error occurred",
            Component = "TestComponent",
            Exception = "System.InvalidOperationException: Test exception",
            StackTrace = "at TestClass.TestMethod()\nat Program.Main()"
        };

        // Act
        control.SelectedLogEntry = logEntry;

        // Assert
        control.HasException.Should().BeTrue("log has exception");
        control.HasStackTrace.Should().BeTrue("log has stack trace");
    }

    [Fact]
    public void SelectedLogEntry_WithContext_ShouldSetHasContextAndContextJson()
    {
        // Arrange
        var control = new LogViewerControl();
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Information,
            Message = "Test message",
            Component = "TestComponent",
            Context = new Dictionary<string, object>
            {
                { "Key1", "Value1" },
                { "Key2", 123 }
            }
        };

        // Act
        control.SelectedLogEntry = logEntry;

        // Assert
        control.HasContext.Should().BeTrue("log has context");
        control.ContextJson.Should().NotBeEmpty("context JSON should be populated");
        control.ContextJson.Should().Contain("Key1", "JSON should contain Key1");
        control.ContextJson.Should().Contain("Value1", "JSON should contain Value1");
    }

    [Fact]
    public void SelectedLogEntry_SetToNull_ShouldClearDetails()
    {
        // Arrange
        var control = new LogViewerControl();
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Message = "Error occurred",
            Component = "TestComponent",
            Exception = "Test exception",
            StackTrace = "Test stack trace",
            Context = new Dictionary<string, object> { { "Key", "Value" } }
        };
        control.SelectedLogEntry = logEntry;

        // Act
        control.SelectedLogEntry = null;

        // Assert
        control.HasSelectedLog.Should().BeFalse("no log selected");
        control.HasException.Should().BeFalse("exception cleared");
        control.HasStackTrace.Should().BeFalse("stack trace cleared");
        control.HasContext.Should().BeFalse("context cleared");
        control.ContextJson.Should().BeEmpty("context JSON cleared");
    }

    [Fact]
    public void MinimumLevel_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();

        // Act
        control.MinimumLevel = LogLevel.Warning;

        // Assert
        control.MinimumLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void CorrelationIdFilter_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();

        // Act
        control.CorrelationIdFilter = "12345-abc";

        // Assert
        control.CorrelationIdFilter.Should().Be("12345-abc");
    }

    [Fact]
    public void ComponentFilter_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();

        // Act
        control.ComponentFilter = "FluentPDF.Rendering";

        // Assert
        control.ComponentFilter.Should().Be("FluentPDF.Rendering");
    }

    [Fact]
    public void SearchText_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();

        // Act
        control.SearchText = "error occurred";

        // Assert
        control.SearchText.Should().Be("error occurred");
    }

    [Fact]
    public void StartTime_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();
        var startTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        control.StartTime = startTime;

        // Assert
        control.StartTime.Should().Be(startTime);
    }

    [Fact]
    public void EndTime_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();
        var endTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act
        control.EndTime = endTime;

        // Assert
        control.EndTime.Should().Be(endTime);
    }

    [Fact]
    public void IsDetailsExpanded_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new LogViewerControl();

        // Act
        control.IsDetailsExpanded = false;

        // Assert
        control.IsDetailsExpanded.Should().BeFalse();
    }

    [Fact]
    public void Commands_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var control = new LogViewerControl();

        // Assert
        control.RefreshCommand.Should().BeNull("no command assigned initially");
        control.ExportCommand.Should().BeNull("no command assigned initially");
        control.ClearFiltersCommand.Should().BeNull("no command assigned initially");
        control.CopyCorrelationIdCommand.Should().BeNull("no command assigned initially");
    }

    [Fact]
    public void LogLevels_ShouldContainAllLogLevels()
    {
        // Arrange & Act
        var control = new LogViewerControl();

        // Assert
        control.LogLevels.Should().Contain(LogLevel.Trace);
        control.LogLevels.Should().Contain(LogLevel.Debug);
        control.LogLevels.Should().Contain(LogLevel.Information);
        control.LogLevels.Should().Contain(LogLevel.Warning);
        control.LogLevels.Should().Contain(LogLevel.Error);
        control.LogLevels.Should().Contain(LogLevel.Critical);
    }

    [Fact]
    public void MultipleFilters_ShouldBeIndependent()
    {
        // Arrange
        var control = new LogViewerControl();
        var startTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act
        control.MinimumLevel = LogLevel.Warning;
        control.CorrelationIdFilter = "12345";
        control.ComponentFilter = "TestComponent";
        control.SearchText = "error";
        control.StartTime = startTime;
        control.EndTime = endTime;

        // Assert - all filters should maintain their values
        control.MinimumLevel.Should().Be(LogLevel.Warning);
        control.CorrelationIdFilter.Should().Be("12345");
        control.ComponentFilter.Should().Be("TestComponent");
        control.SearchText.Should().Be("error");
        control.StartTime.Should().Be(startTime);
        control.EndTime.Should().Be(endTime);
    }
}
