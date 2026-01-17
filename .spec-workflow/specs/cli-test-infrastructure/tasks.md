# Tasks Document

- [ ] 1. Comply with tasks-template structure
  - Purpose: Ensure tasks document follows spec-workflow template standards
  - Verify all required fields present (_Leverage, _Requirements, _Prompt)
  - _Requirements: N/A (meta-task)_
  - _Prompt: Role: Spec Workflow Compliance Officer | Task: Verify this tasks.md file complies with .spec-workflow/templates/tasks-template.md structure | Restrictions: Do not modify task content, only verify compliance | Success: All tasks follow template structure, all required fields present_

- [x] 2. Create ICliTest interface in FluentPDF.App/Testing/ICliTest.cs
  - File: src/FluentPDF.App/Testing/ICliTest.cs
  - Define ICliTest interface with Name, Description, RunAsync, VerifyAsync
  - Add XML documentation for all members
  - Purpose: Establish contract for all CLI tests
  - _Leverage: N/A (new interface)_
  - _Requirements: 1_
  - _Prompt: Role: .NET Interface Design Specialist | Task: Create ICliTest interface following requirement 1 design specifications | Restrictions: Keep interface minimal and focused, use async patterns, ensure testability | Success: Interface is well-defined, XML documented, supports all test scenarios_

- [x] 3. Create CliTestResult and TestSuiteResult models in FluentPDF.App/Testing/Models.cs
  - File: src/FluentPDF.App/Testing/Models.cs
  - Define CliTestResult, TestSuiteResult, IVerificationRule, CliTestContext classes
  - Add XML documentation
  - Purpose: Establish data structures for test execution
  - _Leverage: FluentResults patterns_
  - _Requirements: All_
  - _Prompt: Role: .NET Data Modeling Specialist | Task: Create comprehensive test execution data models following design specifications | Restrictions: Use record types where immutable, ensure serializable for reporting, include metadata for debugging | Success: All models defined, support all test scenarios, well-documented_

- [x] 4. Create CliTestContext in FluentPDF.App/Testing/CliTestContext.cs
  - File: src/FluentPDF.App/Testing/CliTestContext.cs
  - Implement context with WorkingDirectory, Services, Logger, Data properties
  - Add automatic cleanup on dispose
  - Purpose: Provide isolated test execution environment
  - _Leverage: IServiceProvider, Serilog, System.IO_
  - _Requirements: 4_
  - _Prompt: Role: Test Infrastructure Developer | Task: Implement CliTestContext providing isolated environment for test execution from requirement 4 | Restrictions: Must implement IDisposable, ensure cleanup always runs, create unique temp directories per test | Success: Context provides full isolation, cleanup is reliable, services accessible_

- [x] 5. Create TestDiscovery in FluentPDF.App/Testing/TestDiscovery.cs
  - File: src/FluentPDF.App/Testing/TestDiscovery.cs
  - Implement reflection-based test discovery
  - Find all ICliTest implementations in assembly
  - Cache discovered tests
  - Purpose: Automatically discover available CLI tests
  - _Leverage: System.Reflection, ICliTest interface_
  - _Requirements: 3_
  - _Prompt: Role: Reflection and Dependency Injection Specialist | Task: Implement TestDiscovery using reflection to find all ICliTest implementations from requirement 3 | Restrictions: Must handle assembly loading safely, cache results for performance, log discovery process | Success: Discovers all test implementations, discovery is fast, handles errors gracefully_

- [x] 6. Create TestExecutor in FluentPDF.App/Testing/TestExecutor.cs
  - File: src/FluentPDF.App/Testing/TestExecutor.cs
  - Implement test execution with proper isolation
  - Create context, run test, capture result
  - Handle exceptions and timeouts
  - Purpose: Execute tests with proper isolation and error handling
  - _Leverage: CliTestContext, Serilog_
  - _Requirements: 1, 4_
  - _Prompt: Role: Test Execution Framework Developer | Task: Implement TestExecutor providing isolated test execution from requirements 1 and 4 | Restrictions: Must isolate each test, timeout long-running tests (30s), capture all exceptions, log execution details | Success: Tests run in isolation, timeouts work, exceptions handled, results captured correctly_

- [x] 7. Create verification rules in FluentPDF.App/Testing/Verification/
  - Files: src/FluentPDF.App/Testing/Verification/FileExistsRule.cs, ExitCodeRule.cs, LogContainsRule.cs
  - Implement IVerificationRule for common verification scenarios
  - Support file existence, exit code checking, log content verification
  - Purpose: Provide reusable verification logic
  - _Leverage: IVerificationRule interface_
  - _Requirements: 2_
  - _Prompt: Role: Test Verification Specialist | Task: Implement common verification rules (file exists, exit code, log content) from requirement 2 | Restrictions: Each rule should be focused and reusable, provide clear failure messages, support async verification | Success: Rules cover common scenarios, failures have clear diagnostics, rules are composable_

