using FluentPDF.QualityAgent.Analyzers;
using FluentPDF.QualityAgent.Config;
using FluentPDF.QualityAgent.Models;
using FluentPDF.QualityAgent.Parsers;
using FluentPDF.QualityAgent.Reporting;
using FluentResults;
using Serilog;

namespace FluentPDF.QualityAgent.Services;

/// <summary>
/// Orchestrates quality analysis by coordinating parsers, analyzers, and report generation.
/// </summary>
public class AiQualityAgent : IAiQualityAgent
{
    private readonly TrxParser _trxParser;
    private readonly LogParser _logParser;
    private readonly SsimParser _ssimParser;
    private readonly LogPatternAnalyzer _logPatternAnalyzer;
    private readonly TestFailureAnalyzer _testFailureAnalyzer;
    private readonly VisualRegressionAnalyzer _visualRegressionAnalyzer;

    public AiQualityAgent(OpenAiConfig openAiConfig)
    {
        _trxParser = new TrxParser();
        _logParser = new LogParser();
        _ssimParser = new SsimParser();
        _logPatternAnalyzer = new LogPatternAnalyzer();
        _testFailureAnalyzer = new TestFailureAnalyzer(openAiConfig);
        _visualRegressionAnalyzer = new VisualRegressionAnalyzer();
    }

