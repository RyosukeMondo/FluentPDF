using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentPDF.E2E.Tests.Fixtures;

/// <summary>
/// Verifies application logs for errors during E2E tests.
/// Parses Serilog JSON logs from %LocalAppData% or temp folder.
/// </summary>
public class LogVerifier
{
    private readonly string _logDirectory;

    /// <summary>
    /// Initializes a new instance of LogVerifier.
    /// </summary>
    public LogVerifier()
    {
        _logDirectory = GetLogDirectory();
    }

    /// <summary>
    /// Gets all log entries from the current log file.
    /// </summary>
    /// <returns>List of log entries.</returns>
    public List<LogEntry> GetLogEntries()
    {
        var entries = new List<LogEntry>();
        var logFiles = GetCurrentLogFiles();

        foreach (var logFile in logFiles)
        {
            if (!File.Exists(logFile))
                continue;

            try
            {
                var lines = File.ReadAllLines(logFile);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var entry = JsonSerializer.Deserialize<LogEntry>(line);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                    catch
                    {
                        // Skip malformed log lines
                    }
                }
            }
            catch
            {
                // Log file might be locked or inaccessible, skip it
            }
        }

        return entries;
    }

    /// <summary>
    /// Asserts that there are no ERROR level log entries.
    /// Throws an exception with details if errors are found.
    /// </summary>
    public void AssertNoErrors()
    {
        var entries = GetLogEntries();
        var errors = entries.Where(e => e.Level == "Error" || e.Level == "Fatal").ToList();

        if (errors.Any())
        {
            var errorMessages = string.Join("\n\n", errors.Select(e =>
                $"[{e.Timestamp}] {e.Level}: {e.MessageTemplate}\n" +
                $"  Properties: {JsonSerializer.Serialize(e.Properties)}\n" +
                (e.Exception != null ? $"  Exception: {e.Exception}\n" : "")));

            throw new Exception(
                $"Found {errors.Count} error(s) in application logs:\n\n{errorMessages}");
        }
    }

    /// <summary>
    /// Asserts that there are no ERROR level log entries since a specific time.
    /// Useful for checking errors only during a specific test.
    /// </summary>
    /// <param name="since">Only check errors after this timestamp.</param>
    public void AssertNoErrorsSince(DateTime since)
    {
        var entries = GetLogEntries();
        var errors = entries
            .Where(e => (e.Level == "Error" || e.Level == "Fatal") && e.Timestamp >= since)
            .ToList();

        if (errors.Any())
        {
            var errorMessages = string.Join("\n\n", errors.Select(e =>
                $"[{e.Timestamp}] {e.Level}: {e.MessageTemplate}\n" +
                $"  Properties: {JsonSerializer.Serialize(e.Properties)}\n" +
                (e.Exception != null ? $"  Exception: {e.Exception}\n" : "")));

            throw new Exception(
                $"Found {errors.Count} error(s) in application logs since {since:O}:\n\n{errorMessages}");
        }
    }

    /// <summary>
    /// Gets the count of log entries by level.
    /// </summary>
    /// <returns>Dictionary mapping log level to count.</returns>
    public Dictionary<string, int> GetLogLevelCounts()
    {
        var entries = GetLogEntries();
        return entries
            .GroupBy(e => e.Level ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Clears all log files in the log directory.
    /// Useful for starting tests with a clean slate.
    /// </summary>
    public void ClearLogs()
    {
        if (!Directory.Exists(_logDirectory))
            return;

        var logFiles = Directory.GetFiles(_logDirectory, "log-*.json");
        foreach (var file in logFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // File might be locked, ignore
            }
        }
    }

    /// <summary>
    /// Gets the log directory path.
    /// Checks both ApplicationData locations and temp path.
    /// </summary>
    private string GetLogDirectory()
    {
        // Check LocalApplicationData first (most common for non-MSIX)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localAppDataLogs = Path.Combine(localAppData, "FluentPDF", "logs");
        if (Directory.Exists(localAppDataLogs))
        {
            return localAppDataLogs;
        }

        // Check temp path (fallback location)
        var tempLogs = Path.Combine(Path.GetTempPath(), "FluentPDF", "logs");
        if (Directory.Exists(tempLogs))
        {
            return tempLogs;
        }

        // Default to temp path if neither exists yet
        return tempLogs;
    }

    /// <summary>
    /// Gets current log files (today's log and possibly yesterday's if near midnight).
    /// </summary>
    private List<string> GetCurrentLogFiles()
    {
        if (!Directory.Exists(_logDirectory))
            return new List<string>();

        var today = DateTime.Now.ToString("yyyyMMdd");
        var yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");

        var logFiles = new List<string>
        {
            Path.Combine(_logDirectory, $"log-{today}.json"),
            Path.Combine(_logDirectory, $"log-{yesterday}.json")
        };

        return logFiles.Where(File.Exists).ToList();
    }
}

/// <summary>
/// Represents a Serilog JSON log entry.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp of the log entry.
    /// </summary>
    [JsonPropertyName("@t")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log level (Debug, Information, Warning, Error, Fatal).
    /// </summary>
    [JsonPropertyName("@l")]
    public string? Level { get; set; }

    /// <summary>
    /// Gets or sets the message template.
    /// </summary>
    [JsonPropertyName("@mt")]
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Gets or sets the rendered message.
    /// </summary>
    [JsonPropertyName("@m")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the exception string if present.
    /// </summary>
    [JsonPropertyName("@x")]
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets additional properties from the log context.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Properties { get; set; }
}
