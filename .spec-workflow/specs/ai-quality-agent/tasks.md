# Tasks Document

## Implementation Tasks

- [x] 1. Create FluentPDF.QualityAgent CLI project and configure dependencies
  - Files: `tools/quality-agent/FluentPDF.QualityAgent.csproj`, `tools/quality-agent/Program.cs`
  - Create .NET 8 console application
  - Add dependencies: Azure.AI.OpenAI, System.CommandLine, FluentResults, Serilog
  - Configure CLI argument parsing (--trx-file, --log-dir, --visual-results, --output)
  - Purpose: Establish CLI infrastructure for quality agent
  - _Prompt: Role: CLI Application Developer | Task: Create console app tools/quality-agent with System.CommandLine for argument parsing. Configure arguments: --trx-file (path), --log-dir (path), --visual-results (path), --validation-results (path), --output (path). Implement basic Program.cs with help text, argument validation, exit codes (0=Pass, 1=Warn, 2=Fail). | Restrictions: Use System.CommandLine.DragonFruit or System.CommandLine, validate all input paths, show usage if args missing. | Success: CLI runs, shows help, validates arguments, exits with correct codes._

- [x] 2. Implement TRX parser for test results
  - Files: `tools/quality-agent/Parsers/TrxParser.cs`, `tools/quality-agent.Tests/Parsers/TrxParserTests.cs`
  - Parse xUnit/NUnit TRX XML files using System.Xml.Linq
  - Extract test name, outcome, error message, stack trace
  - Aggregate test statistics (total, passed, failed, skipped)
  - Write unit tests with sample TRX files
  - Purpose: Enable test result analysis
  - _Prompt: Role: C# Developer with XML parsing expertise | Task: Implement TrxParser parsing xUnit/NUnit TRX files. Use XDocument, extract test results from <UnitTestResult> elements, parse outcome, error message, stack trace. Return Result<TestResults> with total/passed/failed counts and list of failures. Write unit tests with sample TRX (passed tests, failed tests). | Restrictions: Handle malformed XML gracefully, validate namespace, parse both xUnit and NUnit formats. | Success: Parser extracts test results correctly, handles errors, unit tests pass with sample TRX files._

- [x] 3. Implement Serilog JSON log parser
  - Files: `tools/quality-agent/Parsers/LogParser.cs`, `tools/quality-agent.Tests/Parsers/LogParserTests.cs`
  - Parse Serilog JSON log files line-by-line
  - Extract timestamp, level, message, correlation ID, properties, exception
  - Group log entries by correlation ID
  - Write unit tests with sample log entries
  - Purpose: Enable log analysis and pattern detection
  - _Prompt: Role: C# Developer with log analysis expertise | Task: Implement LogParser reading Serilog JSON logs. Read file line-by-line, deserialize each JSON entry to LogEntry model, extract timestamp, level, message, correlation ID, structured properties, exception. Group entries by correlation ID for related operation tracking. Return Result<List<LogEntry>>. Write unit tests with sample log entries (info, warning, error, exception). | Restrictions: Handle large files efficiently (streaming), parse JSON robustly (handle missing fields), support multiple log levels. | Success: Parser reads logs correctly, groups by correlation ID, handles errors, unit tests pass._

- [x] 4. Implement SSIM results parser for visual regression analysis
  - Files: `tools/quality-agent/Parsers/SsimParser.cs`, `tools/quality-agent.Tests/Parsers/SsimParserTests.cs`
  - Parse SSIM results JSON from visual regression tests
  - Extract test name, SSIM score, baseline/current image paths
  - Identify regressions based on thresholds (< 0.99 minor, < 0.97 major, < 0.95 critical)
  - Write unit tests with sample SSIM results
  - Purpose: Enable visual regression analysis
  - _Prompt: Role: C# Developer with image analysis expertise | Task: Implement SsimParser parsing SSIM results JSON. Read JSON file, extract test name, SSIM score, baseline/current image paths. Classify regressions: < 0.99 minor, < 0.97 major, < 0.95 critical. Return Result<SsimResults> with list of tests and regression flags. Write unit tests with sample JSON (passing scores, minor regression, major regression). | Restrictions: Handle missing scores, validate score range (0.0-1.0), support batch results. | Success: Parser extracts SSIM scores, classifies regressions, unit tests verify thresholds._

