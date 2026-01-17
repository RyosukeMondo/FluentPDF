# Tasks Document

- [x] 1. Comply with tasks-template structure
  - Purpose: Ensure tasks document follows spec-workflow template standards
  - Verify all required fields present (_Leverage, _Requirements, _Prompt)
  - Validate task numbering and hierarchy
  - _Requirements: N/A (meta-task)_
  - _Prompt: Role: Spec Workflow Compliance Officer | Task: Verify this tasks.md file complies with .spec-workflow/templates/tasks-template.md structure, ensuring all tasks have _Leverage, _Requirements, and _Prompt fields with proper formatting | Restrictions: Do not modify task content, only verify compliance | Success: All tasks follow template structure, all required fields present, proper markdown formatting_

- [x] 2. Create VerificationResult data models in FluentPDF.Rendering/Interop/Verification/Models.cs
  - File: src/FluentPDF.Rendering/Interop/Verification/Models.cs
  - Define VerificationResult, SignatureDetails, MarshallingTestResult, CoverageReport classes
  - Add XML documentation for all public members
  - Purpose: Establish data structures for verification results
  - _Leverage: N/A (new models)_
  - _Requirements: All requirements (data foundation)_
  - _Prompt: Role: .NET Data Modeling Specialist | Task: Create comprehensive data models for P/Invoke verification results following the design document specifications | Restrictions: Use record types where immutable data is appropriate, ensure all properties are nullable where verification may fail, include XML docs for IntelliSense | Success: All models defined with proper types, XML documentation complete, models support all verification scenarios_

- [x] 3. Create SignatureAnalyzer in FluentPDF.Rendering/Interop/Verification/SignatureAnalyzer.cs
  - File: src/FluentPDF.Rendering/Interop/Verification/SignatureAnalyzer.cs
  - Implement reflection-based signature analysis
  - Compare DllImport attributes against expected signatures
  - Validate return types, parameter types, calling conventions
  - Purpose: Verify P/Invoke signatures match PDFium API
  - _Leverage: PdfiumInterop.cs for function list_
  - _Requirements: 1_
  - _Prompt: Role: .NET Reflection and P/Invoke Expert | Task: Implement SignatureAnalyzer that uses reflection to analyze all DllImport methods in PdfiumInterop.cs and validates signatures against PDFium API specifications from requirement 1 | Restrictions: Must handle all DllImport attribute variations (CharSet, CallingConvention, EntryPoint), do not execute any P/Invoke functions during analysis, maintain thread safety | Success: Analyzer correctly identifies all DllImport methods, validates all signature components, detects mismatches with detailed error messages_

- [x] 4. Create PDFium API specification data in FluentPDF.Rendering/Interop/Verification/PdfiumApiSpec.cs
  - File: src/FluentPDF.Rendering/Interop/Verification/PdfiumApiSpec.cs
  - Define expected signatures for critical PDFium functions
  - Include return types, parameter types, calling conventions
  - Add comments with PDFium documentation links
  - Purpose: Provide ground truth for signature verification
  - _Leverage: PDFium official documentation_
  - _Requirements: 1_
  - _Prompt: Role: API Documentation Specialist with C/C++ and P/Invoke expertise | Task: Create comprehensive PDFium API specification data for all functions used in PdfiumInterop.cs based on official PDFium documentation and requirement 1 | Restrictions: Verify all specifications against PDFium headers (fpdf*.h files), document source of each specification, prioritize functions that caused issues (e.g., FPDF_GetPageWidth vs FPDF_GetPageWidthF) | Success: All critical functions documented, specifications match PDFium headers exactly, sources cited for verification_

- [x] 5. Create DataMarshallerTester in FluentPDF.Rendering/Interop/Verification/DataMarshallerTester.cs
  - File: src/FluentPDF.Rendering/Interop/Verification/DataMarshallerTester.cs
  - Implement test harness for marshalling verification
  - Create test cases with known input/output data
  - Test integer marshalling, double marshalling, string marshalling, handle marshalling
  - Purpose: Verify data marshals correctly at runtime
  - _Leverage: PdfiumInterop.cs, existing test fixtures_
  - _Requirements: 2_
  - _Prompt: Role: .NET Marshalling and Interop Testing Specialist | Task: Implement DataMarshallerTester that tests all P/Invoke functions with known test data to verify marshalling correctness from requirement 2, including test cases for the FPDF_GetPageWidth vs FPDF_GetPageWidthF issue | Restrictions: Must initialize PDFium library properly, use actual test PDF files from tests/Fixtures, handle SafeHandle types correctly, clean up all resources | Success: All marshalling types tested (int, double, IntPtr, SafeHandle, strings), known issues like float API detected, all tests deterministic and repeatable_

