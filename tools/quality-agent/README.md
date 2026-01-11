# FluentPDF AI Quality Agent

AI-powered quality analysis tool for automated testing, log analysis, and visual regression detection.

## Overview

The AI Quality Agent is a .NET 8 CLI tool that provides comprehensive quality analysis for FluentPDF builds. It combines multiple data sources (test results, logs, visual regression tests, validation reports) and uses AI to generate actionable insights and recommendations.

## Features

- **Test Result Analysis**: Parses TRX files to extract test outcomes, failures, and patterns
- **Log Pattern Detection**: Analyzes Serilog JSON logs for error spikes, repeated exceptions, and anomalies
- **Visual Regression Analysis**: Evaluates SSIM scores to detect UI regressions and degradation trends
- **AI-Powered Root Cause Analysis**: Uses OpenAI/Azure OpenAI to generate hypothesis for test failures
- **Quality Scoring**: Calculates overall quality score (0-100) with weighted metrics
- **Automated Recommendations**: Provides actionable recommendations based on analysis results
- **CI/CD Integration**: Designed for GitHub Actions with PR comments and build gates

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- (Optional) OpenAI API key for AI-powered analysis

### Build

```bash
cd tools/quality-agent
dotnet build
```

### Run

```bash
dotnet run --project tools/quality-agent -- [options]
```

Or build and run the executable directly:

```bash
dotnet build -c Release
./bin/Release/net8.0/FluentPDF.QualityAgent [options]
```

## CLI Usage

### Basic Syntax

```bash
FluentPDF.QualityAgent [options]
```

### Options

| Option | Alias | Description | Required |
|--------|-------|-------------|----------|
| `--trx-file` | `-t` | Path to TRX test results file | No |
| `--log-dir` | `-l` | Path to directory containing Serilog JSON logs | No |
| `--visual-results` | `-v` | Path to SSIM visual regression results JSON file | No |
| `--validation-results` | `-r` | Path to validation results JSON file | No |
| `--output` | `-o` | Path to output quality report JSON file (default: quality-report.json) | No |

**Note**: At least one input option (--trx-file, --log-dir, --visual-results, or --validation-results) must be provided.

### Examples

#### Analyze test results only

```bash
dotnet run --project tools/quality-agent -- \
  --trx-file ./TestResults/test-results.trx \
  --output quality-report.json
```

#### Analyze all available data sources

```bash
dotnet run --project tools/quality-agent -- \
  --trx-file ./TestResults/core-tests.trx \
  --log-dir ./logs \
  --visual-results ./visual-results/ssim-results.json \
  --validation-results ./validation-reports/validation-report.json \
  --output quality-report.json
```

#### CI/CD Usage (GitHub Actions)

```yaml
- name: Run AI Quality Analysis
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  run: |
    dotnet run --project tools/quality-agent -- \
      --trx-file test-results/core-tests.trx \
      --output quality-report.json
```

## Exit Codes

The CLI returns different exit codes based on the quality analysis result:

| Exit Code | Status | Meaning |
|-----------|--------|---------|
| 0 | Pass | Quality score ≥ 80 - Build is healthy |
| 1 | Warn | Quality score 60-79 - Build has warnings but can proceed |
| 2 | Fail | Quality score < 60 or analysis error - Build should fail |

## Quality Report Structure

The tool generates a JSON report with the following structure:

```json
{
  "summary": {
    "timestamp": "2026-01-11T00:00:00Z",
    "buildId": "12345",
    "totalIssues": 5,
    "criticalIssues": 1
  },
  "overallScore": 75.5,
  "status": "Warn",
  "buildInfo": {
    "buildId": "12345",
    "timestamp": "2026-01-11T00:00:00Z",
    "branch": "main",
    "commit": "abc123",
    "author": "developer"
  },
  "analysis": {
    "testAnalysis": {
      "totalTests": 100,
      "passedTests": 95,
      "failedTests": 5,
      "passRate": 95.0
    },
    "logAnalysis": {
      "errorCount": 3,
      "warningCount": 10,
      "errorRate": 0.5,
      "hasErrorSpike": false
    },
    "visualAnalysis": {
      "totalTests": 20,
      "criticalRegressions": 0,
      "majorRegressions": 1,
      "minorRegressions": 2
    },
    "validationAnalysis": null
  },
  "rootCauseHypotheses": [
    {
      "testName": "TestFailure1",
      "issue": "Description of issue",
      "hypothesis": "AI-generated hypothesis",
      "confidence": "High",
      "severity": "Critical",
      "suggestedActions": [
        "Action 1",
        "Action 2"
      ]
    }
  ],
  "recommendations": [
    {
      "title": "Fix failing tests",
      "description": "5 tests are failing",
      "severity": "High",
      "category": "Testing"
    }
  ]
}
```

