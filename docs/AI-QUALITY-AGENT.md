# AI Quality Agent Architecture

## Overview

The AI Quality Agent is an intelligent quality assessment system that analyzes test results, logs, visual regressions, and validation reports to provide comprehensive quality insights for FluentPDF builds. It combines traditional rule-based analysis with AI-powered root cause hypothesis to deliver actionable recommendations.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     CI/CD Pipeline                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Tests   │  │   Logs   │  │ Visual   │  │Validation│   │
│  │ (TRX)    │  │  (JSON)  │  │ (SSIM)   │  │  (JSON)  │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │             │              │              │         │
│       └─────────────┴──────────────┴──────────────┘         │
│                           │                                 │
└───────────────────────────┼─────────────────────────────────┘
                            ▼
              ┌─────────────────────────┐
              │  AI Quality Agent CLI   │
              │    (Program.cs)         │
              └────────────┬────────────┘
                           │
                           ▼
              ┌─────────────────────────┐
              │   AiQualityAgent        │
              │   (Orchestration)       │
              └────────────┬────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
   ┌─────────┐      ┌──────────┐      ┌──────────┐
   │ Parsers │      │Analyzers │      │ Reports  │
   └─────────┘      └──────────┘      └──────────┘
        │                  │                  │
        │                  ▼                  │
        │           ┌─────────────┐           │
        │           │  OpenAI API │           │
        │           │ (GPT-4o)    │           │
        │           └─────────────┘           │
        │                                     │
        └──────────────┬──────────────────────┘
                       ▼
              ┌─────────────────┐
              │ Quality Report  │
              │    (JSON)       │
              └─────────────────┘
```

### Component Architecture

#### 1. Parsers Layer

Parsers extract structured data from various input formats:

- **TrxParser**: Parses xUnit/NUnit TRX XML files
  - Extracts test outcomes (passed, failed, skipped)
  - Captures error messages and stack traces
  - Aggregates test statistics

- **LogParser**: Parses Serilog JSON logs
  - Reads logs line-by-line for memory efficiency
  - Extracts timestamp, level, message, correlation ID, properties
  - Groups related log entries by correlation ID

- **SsimParser**: Parses SSIM visual regression results
  - Extracts SSIM scores and image paths
  - Classifies regressions by severity thresholds

- **ValidationParser**: Parses PDF validation reports (future)

#### 2. Analyzers Layer

Analyzers process parsed data to detect patterns and issues:

- **TestFailureAnalyzer**: AI-powered root cause analysis
  - Integrates with OpenAI/Azure OpenAI
  - Sends test failure context + related logs
  - Uses structured outputs (JSON Schema) for consistent results
  - Implements retry logic with exponential backoff
  - Falls back to rule-based analysis on API failures

- **LogPatternAnalyzer**: Detects anomalies in logs
  - Error rate spike detection (> 2x baseline)
  - Repeated exception grouping (by stack trace hash)
  - Performance warning detection (threshold-based)
  - Missing correlation ID detection

- **VisualRegressionAnalyzer**: Analyzes visual changes
  - SSIM threshold-based classification
  - Trend detection (last 10 runs)
  - Degradation pattern identification (3+ consecutive decreases)

#### 3. Reporting Layer

Generates standardized quality reports:

- **QualityReportGenerator**:
  - Aggregates analysis results
  - Calculates weighted overall score
  - Determines quality status (Pass/Warn/Fail)
  - Validates report against JSON Schema
  - Generates actionable recommendations

#### 4. Orchestration Layer

- **AiQualityAgent Service**:
  - Coordinates all components
  - Runs parsers in parallel (Task.WhenAll)
  - Sequences analyzers appropriately
  - Handles partial failures gracefully
  - Logs all operations with correlation ID

## Data Flow

### 1. Input Collection

```
CI Artifacts → CLI Arguments → AnalysisInput Model
```

The CLI validates and collects:
- TRX file path
- Log directory path
- SSIM results file path
- Validation results file path
- Build metadata (from environment variables)

### 2. Parallel Parsing

```
AnalysisInput → Parsers (Parallel) → Parsed Results
```

Three parsers run concurrently:
1. TrxParser → TestResults
2. LogParser → List<LogEntry>
3. SsimParser → SsimResults

### 3. Sequential Analysis

```
Parsed Results → Analyzers (Sequential) → Analysis Results
```

Analyzers run in sequence (some depend on others):
1. LogPatternAnalyzer(logs) → LogPatterns
2. VisualRegressionAnalyzer(ssim) → VisualAnalysis
3. TestFailureAnalyzer(tests, logs) → RootCauseHypotheses

### 4. Report Generation

```
Analysis Results → QualityReportGenerator → Quality Report
```

Report generator:
1. Aggregates all analysis results
2. Calculates weighted scores
3. Determines overall status
4. Generates recommendations
5. Validates against JSON Schema
6. Serializes to JSON

### 5. Output & Actions

```
Quality Report → CLI Output + File + PR Comment
```

The CLI:
1. Displays summary to console
2. Writes report to JSON file
3. Returns exit code based on status

The CI workflow:
1. Parses the JSON report
2. Posts PR comment with summary
3. Fails build if status = "Fail"

## Scoring Methodology

### Component Scores

#### Test Score (40% weight)
```
TestScore = (PassedTests / TotalTests) * 100
```

#### Log Health Score (30% weight)
```
ErrorPenalty = min(ErrorCount * 5, 50)
SpikePenalty = HasErrorSpike ? 30 : 0
LogScore = max(100 - ErrorPenalty - SpikePenalty, 0)
```

#### Visual Score (20% weight)
```
CriticalPenalty = CriticalRegressions * 20
MajorPenalty = MajorRegressions * 10
MinorPenalty = MinorRegressions * 5
VisualScore = max(100 - CriticalPenalty - MajorPenalty - MinorPenalty, 0)
```

#### Validation Score (10% weight)
```
ValidationScore = (PassedValidations / TotalValidations) * 100
```

### Overall Score Calculation

```
OverallScore = (TestScore * 0.4) +
               (LogScore * 0.3) +
               (VisualScore * 0.2) +
               (ValidationScore * 0.1)
