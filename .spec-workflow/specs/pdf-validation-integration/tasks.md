# Tasks Document

## Implementation Tasks

- [x] 1. Create validation tool installation script
  - Files:
    - `tools/validation/install-tools.ps1` (create)
    - `tools/validation/README.md` (create)
  - Implement PowerShell script to download and install VeraPDF, JHOVE
  - Download VeraPDF installer from official releases
  - Download JHOVE from GitHub releases
  - Extract and configure tools for CLI execution
  - Document tool versions and installation process
  - Purpose: Automate validation tool setup for local dev and CI
  - _Leverage: PowerShell Invoke-WebRequest, Expand-Archive_
  - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3_
  - _Prompt: Role: DevOps Engineer specializing in automation and toolchain management | Task: Create PowerShell script tools/validation/install-tools.ps1 to download and install PDF validation tools. Download VeraPDF installer from downloads.verapdf.org (latest stable), JHOVE from GitHub releases (latest stable). Extract to tools/validation/{tool-name}/ directories. Verify executables work (run with --version). Document installation in README.md with tool versions, system requirements (Java for JHOVE), usage instructions. | Restrictions: Must check if tools already installed before downloading, support Windows and cross-platform where possible, verify checksums if available, handle download failures gracefully. | Success: Script downloads and installs tools successfully, verapdf.bat and jhove.jar are executable, README documents process, script is idempotent (safe to run multiple times)._

- [x] 2. Create FluentPDF.Validation project and define models
  - Files:
    - `src/FluentPDF.Validation/FluentPDF.Validation.csproj` (create)
    - `src/FluentPDF.Validation/Models/ValidationReport.cs` (create)
    - `src/FluentPDF.Validation/Models/VeraPdfResult.cs` (create)
    - `src/FluentPDF.Validation/Models/JhoveResult.cs` (create)
    - `src/FluentPDF.Validation/Models/QpdfResult.cs` (create)
    - `schemas/validation-report.schema.json` (create)
  - Create new .NET 8 class library project
  - Define data models for validation reports
  - Create JSON Schema for ValidationReport
  - Add System.Text.Json, FluentResults, Serilog dependencies
  - Purpose: Establish validation project and data models
  - _Leverage: Result<T> pattern, JSON serialization_
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_
  - _Prompt: Role: C# Developer specializing in data modeling and JSON serialization | Task: Create FluentPDF.Validation class library with data models for validation reports. Define ValidationReport (overall status, file path, date, profile, tool results), VeraPdfResult (compliant, flavour, errors), JhoveResult (format, validity, metadata), QpdfResult (status, errors). Create JSON Schema schemas/validation-report.schema.json defining structure with required fields, types, enums. Use required properties and init-only setters. | Restrictions: Models must be immutable (init-only), JSON serializable, follow naming conventions, include XML documentation. | Success: Project builds successfully, models compile without errors, JSON Schema is valid, models serialize to JSON correctly._

- [x] 3. Implement QpdfWrapper for structural validation
  - Files:
    - `src/FluentPDF.Validation/Wrappers/IQpdfWrapper.cs` (create)
    - `src/FluentPDF.Validation/Wrappers/QpdfWrapper.cs` (create)
    - `tests/FluentPDF.Validation.Tests/Wrappers/QpdfWrapperTests.cs` (create)
  - Create interface and implementation for QPDF CLI wrapper
  - Execute qpdf --check with file path argument
  - Parse exit code and stderr output for error details
  - Return Result<QpdfResult> with validation status
  - Write unit tests with mocked Process execution
  - Purpose: Provide QPDF structural validation capability
  - _Leverage: Process execution patterns, Result<T>_
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_
  - _Prompt: Role: C# Developer with expertise in process execution and CLI integration | Task: Implement QpdfWrapper executing QPDF CLI for structural validation. Create IQpdfWrapper interface with ValidateAsync method. Implement QpdfWrapper executing "qpdf --check {filePath}", capturing exit code and stderr. Parse output for errors (cross-reference issues, encryption errors, etc.). Return Result<QpdfResult> with status (Pass/Fail) and error list. Add timeout (30 seconds), logging with correlation ID. Write unit tests mocking Process execution with sample outputs (valid PDF, corrupted PDF, missing file). | Restrictions: Must use ProcessStartInfo with UseShellExecute=false, validate file path before execution, handle timeouts, log all executions, do not block on stdout/stderr reads. | Success: Wrapper executes QPDF successfully, parses output correctly, returns Result<QpdfResult>, handles errors gracefully, unit tests verify logic with mocked process._

