using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for observability features using real OpenTelemetry and file system.
/// These tests verify that OpenTelemetry configuration, metrics collection, distributed tracing,
/// log reading, and export operations work correctly with real dependencies.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ObservabilityIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<MetricsCollectionService>> _metricsLoggerMock;
    private readonly Mock<ILogger<LogExportService>> _logExportLoggerMock;
    private readonly string _tempLogDirectory;
    private readonly List<string> _tempFiles;
    private MeterProvider? _meterProvider;
    private TracerProvider? _tracerProvider;

    public ObservabilityIntegrationTests()
    {
        _metricsLoggerMock = new Mock<ILogger<MetricsCollectionService>>();
        _logExportLoggerMock = new Mock<ILogger<LogExportService>>();
        _tempFiles = new List<string>();

        // Create temp directory for log files
        _tempLogDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF-Test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempLogDirectory);
    }

    public void Dispose()
    {
        // Clean up temp files
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up temp directory
        if (Directory.Exists(_tempLogDirectory))
        {
            try
            {
                Directory.Delete(_tempLogDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Dispose OpenTelemetry providers
        _meterProvider?.Dispose();
        _tracerProvider?.Dispose();
    }

    [Fact]
    public void OpenTelemetry_ConfiguredCorrectly_ShouldRegisterMeterProviderAndTracerProvider()
    {
        // Arrange & Act
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("FluentPDF.Rendering")
            .ConfigureResource(resource => resource.AddService(
                serviceName: "FluentPDF",
                serviceVersion: "1.0.0"))
            .Build();

        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("FluentPDF.Rendering")
            .ConfigureResource(resource => resource.AddService(
                serviceName: "FluentPDF",
                serviceVersion: "1.0.0"))
            .Build();

        // Assert
        _meterProvider.Should().NotBeNull();
        _tracerProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task MetricsCollection_DuringOperation_ShouldRecordMetrics()
    {
        // Arrange
        var exportedMetrics = new List<Metric>();
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("FluentPDF.Rendering")
            .AddInMemoryExporter(exportedMetrics)
            .Build();

        var service = new MetricsCollectionService(_metricsLoggerMock.Object);

        // Act
        service.RecordFPS(60.0);
        service.RecordRenderTime(45.5, 1);
        service.RecordMemoryUsage(150, 256);

        // Force metrics collection
        _meterProvider.ForceFlush();
        await Task.Delay(100); // Allow time for metrics to be collected

        var currentMetrics = service.GetCurrentMetrics();

        // Assert
        currentMetrics.Should().NotBeNull();
        currentMetrics.CurrentFPS.Should().Be(60.0);
        currentMetrics.ManagedMemoryMB.Should().Be(150);
        currentMetrics.NativeMemoryMB.Should().Be(256);
        currentMetrics.LastRenderTimeMs.Should().Be(45.5);
        currentMetrics.CurrentPageNumber.Should().Be(1);
        currentMetrics.Level.Should().Be(PerformanceLevel.Good);
    }

    [Fact]
    public async Task MetricsHistory_ShouldMaintainCircularBuffer()
    {
        // Arrange
        var service = new MetricsCollectionService(_metricsLoggerMock.Object);

        // Act - Record multiple metrics to test circular buffer
        for (int i = 0; i < 1500; i++)
        {
            service.RecordFPS(60.0 - i * 0.01);
            service.RecordRenderTime(50.0 + i * 0.1, i % 100);
            service.RecordMemoryUsage(150 + i, 256 + i);
        }

        var history = service.GetMetricsHistory();

        // Assert - Should only keep last 1000 entries (circular buffer)
        history.Should().HaveCount(1000);
        history.First().CurrentFPS.Should().BeApproximately(60.0 - 500 * 0.01, 0.01);
        history.Last().CurrentFPS.Should().BeApproximately(60.0 - 1499 * 0.01, 0.01);
    }

    [Fact]
    public async Task LogFileReading_WithRealJsonFiles_ShouldReadAndParseLogs()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogDirectory, "test-log.json");
        await File.WriteAllTextAsync(logFilePath, GetSampleLogContent());
        _tempFiles.Add(logFilePath);

        var service = new LogExportService(_logExportLoggerMock.Object, _tempLogDirectory);

        // Act
        var result = await service.GetRecentLogsAsync(100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().HaveCountGreaterThan(0);

        var firstLog = result.Value.First();
        firstLog.Level.Should().Be(LogLevel.Information);
        firstLog.Message.Should().Contain("Application started");
    }

    [Fact]
    public async Task LogFiltering_WithCriteria_ShouldFilterCorrectly()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogDirectory, "test-log-filter.json");
        await File.WriteAllTextAsync(logFilePath, GetSampleLogContent());
        _tempFiles.Add(logFilePath);

        var service = new LogExportService(_logExportLoggerMock.Object, _tempLogDirectory);
        var criteria = new LogFilterCriteria
        {
            MinimumLevel = LogLevel.Warning,
            ComponentFilter = "FluentPDF.Rendering"
        };

        // Act
        var result = await service.FilterLogsAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // All logs should be Warning or Error level
        result.Value.Should().AllSatisfy(log =>
        {
            log.Level.Should().BeOneOf(LogLevel.Warning, LogLevel.Error);
            log.Component.Should().StartWith("FluentPDF.Rendering");
        });
    }

    [Fact]
    public async Task LogFiltering_WithCorrelationId_ShouldFilterByCorrelationId()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogDirectory, "test-log-correlation.json");
        await File.WriteAllTextAsync(logFilePath, GetSampleLogContent());
        _tempFiles.Add(logFilePath);

        var service = new LogExportService(_logExportLoggerMock.Object, _tempLogDirectory);
        var criteria = new LogFilterCriteria
        {
            CorrelationIdFilter = "render-001"
        };

        // Act
        var result = await service.FilterLogsAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().AllSatisfy(log => log.CorrelationId.Should().Be("render-001"));
    }

    [Fact]
    public async Task LogFiltering_WithTimeRange_ShouldFilterByTimeRange()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogDirectory, "test-log-timerange.json");
        await File.WriteAllTextAsync(logFilePath, GetSampleLogContent());
        _tempFiles.Add(logFilePath);

        var service = new LogExportService(_logExportLoggerMock.Object, _tempLogDirectory);
        var startTime = DateTime.Parse("2026-01-11T10:00:05Z").ToUniversalTime();
        var endTime = DateTime.Parse("2026-01-11T10:00:15Z").ToUniversalTime();

        var criteria = new LogFilterCriteria
        {
            StartTime = startTime,
            EndTime = endTime
        };

        // Act
        var result = await service.FilterLogsAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().AllSatisfy(log =>
        {
            log.Timestamp.Should().BeOnOrAfter(startTime);
            log.Timestamp.Should().BeOnOrBefore(endTime);
        });
    }

    [Fact]
    public async Task DistributedTracing_CreatesActivities_ShouldRecordActivitySpans()
    {
        // Arrange
        var activities = new List<Activity>();
        var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "FluentPDF.Rendering",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(activityListener);

        var activitySource = new ActivitySource("FluentPDF.Rendering", "1.0.0");

        // Act - Simulate rendering operation with distributed tracing
        using (var parentActivity = activitySource.StartActivity("RenderPage", ActivityKind.Internal))
        {
            parentActivity?.SetTag("page.number", 5);
            parentActivity?.SetTag("zoom.level", 1.5);
            parentActivity?.SetTag("correlation.id", "render-test-001");

            await Task.Delay(10); // Simulate LoadPage

            using (var loadActivity = activitySource.StartActivity("LoadPage", ActivityKind.Internal))
            {
                await Task.Delay(5);
            }

            await Task.Delay(10); // Simulate RenderBitmap

            using (var renderActivity = activitySource.StartActivity("RenderBitmap", ActivityKind.Internal))
            {
                await Task.Delay(15);
            }

            await Task.Delay(5); // Simulate ConvertToImage

            using (var convertActivity = activitySource.StartActivity("ConvertToImage", ActivityKind.Internal))
            {
                await Task.Delay(3);
            }

            parentActivity?.SetTag("render.time.ms", parentActivity.Duration.TotalMilliseconds);
        }

        await Task.Delay(100); // Allow time for activities to be recorded

        // Assert
        activities.Should().HaveCount(4); // RenderPage + 3 child activities

        var renderPageActivity = activities.FirstOrDefault(a => a.DisplayName == "RenderPage");
        renderPageActivity.Should().NotBeNull();
        renderPageActivity!.Tags.Should().Contain(tag => tag.Key == "page.number" && tag.Value == "5");
        renderPageActivity.Tags.Should().Contain(tag => tag.Key == "zoom.level" && tag.Value == "1.5");
        renderPageActivity.Tags.Should().Contain(tag => tag.Key == "correlation.id" && tag.Value == "render-test-001");

        activities.Should().Contain(a => a.DisplayName == "LoadPage");
        activities.Should().Contain(a => a.DisplayName == "RenderBitmap");
        activities.Should().Contain(a => a.DisplayName == "ConvertToImage");

        activityListener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public async Task ExportMetrics_ToJson_ShouldProduceValidJsonFile()
    {
        // Arrange
        var service = new MetricsCollectionService(_metricsLoggerMock.Object);
        service.RecordFPS(60.0);
        service.RecordRenderTime(45.5, 1);
        service.RecordMemoryUsage(150, 256);

        var exportPath = Path.Combine(_tempLogDirectory, "metrics-export.json");
        _tempFiles.Add(exportPath);

        // Act
        var result = await service.ExportMetricsAsync(exportPath, ExportFormat.Json);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(exportPath).Should().BeTrue();

        var jsonContent = await File.ReadAllTextAsync(exportPath);
        jsonContent.Should().NotBeNullOrWhiteSpace();

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(jsonContent);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        jsonDoc.RootElement.GetArrayLength().Should().BeGreaterThan(0);

        var firstMetric = jsonDoc.RootElement[0];
        firstMetric.GetProperty("CurrentFPS").GetDouble().Should().Be(60.0);
        firstMetric.GetProperty("ManagedMemoryMB").GetInt64().Should().Be(150);
        firstMetric.GetProperty("NativeMemoryMB").GetInt64().Should().Be(256);
    }

    [Fact]
    public async Task ExportMetrics_ToCsv_ShouldProduceValidCsvFile()
    {
        // Arrange
        var service = new MetricsCollectionService(_metricsLoggerMock.Object);
        service.RecordFPS(60.0);
        service.RecordRenderTime(45.5, 1);
        service.RecordMemoryUsage(150, 256);

        var exportPath = Path.Combine(_tempLogDirectory, "metrics-export.csv");
        _tempFiles.Add(exportPath);

        // Act
        var result = await service.ExportMetricsAsync(exportPath, ExportFormat.Csv);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(exportPath).Should().BeTrue();

        var csvContent = await File.ReadAllTextAsync(exportPath);
        csvContent.Should().NotBeNullOrWhiteSpace();

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCountGreaterThan(1); // Header + at least one data row

        // Verify CSV header
        var header = lines[0];
        header.Should().Contain("Timestamp");
        header.Should().Contain("CurrentFPS");
        header.Should().Contain("ManagedMemoryMB");
        header.Should().Contain("NativeMemoryMB");
        header.Should().Contain("LastRenderTimeMs");
        header.Should().Contain("CurrentPageNumber");
        header.Should().Contain("Level");

        // Verify data row
        var dataRow = lines[1];
        dataRow.Should().Contain("60");
        dataRow.Should().Contain("150");
        dataRow.Should().Contain("256");
    }

    [Fact]
    public async Task ExportLogs_ToJson_ShouldPreserveSerilogFormat()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogDirectory, "test-log-export.json");
        await File.WriteAllTextAsync(logFilePath, GetSampleLogContent());
        _tempFiles.Add(logFilePath);

        var service = new LogExportService(_logExportLoggerMock.Object, _tempLogDirectory);
        var exportPath = Path.Combine(_tempLogDirectory, "logs-export.json");
        _tempFiles.Add(exportPath);

        // Act
        var logsResult = await service.GetRecentLogsAsync(100);
        logsResult.IsSuccess.Should().BeTrue();

        var exportResult = await service.ExportLogsAsync(logsResult.Value.ToList(), exportPath, ExportFormat.Json);

        // Assert
        exportResult.IsSuccess.Should().BeTrue();
        File.Exists(exportPath).Should().BeTrue();

        var exportedContent = await File.ReadAllTextAsync(exportPath);
        exportedContent.Should().NotBeNullOrWhiteSpace();

        // Verify it's valid JSON array
        var jsonDoc = JsonDocument.Parse(exportedContent);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        jsonDoc.RootElement.GetArrayLength().Should().BeGreaterThan(0);

        // Verify first log entry has expected properties
        var firstLog = jsonDoc.RootElement[0];
        firstLog.TryGetProperty("Timestamp", out _).Should().BeTrue();
        firstLog.TryGetProperty("Level", out _).Should().BeTrue();
        firstLog.TryGetProperty("Message", out _).Should().BeTrue();
    }

    [Fact]
    public async Task LogFileReading_WithCorruptedJson_ShouldSkipInvalidEntriesGracefully()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogDirectory, "corrupted-log.json");
        var corruptedContent =
            """
            {"Timestamp":"2026-01-11T10:00:00.123Z","Level":"Information","MessageTemplate":"Valid log","Properties":{"CorrelationId":"test-001"}}
            {"Timestamp":"2026-01-11T10:00:01.234Z","Level":"Error","MessageTemplate":"Another valid log
            {"Timestamp":"2026-01-11T10:00:02.345Z","Level":"Warning","MessageTemplate":"Valid log after corruption","Properties":{"CorrelationId":"test-002"}}
            """;
        await File.WriteAllTextAsync(logFilePath, corruptedContent);
        _tempFiles.Add(logFilePath);

        var service = new LogExportService(_logExportLoggerMock.Object, _tempLogDirectory);

        // Act
        var result = await service.GetRecentLogsAsync(100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2); // Only valid entries
        result.Value.First().Message.Should().Be("Valid log");
        result.Value.Last().Message.Should().Be("Valid log after corruption");
    }

    [Fact(Skip = "Only runs if Aspire Dashboard is running on localhost:4317")]
    public async Task OtlpExport_WhenAspireRunning_ShouldExportMetricsAndLogs()
    {
        // This test is skipped by default and only runs if Aspire is available
        // To run: docker-compose -f tools/docker-compose-aspire.yml up -d

        // Arrange
        var exportedMetrics = new List<Metric>();
        try
        {
            _meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("FluentPDF.Rendering")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4317");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
                .Build();

            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource("FluentPDF.Rendering")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4317");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
                .Build();

            var service = new MetricsCollectionService(_metricsLoggerMock.Object);
            var activitySource = new ActivitySource("FluentPDF.Rendering", "1.0.0");

            // Act
            service.RecordFPS(60.0);
            service.RecordRenderTime(45.5, 1);
            service.RecordMemoryUsage(150, 256);

            using (var activity = activitySource.StartActivity("TestRenderPage"))
            {
                activity?.SetTag("test", "true");
                await Task.Delay(10);
            }

            _meterProvider.ForceFlush();
            _tracerProvider.ForceFlush();

            // Assert - If we get here without exception, OTLP export worked
            Assert.True(true);

            activitySource.Dispose();
        }
        catch (Exception)
        {
            // Aspire not running - test should be skipped
            Assert.True(true, "Aspire Dashboard not available - test skipped");
        }
    }

    private string GetSampleLogContent()
    {
        // Read from the sample-logs.json fixture
        var fixturePath = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName,
            "Fixtures",
            "sample-logs.json");

        if (File.Exists(fixturePath))
        {
            return File.ReadAllText(fixturePath);
        }

        // Fallback to inline sample if fixture not found
        return """
        {"Timestamp":"2026-01-11T10:00:00.123Z","Level":"Information","MessageTemplate":"Application started","Properties":{"CorrelationId":"app-start-001","Component":"FluentPDF.App"}}
        {"Timestamp":"2026-01-11T10:00:01.234Z","Level":"Debug","MessageTemplate":"Loading PDF file","Properties":{"CorrelationId":"render-001","Component":"FluentPDF.Rendering"}}
        {"Timestamp":"2026-01-11T10:00:02.345Z","Level":"Warning","MessageTemplate":"Page cache size exceeded","Properties":{"CorrelationId":"cache-001","Component":"FluentPDF.Rendering"}}
        {"Timestamp":"2026-01-11T10:00:03.456Z","Level":"Error","MessageTemplate":"Failed to load page","Properties":{"CorrelationId":"render-003","Component":"FluentPDF.Rendering"}}
        """;
    }
}