- [x] 5. Implement log pattern analyzer for anomaly detection
  - Files: `tools/quality-agent/Analyzers/LogPatternAnalyzer.cs`, `tools/quality-agent.Tests/Analyzers/LogPatternAnalyzerTests.cs`
  - Detect error rate spikes (compare to baseline)
  - Identify repeated exceptions (group by stack trace hash)
  - Detect performance warnings (operations > threshold)
  - Identify missing correlation IDs
  - Write unit tests with mock log entries
  - Purpose: Automatically detect log patterns and anomalies
  - _Prompt: Role: Data Analyst with pattern recognition expertise | Task: Implement LogPatternAnalyzer detecting patterns in logs. Calculate error rate (errors per hour), compare to baseline (stored in config), flag spikes > 2x. Group exceptions by stack trace hash, flag repeated (> 5 occurrences). Detect performance warnings (duration > threshold). Find entries without correlation IDs. Return Result<LogPatterns> with detected patterns. Write unit tests with mock log entries (error spike, repeated exception, slow operation). | Restrictions: Handle baseline calculation (use moving average if no baseline), parameterize thresholds, avoid false positives. | Success: Analyzer detects all pattern types, unit tests verify detection logic._

- [x] 6. Integrate OpenAI/Azure OpenAI for test failure analysis
  - Files: `tools/quality-agent/Analyzers/TestFailureAnalyzer.cs`, `tools/quality-agent/Config/OpenAiConfig.cs`
  - Integrate Azure.AI.OpenAI SDK
  - Configure OpenAI API and Azure OpenAI support
  - Implement structured outputs with JSON schema for root cause hypothesis
  - Add retry logic with exponential backoff
  - Fallback to rule-based analysis if API fails
  - Purpose: Enable AI-powered root cause analysis
  - _Prompt: Role: AI Integration Developer | Task: Implement TestFailureAnalyzer using Azure.AI.OpenAI. Configure with API key (env var OPENAI_API_KEY) and deployment. Create system prompt: "You are a quality analyst...". Send test failure (name, error, stack trace) + related logs (same correlation ID). Use structured outputs with JSON schema defining RootCauseHypothesis (issue, hypothesis, confidence, severity, actions). Implement retry with exponential backoff (3 attempts). Fallback to rule-based if fails. | Restrictions: Never log API keys, sanitize data before sending to LLM, limit prompt size (< 4000 tokens), handle rate limits. | Success: Analyzer calls OpenAI successfully, returns structured hypothesis, retries on transient errors, falls back on failure._

- [ ] 7. Implement visual regression analyzer
  - Files: `tools/quality-agent/Analyzers/VisualRegressionAnalyzer.cs`, `tools/quality-agent.Tests/Analyzers/VisualRegressionAnalyzerTests.cs`
  - Analyze SSIM scores using thresholds
  - Classify regression severity (Minor/Major/Critical)
  - Track SSIM trends over time (last 10 runs)
  - Identify degradation trends (consistent decrease)
  - Write unit tests with mock SSIM data
  - Purpose: Provide visual regression analysis and trending
  - _Prompt: Role: QA Engineer with visual testing expertise | Task: Implement VisualRegressionAnalyzer analyzing SSIM scores. For each test, classify severity: < 0.99 Minor, < 0.97 Major, < 0.95 Critical. Track SSIM history (store last 10 runs in JSON file), detect degradation trends (3+ consecutive decreases). Return Result<VisualAnalysis> with regressions and trends. Write unit tests with mock SSIM data (stable scores, minor regression, degradation trend). | Restrictions: Handle missing history (treat as first run), parameterize thresholds, avoid false positives from minor fluctuations. | Success: Analyzer classifies regressions correctly, detects trends, unit tests verify logic._

- [ ] 8. Implement quality report generator with JSON Schema
  - Files: `tools/quality-agent/Reporting/QualityReportGenerator.cs`, `schemas/quality-report.schema.json`, `tools/quality-agent.Tests/Reporting/ReportGeneratorTests.cs`
  - Define JSON Schema for quality reports
  - Generate QualityReport from analysis results
  - Calculate overall score (weighted: tests 40%, logs 30%, visual 20%, validation 10%)
  - Determine status (Pass ≥ 80, Warn 60-79, Fail < 60)
  - Validate report against JSON Schema
  - Write unit tests verifying report generation and scoring
  - Purpose: Generate standardized quality reports
  - _Prompt: Role: Software Engineer with reporting and JSON Schema expertise | Task: Implement QualityReportGenerator creating reports from analysis results. Define schemas/quality-report.schema.json with structure: summary, overallScore, status, buildInfo, analysis, rootCauseHypotheses, recommendations. Calculate score: (testPassRate * 0.4) + (logHealthScore * 0.3) + (visualScore * 0.2) + (validationScore * 0.1). Determine status from score. Validate report with JsonSchema.Net. Write unit tests verifying score calculation and status determination. | Restrictions: Score must be 0-100, all fields required in schema, report must be human-readable (formatted JSON). | Success: Generator creates valid reports, score calculation correct, status determined accurately, JSON Schema validation passes._