- [x] 4. Implement JhoveWrapper for format validation
  - Files:
    - `src/FluentPDF.Validation/Wrappers/IJhoveWrapper.cs` (create)
    - `src/FluentPDF.Validation/Wrappers/JhoveWrapper.cs` (create)
    - `tests/FluentPDF.Validation.Tests/Wrappers/JhoveWrapperTests.cs` (create)
  - Create interface and implementation for JHOVE CLI wrapper
  - Execute java -jar jhove.jar with PDF module and JSON output
  - Parse JSON output for format validation results
  - Extract metadata (PDF version, page count, security)
  - Write unit tests with sample JHOVE JSON output
  - Purpose: Provide PDF format characterization and validation
  - _Leverage: System.Text.Json, Process execution_
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_
  - _Prompt: Role: C# Developer with expertise in JSON parsing and CLI integration | Task: Implement JhoveWrapper executing JHOVE CLI for format validation. Create IJhoveWrapper interface with ValidateAsync method. Implement wrapper executing "java -jar {jhoveJarPath} -m PDF-hul -h json {filePath}", capturing stdout JSON. Deserialize JSON to JhoveResult with format (PDF version), validity (Well-Formed/Valid/Not Valid), metadata (title, author, creation date, page count). Handle JSON parsing errors, timeouts. Write unit tests with sample JHOVE JSON outputs (valid PDF, invalid format, corrupted). | Restrictions: Verify Java is installed (check JAVA_HOME or PATH), parse JSON robustly (handle missing fields), log execution with structured data, timeout after 30 seconds. | Success: Wrapper executes JHOVE successfully, parses JSON output, extracts metadata, returns Result<JhoveResult>, handles errors, unit tests verify with sample JSON._

- [x] 5. Implement VeraPdfWrapper for PDF/A compliance
  - Files:
    - `src/FluentPDF.Validation/Wrappers/IVeraPdfWrapper.cs` (create)
    - `src/FluentPDF.Validation/Wrappers/VeraPdfWrapper.cs` (create)
    - `tests/FluentPDF.Validation.Tests/Wrappers/VeraPdfWrapperTests.cs` (create)
  - Create interface and implementation for VeraPDF CLI wrapper
  - Execute verapdf.bat with --format json argument
  - Parse JSON output for compliance status, flavour, errors
  - Extract validation errors with rule references
  - Write unit tests with sample VeraPDF JSON output
  - Purpose: Provide PDF/A compliance validation
  - _Leverage: System.Text.Json, Process execution_
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_
  - _Prompt: Role: C# Developer with expertise in PDF/A standards and CLI integration | Task: Implement VeraPdfWrapper executing VeraPDF CLI for PDF/A compliance validation. Create IVeraPdfWrapper interface with ValidateAsync method. Implement wrapper executing "verapdf.bat --format json {filePath}", capturing stdout JSON. Deserialize JSON to VeraPdfResult with compliant (bool), flavour (1a, 1b, 2a, 2b, 2u, 3a, 3b, 3u), errors (list with rule references, page numbers, descriptions). Handle non-compliant PDFs, parsing errors, timeouts. Write unit tests with sample VeraPDF JSON outputs (compliant PDF/A-1b, non-compliant PDF, standard PDF). | Restrictions: Parse VeraPDF JSON structure correctly (nested jobs array), extract all validation errors, log execution, timeout after 30 seconds, handle batch validation results. | Success: Wrapper executes VeraPDF successfully, parses JSON output, extracts compliance status and errors, returns Result<VeraPdfResult>, unit tests verify with sample outputs._

- [x] 6. Implement PdfValidationService orchestrating all tools
  - Files:
    - `src/FluentPDF.Validation/Services/IPdfValidationService.cs` (create)
    - `src/FluentPDF.Validation/Services/PdfValidationService.cs` (create)
    - `tests/FluentPDF.Validation.Tests/Services/PdfValidationServiceTests.cs` (create)
  - Create service interface with ValidateAsync(filePath, profile) method
  - Implement service orchestrating wrappers based on ValidationProfile
  - Execute tools in parallel for Full profile (Task.WhenAll)
  - Aggregate results into ValidationReport
  - Determine overall status (Pass/Warn/Fail) from tool results
  - Write comprehensive unit tests with mocked wrappers
  - Purpose: Provide unified validation API with profile-based execution
  - _Leverage: IVeraPdfWrapper, IJhoveWrapper, IQpdfWrapper, async/await_
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_
  - _Prompt: Role: Backend Service Developer with expertise in service orchestration and async patterns | Task: Implement PdfValidationService orchestrating validation tools. Create IPdfValidationService interface with ValidateAsync(filePath, profile) and VerifyToolsInstalled() methods. Implement service with constructor taking IQpdfWrapper, IJhoveWrapper, IVeraPdfWrapper. Based on ValidationProfile enum (Quick=QPDF, Standard=QPDF+JHOVE, Full=All), execute tools in parallel using Task.WhenAll. Aggregate results into ValidationReport with OverallStatus (Pass if all pass, Warn if warnings, Fail if any fail). Add logging with correlation ID, timeout handling. Write unit tests mocking wrappers, testing all profiles, verifying parallel execution. | Restrictions: Tools must run in parallel for performance, handle individual tool failures gracefully (continue with other tools), log all operations, validate file exists before executing tools. | Success: Service executes validation correctly for all profiles, runs tools in parallel, aggregates results, determines overall status, handles errors, unit tests verify logic with mocked wrappers._

