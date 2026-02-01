using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// In-memory circular buffer for capturing application logs.
/// Thread-safe, provides REST API access for debugging.
/// </summary>
public interface ILogBufferService
{
    void AddLog(string level, string message, string? source = null);
    IReadOnlyList<BufferedLogEntry> GetRecentLogs(int count = 100);
    IReadOnlyList<BufferedLogEntry> GetLogsSince(DateTimeOffset since);
    IReadOnlyList<BufferedLogEntry> GetLogsByLevel(string level);
    void Clear();
}

public record BufferedLogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? Source);

public class LogBufferService : ILogBufferService
{
    private readonly ConcurrentQueue<BufferedLogEntry> _logBuffer = new();
    private const int MaxBufferSize = 1000;

    public void AddLog(string level, string message, string? source = null)
    {
        var entry = new BufferedLogEntry(
            DateTimeOffset.UtcNow,
            level,
            message,
            source);

        _logBuffer.Enqueue(entry);

        // Trim buffer if too large
        while (_logBuffer.Count > MaxBufferSize)
        {
            _logBuffer.TryDequeue(out _);
        }
    }

    public IReadOnlyList<BufferedLogEntry> GetRecentLogs(int count = 100)
    {
        return _logBuffer
            .TakeLast(Math.Min(count, _logBuffer.Count))
            .ToList();
    }

    public IReadOnlyList<BufferedLogEntry> GetLogsSince(DateTimeOffset since)
    {
        return _logBuffer
            .Where(e => e.Timestamp >= since)
            .ToList();
    }

    public IReadOnlyList<BufferedLogEntry> GetLogsByLevel(string level)
    {
        return _logBuffer
            .Where(e => e.Level.Equals(level, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void Clear()
    {
        _logBuffer.Clear();
    }
}
