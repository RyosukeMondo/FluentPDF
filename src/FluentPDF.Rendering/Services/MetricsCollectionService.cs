using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Implements metrics collection using OpenTelemetry and in-memory storage.
/// </summary>
public sealed class MetricsCollectionService : IMetricsCollectionService, IDisposable
{
    private readonly ILogger<MetricsCollectionService> _logger;
    private readonly Meter _meter;
    private readonly CircularBuffer<PerformanceMetrics> _metricsHistory;

    // Current metric values
    private double _currentFPS;
    private long _managedMemoryMB;
    private long _nativeMemoryMB;
    private double _lastRenderTimeMs;
    private int _currentPageNumber;

    // OpenTelemetry instruments
    private readonly ObservableGauge<double> _fpsGauge;
    private readonly ObservableGauge<long> _managedMemoryGauge;
    private readonly ObservableGauge<long> _nativeMemoryGauge;
    private readonly Histogram<double> _renderTimeHistogram;

    public MetricsCollectionService(ILogger<MetricsCollectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsHistory = new CircularBuffer<PerformanceMetrics>(1000);

        // Create meter for FluentPDF.Rendering
        _meter = new Meter("FluentPDF.Rendering", "1.0.0");

        // Create OpenTelemetry instruments
        _fpsGauge = _meter.CreateObservableGauge(
            "fluentpdf.rendering.fps",
            () => _currentFPS,
            description: "Current frames per second");

        _managedMemoryGauge = _meter.CreateObservableGauge(
            "fluentpdf.rendering.memory.managed",
            () => _managedMemoryMB,
            unit: "MB",
            description: "Managed memory usage in megabytes");

        _nativeMemoryGauge = _meter.CreateObservableGauge(
            "fluentpdf.rendering.memory.native",
            () => _nativeMemoryMB,
            unit: "MB",
            description: "Native memory usage in megabytes");

        _renderTimeHistogram = _meter.CreateHistogram<double>(
            "fluentpdf.rendering.page.render_time",
            unit: "ms",
            description: "Page render time in milliseconds");

        _logger.LogInformation("MetricsCollectionService initialized with OpenTelemetry instruments");
    }

    /// <inheritdoc />
    public void RecordFPS(double fps)
    {
        _currentFPS = fps;
        _logger.LogDebug("Recorded FPS: {FPS}", fps);
    }

    /// <inheritdoc />
    public void RecordRenderTime(int pageNumber, double milliseconds)
    {
        _currentPageNumber = pageNumber;
        _lastRenderTimeMs = milliseconds;
        _renderTimeHistogram.Record(milliseconds,
            new KeyValuePair<string, object?>("page.number", pageNumber));

        _logger.LogDebug("Recorded render time for page {PageNumber}: {RenderTimeMs}ms",
            pageNumber, milliseconds);
    }

    /// <inheritdoc />
    public void RecordMemoryUsage(long managedMB, long nativeMB)
    {
        _managedMemoryMB = managedMB;
        _nativeMemoryMB = nativeMB;

        _logger.LogDebug("Recorded memory usage - Managed: {ManagedMB}MB, Native: {NativeMB}MB",
            managedMB, nativeMB);
    }

    /// <inheritdoc />
    public PerformanceMetrics GetCurrentMetrics()
    {
        var totalMemoryMB = _managedMemoryMB + _nativeMemoryMB;
        var level = PerformanceMetrics.CalculateLevel(_currentFPS, totalMemoryMB);

        var metrics = new PerformanceMetrics
        {
            CurrentFPS = _currentFPS,
            ManagedMemoryMB = _managedMemoryMB,
            NativeMemoryMB = _nativeMemoryMB,
            LastRenderTimeMs = _lastRenderTimeMs,
            CurrentPageNumber = _currentPageNumber,
            Timestamp = DateTime.UtcNow,
            Level = level
        };

        // Add to circular buffer
        _metricsHistory.Add(metrics);

        return metrics;
    }

    /// <inheritdoc />
    public IReadOnlyList<PerformanceMetrics> GetMetricsHistory(TimeSpan duration)
    {
        var cutoffTime = DateTime.UtcNow - duration;
        var items = _metricsHistory.GetItems();

        var filteredMetrics = items
            .Where(m => m.Timestamp >= cutoffTime)
            .OrderBy(m => m.Timestamp)
            .ToList();

        _logger.LogDebug("Retrieved {Count} metrics from history within {Duration}",
            filteredMetrics.Count, duration);

        return filteredMetrics.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Result> ExportMetricsAsync(string filePath, ExportFormat format)
    {
        try
        {
            _logger.LogInformation("Exporting metrics to {FilePath} in {Format} format",
                filePath, format);

            var metrics = _metricsHistory.GetItems().ToList();

            if (metrics.Count == 0)
            {
                return Result.Fail("No metrics available to export");
            }

            switch (format)
            {
                case ExportFormat.Json:
                    await ExportAsJsonAsync(filePath, metrics);
                    break;

                case ExportFormat.Csv:
                    await ExportAsCsvAsync(filePath, metrics);
                    break;

                default:
                    return Result.Fail($"Unsupported export format: {format}");
            }

            _logger.LogInformation("Successfully exported {Count} metrics to {FilePath}",
                metrics.Count, filePath);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export metrics to {FilePath}", filePath);
            return Result.Fail($"Failed to export metrics: {ex.Message}");
        }
    }

    private static async Task ExportAsJsonAsync(string filePath, List<PerformanceMetrics> metrics)
    {
        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    private static async Task ExportAsCsvAsync(string filePath, List<PerformanceMetrics> metrics)
    {
        var csv = new StringBuilder();

        // CSV header
        csv.AppendLine("Timestamp,CurrentFPS,ManagedMemoryMB,NativeMemoryMB,TotalMemoryMB," +
                      "LastRenderTimeMs,CurrentPageNumber,Level");

        // CSV rows
        foreach (var metric in metrics)
        {
            csv.AppendLine($"{metric.Timestamp:O}," +
                          $"{metric.CurrentFPS}," +
                          $"{metric.ManagedMemoryMB}," +
                          $"{metric.NativeMemoryMB}," +
                          $"{metric.TotalMemoryMB}," +
                          $"{metric.LastRenderTimeMs}," +
                          $"{metric.CurrentPageNumber}," +
                          $"{metric.Level}");
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());
    }

    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("MetricsCollectionService disposed");
    }
}

/// <summary>
/// A circular buffer with fixed capacity that overwrites oldest items when full.
/// Provides O(1) insertion performance.
/// </summary>
internal sealed class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    private readonly object _lock = new();

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than zero", nameof(capacity));
        }

        _buffer = new T[capacity];
        _head = 0;
        _count = 0;
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
            {
                _count++;
            }
        }
    }

    public IEnumerable<T> GetItems()
    {
        lock (_lock)
        {
            var result = new T[_count];

            if (_count == 0)
            {
                return result;
            }

            // If buffer is not full, items are from 0 to _count-1
            if (_count < _buffer.Length)
            {
                Array.Copy(_buffer, 0, result, 0, _count);
            }
            // If buffer is full, items are from _head to end, then 0 to _head-1
            else
            {
                var itemsAfterHead = _buffer.Length - _head;
                Array.Copy(_buffer, _head, result, 0, itemsAfterHead);
                Array.Copy(_buffer, 0, result, itemsAfterHead, _head);
            }

            return result;
        }
    }
}