- [x] 7. Create validation test fixtures (valid and invalid PDFs)
  - Files:
    - `tests/Fixtures/validation/valid-pdf17.pdf` (add)
    - `tests/Fixtures/validation/valid-pdfa-1b.pdf` (add)
    - `tests/Fixtures/validation/valid-pdfa-2u.pdf` (add)
    - `tests/Fixtures/validation/invalid-structure.pdf` (add)
    - `tests/Fixtures/validation/invalid-pdfa.pdf` (add)
    - `tests/Fixtures/validation/README.md` (create)
  - Create or source valid PDF samples (PDF 1.7, PDF/A-1b, PDF/A-2u)
  - Create corrupted PDF with invalid structure
  - Create non-compliant PDF claiming PDF/A
  - Document fixture characteristics in README
  - Purpose: Provide test data for validation logic testing
  - _Leverage: Existing test fixtures, PDFium, VeraPDF test suite_
  - _Requirements: 7.1, 7.2, 7.3, 7.4_
  - _Prompt: Role: QA Engineer specializing in test data creation | Task: Create validation test fixtures in tests/Fixtures/validation/. Add valid PDFs: PDF 1.7 (standard document), PDF/A-1b (archival-compliant), PDF/A-2u (archival with Unicode). Create invalid PDFs: corrupted structure (damaged cross-reference table), invalid PDF/A (claims PDF/A but violates rules). Document each fixture in README.md (what it is, expected validation results). Keep files small (< 1MB). | Restrictions: PDFs must be redistributable, files small enough to commit, must trigger expected validation results (valid PDFs pass, invalid PDFs fail with specific errors). | Success: 5 PDF fixtures created, README documents each file, valid PDFs pass validation, invalid PDFs fail with expected errors, files under 1MB each._

- [x] 8. Integration tests with real validation tools
  - Files:
    - `tests/FluentPDF.Validation.Tests/Integration/ValidationIntegrationTests.cs` (create)
  - Create integration tests using real validation tools (not mocked)
  - Test Quick/Standard/Full profiles with valid fixtures
  - Verify valid PDFs pass validation
  - Verify invalid PDFs fail with expected errors
  - Test parallel execution of Full profile
  - Test timeout handling with large PDFs
  - Purpose: Verify validation system works end-to-end with real tools
  - _Leverage: IPdfValidationService, test fixtures, real CLI tools_
  - _Requirements: All functional requirements_
  - _Prompt: Role: QA Integration Engineer with expertise in end-to-end testing | Task: Create integration tests for validation system in ValidationIntegrationTests.cs. Use real PdfValidationService with real wrappers (not mocked) and real CLI tools. Test Quick profile (QPDF only) with valid PDF, verify pass. Test Standard profile (QPDF+JHOVE) with valid PDF, verify format detection. Test Full profile (all tools) with valid PDF/A, verify compliance. Test with invalid PDFs, verify failures with expected errors. Test parallel execution timing (Full profile should be faster than sequential). Add [Trait("Category", "Integration")], require tools installed. | Restrictions: Tests must run with real CLI tools, require tool installation (skip if not available), use test fixtures, verify JSON output structure, check that parallel execution works. | Success: Integration tests pass with real tools, valid PDFs verified correctly, invalid PDFs fail as expected, Full profile executes tools in parallel, all validation profiles work correctly._

