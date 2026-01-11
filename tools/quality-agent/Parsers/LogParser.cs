using System.Text.Json;
using FluentPDF.QualityAgent.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.QualityAgent.Parsers;

public class LogParser
{
    public Result<LogResults> Parse(string logFilePath)
    {
        try
        {
            if (!File.Exists(logFilePath))
            {
                return Result.Fail<LogResults>($"Log file not found: {logFilePath}");
            }

            Log.Information("Parsing log file: {LogFile}", logFilePath);

            var entries = new List<LogEntry>();
            var errorCount = 0;
            var warningCount = 0;
            var infoCount = 0;

            using (var reader = new StreamReader(logFilePath))
            {
                string? line;
                var lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        var entry = ParseLogEntry(line);
                        entries.Add(entry);

                        // Count by level
                        switch (entry.Level.ToUpperInvariant())
                        {
                            case "ERROR":
                            case "FATAL":
                                errorCount++;
                                break;
                            case "WARNING":
                            case "WARN":
                                warningCount++;
                                break;
                            case "INFORMATION":
                            case "INFO":
                                infoCount++;
                                break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Log.Warning(ex, "Failed to parse JSON on line {LineNumber}: {Line}", lineNumber, line);
                    }
                }
            }

            // Group by correlation ID
            var entriesByCorrelationId = entries
                .Where(e => !string.IsNullOrEmpty(e.CorrelationId))
                .GroupBy(e => e.CorrelationId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = new LogResults
            {
                Entries = entries,
                EntriesByCorrelationId = entriesByCorrelationId,
                ErrorCount = errorCount,
                WarningCount = warningCount,
                InfoCount = infoCount
            };

            Log.Information(
                "Log parsing completed: Total={Total}, Errors={Errors}, Warnings={Warnings}, Info={Info}, CorrelationGroups={CorrelationGroups}",
                entries.Count, errorCount, warningCount, infoCount, entriesByCorrelationId.Count);

            return Result.Ok(results);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse log file: {LogFile}", logFilePath);
            return Result.Fail<LogResults>($"Log parsing error: {ex.Message}");
        }
    }

    private LogEntry ParseLogEntry(string jsonLine)
    {
        using var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Extract timestamp
        var timestamp = root.TryGetProperty("Timestamp", out var tsValue) ||
                       root.TryGetProperty("@t", out tsValue)
            ? DateTime.Parse(tsValue.GetString()!)
            : DateTime.UtcNow;

        // Extract level
        var level = root.TryGetProperty("Level", out var levelValue) ||
                   root.TryGetProperty("@l", out levelValue)
            ? levelValue.GetString() ?? "Information"
            : "Information";

        // Extract message
        var message = root.TryGetProperty("MessageTemplate", out var msgValue) ||
                     root.TryGetProperty("@mt", out msgValue) ||
                     root.TryGetProperty("Message", out msgValue) ||
                     root.TryGetProperty("@m", out msgValue)
            ? msgValue.GetString() ?? string.Empty
            : string.Empty;

        // Extract correlation ID
        string? correlationId = null;
        if (root.TryGetProperty("CorrelationId", out var corrValue))
        {
            correlationId = corrValue.GetString();
        }
        else if (root.TryGetProperty("Properties", out var propsValue) &&
                 propsValue.TryGetProperty("CorrelationId", out var corrIdValue))
        {
            correlationId = corrIdValue.GetString();
        }

        // Extract properties
        Dictionary<string, object>? properties = null;
        if (root.TryGetProperty("Properties", out var propertiesValue))
        {
            properties = ParseProperties(propertiesValue);
        }

        // Extract exception
        ExceptionInfo? exception = null;
        if (root.TryGetProperty("Exception", out var exValue))
        {
            exception = ParseException(exValue);
        }

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            CorrelationId = correlationId,
            Properties = properties,
            Exception = exception
        };
    }

    private Dictionary<string, object> ParseProperties(JsonElement propertiesElement)
    {
        var properties = new Dictionary<string, object>();

        foreach (var prop in propertiesElement.EnumerateObject())
        {
            properties[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => string.Empty,
                _ => prop.Value.ToString()
            };
        }

        return properties;
    }

    private ExceptionInfo ParseException(JsonElement exceptionElement)
    {
        if (exceptionElement.ValueKind == JsonValueKind.String)
        {
            // Exception is a string
            var exStr = exceptionElement.GetString() ?? string.Empty;
            var lines = exStr.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var firstLine = lines.FirstOrDefault() ?? string.Empty;

            // Try to extract type and message from first line
            var colonIndex = firstLine.IndexOf(':');
            if (colonIndex > 0)
            {
                return new ExceptionInfo
                {
                    Type = firstLine.Substring(0, colonIndex).Trim(),
                    Message = firstLine.Substring(colonIndex + 1).Trim(),
                    StackTrace = exStr
                };
            }

            return new ExceptionInfo
            {
                Type = "Exception",
                Message = firstLine,
                StackTrace = exStr
            };
        }
        else if (exceptionElement.ValueKind == JsonValueKind.Object)
        {
            // Exception is an object
            var type = exceptionElement.TryGetProperty("Type", out var typeValue) ||
                      exceptionElement.TryGetProperty("ClassName", out typeValue)
                ? typeValue.GetString() ?? "Exception"
                : "Exception";

            var message = exceptionElement.TryGetProperty("Message", out var msgValue)
                ? msgValue.GetString() ?? string.Empty
                : string.Empty;

            var stackTrace = exceptionElement.TryGetProperty("StackTrace", out var stValue)
                ? stValue.GetString()
                : null;

            return new ExceptionInfo
            {
                Type = type,
                Message = message,
                StackTrace = stackTrace
            };
        }

        return new ExceptionInfo
        {
            Type = "Exception",
            Message = exceptionElement.ToString(),
            StackTrace = null
        };
    }
}
