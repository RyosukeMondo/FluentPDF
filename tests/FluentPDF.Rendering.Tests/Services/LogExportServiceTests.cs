using System.Text.Json;
using FluentAssertions;
using FluentPDF.Core.Observability;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using LogLevel = FluentPDF.Core.Observability.LogLevel;

namespace FluentPDF.Rendering.Tests.Services;

public sealed class LogExportServiceTests : IDisposable
{
    private readonly Mock<ILogger<LogExportService>> _loggerMock;
    private readonly LogExportService _service;
    private readonly string _testLogDirectory;

    public LogExportServiceTests()
    {
        _loggerMock = new Mock<ILogger<LogExportService>>();
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF_LogTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLogDirectory);

        // Pass the test log directory to the service
        _service = new LogExportService(_loggerMock.Object, _testLogDirectory);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LogExportService(null!, _testLogDirectory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task GetRecentLogsAsync_WithNoLogFiles_ReturnsEmptyList()
    {
        // Arrange
        var emptyLogDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF_Empty_{Guid.NewGuid()}");
        Directory.CreateDirectory(emptyLogDirectory);

        try
        {
            var emptyService = new LogExportService(_loggerMock.Object, emptyLogDirectory);

            // Act
            var result = await emptyService.GetRecentLogsAsync(100);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(emptyLogDirectory))
                Directory.Delete(emptyLogDirectory, true);
        }
    }