- [ ] 9. Integrate validation into PDF generation tests
  - Files:
    - `tests/FluentPDF.Core.Tests/Integration/MergeValidationTests.cs` (create)
    - `tests/FluentPDF.Core.Tests/Integration/SplitValidationTests.cs` (create)
    - `tests/FluentPDF.Core.Tests/Integration/OptimizeValidationTests.cs` (create)
  - Add validation checks to PDF merge operation tests
  - Add validation checks to PDF split operation tests
  - Add validation checks to PDF optimize operation tests
  - Verify generated PDFs pass QPDF structural validation
  - Verify generated PDFs pass JHOVE format validation
  - Purpose: Ensure PDF operations produce valid output
  - _Leverage: IPdfValidationService, PDF generation tests_
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_
  - _Prompt: Role: QA Engineer specializing in integration testing and quality assurance | Task: Create integration tests validating PDF generation operations. In MergeValidationTests, test merging 2+ PDFs and validate output with IPdfValidationService (Quick profile). In SplitValidationTests, test splitting multi-page PDF and validate outputs. In OptimizeValidationTests, test PDF optimization and verify output maintains validity. If validation fails, include ValidationReport in test failure message. Use existing PDF generation logic, add validation as final step. Add [Trait("Category", "Integration")]. | Restrictions: Tests require PDF generation operations working, require validation tools installed, use test fixtures, fail test if validation fails, include full validation report in failure message. | Success: Integration tests validate generated PDFs, merged PDFs pass validation, split PDFs pass validation, optimized PDFs pass validation, validation failures cause test failures with detailed reports._

- [ ] 10. Update CI workflows to install validation tools
  - Files:
    - `.github/workflows/build.yml` (modify)
    - `.github/workflows/test.yml` (modify)
  - Add step to run tools/validation/install-tools.ps1 in build.yml
  - Verify tools are installed and executable in CI
  - Run validation integration tests in test.yml
  - Upload validation reports as artifacts
  - Purpose: Enable validation in CI pipeline
  - _Leverage: GitHub Actions, validation installation script_
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_
  - _Prompt: Role: DevOps Engineer specializing in CI/CD pipeline integration | Task: Update GitHub Actions workflows to support PDF validation. In build.yml, add step after native library build to run tools/validation/install-tools.ps1, verify tools installed (run verapdf --version, java -jar jhove.jar -v). In test.yml, run validation integration tests (dotnet test --filter "Category=Integration"), upload validation report JSON files as artifacts. Ensure Java is available (setup-java action). Handle tool installation failures gracefully (fail build if tools don't install). | Restrictions: Must run on Windows agents, install tools in consistent location, cache tool downloads if possible, verify tool installation before tests, include tool versions in workflow logs. | Success: Build workflow installs validation tools successfully, tools are executable in CI, integration tests run with real tools, validation reports uploaded as artifacts, workflow fails if tools cannot be installed._

- [ ] 11. Documentation and final verification
  - Files:
    - `src/FluentPDF.Validation/README.md` (create)
    - `docs/VALIDATION.md` (create)
    - `README.md` (update - add validation section)
  - Document IPdfValidationService usage
  - Document validation tool installation and requirements
  - Document validation profiles and when to use each
  - Document ValidationReport structure
  - Add validation capabilities to main README
  - Verify all validation functionality works end-to-end
  - Purpose: Ensure validation system is documented and complete
  - _Leverage: Existing documentation structure_
  - _Requirements: All requirements_
  - _Prompt: Role: Technical Writer performing final documentation and verification | Task: Create comprehensive documentation for validation system. Write src/FluentPDF.Validation/README.md explaining: how to install tools (run install-tools.ps1), how to use IPdfValidationService (code examples for each profile), how to interpret ValidationReport (status meanings, error formats). Create docs/VALIDATION.md with: validation architecture overview, tool descriptions (VeraPDF, JHOVE, QPDF), profile comparison table (Quick vs Standard vs Full), integration patterns (how to use in tests). Update main README.md with validation capabilities section. Verify end-to-end: install tools, run validation on test PDFs, check reports, run CI workflow. | Restrictions: Documentation must be clear for developers unfamiliar with PDF validation, include code examples, link to tool documentation, verify all examples work. | Success: README documents tool installation and usage, VALIDATION.md provides comprehensive overview, main README updated, all code examples run successfully, end-to-end verification complete._

## Summary

This spec implements comprehensive PDF validation integration:
- VeraPDF CLI wrapper for PDF/A compliance validation
- JHOVE CLI wrapper for format characterization
- QPDF CLI wrapper for structural validation
- Unified PdfValidationService with profile-based execution (Quick/Standard/Full)
- JSON validation reports with schema
- Integration tests validating PDF generation operations
- CI integration with automated validation
- Comprehensive documentation and tool installation automation

**Next steps after completion:**
- Use validation in all PDF generation tests
- Monitor validation trends over time
- Integrate validation reports into quality dashboard
- Expand validation profiles for specific use cases (e.g., PDF/X for print)