    /// <summary>
    /// Perform comprehensive quality analysis.
    /// </summary>
    public async Task<Result<QualityReport>> AnalyzeAsync(
        AnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        Log.Information("[{CorrelationId}] Starting quality analysis for build {BuildId}",
            correlationId, input.BuildInfo.BuildId);

        try
        {
            // Validate inputs
            var validationResult = ValidateInput(input);
            if (validationResult.IsFailed)
            {
                return Result.Fail<QualityReport>(validationResult.Errors.First().Message);
            }

            // Step 1: Run all parsers in parallel
            Log.Information("[{CorrelationId}] Running parsers in parallel", correlationId);
            var (testResults, logResults, ssimResults) = await RunParsersAsync(input, correlationId);

            // Step 2: Run analyzers
            Log.Information("[{CorrelationId}] Running analyzers", correlationId);
            var (logPatterns, visualAnalysis, testFailureAnalyses) = await RunAnalyzersAsync(
                testResults, logResults, ssimResults, input, correlationId, cancellationToken);

            // Step 3: Generate quality report
            Log.Information("[{CorrelationId}] Generating quality report", correlationId);
            var reportGenerator = new QualityReportGenerator(input.SchemaPath);
            var reportResult = reportGenerator.GenerateReport(
                testResults,
                logPatterns,
                visualAnalysis,
                testFailureAnalyses,
                input.BuildInfo,
                validationAnalysis: null // Validation analysis not implemented yet
            );

            if (reportResult.IsFailed)
            {
                Log.Error("[{CorrelationId}] Failed to generate quality report: {Error}",
                    correlationId, reportResult.Errors.First().Message);
                return Result.Fail<QualityReport>(reportResult.Errors.First().Message);
            }

            var report = reportResult.Value;

            // Step 4: Write report to output file if specified
            if (!string.IsNullOrEmpty(input.OutputPath))
            {
                Log.Information("[{CorrelationId}] Writing report to {OutputPath}", correlationId, input.OutputPath);
                var json = reportGenerator.SerializeReport(report);
                await File.WriteAllTextAsync(input.OutputPath, json, cancellationToken);
            }

            Log.Information(
                "[{CorrelationId}] Quality analysis completed: Status={Status}, Score={Score}, TotalIssues={TotalIssues}, CriticalIssues={CriticalIssues}",
                correlationId, report.Status, report.OverallScore, report.Summary.TotalIssues, report.Summary.CriticalIssues);

            return Result.Ok(report);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Quality analysis failed", correlationId);
            return Result.Fail<QualityReport>($"Quality analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate analysis input.
    /// </summary>
    private Result ValidateInput(AnalysisInput input)
    {
        var errors = new List<string>();

        if (!string.IsNullOrEmpty(input.TrxFilePath) && !File.Exists(input.TrxFilePath))
        {
            errors.Add($"TRX file not found: {input.TrxFilePath}");
        }

        if (!string.IsNullOrEmpty(input.LogFilePath) && !File.Exists(input.LogFilePath))
        {
            errors.Add($"Log file not found: {input.LogFilePath}");
        }

        if (!string.IsNullOrEmpty(input.SsimResultsPath) && !File.Exists(input.SsimResultsPath))
        {
            errors.Add($"SSIM results file not found: {input.SsimResultsPath}");
        }

        if (!File.Exists(input.SchemaPath))
        {
            errors.Add($"JSON schema file not found: {input.SchemaPath}");
        }

        // At least one input file must be provided
        if (string.IsNullOrEmpty(input.TrxFilePath) &&
            string.IsNullOrEmpty(input.LogFilePath) &&
            string.IsNullOrEmpty(input.SsimResultsPath))
        {
            errors.Add("At least one input file (TRX, log, or SSIM) must be provided");
        }

        if (errors.Any())
        {
            return Result.Fail(string.Join("; ", errors));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Run all parsers in parallel.
    /// </summary>
    private async Task<(TestResults, LogResults, SsimResults)> RunParsersAsync(
        AnalysisInput input,
        string correlationId)
    {
        var tasks = new List<Task<object>>();

        // Parse TRX file
        Task<object>? trxTask = null;
        if (!string.IsNullOrEmpty(input.TrxFilePath))
        {
            trxTask = Task.Run<object>(() =>
            {
                Log.Information("[{CorrelationId}] Parsing TRX file: {FilePath}", correlationId, input.TrxFilePath);
                var result = _trxParser.Parse(input.TrxFilePath);
                if (result.IsFailed)
                {
                    Log.Warning("[{CorrelationId}] TRX parsing failed: {Error}", correlationId, result.Errors.First().Message);
                    return new TestResults { Total = 0, Passed = 0, Failed = 0, Skipped = 0 };
                }
                return result.Value;
            });
            tasks.Add(trxTask);
        }

        // Parse log file
        Task<object>? logTask = null;
        if (!string.IsNullOrEmpty(input.LogFilePath))
        {
            logTask = Task.Run<object>(() =>
            {
                Log.Information("[{CorrelationId}] Parsing log file: {FilePath}", correlationId, input.LogFilePath);
                var result = _logParser.Parse(input.LogFilePath);
                if (result.IsFailed)
                {
                    Log.Warning("[{CorrelationId}] Log parsing failed: {Error}", correlationId, result.Errors.First().Message);
                    return new LogResults();
                }
                return result.Value;
            });
            tasks.Add(logTask);
        }

        // Parse SSIM results file
        Task<object>? ssimTask = null;
        if (!string.IsNullOrEmpty(input.SsimResultsPath))
        {
            ssimTask = Task.Run<object>(() =>
            {
                Log.Information("[{CorrelationId}] Parsing SSIM results: {FilePath}", correlationId, input.SsimResultsPath);
                var result = _ssimParser.Parse(input.SsimResultsPath);
                if (result.IsFailed)
                {
                    Log.Warning("[{CorrelationId}] SSIM parsing failed: {Error}", correlationId, result.Errors.First().Message);
                    return new SsimResults { Tests = new List<SsimTestResult>() };
                }
                return result.Value;
            });
            tasks.Add(ssimTask);
        }

        // Wait for all parsers to complete
        await Task.WhenAll(tasks);

        // Extract results
        var testResults = trxTask != null ? (TestResults)await trxTask : new TestResults { Total = 0, Passed = 0, Failed = 0, Skipped = 0 };
        var logResults = logTask != null ? (LogResults)await logTask : new LogResults();
        var ssimResults = ssimTask != null ? (SsimResults)await ssimTask : new SsimResults { Tests = new List<SsimTestResult>() };

        Log.Information(
            "[{CorrelationId}] Parsing completed: Tests={TestCount}, LogEntries={LogCount}, VisualTests={VisualCount}",
            correlationId, testResults.Total, logResults.Entries.Count, ssimResults.Total);

        return (testResults, logResults, ssimResults);
    }

    /// <summary>
    /// Run all analyzers.
    /// </summary>
    private async Task<(LogPatterns, VisualAnalysis, List<TestFailureAnalysis>)> RunAnalyzersAsync(
        TestResults testResults,
        LogResults logResults,
        SsimResults ssimResults,
        AnalysisInput input,
        string correlationId,
        CancellationToken cancellationToken)
    {
        // Analyze log patterns
        Log.Information("[{CorrelationId}] Analyzing log patterns", correlationId);
        var logPatternsResult = _logPatternAnalyzer.Analyze(logResults, input.BaselineErrorsPerHour);
        var logPatterns = logPatternsResult.IsSuccess
            ? logPatternsResult.Value
            : new LogPatterns
            {
                ErrorRate = new ErrorRateAnalysis(),
                RepeatedExceptions = new List<RepeatedExceptionPattern>(),
                PerformanceWarnings = new List<PerformanceWarning>(),
                MissingCorrelationIds = new List<string>()
            };

        // Analyze visual regressions
        Log.Information("[{CorrelationId}] Analyzing visual regressions", correlationId);
        var visualAnalysisResult = _visualRegressionAnalyzer.Analyze(ssimResults, input.BuildInfo.BuildId);
        var visualAnalysis = visualAnalysisResult.IsSuccess
            ? visualAnalysisResult.Value
            : new VisualAnalysis
            {
                Regressions = new List<VisualRegression>(),
                Trends = new List<VisualTrend>(),
                TotalTests = 0,
                PassedTests = 0,
                MinorRegressions = 0,
                MajorRegressions = 0,
                CriticalRegressions = 0,
                DegradingTests = 0
            };

        // Analyze test failures with AI
        Log.Information("[{CorrelationId}] Analyzing {FailureCount} test failures", correlationId, testResults.Failures.Count);
        var testFailureAnalyses = new List<TestFailureAnalysis>();

        foreach (var failure in testResults.Failures)
        {
            // Find related logs by correlation ID or test name
            var relatedLogs = FindRelatedLogs(failure, logResults);

            Log.Information("[{CorrelationId}] Analyzing test failure: {TestName}", correlationId, failure.TestName);
            var analysisResult = await _testFailureAnalyzer.AnalyzeFailureAsync(failure, relatedLogs, cancellationToken);

            if (analysisResult.IsSuccess)
            {
                testFailureAnalyses.Add(analysisResult.Value);
            }
            else
            {
                Log.Warning("[{CorrelationId}] Test failure analysis failed for {TestName}: {Error}",
                    correlationId, failure.TestName, analysisResult.Errors.First().Message);

                // Add a fallback analysis
                testFailureAnalyses.Add(new TestFailureAnalysis
                {
                    TestName = failure.TestName,
                    UsedFallback = true,
                    AnalysisError = analysisResult.Errors.First().Message
                });
            }
        }

        Log.Information(
            "[{CorrelationId}] Analysis completed: LogPatterns={PatternCount}, VisualRegressions={RegressionCount}, TestFailureAnalyses={AnalysisCount}",
            correlationId, logPatterns.RepeatedExceptions.Count + logPatterns.PerformanceWarnings.Count,
            visualAnalysis.Regressions.Count, testFailureAnalyses.Count);

        return (logPatterns, visualAnalysis, testFailureAnalyses);
    }

    /// <summary>
    /// Find log entries related to a test failure.
    /// </summary>
    private List<LogEntry> FindRelatedLogs(TestFailure failure, LogResults logResults)
    {
        var relatedLogs = new List<LogEntry>();

        // First, try to find logs by correlation ID
        if (!string.IsNullOrEmpty(failure.CorrelationId) &&
            logResults.EntriesByCorrelationId.TryGetValue(failure.CorrelationId, out var correlatedLogs))
        {
            relatedLogs.AddRange(correlatedLogs);
        }

        // If no correlation ID or no logs found, try to find logs by test name in message
        if (relatedLogs.Count == 0)
        {
            var testNameLogs = logResults.Entries
                .Where(e => e.Message.Contains(failure.TestName, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();

            relatedLogs.AddRange(testNameLogs);
        }

        return relatedLogs;
    }
}