```

### Status Determination

```
Status = OverallScore >= 80 ? Pass :
         OverallScore >= 60 ? Warn :
         Fail
```

## OpenAI Integration

### Configuration

The TestFailureAnalyzer integrates with OpenAI using the Azure.AI.OpenAI SDK.

**Supported Backends**:
- OpenAI API (api.openai.com)
- Azure OpenAI Service

**Configuration**:
```csharp
var config = new OpenAiConfig
{
    ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    Model = "gpt-4o",
    MaxRetries = 3
};
```

### Prompt Engineering

**System Prompt**:
```
You are a quality analyst expert specializing in software testing and debugging.
Analyze test failures and provide root cause hypotheses with actionable recommendations.
```

**User Prompt Template**:
```
Test Failure:
- Test Name: {testName}
- Error Message: {errorMessage}
- Stack Trace: {stackTrace}

Related Logs (same correlation ID):
{relatedLogEntries}

Provide:
1. Issue summary
2. Root cause hypothesis
3. Confidence level (Low/Medium/High)
4. Severity (Info/Low/Medium/High/Critical)
5. Suggested actions
```

### Structured Outputs

The integration uses JSON Schema-based structured outputs:

```json
{
  "type": "object",
  "properties": {
    "issue": { "type": "string" },
    "hypothesis": { "type": "string" },
    "confidence": { "enum": ["Low", "Medium", "High"] },
    "severity": { "enum": ["Info", "Low", "Medium", "High", "Critical"] },
    "suggestedActions": {
      "type": "array",
      "items": { "type": "string" }
    }
  },
  "required": ["issue", "hypothesis", "confidence", "severity", "suggestedActions"]
}
```

### Retry Logic

```
Attempt 1: Immediate
  ↓ (failure)
Wait 1s
Attempt 2
  ↓ (failure)
Wait 2s
Attempt 3
  ↓ (failure)
Fallback to rule-based analysis
```

### Fallback Strategy

When OpenAI is unavailable or fails:
1. Log warning
2. Use rule-based pattern matching:
   - NullReferenceException → Check for null guards
   - ArgumentException → Validate input parameters
   - TimeoutException → Check network/resource availability
3. Confidence level = "Low"
4. Generic recommendations based on exception type

## CI Integration

### Workflow Trigger

```yaml
on:
  workflow_run:
    workflows: ["Test"]
    types: [completed]
```

Quality analysis runs after test workflow completes.

### Artifact Collection

The workflow downloads:
1. **test-results**: TRX files from test execution
2. **validation-reports**: PDF validation JSON reports
3. **visual-regression-results**: SSIM analysis results (when available)

### Quality Agent Execution

```bash
dotnet run --project tools/quality-agent -- \
  --trx-file test-results/core-tests.trx \
  --validation-results validation-reports/report.json \
  --visual-results visual-results/ssim-results.json \
  --output quality-report.json
```

### PR Comment Generation

The workflow parses `quality-report.json` and posts:

```markdown
## ✅ Quality Analysis Report

**Overall Score:** 85/100
**Status:** Pass
**Total Issues:** 3
**Critical Issues:** 0

### Test Analysis
- Total Tests: 150
- Passed: 147
- Failed: 3
- Pass Rate: 98.00%

### Log Analysis
- Errors: 2
- Warnings: 5
- Error Rate: 0.33/hour

