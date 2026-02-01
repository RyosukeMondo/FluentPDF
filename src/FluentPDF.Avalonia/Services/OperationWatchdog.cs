using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Monitors long-running operations and detects hangs/freezes.
/// Provides autonomous error detection for CLI/REST API monitoring.
/// </summary>
public interface IOperationWatchdog
{
    /// <summary>
    /// Starts monitoring an operation with a timeout.
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="operationName">Human-readable operation name</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 30000)</param>
    void StartOperation(string operationId, string operationName, int timeoutMs = 30000);

    /// <summary>
    /// Marks an operation as completed successfully.
    /// </summary>
    void CompleteOperation(string operationId);

    /// <summary>
    /// Marks an operation as failed with an error.
    /// </summary>
    void FailOperation(string operationId, Exception exception);

    /// <summary>
    /// Gets all currently monitored operations.
    /// </summary>
    OperationStatus[] GetActiveOperations();

    /// <summary>
    /// Gets operations that have exceeded their timeout (likely hung).
    /// </summary>
    OperationStatus[] GetHungOperations();

    /// <summary>
    /// Checks if the application is healthy (no hung operations).
    /// </summary>
    bool IsHealthy();
}

/// <summary>
/// Status of a monitored operation.
/// </summary>
public record OperationStatus(
    string OperationId,
    string OperationName,
    DateTimeOffset StartTime,
    int TimeoutMs,
    bool IsHung,
    TimeSpan ElapsedTime,
    string? ErrorMessage = null);

/// <summary>
/// Implementation of operation watchdog.
/// </summary>
public class OperationWatchdog : IOperationWatchdog, IDisposable
{
    private readonly ILogger<OperationWatchdog> _logger;
    private readonly ILogBufferService? _logBuffer;
    private readonly ConcurrentDictionary<string, TrackedOperation> _operations;
    private readonly Timer _monitorTimer;
    private bool _disposed;

    private class TrackedOperation
    {
        public string OperationId { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
        public int TimeoutMs { get; set; }
        public bool IsCompleted { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public OperationWatchdog(
        ILogger<OperationWatchdog> logger,
        ILogBufferService? logBuffer = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logBuffer = logBuffer;
        _operations = new ConcurrentDictionary<string, TrackedOperation>();

        // Monitor every 5 seconds for hung operations
        _monitorTimer = new Timer(CheckForHungOperations, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        _logger.LogInformation("OperationWatchdog started - monitoring for hangs");
        _logBuffer?.AddLog("Info", "OperationWatchdog initialized - autonomous error detection enabled", "Watchdog");
    }

    public void StartOperation(string operationId, string operationName, int timeoutMs = 30000)
    {
        var operation = new TrackedOperation
        {
            OperationId = operationId,
            OperationName = operationName,
            TimeoutMs = timeoutMs
        };

        _operations[operationId] = operation;

        _logger.LogInformation("Watchdog: Started monitoring '{Operation}' (timeout: {Timeout}ms)", operationName, timeoutMs);
        _logBuffer?.AddLog("Info", $"🔍 WATCHDOG: Monitoring '{operationName}' (timeout: {timeoutMs}ms)", "Watchdog");
    }

    public void CompleteOperation(string operationId)
    {
        if (_operations.TryGetValue(operationId, out var operation))
        {
            operation.IsCompleted = true;
            operation.Stopwatch.Stop();

            _logger.LogInformation("Watchdog: Operation '{Operation}' completed in {Elapsed}ms",
                operation.OperationName, operation.Stopwatch.ElapsedMilliseconds);

            _logBuffer?.AddLog("Info",
                $"✅ WATCHDOG: '{operation.OperationName}' completed ({operation.Stopwatch.ElapsedMilliseconds}ms)",
                "Watchdog");

            // Remove from tracking after 10 seconds
            Task.Delay(10000).ContinueWith(_ =>
            {
                TrackedOperation? removed;
                _operations.TryRemove(operationId, out removed);
            });
        }
    }

    public void FailOperation(string operationId, Exception exception)
    {
        if (_operations.TryGetValue(operationId, out var operation))
        {
            operation.IsCompleted = true;
            operation.ErrorMessage = exception.Message;
            operation.Stopwatch.Stop();

            _logger.LogError(exception, "Watchdog: Operation '{Operation}' failed after {Elapsed}ms",
                operation.OperationName, operation.Stopwatch.ElapsedMilliseconds);

            _logBuffer?.AddLog("Error",
                $"❌ WATCHDOG: '{operation.OperationName}' FAILED after {operation.Stopwatch.ElapsedMilliseconds}ms - {exception.Message}",
                "Watchdog");

            // Keep failed operations for 30 seconds for debugging
            Task.Delay(30000).ContinueWith(_ =>
            {
                TrackedOperation? removed;
                _operations.TryRemove(operationId, out removed);
            });
        }
    }

    public OperationStatus[] GetActiveOperations()
    {
        var result = new List<OperationStatus>();

        foreach (var kvp in _operations)
        {
            var op = kvp.Value;
            if (!op.IsCompleted)
            {
                var elapsed = op.Stopwatch.Elapsed;
                var isHung = elapsed.TotalMilliseconds > op.TimeoutMs;

                result.Add(new OperationStatus(
                    op.OperationId,
                    op.OperationName,
                    DateTimeOffset.Now - elapsed,
                    op.TimeoutMs,
                    isHung,
                    elapsed,
                    op.ErrorMessage));
            }
        }

        return result.ToArray();
    }

    public OperationStatus[] GetHungOperations()
    {
        return GetActiveOperations().Where(op => op.IsHung).ToArray();
    }

    public bool IsHealthy()
    {
        return !GetHungOperations().Any();
    }

    private void CheckForHungOperations(object? state)
    {
        try
        {
            var hungOps = GetHungOperations();

            foreach (var op in hungOps)
            {
                _logger.LogWarning("⚠️ HUNG OPERATION DETECTED: '{Operation}' has been running for {Elapsed}ms (timeout: {Timeout}ms)",
                    op.OperationName, op.ElapsedTime.TotalMilliseconds, op.TimeoutMs);

                _logBuffer?.AddLog("Warning",
                    $"⚠️ HANG DETECTED: '{op.OperationName}' running {op.ElapsedTime.TotalMilliseconds:F0}ms (timeout: {op.TimeoutMs}ms)",
                    "Watchdog");
            }

            if (hungOps.Length > 0)
            {
                _logBuffer?.AddLog("Warning",
                    $"⚠️ TOTAL HUNG OPERATIONS: {hungOps.Length} - Check via REST API: GET /api/health/watchdog",
                    "Watchdog");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in watchdog monitoring");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _monitorTimer?.Dispose();
        _operations.Clear();
        _disposed = true;

        _logger.LogInformation("OperationWatchdog stopped");
    }
}
