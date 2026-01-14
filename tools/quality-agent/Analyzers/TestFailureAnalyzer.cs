using Azure;
using Azure.AI.OpenAI;
using FluentPDF.QualityAgent.Config;
using FluentPDF.QualityAgent.Models;
using FluentResults;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using System.Text.Json;

namespace FluentPDF.QualityAgent.Analyzers;

/// <summary>
/// Analyzes test failures using AI to generate root cause hypotheses.
/// </summary>
public class TestFailureAnalyzer
{
    private readonly OpenAiConfig _config;
    private readonly ChatClient? _chatClient;

    public TestFailureAnalyzer(OpenAiConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (_config.IsValid())
        {
            try
            {
                // Only support Azure OpenAI for now
                // For standard OpenAI, users can set Azure endpoint to api.openai.com
                if (_config.UseAzure)
                {
                    var azureClient = new AzureOpenAIClient(
                        new Uri(_config.AzureEndpoint!),
                        new AzureKeyCredential(_config.ApiKey!));
                    _chatClient = azureClient.GetChatClient(_config.AzureDeploymentName!);
                }
                // If API key but no Azure endpoint, we can't initialize
                // Will fall back to rule-based analysis
            }
            catch (Exception ex)
            {
                // Log but don't throw - will fall back to rule-based analysis
                Console.WriteLine($"Warning: Failed to initialize OpenAI client: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Analyze a test failure with AI assistance.
    /// </summary>
    public async Task<Result<TestFailureAnalysis>> AnalyzeFailureAsync(
        TestFailure failure,
        List<LogEntry>? relatedLogs = null,
        CancellationToken cancellationToken = default)
    {
        var analysis = new TestFailureAnalysis
        {
            TestName = failure.TestName,
            UsedFallback = false
        };

        // Try AI analysis first if available
        if (_chatClient != null)
        {
            var aiResult = await AnalyzeWithAiAsync(failure, relatedLogs, cancellationToken);
            if (aiResult.IsSuccess)
            {
                analysis = analysis with { AiHypothesis = aiResult.Value };
                return Result.Ok(analysis);
            }
            else
            {
                analysis = analysis with { AnalysisError = aiResult.Errors.FirstOrDefault()?.Message };
            }
        }

        // Fall back to rule-based analysis
        var ruleBasedResult = AnalyzeWithRules(failure, relatedLogs);
        analysis = analysis with
        {
            RuleBasedHypothesis = ruleBasedResult,
            UsedFallback = true
        };

        return Result.Ok(analysis);
    }

    /// <summary>
    /// Analyze test failure using OpenAI with retry logic.
    /// </summary>
    private async Task<Result<RootCauseHypothesis>> AnalyzeWithAiAsync(
        TestFailure failure,
        List<LogEntry>? relatedLogs,
        CancellationToken cancellationToken)
    {
        if (_chatClient == null)
        {
            return Result.Fail<RootCauseHypothesis>("OpenAI client not initialized");
        }

        var prompt = BuildPrompt(failure, relatedLogs);

        // Truncate prompt if too long
        if (prompt.Length > _config.MaxPromptTokens * 4) // Rough estimate: 1 token ≈ 4 chars
        {
            prompt = prompt.Substring(0, _config.MaxPromptTokens * 4);
        }

        // Retry with exponential backoff
        var delay = _config.InitialRetryDelayMs;
        Exception? lastException = null;

        for (int attempt = 0; attempt < _config.MaxRetries; attempt++)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(GetSystemPrompt()),
                    new UserChatMessage(prompt)
                };

                var options = new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                        jsonSchemaFormatName: "root_cause_analysis",
                        jsonSchema: BinaryData.FromString(GetJsonSchema()),
                        jsonSchemaIsStrict: true
                    )
                };

                var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

                if (response?.Value?.Content?.Count > 0)
                {
                    var content = response.Value.Content[0].Text;
                    var hypothesis = JsonSerializer.Deserialize<RootCauseHypothesis>(content);

                    if (hypothesis != null)
                    {
                        return Result.Ok(hypothesis);
                    }
                }

                return Result.Fail<RootCauseHypothesis>("Empty response from OpenAI");
            }
            catch (ClientResultException ex) when (IsTransientError(ex))
            {
                lastException = ex;

                if (attempt < _config.MaxRetries - 1)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay *= 2; // Exponential backoff
                }
            }
            catch (Exception ex)
            {
                // Non-transient error, fail immediately
                return Result.Fail<RootCauseHypothesis>($"OpenAI API error: {ex.Message}");
            }
        }

        return Result.Fail<RootCauseHypothesis>($"OpenAI API failed after {_config.MaxRetries} attempts: {lastException?.Message}");
    }

    /// <summary>
    /// Build the analysis prompt from test failure and logs.
    /// </summary>
    private string BuildPrompt(TestFailure failure, List<LogEntry>? relatedLogs)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Test Name: {failure.TestName}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(failure.ErrorMessage))
        {
            sb.AppendLine("Error Message:");
            sb.AppendLine(SanitizeText(failure.ErrorMessage));
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(failure.StackTrace))
        {
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(SanitizeText(failure.StackTrace));
            sb.AppendLine();
        }

        if (relatedLogs != null && relatedLogs.Count > 0)
        {
            sb.AppendLine("Related Log Entries:");
            foreach (var log in relatedLogs.Take(10)) // Limit to 10 logs
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Level}: {SanitizeText(log.Message)}");

                if (log.Exception != null)
                {
                    sb.AppendLine($"  Exception: {SanitizeText(log.Exception.Type)} - {SanitizeText(log.Exception.Message)}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Sanitize text to remove potential PII and secrets.
    /// </summary>
    private string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Remove common patterns that might contain secrets
        var sanitized = text;

        // Remove API keys, tokens, passwords
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(api[_-]?key|token|password|secret)[\s:=]+[^\s]+",
            "$1=***REDACTED***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove email addresses
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
            "***EMAIL***");

        return sanitized;
    }

    /// <summary>
    /// Get the system prompt for AI analysis.
    /// </summary>
    private string GetSystemPrompt()
    {
        return @"You are a quality analyst expert at diagnosing test failures in software systems.

Analyze the provided test failure information including error messages, stack traces, and related log entries.

Your task is to:
1. Identify the specific issue that caused the test to fail
2. Hypothesize the root cause based on the available evidence
3. Assess the confidence level of your hypothesis (0.0-1.0)
4. Determine the severity (Low, Medium, High, Critical)
5. Recommend specific actions to resolve the issue

Be concise but thorough. Focus on actionable insights.";
    }

    /// <summary>
    /// Get the JSON schema for structured output.
    /// </summary>
    private string GetJsonSchema()
    {
        return @"{
  ""type"": ""object"",
  ""properties"": {
    ""Issue"": {
      ""type"": ""string"",
      ""description"": ""The specific issue or problem identified""
    },
    ""Hypothesis"": {
      ""type"": ""string"",
      ""description"": ""The hypothesis about the root cause""
    },
    ""Confidence"": {
      ""type"": ""number"",
      ""description"": ""Confidence level from 0.0 to 1.0"",
      ""minimum"": 0.0,
      ""maximum"": 1.0
    },
    ""Severity"": {
      ""type"": ""string"",
      ""description"": ""Severity level"",
      ""enum"": [""Low"", ""Medium"", ""High"", ""Critical""]
    },
    ""RecommendedActions"": {
      ""type"": ""array"",
      ""description"": ""List of recommended actions"",
      ""items"": {
        ""type"": ""string""
      }
    },
    ""RelatedContext"": {
      ""type"": ""array"",
      ""description"": ""Related context or log entries"",
      ""items"": {
        ""type"": ""string""
      }
    }
  },
  ""required"": [""Issue"", ""Hypothesis"", ""Confidence"", ""Severity"", ""RecommendedActions"", ""RelatedContext""],
  ""additionalProperties"": false
}";
    }

    /// <summary>
    /// Analyze test failure using rule-based logic (fallback).
    /// </summary>
    private RootCauseHypothesis AnalyzeWithRules(TestFailure failure, List<LogEntry>? relatedLogs)
    {
        var issue = "Test failure detected";
        var hypothesis = "Unable to determine root cause";
        var severity = "Medium";
        var confidence = 0.5;
        var actions = new List<string> { "Review test failure details", "Check related logs" };
        var context = new List<string>();

        var errorMsg = failure.ErrorMessage ?? "";
        var stackTrace = failure.StackTrace ?? "";

        // Rule: Null reference exception
        if (errorMsg.Contains("NullReferenceException") || stackTrace.Contains("NullReferenceException"))
        {
            issue = "Null reference exception";
            hypothesis = "A null object was accessed without proper null checking";
            severity = "High";
            confidence = 0.8;
            actions = new List<string>
            {
                "Add null checks before accessing the object",
                "Initialize the object before use",
                "Use null-conditional operators (?. or ?[])"
            };
        }
        // Rule: Assertion failure
        else if (errorMsg.Contains("Assert") || errorMsg.Contains("Expected") || errorMsg.Contains("Actual"))
        {
            issue = "Assertion failure";
            hypothesis = "Expected value does not match actual value";
            severity = "Medium";
            confidence = 0.7;
            actions = new List<string>
            {
                "Verify expected values are correct",
                "Check if implementation logic changed",
                "Review test assumptions"
            };
        }
        // Rule: Timeout
        else if (errorMsg.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                 errorMsg.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            issue = "Test timeout";
            hypothesis = "Operation took longer than expected, possibly due to performance issue or deadlock";
            severity = "High";
            confidence = 0.75;
            actions = new List<string>
            {
                "Increase timeout threshold if operation is legitimately slow",
                "Investigate performance bottlenecks",
                "Check for deadlocks or blocking operations"
            };
        }
        // Rule: File not found
        else if (errorMsg.Contains("FileNotFoundException") || errorMsg.Contains("file not found", StringComparison.OrdinalIgnoreCase))
        {
            issue = "File not found";
            hypothesis = "Required file is missing or path is incorrect";
            severity = "Medium";
            confidence = 0.85;
            actions = new List<string>
            {
                "Verify file exists at expected path",
                "Check file path configuration",
                "Ensure test setup creates required files"
            };
        }
        // Rule: Network/connection error
        else if (errorMsg.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                 errorMsg.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                 errorMsg.Contains("HttpRequestException"))
        {
            issue = "Network or connection error";
            hypothesis = "Network connectivity issue or service unavailable";
            severity = "High";
            confidence = 0.7;
            actions = new List<string>
            {
                "Check network connectivity",
                "Verify service endpoints are accessible",
                "Add retry logic for transient failures"
            };
        }

        // Add context from related logs
        if (relatedLogs != null && relatedLogs.Count > 0)
        {
            var errorLogs = relatedLogs.Where(l => l.Level == "Error").Take(3);
            foreach (var log in errorLogs)
            {
                context.Add($"Error log: {log.Message}");
            }
        }

        return new RootCauseHypothesis
        {
            Issue = issue,
            Hypothesis = hypothesis,
            Severity = severity,
            Confidence = confidence,
            RecommendedActions = actions,
            RelatedContext = context
        };
    }

    /// <summary>
    /// Determine if an error is transient and should be retried.
    /// </summary>
    private bool IsTransientError(ClientResultException ex)
    {
        // Rate limiting errors (429)
        if (ex.Status == 429)
            return true;

        // Server errors (500-599)
        if (ex.Status >= 500 && ex.Status < 600)
            return true;

        // Timeout errors
        if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