## Quality Scoring

The overall quality score (0-100) is calculated using weighted metrics:

- **Tests (40%)**: Test pass rate
- **Logs (30%)**: Log health score (based on error rates and patterns)
- **Visual (20%)**: Visual regression score
- **Validation (10%)**: PDF validation score

### Status Thresholds

- **Pass**: Score ≥ 80
- **Warn**: Score 60-79
- **Fail**: Score < 60

## AI Configuration

### OpenAI API Key

Set the `OPENAI_API_KEY` environment variable:

```bash
export OPENAI_API_KEY="sk-..."
```

Or on Windows:

```powershell
$env:OPENAI_API_KEY="sk-..."
```

### Azure OpenAI

To use Azure OpenAI instead:

1. Set environment variables:
   ```bash
   export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
   export AZURE_OPENAI_API_KEY="your-api-key"
   export AZURE_OPENAI_DEPLOYMENT="your-deployment-name"
   ```

2. The tool will automatically detect and use Azure OpenAI when these variables are set.

### Fallback Behavior

If no API key is provided, the tool will:
- Log a warning
- Use rule-based analysis instead of AI
- Still generate quality reports with reduced insights

## Log Analysis

### Supported Log Format

The tool expects Serilog JSON logs with the following structure:

```json
{
  "@t": "2026-01-11T00:00:00.000Z",
  "@l": "Error",
  "@m": "Error message",
  "@x": "Exception details",
  "CorrelationId": "guid",
  "CustomProperty": "value"
}
```

### Detected Patterns

- **Error Spikes**: Error rate > 2x baseline
- **Repeated Exceptions**: Same exception occurring > 5 times
- **Performance Issues**: Operations exceeding thresholds
- **Missing Correlation IDs**: Log entries without correlation tracking

## Visual Regression Analysis

### SSIM Results Format

```json
{
  "tests": [
    {
      "testName": "Component1_Render",
      "ssimScore": 0.98,
      "baselinePath": "./baselines/component1.png",
      "currentPath": "./current/component1.png"
    }
  ]
}
```

### Severity Classification

- **Critical**: SSIM < 0.95
- **Major**: SSIM < 0.97
- **Minor**: SSIM < 0.99
- **Pass**: SSIM ≥ 0.99

## Troubleshooting

### Issue: "Quality report JSON schema not found"

**Solution**: Ensure `schemas/quality-report.schema.json` exists in the repository root.

### Issue: "OPENAI_API_KEY environment variable not set"

**Solution**: Set the environment variable or accept rule-based analysis:

```bash
export OPENAI_API_KEY="your-api-key"
```

### Issue: "No test results found"

**Solution**: Verify the TRX file path and ensure tests have run:

```bash
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### Issue: CLI exits with code 2

**Possible causes**:
- Quality score < 60 (expected behavior for failing builds)
- Missing required input files
- Invalid file paths
- Analysis error (check console output for details)

## Development

### Project Structure

```
tools/quality-agent/
├── Analyzers/          # Pattern detection and analysis logic
├── Config/             # Configuration models
├── Models/             # Data models for reports and results
├── Parsers/            # TRX, log, SSIM, validation parsers
├── Reporting/          # Quality report generation
├── Services/           # AiQualityAgent orchestration service
└── Program.cs          # CLI entry point
```

### Running Tests

```bash
dotnet test tools/quality-agent.Tests
```

### Adding New Analyzers

1. Create analyzer class in `Analyzers/`
2. Implement analysis logic
3. Add to `AiQualityAgent.cs` orchestration
4. Update scoring in `QualityReportGenerator.cs`

## CI/CD Integration

The quality agent is integrated into the GitHub Actions workflow:

1. **Test Workflow**: Runs tests and uploads TRX files
2. **Quality Analysis Workflow**: Downloads artifacts, runs quality agent, posts PR comments
3. **Build Gate**: Fails if quality status is "Fail"

See `.github/workflows/quality-analysis.yml` for complete integration details.

## See Also

- [AI Quality Agent Architecture](../../docs/AI-QUALITY-AGENT.md)
- [FluentPDF Documentation](../../README.md)
- [JSON Schema](../../schemas/quality-report.schema.json)

## License

Same as FluentPDF project license.