- [x] 6. Create CoverageReporter in FluentPDF.Rendering/Interop/Verification/CoverageReporter.cs
  - File: src/FluentPDF.Rendering/Interop/Verification/CoverageReporter.cs
  - Implement report generation logic
  - Format results as markdown table
  - Highlight critical gaps in coverage
  - Purpose: Generate human-readable verification reports
  - _Leverage: Models.cs for data structures_
  - _Requirements: 4_
  - _Prompt: Role: Technical Writer and .NET Developer | Task: Implement CoverageReporter that generates comprehensive markdown reports showing marshalling verification coverage from requirement 4 | Restrictions: Report must be readable by both humans and CI/CD tools, include summary statistics at top, sort results by importance (failed first, then untested, then passed), support both console and file output | Success: Report is clear and actionable, highlights critical issues prominently, includes specific recommendations for fixes_

- [x] 7. Create MarshallingVerifier orchestrator in FluentPDF.Rendering/Interop/Verification/MarshallingVerifier.cs
  - File: src/FluentPDF.Rendering/Interop/Verification/MarshallingVerifier.cs
  - Orchestrate signature analysis, marshalling testing, reporting
  - Implement VerifyAllSignatures(), TestMarshalling(), GenerateReport()
  - Return combined results
  - Purpose: Provide unified verification interface
  - _Leverage: SignatureAnalyzer.cs, DataMarshallerTester.cs, CoverageReporter.cs_
  - _Requirements: All_
  - _Prompt: Role: Software Architect and Orchestration Specialist | Task: Create MarshallingVerifier orchestrator that coordinates all verification components (SignatureAnalyzer, DataMarshallerTester, CoverageReporter) to provide unified verification interface covering all requirements | Restrictions: Must handle component failures gracefully, provide progress feedback for long operations, support cancellation tokens for async operations | Success: All components integrated smoothly, verification runs end-to-end, results aggregated correctly, error handling robust_

- [x] 8. Add CLI command --verify-marshalling in CommandLineOptions.cs and DiagnosticCommandHandler.cs
  - Files: src/FluentPDF.App/CommandLineOptions.cs, src/FluentPDF.App/Services/DiagnosticCommandHandler.cs
  - Add VerifyMarshalling property to CommandLineOptions
  - Implement HandleVerifyMarshallingAsync in DiagnosticCommandHandler
  - Return appropriate exit codes (0=success, 1=failure)
  - Purpose: Enable CI/CD marshalling verification
  - _Leverage: Existing CLI command infrastructure, MarshallingVerifier.cs_
  - _Requirements: 3_
  - _Prompt: Role: CLI Developer with .NET and CI/CD expertise | Task: Add --verify-marshalling CLI command following requirement 3, integrating MarshallingVerifier and returning proper exit codes for automation | Restrictions: Must follow existing CLI command patterns (see --test-thumbnails), output must be machine-parseable (JSON) and human-readable (console), must work without GUI | Success: Command runs successfully in headless mode, exit codes correct for CI/CD, output includes both summary and detailed results, command documented in --help_

- [ ] 9. Add CLI command --marshalling-report in CommandLineOptions.cs and DiagnosticCommandHandler.cs
  - Files: src/FluentPDF.App/CommandLineOptions.cs, src/FluentPDF.App/Services/DiagnosticCommandHandler.cs
  - Add MarshallingReport property and OutputPath property
  - Implement HandleMarshallingReportAsync in DiagnosticCommandHandler
  - Save report to file if --output specified
  - Purpose: Generate marshalling coverage reports
  - _Leverage: Existing CLI infrastructure, CoverageReporter.cs_
  - _Requirements: 4_
  - _Prompt: Role: Reporting and CLI Integration Specialist | Task: Add --marshalling-report CLI command following requirement 4, using CoverageReporter to generate and save coverage reports | Restrictions: Must support both console output and file output (via --output flag), default to markdown format, include timestamp in report | Success: Report generated successfully, saved to file when requested, console output formatted correctly, report includes all coverage metrics_