- [ ] 9. Implement AiQualityAgent orchestration service
  - Files: `tools/quality-agent/Services/IAiQualityAgent.cs`, `tools/quality-agent/Services/AiQualityAgent.cs`, `tools/quality-agent.Tests/Services/AiQualityAgentTests.cs`
  - Create service interface with AnalyzeAsync method
  - Orchestrate parsers in parallel (TRX, logs, SSIM, validation)
  - Run analyzers (test failure, log pattern, visual regression)
  - Generate quality report
  - Write integration tests with real inputs
  - Purpose: Provide unified quality analysis API
  - _Prompt: Role: Backend Service Developer | Task: Implement AiQualityAgent orchestrating quality analysis. Create IAiQualityAgent interface with AnalyzeAsync(AnalysisInput). Implement service running parsers in parallel (Task.WhenAll), then analyzers, then report generation. Log all operations with correlation ID. Return Result<QualityReport>. Write integration tests with sample TRX, logs, SSIM results, verify end-to-end analysis. | Restrictions: Run parsers in parallel for performance, handle individual failures gracefully, log detailed progress, validate inputs before processing. | Success: Service orchestrates all components correctly, runs in parallel, generates report, integration tests pass end-to-end._

- [ ] 10. Create CI workflow for quality analysis
  - Files: `.github/workflows/quality-analysis.yml`
  - Create workflow running after test completion
  - Collect TRX files, Serilog logs, SSIM results, validation reports
  - Run quality agent CLI tool
  - Upload quality report as artifact
  - Post PR comment with quality summary
  - Fail build if status is "Fail"
  - Purpose: Integrate quality analysis into CI pipeline
  - _Prompt: Role: DevOps Engineer | Task: Create .github/workflows/quality-analysis.yml running on PR and main push. Steps: checkout, setup .NET, download test/validation artifacts from previous jobs, run quality agent CLI (dotnet run --project tools/quality-agent -- --trx-file ... --log-dir ... --visual-results ... --output quality-report.json), upload quality-report.json as artifact, parse report JSON and post PR comment with summary (overall score, status, top issues), fail workflow if status = "Fail". Configure OpenAI API key from secrets. | Restrictions: Run after all tests complete, handle missing artifacts gracefully, ensure API key is secure (use secrets), include clear error messages. | Success: Workflow runs after tests, quality agent executes, report uploaded, PR comment posted, build fails appropriately._

- [ ] 11. Documentation and final validation
  - Files: `tools/quality-agent/README.md`, `docs/AI-QUALITY-AGENT.md`, `README.md` (update)
  - Document CLI usage and arguments
  - Document quality report structure and interpretation
  - Document OpenAI/Azure OpenAI configuration
  - Document CI integration
  - Validate end-to-end quality analysis workflow
  - Purpose: Ensure quality agent is documented and operational
  - _Prompt: Role: Technical Writer | Task: Create comprehensive documentation for AI Quality Agent. Write tools/quality-agent/README.md: CLI usage, argument descriptions, examples, exit codes. Create docs/AI-QUALITY-AGENT.md: architecture overview, parsers/analyzers descriptions, OpenAI integration, scoring methodology, CI integration. Update main README.md with quality agent capabilities. Verify end-to-end: run CLI locally, check report, run in CI, verify PR comment. | Restrictions: Documentation must be clear for new users, include examples, verify all examples work, document troubleshooting. | Success: README documents CLI usage, AI-QUALITY-AGENT.md comprehensive, main README updated, end-to-end verification complete._

## Summary

This spec implements AI-powered quality analysis:
- TRX parser for test result analysis
- Serilog JSON log parser with pattern detection
- SSIM visual regression analysis
- OpenAI/Azure OpenAI integration for root cause hypothesis
- Quality report generation with JSON Schema validation
- CLI tool for local and CI execution
- CI integration with automated analysis and PR comments

**Next steps after completion:**
- Monitor quality trends across builds
- Refine AI prompts based on hypothesis accuracy
- Expand pattern detection rules
- Integrate with quality dashboard
