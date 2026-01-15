using FluentPDF.Core.Observability;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Contract for reading, filtering, and exporting Serilog JSON log files.
/// </summary>
public interface ILogExportService
{
    /// <summary>
    /// Gets the most recent log entries from the log files.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to retrieve.</param>
    /// <returns>A result containing the recent log entries or an error.</returns>
    Task<Result<IReadOnlyList<LogEntry>>> GetRecentLogsAsync(int maxEntries);

    /// <summary>
    /// Filters log entries based on the specified criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria to apply.</param>
    /// <returns>A result containing the filtered log entries or an error.</returns>
    Task<Result<IReadOnlyList<LogEntry>>> FilterLogsAsync(LogFilterCriteria criteria);

    /// <summary>
    /// Exports log entries to a file in Serilog JSON format.
    /// </summary>
    /// <param name="entries">The log entries to export.</param>
    /// <param name="filePath">The file path to export to.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExportLogsAsync(IReadOnlyList<LogEntry> entries, string filePath);
}
