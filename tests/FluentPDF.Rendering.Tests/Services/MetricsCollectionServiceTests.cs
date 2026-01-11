using FluentAssertions;
using FluentPDF.Core.Observability;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.Rendering.Tests.Services;

public sealed class MetricsCollectionServiceTests : IDisposable
{
    private readonly Mock<ILogger<MetricsCollectionService>> _loggerMock;
    private readonly MetricsCollectionService _service;

    public MetricsCollectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<MetricsCollectionService>>();
        _service = new MetricsCollectionService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MetricsCollectionService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void RecordFPS_UpdatesCurrentFPS()
    {
        // Arrange
        const double expectedFPS = 60.5;

        // Act
        _service.RecordFPS(expectedFPS);
        var metrics = _service.GetCurrentMetrics();

        // Assert
        metrics.CurrentFPS.Should().Be(expectedFPS);
    }

    [Fact]
    public void RecordRenderTime_UpdatesLastRenderTimeAndPageNumber()
    {
        // Arrange
        const int pageNumber = 5;
        const double renderTime = 123.45;

        // Act
        _service.RecordRenderTime(pageNumber, renderTime);
        var metrics = _service.GetCurrentMetrics();

        // Assert
        metrics.CurrentPageNumber.Should().Be(pageNumber);
        metrics.LastRenderTimeMs.Should().Be(renderTime);
    }

    [Fact]
    public void RecordMemoryUsage_UpdatesMemoryMetrics()
    {
        // Arrange
        const long managedMemory = 256;
        const long nativeMemory = 128;

        // Act
        _service.RecordMemoryUsage(managedMemory, nativeMemory);
        var metrics = _service.GetCurrentMetrics();

        // Assert
        metrics.ManagedMemoryMB.Should().Be(managedMemory);
        metrics.NativeMemoryMB.Should().Be(nativeMemory);
        metrics.TotalMemoryMB.Should().Be(managedMemory + nativeMemory);
    }

    [Fact]
    public void GetCurrentMetrics_CalculatesPerformanceLevelCorrectly_Good()
    {
        // Arrange
        _service.RecordFPS(60);
        _service.RecordMemoryUsage(200, 100);

        // Act
        var metrics = _service.GetCurrentMetrics();

        // Assert
        metrics.Level.Should().Be(PerformanceLevel.Good);
    }

    [Fact]
    public void GetCurrentMetrics_CalculatesPerformanceLevelCorrectly_Warning()
    {
        // Arrange
        _service.RecordFPS(25);
        _service.RecordMemoryUsage(300, 250);

        // Act
        var metrics = _service.GetCurrentMetrics();

        // Assert
        metrics.Level.Should().Be(PerformanceLevel.Warning);
    }

    [Fact]
    public void GetCurrentMetrics_CalculatesPerformanceLevelCorrectly_Critical()
    {
        // Arrange
        _service.RecordFPS(10);
        _service.RecordMemoryUsage(600, 500);

        // Act
        var metrics = _service.GetCurrentMetrics();

        // Assert
        metrics.Level.Should().Be(PerformanceLevel.Critical);
    }

    [Fact]
    public void GetCurrentMetrics_AddsToHistory()
    {
        // Act
        _service.RecordFPS(30);
        _service.GetCurrentMetrics();
        _service.RecordFPS(40);
        _service.GetCurrentMetrics();
        _service.RecordFPS(50);
        _service.GetCurrentMetrics();

        var history = _service.GetMetricsHistory(TimeSpan.FromMinutes(5));

        // Assert
        history.Should().HaveCount(3);
        history[0].CurrentFPS.Should().Be(30);
        history[1].CurrentFPS.Should().Be(40);
        history[2].CurrentFPS.Should().Be(50);
    }

