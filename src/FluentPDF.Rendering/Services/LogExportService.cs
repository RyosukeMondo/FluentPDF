using System.Text.Json;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Implements log reading, filtering, and export from Serilog JSON log files.
/// </summary>
public sealed class LogExportService : ILogExportService
{
    private readonly ILogger<LogExportService> _logger;
    private readonly LruCache<string, LogEntry> _logCache;
    private readonly string _logDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogExportService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="logDirectory">The directory where log files are stored. If null, uses a default location.</param>
    public LogExportService(ILogger<LogExportService> logger, string? logDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logCache = new LruCache<string, LogEntry>(10_000);

        // Use provided directory or default to user's temp/app data
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentPDF",
            "logs");

        _logger.LogInformation("LogExportService initialized with log directory: {LogDirectory}", _logDirectory);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LogEntry>>> GetRecentLogsAsync(int maxEntries)
    {
        try
        {
            _logger.LogDebug("Getting recent logs, max entries: {MaxEntries}", maxEntries);

            if (!Directory.Exists(_logDirectory))
            {
                _logger.LogWarning("Log directory does not exist: {LogDirectory}", _logDirectory);
                return Result.Ok<IReadOnlyList<LogEntry>>(Array.Empty<LogEntry>());
            }

            var logFiles = Directory.GetFiles(_logDirectory, "*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();

            if (logFiles.Count == 0)
            {
                _logger.LogInformation("No log files found in directory: {LogDirectory}", _logDirectory);
                return Result.Ok<IReadOnlyList<LogEntry>>(Array.Empty<LogEntry>());
            }

            var allLogs = new List<LogEntry>();

            foreach (var logFile in logFiles)
            {
                var entries = await ReadLogFileAsync(logFile);
                allLogs.AddRange(entries);

                if (allLogs.Count >= maxEntries)
                {
                    break;
                }
            }

            var recentLogs = allLogs
                .OrderByDescending(e => e.Timestamp)
                .Take(maxEntries)
                .ToList();

            _logger.LogInformation("Retrieved {Count} recent log entries from {FileCount} files",
                recentLogs.Count, logFiles.Count);

            return Result.Ok<IReadOnlyList<LogEntry>>(recentLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent logs");
            return Result.Fail<IReadOnlyList<LogEntry>>($"Failed to read logs: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LogEntry>>> FilterLogsAsync(LogFilterCriteria criteria)
    {
        try
        {
            _logger.LogDebug("Filtering logs with criteria - MinLevel: {MinLevel}, CorrelationId: {CorrelationId}, Component: {Component}",
                criteria.MinimumLevel, criteria.CorrelationId, criteria.ComponentFilter);

            // Get all logs first (using cache)
            var allLogsResult = await GetRecentLogsAsync(10_000);
            if (allLogsResult.IsFailed)
            {
                return allLogsResult;
            }

            var filteredLogs = allLogsResult.Value
                .Where(criteria.Matches)
                .ToList();

            _logger.LogInformation("Filtered logs: {FilteredCount} out of {TotalCount}",
                filteredLogs.Count, allLogsResult.Value.Count);

            return Result.Ok<IReadOnlyList<LogEntry>>(filteredLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to filter logs");
            return Result.Fail<IReadOnlyList<LogEntry>>($"Failed to filter logs: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ExportLogsAsync(IReadOnlyList<LogEntry> entries, string filePath)
    {
        try
        {
            _logger.LogDebug("Exporting {Count} log entries to {FilePath}", entries.Count, filePath);

            // Convert to Serilog JSON format
            var serilogEntries = entries.Select(ConvertToSerilogFormat).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await using var fileStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fileStream, serilogEntries, options);

            _logger.LogInformation("Successfully exported {Count} log entries to {FilePath}",
                entries.Count, filePath);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export logs to {FilePath}", filePath);
            return Result.Fail($"Failed to export logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads a log file and parses entries, using cache and streaming to avoid memory issues.
    /// </summary>
    private async Task<List<LogEntry>> ReadLogFileAsync(string filePath)
    {
        var entries = new List<LogEntry>();

        try
        {
            await using var fileStream = File.OpenRead(filePath);
            using var reader = new StreamReader(fileStream);

            string? line;
            int lineNumber = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Check cache first (use file path + line number as key)
                var cacheKey = $"{filePath}:{lineNumber}";
                if (_logCache.TryGet(cacheKey, out var cachedEntry))
                {
                    entries.Add(cachedEntry);
                    continue;
                }

                // Parse JSON line
                try
                {
                    var entry = ParseSerilogJsonLine(line);
                    if (entry != null)
                    {
                        _logCache.Add(cacheKey, entry);
                        entries.Add(entry);
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx,
                        "Corrupted JSON in log file {FilePath} at line {LineNumber}, skipping",
                        filePath, lineNumber);
                    // Skip invalid entry, continue processing
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading log file {FilePath}", filePath);
        }

        return entries;
    }

    /// <summary>
    /// Parses a Serilog JSON log line into a LogEntry.
    /// </summary>
    private LogEntry? ParseSerilogJsonLine(string jsonLine)
    {
        using var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Serilog JSON format has @t (timestamp), @l (level), @mt (message template)
        if (!root.TryGetProperty("@t", out var timestampElement) ||
            !root.TryGetProperty("@l", out var levelElement) ||
            !root.TryGetProperty("@mt", out var messageElement))
        {
            return null;
        }

        var timestamp = timestampElement.GetDateTime();
        var levelString = levelElement.GetString() ?? "Information";
        var message = messageElement.GetString() ?? string.Empty;

        // Parse log level
        if (!Enum.TryParse<Core.Observability.LogLevel>(levelString, true, out var level))
        {
            level = Core.Observability.LogLevel.Information;
        }

        // Extract correlation ID (custom property)
        string? correlationId = null;
        if (root.TryGetProperty("CorrelationId", out var corrIdElement))
        {
            correlationId = corrIdElement.GetString();
        }

        // Extract component (SourceContext in Serilog)
        var component = "Unknown";
        if (root.TryGetProperty("SourceContext", out var sourceContextElement))
        {
            component = sourceContextElement.GetString() ?? "Unknown";
        }

        // Extract exception
        string? exception = null;
        string? stackTrace = null;
        if (root.TryGetProperty("@x", out var exceptionElement))
        {
            exception = exceptionElement.GetString();
        }

        // Extract additional context
        var context = new Dictionary<string, object>();
        foreach (var property in root.EnumerateObject())
        {
            // Skip Serilog built-in properties
            if (property.Name.StartsWith('@') ||
                property.Name == "CorrelationId" ||
                property.Name == "SourceContext")
            {
                continue;
            }

            context[property.Name] = property.Value.ToString();
        }

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            CorrelationId = correlationId,
            Component = component,
            Context = context,
            Exception = exception,
            StackTrace = stackTrace
        };
    }

    /// <summary>
    /// Converts a LogEntry back to Serilog JSON format for export.
    /// </summary>
    private object ConvertToSerilogFormat(LogEntry entry)
    {
        var serilogEntry = new Dictionary<string, object?>
        {
            ["@t"] = entry.Timestamp,
            ["@l"] = entry.Level.ToString(),
            ["@mt"] = entry.Message,
            ["SourceContext"] = entry.Component
        };

        if (!string.IsNullOrEmpty(entry.CorrelationId))
        {
            serilogEntry["CorrelationId"] = entry.CorrelationId;
        }

        if (!string.IsNullOrEmpty(entry.Exception))
        {
            serilogEntry["@x"] = entry.Exception;
        }

        // Add context properties
        foreach (var kvp in entry.Context)
        {
            serilogEntry[kvp.Key] = kvp.Value;
        }

        return serilogEntry;
    }
}

/// <summary>
/// Simple LRU cache implementation using Dictionary and LinkedList.
/// </summary>
internal sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _maxCapacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;

    public LruCache(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(maxCapacity);
        _lruList = new LinkedList<CacheItem>();
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var existingNode))
        {
            // Update existing entry
            _lruList.Remove(existingNode);
            existingNode.Value = new CacheItem(key, value);
            _lruList.AddFirst(existingNode);
            return;
        }

        // Check capacity
        if (_cache.Count >= _maxCapacity)
        {
            // Remove least recently used (last item)
            var lastNode = _lruList.Last;
            if (lastNode != null)
            {
                _lruList.RemoveLast();
                _cache.Remove(lastNode.Value.Key);
            }
        }

        // Add new entry
        var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
        _lruList.AddFirst(newNode);
        _cache[key] = newNode;
    }

    private readonly struct CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
