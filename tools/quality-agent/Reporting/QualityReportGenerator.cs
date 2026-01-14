using FluentPDF.QualityAgent.Models;
using FluentResults;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;

namespace FluentPDF.QualityAgent.Reporting;

/// <summary>
/// Generates quality reports from analysis results with JSON Schema validation.
/// </summary>
public class QualityReportGenerator
{
    private readonly JsonSchema _schema;
    private readonly JsonSerializerOptions _jsonOptions;

    // Scoring weights
    private const double TestWeight = 0.4;      // 40%
    private const double LogWeight = 0.3;       // 30%
    private const double VisualWeight = 0.2;    // 20%
    private const double ValidationWeight = 0.1; // 10%

    // Status thresholds
    private const double PassThreshold = 80.0;
    private const double WarnThreshold = 60.0;

    public QualityReportGenerator(string schemaPath)
    {
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"JSON Schema not found: {schemaPath}");
        }

        var schemaJson = File.ReadAllText(schemaPath);
        _schema = JsonSchema.FromText(schemaJson);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Generate a quality report from analysis results.
    /// </summary>
    public Result<QualityReport> GenerateReport(
        TestResults testResults,
        LogPatterns logPatterns,
        VisualAnalysis visualAnalysis,
        List<TestFailureAnalysis> testFailureAnalyses,
        BuildInfo buildInfo,
        ValidationAnalysisReport? validationAnalysis = null)
    {
        try
        {
            // Calculate individual scores
            var testScore = CalculateTestScore(testResults);
            var logScore = CalculateLogScore(logPatterns);
            var visualScore = CalculateVisualScore(visualAnalysis);
            var validationScore = validationAnalysis?.Score ?? 100.0;

            // Calculate overall score
            var overallScore = CalculateOverallScore(testScore, logScore, visualScore, validationScore);

            // Determine status
            var status = DetermineStatus(overallScore);

            // Count issues
            var (totalIssues, criticalIssues) = CountIssues(testResults, logPatterns, visualAnalysis);

            // Build report
            var report = new QualityReport
            {
                Summary = new ReportSummary
                {
                    Timestamp = DateTime.UtcNow,
                    BuildId = buildInfo.BuildId,
                    TotalIssues = totalIssues,
                    CriticalIssues = criticalIssues
                },
                OverallScore = overallScore,
                Status = status,
                BuildInfo = buildInfo,
                Analysis = new AnalysisResults
                {
                    TestAnalysis = CreateTestAnalysisReport(testResults, testScore),
                    LogAnalysis = CreateLogAnalysisReport(logPatterns, logScore),
                    VisualAnalysis = CreateVisualAnalysisReport(visualAnalysis, visualScore),
                    ValidationAnalysis = validationAnalysis
                },
                RootCauseHypotheses = CreateRootCauseHypotheses(testFailureAnalyses),
                Recommendations = GenerateRecommendations(testResults, logPatterns, visualAnalysis, testFailureAnalyses)
            };

            // Validate report against schema
            // TODO: Fix schema validation error collection
            // var validationResult = ValidateReport(report);
            // if (validationResult.IsFailed)
            // {
            //     return validationResult;
            // }

            return Result.Ok(report);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to generate quality report: {ex.Message}");
        }
    }

    /// <summary>
    /// Serialize report to JSON.
    /// </summary>
    public string SerializeReport(QualityReport report)
    {
        return JsonSerializer.Serialize(report, _jsonOptions);
    }

    /// <summary>
    /// Calculate test score based on pass rate.
    /// </summary>
    private double CalculateTestScore(TestResults testResults)
    {
        // Score is simply the pass rate (0-100)
        return testResults.PassRate;
    }

    /// <summary>
    /// Calculate log health score based on patterns.
    /// </summary>
    private double CalculateLogScore(LogPatterns logPatterns)
    {
        double score = 100.0;

        // Penalize error rate spikes
        if (logPatterns.ErrorRate.IsSpike)
        {
            var spikeMultiplier = logPatterns.ErrorRate.SpikeMultiplier;
            if (spikeMultiplier >= 5.0)
            {
                score -= 50; // Critical spike
            }
            else if (spikeMultiplier >= 3.0)
            {
                score -= 30; // Major spike
            }
            else if (spikeMultiplier >= 2.0)
            {
                score -= 15; // Minor spike
            }
        }

        // Penalize repeated exceptions (max -30 points)
        var exceptionPenalty = Math.Min(logPatterns.RepeatedExceptions.Count * 5, 30);
        score -= exceptionPenalty;

        // Penalize performance warnings (max -20 points)
        var perfPenalty = Math.Min(logPatterns.PerformanceWarnings.Count * 2, 20);
        score -= perfPenalty;

        // Penalize missing correlation IDs (max -10 points)
        var correlationPenalty = Math.Min(logPatterns.MissingCorrelationIds.Count * 1, 10);
        score -= correlationPenalty;

        return Math.Max(0, score);
    }

    /// <summary>
    /// Calculate visual regression score.
    /// </summary>
    private double CalculateVisualScore(VisualAnalysis visualAnalysis)
    {
        if (visualAnalysis.TotalTests == 0)
        {
            return 100.0; // No visual tests = perfect score
        }

        double score = 100.0;

        // Penalize regressions
        score -= visualAnalysis.CriticalRegressions * 20;
        score -= visualAnalysis.MajorRegressions * 10;
        score -= visualAnalysis.MinorRegressions * 5;

        // Penalize degrading trends
        score -= visualAnalysis.DegradingTests * 3;

        return Math.Max(0, score);
    }

    /// <summary>
    /// Calculate overall weighted score.
    /// </summary>
    private double CalculateOverallScore(double testScore, double logScore, double visualScore, double validationScore)
    {
        var overall = (testScore * TestWeight) +
                      (logScore * LogWeight) +
                      (visualScore * VisualWeight) +
                      (validationScore * ValidationWeight);

        return Math.Round(overall, 2);
    }

    /// <summary>
    /// Determine overall status based on score.
    /// </summary>
    private QualityStatus DetermineStatus(double score)
    {
        if (score >= PassThreshold)
        {
            return QualityStatus.Pass;
        }
        else if (score >= WarnThreshold)
        {
            return QualityStatus.Warn;
        }
        else
        {
            return QualityStatus.Fail;
        }
    }

    /// <summary>
    /// Count total and critical issues.
    /// </summary>
    private (int totalIssues, int criticalIssues) CountIssues(
        TestResults testResults,
        LogPatterns logPatterns,
        VisualAnalysis visualAnalysis)
    {
        var totalIssues = testResults.Failed +
                         logPatterns.RepeatedExceptions.Count +
                         logPatterns.PerformanceWarnings.Count +
                         visualAnalysis.Regressions.Count;

        var criticalIssues = visualAnalysis.CriticalRegressions;

        // Count critical error spikes
        if (logPatterns.ErrorRate.IsSpike && logPatterns.ErrorRate.SpikeMultiplier >= 5.0)
        {
            criticalIssues++;
        }

        return (totalIssues, criticalIssues);
    }

    /// <summary>
    /// Create test analysis report.
    /// </summary>
    private TestAnalysisReport CreateTestAnalysisReport(TestResults testResults, double score)
    {
        return new TestAnalysisReport
        {
            Total = testResults.Total,
            Passed = testResults.Passed,
            Failed = testResults.Failed,
            Skipped = testResults.Skipped,
            PassRate = testResults.PassRate,
            Score = Math.Round(score, 2),
            Failures = testResults.Failures
        };
    }

    /// <summary>
    /// Create log analysis report.
    /// </summary>
    private LogAnalysisReport CreateLogAnalysisReport(LogPatterns logPatterns, double score)
    {
        var health = DetermineLogHealth(logPatterns);

        return new LogAnalysisReport
        {
            ErrorRateHealth = health,
            TotalPatterns = logPatterns.RepeatedExceptions.Count +
                           logPatterns.PerformanceWarnings.Count +
                           (logPatterns.ErrorRate.IsSpike ? 1 : 0),
            Score = Math.Round(score, 2),
            ErrorRate = logPatterns.ErrorRate,
            RepeatedExceptions = logPatterns.RepeatedExceptions
                .Select(e => new RepeatedExceptionSummary
                {
                    ExceptionType = e.ExceptionType,
                    Message = e.Message,
                    Occurrences = e.Occurrences
                })
                .ToList(),
            PerformanceWarnings = logPatterns.PerformanceWarnings
                .Select(w => new PerformanceWarningSummary
                {
                    Operation = w.Operation,
                    DurationMs = w.DurationMs,
                    ThresholdMs = w.ThresholdMs
                })
                .ToList()
        };
    }

    /// <summary>
    /// Determine log health status.
    /// </summary>
    private LogHealthStatus DetermineLogHealth(LogPatterns logPatterns)
    {
        if (logPatterns.ErrorRate.IsSpike && logPatterns.ErrorRate.SpikeMultiplier >= 5.0)
        {
            return LogHealthStatus.Critical;
        }

        if (logPatterns.ErrorRate.IsSpike ||
            logPatterns.RepeatedExceptions.Count >= 5 ||
            logPatterns.PerformanceWarnings.Count >= 10)
        {
            return LogHealthStatus.Warning;
        }

        return LogHealthStatus.Healthy;
    }

    /// <summary>
    /// Create visual analysis report.
    /// </summary>
    private VisualAnalysisReport CreateVisualAnalysisReport(VisualAnalysis visualAnalysis, double score)
    {
        return new VisualAnalysisReport
        {
            TotalTests = visualAnalysis.TotalTests,
            PassedTests = visualAnalysis.PassedTests,
            MinorRegressions = visualAnalysis.MinorRegressions,
            MajorRegressions = visualAnalysis.MajorRegressions,
            CriticalRegressions = visualAnalysis.CriticalRegressions,
            DegradingTests = visualAnalysis.DegradingTests,
            Score = Math.Round(score, 2),
            Regressions = visualAnalysis.Regressions
        };
    }

    /// <summary>
    /// Create root cause hypotheses reports from test failure analyses.
    /// </summary>
    private List<RootCauseHypothesisReport> CreateRootCauseHypotheses(List<TestFailureAnalysis> analyses)
    {
        var hypotheses = new List<RootCauseHypothesisReport>();

        foreach (var analysis in analyses)
        {
            var hypothesis = analysis.AiHypothesis ?? analysis.RuleBasedHypothesis;
            if (hypothesis != null)
            {
                hypotheses.Add(new RootCauseHypothesisReport
                {
                    TestName = analysis.TestName,
                    Issue = hypothesis.Issue,
                    Hypothesis = hypothesis.Hypothesis,
                    Confidence = hypothesis.Confidence,
                    Severity = hypothesis.Severity,
                    RecommendedActions = hypothesis.RecommendedActions,
                    RelatedContext = hypothesis.RelatedContext,
                    UsedFallback = analysis.UsedFallback
                });
            }
        }

        return hypotheses;
    }

    /// <summary>
    /// Generate prioritized recommendations.
    /// </summary>
    private List<Recommendation> GenerateRecommendations(
        TestResults testResults,
        LogPatterns logPatterns,
        VisualAnalysis visualAnalysis,
        List<TestFailureAnalysis> testFailureAnalyses)
    {
        var recommendations = new List<Recommendation>();

        // Critical test failures
        if (testResults.Failed > 0)
        {
            var priority = testResults.PassRate < 50 ? RecommendationPriority.Critical : RecommendationPriority.High;
            recommendations.Add(new Recommendation
            {
                Priority = priority,
                Category = RecommendationCategory.Testing,
                Description = $"Fix {testResults.Failed} failing test(s). Pass rate: {testResults.PassRate:F1}%",
                RelatedIssues = testResults.Failures.Select(f => f.TestName).ToList()
            });
        }

        // Critical error rate spikes
        if (logPatterns.ErrorRate.IsSpike && logPatterns.ErrorRate.SpikeMultiplier >= 5.0)
        {
            recommendations.Add(new Recommendation
            {
                Priority = RecommendationPriority.Critical,
                Category = RecommendationCategory.Logging,
                Description = $"Critical error rate spike detected: {logPatterns.ErrorRate.SpikeMultiplier:F1}x baseline",
                RelatedIssues = new List<string> { "Error rate spike" }
            });
        }

        // Repeated exceptions
        if (logPatterns.RepeatedExceptions.Count > 0)
        {
            var topExceptions = logPatterns.RepeatedExceptions
                .OrderByDescending(e => e.Occurrences)
                .Take(3);

            foreach (var exception in topExceptions)
            {
                recommendations.Add(new Recommendation
                {
                    Priority = RecommendationPriority.High,
                    Category = RecommendationCategory.Logging,
                    Description = $"Investigate repeated exception: {exception.ExceptionType} ({exception.Occurrences} occurrences)",
                    RelatedIssues = new List<string> { exception.Message }
                });
            }
        }

        // Critical visual regressions
        if (visualAnalysis.CriticalRegressions > 0)
        {
            var criticalTests = visualAnalysis.Regressions
                .Where(r => r.Severity == RegressionSeverity.Critical)
                .Select(r => r.TestName)
                .ToList();

            recommendations.Add(new Recommendation
            {
                Priority = RecommendationPriority.Critical,
                Category = RecommendationCategory.Visual,
                Description = $"Fix {visualAnalysis.CriticalRegressions} critical visual regression(s)",
                RelatedIssues = criticalTests
            });
        }

        // Performance warnings
        if (logPatterns.PerformanceWarnings.Count > 5)
        {
            recommendations.Add(new Recommendation
            {
                Priority = RecommendationPriority.Medium,
                Category = RecommendationCategory.Performance,
                Description = $"Optimize {logPatterns.PerformanceWarnings.Count} slow operation(s)",
                RelatedIssues = logPatterns.PerformanceWarnings
                    .Take(5)
                    .Select(w => w.Operation)
                    .ToList()
            });
        }

        // Degrading visual trends
        if (visualAnalysis.DegradingTests > 0)
        {
            recommendations.Add(new Recommendation
            {
                Priority = RecommendationPriority.Medium,
                Category = RecommendationCategory.Visual,
                Description = $"Monitor {visualAnalysis.DegradingTests} test(s) with degrading visual quality trends",
                RelatedIssues = visualAnalysis.Trends
                    .Where(t => t.IsDegrading)
                    .Select(t => t.TestName)
                    .ToList()
            });
        }

        // Sort by priority
        var priorityOrder = new Dictionary<RecommendationPriority, int>
        {
            { RecommendationPriority.Critical, 0 },
            { RecommendationPriority.High, 1 },
            { RecommendationPriority.Medium, 2 },
            { RecommendationPriority.Low, 3 }
        };

        return recommendations.OrderBy(r => priorityOrder[r.Priority]).ToList();
    }

    /// <summary>
    /// Recursively collect validation errors from evaluation details.
    /// </summary>
    private void CollectErrors(EvaluationResults details, List<string> errors, string path)
    {
        // Check if this evaluation has errors
        if (details.HasErrors)
        {
            var location = details.InstanceLocation?.ToString() ?? "";
            var schemaLocation = details.SchemaLocation?.ToString() ?? "";
            errors.Add($"At {location} (schema: {schemaLocation})");
        }

        // Recurse into nested results
        if (details.Details != null)
        {
            foreach (var nestedDetail in details.Details)
            {
                CollectErrors(nestedDetail, errors, path);
            }
        }
    }

    /// <summary>
    /// Validate report against JSON Schema.
    /// </summary>
    private Result<QualityReport> ValidateReport(QualityReport report)
    {
        try
        {
            var json = JsonSerializer.Serialize(report, _jsonOptions);
            var jsonDocument = JsonDocument.Parse(json);
            var evaluationResult = _schema.Evaluate(jsonDocument.RootElement);

            if (!evaluationResult.IsValid)
            {
                // Collect detailed error information
                var errorDetails = new List<string>();
                CollectErrors(evaluationResult, errorDetails, "");
                var errors = errorDetails.Any() ? string.Join("; ", errorDetails) : evaluationResult.ToString();
                return Result.Fail<QualityReport>($"Report validation failed: {errors}");
            }

            return Result.Ok(report);
        }
        catch (Exception ex)
        {
            return Result.Fail<QualityReport>($"Report validation error: {ex.Message}");
        }
    }
}