    [Fact]
    public async Task GetRecentLogsAsync_WithValidLogFile_ParsesEntriesCorrectly()
    {
        // Arrange
        var logFile = CreateTestLogFile("test1.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Test message 1", "FluentPDF.Test", "corr-123"),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-10), "Warning", "Test message 2", "FluentPDF.Test", "corr-456"),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-20), "Error", "Test message 3", "FluentPDF.Test", null)
        });

        try
        {
            // Act
            var result = await _service.GetRecentLogsAsync(100);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(3);

            var firstLog = result.Value.First();
            firstLog.Level.Should().Be(LogLevel.Information);
            firstLog.Message.Should().Be("Test message 1");
            firstLog.Component.Should().Be("FluentPDF.Test");
            firstLog.CorrelationId.Should().Be("corr-123");
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task GetRecentLogsAsync_WithMaxEntries_LimitsResults()
    {
        // Arrange
        var entries = Enumerable.Range(0, 100)
            .Select(i => CreateSerilogJsonLine(
                DateTime.UtcNow.AddSeconds(-i),
                "Information",
                $"Message {i}",
                "FluentPDF.Test",
                null))
            .ToArray();

        var logFile = CreateTestLogFile("test-limit.json", entries);

        try
        {
            // Act
            var result = await _service.GetRecentLogsAsync(25);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(25);
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task GetRecentLogsAsync_WithMultipleFiles_ReadsFromMultipleFiles()
    {
        // Arrange
        var logFile1 = CreateTestLogFile("test-multi-1.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Message from file 1", "FluentPDF.Test", null)
        });

        var logFile2 = CreateTestLogFile("test-multi-2.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-5), "Warning", "Message from file 2", "FluentPDF.Test", null)
        });

        try
        {
            // Act
            var result = await _service.GetRecentLogsAsync(100);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCountGreaterThanOrEqualTo(2);
        }
        finally
        {
            if (File.Exists(logFile1))
                File.Delete(logFile1);
            if (File.Exists(logFile2))
                File.Delete(logFile2);
        }
    }

    [Fact]
    public async Task GetRecentLogsAsync_WithCorruptedJson_SkipsInvalidEntriesAndContinues()
    {
        // Arrange
        var logFile = CreateTestLogFile("test-corrupted.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Valid message 1", "FluentPDF.Test", null),
            "{ invalid json without closing brace",
            "",
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-5), "Warning", "Valid message 2", "FluentPDF.Test", null)
        });

        try
        {
            // Act
            var result = await _service.GetRecentLogsAsync(100);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2); // Only valid entries
            result.Value.Should().Contain(e => e.Message == "Valid message 1");
            result.Value.Should().Contain(e => e.Message == "Valid message 2");
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task FilterLogsAsync_WithMinimumLevel_FiltersCorrectly()
    {
        // Arrange
        var logFile = CreateTestLogFile("test-filter-level.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Info message", "FluentPDF.Test", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-1), "Warning", "Warning message", "FluentPDF.Test", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-2), "Error", "Error message", "FluentPDF.Test", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-3), "Debug", "Debug message", "FluentPDF.Test", null)
        });

        try
        {
            var criteria = new LogFilterCriteria { MinimumLevel = LogLevel.Warning };

            // Act
            var result = await _service.FilterLogsAsync(criteria);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2); // Warning and Error
            result.Value.Should().OnlyContain(e => e.Level >= LogLevel.Warning);
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task FilterLogsAsync_WithCorrelationId_FiltersCorrectly()
    {
        // Arrange
        var logFile = CreateTestLogFile("test-filter-corr.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Message 1", "FluentPDF.Test", "corr-123"),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-1), "Information", "Message 2", "FluentPDF.Test", "corr-456"),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-2), "Information", "Message 3", "FluentPDF.Test", "corr-123")
        });

        try
        {
            var criteria = new LogFilterCriteria { CorrelationId = "corr-123" };

            // Act
            var result = await _service.FilterLogsAsync(criteria);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.Should().OnlyContain(e => e.CorrelationId == "corr-123");
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task FilterLogsAsync_WithComponentFilter_FiltersCorrectly()
    {
        // Arrange
        var logFile = CreateTestLogFile("test-filter-component.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Message 1", "FluentPDF.Rendering", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-1), "Information", "Message 2", "FluentPDF.Core", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-2), "Information", "Message 3", "FluentPDF.Rendering.Services", null)
        });

        try
        {
            var criteria = new LogFilterCriteria { ComponentFilter = "FluentPDF.Rendering" };

            // Act
            var result = await _service.FilterLogsAsync(criteria);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2); // Both Rendering components
            result.Value.Should().OnlyContain(e => e.Component.StartsWith("FluentPDF.Rendering"));
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task FilterLogsAsync_WithSearchText_FiltersCorrectly()
    {
        // Arrange
        var logFile = CreateTestLogFile("test-filter-search.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Rendering PDF page", "FluentPDF.Test", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-1), "Information", "Loading document", "FluentPDF.Test", null),
            CreateSerilogJsonLine(DateTime.UtcNow.AddSeconds(-2), "Information", "PDF rendering complete", "FluentPDF.Test", null)
        });

        try
        {
            var criteria = new LogFilterCriteria { SearchText = "rendering" };

            // Act
            var result = await _service.FilterLogsAsync(criteria);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.Should().OnlyContain(e => e.Message.Contains("rendering", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task FilterLogsAsync_WithTimeRange_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logFile = CreateTestLogFile("test-filter-time.json", new[]
        {
            CreateSerilogJsonLine(now.AddMinutes(-5), "Information", "Old message", "FluentPDF.Test", null),
            CreateSerilogJsonLine(now.AddMinutes(-3), "Information", "Recent message 1", "FluentPDF.Test", null),
            CreateSerilogJsonLine(now.AddMinutes(-1), "Information", "Recent message 2", "FluentPDF.Test", null)
        });

        try
        {
            var criteria = new LogFilterCriteria
            {
                StartTime = now.AddMinutes(-4),
                EndTime = now
            };

            // Act
            var result = await _service.FilterLogsAsync(criteria);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.Should().OnlyContain(e => e.Timestamp >= criteria.StartTime && e.Timestamp <= criteria.EndTime);
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task FilterLogsAsync_WithMultipleCriteria_AppliesAllFilters()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logFile = CreateTestLogFile("test-filter-multiple.json", new[]
        {
            CreateSerilogJsonLine(now, "Warning", "Warning in rendering", "FluentPDF.Rendering", "corr-123"),
            CreateSerilogJsonLine(now.AddSeconds(-1), "Information", "Info in rendering", "FluentPDF.Rendering", "corr-123"),
            CreateSerilogJsonLine(now.AddSeconds(-2), "Warning", "Warning in core", "FluentPDF.Core", "corr-456"),
            CreateSerilogJsonLine(now.AddSeconds(-3), "Error", "Error in rendering", "FluentPDF.Rendering", "corr-123")
        });

        try
        {
            var criteria = new LogFilterCriteria
            {
                MinimumLevel = LogLevel.Warning,
                ComponentFilter = "FluentPDF.Rendering",
                CorrelationId = "corr-123"
            };

            // Act
            var result = await _service.FilterLogsAsync(criteria);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2); // Warning and Error from Rendering with corr-123
            result.Value.Should().OnlyContain(e =>
                e.Level >= LogLevel.Warning &&
                e.Component.StartsWith("FluentPDF.Rendering") &&
                e.CorrelationId == "corr-123");
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    [Fact]
    public async Task ExportLogsAsync_CreatesValidSerilogJsonFile()
    {
        // Arrange
        var entries = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Information,
                Message = "Test message",
                Component = "FluentPDF.Test",
                CorrelationId = "corr-123",
                Context = new Dictionary<string, object> { ["Property1"] = "Value1" }
            }
        };

        var exportFile = Path.Combine(_testLogDirectory, "export.json");

        try
        {
            // Act
            var result = await _service.ExportLogsAsync(entries, exportFile);

            // Assert
            result.IsSuccess.Should().BeTrue();
            File.Exists(exportFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(exportFile);
            content.Should().Contain("@t");
            content.Should().Contain("@l");
            content.Should().Contain("@mt");
            content.Should().Contain("Test message");
            content.Should().Contain("corr-123");
        }
        finally
        {
            if (File.Exists(exportFile))
                File.Delete(exportFile);
        }
    }

    [Fact]
    public async Task ExportLogsAsync_WithException_IncludesExceptionData()
    {
        // Arrange
        var entries = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Error,
                Message = "Error occurred",
                Component = "FluentPDF.Test",
                Exception = "System.InvalidOperationException: Test exception",
                StackTrace = "at FluentPDF.Test.Method()"
            }
        };

        var exportFile = Path.Combine(_testLogDirectory, "export-exception.json");

        try
        {
            // Act
            var result = await _service.ExportLogsAsync(entries, exportFile);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var content = await File.ReadAllTextAsync(exportFile);
            content.Should().Contain("@x");
            content.Should().Contain("System.InvalidOperationException");
        }
        finally
        {
            if (File.Exists(exportFile))
                File.Delete(exportFile);
        }
    }

    [Fact]
    public async Task ExportLogsAsync_WithInvalidPath_ReturnsFailure()
    {
        // Arrange
        var entries = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Information,
                Message = "Test",
                Component = "Test"
            }
        };

        var invalidPath = "/invalid/path/that/does/not/exist/logs.json";

        // Act
        var result = await _service.ExportLogsAsync(entries, invalidPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Failed to export logs");
    }

    [Fact]
    public async Task LruCache_CachesAndRetrievesEntries()
    {
        // Arrange - Create a log file
        var logFile = CreateTestLogFile("test-cache.json", new[]
        {
            CreateSerilogJsonLine(DateTime.UtcNow, "Information", "Cached message", "FluentPDF.Test", null)
        });

        try
        {
            // Act - Read the same file twice
            var result1 = await _service.GetRecentLogsAsync(100);
            var result2 = await _service.GetRecentLogsAsync(100);

            // Assert - Both should succeed and return same data
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeTrue();
            result1.Value.Should().HaveCount(result2.Value.Count);
        }
        finally
        {
            if (File.Exists(logFile))
                File.Delete(logFile);
        }
    }

    private string CreateTestLogFile(string fileName, string[] jsonLines)
    {
        var filePath = Path.Combine(_testLogDirectory, fileName);
        File.WriteAllLines(filePath, jsonLines);
        return filePath;
    }

    private string CreateSerilogJsonLine(DateTime timestamp, string level, string message, string sourceContext, string? correlationId)
    {
        var logEntry = new Dictionary<string, object?>
        {
            ["@t"] = timestamp.ToString("o"),
            ["@l"] = level,
            ["@mt"] = message,
            ["SourceContext"] = sourceContext
        };

        if (!string.IsNullOrEmpty(correlationId))
        {
            logEntry["CorrelationId"] = correlationId;
        }

        return JsonSerializer.Serialize(logEntry);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testLogDirectory))
        {
            try
            {
                Directory.Delete(_testLogDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
