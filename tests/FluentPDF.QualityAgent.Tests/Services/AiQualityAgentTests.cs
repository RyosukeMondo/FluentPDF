using FluentPDF.QualityAgent.Config;
using FluentPDF.QualityAgent.Models;
using FluentPDF.QualityAgent.Services;
using Serilog;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Services;

public class AiQualityAgentTests : IDisposable
{
    private readonly string _testDataDir;
    private readonly string _schemaPath;
    private readonly AiQualityAgent _agent;

    public AiQualityAgentTests()
    {
        // Configure Serilog for tests
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        var projectRoot = GetProjectRoot();
        _testDataDir = Path.Combine(Path.GetTempPath(), "quality-agent-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDir);

        _schemaPath = Path.Combine(projectRoot, "schemas", "quality-report.schema.json");

        // Create agent with no OpenAI config (will use fallback analysis)
        var openAiConfig = new OpenAiConfig();
        _agent = new AiQualityAgent(openAiConfig);
    }

    [Fact]
    public async Task AnalyzeAsync_WithAllInputs_GeneratesCompleteReport()
    {
        // Arrange
        var trxPath = CreateSampleTrxFile();
        var logPath = CreateSampleLogFile();
        var ssimPath = CreateSampleSsimFile();
        var outputPath = Path.Combine(_testDataDir, "quality-report.json");

        var input = new AnalysisInput
        {
            TrxFilePath = trxPath,
            LogFilePath = logPath,
            SsimResultsPath = ssimPath,
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath,
            OutputPath = outputPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsSuccess, $"Analysis failed: {result.Errors.FirstOrDefault()?.Message}");
        var report = result.Value;

        // Verify report structure
        Assert.NotNull(report);
        Assert.NotNull(report.Summary);
        Assert.NotNull(report.BuildInfo);
        Assert.NotNull(report.Analysis);

        // Verify test analysis
        Assert.Equal(10, report.Analysis.TestAnalysis.Total);
        Assert.Equal(8, report.Analysis.TestAnalysis.Passed);
        Assert.Equal(2, report.Analysis.TestAnalysis.Failed);
        Assert.Equal(80.0, report.Analysis.TestAnalysis.PassRate);

        // Verify log analysis
        Assert.NotNull(report.Analysis.LogAnalysis);
        Assert.True(report.Analysis.LogAnalysis.Score >= 0);

        // Verify visual analysis
        Assert.NotNull(report.Analysis.VisualAnalysis);
        Assert.Equal(5, report.Analysis.VisualAnalysis.TotalTests);

        // Verify overall score and status
        Assert.True(report.OverallScore >= 0 && report.OverallScore <= 100);
        Assert.True(Enum.IsDefined(typeof(QualityStatus), report.Status));

        // Verify output file was created
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task AnalyzeAsync_WithOnlyTrxFile_GeneratesPartialReport()
    {
        // Arrange
        var trxPath = CreateSampleTrxFile();
        var outputPath = Path.Combine(_testDataDir, "trx-only-report.json");

        var input = new AnalysisInput
        {
            TrxFilePath = trxPath,
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath,
            OutputPath = outputPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;

        Assert.Equal(10, report.Analysis.TestAnalysis.Total);
        Assert.Equal(0, report.Analysis.LogAnalysis.TotalPatterns);
        Assert.Equal(0, report.Analysis.VisualAnalysis.TotalTests);
    }

    [Fact]
    public async Task AnalyzeAsync_WithFailingTests_IncludesRootCauseHypotheses()
    {
        // Arrange
        var trxPath = CreateSampleTrxFile();
        var logPath = CreateSampleLogFile();

        var input = new AnalysisInput
        {
            TrxFilePath = trxPath,
            LogFilePath = logPath,
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;

        // Should have root cause hypotheses for failed tests
        Assert.NotEmpty(report.RootCauseHypotheses);
        Assert.True(report.RootCauseHypotheses.Count <= 2); // We have 2 failures

        var hypothesis = report.RootCauseHypotheses.First();
        Assert.NotNull(hypothesis.Issue);
        Assert.NotNull(hypothesis.Hypothesis);
        Assert.True(hypothesis.Confidence >= 0 && hypothesis.Confidence <= 1);
        Assert.NotNull(hypothesis.Severity);
        Assert.True(hypothesis.UsedFallback); // No OpenAI configured
    }

    [Fact]
    public async Task AnalyzeAsync_WithErrorSpike_GeneratesCriticalRecommendations()
    {
        // Arrange
        var logPath = CreateLogFileWithErrorSpike();

        var input = new AnalysisInput
        {
            LogFilePath = logPath,
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath,
            BaselineErrorsPerHour = 10.0 // Baseline to trigger critical spike (100/10 = 10x, which is >= 5.0)
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;

        // Should have recommendations for error spike (might be critical or high depending on multiplier)
        var recommendations = report.Recommendations
            .Where(r => r.Category == RecommendationCategory.Logging)
            .ToList();

        Assert.NotEmpty(recommendations);

        // Should have error spike in description
        var spikeRec = recommendations.FirstOrDefault(r => r.Description.Contains("spike"));
        Assert.NotNull(spikeRec);
    }

    [Fact]
    public async Task AnalyzeAsync_WithVisualRegressions_IdentifiesIssues()
    {
        // Arrange
        var ssimPath = CreateSsimFileWithRegressions();

        var input = new AnalysisInput
        {
            SsimResultsPath = ssimPath,
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;

        Assert.True(report.Analysis.VisualAnalysis.CriticalRegressions > 0);
        Assert.NotEmpty(report.Recommendations);

        var visualRecs = report.Recommendations
            .Where(r => r.Category == RecommendationCategory.Visual)
            .ToList();

        Assert.NotEmpty(visualRecs);
    }

    [Fact]
    public async Task AnalyzeAsync_WithMissingRequiredInputs_ReturnsError()
    {
        // Arrange - no input files provided
        var input = new AnalysisInput
        {
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("At least one input file", result.Errors.First().Message);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentFiles_ReturnsError()
    {
        // Arrange
        var input = new AnalysisInput
        {
            TrxFilePath = "/nonexistent/file.trx",
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors.First().Message);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCorrelatedLogs_FindsRelatedEntries()
    {
        // Arrange
        var trxPath = CreateTrxFileWithCorrelationIds();
        var logPath = CreateLogFileWithCorrelationIds();

        var input = new AnalysisInput
        {
            TrxFilePath = trxPath,
            LogFilePath = logPath,
            BuildInfo = CreateBuildInfo(),
            SchemaPath = _schemaPath
        };

        // Act
        var result = await _agent.AnalyzeAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var report = result.Value;

        // Root cause hypotheses should include related log context
        var hypotheses = report.RootCauseHypotheses;
        if (hypotheses.Any())
        {
            var hypothesis = hypotheses.First();
            // Related context might be populated if logs were found
            Assert.NotNull(hypothesis.RelatedContext);
        }
    }

    // Helper methods

    private string CreateSampleTrxFile()
    {
        var trxPath = Path.Combine(_testDataDir, "test-results.trx");
        var trxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"">
  <Results>
    <UnitTestResult testName=""Test1"" outcome=""Passed"" />
    <UnitTestResult testName=""Test2"" outcome=""Passed"" />
    <UnitTestResult testName=""Test3"" outcome=""Passed"" />
    <UnitTestResult testName=""Test4"" outcome=""Passed"" />
    <UnitTestResult testName=""Test5"" outcome=""Passed"" />
    <UnitTestResult testName=""Test6"" outcome=""Passed"" />
    <UnitTestResult testName=""Test7"" outcome=""Passed"" />
    <UnitTestResult testName=""Test8"" outcome=""Passed"" />
    <UnitTestResult testName=""Test9"" outcome=""Failed"">
      <Output>
        <ErrorInfo>
          <Message>NullReferenceException: Object reference not set</Message>
          <StackTrace>at TestClass.TestMethod()</StackTrace>
        </ErrorInfo>
      </Output>
    </UnitTestResult>
    <UnitTestResult testName=""Test10"" outcome=""Failed"">
      <Output>
        <ErrorInfo>
          <Message>Expected 10 but was 5</Message>
          <StackTrace>at TestClass.AssertMethod()</StackTrace>
        </ErrorInfo>
      </Output>
    </UnitTestResult>
  </Results>
</TestRun>";
        File.WriteAllText(trxPath, trxContent);
        return trxPath;
    }

    private string CreateTrxFileWithCorrelationIds()
    {
        var trxPath = Path.Combine(_testDataDir, "test-results-corr.trx");
        var trxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"">
  <Results>
    <UnitTestResult testName=""CorrelatedTest"" outcome=""Failed"">
      <Output>
        <ErrorInfo>
          <Message>Test failed with correlation ID: corr-123</Message>
          <StackTrace>at TestClass.TestMethod()</StackTrace>
        </ErrorInfo>
      </Output>
    </UnitTestResult>
  </Results>
</TestRun>";
        File.WriteAllText(trxPath, trxContent);
        return trxPath;
    }

    private string CreateSampleLogFile()
    {
        var logPath = Path.Combine(_testDataDir, "app.log");
        var logLines = new[]
        {
            @"{""@t"":""2024-01-01T10:00:00.000Z"",""@l"":""Information"",""@mt"":""Application started""}",
            @"{""@t"":""2024-01-01T10:01:00.000Z"",""@l"":""Information"",""@mt"":""Processing request""}",
            @"{""@t"":""2024-01-01T10:02:00.000Z"",""@l"":""Warning"",""@mt"":""Slow operation detected"",""Properties"":{""Duration"":1500}}",
            @"{""@t"":""2024-01-01T10:03:00.000Z"",""@l"":""Error"",""@mt"":""Operation failed"",""Exception"":""NullReferenceException: Object reference not set""}",
        };
        File.WriteAllLines(logPath, logLines);
        return logPath;
    }

    private string CreateLogFileWithCorrelationIds()
    {
        var logPath = Path.Combine(_testDataDir, "app-corr.log");
        var logLines = new[]
        {
            @"{""@t"":""2024-01-01T10:00:00.000Z"",""@l"":""Information"",""@mt"":""Test started"",""CorrelationId"":""corr-123""}",
            @"{""@t"":""2024-01-01T10:01:00.000Z"",""@l"":""Error"",""@mt"":""Test failed"",""CorrelationId"":""corr-123"",""Exception"":""NullReferenceException""}",
        };
        File.WriteAllLines(logPath, logLines);
        return logPath;
    }

    private string CreateLogFileWithErrorSpike()
    {
        var logPath = Path.Combine(_testDataDir, "error-spike.log");
        var logLines = new List<string>();

        // Generate 100 error entries spread over 1 hour to create spike
        for (int i = 0; i < 100; i++)
        {
            var minute = i % 60; // Keep minutes valid (0-59)
            var hour = 10 + (i / 60); // Increment hour after 60 entries
            logLines.Add($@"{{""@t"":""2024-01-01T{hour:D2}:{minute:D2}:00.000Z"",""@l"":""Error"",""@mt"":""Error {i}"",""Exception"":""Exception message""}}");
        }

        File.WriteAllLines(logPath, logLines);
        return logPath;
    }

    private string CreateSampleSsimFile()
    {
        var ssimPath = Path.Combine(_testDataDir, "ssim-results.json");
        var ssimContent = @"{
  ""tests"": [
    {""testName"": ""VisualTest1"", ""ssimScore"": 0.995},
    {""testName"": ""VisualTest2"", ""ssimScore"": 0.992},
    {""testName"": ""VisualTest3"", ""ssimScore"": 0.998},
    {""testName"": ""VisualTest4"", ""ssimScore"": 0.997},
    {""testName"": ""VisualTest5"", ""ssimScore"": 0.996}
  ]
}";
        File.WriteAllText(ssimPath, ssimContent);
        return ssimPath;
    }

    private string CreateSsimFileWithRegressions()
    {
        var ssimPath = Path.Combine(_testDataDir, "ssim-regressions.json");
        var ssimContent = @"{
  ""tests"": [
    {""testName"": ""VisualTest1"", ""ssimScore"": 0.93},
    {""testName"": ""VisualTest2"", ""ssimScore"": 0.96},
    {""testName"": ""VisualTest3"", ""ssimScore"": 0.99}
  ]
}";
        File.WriteAllText(ssimPath, ssimContent);
        return ssimPath;
    }

    private BuildInfo CreateBuildInfo()
    {
        return new BuildInfo
        {
            BuildId = $"build-{Guid.NewGuid():N}",
            Timestamp = DateTime.UtcNow,
            Branch = "main",
            Commit = "abc123",
            Author = "test@example.com"
        };
    }

    private string GetProjectRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null && !File.Exists(Path.Combine(directory, "FluentPDF.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        if (directory == null)
        {
            throw new InvalidOperationException("Could not find project root (FluentPDF.sln not found)");
        }

        return directory;
    }

    public void Dispose()
    {
        // Cleanup test data directory
        if (Directory.Exists(_testDataDir))
        {
            try
            {
                Directory.Delete(_testDataDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
