using System.Text.Json;
using FluentPDF.QualityAgent.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.QualityAgent.Analyzers;

public class VisualRegressionAnalyzer
{
    private readonly double _minorThreshold;
    private readonly double _majorThreshold;
    private readonly double _criticalThreshold;
    private readonly int _maxHistoryEntries;
    private readonly int _degradationThreshold;
    private readonly string _historyFilePath;

    public VisualRegressionAnalyzer(
        double minorThreshold = 0.99,
        double majorThreshold = 0.97,
        double criticalThreshold = 0.95,
        int maxHistoryEntries = 10,
        int degradationThreshold = 3,
        string? historyFilePath = null)
    {
        _minorThreshold = minorThreshold;
        _majorThreshold = majorThreshold;
        _criticalThreshold = criticalThreshold;
        _maxHistoryEntries = maxHistoryEntries;
        _degradationThreshold = degradationThreshold;
        _historyFilePath = historyFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentPDF.QualityAgent",
            "ssim-history.json");
    }

    public Result<VisualAnalysis> Analyze(SsimResults ssimResults, string? buildId = null)
    {
        try
        {
            Log.Information("Starting visual regression analysis on {TestCount} tests", ssimResults.Total);

            var regressions = AnalyzeRegressions(ssimResults);
            var history = LoadHistory();
            var updatedHistory = UpdateHistory(history, ssimResults, buildId);
            var trends = AnalyzeTrends(updatedHistory);

            SaveHistory(updatedHistory);

            var analysis = new VisualAnalysis
            {
                Regressions = regressions,
                Trends = trends,
                TotalTests = ssimResults.Total,
                PassedTests = ssimResults.Passed,
                MinorRegressions = ssimResults.MinorRegressions,
                MajorRegressions = ssimResults.MajorRegressions,
                CriticalRegressions = ssimResults.CriticalRegressions,
                DegradingTests = trends.Count(t => t.IsDegrading)
            };

            Log.Information(
                "Visual regression analysis completed: Total={Total}, Passed={Passed}, Minor={Minor}, Major={Major}, Critical={Critical}, Degrading={Degrading}",
                analysis.TotalTests, analysis.PassedTests, analysis.MinorRegressions,
                analysis.MajorRegressions, analysis.CriticalRegressions, analysis.DegradingTests);

            return Result.Ok(analysis);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze visual regressions");
            return Result.Fail<VisualAnalysis>($"Visual regression analysis error: {ex.Message}");
        }
    }

    private List<VisualRegression> AnalyzeRegressions(SsimResults ssimResults)
    {
        var regressions = new List<VisualRegression>();

        foreach (var test in ssimResults.Tests.Where(t => t.Regression != RegressionSeverity.None))
        {
            regressions.Add(new VisualRegression
            {
                TestName = test.TestName,
                SsimScore = test.SsimScore,
                Severity = test.Regression,
                BaselineImagePath = test.BaselineImagePath,
                CurrentImagePath = test.CurrentImagePath
            });
        }

        return regressions.OrderBy(r => r.SsimScore).ToList();
    }

    private Dictionary<string, List<SsimHistoryEntry>> LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                Log.Information("No SSIM history file found, starting fresh");
                return new Dictionary<string, List<SsimHistoryEntry>>();
            }

            var json = File.ReadAllText(_historyFilePath);
            var history = JsonSerializer.Deserialize<Dictionary<string, List<SsimHistoryEntry>>>(json);

            Log.Information("Loaded SSIM history with {TestCount} tests", history?.Count ?? 0);
            return history ?? new Dictionary<string, List<SsimHistoryEntry>>();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load SSIM history, starting fresh");
            return new Dictionary<string, List<SsimHistoryEntry>>();
        }
    }

    private Dictionary<string, List<SsimHistoryEntry>> UpdateHistory(
        Dictionary<string, List<SsimHistoryEntry>> history,
        SsimResults ssimResults,
        string? buildId)
    {
        var timestamp = DateTime.UtcNow;

        foreach (var test in ssimResults.Tests)
        {
            if (!history.ContainsKey(test.TestName))
            {
                history[test.TestName] = new List<SsimHistoryEntry>();
            }

            var entry = new SsimHistoryEntry
            {
                Timestamp = timestamp,
                SsimScore = test.SsimScore,
                BuildId = buildId
            };

            history[test.TestName].Add(entry);

            // Keep only the last N entries
            if (history[test.TestName].Count > _maxHistoryEntries)
            {
                history[test.TestName] = history[test.TestName]
                    .OrderByDescending(e => e.Timestamp)
                    .Take(_maxHistoryEntries)
                    .OrderBy(e => e.Timestamp)
                    .ToList();
            }
        }

        return history;
    }

    private void SaveHistory(Dictionary<string, List<SsimHistoryEntry>> history)
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_historyFilePath, json);
            Log.Information("Saved SSIM history to {HistoryPath}", _historyFilePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save SSIM history");
        }
    }

    private List<VisualTrend> AnalyzeTrends(Dictionary<string, List<SsimHistoryEntry>> history)
    {
        var trends = new List<VisualTrend>();

        foreach (var (testName, entries) in history)
        {
            if (entries.Count < 2)
            {
                // Not enough data for trend analysis
                continue;
            }

            var sortedEntries = entries.OrderBy(e => e.Timestamp).ToList();
            var consecutiveDecreases = CountConsecutiveDecreases(sortedEntries);
            var isDegrading = consecutiveDecreases >= _degradationThreshold;
            var averageScore = sortedEntries.Average(e => e.SsimScore);

            trends.Add(new VisualTrend
            {
                TestName = testName,
                History = sortedEntries,
                IsDegrading = isDegrading,
                ConsecutiveDecreases = consecutiveDecreases,
                AverageScore = averageScore
            });
        }

        return trends.Where(t => t.IsDegrading).OrderByDescending(t => t.ConsecutiveDecreases).ToList();
    }

    private int CountConsecutiveDecreases(List<SsimHistoryEntry> sortedEntries)
    {
        if (sortedEntries.Count < 2)
        {
            return 0;
        }

        int maxConsecutive = 0;
        int currentConsecutive = 0;

        for (int i = 1; i < sortedEntries.Count; i++)
        {
            if (sortedEntries[i].SsimScore < sortedEntries[i - 1].SsimScore)
            {
                currentConsecutive++;
                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return maxConsecutive;
    }
}