### Visual Regression Analysis
- Critical Regressions: 0
- Major Regressions: 0
- Minor Regressions: 1

### Top Recommendations
1. **Fix Failing Tests** (High): 3 tests need attention
2. **Review Error Logs** (Medium): 2 errors detected

[View full quality report](link-to-artifact)
```

### Build Gate

```yaml
- name: Check quality status
  run: |
    STATUS="$(jq -r '.status' quality-report.json)"
    if [ "$STATUS" == "Fail" ]; then
      exit 1  # Fail build
    fi
```

## Security Considerations

### API Key Management

- API keys stored in GitHub Secrets
- Never logged or exposed in console output
- Passed via environment variables only

### Data Sanitization

Before sending to OpenAI:
- Remove PII (personally identifiable information)
- Sanitize file paths (remove sensitive directories)
- Limit payload size (< 4000 tokens)
- Filter out secrets from logs

### Rate Limiting

- Respect OpenAI rate limits
- Implement exponential backoff
- Fallback to rule-based analysis

## Performance Optimization

### Parallel Processing

Parsers run in parallel using `Task.WhenAll`:
```csharp
var (testResults, logResults, ssimResults) = await Task.WhenAll(
    _trxParser.ParseAsync(input.TrxFilePath),
    _logParser.ParseAsync(input.LogFilePath),
    _ssimParser.ParseAsync(input.SsimResultsPath)
);
```

### Memory Efficiency

- LogParser reads files line-by-line (streaming)
- Avoids loading entire files into memory
- Suitable for large log files (100MB+)

### Caching

SSIM trend history is cached locally:
```
.ssim-history/{testName}.json
```

## Extensibility

### Adding New Parsers

1. Implement parser in `Parsers/` directory
2. Parse input format to domain model
3. Return `Result<T>` with FluentResults
4. Add to AiQualityAgent orchestration

### Adding New Analyzers

1. Implement analyzer in `Analyzers/` directory
2. Process parsed data to detect patterns
3. Return analysis results model
4. Integrate into report generator scoring

### Adding New Recommendation Rules

Update `QualityReportGenerator.GenerateRecommendations()`:
```csharp
if (testAnalysis.FailedTests > threshold)
{
    recommendations.Add(new Recommendation
    {
        Title = "Fix Failing Tests",
        Description = $"{testAnalysis.FailedTests} tests failing",
        Severity = "High",
        Category = "Testing"
    });
}
```

## Testing Strategy

### Unit Tests

Each component has dedicated unit tests:
- `TrxParserTests`: Sample TRX files (pass/fail scenarios)
- `LogParserTests`: Sample JSON logs (various levels)
- `SsimParserTests`: Sample SSIM results (various scores)
- `LogPatternAnalyzerTests`: Mock log entries (pattern detection)
- `VisualRegressionAnalyzerTests`: Mock SSIM data (trend detection)

### Integration Tests

`AiQualityAgentTests` verify end-to-end:
- Real TRX files
- Real log files
- Real SSIM results
- Report generation
- Scoring calculation

### CI Tests

Workflow validation:
- Test workflow uploads artifacts
- Quality workflow downloads artifacts
- Quality agent executes successfully
- Report is generated
- PR comment is posted

## Future Enhancements

### Planned Features

1. **Validation Analysis**: Parse and analyze PDF validation reports
2. **Historical Trending**: Track quality metrics over time
3. **Baseline Comparison**: Compare against previous builds
4. **Custom Rules**: User-configurable analysis rules
5. **Multi-Model Support**: Support for Claude, Gemini, etc.
6. **Performance Benchmarks**: Integrate with benchmark results
7. **Code Coverage Analysis**: Correlate coverage with test failures

### Architecture Improvements

1. **Plugin System**: Load analyzers dynamically
2. **Configuration File**: YAML/JSON config instead of CLI args
3. **Real-time Streaming**: WebSocket updates during analysis
4. **Dashboard UI**: Web-based quality dashboard
5. **Alert System**: Slack/Teams notifications on quality degradation

## Troubleshooting

### Common Issues

#### Issue: Parsers return empty results
**Solution**: Verify input file formats match expected schemas

#### Issue: AI analysis timeout
**Solution**: Increase timeout in OpenAiConfig or reduce prompt size

#### Issue: Score calculation seems wrong
**Solution**: Check component weights and individual scores in report

#### Issue: PR comment not posted
**Solution**: Verify GitHub token permissions and PR detection logic

## References

- [CLI Usage Documentation](../tools/quality-agent/README.md)
- [JSON Schema](../schemas/quality-report.schema.json)
- [GitHub Workflow](.github/workflows/quality-analysis.yml)
- [OpenAI API Documentation](https://platform.openai.com/docs/api-reference)
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)

## License

Same as FluentPDF project license.
