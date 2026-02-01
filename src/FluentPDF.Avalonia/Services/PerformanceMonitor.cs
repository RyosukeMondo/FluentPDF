using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Monitors real-time application performance including FPS and memory usage.
/// Uses high-resolution timers for accurate frame timing and Process APIs for memory tracking.
/// </summary>
public sealed class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly Subject<PerformanceMetrics> _metricsSubject;
    private readonly Queue<DateTime> _frameTimestamps;
    private readonly TimeSpan _fpsWindow;
    private readonly IDisposable? _loggingSubscription;
    private DateTime _lastMetricsLogTime;
    private PerformanceMetrics _currentMetrics;
    private bool _disposed;
    private bool _isMonitoring;
    private Timer? _monitoringTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger for performance metrics.</param>
    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsSubject = new Subject<PerformanceMetrics>();
        _frameTimestamps = new Queue<DateTime>(120); // Store ~2 seconds at 60 FPS
        _fpsWindow = TimeSpan.FromSeconds(1); // Calculate FPS over 1-second rolling window
        _lastMetricsLogTime = DateTime.UtcNow;

        // Initialize current metrics
        _currentMetrics = CreateMetrics(0, 0, 0);

        // Set up logging subscription to emit metrics every 5 seconds
        _loggingSubscription = _metricsSubject
            .Buffer(TimeSpan.FromSeconds(5))
            .Where(metrics => metrics.Any())
            .Subscribe(metrics =>
            {
                var latest = metrics.Last();
                var correlationId = Guid.NewGuid();
                Log.Information(
                    "Performance: FPS={Fps:F1}, Memory={TotalMemoryMb:F1}MB (Managed={ManagedMemoryMb:F1}MB, Native={NativeMemoryMb:F1}MB), " +
                    "AvgFrameTime={AvgFrameTimeMs:F2}ms [CorrelationId: {CorrelationId}]",
                    latest.CurrentFps,
                    latest.TotalMemoryMb,
                    latest.ManagedMemoryMb,
                    latest.NativeMemoryMb,
                    latest.AverageFrameTimeMs,
                    correlationId);
            });

        var startCorrelationId = Guid.NewGuid();
        Log.Information("PerformanceMonitor initialized [CorrelationId: {CorrelationId}]", startCorrelationId);
    }

    /// <inheritdoc/>
    public double CurrentFps => _currentMetrics.CurrentFps;

    /// <inheritdoc/>
    public double MemoryUsageMb => _currentMetrics.TotalMemoryMb;

    /// <inheritdoc/>
    public PerformanceMetrics CurrentMetrics => _currentMetrics;

    /// <inheritdoc/>
    public IObservable<PerformanceMetrics> MetricsStream => _metricsSubject.AsObservable();

    /// <inheritdoc/>
    public bool IsMonitoring => _isMonitoring;

    /// <inheritdoc/>
    public void Start()
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("PerformanceMonitor is already running");
            return;
        }

        try
        {
            // Use high-precision timer for frame monitoring (~60 FPS)
            _monitoringTimer = new Timer(
                _ => OnRenderFrame(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(16.67)); // ~60 FPS

            _isMonitoring = true;
            _lastMetricsLogTime = DateTime.UtcNow;

            var correlationId = Guid.NewGuid();
            Log.Information("PerformanceMonitor started [CorrelationId: {CorrelationId}]", correlationId);
            _logger.LogInformation("PerformanceMonitor started");
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            Log.Error(ex, "Failed to start PerformanceMonitor [CorrelationId: {CorrelationId}]", correlationId);
            _logger.LogError(ex, "Failed to start PerformanceMonitor");
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
        _isMonitoring = false;

        var correlationId = Guid.NewGuid();
        Log.Information("PerformanceMonitor stopped [CorrelationId: {CorrelationId}]", correlationId);
        _logger.LogInformation("PerformanceMonitor stopped");
    }

    private void OnRenderFrame()
    {
        var now = DateTime.UtcNow;

        // Record frame timestamp
        _frameTimestamps.Enqueue(now);

        // Remove timestamps outside the FPS window
        while (_frameTimestamps.Count > 0 &&
               (now - _frameTimestamps.Peek()) > _fpsWindow)
        {
            _frameTimestamps.Dequeue();
        }

        // Calculate FPS over the rolling window
        var fps = _frameTimestamps.Count / _fpsWindow.TotalSeconds;

        // Get memory usage
        var (managedMb, nativeMb) = GetMemoryUsage();

        // Update current metrics
        _currentMetrics = CreateMetrics(fps, managedMb, nativeMb);

        // Emit metrics to stream (throttled by buffer operator)
        _metricsSubject.OnNext(_currentMetrics);
    }

    private static (double managedMb, double nativeMb) GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();

            // Managed memory (GC heap)
            var managedBytes = GC.GetTotalMemory(forceFullCollection: false);
            var managedMb = managedBytes / 1024.0 / 1024.0;

            // Total memory (including native)
            var totalBytes = process.WorkingSet64;
            var totalMb = totalBytes / 1024.0 / 1024.0;

            // Native memory = Total - Managed
            var nativeMb = totalMb - managedMb;

            return (managedMb, nativeMb);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get memory usage");
            return (0, 0);
        }
    }

    private static PerformanceMetrics CreateMetrics(
        double fps,
        double managedMb,
        double nativeMb)
    {
        return new PerformanceMetrics
        {
            CurrentFps = fps,
            ManagedMemoryMb = managedMb,
            NativeMemoryMb = nativeMb,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Disposes the performance monitor and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _loggingSubscription?.Dispose();
        _metricsSubject?.Dispose();
        _disposed = true;

        var correlationId = Guid.NewGuid();
        Log.Information("PerformanceMonitor disposed [CorrelationId: {CorrelationId}]", correlationId);
    }
}