- [ ] 10. Create marshalling verification unit tests in FluentPDF.Rendering.Tests/Interop/MarshallingVerificationTests.cs
  - File: tests/FluentPDF.Rendering.Tests/Interop/MarshallingVerificationTests.cs
  - Test SignatureAnalyzer with correct and incorrect signatures
  - Test DataMarshallerTester with known test data
  - Test CoverageReporter output format
  - Purpose: Ensure verification logic is correct
  - _Leverage: xUnit framework, existing test infrastructure_
  - _Requirements: All_
  - _Prompt: Role: Quality Assurance Engineer with .NET Testing Expertise | Task: Create comprehensive unit tests for all marshalling verification components covering all requirements, including tests for the FPDF_GetPageWidthF issue that was discovered | Restrictions: Must mock PDFium where appropriate, use deterministic test data, test both success and failure scenarios, ensure tests run in isolation | Success: All verification components tested, edge cases covered, tests detect known issues (like float API problem), all tests pass consistently_

- [ ] 11. Add MSBuild pre-build verification target in Directory.Build.targets
  - File: Directory.Build.targets (create or modify)
  - Add MSBuild target that runs marshalling verification before build
  - Execute MarshallingVerifier via console app
  - Fail build if verification fails
  - Purpose: Catch marshalling errors at build time
  - _Leverage: MSBuild infrastructure, CLI verification command_
  - _Requirements: 1_
  - _Prompt: Role: Build Systems Engineer with MSBuild expertise | Task: Create MSBuild target that runs marshalling verification before every build following requirement 1, failing the build if verification fails | Restrictions: Must only run for FluentPDF.App project, must not slow down incremental builds significantly, must provide clear error messages on failure, allow opt-out via MSBuild property for local development | Success: Verification runs automatically on build, build fails on marshalling errors with clear diagnostics, incremental builds remain fast, developers can disable for rapid iteration_

- [ ] 12. Add CI/CD pipeline integration in .github/workflows/build.yml
  - File: .github/workflows/build.yml (modify if exists)
  - Add marshalling verification step before tests
  - Save coverage report as artifact
  - Fail build if verification fails
  - Purpose: Ensure marshalling correctness in CI/CD
  - _Leverage: CLI verification commands, GitHub Actions_
  - _Requirements: 3, 4_
  - _Prompt: Role: DevOps Engineer with GitHub Actions expertise | Task: Integrate marshalling verification into CI/CD pipeline following requirements 3 and 4, running verification before tests and saving coverage reports | Restrictions: Must run on all pull requests and main branch commits, must cache PDFium library for faster builds, must upload report artifacts for failed builds | Success: Verification runs in CI/CD, coverage reports uploaded as artifacts, builds fail early on marshalling errors, pipeline execution time remains reasonable_

- [ ] 13. Create marshalling verification documentation in docs/marshalling-verification.md
  - File: docs/marshalling-verification.md
  - Document verification system architecture
  - Provide CLI usage examples
  - Explain how to fix common marshalling errors
  - Purpose: Enable developers to use and maintain verification system
  - _Leverage: Existing documentation structure_
  - _Requirements: All_
  - _Prompt: Role: Technical Writer with .NET P/Invoke expertise | Task: Create comprehensive documentation for marshalling verification system covering all requirements, including troubleshooting guide for common issues | Restrictions: Use clear examples, include screenshots of CLI output, provide step-by-step fix procedures, link to PDFium documentation | Success: Documentation is clear and comprehensive, covers all CLI commands, troubleshooting guide addresses common errors (like float API issue), includes examples of fixing marshalling errors_

- [ ] 14. Add verification examples to README.md
  - File: README.md (modify)
  - Add section on marshalling verification
  - Show CLI command examples
  - Link to detailed documentation
  - Purpose: Make verification discoverable to new developers
  - _Leverage: Existing README structure_
  - _Requirements: 3, 4_
  - _Prompt: Role: Developer Advocate and Technical Writer | Task: Add marshalling verification section to README following requirements 3 and 4, with clear examples and links to full documentation | Restrictions: Keep examples concise, use most common use cases, maintain README readability, follow existing README format | Success: Verification system documented in README, examples are clear and copy-pasteable, links to full docs provided, maintains README quality_