    [Fact]
    public void GetMetricsHistory_FiltersOldMetrics()
    {
        // Arrange - Create a history entry and wait a bit
        _service.RecordFPS(30);
        var oldMetric = _service.GetCurrentMetrics();

        // Wait a tiny bit to ensure timestamp difference
        Thread.Sleep(10);

        _service.RecordFPS(60);
        _service.GetCurrentMetrics();

        // Act - Request only metrics from the last millisecond
        var history = _service.GetMetricsHistory(TimeSpan.FromMilliseconds(5));

        // Assert - Should only get the most recent one
        history.Should().HaveCountGreaterThanOrEqualTo(1);
        history.Should().NotContain(m => m.Timestamp == oldMetric.Timestamp);
    }

    [Fact]
    public void GetMetricsHistory_ReturnsOrderedByTimestamp()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _service.RecordFPS(i * 10);
            _service.GetCurrentMetrics();
        }

        // Act
        var history = _service.GetMetricsHistory(TimeSpan.FromMinutes(5));

        // Assert
        history.Should().BeInAscendingOrder(m => m.Timestamp);
    }

    [Fact]
    public async Task ExportMetricsAsync_WithJsonFormat_CreatesValidJsonFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.RecordFPS(60);
            _service.RecordMemoryUsage(256, 128);
            _service.RecordRenderTime(1, 15.5);
            _service.GetCurrentMetrics();

            // Act
            var result = await _service.ExportMetricsAsync(tempFile, ExportFormat.Json);

            // Assert
            result.IsSuccess.Should().BeTrue();
            File.Exists(tempFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("currentFPS");
            content.Should().Contain("managedMemoryMB");
            content.Should().Contain("nativeMemoryMB");
            content.Should().Contain("60");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportMetricsAsync_WithCsvFormat_CreatesValidCsvFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            _service.RecordFPS(45);
            _service.RecordMemoryUsage(200, 100);
            _service.RecordRenderTime(2, 20.3);
            _service.GetCurrentMetrics();

            // Act
            var result = await _service.ExportMetricsAsync(tempFile, ExportFormat.Csv);

            // Assert
            result.IsSuccess.Should().BeTrue();
            File.Exists(tempFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("Timestamp,CurrentFPS,ManagedMemoryMB");
            content.Should().Contain("45");
            content.Should().Contain("200");
            content.Should().Contain("100");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportMetricsAsync_WithNoMetrics_ReturnsFailure()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act - Export without recording any metrics
            var result = await _service.ExportMetricsAsync(tempFile, ExportFormat.Json);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Contain("No metrics available");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportMetricsAsync_WithInvalidPath_ReturnsFailure()
    {
        // Arrange
        _service.RecordFPS(30);
        _service.GetCurrentMetrics();
        var invalidPath = "/invalid/path/that/does/not/exist/metrics.json";

        // Act
        var result = await _service.ExportMetricsAsync(invalidPath, ExportFormat.Json);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Failed to export metrics");
    }

    [Fact]
    public void CircularBuffer_HandlesOverflow_OverwritesOldestEntries()
    {
        // Arrange - Record more than 1000 metrics to test circular buffer
        for (int i = 0; i < 1100; i++)
        {
            _service.RecordFPS(i);
            _service.GetCurrentMetrics();
        }

        // Act
        var history = _service.GetMetricsHistory(TimeSpan.FromHours(1));

        // Assert - Should only have 1000 entries (buffer capacity)
        history.Should().HaveCount(1000);
        // First entry should be from iteration 100 (0-99 were overwritten)
        history[0].CurrentFPS.Should().Be(100);
        // Last entry should be from iteration 1099
        history[^1].CurrentFPS.Should().Be(1099);
    }

    [Fact]
    public void GetCurrentMetrics_SetsTimestampToUtcNow()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var metrics = _service.GetCurrentMetrics();

        // Assert
        var afterCall = DateTime.UtcNow;
        metrics.Timestamp.Should().BeOnOrAfter(beforeCall);
        metrics.Timestamp.Should().BeOnOrBefore(afterCall);
        metrics.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Act
        _service.Dispose();

        // Assert - Should not throw
        // Verify that dispose was logged
        _loggerMock.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