- [x] 8. Create ResultVerifier in FluentPDF.App/Testing/ResultVerifier.cs
  - File: src/FluentPDF.App/Testing/ResultVerifier.cs
  - Implement result verification orchestration
  - Apply verification rules to test results
  - Generate verification reports
  - Purpose: Verify test results match expected outcomes
  - _Leverage: IVerificationRule implementations_
  - _Requirements: 2_
  - _Prompt: Role: Test Result Verification Engineer | Task: Implement ResultVerifier orchestrating verification rules from requirement 2 | Restrictions: Must support multiple rules per test, aggregate results correctly, generate actionable reports | Success: Verification runs all rules, results aggregated, reports are clear and actionable_

- [x] 9. Create TestRunner in FluentPDF.App/Testing/TestRunner.cs
  - File: src/FluentPDF.App/Testing/TestRunner.cs
  - Orchestrate test discovery, execution, verification, reporting
  - Implement RunTestAsync and RunAllTestsAsync
  - Return TestSuiteResult
  - Purpose: Provide unified test execution interface
  - _Leverage: TestDiscovery, TestExecutor, ResultVerifier_
  - _Requirements: All_
  - _Prompt: Role: Test Framework Architect | Task: Implement TestRunner orchestrating all test components covering all requirements | Restrictions: Must handle component failures gracefully, provide progress feedback, support cancellation | Success: All components integrated, tests run end-to-end, results reported correctly_

- [x] 10. Create RenderCliTest in FluentPDF.App/Testing/Tests/RenderCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/RenderCliTest.cs
  - Implement ICliTest for PDF rendering verification
  - Test rendering all pages to images
  - Verify output files exist and have correct sizes
  - Purpose: Provide example CLI test implementation
  - _Leverage: IPdfRenderingService, ICliTest, verification rules_
  - _Requirements: All_
  - _Prompt: Role: Integration Test Developer | Task: Implement RenderCliTest as reference implementation of ICliTest for PDF rendering | Restrictions: Must use actual services, verify files created, validate image dimensions, cleanup temp files | Success: Test renders PDFs correctly, verification works, serves as good example for other tests_

- [x] 11. Add CLI commands --list-tests, --run-test, --run-all-tests
  - Files: src/FluentPDF.App/CommandLineOptions.cs, src/FluentPDF.App/Services/DiagnosticCommandHandler.cs
  - Add properties for test commands
  - Implement HandleListTestsAsync, HandleRunTestAsync, HandleRunAllTestsAsync
  - Return appropriate exit codes
  - Purpose: Enable CLI test execution
  - _Leverage: TestRunner, existing CLI infrastructure_
  - _Requirements: 1, 3_
  - _Prompt: Role: CLI Developer with Testing Integration Experience | Task: Add CLI test commands following requirements 1 and 3, integrating TestRunner | Restrictions: Follow existing CLI patterns, return 0 for pass, non-zero for fail, support --verbose for detailed output | Success: Commands work from CLI, exit codes correct, output is clear, integrates with CI/CD_

- [ ] 12. Create test framework unit tests in FluentPDF.App.Tests/Testing/
  - Files: tests/FluentPDF.App.Tests/Testing/TestDiscoveryTests.cs, TestExecutorTests.cs, TestRunnerTests.cs
  - Test each framework component
  - Verify error handling
  - Test with mock implementations
  - Purpose: Ensure test framework reliability
  - _Leverage: xUnit, NSubstitute for mocking_
  - _Requirements: All_
  - _Prompt: Role: QA Engineer with Framework Testing Expertise | Task: Create comprehensive unit tests for test framework components covering all requirements | Restrictions: Mock external dependencies, test both success and failure paths, ensure tests are fast and deterministic | Success: All framework components tested, edge cases covered, tests run quickly_

- [ ] 13. Create test framework documentation in docs/cli-testing.md
  - File: docs/cli-testing.md
  - Document test framework architecture
  - Provide examples of creating new tests
  - Explain verification rules
  - Purpose: Enable developers to create CLI tests
  - _Leverage: Existing docs structure_
  - _Requirements: All_
  - _Prompt: Role: Technical Writer with Testing Expertise | Task: Create comprehensive CLI testing documentation covering all requirements with examples | Restrictions: Include code examples, explain concepts clearly, provide troubleshooting guide | Success: Docs are clear and complete, examples work, developers can create tests after reading_

- [ ] 14. Update README.md with CLI testing section
  - File: README.md
  - Add CLI testing overview
  - Show example commands
  - Link to detailed docs
  - Purpose: Make CLI testing discoverable
  - _Leverage: Existing README structure_
  - _Requirements: 3_
  - _Prompt: Role: Developer Advocate | Task: Add CLI testing section to README from requirement 3 with clear examples | Restrictions: Keep examples concise, use common scenarios, maintain README quality | Success: CLI testing documented in README, examples clear, links to full docs_
