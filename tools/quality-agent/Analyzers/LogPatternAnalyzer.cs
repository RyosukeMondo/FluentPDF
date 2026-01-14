using System.Security.Cryptography;
using System.Text;
using FluentPDF.QualityAgent.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.QualityAgent.Analyzers;

public class LogPatternAnalyzer
{
    private readonly double _errorRateThresholdMultiplier;
    private readonly int _repeatedExceptionThreshold;
    private readonly double _performanceThresholdMs;

    public LogPatternAnalyzer(
        double errorRateThresholdMultiplier = 2.0,
        int repeatedExceptionThreshold = 5,
        double performanceThresholdMs = 1000.0)
    {
        _errorRateThresholdMultiplier = errorRateThresholdMultiplier;
        _repeatedExceptionThreshold = repeatedExceptionThreshold;
        _performanceThresholdMs = performanceThresholdMs;
    }

    public Result<LogPatterns> Analyze(LogResults logResults, double? baselineErrorsPerHour = null)
    {
        try
        {
            Log.Information("Starting log pattern analysis on {EntryCount} log entries", logResults.Entries.Count);

            var errorRate = AnalyzeErrorRate(logResults, baselineErrorsPerHour);
            var repeatedExceptions = AnalyzeRepeatedExceptions(logResults);
            var performanceWarnings = AnalyzePerformanceWarnings(logResults);
            var missingCorrelationIds = AnalyzeMissingCorrelationIds(logResults);

            var patterns = new LogPatterns
            {
                ErrorRate = errorRate,
                RepeatedExceptions = repeatedExceptions,
                PerformanceWarnings = performanceWarnings,
                MissingCorrelationIds = missingCorrelationIds
            };

            Log.Information(
                "Log pattern analysis completed: ErrorSpike={IsSpike}, RepeatedExceptions={RepeatedCount}, PerformanceWarnings={PerfWarnings}, MissingCorrelationIds={MissingCorr}",
                errorRate.IsSpike, repeatedExceptions.Count, performanceWarnings.Count, missingCorrelationIds.Count);

            return Result.Ok(patterns);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze log patterns");
            return Result.Fail<LogPatterns>($"Log pattern analysis error: {ex.Message}");
        }
    }

    private ErrorRateAnalysis AnalyzeErrorRate(LogResults logResults, double? baselineErrorsPerHour)
    {
        if (logResults.Entries.Count == 0)
        {
            return new ErrorRateAnalysis
            {
                ErrorsPerHour = 0,
                BaselineErrorsPerHour = baselineErrorsPerHour ?? 0,
                IsSpike = false,
                SpikeMultiplier = 0
            };
        }

        // Calculate time span
        var minTimestamp = logResults.Entries.Min(e => e.Timestamp);
        var maxTimestamp = logResults.Entries.Max(e => e.Timestamp);
        var timeSpanHours = (maxTimestamp - minTimestamp).TotalHours;

        // Avoid division by zero
        if (timeSpanHours < 0.001) // Less than ~3.6 seconds
        {
            timeSpanHours = 1.0 / 60.0; // Treat as 1 minute
        }

        // Calculate error rate
        var errorsPerHour = logResults.ErrorCount / timeSpanHours;

        // Calculate or use provided baseline
        var baseline = baselineErrorsPerHour ?? CalculateMovingAverageBaseline(errorsPerHour);

        // Detect spike
        var isSpike = baseline > 0 && errorsPerHour > baseline * _errorRateThresholdMultiplier;
        var spikeMultiplier = baseline > 0 ? errorsPerHour / baseline : 0;

        return new ErrorRateAnalysis
        {
            ErrorsPerHour = errorsPerHour,
            BaselineErrorsPerHour = baseline,
            IsSpike = isSpike,
            SpikeMultiplier = spikeMultiplier
        };
    }

    private double CalculateMovingAverageBaseline(double currentErrorRate)
    {
        // Simple baseline calculation: use 50% of current rate as baseline
        // In a real implementation, this would read historical data
        return currentErrorRate * 0.5;
    }

    private List<RepeatedExceptionPattern> AnalyzeRepeatedExceptions(LogResults logResults)
    {
        var exceptionGroups = logResults.Entries
            .Where(e => e.Exception != null)
            .GroupBy(e => ComputeStackTraceHash(e.Exception!.StackTrace ?? e.Exception.Message))
            .Select(g =>
            {
                var firstException = g.First().Exception!;
                return new RepeatedExceptionPattern
                {
                    ExceptionType = firstException.Type,
                    Message = firstException.Message,
                    StackTraceHash = g.Key,
                    Occurrences = g.Count(),
                    Timestamps = g.Select(e => e.Timestamp).OrderBy(t => t).ToList()
                };
            })
            .Where(p => p.Occurrences > _repeatedExceptionThreshold)
            .OrderByDescending(p => p.Occurrences)
            .ToList();

        return exceptionGroups;
    }

    private string ComputeStackTraceHash(string stackTrace)
    {
        // Compute SHA256 hash of stack trace for grouping
        var bytes = Encoding.UTF8.GetBytes(stackTrace);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private List<PerformanceWarning> AnalyzePerformanceWarnings(LogResults logResults)
    {
        var warnings = new List<PerformanceWarning>();

        foreach (var entry in logResults.Entries)
        {
            // Check if entry has duration property
            if (entry.Properties != null &&
                (entry.Properties.TryGetValue("Duration", out var durationObj) ||
                 entry.Properties.TryGetValue("DurationMs", out durationObj) ||
                 entry.Properties.TryGetValue("ElapsedMs", out durationObj)))
            {
                var durationMs = Convert.ToDouble(durationObj);
                if (durationMs > _performanceThresholdMs)
                {
                    // Try to extract operation name from properties or message
                    var operation = entry.Properties.TryGetValue("Operation", out var opObj)
                        ? opObj.ToString() ?? entry.Message
                        : entry.Message;

                    warnings.Add(new PerformanceWarning
                    {
                        Timestamp = entry.Timestamp,
                        Operation = operation,
                        DurationMs = durationMs,
                        ThresholdMs = _performanceThresholdMs
                    });
                }
            }
        }

        return warnings.OrderByDescending(w => w.DurationMs).ToList();
    }

    private List<string> AnalyzeMissingCorrelationIds(LogResults logResults)
    {
        var missingCorrelationIds = logResults.Entries
            .Where(e => string.IsNullOrEmpty(e.CorrelationId))
            .Select(e => $"{e.Timestamp:yyyy-MM-dd HH:mm:ss} [{e.Level}] {e.Message.Substring(0, Math.Min(100, e.Message.Length))}")
            .Take(100) // Limit to first 100 entries to avoid overwhelming output
            .ToList();

        return missingCorrelationIds;
    }
}
