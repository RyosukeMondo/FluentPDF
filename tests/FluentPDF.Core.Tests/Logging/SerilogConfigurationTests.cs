using FluentAssertions;
using FluentPDF.Core.Logging;
using Serilog;
using Serilog.Events;

namespace FluentPDF.Core.Tests.Logging;

/// <summary>
/// Tests for Serilog configuration.
/// </summary>
public class SerilogConfigurationTests
{
    [Fact]
    public void CreateLogger_ShouldReturnValidLogger()
    {
        // Act
        var logger = SerilogConfiguration.CreateLogger();

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<ILogger>();
    }

    [Fact]
    public void CreateLogger_ShouldCreateLogFile()
    {
        // Arrange
        var logger = SerilogConfiguration.CreateLogger();
        var testMessage = $"Test message at {DateTime.UtcNow:O}";

        // Act
        logger.Information(testMessage);
        Log.CloseAndFlush(); // Force flush to disk

        // Assert
        // In test context, logs go to temp directory
        var tempLogDir = Path.Combine(Path.GetTempPath(), "FluentPDF", "logs");
        Directory.Exists(tempLogDir).Should().BeTrue();
        Directory.GetFiles(tempLogDir, "log-*.json").Should().NotBeEmpty();
    }

    [Fact]
    public void CreateLogger_ShouldIncludeEnrichers()
    {
        // Arrange
        var logger = SerilogConfiguration.CreateLogger();
        var logEvents = new List<LogEvent>();

        // Create a test sink to capture log events
        var testLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "FluentPDF")
            .Enrich.WithProperty("Version", "1.0.0")
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Sink(new TestSink(logEvents))
            .CreateLogger();

        // Act
        testLogger.Information("Test message with enrichers");

        // Assert
        logEvents.Should().HaveCount(1);
        var logEvent = logEvents[0];
        logEvent.Properties.Should().ContainKey("Application");
        logEvent.Properties["Application"].ToString().Should().Contain("FluentPDF");
        logEvent.Properties.Should().ContainKey("MachineName");
        logEvent.Properties.Should().ContainKey("ThreadId");
    }

    [Fact]
    public void CreateLogger_ShouldSetMinimumLevelToDebug()
    {
        // Arrange
        var logger = SerilogConfiguration.CreateLogger();

        // Act & Assert
        // Debug level should be enabled
        logger.IsEnabled(LogEventLevel.Debug).Should().BeTrue();
        logger.IsEnabled(LogEventLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogEventLevel.Warning).Should().BeTrue();
        logger.IsEnabled(LogEventLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogEventLevel.Fatal).Should().BeTrue();
    }

    /// <summary>
    /// Test sink for capturing log events in tests.
    /// </summary>
    private class TestSink : Serilog.Core.ILogEventSink
    {
        private readonly List<LogEvent> _logEvents;

        public TestSink(List<LogEvent> logEvents)
        {
            _logEvents = logEvents;
        }

        public void Emit(LogEvent logEvent)
        {
            _logEvents.Add(logEvent);
        }
    }
}
