using FluentPDF.QualityAgent.Analyzers;
using FluentPDF.QualityAgent.Config;
using FluentPDF.QualityAgent.Models;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Analyzers;

public class TestFailureAnalyzerTests
{
    [Fact]
    public async Task AnalyzeFailureAsync_WithNoApiKey_UsesFallback()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_NullReference",
            ErrorMessage = "System.NullReferenceException: Object reference not set to an instance of an object.",
            StackTrace = "at MyClass.MyMethod() in Program.cs:line 42"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.Equal("Test_NullReference", analysis.TestName);
        Assert.True(analysis.UsedFallback);
        Assert.NotNull(analysis.RuleBasedHypothesis);
        Assert.Null(analysis.AiHypothesis);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithNullReferenceException_IdentifiesCorrectly()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_NullRef",
            ErrorMessage = "NullReferenceException: Object reference not set to an instance of an object.",
            StackTrace = "at Program.Main() in Program.cs:line 10"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("Null reference exception", hypothesis.Issue);
        Assert.Contains("null", hypothesis.Hypothesis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("High", hypothesis.Severity);
        Assert.True(hypothesis.Confidence >= 0.7);
        Assert.Contains(hypothesis.RecommendedActions, a => a.Contains("null check", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithAssertionFailure_IdentifiesCorrectly()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_Assertion",
            ErrorMessage = "Assert.Equal() Failure: Expected: 42, Actual: 43",
            StackTrace = "at TestClass.TestMethod() in Tests.cs:line 20"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("Assertion failure", hypothesis.Issue);
        Assert.Contains("expected", hypothesis.Hypothesis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Medium", hypothesis.Severity);
        Assert.Contains(hypothesis.RecommendedActions, a => a.Contains("expected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithTimeout_IdentifiesCorrectly()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_Timeout",
            ErrorMessage = "Test execution timed out after 30000ms",
            StackTrace = "at AsyncTest.WaitForever() in AsyncTests.cs:line 15"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("Test timeout", hypothesis.Issue);
        Assert.Contains("longer than expected", hypothesis.Hypothesis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("High", hypothesis.Severity);
        Assert.Contains(hypothesis.RecommendedActions, a => a.Contains("timeout", StringComparison.OrdinalIgnoreCase) || a.Contains("performance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithFileNotFound_IdentifiesCorrectly()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_FileNotFound",
            ErrorMessage = "System.IO.FileNotFoundException: Could not find file 'data.txt'",
            StackTrace = "at FileReader.ReadFile() in FileReader.cs:line 25"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("File not found", hypothesis.Issue);
        Assert.Contains("file", hypothesis.Hypothesis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Medium", hypothesis.Severity);
        Assert.Contains(hypothesis.RecommendedActions, a => a.Contains("file", StringComparison.OrdinalIgnoreCase) || a.Contains("path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithNetworkError_IdentifiesCorrectly()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_Network",
            ErrorMessage = "System.Net.Http.HttpRequestException: Connection refused",
            StackTrace = "at HttpClient.SendAsync() in HttpClient.cs:line 100"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("Network or connection error", hypothesis.Issue);
        Assert.Contains("network", hypothesis.Hypothesis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("High", hypothesis.Severity);
        Assert.Contains(hypothesis.RecommendedActions, a => a.Contains("network", StringComparison.OrdinalIgnoreCase) || a.Contains("connectivity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithRelatedLogs_IncludesContextInRuleBasedAnalysis()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_WithLogs",
            ErrorMessage = "Test failed",
            StackTrace = "at Test.Method()"
        };
        var relatedLogs = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Error",
                Message = "Database connection failed",
                CorrelationId = "abc123"
            },
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Error",
                Message = "Retry attempt 3 failed",
                CorrelationId = "abc123"
            },
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Info",
                Message = "Processing request",
                CorrelationId = "abc123"
            }
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure, relatedLogs);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.NotEmpty(hypothesis.RelatedContext);
        Assert.Contains(hypothesis.RelatedContext, c => c.Contains("Database connection failed"));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithUnknownError_ReturnsGenericHypothesis()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_Unknown",
            ErrorMessage = "Something unexpected happened",
            StackTrace = "at Unknown.Method()"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("Test failure detected", hypothesis.Issue);
        Assert.Equal("Unable to determine root cause", hypothesis.Hypothesis);
        Assert.Equal("Medium", hypothesis.Severity);
        Assert.True(hypothesis.Confidence > 0);
        Assert.NotEmpty(hypothesis.RecommendedActions);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFailureAnalyzer(null!));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithEmptyErrorMessage_HandlesGracefully()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_Empty",
            ErrorMessage = "",
            StackTrace = ""
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithMultipleErrorPatterns_PrioritizesFirst()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_Multiple",
            ErrorMessage = "NullReferenceException occurred during assertion: Expected 5 but got null",
            StackTrace = "at Test.Method()"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        // NullReferenceException should be detected first
        Assert.Equal("Null reference exception", hypothesis.Issue);
    }

    [Theory]
    [InlineData("Timeout occurred", "Test timeout")]
    [InlineData("Operation timed out", "Test timeout")]
    [InlineData("Test TIMED OUT after 5 seconds", "Test timeout")]
    public async Task AnalyzeFailureAsync_WithTimeoutVariations_DetectsCorrectly(string errorMessage, string expectedIssue)
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_TimeoutVariation",
            ErrorMessage = errorMessage,
            StackTrace = "at Test.Method()"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal(expectedIssue, hypothesis.Issue);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_WithLargeNumberOfLogs_LimitsContext()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_ManyLogs",
            ErrorMessage = "Test failed",
            StackTrace = "at Test.Method()"
        };

        // Create 20 error logs
        var relatedLogs = Enumerable.Range(1, 20)
            .Select(i => new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Error",
                Message = $"Error {i}",
                CorrelationId = "abc123"
            })
            .ToList();

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure, relatedLogs);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        // Should limit to 3 error logs in context
        Assert.True(hypothesis.RelatedContext.Count <= 3);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_CaseInsensitivePatternMatching_WorksCorrectly()
    {
        // Arrange
        var config = new OpenAiConfig { ApiKey = null };
        var analyzer = new TestFailureAnalyzer(config);
        var failure = new TestFailure
        {
            TestName = "Test_CaseInsensitive",
            ErrorMessage = "CONNECTION ERROR: Failed to reach server",
            StackTrace = "at NetworkClient.Connect()"
        };

        // Act
        var result = await analyzer.AnalyzeFailureAsync(failure);

        // Assert
        Assert.True(result.IsSuccess);
        var hypothesis = result.Value.RuleBasedHypothesis;
        Assert.NotNull(hypothesis);
        Assert.Equal("Network or connection error", hypothesis.Issue);
    }
}
